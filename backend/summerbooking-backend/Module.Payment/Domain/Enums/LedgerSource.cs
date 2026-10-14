namespace Module.Payment.Domain.Enums;

/// <summary>Da dove nasce un movimento di incasso (<see cref="Entities.LedgerEntry"/>).</summary>
public enum LedgerSource
{
	Manual = 0,
	OnlineGateway = 1,
	PaymentLink = 2,
	PosOfsale = 3,
	PosTeamsystem = 4,
	Wallet = 5,
	Agreement = 6
}
