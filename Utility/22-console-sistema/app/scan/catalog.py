"""Catalogo delle chiamate: cosa leggere di ogni servizio.

Due livelli:

1. **Operazioni generiche** - per ogni servizio si chiamano tutte le `list_*`, `describe_*`, `get_*`
   che non hanno parametri obbligatori (o che ne hanno solo di fissi, in `OVERRIDES`). E' il modo
   per non dimenticare niente senza scrivere a mano centinaia di chiamate. Si escludono i cataloghi
   pubblici di AWS (tipi di istanza, versioni dei motori, AMI pubbliche), lo storico (eventi,
   esecuzioni, job) e gli elenchi di dati personali (numeri e indirizzi soppressi).
2. **Regole di dettaglio** (`RULES`) - le chiamate per singola risorsa, che prendono l'id da una
   risposta precedente: listener di un load balancer, parametri di un parameter group, attributi
   di una coda... Una regola puo' leggere il risultato di un'altra (catene).

Le letture dei dati veri e propri (righe dei DB, oggetti S3, messaggi delle code, eventi dei log)
non ci sono: richiedono tutte parametri che il catalogo non fornisce.
"""
from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Callable

# Servizi letti una volta sola (cartella `global`, endpoint us-east-1).
GLOBAL_SERVICES = {"iam", "route53", "route53domains", "cloudfront", "organizations", "budgets", "s3"}
# Servizi regionali da leggere ANCHE nel globale (us-east-1) per cio' che serve a CloudFront.
GLOBAL_ALSO = {"acm", "wafv2"}

PREFIXES = ("list_", "describe_", "get_")

# Sottostringhe che escludono un'operazione in qualunque servizio: cataloghi, prezzi, storico.
EXCLUDE_ANY = ("offering", "engine_version", "price", "history", "describe_events", "_recommendation",
               "describe_available_", "list_available_", "get_available_", "_quotas")

