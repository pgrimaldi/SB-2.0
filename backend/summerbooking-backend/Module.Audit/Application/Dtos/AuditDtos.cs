using System.Text.Json;
using Module.Audit.Domain.Entities;
using Shared.Utils;

namespace Module.Audit.Application.Dtos;

/// <summary>Filtri comuni alle liste e agli export del registro.</summary>
public interface IAuditEntryFilterQuery
{
	DateTimeOffset? From { get; }
	DateTimeOffset? To { get; }
	string[]? ActionCodes { get; }
	Guid? ActorUserId { get; }
	string? Search { get; }
	string? SortDir { get; }
}

/// <summary>Registro dello stabilimento del token (pagina «Tutti i log», pulsante «Log» dell'utente).</summary>
/// <param name="From">Istante iniziale incluso; con <paramref name="To"/> l'intervallo del filtro date.</param>
/// <param name="To">Istante finale escluso.</param>
/// <param name="Search">Nome dell'operatore, numero di prenotazione o postazione.</param>
/// <param name="SortDir"><c>desc</c> (default) o <c>asc</c>, sulla data.</param>
/// <param name="PageSize">10, 20, 25, 50 o 100.</param>
public sealed record AuditEntryListQuery(
	DateTimeOffset? From = null, DateTimeOffset? To = null, string[]? ActionCodes = null, Guid? ActorUserId = null,
	string? Search = null, string? SortDir = null, int Page = 1, int PageSize = 10) : IAuditEntryFilterQuery;

/// <summary>Registro di tutta la piattaforma per il systemadmin.</summary>
/// <param name="Categories"><c>operator</c>, <c>security</c>, <c>platform</c>; vuoto = tutte.</param>
public sealed record PlatformAuditEntryListQuery(
	Guid? PropertyId = null, string[]? Categories = null,
	DateTimeOffset? From = null, DateTimeOffset? To = null, string[]? ActionCodes = null, Guid? ActorUserId = null,
	string? Search = null, string? SortDir = null, int Page = 1, int PageSize = 10) : IAuditEntryFilterQuery;

/// <summary>Voce come la vede lo stabilimento: niente IP, user-agent né identità del systemadmin.</summary>
/// <param name="Impersonated">Azione compiuta dal supporto della piattaforma entrato come gestore.</param>
public sealed record AuditEntryDto(
	long Id, DateTime OccurredAt, string Category, string ActionCode, string Outcome, string ActorKind,
	Guid? ActorUserId, string? ActorName, string? ActorRole, bool Impersonated,
	string? TargetType, Guid? TargetId, string? BookingNumber, string? SpotLabel, JsonElement Metadata)
{
	public static AuditEntryDto FromEntity(AuditTrailEntry e) => new(
		e.Id, e.OccurredAt, Codes.Of(e.Category), e.ActionCode, Codes.Of(e.Outcome), Codes.Of(e.ActorKind),
		e.ActorUserId, e.ActorNameSnapshot, e.ActorRoleSnapshot, e.ImpersonatorUserId is not null,
		e.TargetType, e.TargetId, e.BookingNumberSnapshot, e.SpotLabelSnapshot, ParseMetadata(e.Metadata));

	internal static JsonElement ParseMetadata(string metadata)
	{
		using var document = JsonDocument.Parse(metadata);
		return document.RootElement.Clone();
	}
}

/// <summary>Voce completa per il systemadmin.</summary>
public sealed record PlatformAuditEntryDto(
	long Id, DateTime OccurredAt, DateTime RecordedAt, string Category, string SourceModule, string ActionCode, string Outcome,
	Guid? PropertyId, string ActorKind, Guid? ActorUserId, string? ActorName, string? ActorRole, Guid? ImpersonatorUserId,
	string? TargetType, Guid? TargetId, string? BookingNumber, string? SpotLabel, JsonElement Metadata,
	string? ClientIp, string? ClientUserAgent, string? TraceId)
{
	public static PlatformAuditEntryDto FromEntity(AuditTrailEntry e) => new(
		e.Id, e.OccurredAt, e.RecordedAt, Codes.Of(e.Category), Codes.Of(e.SourceModule), e.ActionCode, Codes.Of(e.Outcome),
		e.PropertyId, Codes.Of(e.ActorKind), e.ActorUserId, e.ActorNameSnapshot, e.ActorRoleSnapshot, e.ImpersonatorUserId,
		e.TargetType, e.TargetId, e.BookingNumberSnapshot, e.SpotLabelSnapshot, AuditEntryDto.ParseMetadata(e.Metadata),
		e.ClientIp?.ToString(), e.ClientUserAgent, e.TraceId);
}

/// <summary>Valore del filtro «Tipologia di azione»; il frontend traduce <c>code</c> e sceglie l'icona.</summary>
public sealed record AuditActionCodeDto(string Code, string Category, string SourceModule);

/// <summary>Valore del filtro «Utente»: chiunque compaia nel registro, anche se l'account non c'è più.</summary>
public sealed record AuditActorDto(Guid UserId, string? Name, string? Role);
