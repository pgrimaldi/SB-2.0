# Tabella `identity_access_refresh_tokens`

> **Dominio**: C5 — `identity` · **sottoarea**: `access` · **schema**: `identity` · **nome flat**:
> `identity_access_refresh_tokens` · **forma canonica**: `identity.access_refresh_tokens`.
>
> **Nuova** — non ha corrispondente legacy (il legacy emette JWT validi **365 giorni senza revoca**;
> il logout fa solo `clearCookie` mentre l'app usa header `x-access-token` → un token rubato resta
> valido un anno anche dopo il cambio password). Fonti:
> [`docs/01-...backend.md` §7](../../../progettazione/01-criticita-e-indicazioni-backend.md),
> [`docs/pre/autenticazione-autorizzazione-dotnet.md`](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md)
> (access token a vita breve 15–60 min + refresh revocabile; niente refresh per M2M),
> [`analisi-pre/azioni/cliente-finale/01-account-e-registrazione.md`](../../../analisi-pre/azioni/cliente-finale/01-account-e-registrazione.md)
> (endpoint `/api/v1/refresh`, `logout`),
> [`docs/pre/identificatori-e-uuid.md` §3](../../../progettazione/pre/identificatori-e-uuid.md) (sessione/refresh =
> token opachi random, revocabili — **mai** una PK/uuid come sessione).
>
> **Nota sulla voce "Legacy"**: questa tabella non ha antecedente legaco — **tutti i campi sono
> `— (nuovo)`**. Nel legacy la "sessione" è il solo JWT a 365 giorni, non persistito.

---

## Descrizione

Registro dei **refresh token** emessi agli account **utente** (non M2M). Ogni riga rappresenta un
refresh token vivo o consumato/revocato. Consente:

- **access token a vita breve** (15–60 min) + rinnovo senza re-login;
- **revoca reale** al logout, al cambio password, alla disabilitazione account, o su sospetto furto;
- **rotazione**: a ogni uso il token viene consumato (`consumed_at`) e ne viene emesso uno nuovo
  (`replaced_by_id`); il riuso di un token già consumato è un segnale di furto → revoca dell'intera
  catena.

Tabella di **append + update di stato**, ad alta rotazione → **niente `deleted_at`/`ISoftDeletable`**
(coerente con [`docs/03-gestione-dati.md` §3.5](../../../progettazione/03-gestione-dati.md), tabelle di
puro log/append). La pulizia è per **retention** (hard-delete dei token scaduti da > N giorni).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_identity_access_refresh_tokens` | `id` | PK | — |
| `uq_..._token_hash` | `token_hash` | UNIQUE | lookup al refresh (unico accesso caldo) |
| `ix_..._user_active` | `(user_id) WHERE consumed_at IS NULL AND revoked_at IS NULL` | parziale | "sessioni attive di questo utente" (pannello sicurezza, revoca massiva a cambio password) |
| `ix_..._expires_at` | `(expires_at)` | — | batch di retention (hard-delete scaduti) |


> ### ✅ Applicato nel backend (2026-09-21)
>
> Indice **BRIN** su `(issued_at, expires_at)` ([`docs/05-ottimizzazioni-db.md` §3](../../../progettazione/05-ottimizzazioni-db.md)), migration
> `AddBrinTimeIndexes`. Serve al job di purge dei token scaduti, che scansiona per intervallo di date.
>
> BRIN e non B-tree perché la tabella è in **solo append** e l'ordine fisico dei blocchi coincide già
> con l'ordine temporale: l'indice memorizza un intervallo per blocco invece di una voce per riga, e
> costa una frazione in spazio e in manutenzione. Non sostituisce gli indici B-tree esistenti, che
> servono query diverse (tutte con un'altra colonna come prefisso).

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; auto-referenziata da `replaced_by_id`.
- **Note**: **non** esposta. L'identificatore che il client possiede è il token opaco, non questo id.

### user_id
- **Legacy**: — (nuovo)
- **Descrizione**: account proprietario della sessione.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`identity_access_users.id`](identity_access_users.md) `ON DELETE CASCADE`.
- **Note**: —.

### token_hash
- **Legacy**: — (nuovo)
- **Descrizione**: hash del refresh token (il token in chiaro esiste solo nel client).
- **Tipo**: `varchar(64)` (SHA-256 hex) o `bytea`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: il token in chiaro è **≥ 256 bit random** (`RandomNumberGenerator`), base64url, opaco — non
  un JWT, non un uuid ([`docs/pre/identificatori-e-uuid.md` §3](../../../progettazione/pre/identificatori-e-uuid.md)).
  Confronto a tempo costante. `UNIQUE` sull'hash.

### family_id
- **Legacy**: — (nuovo)
- **Descrizione**: identificativo della **catena di rotazione** (tutti i token derivati da uno stesso
  login condividono lo stesso `family_id`).
- **Tipo**: `uuid`.
- **Default**: nessuno (assegnato al primo token della catena).
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: se un token già consumato viene riusato → si revoca **tutta la famiglia** (`revoked_at` su
  ogni riga con quel `family_id`). Non è un `public_id` (non esce dal modulo).

### replaced_by_id
- **Legacy**: — (nuovo)
- **Descrizione**: token che ha sostituito questo alla rotazione.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` finché il token non viene ruotato).
- **Relazioni**: **FK** → `identity_access_refresh_tokens.id` (auto-referenza) `ON DELETE SET NULL`.
- **Note**: —.