# Esclusioni per servizio.
EXCLUDE: dict[str, set[str]] = {
    "ec2": {
        "describe_application_status", "describe_application_status_checks",
        "describe_aws_network_performance_metric_subscriptions", "describe_bundle_tasks",
        "describe_capacity_block_status", "describe_capacity_manager_data_exports",
        "describe_capacity_reservation_cancellation_quotes",
        "describe_capacity_reservation_date_change_quotes", "describe_conversion_tasks",
        "describe_export_image_tasks", "describe_export_tasks", "describe_image_usage_report_entries",
        "describe_image_usage_reports", "describe_import_image_tasks",
        "describe_import_snapshot_tasks", "describe_instance_image_metadata",
        "describe_instance_status", "describe_instance_topology", "describe_instance_type_offerings",
        "describe_instance_types", "describe_launch_template_versions",
        "describe_mac_modification_tasks", "describe_regions", "describe_replace_root_volume_tasks",
        "describe_spot_datafeed_subscription", "describe_spot_instance_requests",
        "describe_store_image_tasks", "describe_trunk_interface_associations",
        "describe_volume_status", "describe_vpc_endpoint_services", "get_aws_network_performance_data",
        "get_instance_types_from_instance_requirements", "get_vpn_connection_device_types"},
    "rds": {
        "describe_db_major_engine_versions", "describe_db_recommendations",
        "describe_event_categories", "describe_export_tasks", "describe_pending_maintenance_actions",
        "describe_source_regions"},
    "elasticache": {
        "describe_service_updates", "describe_update_actions", "list_allowed_node_type_modifications"},
    "ssm": {
        "describe_association", "describe_automation_executions", "describe_instance_information",
        "describe_instance_properties", "describe_inventory_deletions",
        "describe_maintenance_window_schedule", "describe_ops_items", "describe_patch_baselines",
        "get_inventory", "get_inventory_schema", "get_ops_summary", "list_command_invocations",
        "list_commands", "list_compliance_items", "list_inventory_entries", "list_nodes",
        "list_nodes_summary", "list_ops_item_events", "list_ops_item_related_items",
        "list_ops_metadata", "list_resource_compliance_summaries"},
    "cloudformation": {
        "describe_account_limits", "describe_publisher", "describe_stack_events",
        "describe_stack_resources", "describe_type", "get_hook_result", "get_template",
        "get_template_summary", "list_hook_results", "list_stack_set_auto_deployment_targets",
        "list_type_registrations", "list_type_versions"},
    "logs": {
        "describe_export_tasks", "describe_import_tasks", "describe_log_streams", "describe_queries",
        "get_log_group_fields", "list_anomalies", "list_log_anomaly_detectors", "list_log_groups"},
    "cloudwatch": {"list_metrics", "describe_alarm_history", "get_metric_widget_image"},
    "lambda": set(),
    "iam": {
        "get_credential_report", "get_login_profile", "get_outbound_web_identity_federation_info",
        "get_user", "list_access_keys", "list_mfa_devices", "list_service_specific_credentials",
        "list_signing_certificates", "list_ssh_public_keys"},
    "ecr": {"get_authorization_token"},
    "secretsmanager": {"get_random_password"},
    "sns": {"list_phone_numbers_opted_out", "list_sms_sandbox_phone_numbers", "list_origination_numbers"},
    "sesv2": {"list_suppressed_destinations", "list_deliverability_test_reports", "list_import_jobs",
              "list_export_jobs", "get_deliverability_dashboard_options", "list_recommendations",
              "get_blacklist_reports", "list_reputation_entities"},
    "ses": {"get_send_statistics", "list_verified_email_addresses"},
    "apigateway": {"get_sdk_types"},
    "elbv2": {
        "describe_listeners", "describe_rules", "describe_ssl_policies"},
    "elb": {"describe_load_balancer_policy_types", "describe_load_balancer_policies"},
    "autoscaling": {
        "describe_adjustment_types", "describe_auto_scaling_instances",
        "describe_auto_scaling_notification_types", "describe_lifecycle_hook_types",
        "describe_metric_collection_types", "describe_scaling_activities",
        "describe_scaling_process_types", "describe_termination_policy_types"},
    "ecs": {"list_tasks", "list_container_instances", "describe_clusters", "list_services"},
    "eks": {"describe_addon_versions", "list_access_policies", "describe_cluster_versions"},
    "elasticbeanstalk": {
        "describe_configuration_options", "describe_environment_health",
        "describe_environment_managed_actions", "describe_environment_resources",
        "describe_instances_health", "describe_platform_version", "list_available_solution_stacks",
        "list_platform_branches", "list_platform_versions"},
    "dynamodb": {"describe_endpoints", "list_contributor_insights"},
    "route53": {"get_checker_ip_ranges", "get_geo_location", "list_geo_locations"},
    "route53domains": {"list_operations", "list_prices", "view_billing"},
    "codebuild": {
        "list_build_batches", "list_build_batches_for_project", "list_builds",
        "list_builds_for_project", "list_curated_environment_images", "list_reports", "list_sandboxes"},
    "codepipeline": {"list_action_types", "list_action_executions", "list_rule_types"},
    "codedeploy": {
        "list_deployment_instances", "list_deployment_targets", "list_deployments",
        "list_git_hub_account_token_names"},
    "backup": {
        "get_supported_resource_types", "list_backup_job_summaries", "list_backup_jobs",
        "list_backup_plan_templates", "list_copy_job_summaries", "list_copy_jobs",
        "list_indexed_recovery_points", "list_report_jobs", "list_restore_job_summaries",
        "list_restore_jobs", "list_scan_job_summaries", "list_scan_jobs"},
    "cloudtrail": {
        "describe_query", "get_event_configuration", "get_insight_selectors",
        "list_insights_metric_data", "list_public_keys", "list_queries", "lookup_events"},
    "lightsail": {
        "get_active_names", "get_auto_snapshots", "get_blueprints", "get_bucket_bundles",
        "get_bundles", "get_container_api_metadata", "get_container_service_powers",
        "get_distribution_bundles", "get_distribution_latest_cache_reset",
        "get_load_balancer_tls_policies", "get_operations", "get_regions",
        "get_relational_database_blueprints", "get_relational_database_bundles",
        "get_relational_database_events"},
    "redshift": {
        "describe_cluster_tracks", "describe_cluster_versions", "describe_default_cluster_parameters",
        "describe_event_categories", "describe_orderable_cluster_options",
        "describe_reserved_node_exchange_status", "describe_storage", "describe_table_restore_status",
        "get_cluster_credentials_with_iam"},
    "glue": {
        "get_catalog_import_status", "get_crawler_metrics", "get_data_catalog_export_configuration",
        "get_dataflow_graph", "get_dev_endpoints", "get_job_runs", "get_ml_transforms",
        "get_schema_version", "list_asset_types", "list_column_statistics_task_runs",
        "list_connection_types", "list_crawls", "list_data_quality_results",
        "list_data_quality_results_v2", "list_data_quality_ruleset_evaluation_runs",
        "list_data_quality_statistic_annotations", "list_data_quality_statistics",
        "list_dev_endpoints", "list_entities", "list_form_types", "list_glossaries",
        "list_integration_resource_properties", "list_integration_table_properties", "list_sessions"},
    "athena": {
        "list_application_dpu_sizes", "list_engine_versions", "list_query_executions"},
    "es": {"get_compatible_elasticsearch_versions", "list_elasticsearch_versions",
           "list_elasticsearch_instance_types"},
    "opensearch": {"list_versions", "get_compatible_versions", "list_instance_type_details"},
    "kafka": {"list_kafka_versions", "get_compatible_kafka_versions"},
    "mq": {"describe_broker_engine_types", "describe_broker_instance_options"},
    "memorydb": {"describe_service_updates", "describe_engine_versions"},
    "guardduty": {
        "get_organization_statistics", "list_malware_scans", "list_organization_admin_accounts"},
    "securityhub": {
        "describe_products", "describe_products_v2", "describe_standards", "get_finding_aggregator",
        "get_findings", "get_findings_v2", "get_insights", "get_resources_v2", "list_export_jobs_v2",
        "list_security_control_definitions", "list_standards_control_associations"},
    "config": {
        "describe_remediation_execution_status", "get_compliance_details_by_config_rule",
        "get_compliance_details_by_resource", "get_custom_rule_policy",
        "list_conformance_pack_compliance_scores", "list_resource_evaluations"},
    "dms": {
        "describe_applicable_individual_assessments", "describe_endpoint_settings",
        "describe_endpoint_types", "describe_engine_versions", "describe_event_categories",
        "describe_fleet_advisor_collectors", "describe_fleet_advisor_databases",
        "describe_fleet_advisor_lsa_analysis", "describe_fleet_advisor_schema_object_summary",
        "describe_fleet_advisor_schemas", "describe_orderable_replication_instances",
        "describe_replication_task_assessment_results", "describe_replication_task_assessment_runs",
        "describe_replication_task_individual_assessments", "list_tags_for_resource"},
    "batch": {
        "list_jobs", "list_service_jobs"},
    "xray": {
        "get_group", "get_insight_summaries", "get_sampling_statistic_summaries", "get_service_graph",
        "get_time_series_service_statistics", "get_trace_graph", "get_trace_summaries"},
    "kms": {"list_retirable_grants"},
    "organizations": set(),
    "network-firewall": {
        "describe_container_association", "describe_firewall", "describe_firewall_metadata",
        "describe_firewall_policy", "describe_logging_configuration", "describe_proxy",
        "describe_proxy_configuration", "describe_proxy_rule_group", "describe_rule_group",
        "describe_rule_group_metadata", "describe_rule_group_summary",
        "describe_tls_inspection_configuration", "list_analysis_reports"},
    "resourcegroupstaggingapi": {"get_compliance_summary", "get_tag_keys"},
    "accessanalyzer": set(),
    "transfer": {"list_security_policies"},
    "apprunner": set(),
    "route53resolver": set(),
    "ram": {"list_permissions", "list_resource_types", "list_permission_versions"},
    "codecommit": {"get_branch"},
    "resource-groups": {"get_group", "get_group_configuration", "get_group_query", "list_group_resources"},
    "wafv2": {"get_rule_group", "get_web_acl"},
    "kinesis": {"describe_stream", "describe_stream_consumer", "describe_stream_summary", "list_shards", "list_tags_for_stream"},
    "stepfunctions": {"list_executions"},
    "efs": {"describe_mount_targets"},
    "cloudfront": {"get_realtime_log_config", "list_distribution_tenants_by_customization", "list_distributions_by_realtime_log_config"},
    "servicediscovery": {"list_operations"},
    "s3control": {"list_multi_region_access_points"},
}

