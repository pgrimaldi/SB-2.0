using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Outbox + audit unico per destinatario: una riga = un messaggio verso un singolo destinatario su un
/// singolo canale (email transazionale/marketing, push, WhatsApp, ...).
/// </summary>
public sealed class Delivery : IAuditable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid? PropertyId { get; private set; }
	public NotificationChannel Channel { get; private set; }
	public DeliveryKind Kind { get; private set; }
	public NotificationSubjectType SubjectType { get; private set; }
	public Guid? SubjectPublicId { get; private set; }
	public RecipientType RecipientType { get; private set; }
	public Guid? RecipientCustomerPublicId { get; private set; }
	public Guid? RecipientUserPublicId { get; private set; }
	public string? RecipientAddressSnapshot { get; private set; }
	public string? RecipientNameSnapshot { get; private set; }
	public string? FromAddressSnapshot { get; private set; }
	public long? TemplateId { get; private set; }
	public MessageTemplate? Template { get; private set; }
	public string? SubjectSnapshot { get; private set; }
	public string? RenderedBody { get; private set; }
	public bool HasAttachments { get; private set; }
	public DeliveryStatus Status { get; private set; }
	public short AttemptCount { get; private set; }
	public short MaxAttempts { get; private set; }
	public DateTime? NextAttemptAt { get; private set; }
	public DeliveryProvider Provider { get; private set; }
	public string? ProviderMessageId { get; private set; }
	public string? IdempotencyKey { get; private set; }
	public string? FailureReason { get; private set; }
	public SkipReason? SkipReason { get; private set; }
	public DateTime QueuedAt { get; private set; }
	public DateTime? SentAt { get; private set; }
	public DateTime? DeliveredAt { get; private set; }
	public DateTime? FailedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private Delivery() { }   // unico costruttore, per il materializzatore di EF Core
}
