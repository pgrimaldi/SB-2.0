using Shared.Contracts;

namespace Module.Identity.Domain.Entities;

/// <summary>
/// "Mi piace" di un utente consumer verso una struttura (bathhouse, vetrina pubblica di catalog).
/// Il "non mi piace" e' un soft-delete; un nuovo "mi piace" crea una riga nuova.
/// </summary>
public sealed class BathhouseLike : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long UserId { get; private set; }
	public Account User { get; private set; } = null!;
	public Guid BathhouseId { get; private set; }
	public DateTime LikedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private BathhouseLike() { }

	public static BathhouseLike Create(long userId, Guid bathhouseId, DateTime now)
	{
		if (bathhouseId == Guid.Empty)
			throw new ArgumentException("Struttura obbligatoria.", nameof(bathhouseId));
		return new BathhouseLike { UserId = userId, BathhouseId = bathhouseId, LikedAt = now };
	}
}
