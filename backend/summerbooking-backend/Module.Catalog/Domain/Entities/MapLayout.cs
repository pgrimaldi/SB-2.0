using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Layout grafico della mappa spiaggia di uno stabilimento (1:1 con Property): griglia, sfondo, colore,
/// icone per tipo elemento nei due stati. Lo stato cella-per-cella vive in MapSpot.
/// </summary>
public sealed class MapLayout : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	public short GridRows { get; private set; }
	public short GridColumns { get; private set; }
	public bool IsCustomTemplate { get; private set; }
	public long? TemplateAssetId { get; private set; }
	public MediaAsset? TemplateAsset { get; private set; }
	public string? BackgroundColor { get; private set; }
	public short TiltDegrees { get; private set; }
	public bool ResetDeletesBookings { get; private set; }
	public long? UmbrellaOccupiedAssetId { get; private set; }
	public MediaAsset? UmbrellaOccupiedAsset { get; private set; }
	public long? UmbrellaFreeAssetId { get; private set; }
	public MediaAsset? UmbrellaFreeAsset { get; private set; }
	public Guid? CreatedByUserPublicId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<MapLayoutSpecialIcon> SpecialIcons { get; private set; } = new List<MapLayoutSpecialIcon>();

	public const short MaxTiltDegrees = 45;
	public const short MaxRows = 50;
	public const short MaxColumns = 80;

	private MapLayout() { }

	public static MapLayout Create(long propertyId, Guid? createdBy) => new()
	{
		PropertyId = propertyId,
		CreatedByUserPublicId = createdBy,
	};

	/// <summary>File (righe) e massimo di articoli per fila (colonne) del reticolo, inclinazione in gradi.</summary>
	public void SetGrid(short rows, short columns, short tiltDegrees)
	{
		if (rows is < 0 or > MaxRows || columns is < 0 or > MaxColumns)
			throw new ArgumentException("Dimensioni del reticolo fuori dai limiti.", nameof(rows));
		if (Math.Abs((int)tiltDegrees) > MaxTiltDegrees)
			throw new ArgumentException("Inclinazione fuori dai limiti.", nameof(tiltDegrees));
		GridRows = rows;
		GridColumns = columns;
		TiltDegrees = tiltDegrees;
	}

	/// <summary>Sfondo: immagine del catalogo o caricata dal gestore, più il colore dell'area esterna.</summary>
	public void SetBackground(long? templateAssetId, bool isCustomTemplate, string? backgroundColor)
	{
		TemplateAssetId = templateAssetId;
		IsCustomTemplate = templateAssetId is not null && isCustomTemplate;
		BackgroundColor = backgroundColor;
	}

	public void SetUmbrellaIcons(long? freeAssetId, long? occupiedAssetId)
	{
		UmbrellaFreeAssetId = freeAssetId;
		UmbrellaOccupiedAssetId = occupiedAssetId;
	}

	/// <summary>Icona di un articolo personalizzato in uno stato; null torna all'icona predefinita.</summary>
	public void SetSpecialIcon(short variant, MapIconState state, long? mediaAssetId)
	{
		if (variant is < 1 or > MapLayoutSpecialIcon.MaxVariant)
			throw new ArgumentOutOfRangeException(nameof(variant));
		var current = SpecialIcons.FirstOrDefault(i => i.Variant == variant && i.State == state);
		if (mediaAssetId is null)
		{
			if (current is not null)
				SpecialIcons.Remove(current);
		}
		else if (current is null)
			SpecialIcons.Add(MapLayoutSpecialIcon.Create(variant, state, mediaAssetId.Value));
		else
			current.SetAsset(mediaAssetId.Value);
	}

	/// <summary>Con true «Reimposta mappa» ed eliminazione di righe e colonne cancellano anche le prenotazioni.</summary>
	public void SetResetMode(bool deletesBookings) => ResetDeletesBookings = deletesBookings;

	/// <summary>Reticolo vuoto: dimensioni a zero, sfondo e icone restano.</summary>
	public void ClearGrid()
	{
		GridRows = 0;
		GridColumns = 0;
	}
}
