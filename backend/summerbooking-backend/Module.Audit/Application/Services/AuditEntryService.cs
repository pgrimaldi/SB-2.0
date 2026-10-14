using BuildingBlocks.Application.Validation;
using Module.Audit.Application.Abstractions;
using Module.Audit.Application.Dtos;
using Module.Audit.Application.Validators;
using Module.Audit.Domain;
using Module.Audit.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Contracts.Identity;
using Shared.Dtos;
using Shared.Localization;
using Shared.Security;
using Shared.Utils;

namespace Module.Audit.Application.Services;

/// <summary>
/// Registro dello stabilimento del token (Impostazioni › Gestione utenti › «Tutti i log»). Lo stabilimento vede le
/// azioni degli operatori e gli eventi di sicurezza dei propri account; le azioni del systemadmin restano escluse.
/// </summary>
public sealed class AuditEntryService
{
	public static readonly EntryCategory[] VisibleCategories = [EntryCategory.Operator, EntryCategory.Security];

	private readonly IAuditEntryRepository _entries;
	private readonly IAuditExportWriter _export;
	private readonly IAuditTrail _audit;
	private readonly IUserDirectory _users;
	private readonly ICurrentUser _currentUser;
	private readonly AuditEntryListQueryValidator _listValidator;
	private readonly AuditEntryFilterValidator _filterValidator;

	public AuditEntryService(
		IAuditEntryRepository entries, IAuditExportWriter export, IAuditTrail audit, IUserDirectory users, ICurrentUser currentUser,
		AuditEntryListQueryValidator listValidator, AuditEntryFilterValidator filterValidator)
	{
		_entries = entries;
		_export = export;
		_audit = audit;
		_users = users;
		_currentUser = currentUser;
		_listValidator = listValidator;
		_filterValidator = filterValidator;
	}

	public async Task<PagedResult<AuditEntryDto>> ListAsync(AuditEntryListQuery query, CancellationToken ct = default)
	{
		_listValidator.EnsureValid(query);
		var (items, total) = await _entries.ListAsync(ToFilter(query), query.Page, query.PageSize, ct);
		return new PagedResult<AuditEntryDto>(total, query.Page, query.PageSize, items.Select(AuditEntryDto.FromEntity).ToList());
	}

	public async Task<TabularFile> ExportAsync(AuditEntryListQuery query, TabularFormat format, CancellationToken ct = default)
	{
		_filterValidator.EnsureValid(query);
		var filter = ToFilter(query);
		var entries = await AuditExport.LoadAsync(_entries, filter, ct);
		var language = await AuditExport.LanguageAsync(_users, _currentUser, ct);

		await _audit.RecordAsync(new AuditEntry(
			AuditCategory.Operator, AuditActionCodes.Audit.Module, AuditActionCodes.Audit.EntriesExported,
			Metadata: AuditExport.Metadata(entries.Count, format, filter)), ct);
		return _export.Write(entries, format, language, platform: false);
	}

	public IReadOnlyList<AuditActionCodeDto> ActionCodes()
		=> AuditActionCodes.All
			.Where(a => VisibleCategories.Contains(AuditEventMapping.Category(a.Category)))
			.Select(a => new AuditActionCodeDto(a.Code, Codes.Of(a.Category), a.SourceModule))
			.ToList();

	public async Task<IReadOnlyList<AuditActorDto>> ActorsAsync(string? search, CancellationToken ct = default)
	{
		var actors = await _entries.ListActorsAsync(RequirePropertyId(), VisibleCategories, ct);
		var text = search?.Trim();
		return actors
			.Where(a => string.IsNullOrEmpty(text) || (a.Name?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false))
			.OrderBy(a => a.Name ?? "￿", StringComparer.CurrentCultureIgnoreCase)
			.Select(a => new AuditActorDto(a.UserId, a.Name, a.Role))
			.ToList();
	}

	private AuditEntryFilter ToFilter(AuditEntryListQuery query) => new(
		RequirePropertyId(), VisibleCategories, query.From?.UtcDateTime, query.To?.UtcDateTime,
		query.ActionCodes ?? [], query.ActorUserId, NullIfBlank(query.Search), query.SortDir != SortDirections.Asc);

	private Guid RequirePropertyId()
		=> _currentUser.PropertyId ?? throw DomainException.Forbidden(AuditErrors.PropertyRequired);

	internal static string? NullIfBlank(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
