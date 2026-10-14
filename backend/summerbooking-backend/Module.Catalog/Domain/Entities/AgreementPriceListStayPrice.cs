using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Override manuale del prezzo per una durata specifica (1-90 giorni) di un periodo tariffario di un
/// listino/voucher hotel. Entità figlia di secondo livello, appesa a
/// <see cref="AgreementPriceListPeriodPrice"/> — gemella di <see cref="PriceListStayPrice"/> lato
/// pricing (la griglia legacy
/// `hotel_price_lists.detailPrices[].edit_price_data` è identica a quella di `price_lists`, mancava
/// solo la tabella target).
/// </summary>
public sealed class AgreementPriceListStayPrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PeriodPriceId { get; private set; }
	public long? SectorId { get; private set; }
	public short StayDays { get; private set; }
	public decimal Price { get; private set; }
	public bool IsManualOverride { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private AgreementPriceListStayPrice() { }
}
