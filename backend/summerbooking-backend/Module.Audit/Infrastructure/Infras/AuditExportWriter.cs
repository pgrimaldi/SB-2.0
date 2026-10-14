using System.Globalization;
using System.Resources;
using BuildingBlocks.Infrastructure.Export;
using Module.Audit.Application.Abstractions;
using Module.Audit.Domain.Entities;
using Shared.Dtos;
using Shared.Localization;
using Shared.Utils;

namespace Module.Audit.Infrastructure.Infras;

/// <summary>
/// Colonne dell'export del registro, con etichette e azioni tradotte da Resources/AuditExport.*.resx nella lingua di
/// chi esporta (italiano se manca la traduzione, il codice se manca anche quella). Date in ora italiana.
/// </summary>
public sealed class AuditExportWriter : IAuditExportWriter
{
	private static readonly ResourceManager Texts =
		new("Module.Audit.Resources.AuditExport", typeof(AuditExportWriter).Assembly);

	private static readonly string[] PropertyColumns =
	[
		"Column_OccurredAt", "Column_Operator", "Column_Role", "Column_Action", "Column_Outcome",
		"Column_BookingNumber", "Column_Spot", "Column_Impersonated",
	];

	private static readonly string[] PlatformColumns =
	[
		"Column_OccurredAt", "Column_Property", "Column_Category", "Column_Operator", "Column_OperatorId", "Column_Role",
		"Column_Action", "Column_Outcome", "Column_BookingNumber", "Column_Spot", "Column_Impersonator",
		"Column_ClientIp", "Column_UserAgent", "Column_TraceId",
	];

	public TabularFile Write(IReadOnlyList<AuditTrailEntry> entries, TabularFormat format, AppLanguage language, bool platform)
	{
		var culture = SupportedLanguages.Culture(language);
		string? Find(string key) => Texts.GetString(key, culture);
		string T(string key) => Find(key) ?? key;
		string Action(string code) => Find("Action_" + code.Replace('.', '_')) ?? code;

		var headers = (platform ? PlatformColumns : PropertyColumns).Select(T).ToArray();
		var yes = T("Value_Yes");
		var no = T("Value_No");

		var rows = platform
			? entries.Select(e => PlatformRow(e, T, Action))
			: entries.Select(e => PropertyRow(e, T, Action, yes, no));

		return TabularExportWriter.Write(T(platform ? "PlatformFileName" : "FileName"), T("SheetName"), headers, rows, format);
	}

	private static string?[] PropertyRow(AuditTrailEntry e, Func<string, string> t, Func<string, string> action, string yes, string no) =>
	[
		Time(e.OccurredAt), e.ActorNameSnapshot, e.ActorRoleSnapshot, action(e.ActionCode), t("Outcome_" + Codes.Of(e.Outcome)),
		e.BookingNumberSnapshot, e.SpotLabelSnapshot, e.ImpersonatorUserId is null ? no : yes,
	];

	private static string?[] PlatformRow(AuditTrailEntry e, Func<string, string> t, Func<string, string> action) =>
	[
		Time(e.OccurredAt), e.PropertyId?.ToString(), t("Category_" + Codes.Of(e.Category)), e.ActorNameSnapshot,
		e.ActorUserId?.ToString(), e.ActorRoleSnapshot, action(e.ActionCode), t("Outcome_" + Codes.Of(e.Outcome)),
		e.BookingNumberSnapshot, e.SpotLabelSnapshot, e.ImpersonatorUserId?.ToString(),
		e.ClientIp?.ToString(), e.ClientUserAgent, e.TraceId,
	];

	private static string Time(DateTime utc) => ItalianTime.FromUtc(utc).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
}
