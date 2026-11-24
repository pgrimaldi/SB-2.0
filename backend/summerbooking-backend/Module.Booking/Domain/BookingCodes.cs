namespace Module.Booking.Domain;

/// <summary>Avvisi di Booking nella risposta del salvataggio (in aggiunta a quelli del preventivo di Catalog).</summary>
public static class BookingWarnings
{
	/// <summary>Un articolo di magazzino è sotto la scorta minima.</summary>
	public const string InventoryLow = "inventory_low";

	/// <summary>Prenotazione continua su più postazioni prezzata come un periodo unico.</summary>
	public const string ContinuousPrice = "continuous_price";

	/// <summary>Blocchi separati della stessa postazione prezzati sui giorni totali (pacchetto cumulativo).</summary>
	public const string CumulativeBlocks = "cumulative_blocks";
}

/// <summary>Limite superato di un servizio, di un tavolo o del magazzino (<c>CapacityViolation.Rule</c>).</summary>
public static class CapacityRules
{
	public const string NotAvailable = "not_available";
	public const string OutsideHours = "outside_hours";
	public const string SlotNotAligned = "slot_not_aligned";
	public const string MaxPerDay = "max_per_day";
	public const string MaxPerHour = "max_per_hour";
	public const string MaxPerSlot = "max_per_slot";
	public const string MinItems = "min_items";
	public const string MaxItems = "max_items";
	public const string Interval = "interval";
	public const string Inventory = "inventory";
	public const string TableSlot = "table_slot";
	public const string TableSeats = "table_seats";
	public const string MaxBookingsPerSlot = "max_bookings_per_slot";
	public const string NoTable = "no_table";
	public const string SeatsFull = "seats_full";
}

/// <summary>Stato del pagamento di una prenotazione (filtro dell'elenco).</summary>
public static class PaymentStatuses
{
	public const string Paid = "paid";
	public const string Partial = "partial";
	public const string Unpaid = "unpaid";

	public static readonly IReadOnlySet<string> All = new HashSet<string> { Paid, Partial, Unpaid };
}

/// <summary>Giorni ceduti: sharing (rivendibili) o assenze programmate.</summary>
public static class ReleaseKinds
{
	public const string Sharing = "sharing";
	public const string Absence = "absence";

	/// <summary>Solo come filtro: entrambi.</summary>
	public const string All = "all";
}

/// <summary>Come si divide il conto.</summary>
public static class SplitModes
{
	public const string Proportional = "proportional";
	public const string Manual = "manual";
}

/// <summary>Disponibilità di una postazione in un intervallo (ricerca smart).</summary>
public static class RangeStatuses
{
	public const string Free = "free";
	public const string Partial = "partial";
	public const string Occupied = "occupied";
}

/// <summary>Stato di una postazione nell'intervallo chiesto a «Prenota».</summary>
public static class AvailabilityStatuses
{
	/// <summary>Nessuno la occupa.</summary>
	public const string Free = "free";

	/// <summary>Occupata solo da riserve: si può prenotare lo stesso, le riserve si tolgono.</summary>
	public const string Held = "held";

	/// <summary>Occupata da prenotazioni (o area piena) in almeno un giorno.</summary>
	public const string Occupied = "occupied";
}
