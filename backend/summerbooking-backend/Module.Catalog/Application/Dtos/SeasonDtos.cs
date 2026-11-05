using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Application.Dtos;

/// <summary>Stagione corrente e successiva; la successiva serve al rinnovo delle prenotazioni.</summary>
public sealed record SeasonDto(DateOnly StartsOn, DateOnly EndsOn, DateOnly? NextStartsOn, DateOnly? NextEndsOn)
{
	public static SeasonDto FromEntity(PricingSeason s) => new(s.SeasonStartsOn, s.SeasonEndsOn, s.NextSeasonStartsOn, s.NextSeasonEndsOn);
}

/// <param name="NextStartsOn">Con <paramref name="NextEndsOn"/>: entrambe o nessuna, dopo la fine della stagione corrente.</param>
public sealed record SaveSeasonRequest(DateOnly StartsOn, DateOnly EndsOn, DateOnly? NextStartsOn, DateOnly? NextEndsOn);

public sealed record DateRangeDto(DateOnly StartsOn, DateOnly EndsOn);

/// <summary>Periodo tariffario (es. «Bassa stagione»), anche su più intervalli non contigui.</summary>
/// <param name="Key">Identificativo del periodo nella stagione; i prezzi del listino vi fanno riferimento.</param>
public sealed record TariffPeriodDto(short Key, string Name, IReadOnlyList<DateRangeDto> Ranges);

/// <param name="Ranges">Intervalli dentro la stagione corrente, senza sovrapposizioni tra loro.</param>
public sealed record SaveTariffPeriodRequest(string Name, IReadOnlyList<DateRangeDto> Ranges);

public sealed record SectorDto(Guid Id, string Name, short SortOrder)
{
	public static SectorDto FromEntity(MapSector s) => new(s.PublicId, s.Name, s.SortOrder);
}

public sealed record SaveSectorRequest(string Name);
