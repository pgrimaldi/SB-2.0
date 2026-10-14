using Module.Identity.Domain.Enums;
using Shared.Contracts;

namespace Module.Identity.Domain.Entities;

/// <summary>
/// Impostazioni di sicurezza applicate a tutto lo staff di uno stabilimento (property); una riga per property.
/// </summary>
public sealed class PropertySecuritySettings : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public bool IsTwoFactorEnabled { get; private set; }
	public SecondFactorChannel TwoFactorChannel { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private PropertySecuritySettings() { }

	public static PropertySecuritySettings Create(Guid propertyId) => new()
	{
		PropertyId = propertyId,
		TwoFactorChannel = SecondFactorChannel.Email,
	};

	/// <summary>Solo email: SMS e TOTP sono previsti dallo schema ma non implementati.</summary>
	public void SetTwoFactor(bool enabled)
	{
		IsTwoFactorEnabled = enabled;
		TwoFactorChannel = SecondFactorChannel.Email;
	}
}
