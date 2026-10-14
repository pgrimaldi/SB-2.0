using System.Net;
using Module.Identity.Domain.Enums;
using Shared.Contracts;

namespace Module.Identity.Domain.Entities;

/// <summary>
/// Sfida di autenticazione a tempo legata a un account (OTP 2FA, verifica email, reset password,
/// conferma cambio email, invito). Tabella di puro append/stato ad alta rotazione e vita brevissima:
/// nessun soft-delete, la pulizia e' per retention (hard-delete).
/// </summary>
public sealed class AuthChallenge : IAuditable
{
	public long Id { get; private set; }
	public long UserId { get; private set; }
	public Account User { get; private set; } = null!;
	public AuthChallengePurpose Purpose { get; private set; }
	public AuthChallengeChannel Channel { get; private set; }
	public string SecretHash { get; private set; } = null!;
	public string? DeliveryTarget { get; private set; }
	public short Attempts { get; private set; }
	public short MaxAttempts { get; private set; }
	public DateTime ExpiresAt { get; private set; }
	public DateTime? ConsumedAt { get; private set; }
	public DateTime? InvalidatedAt { get; private set; }
	public AuthChallengeInvalidation? InvalidatedReason { get; private set; }
	public IPAddress? RequestedIp { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }

	private AuthChallenge() { }

	public static AuthChallenge Create(
		long userId, AuthChallengePurpose purpose, AuthChallengeChannel channel, string secretHash,
		string? deliveryTarget, short maxAttempts, DateTime expiresAt, IPAddress? requestedIp)
	{
		if (maxAttempts <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxAttempts));

		return new AuthChallenge
		{
			UserId = userId,
			Purpose = purpose,
			Channel = channel,
			SecretHash = secretHash,
			DeliveryTarget = deliveryTarget,
			MaxAttempts = maxAttempts,
			ExpiresAt = expiresAt,
			RequestedIp = requestedIp,
		};
	}

	public bool IsUsable(DateTime now) => ConsumedAt is null && InvalidatedAt is null && ExpiresAt > now && Attempts < MaxAttempts;

	public void Consume(DateTime now) => ConsumedAt = now;

	/// <summary>Tentativo errato: all'ultimo disponibile la sfida si chiude.</summary>
	public void RegisterFailedAttempt(DateTime now)
	{
		Attempts++;
		if (Attempts >= MaxAttempts)
			Invalidate(AuthChallengeInvalidation.TooManyAttempts, now);
	}

	public void Invalidate(AuthChallengeInvalidation reason, DateTime now)
	{
		if (InvalidatedAt is not null || ConsumedAt is not null)
			return;
		InvalidatedAt = now;
		InvalidatedReason = reason;
	}
}
