using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Limite minimo/massimo di giorni consecutivi prenotabili online, per settore e finestra di date.</summary>
public sealed class OnlineStayLimit : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long SettingsId { get; private set; }
	public long SectorId { get; private set; }
	public DateOnly StartsOn { get; private set; }
	public DateOnly EndsOn { get; private set; }
	public short? MinDays { get; private set; }
	public short? MaxDays { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private OnlineStayLimit() { }

	public static OnlineStayLimit Create(long settingsId, long sectorId, DateOnly startsOn, DateOnly endsOn, short? minDays, short? maxDays)
	{
		var limit = new OnlineStayLimit { PublicId = Guid.CreateVersion7(), SettingsId = settingsId };
		limit.Update(sectorId, startsOn, endsOn, minDays, maxDays);
		return limit;
	}

	/// <summary>Almeno uno fra minimo e massimo; il massimo non sotto il minimo.</summary>
	public void Update(long sectorId, DateOnly startsOn, DateOnly endsOn, short? minDays, short? maxDays)
	{
		if (endsOn < startsOn)
			throw new ArgumentException("Fine prima dell'inizio.", nameof(endsOn));
		if (minDays is null && maxDays is null || minDays < 1 || maxDays < 1 || maxDays < minDays)
			throw new ArgumentException("Giorni minimi e massimi non validi.", nameof(minDays));
		SectorId = sectorId;
		StartsOn = startsOn;
		EndsOn = endsOn;
		MinDays = minDays;
		MaxDays = maxDays;
	}
}
