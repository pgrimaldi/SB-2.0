using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Elemento posizionato nel reticolo della mappa: una postazione prenotabile (ombrellone, lettino, cabina, area…)
/// referenziata da Booking come risorsa, oppure un elemento spiaggia solo grafico (passerella, doccia…).
/// </summary>
public sealed class MapSpot : IAuditable, ISoftDeletable
{
	public const int LabelMaxLength = 32;
	public const short RotationStep = 45;
	public const short MaxOffset = 2;

	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	public long? SectorId { get; private set; }
	public MapSector? Sector { get; private set; }
	public string? Label { get; private set; }
	public SpotType SpotType { get; private set; }
	public bool IsArea { get; private set; }
	public short? AreaSeatCount { get; private set; }
	public short? GridRow { get; private set; }
	public short? GridColumn { get; private set; }
	public short RowSpan { get; private set; } = 1;
	public short ColumnSpan { get; private set; } = 1;
	public short RotationDegrees { get; private set; }
	public short OffsetX { get; private set; }
	public short OffsetY { get; private set; }
	public bool IsAgreementOnly { get; private set; }
	public long? BeachElementId { get; private set; }
	public MapBeachElement? BeachElement { get; private set; }
	public bool IsBookable { get; private set; }
	public Guid? CreatedByUserPublicId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private MapSpot() { }

	/// <summary>Postazione prenotabile (articolo o area di articoli) in una cella del reticolo.</summary>
	public static MapSpot CreateArticle(
		long propertyId, short row, short column, SpotType type, string label, long? sectorId, short? areaSeatCount,
		bool isAgreementOnly, Guid? createdBy)
	{
		if (type == SpotType.BeachElement)
			throw new ArgumentException("Un articolo non può essere un elemento spiaggia.", nameof(type));
		var isArea = IsAreaType(type);
		if (isArea && areaSeatCount is null or < 1)
			throw new ArgumentException("Un'area di articoli ha un numero massimo di posti.", nameof(areaSeatCount));

		var spot = New(propertyId, row, column, type, createdBy);
		spot.IsArea = isArea;
		spot.AreaSeatCount = isArea ? areaSeatCount : null;
		spot.IsBookable = true;
		spot.Edit(label, sectorId, isAgreementOnly);
		return spot;
	}

	/// <summary>Elemento spiaggia solo grafico, anche su più celle; nessun numero né settore.</summary>
	public static MapSpot CreateBeachElement(long propertyId, short row, short column, long beachElementId, short rowSpan, short columnSpan, Guid? createdBy)
	{
		if (rowSpan < 1 || columnSpan < 1)
			throw new ArgumentException("Un elemento occupa almeno una cella.", nameof(rowSpan));
		var spot = New(propertyId, row, column, SpotType.BeachElement, createdBy);
		spot.BeachElementId = beachElementId;
		spot.RowSpan = rowSpan;
		spot.ColumnSpan = columnSpan;
		return spot;
	}

	public bool IsBeachElement => SpotType == SpotType.BeachElement;

	public void Edit(string label, long? sectorId, bool isAgreementOnly)
	{
		if (IsBeachElement)
			throw new InvalidOperationException("Un elemento spiaggia non ha numero né settore.");
		var trimmed = label.Trim();
		if (trimmed.Length is 0 or > LabelMaxLength)
			throw new ArgumentException("Numero o nome della postazione vuoto o troppo lungo.", nameof(label));
		Label = trimmed;
		SectorId = sectorId;
		IsAgreementOnly = isAgreementOnly;
	}

	public void SetAreaSeatCount(short seats)
	{
		if (!IsArea || seats < 1)
			throw new ArgumentException("Numero di posti valido solo per un'area.", nameof(seats));
		AreaSeatCount = seats;
	}

	/// <summary>Rotazione a passi di 45° e spostamento a quinti di casella, sempre dentro la casella.</summary>
	public void Place(short rotationDegrees, short offsetX, short offsetY)
	{
		if (rotationDegrees % RotationStep != 0 || rotationDegrees is < 0 or >= 360)
			throw new ArgumentException("Rotazione non valida.", nameof(rotationDegrees));
		if (Math.Abs((int)offsetX) > MaxOffset || Math.Abs((int)offsetY) > MaxOffset)
			throw new ArgumentException("Spostamento fuori dalla casella.", nameof(offsetX));
		RotationDegrees = rotationDegrees;
		OffsetX = offsetX;
		OffsetY = offsetY;
	}

	/// <summary>Nuova posizione dopo l'inserimento o l'eliminazione di una riga o colonna del reticolo.</summary>
	public void MoveTo(short row, short column)
	{
		GridRow = row;
		GridColumn = column;
	}

	/// <summary>Nuova ampiezza dopo l'inserimento o l'eliminazione di una riga o colonna che attraversa l'elemento.</summary>
	public void Resize(short rowSpan, short columnSpan)
	{
		if (rowSpan < 1 || columnSpan < 1 || (!IsBeachElement && (rowSpan > 1 || columnSpan > 1)))
			throw new ArgumentException("Ampiezza non valida.", nameof(rowSpan));
		RowSpan = rowSpan;
		ColumnSpan = columnSpan;
	}

	public bool Covers(short row, short column)
		=> GridRow <= row && row < GridRow + RowSpan && GridColumn <= column && column < GridColumn + ColumnSpan;

	public static bool IsAreaType(SpotType type)
		=> type is SpotType.UmbrellaArea or SpotType.CabinArea or SpotType.SunbedArea or SpotType.ParkingArea;

	private static MapSpot New(long propertyId, short row, short column, SpotType type, Guid? createdBy) => new()
	{
		PublicId = Guid.CreateVersion7(),
		PropertyId = propertyId,
		SpotType = type,
		GridRow = row,
		GridColumn = column,
		CreatedByUserPublicId = createdBy,
	};
}
