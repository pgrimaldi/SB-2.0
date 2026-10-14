using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Catalogo unico dei template di messaggio: oggetto + corpo con segnaposto per uno scopo, un canale
/// e una lingua.
/// </summary>
public sealed class MessageTemplate : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid? PropertyId { get; private set; }
	public TemplateKind TemplateKind { get; private set; }
	public TemplateChannel Channel { get; private set; }
	public string Culture { get; private set; } = null!;
	public string Title { get; private set; } = null!;
	public string? Subject { get; private set; }
	public string Body { get; private set; } = null!;
	public List<PlaceholderKey> PlaceholderKeysUsed { get; private set; } = null!;
	public bool IsActive { get; private set; }
	public bool IsDefault { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<TemplateAttachment> Attachments { get; private set; } = null!;

	private MessageTemplate() { }   // unico costruttore, per il materializzatore di EF Core
}
