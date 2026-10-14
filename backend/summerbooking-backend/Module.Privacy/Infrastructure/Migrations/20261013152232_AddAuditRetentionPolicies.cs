using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Privacy.Infrastructure.Migrations
{
    /// <summary>
    /// Il registro di audit tiene tre categorie con durate diverse nella stessa tabella: tre policy che si
    /// distinguono solo per target_filter. Per ammetterle target_filter entra nella chiave naturale delle policy.
    /// Le policy nascono in prova (is_dry_run_only, default di colonna) come tutte le altre.
    /// </summary>
    public partial class AddAuditRetentionPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DROP INDEX privacy.uq_privacy_retention_policies_target;
CREATE UNIQUE INDEX uq_privacy_retention_policies_target
  ON privacy.privacy_retention_policies
     (schema_name, table_name, field_names, action, reference_event, storage_prefix, target_filter)
  NULLS NOT DISTINCT
  WHERE deleted_at IS NULL;
""");

            migrationBuilder.Sql("""
INSERT INTO privacy.privacy_retention_policies
  (public_id, schema_name, table_name, field_names, reference_date_column,
   duration_value, duration_unit, action, only_if_unreferenced, reference_event,
   storage_prefix, storage_date_source, target_filter, linked_asset, is_active,
   basis_source, legal_basis)
VALUES
  ('01a135db-afc4-710a-b216-3a743a889265', 'audit', 'trail_entries', '{}', 'occurred_at', 6, 'months', 'delete_row', false, NULL, NULL, NULL, '{"column": "category", "op": "=", "value": "security"}', NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: eventi di autenticazione e sicurezza degli account (accessi, blocchi, reset, 2FA, gestione utenti), con IP e user-agent. Sei mesi bastano a ricostruire un abuso o un attacco; termine validato dal titolare.'),
  ('01a135db-afc4-73ef-a7b2-c3b30c5a8bf7', 'audit', 'trail_entries', '{}', 'occurred_at', 12, 'months', 'delete_row', false, NULL, NULL, NULL, '{"column": "category", "op": "=", "value": "platform"}', NULL, true, 'law', 'Provv. Garante 27/11/2008 (amministratori di sistema): registro degli accessi e delle azioni del systemadmin, impersonazione compresa, conservato almeno sei mesi. Dodici mesi per decisione del titolare.'),
  ('01a135db-afc4-7416-ae77-655a9e042c7d', 'audit', 'trail_entries', '{}', 'occurred_at', 12, 'months', 'delete_row', false, NULL, NULL, NULL, '{"column": "category", "op": "=", "value": "operator"}', NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR e art. 4 L. 300/1970: azioni degli operatori di stabilimento, dato personale dei lavoratori. Dodici mesi coprono la stagione e le contestazioni di fine stagione; termine deciso dal titolare.');
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DELETE FROM privacy.privacy_retention_policies
 WHERE public_id IN ('01a135db-afc4-710a-b216-3a743a889265', '01a135db-afc4-73ef-a7b2-c3b30c5a8bf7', '01a135db-afc4-7416-ae77-655a9e042c7d');
DROP INDEX privacy.uq_privacy_retention_policies_target;
CREATE UNIQUE INDEX uq_privacy_retention_policies_target
  ON privacy.privacy_retention_policies
     (schema_name, table_name, field_names, action, reference_event, storage_prefix)
  NULLS NOT DISTINCT
  WHERE deleted_at IS NULL;
""");
        }
    }
}
