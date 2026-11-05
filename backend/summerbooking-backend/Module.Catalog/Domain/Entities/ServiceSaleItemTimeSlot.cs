using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Fascia oraria interna prenotabile di un servizio a fascia oraria (<see cref="ServiceSaleItem"/>, kind =
/// hourly), con eventuale prezzo dedicato.
/// </summary>
public sealed class ServiceSaleItemTimeSlot : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long SaleItemId { get; private set; }
	/// <summary>Disponibilità a cui appartiene la durata (<see cref="ServiceHourlyOption"/>).</summary>
	public long? OptionId { get; private set; }
	/// <summary>Durata personalizzata dal gestore, oltre a quelle standard.</summary>
	public bool IsCustom { get; private set; }
	public string Label { get; private set; } = null!;
	public TimeOnly? SlotStart { get; private set; }
	public TimeOnly? SlotEnd { get; private set; }
	public short? DurationMinutes { get; private set; }
	public decimal? Price { get; private set; }
	public bool IsActive { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ServiceSaleItemTimeSlot() { }

	internal static ServiceSaleItemTimeSlot Create(HourlySlotData d, bool isCustom, short sortOrder) => new()
	{
		PublicId = Guid.CreateVersion7(),
		Label = DurationLabel(d.DurationMinutes),
		DurationMinutes = d.DurationMinutes,
		Price = d.Price,
		IsActive = d.IsEnabled,
		IsCustom = isCustom,
		SortOrder = sortOrder,
	};

	/// <summary>Etichetta della durata: «30 min», «1 h», «1 h 30 min».</summary>
	public static string DurationLabel(short? minutes) => minutes switch
	{
		null => "-",
		< 60 => $"{minutes} min",
		_ when minutes % 60 == 0 => $"{minutes / 60} h",
		_ => $"{minutes / 60} h {minutes % 60} min",
	};
}
