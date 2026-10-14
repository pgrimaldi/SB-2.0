using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Rubrica marketing per stabilimento: un contatto identificato dall'email, con stato di consenso,
/// fonte e token di disiscrizione.
/// </summary>
public sealed class MarketingContact : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public string Email { get; private set; } = null!;
	public string? FullName { get; private set; }
	public string? Phone { get; private set; }
	public Guid? CustomerPublicId { get; private set; }
	public MarketingConsentStatus ConsentStatus { get; private set; }
	public ConsentSource ConsentSource { get; private set; }
	public DateTime? ConsentAt { get; private set; }
	public string? ConsentNoticeVersion { get; private set; }
	public string UnsubscribeToken { get; private set; } = null!;
	public bool IsSeedFromBooking { get; private set; }
	public DateTime? LastContactedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private MarketingContact() { }   // unico costruttore, per il materializzatore di EF Core
}
