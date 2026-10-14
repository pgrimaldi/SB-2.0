using Module.Audit.Domain.Entities;
using Module.Audit.Domain.Enums;
using Shared.Dtos;
using Shared.Localization;

namespace Module.Audit.Application.Abstractions;

/// <summary>Filtro già validato e risolto: lo stabilimento viene dal token, mai dalla richiesta.</summary>
public sealed record AuditEntryFilter(
	Guid? PropertyId,
	IReadOnlyCollection<EntryCategory> Categories,
	DateTime? From,
	DateTime? To,
	IReadOnlyCollection<string> ActionCodes,
	Guid? ActorUserId,
	string? Search,
	bool Descending);

public sealed record AuditActor(Guid UserId, string? Name, string? Role);

public interface IAuditEntryRepository
{
	Task<(IReadOnlyList<AuditTrailEntry> Items, int Total)> ListAsync(AuditEntryFilter filter, int page, int pageSize, CancellationToken ct = default);

	Task<int> CountAsync(AuditEntryFilter filter, CancellationToken ct = default);

	Task<IReadOnlyList<AuditTrailEntry>> ListAllAsync(AuditEntryFilter filter, CancellationToken ct = default);

	/// <summary>Autori distinti delle voci dello stabilimento, con l'ultimo nome e ruolo registrati.</summary>
	Task<IReadOnlyList<AuditActor>> ListActorsAsync(Guid propertyId, IReadOnlyCollection<EntryCategory> categories, CancellationToken ct = default);

	/// <summary>Scrittura idempotente: una seconda consegna dello stesso evento non fa nulla.</summary>
	Task AppendAsync(AuditTrailEntry entry, CancellationToken ct = default);
}

public interface IAuditExportWriter
{
	TabularFile Write(IReadOnlyList<AuditTrailEntry> entries, TabularFormat format, AppLanguage language, bool platform);
}