# Parametri aggiunti d'ufficio. Un dict = una chiamata; una lista di dict = piu' chiamate, salvate
# insieme (chiave = parametri). {"regional": ..., "global": ...} = diverso fra region e globale.
# "{account}" e' sostituito con l'id dell'account.
OVERRIDES: dict[tuple[str, str], Any] = {
    ("ec2", "describe_images"): {"Owners": ["self"]},
    ("ec2", "describe_snapshots"): {"OwnerIds": ["self"]},
    ("ec2", "describe_fpga_images"): {"Owners": ["self"]},
    ("ec2", "describe_security_group_vpc_associations"): None,
    ("rds", "describe_db_snapshots"): {"SnapshotType": "manual"},
    ("rds", "describe_db_cluster_snapshots"): {"SnapshotType": "manual"},
    ("ssm", "list_documents"): {"Filters": [{"Key": "Owner", "Values": ["Self"]}]},
    ("iam", "list_policies"): {"Scope": "Local"},
    ("cloudformation", "list_stacks"): {"StackStatusFilter": [
        "CREATE_IN_PROGRESS", "CREATE_FAILED", "CREATE_COMPLETE", "ROLLBACK_IN_PROGRESS", "ROLLBACK_FAILED",
        "ROLLBACK_COMPLETE", "DELETE_IN_PROGRESS", "DELETE_FAILED", "UPDATE_IN_PROGRESS",
        "UPDATE_COMPLETE_CLEANUP_IN_PROGRESS", "UPDATE_COMPLETE", "UPDATE_FAILED", "UPDATE_ROLLBACK_IN_PROGRESS",
        "UPDATE_ROLLBACK_FAILED", "UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS", "UPDATE_ROLLBACK_COMPLETE",
        "REVIEW_IN_PROGRESS", "IMPORT_IN_PROGRESS", "IMPORT_COMPLETE", "IMPORT_ROLLBACK_IN_PROGRESS",
        "IMPORT_ROLLBACK_FAILED", "IMPORT_ROLLBACK_COMPLETE"]},
    ("cloudformation", "list_types"): {"Visibility": "PRIVATE"},
    ("cloudfront", "list_cache_policies"): {"Type": "custom"},
    ("cloudfront", "list_origin_request_policies"): {"Type": "custom"},
    ("cloudfront", "list_response_headers_policies"): {"Type": "custom"},
    ("cognito-idp", "list_user_pools"): {"MaxResults": 60},
    ("cognito-identity", "list_identity_pools"): {"MaxResults": 60},
    ("wafv2", "list_web_acls"): {"regional": {"Scope": "REGIONAL"}, "global": {"Scope": "CLOUDFRONT"}},
    ("wafv2", "list_rule_groups"): {"regional": {"Scope": "REGIONAL"}, "global": {"Scope": "CLOUDFRONT"}},
    ("wafv2", "list_ip_sets"): {"regional": {"Scope": "REGIONAL"}, "global": {"Scope": "CLOUDFRONT"}},
    ("wafv2", "list_regex_pattern_sets"): {"regional": {"Scope": "REGIONAL"}, "global": {"Scope": "CLOUDFRONT"}},
    ("wafv2", "list_managed_rule_sets"): {"regional": {"Scope": "REGIONAL"}, "global": {"Scope": "CLOUDFRONT"}},
    ("wafv2", "list_logging_configurations"): {"regional": {"Scope": "REGIONAL"}, "global": {"Scope": "CLOUDFRONT"}},
    ("organizations", "list_policies"): [{"Filter": f} for f in (
        "SERVICE_CONTROL_POLICY", "RESOURCE_CONTROL_POLICY", "TAG_POLICY", "BACKUP_POLICY",
        "AISERVICES_OPT_OUT_POLICY", "DECLARATIVE_POLICY_EC2")],
    ("application-autoscaling", "describe_scalable_targets"): [{"ServiceNamespace": n} for n in (
        "ecs", "rds", "dynamodb", "lambda", "elasticache", "ec2", "kafka", "sagemaker", "comprehend",
        "custom-resource", "appstream", "cassandra", "neptune", "workspaces")],
    ("application-autoscaling", "describe_scaling_policies"): [{"ServiceNamespace": n} for n in (
        "ecs", "rds", "dynamodb", "lambda", "elasticache")],
    ("application-autoscaling", "describe_scheduled_actions"): [{"ServiceNamespace": n} for n in (
        "ecs", "rds", "dynamodb", "lambda", "elasticache")],
    ("ram", "get_resource_shares"): [{"resourceOwner": "SELF"}, {"resourceOwner": "OTHER-ACCOUNTS"}],
    ("ram", "list_resources"): {"resourceOwner": "SELF"},
    ("ram", "list_principals"): {"resourceOwner": "SELF"},
    ("budgets", "describe_budgets"): {"AccountId": "{account}"},
    ("budgets", "describe_budget_actions_for_account"): {"AccountId": "{account}"},
    ("s3control", "get_public_access_block"): {"AccountId": "{account}"},
    ("s3control", "list_access_points"): {"AccountId": "{account}"},
    ("s3control", "list_multi_region_access_points"): {"AccountId": "{account}"},
    ("s3control", "list_storage_lens_configurations"): {"AccountId": "{account}"},
    ("s3control", "list_access_grants_instances"): {"AccountId": "{account}"},
    ("s3control", "list_access_points_for_object_lambda"): {"AccountId": "{account}"},
    ("events", "list_rules"): None,
    ("apigateway", "get_api_keys"): {"includeValues": False},
}


