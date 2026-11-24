using Shared.Contracts.Catalog;
using Module.Booking.Domain.Enums;

namespace Module.Booking.Domain;

/// <summary>
/// Dal tipo di postazione di Catalog al tipo di consumo di Booking. Le postazioni a uso esclusivo (ombrellone, lettino,
/// cabina, tenda, parcheggio) si occupano una sola volta per giorno e fascia; le aree hanno una capienza in posti; gli
/// articoli personalizzati e i biglietti restano fuori dal vincolo.
/// </summary>
public static class SpotKinds
{
	public static ResourceType ResourceTypeOf(string spotType) => spotType switch
	{
		"umbrella" => ResourceType.Umbrella,
		"sunbed" => ResourceType.Sunbed,
		"sunbed_xl" => ResourceType.SunbedXl,
		"cabin" => ResourceType.Cabin,
		"custom_cabin" => ResourceType.SpecialCabin,
		"tent" => ResourceType.Tent,
		"parking" => ResourceType.Parking,
		"ticket" => ResourceType.Ticket,
		_ when spotType.EndsWith("_area", StringComparison.Ordinal) => ResourceType.AreaSeat,
		_ => ResourceType.InventoryItem,
	};

	public static bool IsExclusive(ResourceType type)
		=> type is ResourceType.Umbrella or ResourceType.Sunbed or ResourceType.SunbedXl or ResourceType.Cabin or ResourceType.SpecialCabin
			or ResourceType.Tent or ResourceType.Parking;

	/// <summary>Due fasce dello stesso giorno si sovrappongono: la giornata intera vale mattina e pomeriggio.</summary>
	public static bool Overlaps(DayPeriod a, DayPeriod b) => a == DayPeriod.FullDay || b == DayPeriod.FullDay || a == b;

	public static DayPeriod ParsePeriod(string? dayPart) => dayPart switch
	{
		DayParts.Morning => DayPeriod.Morning,
		DayParts.Afternoon => DayPeriod.Afternoon,
		_ => DayPeriod.FullDay,
	};

	public static string CodeOf(DayPeriod period) => period switch
	{
		DayPeriod.Morning => DayParts.Morning,
		DayPeriod.Afternoon => DayParts.Afternoon,
		_ => DayParts.FullDay,
	};
}
