using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// «Prezzo stagionale ridotto»: un periodo più breve della stagione (es. luglio e agosto) prezzato come uno stagionale.
/// È una voce dello stabilimento, valida per tutti i pacchetti; ogni pacchetto ne imposta il prezzo per fila.
/// </summary>
public sealed class ReducedSeason : IAuditable, ISoftDeletable
{
	public const int NameMaxLength = 120;

	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public string Name { get; private set; } = null!;
	public DateOnly StartsOn { get; private set; }
	public DateOnly EndsOn { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ReducedSeason() { }

	public static ReducedSeason Create(long propertyId, string name, DateOnly startsOn, DateOnly endsOn)
	{
		var season = new ReducedSeason { PublicId = Guid.CreateVersion7(), PropertyId = propertyId };
		season.Update(name, startsOn, endsOn);
		return season;
	}

	public void Update(string name, DateOnly startsOn, DateOnly endsOn)
	{
		if (endsOn < startsOn)
			throw new ArgumentException("Fine prima dell'inizio.", nameof(endsOn));
		Name = name.Trim();
		StartsOn = startsOn;
		EndsOn = endsOn;
	}
}
