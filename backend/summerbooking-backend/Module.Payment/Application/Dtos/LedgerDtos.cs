using Module.Payment.Domain.Entities;
using Shared.Utils;

namespace Module.Payment.Application.Dtos;

/// <summary>Un movimento di una prenotazione: <c>amount</c> è positivo se entra, negativo se esce.</summary>
/// <param name="Type">deposit, payment, refund.</param>
/// <param name="Mode">cash, credit_card, bank_transfer, other.</param>
/// <param name="Source">manual, online_gateway, payment_link…</param>
public sealed record LedgerEntryDto(
	Guid Id, Guid? ReservationId, string Type, string? Mode, string Source, decimal Amount, DateTime OccurredAt, Guid? RecordedByUserId)
{
	public static LedgerEntryDto FromEntity(LedgerEntry e)
		=> new(e.PublicId, e.ReservationPublicId, Codes.Of(e.EntryType), e.PaymentMode is { } m ? Codes.Of(m) : null, Codes.Of(e.Source),
			e.SignedAmount, e.OccurredAt, e.RecordedByUserId);
}
