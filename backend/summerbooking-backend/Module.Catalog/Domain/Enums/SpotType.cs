namespace Module.Catalog.Domain.Enums;

/// <summary>
/// Tipo di elemento posizionato in una cella della mappa spiaggia (catalog_map_spots). Gli articoli coincidono con
/// quelli dell'elenco fisso del magazzino che si posizionano sulla mappa.
/// </summary>
public enum SpotType
{
	Umbrella = 0,
	Sunbed = 1,
	SunbedXl = 2,
	Cabin = 3,
	CustomCabin = 4,
	CustomArticle1 = 5,
	CustomArticle2 = 6,
	CustomArticle3 = 7,
	CustomArticle4 = 8,
	CustomArticle5 = 9,
	Tent = 10,
	Parking = 11,
	Ticket = 12,
	UmbrellaArea = 13,
	CabinArea = 14,
	SunbedArea = 15,
	ParkingArea = 16,
	CustomArticle6 = 17,
	CustomArticle7 = 18,
	CustomArticle8 = 19,

	/// <summary>Elemento solo grafico (passerella, doccia, ecc.), non prenotabile e senza numero.</summary>
	BeachElement = 20,
}
