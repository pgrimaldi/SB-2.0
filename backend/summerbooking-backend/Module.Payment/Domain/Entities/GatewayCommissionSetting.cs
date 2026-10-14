using Module.Payment.Domain.Enums;
using Shared.Contracts;

namespace Module.Payment.Domain.Entities;

/// <summary>
/// Tasso di commissione trattenuto dalla piattaforma sugli incassi online, per stabilimento e canale.
/// </summary>
public sealed class GatewayCommissionSetting : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public decimal OnlineRate { get; private set; }
	public decimal IframeRate { get; private set; }
	public decimal FixedFeeAmount { get; private set; }
	public CommissionExemption Exemption { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private GatewayCommissionSetting() { }
}
