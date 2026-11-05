namespace Module.Catalog.Domain.Enums;

/// <summary>
/// Articolo dell'elenco fisso del magazzino (catalog.inventory_article). Gli articoli scritti con «Altro» non hanno
/// codice. Il frontend traduce il codice; quali articoli si posizionano sulla mappa lo dice <see cref="InventoryArticles"/>.
/// </summary>
public enum InventoryArticle
{
	Umbrella = 0,
	Sunbed = 1,
	SunbedXl = 2,
	SunbedMaxi = 3,
	Deckchair = 4,
	DirectorsChair = 5,
	Tent = 6,
	Gazebo = 7,
	Cabin = 8,
	CustomCabin = 9,
	Parking = 10,
	Ticket = 11,
	SunbedArea = 12,
	UmbrellaArea = 13,
	ParkingArea = 14,
	CabinArea = 15,
	CustomArticle1 = 16,
	CustomArticle2 = 17,
	CustomArticle3 = 18,
	CustomArticle4 = 19,
	CustomArticle5 = 20,
	CustomArticle6 = 21,
	CustomArticle7 = 22,
	CustomArticle8 = 23,
}

/// <summary>Proprietà fisse degli articoli dell'elenco.</summary>
public static class InventoryArticles
{
	private static readonly HashSet<InventoryArticle> StockOnly =
		[InventoryArticle.SunbedMaxi, InventoryArticle.Deckchair, InventoryArticle.DirectorsChair, InventoryArticle.Gazebo];

	/// <summary>Articolo che si può posizionare sulla mappa (gli altri esistono solo a magazzino).</summary>
	public static bool IsMapPlaceable(InventoryArticle article) => !StockOnly.Contains(article);
}
