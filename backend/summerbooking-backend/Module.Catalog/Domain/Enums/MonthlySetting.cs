namespace Module.Catalog.Domain.Enums;

/// <summary>Come conteggiare la durata "mese" nel calcolo mensile di una voce di listino.</summary>
public enum MonthlySetting
{
	EffectiveMonthDuration = 0,
	ActualMonthDuration = 1,
	Fixed30Days = 2
}
