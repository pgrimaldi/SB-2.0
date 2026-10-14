using Module.Audit.Application.Abstractions;
using Module.Audit.Application.Validators;
using Module.Audit.Domain;
using Module.Audit.Domain.Entities;
using Shared.Contracts.Errors;
using Shared.Contracts.Identity;
using Shared.Dtos;
using Shared.Localization;
using Shared.Security;
using Shared.Utils;

namespace Module.Audit.Application.Services;

/// <summary>Passi comuni agli export dello stabilimento e del systemadmin.</summary>
internal static class AuditExport
{
	/// <summary>Le voci del filtro; 422 se superano il tetto, invece di costruire in memoria un file enorme.</summary>
	public static async Task<IReadOnlyList<AuditTrailEntry>> LoadAsync(IAuditEntryRepository entries, AuditEntryFilter filter, CancellationToken ct)
	{
		var total = await entries.CountAsync(filter, ct);
		if (total > AuditQueryRules.ExportMaxRows)
			throw DomainException.Unprocessable(AuditErrors.ExportTooLarge,
				new Dictionary<string, object?> { ["max"] = AuditQueryRules.ExportMaxRows, ["total"] = total });
		return await entries.ListAllAsync(filter, ct);
	}

	/// <summary>Lingua dell'interfaccia di chi esporta, come per l'export dei contatti.</summary>
	public static async Task<AppLanguage> LanguageAsync(IUserDirectory users, ICurrentUser currentUser, CancellationToken ct)
		=> currentUser.UserId is { } id && await users.FindAsync(id, ct) is { } user ? user.Language : SupportedLanguages.Default;

	public static IReadOnlyDictionary<string, object?> Metadata(int rows, TabularFormat format, AuditEntryFilter filter) => new Dictionary<string, object?>
	{
		["rows"] = rows,
		["format"] = Codes.Of(format),
		["from"] = filter.From,
		["to"] = filter.To,
		["actionCodes"] = filter.ActionCodes,
		["actorUserId"] = filter.ActorUserId,
		["propertyId"] = filter.PropertyId,
	};
}
