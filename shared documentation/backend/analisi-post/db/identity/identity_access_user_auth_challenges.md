# Tabella `identity_access_user_auth_challenges`

> **Dominio**: C5 — `identity` · **sottoarea**: `access` · **schema**: `identity` · **nome flat**:
> `identity_access_user_auth_challenges` · **forma canonica**: `identity.access_user_auth_challenges`.
>
> **Nuova** — sostituisce i meccanismi legacy sparsi e insicuri: `users.two_factor_otp`/`users.expiry`
> (OTP 4 cifre `Math.random()`, non invalidato dopo l'uso, nessun rate limit), `users.loginOtp`
> (morto), e il reset password via `Cryptr("myTotalySecretKey")` (chiave statica hardcoded → "conferma"
> forgeable) su endpoint **non autenticato**. Fonti:
> [`docs/01-...backend.md` §7](../../../progettazione/01-criticita-e-indicazioni-backend.md),
> [`analisi-pre/db/users.md`](../../../analisi-pre/db/users.md) (`two_factor_otp`, `expiry`, `loginOtp`),
> [`analisi-pre/azioni/cliente-finale/01-account-e-registrazione.md`](../../../analisi-pre/azioni/cliente-finale/01-account-e-registrazione.md)
> (verifica email/OTP, recupero password),
> [`docs/pre/identificatori-e-uuid.md` §3](../../../progettazione/pre/identificatori-e-uuid.md) (reset/conferma =
> **token random ≥ 128 bit, salvato hashed, con scadenza**),
> [`docs/11-modulo-privacy.md` §4](../../../progettazione/11-modulo-privacy.md) (`two_factor_otp`: TTL breve,
> cancellazione dopo il consumo, mai loggato).
>
> **Nota sulla voce "Legacy"**: `nome_tabella.campo` di origine; `— (nuovo)` = campo senza
> corrispondente legacy. Tabella in gran parte nuova — pochi campi hanno un antecedente su `users`.

---

## Descrizione

Un'unica tabella per **tutte le sfide di autenticazione a tempo** legate a un account:

- **OTP di secondo fattore** (login staff quando
  [`identity_access_property_security_settings.is_two_factor_enabled`](identity_access_property_security_settings.md));
- **OTP / token di verifica email** post-registrazione;
- **token di reset password** ("password dimenticata");
- **token di conferma cambio email** / magic link / inviti (predisposto).

Ogni riga è una sfida: un **segreto hashed**, un tipo, una scadenza, un contatore tentativi, uno stato
terminale (`consumed_at` o `invalidated_at`). Tabella di **append + update di stato**, ad alta
rotazione → **niente `deleted_at`** (§3.5); pulizia per retention (minuti/ore dopo la scadenza).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_identity_access_user_auth_challenges` | `id` | PK | — |
| `uq_..._secret_hash` | `secret_hash` | UNIQUE | verifica del token/OTP (accesso caldo) |
| `ix_..._user_purpose_active` | `(user_id, purpose) WHERE consumed_at IS NULL AND invalidated_at IS NULL` | parziale | "esiste una sfida viva di questo tipo per questo utente?" (invalidazione della precedente all'emissione di una nuova; throttling) |
| `ix_..._expires_at` | `(expires_at)` | — | batch di retention |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.
- **Note**: non esposta.

### user_id
- **Legacy**: — (nuovo; nel legacy l'OTP era una colonna sulla riga `users` stessa, non una riga a parte)
- **Descrizione**: account a cui si riferisce la sfida.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`identity_access_users.id`](identity_access_users.md) `ON DELETE CASCADE`.
- **Note**: per il reset password la sfida si emette **sempre** se l'email esiste, e la risposta
  dell'API è **identica** che l'email esista o no (no user enumeration —
  [`docs/01-...backend.md` §7](../../../progettazione/01-criticita-e-indicazioni-backend.md)).

### purpose
- **Legacy**: — (nuovo; nel legacy i vari flussi non erano tipizzati — `two_factor_otp`, `loginOtp`, reset via `Cryptr` erano meccanismi separati)
- **Descrizione**: tipo di sfida.
- **Tipo**: enum PG `identity.auth_challenge_purpose` — valori: `second_factor_login`,
  `email_verification`, `password_reset`, `email_change_confirmation`, `invitation`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: ogni valore ha regole proprie (lunghezza codice, TTL, canale).

### channel
- **Legacy**: — (nuovo; nel legacy il canale era cablato: email per l'OTP 2FA via `sendOtpOnMail`)
- **Descrizione**: canale su cui la sfida è stata recapitata.
- **Tipo**: enum PG `identity.auth_challenge_channel` — valori: `email`, `sms`.
- **Default**: `'email'`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: l'invio effettivo è compito del modulo `Notification` (C6); qui si registra solo il canale.

### secret_hash
- **Legacy**: `users.two_factor_otp` (OTP 2FA, in chiaro) · `users.loginOtp` (colonna morta) · il token di reset legacy **non era persistito** (email cifrata con `Cryptr` a chiave statica, non un token random hashed)
- **Descrizione**: hash del segreto (OTP o token).
- **Tipo**: `varchar(64)` (SHA-256 hex) o `bytea`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**:
  - **token** (reset password, magic link, inviti): **≥ 128 bit random**, base64url — mai derivato da
    id/email, mai una PK ([`docs/pre/identificatori-e-uuid.md` §3](../../../progettazione/pre/identificatori-e-uuid.md)).
  - **OTP** (2FA, verifica): codice numerico **≥ 6 cifre** da CSPRNG (`RandomNumberGenerator`), non
    `Math.random()`; sempre salvato **solo** hashed, mai in chiaro, mai loggato.
  - Confronto a tempo costante.

### delivery_target
- **Legacy**: — (nuovo)
- **Descrizione**: destinazione oscurata mostrabile all'utente (es. `m***@dominio.it`, `+39 *** ** 1234`).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: **non** l'indirizzo/numero in chiaro (minimizzazione — [`docs/11-modulo-privacy.md`](../../../progettazione/11-modulo-privacy.md)).

### attempts
- **Legacy**: — (nuovo; nel legacy nessun contatore, nessun lockout)
- **Descrizione**: numero di tentativi di verifica falliti su questa sfida.
- **Tipo**: `smallint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: al superamento di `max_attempts` la sfida è `invalidated_at` (lockout) → nuova emissione
  richiesta. Rate limiting aggiuntivo per IP/account a livello applicativo (ASP.NET Core rate limiter).

### max_attempts
- **Legacy**: — (nuovo)
- **Descrizione**: soglia di tentativi ammessi.
- **Tipo**: `smallint`.
- **Default**: `5`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: parametrico per `purpose`.

### expires_at
- **Legacy**: `users.expiry` (scadenza dell'OTP 2FA)
- **Descrizione**: scadenza della sfida.
- **Tipo**: `timestamptz`.
- **Default**: nessuno (calcolato: `created_at + TTL` per `purpose`).
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: i TTL (`Auth:OtpChallengeMinutes` ~5–10 min per OTP 2FA/verifica; `Auth:PasswordResetMinutes`
  ~30–60 min per il reset) sono **valori di configurazione / variabili d'ambiente**, non hardcoded —
  [`docs/pre/autenticazione-autorizzazione-dotnet.md` §5bis](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md).

### consumed_at
- **Legacy**: — (nuovo; il legacy **non** azzerava `two_factor_otp` dopo l'uso → riutilizzabile fino a scadenza)
- **Descrizione**: quando la sfida è stata verificata con successo (uso singolo).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: una sfida con `consumed_at IS NOT NULL` **non è riutilizzabile** (la legacy non azzerava
  `two_factor_otp` dopo l'uso → riutilizzabile fino a scadenza: bug corretto qui strutturalmente).

### invalidated_at / invalidated_reason
- **Legacy**: — (nuovo)
- **Descrizione**: quando/perché la sfida è stata invalidata senza essere consumata.
- **Tipo**: `invalidated_at` → `timestamptz`; `invalidated_reason` → enum PG
  `identity.auth_challenge_invalidation` (`superseded`, `too_many_attempts`, `manual`,
  `account_disabled`).
- **Default**: `NULL`.
- **Nullabile**: Sì (entrambi).
- **Relazioni**: —.
- **Note**: `superseded` = ne è stata emessa una nuova per lo stesso `(user_id, purpose)`.

### requested_ip
- **Legacy**: — (nuovo)
- **Descrizione**: IP che ha richiesto l'emissione (audit anti-abuso).
- **Tipo**: `inet`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: dato personale, retention brevissima (segue la sfida), hard-delete con la riga.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / ultimo cambio di stato.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: presenti per uniformità. **Nessun `deleted_at`**: append/stato, rimozione per retention
  (minuti–ore dopo `expires_at`).

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- Accesso caldo: `WHERE secret_hash = $1` (verifica) → `UNIQUE`. All'emissione:
  `WHERE user_id = $1 AND purpose = $2 AND <vive>` per invalidare la precedente → indice parziale.
- **Alta scrittura, vita brevissima**: indici minimi; **retention aggressiva** (batch ogni pochi
  minuti: `DELETE ... WHERE expires_at < now() - interval '1 hour'`).
- Nessun OTP/token in chiaro persiste: elimina il rischio di leak da questa tabella.

## Implementazione .NET

Entità figlia (di append/stato) del modulo `Module.Identity`, sottoarea `Access`, schema `identity`.
`AuthChallenge` **non** implementa `ISoftDeletable` (append/stato ad alta rotazione e vita brevissima,
§3.5 del template); le transizioni (`Verify`, `Invalidate`) sono metodi dell'entità, orchestrati dal
servizio applicativo `AuthChallengeService`.

### Enum propri

```csharp
// Domain/Enums/AuthChallengePurpose.cs
public enum AuthChallengePurpose
{
	SecondFactorLogin = 0,
	EmailVerification = 1,
	PasswordReset = 2,
	EmailChangeConfirmation = 3,
	Invitation = 4
}

// Domain/Enums/AuthChallengeChannel.cs
public enum AuthChallengeChannel { Email = 0, Sms = 1 }

// Domain/Enums/AuthChallengeInvalidation.cs
public enum AuthChallengeInvalidation
{
	Superseded = 0,
	TooManyAttempts = 1,
	Manual = 2,
	AccountDisabled = 3
}
```

```csharp
modelBuilder.HasPostgresEnum<AuthChallengePurpose>("identity", "auth_challenge_purpose");
modelBuilder.HasPostgresEnum<AuthChallengeChannel>("identity", "auth_challenge_channel");
modelBuilder.HasPostgresEnum<AuthChallengeInvalidation>("identity", "auth_challenge_invalidation");
```

```csharp
dataSourceBuilder.MapEnum<AuthChallengePurpose>("identity.auth_challenge_purpose");
dataSourceBuilder.MapEnum<AuthChallengeChannel>("identity.auth_challenge_channel");
dataSourceBuilder.MapEnum<AuthChallengeInvalidation>("identity.auth_challenge_invalidation");
```

### `AuthChallengeConfiguration`

```csharp
namespace Module.Identity.Infrastructure.Configurations;

public sealed class AuthChallengeConfiguration : IEntityTypeConfiguration<AuthChallenge>
{
	public void Configure(EntityTypeBuilder<AuthChallenge> builder)
	{
		builder.ToTable("access_user_auth_challenges", "identity", t => t.HasComment(
			"Sfide di autenticazione a tempo (OTP 2FA, verifica email, reset password, inviti)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_access_user_auth_challenges");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.IsRequired()
			.HasComment("Account a cui si riferisce la sfida.");

		builder.Property(x => x.Purpose)
			.HasColumnName("purpose")
			.IsRequired()
			.HasComment("Tipo di sfida (secondo fattore, verifica email, reset password, conferma cambio email, invito).");

		builder.Property(x => x.Channel)
			.HasColumnName("channel")
			.IsRequired()
			.HasDefaultValue(AuthChallengeChannel.Email)
			.HasComment("Canale su cui la sfida è stata recapitata.");

		builder.Property(x => x.SecretHash)
			.HasColumnName("secret_hash")
			.HasMaxLength(64)
			.IsRequired()
			.HasComment("Hash del segreto (OTP o token); mai salvato in chiaro.");

		builder.Property(x => x.DeliveryTarget)
			.HasColumnName("delivery_target")
			.HasMaxLength(255)
			.HasComment("Destinazione oscurata mostrabile all'utente (es. m***@dominio.it).");

		builder.Property(x => x.Attempts)
			.HasColumnName("attempts")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Numero di tentativi di verifica falliti su questa sfida.");

		builder.Property(x => x.MaxAttempts)
			.HasColumnName("max_attempts")
			.IsRequired()
			.HasDefaultValue((short)5)
			.HasComment("Soglia di tentativi ammessi, oltre la quale la sfida è invalidata.");

		builder.Property(x => x.ExpiresAt)
			.HasColumnName("expires_at")
			.IsRequired()
			.HasComment("Scadenza della sfida (created_at + TTL da configurazione per purpose).");

		builder.Property(x => x.ConsumedAt)
			.HasColumnName("consumed_at")
			.HasComment("Quando la sfida è stata verificata con successo (uso singolo, non riutilizzabile).");

		builder.Property(x => x.InvalidatedAt)
			.HasColumnName("invalidated_at")
			.HasComment("Quando la sfida è stata invalidata senza essere consumata.");

		builder.Property(x => x.InvalidatedReason)
			.HasColumnName("invalidated_reason")
			.HasComment("Perché la sfida è stata invalidata.");

		builder.Property(x => x.RequestedIp)
			.HasColumnName("requested_ip")
			.HasColumnType("inet")
			.HasComment("IP che ha richiesto l'emissione (audit anti-abuso); dato personale, retention brevissima.");

		// created_at / updated_at: converter UTC e commenti da convenzione comune (§2, §6). Nessun deleted_at.

		builder.HasIndex(x => x.SecretHash)
			.IsUnique()
			.HasDatabaseName("uq_identity_access_user_auth_challenges_secret_hash");

		builder.HasIndex(x => new { x.UserId, x.Purpose })
			.HasFilter("consumed_at IS NULL AND invalidated_at IS NULL")
			.HasDatabaseName("ix_identity_access_user_auth_challenges_user_purpose_active");

		builder.HasIndex(x => x.ExpiresAt)
			.HasDatabaseName("ix_identity_access_user_auth_challenges_expires_at");

		builder.HasOne(x => x.User)
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.HasConstraintName("fk_identity_access_user_auth_challenges_user")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

### Servizio applicativo

- `AuthChallengeService` (issue/verify/invalidate): OTP numerico ≥ 6 cifre o token ≥ 128 bit random da
  `RandomNumberGenerator` (mai `Math.random()`), salvato **solo** hashed; confronto a tempo costante.
- Rate limiting per IP e per account (ASP.NET Core `RateLimiter`) su emissione e verifica.
- Risposte API **costanti** (no user enumeration) per `PasswordReset`/`EmailVerification`: la sfida si
  emette sempre se l'email esiste, la risposta HTTP è identica in entrambi i casi.
- Integrazione con `Module.Notification` (C6) per il recapito effettivo; qui si registra solo `Channel`
  + `DeliveryTarget`.

### Note di migrazione

- **Non** si migrano dati: i campi legacy sono morti (`loginOtp`) o transienti e insicuri
  (`two_factor_otp`/`expiry`, reset via `Cryptr` a chiave statica non persistito) — si crea solo lo
  schema.
- Job di retention ad alta frequenza (`DELETE ... WHERE expires_at < now() - interval '1 hour'`).
