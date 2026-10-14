using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Catalogo locale al modulo dei riferimenti a file usati nelle comunicazioni: allegati, immagini di
/// copertina, PDF renderizzati, upload da lead.
/// </summary>
public sealed class MediaAsset : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid? PropertyId { get; private set; }
	public NotificationMediaOrigin Origin { get; private set; }
	public string Path { get; private set; } = null!;
	public NotificationMediaKind Kind { get; private set; }
	public string? Filename { get; private set; }
	public byte[]? Checksum { get; private set; }
	public int? ByteSize { get; private set; }
	public string? ContentType { get; private set; }
	public Guid? UploadedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private MediaAsset() { }   // unico costruttore, per il materializzatore di EF Core
}
