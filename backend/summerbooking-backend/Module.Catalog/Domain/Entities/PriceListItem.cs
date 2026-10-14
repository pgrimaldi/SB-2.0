using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Componente di una voce di listino: articolo di magazzino incluso, servizio incluso, articolo d'area o
/// pacchetto vincolato (per un additional). Entità figlia dell'aggregate <see cref="PriceList"/>.
/// </summary>
public sealed class PriceListItem : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public PackageItemKind ItemKind { get; private set; }
	public long? InventoryItemId { get; private set; }
	public int? IncludedServiceId { get; private set; }
	public long? LinkedPriceListId { get; private set; }
	public short Quantity { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private PriceListItem() { }
}
