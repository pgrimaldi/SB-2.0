using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Nome di un paese (<see cref="Country"/>) in una culture. Una riga per (paese × lingua).
/// </summary>
public sealed class CountryTranslation : IAuditable, ISoftDeletable
{
	public short Id { get; private set; }
	public short CountryId { get; private set; }
	public Country? Country { get; private set; }
	public string Culture { get; private set; } = null!;
	public string Name { get; private set; } = null!;
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private CountryTranslation() { }
}