### issued_at
- **Legacy**: — (nuovo)
- **Descrizione**: emissione del token.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: —.

### expires_at
- **Legacy**: — (nuovo)
- **Descrizione**: scadenza naturale del refresh token.
- **Tipo**: `timestamptz`.
- **Default**: nessuno (calcolato: `issued_at + TTL`).
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: il TTL (`Auth:RefreshTokenDays`, e `Auth:RefreshTokenRememberMeDays` per "ricordami") è un
  **valore di configurazione / variabile d'ambiente**, non hardcoded —
  [`docs/pre/autenticazione-autorizzazione-dotnet.md` §5bis](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md).
  Oltre questa data il token non è più rinnovabile anche se non consumato/revocato.

### is_persistent
- **Legacy**: — (nuovo)
- **Descrizione**: scelta "ricordami" fatta al login. `true` → il cookie del refresh token è
  persistente (`Max-Age`) e il TTL è `Auth:RefreshTokenRememberMeDays`; `false` → cookie di sessione
  (si chiude col browser) e TTL `Auth:RefreshTokenDays`.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: copiato su ogni token della stessa famiglia alla rotazione: senza questa colonna il
  refresh non saprebbe che tipo di cookie riemettere. Aggiunta il 2026-10-07 su richiesta del contratto
  frontend ([`docs/pre/autenticazione-autorizzazione-dotnet.md` §5ter](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md));
  **migration da creare** insieme alle API di autenticazione.

### consumed_at
- **Legacy**: — (nuovo)
- **Descrizione**: quando il token è stato usato per ottenere un nuovo access+refresh (rotazione).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: un token con `consumed_at IS NOT NULL` non è più valido; un tentativo di riuso attiva la
  revoca della famiglia.

### revoked_at
- **Legacy**: — (nuovo; nel legacy non esiste revoca — il logout non invalida nulla lato server)
- **Descrizione**: quando il token è stato revocato esplicitamente (logout, cambio password,
  disabilitazione account, revoca manuale, riuso sospetto nella famiglia).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: —.

### revoked_reason
- **Legacy**: — (nuovo)
- **Descrizione**: motivo della revoca.
- **Tipo**: enum PG `identity.refresh_revoke_reason` — valori: `logout`, `password_changed`,
  `account_disabled`, `manual`, `reuse_detected`, `superseded`.
- **Default**: nessuno.
- **Nullabile**: Sì (valorizzato con `revoked_at`).
- **Relazioni**: —.
- **Note**: `superseded` = rotazione normale; gli altri sono revoche di sicurezza.

### client_user_agent / client_ip
- **Legacy**: — (nuovo)
- **Descrizione**: user-agent e IP del client all'emissione (audit "sessioni attive", riconoscimento
  dispositivo).
- **Tipo**: `client_user_agent` → `varchar(400)`; `client_ip` → `inet`.
- **Default**: nessuno.
- **Nullabile**: Sì (entrambi).
- **Relazioni**: —.
- **Note**: `client_ip` è dato personale ([`docs/11-modulo-privacy.md`](../../../progettazione/11-modulo-privacy.md)):
  retention breve (segue la vita del token + finestra di audit), poi hard-delete con la riga.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / ultima modifica della riga (allineati a `issued_at` / ultimo cambio di
  stato).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: presenti per uniformità (richiesta trasversale). **Nessun `deleted_at`**: tabella
  append/stato, la rimozione è hard-delete di retention.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- Unico accesso caldo: `SELECT ... WHERE token_hash = $1` al refresh → indice `UNIQUE`.
- La revoca massiva a cambio password usa `ix_..._user_active` (`user_id` + parziale sugli stati vivi).
- **Alta scrittura** (una rotazione per ogni refresh): tenere il set di indici **minimo** (§3.11),
  niente indici "di comodo".
- **Retention**: batch che fa `DELETE ... WHERE expires_at < now() - interval '30 days'` (usa
  `ix_..._expires_at`); tabella potata, non lasciata crescere all'infinito.
- Valutare partizionamento per range su `issued_at` **solo** se il volume lo richiede (non al day one).
- In alternativa/aggiunta: lista di revoca in **Redis** (TTL = vita access token) per invalidare gli
  access token a vita breve senza query DB — [`docs/16-redis.md`](../../../progettazione/16-redis.md).

## Implementazione .NET

Entità figlia (di append/stato) del modulo `Module.Identity`, sottoarea `Access`, schema `identity`.
`RefreshToken` **non** implementa `ISoftDeletable` (tabella di puro append/stato ad alta rotazione, §3.5
del template) e **non** è un aggregate root con eventi propri: le transizioni (`Consume`, `Revoke`) sono
metodi dell'entità, invocati dal servizio applicativo `RefreshTokenService`.

