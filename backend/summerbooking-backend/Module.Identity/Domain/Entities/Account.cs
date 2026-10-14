using Module.Identity.Domain.Enums;
using Shared.Contracts;
using Shared.Localization;

namespace Module.Identity.Domain.Entities;

/// <summary>
/// Account di login del sistema (titolare/superadmin, staff operativo, cliente finale consumer).
/// </summary>
public sealed class Account : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public string? Username { get; private set; }
	public string Email { get; private set; } = null!;
	public string? PasswordHash { get; private set; }
	public DateTime? PasswordChangedAt { get; private set; }
	public string? FirstName { get; private set; }
	public string? LastName { get; private set; }
	public string? PhoneCountryCode { get; private set; }
	public string? Phone { get; private set; }
	public StorageOrigin? ProfilePictureOrigin { get; private set; }
	public string? ProfilePicturePath { get; private set; }
	public LoginMethod LoginMethod { get; private set; }
	public string? GoogleId { get; private set; }
	public string? FacebookId { get; private set; }
	/// <summary>
	/// Claim <c>sub</c> dell'identita' Apple collegata: identificativo stabile e sempre presente.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Sul percorso Apple l'email puo' mancare, e non e' un caso limite.</b> "Accedi con Apple"
	/// permette all'utente di nascondere il proprio indirizzo: Apple genera un alias
	/// <c>@privaterelay.appleid.com</c> e lo comunica all'applicazione <b>solo alla prima
	/// autorizzazione</b>. Se quella prima risposta non viene persistita, agli accessi successivi non
	/// torna piu' e l'account resta senza email per sempre — non esiste modo di richiederlo.
	/// </para>
	/// <para>
	/// Conseguenze per chi implementa l'autenticazione Apple:
	/// <list type="number">
	///   <item>identificare l'utente con <see cref="AppleSubject"/>, non con l'email: il <c>sub</c> e'
	///   stabile e sempre presente, l'indirizzo no;</item>
	///   <item>se Apple non fornisce l'email, <b>chiederla all'utente</b> invece di creare una riga
	///   incompleta: la registrazione e' l'ultimo momento in cui si puo' ancora ottenere;</item>
	///   <item><b>persistere la prima risposta di Apple.</b> Vale anche per il nome, che Apple invia
	///   con la stessa regola.</item>
	/// </list>
	/// Lo schema usa l'email come chiave di accesso (indice unico funzionale su <c>lower(email)</c>):
	/// il percorso Apple va progettato sapendo che quel campo puo' non arrivare, non scoprendolo alla
	/// prima registrazione in produzione.
	/// </para>
	/// </remarks>
	public string? AppleSubject { get; private set; }
	public AppLanguage Language { get; private set; }
	public long RoleId { get; private set; }
	public Role Role { get; private set; } = null!;
	public Guid? PropertyId { get; private set; }
	public bool IsResourceScopeRestricted { get; private set; }
	public bool NewsSectionEnabled { get; private set; }
	/// <summary>
	/// Ambiti per cui l'utente vuole il segnale acustico. Array vuoto = nessun suono.
	/// </summary>
	public NotificationSoundScope[] EnabledNotificationSounds { get; private set; } = [];
	public DateTime? LastLoginAt { get; private set; }
	public long? CreatedByUserId { get; private set; }
	public Account? CreatedByUser { get; private set; }
	/// <summary>
	/// Postazioni/settori della mappa su cui l'account puo' operare quando
	/// <see cref="IsResourceScopeRestricted"/> e' attivo.
	/// </summary>
	/// <remarks>
	/// Era una tabella figlia (<c>identity.access_user_resource_scopes</c>), collassata in array il
	/// 2026-09-30. La riserva su questa scelta era
	/// la perdita della storia per singola assegnazione (<c>created_at</c>/<c>deleted_at</c> di
	/// riga): i dati migrati la sciolgono — 19 assegnazioni, **due soli** istanti di creazione e
	/// **nessuna** revoca mai registrata, cioe' due scritture in blocco. Quella storia non esisteva,
	/// e il committente ha confermato (2026-09-30) che non serve tracciare chi concede o revoca un
	/// permesso, ne' quando.
	/// </remarks>
	public string[] ResourceScopes { get; private set; } = [];
	/// <summary>Accettazione di privacy e termini alla registrazione pubblica (prova del consenso).</summary>
	public DateTime? TermsAcceptedAt { get; private set; }
	public string? TermsVersion { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	/// <summary>Placeholder non instradabile dopo l'anonimizzazione (identity_access_users.email).</summary>
	public static string AnonymizedEmail(long id) => $"deleted+{id}@invalid.local";

	public string? FullName
	{
		get
		{
			var name = $"{FirstName} {LastName}".Trim();
			return name.Length > 0 ? name : null;
		}
	}

	public bool HasPassword => PasswordHash is not null;

	public static Account RegisterConsumer(
		string email, string passwordHash, string? firstName, string? lastName,
		string? phoneCountryCode, string? phone, AppLanguage language, long consumerRoleId,
		string termsVersion, DateTime now)
	{
		var account = NewAccount(email, consumerRoleId, language);
		account.PasswordHash = passwordHash;
		account.PasswordChangedAt = now;
		account.FirstName = firstName;
		account.LastName = lastName;
		account.PhoneCountryCode = phoneCountryCode;
		account.Phone = phone;
		account.TermsAcceptedAt = now;
		account.TermsVersion = termsVersion;
		return account;
	}

	public static Account RegisterFromSocial(
		LoginMethod provider, string subject, string email, string? firstName, string? lastName,
		string? pictureUrl, AppLanguage language, long consumerRoleId, string termsVersion, DateTime now)
	{
		var account = NewAccount(email, consumerRoleId, language);
		account.FirstName = firstName;
		account.LastName = lastName;
		account.LinkProvider(provider, subject);
		account.LoginMethod = provider;
		if (!string.IsNullOrWhiteSpace(pictureUrl))
		{
			account.ProfilePictureOrigin = StorageOrigin.External;
			account.ProfilePicturePath = pictureUrl;
		}
		account.TermsAcceptedAt = now;
		account.TermsVersion = termsVersion;
		return account;
	}

	/// <summary>Staff di stabilimento o titolare: password scelta da chi lo crea, cambiabile dall'interessato.</summary>
	public static Account CreateStaff(
		string email, string? username, string passwordHash, string? firstName, string? lastName,
		string? phoneCountryCode, string? phone, long roleId, Guid propertyId, long? createdByUserId,
		AppLanguage language, DateTime now)
	{
		var account = NewAccount(email, roleId, language);
		account.Username = username;
		account.PasswordHash = passwordHash;
		account.PasswordChangedAt = now;
		account.FirstName = firstName;
		account.LastName = lastName;
		account.PhoneCountryCode = phoneCountryCode;
		account.Phone = phone;
		account.PropertyId = propertyId;
		account.CreatedByUserId = createdByUserId;
		return account;
	}

	private static Account NewAccount(string email, long roleId, AppLanguage language)
	{
		if (string.IsNullOrWhiteSpace(email))
			throw new ArgumentException("L'email e' obbligatoria.", nameof(email));
		if (roleId <= 0)
			throw new ArgumentOutOfRangeException(nameof(roleId), "Il ruolo e' obbligatorio: nessun fallback implicito.");

		return new Account
		{
			PublicId = Guid.CreateVersion7(),
			Email = email.Trim(),
			RoleId = roleId,
			Language = language,
			LoginMethod = LoginMethod.Normal,
			NewsSectionEnabled = true,
			EnabledNotificationSounds =
			[
				NotificationSoundScope.ManagementBooking, NotificationSoundScope.OnlineBooking,
				NotificationSoundScope.ManagementServiceBooking, NotificationSoundScope.OnlineServiceBooking,
			],
		};
	}

	/// <summary>Nuova password scelta da una persona: chiude le sessioni (lo fa il servizio).</summary>
	public void ChangePassword(string passwordHash, DateTime now)
	{
		PasswordHash = passwordHash;
		PasswordChangedAt = now;
	}

	/// <summary>Re-hash silenzioso con i parametri correnti: la password non cambia, le sessioni restano.</summary>
	public void UpgradePasswordHash(string passwordHash) => PasswordHash = passwordHash;

	public void RecordLogin(LoginMethod method, DateTime now)
	{
		LoginMethod = method;
		LastLoginAt = now;
	}

	public void LinkProvider(LoginMethod provider, string subject)
	{
		switch (provider)
		{
			case LoginMethod.Google: GoogleId = subject; break;
			case LoginMethod.Facebook: FacebookId = subject; break;
			case LoginMethod.Apple: AppleSubject = subject; break;
			default: throw new ArgumentOutOfRangeException(nameof(provider));
		}
	}

	public void UpdateProfile(string? firstName, string? lastName, string? phoneCountryCode, string? phone)
	{
		FirstName = firstName;
		LastName = lastName;
		PhoneCountryCode = phoneCountryCode;
		Phone = phone;
	}

	public void ChangeEmail(string email) => Email = email.Trim();

	public void ChangeUsername(string? username) => Username = username;

	public void SetLanguage(AppLanguage language) => Language = language;

	public void SetNotificationSounds(IEnumerable<NotificationSoundScope> scopes) => EnabledNotificationSounds = scopes.Distinct().ToArray();

	public void SetNewsSection(bool enabled) => NewsSectionEnabled = enabled;

	public void AssignRole(long roleId) => RoleId = roleId;

	public void SetResourceScopes(bool restricted, IEnumerable<string> scopes)
	{
		IsResourceScopeRestricted = restricted;
		ResourceScopes = scopes.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().Order(StringComparer.Ordinal).ToArray();
	}

	/// <summary>Chiusura dell'account: via i dati personali, restano id e public_id.</summary>
	public void Anonymize()
	{
		Email = AnonymizedEmail(Id);
		Username = null;
		PasswordHash = null;
		FirstName = null;
		LastName = null;
		PhoneCountryCode = null;
		Phone = null;
		ProfilePictureOrigin = null;
		ProfilePicturePath = null;
		GoogleId = null;
		FacebookId = null;
		AppleSubject = null;
		ResourceScopes = [];
	}

	private Account() { }
}
