using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Lookup dei metodi di calcolo del prezzo multi-giorno di una voce di listino.</summary>
public sealed class CalculationMethod : IAuditable, ISoftDeletable
{
	public short Id { get; private set; }
	public string Code { get; private set; } = null!;
	public bool IsActive { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private CalculationMethod() { }
}