def override_for(service: str, op: str, region: str) -> Any:
    """Parametri d'ufficio per l'operazione; `...` = nessun override (chiamata normale)."""
    if (service, op) not in OVERRIDES:
        return ...
    v = OVERRIDES[(service, op)]
    if isinstance(v, dict) and set(v) <= {"regional", "global"} and v:
        return v.get("global" if region == "global" else "regional", ...)
    return v


def generic_ops(client, service: str, region: str) -> list[tuple[str, Any]]:
    """[(operazione, override)] delle chiamate generiche di un servizio in una region."""
    out = []
    model = client.meta.service_model
    excluded = EXCLUDE.get(service, set())
    for op, api in sorted(client.meta.method_to_api_mapping.items()):
        if not op.startswith(PREFIXES) or op in excluded or any(x in op for x in EXCLUDE_ANY):
            continue
        ov = override_for(service, op, region)
        if ov is None:
            continue  # escluso esplicitamente
        if ov is ...:
            shape = model.operation_model(api).input_shape
            if shape is not None and shape.required_members:
                continue
            out.append((op, None))
        else:
            out.append((op, ov))
    return out


# ------------------------------------------------------------------------------------------ regole

@dataclass
class Rule:
    """Una chiamata per ogni elemento estratto (con `path`, JMESPath) dal risultato di `source`.

    `param`: nome del parametro che riceve l'elemento, oppure funzione (elemento, chiave della
    sorgente, region) -> kwargs. Con `chunk` l'elemento e' una lista di al massimo `chunk` id.
    """
    service: str
    source: str
    path: str
    op: str
    param: str | Callable[..., dict]
    extra: dict = field(default_factory=dict)
    chunk: int = 0
    paginate: bool = True


