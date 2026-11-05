using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Componente di un listino/voucher di una convenzione: articolo di magazzino, servizio incluso o pacchetto collegato.
/// Entità figlia di <see cref="AgreementPriceList"/>.
/// </summary>
public sealed class AgreementPriceListItem : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public AgreementPriceList? PriceList { get; private set; }
	public PackageItemKind ItemKind { get; private set; }
	public long? InventoryItemId { get; private set; }
	public InventoryItem? InventoryItemRef { get; private set; }
	public long? IncludedSaleItemId { get; private set; }
	public long? LinkedPriceListId { get; private set; }
	public AgreementPriceList? LinkedPriceList { get; private set; }
	public short Quantity { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private AgreementPriceListItem() { }

	internal static AgreementPriceListItem Create(PriceListItemData d) => new()
	{
		ItemKind = d.Kind,
		InventoryItemId = d.InventoryItemId,
		IncludedSaleItemId = d.IncludedSaleItemId,
		LinkedPriceListId = d.LinkedPriceListId,
		Quantity = d.Quantity,
	};
}
