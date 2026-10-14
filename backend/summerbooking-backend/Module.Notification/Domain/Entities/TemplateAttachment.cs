using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Giunzione ordinata: il template T allega il file F in posizione N, spedito con ogni messaggio
/// renderizzato da quel template.
/// </summary>
public sealed class TemplateAttachment : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long TemplateId { get; private set; }
	public MessageTemplate Template { get; private set; } = null!;
	public long MediaAssetId { get; private set; }
	public MediaAsset MediaAsset { get; private set; } = null!;
	public short SortOrder { get; private set; }
	public string? FilenameSnapshot { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private TemplateAttachment() { }   // unico costruttore, per il materializzatore di EF Core
}