def _r(service: str, source: str, path: str, param: str | Callable, *ops: str, **kw) -> list[Rule]:
    return [Rule(service, source, path, op, param, **kw) for op in ops]


def _scope(region: str) -> str:
    return "CLOUDFRONT" if region == "global" else "REGIONAL"


RULES: list[Rule] = [
    # IAM (global): il dump completo e' get_account_authorization_details (operazione generica)
    *_r("iam", "get_account_authorization_details", "UserDetailList[].UserName", "UserName",
        "list_access_keys", "get_login_profile", "list_mfa_devices", "list_ssh_public_keys",
        "list_service_specific_credentials", "list_signing_certificates"),
    *_r("iam", "list_saml_providers", "SAMLProviderList[].Arn", "SAMLProviderArn", "get_saml_provider"),
    *_r("iam", "list_open_id_connect_providers", "OpenIDConnectProviderList[].Arn",
        "OpenIDConnectProviderArn", "get_open_id_connect_provider"),
    *_r("iam", "list_server_certificates", "ServerCertificateMetadataList[].ServerCertificateName",
        "ServerCertificateName", "get_server_certificate"),
    # CloudFormation
    *_r("cloudformation", "list_stacks", "StackSummaries[?StackStatus!='DELETE_COMPLETE'].StackName",
        "StackName", "get_stack_policy"),
    *_r("cloudformation", "list_stacks", "StackSummaries[?StackStatus!='DELETE_COMPLETE'].StackName",
        "StackName", "list_stack_resources"),
    *_r("cloudformation", "list_stack_sets", "Summaries[].StackSetName", "StackSetName",
        "describe_stack_set", "list_stack_instances"),
    # EC2
    *_r("ec2", "describe_launch_templates", "LaunchTemplates[].LaunchTemplateId", "LaunchTemplateId",
        "describe_launch_template_versions"),
    *_r("ec2", "describe_instances", "Reservations[].Instances[].InstanceId",
        lambda i, k, r: {"InstanceId": i, "Attribute": "userData"}, "describe_instance_attribute"),
    # Load balancer
    *_r("elbv2", "describe_load_balancers", "LoadBalancers[].LoadBalancerArn", "LoadBalancerArn",
        "describe_listeners", "describe_load_balancer_attributes"),
    *_r("elbv2", "describe_listeners", "Listeners[].ListenerArn", "ListenerArn",
        "describe_rules", "describe_listener_certificates", "describe_listener_attributes"),
    *_r("elbv2", "describe_target_groups", "TargetGroups[].TargetGroupArn", "TargetGroupArn",
        "describe_target_group_attributes"),
    *_r("elbv2", "describe_load_balancers", "LoadBalancers[].LoadBalancerArn",
        lambda ids, k, r: {"ResourceArns": ids}, "describe_tags", chunk=20),
    *_r("elbv2", "describe_target_groups", "TargetGroups[].TargetGroupArn",
        lambda ids, k, r: {"ResourceArns": ids}, "describe_tags", chunk=20),
    *_r("elb", "describe_load_balancers", "LoadBalancerDescriptions[].LoadBalancerName", "LoadBalancerName",
        "describe_load_balancer_attributes"),
    # ECS
    *_r("ecs", "list_clusters", "clusterArns[]",
        lambda ids, k, r: {"clusters": ids, "include": ["ATTACHMENTS", "CONFIGURATIONS", "SETTINGS",
                                                        "STATISTICS", "TAGS"]}, "describe_clusters", chunk=100),
    *_r("ecs", "list_clusters", "clusterArns[]", "cluster", "list_services"),
    *_r("ecs", "list_services", "serviceArns[]",
        lambda ids, k, r: {"cluster": k, "services": ids, "include": ["TAGS"]}, "describe_services", chunk=10),
    *_r("ecs", "list_task_definitions", "taskDefinitionArns[]",
        lambda i, k, r: {"taskDefinition": i, "include": ["TAGS"]}, "describe_task_definition"),
    # Lambda
    *_r("lambda", "list_functions", "Functions[].FunctionName", "FunctionName",
        "get_function", "get_policy", "list_aliases", "list_versions_by_function", "list_function_url_configs",
        "list_function_event_invoke_configs", "list_provisioned_concurrency_configs",
        "get_function_concurrency", "get_function_code_signing_config", "get_runtime_management_config",
        "get_function_recursion_config"),
    *_r("lambda", "list_layers", "Layers[].LayerName", "LayerName", "list_layer_versions"),
    # RDS / ElastiCache / Redshift / MemoryDB
    *_r("rds", "describe_db_parameter_groups",
        "DBParameterGroups[?!starts_with(DBParameterGroupName, 'default.')].DBParameterGroupName",
        "DBParameterGroupName", "describe_db_parameters"),
    *_r("rds", "describe_db_cluster_parameter_groups",
        "DBClusterParameterGroups[?!starts_with(DBClusterParameterGroupName, 'default.')]"
        ".DBClusterParameterGroupName", "DBClusterParameterGroupName", "describe_db_cluster_parameters"),
    *_r("elasticache", "describe_cache_parameter_groups",
        "CacheParameterGroups[?!starts_with(CacheParameterGroupName, 'default.')].CacheParameterGroupName",
        "CacheParameterGroupName", "describe_cache_parameters"),
    *_r("redshift", "describe_cluster_parameter_groups",
        "ParameterGroups[?!starts_with(ParameterGroupName, 'default.')].ParameterGroupName",
        "ParameterGroupName", "describe_cluster_parameters"),
    # Code, notifiche, eventi
    *_r("sqs", "list_queues", "QueueUrls[]", lambda i, k, r: {"QueueUrl": i, "AttributeNames": ["All"]},
        "get_queue_attributes"),
    *_r("sqs", "list_queues", "QueueUrls[]", "QueueUrl", "list_queue_tags"),
    *_r("sns", "list_topics", "Topics[].TopicArn", "TopicArn", "get_topic_attributes"),
    *_r("sns", "list_topics", "Topics[].TopicArn", "ResourceArn", "list_tags_for_resource",
        "get_data_protection_policy"),
    *_r("sns", "list_subscriptions", "Subscriptions[?starts_with(SubscriptionArn, 'arn:')].SubscriptionArn",
        "SubscriptionArn", "get_subscription_attributes"),
    *_r("events", "list_event_buses", "EventBuses[].Name", "EventBusName", "list_rules"),
    *_r("events", "list_rules", "Rules[]", lambda i, k, r: {"Rule": i["Name"], "EventBusName": i["EventBusName"]},
        "list_targets_by_rule"),
    *_r("events", "list_connections", "Connections[].Name", "Name", "describe_connection"),
    *_r("events", "list_api_destinations", "ApiDestinations[].Name", "Name", "describe_api_destination"),
    *_r("scheduler", "list_schedules", "Schedules[]", lambda i, k, r: {"Name": i["Name"], "GroupName": i["GroupName"]},
        "get_schedule"),
    *_r("stepfunctions", "list_state_machines", "stateMachines[].stateMachineArn", "stateMachineArn",
        "describe_state_machine"),
    # KMS
    *_r("kms", "list_keys", "Keys[].KeyId", "KeyId", "describe_key", "get_key_rotation_status",
        "list_resource_tags", "list_grants", "list_key_policies"),
    *_r("kms", "list_keys", "Keys[].KeyId", lambda i, k, r: {"KeyId": i, "PolicyName": "default"},
        "get_key_policy"),
    # Log e metriche
    *_r("logs", "describe_log_groups", "logGroups[].logGroupName", "logGroupName",
        "describe_subscription_filters"),
    *_r("cloudwatch", "list_dashboards", "DashboardEntries[].DashboardName", "DashboardName", "get_dashboard"),
    # DNS, CDN, certificati, WAF
    *_r("route53", "list_hosted_zones", "HostedZones[].Id", "HostedZoneId", "list_resource_record_sets"),
    *_r("route53", "list_hosted_zones", "HostedZones[].Id", "Id", "get_hosted_zone"),
    *_r("route53", "list_hosted_zones", "HostedZones[?Config.PrivateZone==`false`].Id", "HostedZoneId",
        "get_dnssec"),
    *_r("route53domains", "list_domains", "Domains[].DomainName", "DomainName", "get_domain_detail"),
    *_r("cloudfront", "list_distributions", "DistributionList.Items[].Id", "Id", "get_distribution_config"),
    *_r("cloudfront", "list_distributions", "DistributionList.Items[].ARN", "Resource", "list_tags_for_resource"),
    *_r("acm", "list_certificates", "CertificateSummaryList[].CertificateArn", "CertificateArn",
        "describe_certificate"),
    *_r("acm", "list_certificates", "CertificateSummaryList[?Status=='ISSUED'].CertificateArn", "CertificateArn",
        "get_certificate"),
    *_r("wafv2", "list_web_acls", "WebACLs[]",
        lambda i, k, r: {"Name": i["Name"], "Id": i["Id"], "Scope": _scope(r)}, "get_web_acl"),
    *_r("wafv2", "list_rule_groups", "RuleGroups[]",
        lambda i, k, r: {"Name": i["Name"], "Id": i["Id"], "Scope": _scope(r)}, "get_rule_group"),
    *_r("wafv2", "list_ip_sets", "IPSets[]",
        lambda i, k, r: {"Name": i["Name"], "Id": i["Id"], "Scope": _scope(r)}, "get_ip_set"),
    *_r("wafv2", "list_regex_pattern_sets", "RegexPatternSets[]",
        lambda i, k, r: {"Name": i["Name"], "Id": i["Id"], "Scope": _scope(r)}, "get_regex_pattern_set"),
    *_r("wafv2", "list_web_acls", "WebACLs[].ARN", "ResourceArn", "get_logging_configuration"),
    *_r("wafv2", "list_web_acls", "WebACLs[].ARN", "WebACLArn", "list_resources_for_web_acl"),
    # Email
    *_r("sesv2", "list_email_identities", "EmailIdentities[].IdentityName", "EmailIdentity",
        "get_email_identity", "get_email_identity_policies"),
    *_r("sesv2", "list_configuration_sets", "ConfigurationSets[]", "ConfigurationSetName",
        "get_configuration_set", "get_configuration_set_event_destinations"),
    *_r("sesv2", "list_email_templates", "TemplatesMetadata[].TemplateName", "TemplateName", "get_email_template"),
    *_r("sesv2", "list_contact_lists", "ContactLists[].ContactListName", "ContactListName", "get_contact_list"),
    *_r("ses", "list_receipt_rule_sets", "RuleSets[].Name", "RuleSetName", "describe_receipt_rule_set"),
    *_r("ses", "list_templates", "TemplatesMetadata[].Name", "TemplateName", "get_template"),
    *_r("ses", "list_identities", "Identities[]", lambda ids, k, r: {"Identities": ids},
        "get_identity_verification_attributes", "get_identity_dkim_attributes",
        "get_identity_mail_from_domain_attributes", "get_identity_notification_attributes", chunk=100),
    # DynamoDB (solo struttura, niente dati)
    *_r("dynamodb", "list_tables", "TableNames[]", "TableName", "describe_table", "describe_continuous_backups",
        "describe_time_to_live", "describe_kinesis_streaming_destination"),
    # API Gateway
    *_r("apigateway", "get_rest_apis", "items[].id", "restApiId", "get_stages", "get_authorizers", "get_models",
        "get_request_validators", "get_gateway_responses", "get_documentation_parts", "get_deployments"),
    *_r("apigateway", "get_rest_apis", "items[].id", lambda i, k, r: {"restApiId": i, "embed": ["methods"]},
        "get_resources"),
    *_r("apigateway", "get_domain_names", "items[].domainName", "domainName", "get_base_path_mappings"),
    *_r("apigateway", "get_usage_plans", "items[].id", "usagePlanId", "get_usage_plan_keys"),
    *_r("apigatewayv2", "get_apis", "Items[].ApiId", "ApiId", "get_routes", "get_integrations", "get_stages",
        "get_authorizers", "get_models", "get_deployments"),
    *_r("apigatewayv2", "get_domain_names", "Items[].DomainName", "DomainName", "get_api_mappings"),
    # Cognito (configurazione dei pool, non gli utenti)
    *_r("cognito-idp", "list_user_pools", "UserPools[].Id", "UserPoolId", "describe_user_pool",
        "get_user_pool_mfa_config", "list_identity_providers", "list_groups"),
    *_r("cognito-idp", "list_user_pools", "UserPools[].Id", lambda i, k, r: {"UserPoolId": i, "MaxResults": 60},
        "list_user_pool_clients"),
    *_r("cognito-idp", "list_user_pools", "UserPools[].Id", lambda i, k, r: {"UserPoolId": i, "MaxResults": 50},
        "list_resource_servers"),
    *_r("cognito-idp", "list_user_pool_clients", "UserPoolClients[]",
        lambda i, k, r: {"UserPoolId": i["UserPoolId"], "ClientId": i["ClientId"]}, "describe_user_pool_client"),
    *_r("cognito-idp", "describe_user_pool", "UserPool.Domain", "Domain", "describe_user_pool_domain"),
    *_r("cognito-identity", "list_identity_pools", "IdentityPools[].IdentityPoolId", "IdentityPoolId",
        "describe_identity_pool", "get_identity_pool_roles"),
    # Container registry
    *_r("ecr", "describe_repositories", "repositories[].repositoryName", "repositoryName",
        "get_repository_policy", "get_lifecycle_policy"),
    *_r("ecr", "describe_repositories", "repositories[].repositoryArn", "resourceArn", "list_tags_for_resource"),
    # SSM: documenti propri (il contenuto e' nel JSON); i parametri sono nella sezione segreti
    *_r("ssm", "list_documents", "DocumentIdentifiers[].Name", "Name", "get_document", "describe_document"),
    *_r("ssm", "describe_maintenance_windows", "WindowIdentities[].WindowId", "WindowId",
        "describe_maintenance_window_targets", "describe_maintenance_window_tasks"),
    # Backup
    *_r("backup", "list_backup_plans", "BackupPlansList[].BackupPlanId", "BackupPlanId",
        "get_backup_plan", "list_backup_selections"),
    *_r("backup", "list_backup_selections", "BackupSelectionsList[]",
        lambda i, k, r: {"BackupPlanId": i["BackupPlanId"], "SelectionId": i["SelectionId"]}, "get_backup_selection"),
    *_r("backup", "list_backup_vaults", "BackupVaultList[].BackupVaultName", "BackupVaultName",
        "get_backup_vault_access_policy", "get_backup_vault_notifications"),
    # CloudTrail, Config
    *_r("cloudtrail", "describe_trails", "trailList[].TrailARN", "TrailName", "get_event_selectors",
        "get_insight_selectors"),
    *_r("cloudtrail", "describe_trails", "trailList[].TrailARN", "Name", "get_trail_status"),
    # Organizations
    *_r("organizations", "list_policies", "Policies[].Id", "PolicyId", "describe_policy",
        "list_targets_for_policy"),
    *_r("organizations", "list_roots", "Roots[].Id", "ParentId", "list_organizational_units_for_parent"),
    # EFS
    *_r("efs", "describe_file_systems", "FileSystems[].FileSystemId", "FileSystemId",
        "describe_mount_targets", "describe_file_system_policy", "describe_lifecycle_configuration",
        "describe_backup_policy"),
    *_r("efs", "describe_mount_targets", "MountTargets[].MountTargetId", "MountTargetId",
        "describe_mount_target_security_groups"),
    # Altri
    *_r("elasticbeanstalk", "describe_environments", "Environments[]",
        lambda i, k, r: {"ApplicationName": i["ApplicationName"], "EnvironmentName": i["EnvironmentName"]},
        "describe_configuration_settings"),
    *_r("amplify", "list_apps", "apps[].appId", "appId", "list_branches", "list_domain_associations",
        "list_webhooks"),
    *_r("apprunner", "list_services", "ServiceSummaryList[].ServiceArn", "ServiceArn", "describe_service"),
    *_r("codepipeline", "list_pipelines", "pipelines[].name", "name", "get_pipeline"),
    *_r("codebuild", "list_projects", "projects[]", lambda ids, k, r: {"names": ids}, "batch_get_projects",
        chunk=100),
    *_r("servicediscovery", "list_services", "Services[].Id", "Id", "get_service"),
    *_r("servicediscovery", "list_namespaces", "Namespaces[].Id", "Id", "get_namespace"),
    *_r("eks", "list_clusters", "clusters[]", "name", "describe_cluster"),
    *_r("eks", "list_clusters", "clusters[]", "clusterName", "list_nodegroups", "list_addons",
        "list_fargate_profiles", "list_access_entries"),
    *_r("mq", "list_brokers", "BrokerSummaries[].BrokerId", "BrokerId", "describe_broker"),
    *_r("opensearch", "list_domain_names", "DomainNames[].DomainName", "DomainName", "describe_domain",
        "describe_domain_config"),
    *_r("transfer", "list_servers", "Servers[].ServerId", "ServerId", "describe_server", "list_users"),
    *_r("glue", "get_databases", "DatabaseList[].Name", "DatabaseName", "get_tables"),
    *_r("kinesis", "list_streams", "StreamNames[]", "StreamName", "describe_stream_summary"),
    *_r("firehose", "list_delivery_streams", "DeliveryStreamNames[]", "DeliveryStreamName",
        "describe_delivery_stream"),
    *_r("appconfig", "list_applications", "Items[].Id", "ApplicationId", "list_environments",
        "list_configuration_profiles"),
    *_r("sso-admin", "list_instances", "Instances[].InstanceArn", "InstanceArn", "list_permission_sets"),
    *_r("sso-admin", "list_permission_sets", "PermissionSets[]",
        lambda i, k, r: {"InstanceArn": k, "PermissionSetArn": i}, "describe_permission_set",
        "list_managed_policies_in_permission_set", "get_inline_policy_for_permission_set",
        "list_accounts_for_provisioned_permission_set", "list_customer_managed_policy_references_in_permission_set"),
    *_r("network-firewall", "list_firewalls", "Firewalls[].FirewallArn", "FirewallArn", "describe_firewall"),
    *_r("network-firewall", "list_firewall_policies", "FirewallPolicies[].Arn", "FirewallPolicyArn",
        "describe_firewall_policy"),
    *_r("network-firewall", "list_rule_groups", "RuleGroups[].Arn", "RuleGroupArn", "describe_rule_group"),
    *_r("route53resolver", "list_resolver_endpoints", "ResolverEndpoints[].Id", "ResolverEndpointId",
        "list_resolver_endpoint_ip_addresses"),
    *_r("accessanalyzer", "list_analyzers", "analyzers[].name", "analyzerName", "list_archive_rules"),
]
