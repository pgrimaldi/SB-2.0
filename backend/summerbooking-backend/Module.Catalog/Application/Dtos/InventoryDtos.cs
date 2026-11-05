using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Utils;

namespace Module.Catalog.Application.Dtos;

/// <summary>Campi ammessi in <c>sortBy</c> della lista del magazzino.</summary>
public static class InventorySortFields
{
	public const string Name = "name";
	public const string TotalQuantity = "totalQuantity";
	public const string LowStockThreshold = "lowStockThreshold";

	public static readonly string[] All = [Name, TotalQuantity, LowStockThreshold];
}

/// <param name="Search">Parte del nome dell'articolo.</param>
/// <param name="SortBy">Uno di <see cref="InventorySortFields"/>; default <c>name</c>.</param>
/// <param name="SortDir"><c>asc</c> (default) o <c>desc</c>.</param>
/// <param name="PageSize">10, 20, 25, 50, 100, 200 o 300.</param>
public sealed record InventoryItemListQuery(string? Search = null, string? SortBy = null, string? SortDir = null, int Page = 1, int PageSize = 10);

/// <param name="ArticleCode">Articolo dell'elenco fisso (<c>GET /inventory-articles</c>); null per «Altro».</param>
/// <param name="Name">Nome mostrato; per un articolo dell'elenco il frontend propone l'etichetta tradotta.</param>
/// <param name="LowStockThreshold">Soglia minima, da 0 alla quantità totale; null = nessuna soglia.</param>
/// <param name="LowStockAlertEnabled">Avviso quando la disponibilità del giorno scende sotto la soglia.</param>
public sealed record SaveInventoryItemRequest(
	string? ArticleCode, string Name, int TotalQuantity, int? LowStockThreshold, bool LowStockAlertEnabled);

/// <param name="MapPlaceable">L'articolo si può posizionare sulla mappa.</param>
public sealed record InventoryItemDto(
	Guid Id, string? ArticleCode, string Name, int TotalQuantity, int? LowStockThreshold, bool LowStockAlertEnabled, bool MapPlaceable)
{
	public static InventoryItemDto FromEntity(InventoryItem item) => new(
		item.PublicId, item.ArticleCode is { } a ? Codes.Of(a) : null, item.Name, item.TotalQuantity,
		item.LowStockThreshold, item.LowStockAlertEnabled, item.ArticleCode is { } code && InventoryArticles.IsMapPlaceable(code));
}

/// <summary>Voce della tendina degli articoli.</summary>
public sealed record InventoryItemLookupDto(Guid Id, string Name, string? ArticleCode);

/// <summary>Articolo dell'elenco fisso; il frontend traduce il codice.</summary>
public sealed record InventoryArticleDto(string Code, bool MapPlaceable);
