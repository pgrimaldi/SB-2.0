using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Configurazione del canale di prenotazione online dei servizi (bar/normali/a ore) per uno stabilimento (1:1).</summary>
public sealed class ServiceBookingSettings : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PropertyId { get; private set; }
	public bool IsEnabled { get; private set; }
	public SurchargeType SurchargeType { get; private set; }
	public decimal SurchargeValue { get; private set; }
	public bool RequiresManualConfirmation { get; private set; }
	public string? SectionTitle { get; private set; }
	public string? UsefulInfo { get; private set; }
	public List<OnlinePaymentMethod> EnabledPaymentMethods { get; private set; } = new();
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ServiceBookingSettings() { }
}