### Enum proprio

```csharp
// Domain/Enums/RefreshRevokeReason.cs
public enum RefreshRevokeReason
{
	Logout = 0,
	PasswordChanged = 1,
	AccountDisabled = 2,
	Manual = 3,
	ReuseDetected = 4,
	Superseded = 5
}
```

```csharp
modelBuilder.HasPostgresEnum<RefreshRevokeReason>("identity", "refresh_revoke_reason");
dataSourceBuilder.MapEnum<RefreshRevokeReason>("identity.refresh_revoke_reason");
```

### `RefreshTokenConfiguration`

```csharp
namespace Module.Identity.Infrastructure.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
	public void Configure(EntityTypeBuilder<RefreshToken> builder)
	{
		builder.ToTable("access_refresh_tokens", "identity", t => t.HasComment(
			"Registro dei refresh token emessi agli account utente (rotazione, revoca, sessioni attive)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_access_refresh_tokens");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.IsRequired()
			.HasComment("Account proprietario della sessione.");

		builder.Property(x => x.TokenHash)
			.HasColumnName("token_hash")
			.HasMaxLength(64)
			.IsRequired()
			.HasComment("Hash del refresh token (il token in chiaro esiste solo nel client).");

		builder.Property(x => x.FamilyId)
			.HasColumnName("family_id")
			.IsRequired()
			.HasComment("Identificativo della catena di rotazione; il riuso di un token consumato revoca l'intera famiglia.");

		builder.Property(x => x.ReplacedById)
			.HasColumnName("replaced_by_id")
			.HasComment("Token che ha sostituito questo alla rotazione.");

		builder.Property(x => x.IssuedAt)
			.HasColumnName("issued_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Emissione del token.");

		builder.Property(x => x.ExpiresAt)
			.HasColumnName("expires_at")
			.IsRequired()
			.HasComment("Scadenza naturale del refresh token (issued_at + TTL da configurazione).");

		builder.Property(x => x.ConsumedAt)
			.HasColumnName("consumed_at")
			.HasComment("Quando il token è stato usato per ottenere un nuovo access+refresh (rotazione).");

		builder.Property(x => x.RevokedAt)
			.HasColumnName("revoked_at")
			.HasComment("Quando il token è stato revocato esplicitamente.");

		builder.Property(x => x.RevokedReason)
			.HasColumnName("revoked_reason")
			.HasComment("Motivo della revoca (logout, cambio password, disabilitazione, riuso sospetto, rotazione).");

		builder.Property(x => x.ClientUserAgent)
			.HasColumnName("client_user_agent")
			.HasMaxLength(400)
			.HasComment("User-agent del client all'emissione (audit sessioni attive).");

		builder.Property(x => x.ClientIp)
			.HasColumnName("client_ip")
			.HasColumnType("inet")
			.HasComment("IP del client all'emissione; dato personale, retention breve.");

		// created_at / updated_at: converter UTC e commenti da convenzione comune (§2, §6). Nessun deleted_at.

		builder.HasIndex(x => x.TokenHash)
			.IsUnique()
			.HasDatabaseName("uq_identity_access_refresh_tokens_token_hash");

		builder.HasIndex(x => x.UserId)
			.HasFilter("consumed_at IS NULL AND revoked_at IS NULL")
			.HasDatabaseName("ix_identity_access_refresh_tokens_user_active");

		builder.HasIndex(x => x.ExpiresAt)
			.HasDatabaseName("ix_identity_access_refresh_tokens_expires_at");

		builder.HasOne(x => x.User)
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.HasConstraintName("fk_identity_access_refresh_tokens_user")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.ReplacedBy)
			.WithMany()
			.HasForeignKey(x => x.ReplacedById)
			.HasConstraintName("fk_identity_access_refresh_tokens_replaced_by")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

### Servizio applicativo

- `RefreshTokenService` (emissione, rotazione, revoca famiglia): genera il token opaco con
  `RandomNumberGenerator` (≥ 256 bit, base64url), salva solo `TokenHash` (SHA-256), confronto a tempo
  costante alla verifica.
- Riuso di un token con `ConsumedAt IS NOT NULL` → revoca **tutta** la famiglia (`RevokedReason =
  ReuseDetected`) su tutte le righe con lo stesso `FamilyId`.
- Hook: revoca famiglie dell'utente agli eventi `PasswordChanged` (`RevokedReason.PasswordChanged`) e
  `AccountDisabled` (`RevokedReason.AccountDisabled`).
- Schema JWT `UserBearer` con access token a vita breve (15–60 min); endpoint `/refresh`, `/logout`.
- Nessun refresh token per i client M2M (`ApiBearer`) —
  [`docs/pre/autenticazione-autorizzazione-dotnet.md` §7](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md).

### Note di migrazione

- Nessun dato da migrare (tabella nuova, il legacy non persiste alcuna sessione): solo creazione schema.
- Job di retention (Hangfire/BackgroundService) per l'hard-delete dei token scaduti da oltre 30 giorni,
  usando `ix_identity_access_refresh_tokens_expires_at`.
