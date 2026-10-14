using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Servizio (bar/normale/a fascia oraria) venduto da uno stabilimento a corredo della prenotazione.
/// Aggregate root del cluster "servizi venduti": possiede le collezioni figlie
/// <see cref="ServiceSaleItemWeekday"/> e <see cref="ServiceSaleItemTimeSlot"/>.
/// </summary>
public sealed class ServiceSaleItem : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public ServiceSaleKind Kind { get; private set; }
	public string Name { get; private set; } = null!;
	public decimal Price { get; private set; }
	public int? IconId { get; private set; }
	public long? IconAssetId { get; private set; }
	public bool IsPurchasable { get; private set; }
	/// <summary>
	/// Giorni della settimana in cui il servizio e' attivo.
	/// Sostituisce catalog_service_sale_item_weekdays: sette righe figlie con un flag booleano per
	/// rappresentare un insieme di al piu' sette valori, sempre letto e scritto per intero insieme
	/// al padre, senza FK in ingresso e senza dato di audit per riga.
	/// Array vuoto = nessun giorno attivo; il servizio non e' mai prenotabile.
	/// </summary>
	public Weekday[] ActiveWeekdays { get; private set; } = [];

	public TimeOnly? HourFrom { get; private set; }
	public TimeOnly? HourTo { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<ServiceSaleItemTimeSlot> TimeSlots { get; private set; } = new List<ServiceSaleItemTimeSlot>();

	private ServiceSaleItem() { }
}
