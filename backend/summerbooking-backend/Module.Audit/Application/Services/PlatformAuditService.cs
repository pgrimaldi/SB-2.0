using BuildingBlocks.Application.Validation;
using Module.Audit.Application.Abstractions;
using Module.Audit.Application.Dtos;
using Module.Audit.Application.Validators;
using Module.Audit.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Identity;
using Shared.Dtos;
using Shared.Security;
using Shared.Utils;

namespace Module.Audit.Application.Services;

/// <summary>Registro di tutta la piattaforma per il systemadmin, con IP, user-agent e identità di chi impersonava.</summary>
public sealed class PlatformAuditService
{
	private readonly IAuditEntryRepository _entries;
	private readonly IAuditExportWriter _export;
	private readonly IAuditTrail _audit;
	private readonly IUserDirectory _users;
	private readonly ICurrentUser _currentUser;
	private readonly PlatformAuditEntryListQueryValidator _listValidator;
	private readonly PlatformAuditEntryFilterValidator _filterValidator;

	public PlatformAuditService(
		IAuditEntryRepository entries, IAuditExportWriter export, IAuditTrail audit, IUserDirectory users, ICurrentUser currentUser,
		PlatformAuditEntryListQueryValidator listValidator, PlatformAuditEntryFilterValidator filterValidator)
	{
		_entries = entries;
		_export = export;
		_audit = audit;
		_users = users;
		_currentUser = currentUser;
		_listValidator = listValidator;
		_filterValidator = filterValidator;
	}

	public async Task<PagedResult<PlatformAuditEntryDto>> ListAsync(PlatformAuditEntryListQuery query, CancellationToken ct = default)
	{
		_listValidator.EnsureValid(query);
		var (items, total) = await _entries.ListAsync(ToFilter(query), query.Page, query.PageSize, ct);
		return new PagedResult<PlatformAuditEntryDto>(total, query.Page, query.PageSize, items.Select(PlatformAuditEntryDto.FromEntity).ToList());
	}

	public async Task<TabularFile> ExportAsync(PlatformAuditEntryListQuery query, TabularFormat format, CancellationToken ct = default)
	{
		_filterValidator.EnsureValid(query);
		var filter = ToFilter(query);
		var entries = await AuditExport.LoadAsync(_entries, filter, ct);
		var language = await AuditExport.LanguageAsync(_users, _currentUser, ct);

		var metadata = new Dictionary<string, object?>(AuditExport.Metadata(entries.Count, format, filter))
		{
			["categories"] = filter.Categories.Select(c => Codes.Of(c)).ToArray(),
		};
		await _audit.RecordAsync(new AuditEntry(
			AuditCategory.Platform, AuditActionCodes.Audit.Module, AuditActionCodes.Audit.PlatformEntriesExported,
			PropertyId: query.PropertyId, Metadata: metadata), ct);
		return _export.Write(entries, format, language, platform: true);
	}

	public IReadOnlyList<AuditActionCodeDto> ActionCodes()
		=> AuditActionCodes.All.Select(a => new AuditActionCodeDto(a.Code, Codes.Of(a.Category), a.SourceModule)).ToList();

	private static AuditEntryFilter ToFilter(PlatformAuditEntryListQuery query)
	{
		var categories = (query.Categories ?? [])
			.Select(c => Codes.TryParse<EntryCategory>(c, out var category) ? category : (EntryCategory?)null)
			.OfType<EntryCategory>()
			.Distinct()
			.ToArray();

		return new AuditEntryFilter(
			query.PropertyId, categories.Length > 0 ? categories : Enum.GetValues<EntryCategory>(), query.From?.UtcDateTime, query.To?.UtcDateTime,
			query.ActionCodes ?? [], query.ActorUserId, AuditEntryService.NullIfBlank(query.Search), query.SortDir != SortDirections.Asc);
	}
}
