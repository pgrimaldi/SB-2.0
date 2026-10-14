using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Module.Audit.Application.Abstractions;
using Module.Audit.Domain.Entities;
using Module.Audit.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Events;
using Shared.Utils;

namespace Module.Audit.Application.Services;

/// <summary>Salva nel registro le voci pubblicate dai moduli.</summary>
public sealed class AuditEntryRecordedHandler : IIntegrationEventHandler<AuditEntryRecorded>
{
	private static readonly JsonSerializerOptions MetadataJson = new(JsonSerializerDefaults.Web);

	private readonly IAuditEntryRepository _entries;
	private readonly ILogger<AuditEntryRecordedHandler> _logger;

	public AuditEntryRecordedHandler(IAuditEntryRepository entries, ILogger<AuditEntryRecordedHandler> logger)
	{
		_entries = entries;
		_logger = logger;
	}

	public async Task HandleAsync(AuditEntryRecorded @event, CancellationToken ct = default)
	{
		// Un modulo non previsto dall'enum è un errore di codice: la voce si scarta, l'errore resta nei log.
		if (!Codes.TryParse<SourceModule>(@event.SourceModule, out var sourceModule))
		{
			_logger.LogError("Voce di audit {EventId} scartata: modulo {SourceModule} sconosciuto (azione {ActionCode})",
				@event.EventId, @event.SourceModule, @event.ActionCode);
			return;
		}

		var entry = AuditTrailEntry.Record(
			@event.EventId, @event.OccurredAt, AuditEventMapping.Category(@event.Category), sourceModule, @event.ActionCode,
			AuditEventMapping.Outcome(@event.Outcome), @event.PropertyId, AuditEventMapping.ActorKind(@event.ActorKind),
			@event.ActorUserId, @event.ActorNameSnapshot, @event.ActorRoleSnapshot, @event.ImpersonatorUserId,
			@event.TargetType, @event.TargetId, @event.BookingNumberSnapshot, @event.SpotLabelSnapshot,
			JsonSerializer.Serialize(@event.Metadata, MetadataJson),
			IPAddress.TryParse(@event.ClientIp, out var ip) ? ip : null, @event.ClientUserAgent, @event.TraceId);

		await _entries.AppendAsync(entry, ct);
	}
}

/// <summary>Enum del contratto in Shared ↔ enum del modulo.</summary>
internal static class AuditEventMapping
{
	public static EntryCategory Category(AuditCategory category) => category switch
	{
		AuditCategory.Operator => EntryCategory.Operator,
		AuditCategory.Security => EntryCategory.Security,
		AuditCategory.Platform => EntryCategory.Platform,
		_ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
	};

	public static EntryOutcome Outcome(AuditOutcome outcome) => outcome switch
	{
		AuditOutcome.Succeeded => EntryOutcome.Succeeded,
		AuditOutcome.Failed => EntryOutcome.Failed,
		_ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
	};

	public static ActorKind ActorKind(AuditActorKind kind) => kind switch
	{
		AuditActorKind.User => Domain.Enums.ActorKind.User,
		AuditActorKind.System => Domain.Enums.ActorKind.System,
		AuditActorKind.ExternalClient => Domain.Enums.ActorKind.ExternalClient,
		AuditActorKind.Anonymous => Domain.Enums.ActorKind.Anonymous,
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
	};
}
