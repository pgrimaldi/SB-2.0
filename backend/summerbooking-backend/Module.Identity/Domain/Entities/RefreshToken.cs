using System.Net;
using Module.Identity.Domain.Enums;
using Shared.Contracts;

namespace Module.Identity.Domain.Entities;

/// <summary>
/// Refresh token emesso a un account utente: rotazione, revoca, sessioni attive. Tabella di puro
/// append/stato ad alta rotazione: nessun soft-delete, la pulizia e' per retention (hard-delete).
/// </summary>
public sealed class RefreshToken : IAuditable
{
	public long Id { get; private set; }
	public long UserId { get; private set; }
	public Account User { get; private set; } = null!;
	public string TokenHash { get; private set; } = null!;
	public Guid FamilyId { get; private set; }
	public long? ReplacedById { get; private set; }
	public RefreshToken? ReplacedBy { get; private set; }
	public DateTime IssuedAt { get; private set; }
	public DateTime ExpiresAt { get; private set; }
	/// <summary>"Ricordami": cookie persistente invece che di sessione; ereditato a ogni rotazione.</summary>
	public bool IsPersistent { get; private set; }
	public DateTime? ConsumedAt { get; private set; }
	public DateTime? RevokedAt { get; private set; }
	public RefreshRevokeReason? RevokedReason { get; private set; }
	public string? ClientUserAgent { get; private set; }
	public IPAddress? ClientIp { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }

	private RefreshToken() { }

	/// <param name="user">Navigazione e non id: l'account può essere appena creato e non ancora salvato.</param>
	/// <param name="familyId">Catena di rotazione: nuova al login, ereditata al refresh.</param>
	public static RefreshToken Issue(
		Account user, string tokenHash, Guid familyId, bool isPersistent,
		DateTime issuedAt, DateTime expiresAt, string? userAgent, IPAddress? ip)
	{
		if (expiresAt <= issuedAt)
			throw new ArgumentOutOfRangeException(nameof(expiresAt));

		return new RefreshToken
		{
			User = user,
			UserId = user.Id,
			TokenHash = tokenHash,
			FamilyId = familyId,
			IsPersistent = isPersistent,
			IssuedAt = issuedAt,
			ExpiresAt = expiresAt,
			ClientUserAgent = userAgent is { Length: > 400 } ? userAgent[..400] : userAgent,
			ClientIp = ip,
		};
	}

	public bool IsActive(DateTime now) => ConsumedAt is null && RevokedAt is null && ExpiresAt > now;

	/// <summary>Un token gia' consumato che torna indietro e' il segnale di un furto.</summary>
	public bool IsReplay => ConsumedAt is not null;

	public void RotateTo(RefreshToken next, DateTime now)
	{
		ConsumedAt = now;
		ReplacedBy = next;
		RevokedAt = now;
		RevokedReason = RefreshRevokeReason.Superseded;
	}

	public void Revoke(RefreshRevokeReason reason, DateTime now)
	{
		if (RevokedAt is not null)
			return;
		RevokedAt = now;
		RevokedReason = reason;
	}
}
