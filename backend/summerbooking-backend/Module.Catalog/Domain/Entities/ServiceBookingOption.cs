using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Opzione di prenotazione di un servizio: in un periodo (facoltativo) e in certi giorni, gli orari prenotabili dal
/// gestionale e online e la capienza (massimo all'ora, al giorno, per fascia; intervallo fra due ordini; articoli per
/// prenotazione). Un servizio può averne più d'una.
/// </summary>
public sealed class ServiceBookingOption : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long SaleItemId { get; private set; }
	public DateOnly? StartsOn { get; private set; }
	public DateOnly? EndsOn { get; private set; }
	/// <summary>Giorni in cui l'opzione vale; vuoto = tutti.</summary>
	public Weekday[] Weekdays { get; private set; } = [];
	public TimeOnly? ManagementFrom { get; private set; }
	public TimeOnly? ManagementTo { get; private set; }
	public TimeOnly? OnlineFrom { get; private set; }
	public TimeOnly? OnlineTo { get; private set; }
	public short? MaxPerHour { get; private set; }
	public short? MaxPerDay { get; private set; }
	public short? MaxPerSlot { get; private set; }
	public short? IntervalMinutes { get; private set; }
	public short? MinItems { get; private set; }
	public short? MaxItems { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }
	/// <summary>Servizi orari: disponibilità e prezzi validi nelle date dell'opzione.</summary>
	public ServiceHourlyOption? HourlyOption { get; private set; }

	private ServiceBookingOption() { }

	internal static ServiceBookingOption Create(ServiceBookingOptionData d, short sortOrder) => new()
	{
		StartsOn = d.StartsOn,
		EndsOn = d.EndsOn,
		Weekdays = d.Weekdays.Distinct().Order().ToArray(),
		ManagementFrom = d.ManagementFrom,
		ManagementTo = d.ManagementTo,
		OnlineFrom = d.OnlineFrom,
		OnlineTo = d.OnlineTo,
		MaxPerHour = d.MaxPerHour,
		MaxPerDay = d.MaxPerDay,
		MaxPerSlot = d.MaxPerSlot,
		IntervalMinutes = d.IntervalMinutes,
		MinItems = d.MinItems,
		MaxItems = d.MaxItems,
		SortOrder = sortOrder,
	};
}

public sealed record ServiceBookingOptionData(
	DateOnly? StartsOn, DateOnly? EndsOn, IReadOnlyList<Weekday> Weekdays, TimeOnly? ManagementFrom, TimeOnly? ManagementTo,
	TimeOnly? OnlineFrom, TimeOnly? OnlineTo, short? MaxPerHour, short? MaxPerDay, short? MaxPerSlot, short? IntervalMinutes,
	short? MinItems, short? MaxItems, HourlyOptionData? Hourly = null);
