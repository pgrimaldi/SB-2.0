using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Un evento operativo di uno stabilimento mostrato nel feed della campanella del gestionale; il testo
/// è renderizzato alla lettura da Kind + Payload, non memorizzato.
/// </summary>
public sealed class InAppMessage : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public InAppKind Kind { get; private set; }
	public InAppSubjectKind SubjectKind { get; private set; }
	public Guid? SubjectPublicId { get; private set; }
	public string? BookingNumber { get; private set; }
	public InAppVisibility Visibility { get; private set; }
	public string Payload { get; private set; } = null!;
	public string? LegacyText { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<InAppMessageRead> Reads { get; private set; } = null!;

	private InAppMessage() { }   // unico costruttore, per il materializzatore di EF Core
}
