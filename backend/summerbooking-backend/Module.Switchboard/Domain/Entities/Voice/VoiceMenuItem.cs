using Shared.Contracts;

namespace Module.Switchboard.Domain.Entities.Voice;

/// <summary>
/// Voce di menù dell'IVR ad albero del centralino, nel contesto di un genitore (parent_id
/// auto-referenziale; NULL = primo livello). Nessun enum proprio: dtmf_key è un CHECK di formato, il
/// "tipo di nodo" resta derivato a runtime da EXISTS(figli)/audio_asset_id/EXISTS(azioni).
/// </summary>
public sealed class VoiceMenuItem : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long ConfigId { get; private set; }
	public Guid PropertyId { get; private set; }
	public long? ParentId { get; private set; }
	public short Depth { get; private set; }
	public string DtmfKey { get; private set; } = null!;
	public string Label { get; private set; } = null!;
	public long? AudioAssetId { get; private set; }
	public bool IsActive { get; private set; }
	public int SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public SwitchboardConfig Config { get; private set; } = null!;
	public VoiceMenuItem? Parent { get; private set; }
	public SwitchboardMediaAsset? AudioAsset { get; private set; }
	public ICollection<VoiceMenuItem> Children { get; private set; } = new List<VoiceMenuItem>();
	public ICollection<VoiceMenuAction> Actions { get; private set; } = new List<VoiceMenuAction>();

	private VoiceMenuItem() { }   // unico costruttore, per il materializzatore di EF Core
}
