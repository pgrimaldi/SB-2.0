using Module.Fiskaly.Domain.Enums;
using Shared.Contracts;

namespace Module.Fiskaly.Domain.Entities;

/// <summary>
/// Registratore di cassa / System FISCAL_DEVICE registrato su Fiskaly SIGN IT, con cui si firmano i
/// documenti commerciali elettronici. Owned entity dell'aggregato FiskalyAccount.
/// </summary>
public sealed class CashRegister : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long AccountId { get; private set; }
	public Guid PropertyId { get; private set; }
	public string? FiskalySystemId { get; private set; }
	public FiskalySystemType SystemType { get; private set; }
	public string? ProducerNumber { get; private set; }
	public string? ProducerName { get; private set; }
	public string? SoftwareName { get; private set; }
	public string? SoftwareVersion { get; private set; }
	public FiskalyEntityState? SystemState { get; private set; }
	public FiskalyEntityMode? SystemMode { get; private set; }
	public bool IsDefault { get; private set; }
	public bool IsSoftware { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public Guid? UpdatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private CashRegister() { }   // unico costruttore, per il materializzatore di EF Core
}
