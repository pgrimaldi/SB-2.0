using Module.Fiskaly.Domain.Enums;
using Module.Fiskaly.Domain.ValueObjects;
using Shared.Contracts;

namespace Module.Fiskaly.Domain.Entities;

/// <summary>
/// Catalogo locale al modulo fiskaly dei riferimenti a file — oggi solo i PDF degli scontrini fiscali
/// e pre-conto. Referenziata con FK reale da Receipt.PdfAssetId; mai una tabella cross-schema.
/// </summary>
public sealed class MediaAsset : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public MediaRef Ref { get; private set; } = null!;
	public FiskalyMediaKind Kind { get; private set; }
	public byte[]? Checksum { get; private set; }
	public int? ByteSize { get; private set; }
	public string? ContentType { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private MediaAsset() { }   // unico costruttore, per il materializzatore di EF Core
}
