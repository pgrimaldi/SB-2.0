using Module.Switchboard.Domain.Enums;
using Shared.Contracts;

namespace Module.Switchboard.Domain.Entities;

/// <summary>
/// Catalogo locale allo schema switchboard dei file audio (prompt IVR: benvenuto, voci di menù,
/// fallback) di uno stabilimento. Tabella di supporto per modulo, stessa forma di catalog_media_assets.
/// </summary>
public sealed class SwitchboardMediaAsset : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long ConfigId { get; private set; }
	public Guid PropertyId { get; private set; }
	public MediaOrigin Origin { get; private set; }
	public string Path { get; private set; } = null!;
	public string? OriginalName { get; private set; }
	public string? Description { get; private set; }
	public string ContentType { get; private set; } = null!;
	public int? ByteSize { get; private set; }
	public int? DurationMs { get; private set; }
	public byte[]? Checksum { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public SwitchboardConfig Config { get; private set; } = null!;

	private SwitchboardMediaAsset() { }   // unico costruttore, per il materializzatore di EF Core
}
