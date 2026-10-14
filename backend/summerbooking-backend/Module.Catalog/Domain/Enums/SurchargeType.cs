namespace Module.Catalog.Domain.Enums;

/// <summary>Come interpretare il sovrapprezzo online (SurchargeValue). Condiviso tra i settaggi canale postazioni e servizi.</summary>
public enum SurchargeType
{
	Disable = 0,
	Percentage = 1,
	Fixed = 2
}
