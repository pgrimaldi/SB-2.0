using Shared.Contracts.Audit;

namespace Module.Catalog.Application.Services;

/// <summary>Scrittura nel registro delle modifiche fatte dal gestionale (categoria operator).</summary>
public sealed class CatalogAudit
{
	private readonly IAuditTrail _audit;
	private readonly CurrentProperty _property;

	public CatalogAudit(IAuditTrail audit, CurrentProperty property)
	{
		_audit = audit;
		_property = property;
	}

	public Task RecordAsync(string actionCode, string targetType, Guid? targetId, IReadOnlyDictionary<string, object?>? metadata = null, CancellationToken ct = default)
		=> _audit.RecordAsync(new AuditEntry(
			AuditCategory.Operator, AuditActionCodes.Catalog.Module, actionCode, PropertyId: _property.PublicId,
			TargetType: targetType, TargetId: targetId, Metadata: metadata), ct);

	/// <summary>Modifica del systemadmin, valida per tutti gli stabilimenti (categoria platform).</summary>
	public Task RecordPlatformAsync(
		string actionCode, string targetType, Guid? targetId = null, Guid? propertyId = null, IReadOnlyDictionary<string, object?>? metadata = null,
		CancellationToken ct = default)
		=> _audit.RecordAsync(new AuditEntry(
			AuditCategory.Platform, AuditActionCodes.Catalog.Module, actionCode, PropertyId: propertyId, TargetType: targetType, TargetId: targetId,
			Metadata: metadata), ct);
}
