using System.Net;
using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Lead "Richiedi la tua pagina": un titolare rivendica la gestione di uno stabilimento già elencato
/// sul sito pubblico.
/// </summary>
public sealed class EstablishmentClaimRequest : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid? BathhousePublicId { get; private set; }
	public string? BathhouseNameSnapshot { get; private set; }
	public string FullName { get; private set; } = null!;
	public string Email { get; private set; } = null!;
	public string? Phone { get; private set; }
	public string? Message { get; private set; }
	public ClaimSourceChannel SourceChannel { get; private set; }
	public ClaimStatus Status { get; private set; }
	public Guid? HandledByUserId { get; private set; }
	public DateTime? HandledAt { get; private set; }
	public string? ResolutionNote { get; private set; }
	public IPAddress? ClientIpSnapshot { get; private set; }
	public string? UserAgentSnapshot { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private EstablishmentClaimRequest() { }   // unico costruttore, per il materializzatore di EF Core
}
