using Shared.Contracts.Events;

namespace Shared.Contracts.Booking;

/// <summary>
/// Un giorno ceduto con lo sharing è stato ripreso: il titolare matura il voucher e/o la ricarica della tessera
/// prepagata impostati dallo stabilimento (i crediti manuali restano nel registro dei crediti di Booking). Da gestire in
/// Catalog (voucher) e Payment (tessera) quando quelle funzioni ci saranno.
/// </summary>
/// <param name="VoucherAmount">Valore del voucher; null se lo stabilimento non lo dà.</param>
/// <param name="CardTopUpAmount">Ricarica della tessera; null se lo stabilimento non la dà.</param>
public sealed record ResaleCreditEarned(
	Guid EventId, DateTime OccurredAt, Guid PropertyId, Guid CustomerId, Guid HolderReservationId, Guid OfferId, DateOnly OfferDate,
	decimal? VoucherAmount, decimal? CardTopUpAmount) : IIntegrationEvent;

/// <summary>Chi aveva ripreso il giorno lo ha tolto: voucher e ricarica di quel giorno vanno annullati.</summary>
public sealed record ResaleCreditReversed(
	Guid EventId, DateTime OccurredAt, Guid PropertyId, Guid CustomerId, Guid HolderReservationId, Guid OfferId, DateOnly OfferDate,
	decimal? VoucherAmount, decimal? CardTopUpAmount) : IIntegrationEvent;
