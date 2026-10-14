using System.Net;
using Module.Identity.Domain.Enums;
using Shared.Contracts;
using Shared.Contracts.Errors;

namespace Module.Identity.Domain.Entities;

/// <summary>
/// Evento (accettazione o revoca) nel registro append-only delle accettazioni dei documenti
/// contrattuali da parte di uno stabilimento. Riga immutabile dopo l'inserimento.
/// </summary>
public sealed class ContractAcceptanceEvent : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public long ContractDocumentId { get; private set; }
	public ContractDocument ContractDocument { get; private set; } = null!;
	public ContractAcceptanceEventType EventType { get; private set; }
	public DateTime OccurredAt { get; private set; }
	public long ActorUserId { get; private set; }
	public Account ActorUser { get; private set; } = null!;
	public string DocumentVersionCode { get; private set; } = null!;
	public string? DocumentSha256 { get; private set; }
	public IPAddress? IpAddress { get; private set; }
	public string? UserAgent { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ContractAcceptanceEvent() { }

	/// <summary>Versione e hash sono fotografati al momento: provano cosa e' stato accettato.</summary>
	public static ContractAcceptanceEvent Record(
		Guid propertyId, ContractDocument document, ContractAcceptanceEventType type, long actorUserId,
		DateTime now, IPAddress? ip, string? userAgent)
	{
		if (document.Status != ContractDocumentStatus.Published)
			throw DomainException.Unprocessable(IdentityErrors.ContractNotPublished);

		return new ContractAcceptanceEvent
		{
			PropertyId = propertyId,
			ContractDocumentId = document.Id,
			ContractDocument = document,
			EventType = type,
			OccurredAt = now,
			ActorUserId = actorUserId,
			DocumentVersionCode = document.VersionCode,
			DocumentSha256 = document.ContentSha256,
			IpAddress = ip,
			UserAgent = userAgent is { Length: > 400 } ? userAgent[..400] : userAgent,
		};
	}
}
