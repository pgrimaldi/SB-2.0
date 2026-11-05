namespace Module.Catalog.Application.Dtos;

/// <summary>Configurazione completa: impostazioni del reticolo, elementi posizionati e quello che serve per disegnarli.</summary>
/// <param name="Sectors">File dello stabilimento (nome e ordine), citate da <see cref="MapSpotDto.SectorId"/>.</param>
/// <param name="BeachElements">Elementi spiaggia usati sulla mappa, con l'icona, citati da <see cref="MapSpotDto.BeachElementId"/>.</param>
public sealed record MapDto(MapSettingsDto Settings, IReadOnlyList<MapSpotDto> Spots, IReadOnlyList<SectorDto> Sectors, IReadOnlyList<MapBeachElementDto> BeachElements);

/// <param name="Rows">Numero di file.</param>
/// <param name="Columns">Numero massimo di articoli per fila.</param>
/// <param name="ResetDeletesBookings">false = «Reimposta mappa» e l'eliminazione di righe o colonne mantengono le prenotazioni.</param>
public sealed record MapSettingsDto(
	short Rows, short Columns, short TiltDegrees, string? BackgroundColor, MapBackgroundDto? Background,
	MapIconsDto Icons, bool ResetDeletesBookings);

/// <param name="IconId">Sfondo del catalogo; null se caricato dal gestore.</param>
public sealed record MapBackgroundDto(Guid? IconId, string? Url, bool IsCustom);

/// <param name="IconId">Icona del catalogo scelta; null = predefinita o caricata nel vecchio gestionale.</param>
public sealed record MapIconRefDto(Guid? IconId, string? Url, bool IsDefault);

public sealed record MapIconPairDto(MapIconRefDto Free, MapIconRefDto Occupied);

public sealed record CustomArticleIconsDto(short Variant, MapIconPairDto Icons);

public sealed record MapIconsDto(MapIconPairDto Umbrella, IReadOnlyList<CustomArticleIconsDto> CustomArticles);

/// <param name="Row">Riga dell'angolo in alto a sinistra, da 1 (lato mare).</param>
/// <param name="Column">Colonna dell'angolo in alto a sinistra, da 1 (sinistra).</param>
/// <param name="Label">Numero della postazione; null per un elemento spiaggia.</param>
/// <param name="OffsetX">Spostamento dentro la casella in quinti, da -2 a 2.</param>
public sealed record MapSpotDto(
	Guid Id, short Row, short Column, short RowSpan, short ColumnSpan, string SpotType, string? Label, Guid? SectorId,
	short? AreaSeatCount, bool IsAgreementOnly, short RotationDegrees, short OffsetX, short OffsetY, Guid? BeachElementId);

/// <param name="BackgroundIconId">Sfondo del catalogo; null mantiene lo sfondo caricato, se c'è.</param>
public sealed record SaveMapSettingsRequest(
	short Rows, short Columns, short TiltDegrees, string? BackgroundColor, Guid? BackgroundIconId, bool ResetDeletesBookings);

/// <summary>Icone di un articolo nei due stati; null = icona predefinita.</summary>
public sealed record SaveMapIconPairRequest(Guid? FreeIconId, Guid? OccupiedIconId);

public sealed record MapCellDto(short Row, short Column);

/// <summary>Ordine della numerazione automatica quando si riempiono più caselle.</summary>
/// <param name="Vertical">true = lungo le colonne (dal mare all'interno o viceversa), false = lungo le righe.</param>
public sealed record MapNumberingDto(bool Vertical, bool SeaToInland, bool LeftToRight);

/// <summary>
/// Inserimento nelle caselle selezionate. Articoli e aree: uno per casella, numerati in successione da
/// <paramref name="FirstLabel"/>. Elemento spiaggia: uno solo sull'intero rettangolo selezionato.
/// </summary>
public sealed record FillMapCellsRequest(
	IReadOnlyList<MapCellDto> Cells, string SpotType, string? FirstLabel, Guid? SectorId, short? AreaSeatCount,
	bool IsAgreementOnly, Guid? BeachElementId, MapNumberingDto? Numbering);

public sealed record UpdateMapSpotRequest(string Label, Guid? SectorId, short? AreaSeatCount, bool IsAgreementOnly);

public sealed record MapSpotPlacementDto(Guid Id, short RotationDegrees, short OffsetX, short OffsetY);

public sealed record PlaceMapSpotsRequest(IReadOnlyList<MapSpotPlacementDto> Items);

/// <param name="At">Posizione della nuova riga o colonna, da 1 a quante ce ne sono più una.</param>
public sealed record InsertMapLineRequest(short At);

public sealed record MapIconDto(Guid Id, string Category, string State, string Name, string? Url, bool IsDefault);

public sealed record MapBeachElementDto(Guid Id, string Name, string? IconUrl);
