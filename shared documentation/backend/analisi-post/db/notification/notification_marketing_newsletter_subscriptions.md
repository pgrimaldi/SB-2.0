# Tabella `notification_marketing_newsletter_subscriptions`

> **Dominio**: C6 — `notification` · **sottoarea**: `marketing` · **schema**: `notification` · **nome
> flat**: `notification_marketing_newsletter_subscriptions` · **forma canonica**:
> `notification.marketing_newsletter_subscriptions`.
>
> Sostituisce [`subscribes`](../../../analisi-pre/db/subscribes.md) (6 548 righe in prod; `status` `1`
> 6 114 / `0` 434) — le **iscrizioni newsletter dal sito pubblico** (più le email registrate ad ogni
> signup utente e ad ogni creazione stabilimento). Fonti:
> [`analisi-pre/db/subscribes.md`](../../../analisi-pre/db/subscribes.md),
> [`docs/03-gestione-dati.md` §3.1bis](../../../progettazione/03-gestione-dati.md),
> [`docs/11-modulo-privacy.md` §"subscribes"](../../../progettazione/11-modulo-privacy.md),
> [`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md),
> [`analisi-pre/azioni/systemadmin/03-iscritti-newsletter.md`](../../../analisi-pre/azioni/systemadmin/03-iscritti-newsletter.md).

---

## Descrizione

**Una riga = un'iscrizione alla newsletter globale di piattaforma** (nessun `property_id`: è una lista
unica, non per stabilimento — [`subscribes.md` §"Nessun property_id"](../../../analisi-pre/db/subscribes.md)).
Consumatore attuale: la voce "Subscription Details" del pannello systemadmin
([`front-admin/`](../../../../../../../front-admin/)) — elenco/eliminazione/export; **nessun invio** (il cron
newsletter è spento nel legacy).

Correzioni rispetto al legacy:

- **Nessun consenso / doppio opt-in / token di disiscrizione** ([`docs/11 §"subscribes"`](../../../progettazione/11-modulo-privacy.md):
  "oggi non c'è prova che il consenso sia mai stato dato") → `status` a 3 stati con
  `confirmation_token` + `unsubscribe_token` + `confirmed_at`.
- **`status int` `1`/`0` (soft-delete mascherato)** → `status` enum + `deleted_at` separato.
- **Nessun `UNIQUE(email)`, nessuna deduplica** (`createSubscribes` chiamato ad ogni signup e creazione
  property) → `UNIQUE (lower(email))`.
- **Nessuna `source`** → `source` enum.
- **Endpoint aperti (`optionalAuth`)** → fuori scope schema; annotato che vanno sotto ruolo
  `systemadmin` ([`docs/34-multitenant.md`](../../../progettazione/34-multitenant.md)).
- **Storicamente** confluisce nel modello contatti/liste del modulo `Notification` come **lista
  "Newsletter" globale**; questa tabella ne è la materializzazione (membri = email non
  necessariamente legate a un `customers`).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `status` | **enum esclusivo**: `pending_confirmation` / `subscribed` / `unsubscribed` (ciclo di vita del double opt-in). Sostituisce `status` `1`/`0`. |
| `source` | **enum esclusivo**. |

## Snapshot

Non applicabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_marketing_newsletter_subscriptions` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | export GDPR / pannello admin |
| `uq_..._email` | `(lower(email)) WHERE deleted_at IS NULL` | UNIQUE parziale | una sola iscrizione per email |
| `uq_..._confirmation_token` | `(confirmation_token) WHERE confirmation_token IS NOT NULL` | UNIQUE parziale | conferma double opt-in |
| `uq_..._unsubscribe_token` | `(unsubscribe_token)` | UNIQUE | link di disiscrizione |
| `ix_..._status_created` | `(status, created_at DESC)` | btree | elenco pannello admin + segmenti attivi |

## Campi

### id
- **Legacy**: `subscribes.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### email
- **Legacy**: `subscribes.email`
- **Descrizione**: indirizzo iscritto, normalizzato lower-case + trim.
- **Tipo**: `varchar(320)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE` parziale.

### status
- **Legacy**: `subscribes.status` (`1` attiva / `0` cancellata)
- **Descrizione**: stato dell'iscrizione.
- **Tipo**: `enum notification.newsletter_status` — valori: `pending_confirmation` (opt-in inviato,
  non ancora confermato), `subscribed`, `unsubscribed`.
- **Default**: `pending_confirmation` (le nuove iscrizioni; lo storico migrato → `subscribed`).
- **Nullabile**: No.
- **Relazioni**: `unsubscribed` ⇒ riga in [`notification_marketing_suppressions`](notification_marketing_suppressions.md) (`scope = global`).

### source
- **Legacy**: dedotto dal punto di ingresso (`createSubscribe` da home / `common.helper.createSubscribes` da signup/registrazione property)
- **Descrizione**: da dove è arrivata l'iscrizione.
- **Tipo**: `enum notification.newsletter_source` — valori: `public_site`, `mobile_app`,
  `user_signup`, `property_registration`, `admin`.
- **Default**: `public_site`.
- **Nullabile**: No.
- **Relazioni**: —.

### confirmation_token / confirmed_at
- **Legacy**: — (nessun double opt-in)
- **Descrizione**: token per confermare l'iscrizione (email di conferma) e data della conferma.
- **Tipo**: `varchar(64)` · `timestamptz`.
- **Default**: generato · `NULL`.
- **Nullabile**: Sì (token azzerato dopo la conferma) / Sì.
- **Relazioni**: `confirmed_at` valorizzato ⇒ `status = subscribed`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 5.538 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### unsubscribe_token
- **Legacy**: —
- **Descrizione**: token opaco stabile per il link di disiscrizione.
- **Tipo**: `varchar(64)`.
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### consent_notice_version
- **Legacy**: — (nessuna prova di consenso)
- **Descrizione**: versione dell'**informativa privacy** vigente e accettata al momento dell'iscrizione
  (es. `"2026-01"`). Con `confirmed_at` + `confirmation_token` (il double opt-in) e `source` (fonte)
  costituisce la **prova del consenso** (art. 7 GDPR — [`docs/11 §"subscribes"`](../../../progettazione/11-modulo-privacy.md)).
  IP/user-agent dell'opt-in → log di audit dedicato con retention breve, non in questa tabella
  (minimizzazione).
- **Tipo**: `varchar(40)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`NULL` per lo storico migrato — nessuna informativa tracciabile).
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 5.538 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### customer_public_id
- **Legacy**: — (`subscribes` non collegava a `customers`)
- **Descrizione**: cliente C5, se l'email corrisponde a un'anagrafica (best-effort, per il GDPR).
- **Tipo**: `uuid`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 5.538 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### unsubscribed_at
- **Legacy**: `subscribes.updatedAt` all'atto del `status = 0`
- **Descrizione**: data della disiscrizione.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: `subscribes.createdAt` / `updatedAt` / `status = 0`
- **Descrizione**: iscrizione / modifica / soft-delete (amministrativo). Retention: 24 mesi
  dall'ultimo contatto/consenso ([`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md)), ma la
  **prova di opt-out** resta a vita in `notification_marketing_suppressions`.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- ~6,5 k righe, crescita lenta. Indici mirati; l'export Excel del pannello admin è client-side su un
  `SELECT` filtrato per `status`.
- Se la newsletter viene **riattivata** come invio reale, i destinatari si risolvono da qui con
  `status = subscribed` e verifica su `notification_marketing_suppressions`.

## Implementazione .NET

`NewsletterSubscription` è **aggregate root** a sé — non ha `property_id` (lista globale di
piattaforma) — del modulo `Module.Notification`, sottoarea `Marketing`, schema `notification`.

### Enum propri

```csharp
// Domain/Enums/NewsletterStatus.cs
public enum NewsletterStatus
{
	PendingConfirmation = 0,
	Subscribed = 1,
	Unsubscribed = 2
}

// Domain/Enums/NewsletterSource.cs
public enum NewsletterSource
{
	PublicSite = 0,
	MobileApp = 1,
	UserSignup = 2,
	PropertyRegistration = 3,
	Admin = 4
}
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<NewsletterStatus>("notification", "newsletter_status");
modelBuilder.HasPostgresEnum<NewsletterSource>("notification", "newsletter_source");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<NewsletterStatus>("notification.newsletter_status");
dataSourceBuilder.MapEnum<NewsletterSource>("notification.newsletter_source");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class NewsletterSubscriptionConfiguration : IEntityTypeConfiguration<NewsletterSubscription>
{
	public void Configure(EntityTypeBuilder<NewsletterSubscription> builder)
	{
		builder.ToTable("marketing_newsletter_subscriptions", "notification", t => t.HasComment(
			"Iscrizione alla newsletter globale di piattaforma (nessuno stabilimento associato)."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_newsletter_subscriptions");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico stabile (export GDPR, pannello admin).");
		builder.HasIndex(x => x.PublicId).IsUnique()
			.HasDatabaseName("uq_notification_marketing_newsletter_subscriptions_public_id");

		builder.Property(x => x.Email)
			.HasColumnName("email")
			.HasMaxLength(320)
			.IsRequired()
			.HasComment("Indirizzo iscritto, normalizzato lower-case e trim.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.IsRequired()
			.HasDefaultValue(NewsletterStatus.PendingConfirmation)
			.HasComment("Stato del ciclo di vita double opt-in: in attesa di conferma, iscritto, disiscritto.");

		builder.Property(x => x.Source)
			.HasColumnName("source")
			.IsRequired()
			.HasDefaultValue(NewsletterSource.PublicSite)
			.HasComment("Punto di ingresso dell'iscrizione (sito pubblico, app, signup utente, creazione stabilimento, admin).");

		builder.Property(x => x.ConfirmationToken)
			.HasColumnName("confirmation_token")
			.HasMaxLength(64)
			.HasComment("Token per confermare l'iscrizione via email; azzerato dopo la conferma.");

		builder.Property(x => x.ConfirmedAt)
			.HasColumnName("confirmed_at")
			.HasComment("Data di conferma del double opt-in.");

		builder.Property(x => x.UnsubscribeToken)
			.HasColumnName("unsubscribe_token")
			.HasMaxLength(64)
			.IsRequired()
			.HasComment("Token opaco stabile per il link di disiscrizione.");
		builder.HasIndex(x => x.UnsubscribeToken).IsUnique()
			.HasDatabaseName("uq_notification_marketing_newsletter_subscriptions_unsubscribe_token");

		builder.Property(x => x.ConsentNoticeVersion)
			.HasColumnName("consent_notice_version")
			.HasMaxLength(40)
			.HasComment("Versione dell'informativa privacy accettata al momento dell'iscrizione — prova del consenso art. 7 GDPR.");

		builder.Property(x => x.CustomerPublicId)
			.HasColumnName("customer_public_id")
			.HasComment("public_id di identity_access... customers (C5), copiato — nessuna FK cross-schema.");

		builder.Property(x => x.UnsubscribedAt)
			.HasColumnName("unsubscribed_at")
			.HasComment("Data della disiscrizione.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di iscrizione.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di soft-delete amministrativo.");

		builder.HasIndex(x => x.Email)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_notification_marketing_newsletter_subscriptions_email");
		// nota: l'indice UNIQUE è su lower(email); vedi "Note" sotto per la traduzione HasIndex + funzione SQL.

		builder.HasIndex(x => x.ConfirmationToken)
			.HasFilter("confirmation_token IS NOT NULL")
			.IsUnique()
			.HasDatabaseName("uq_notification_marketing_newsletter_subscriptions_confirmation_token");

		builder.HasIndex(x => new { x.Status, x.CreatedAt })
			.HasDatabaseName("ix_notification_marketing_newsletter_subscriptions_status_created");
	}
}
```

### Relazioni

- `CustomerPublicId`: id copiato da C5 (best-effort, per il GDPR), nessuna FK.
- `Status = Unsubscribed` ⇒ deve esistere una riga corrispondente in
  [`notification_marketing_suppressions`](notification_marketing_suppressions.md) (`scope = global`);
  creata dallo stesso comando applicativo `Unsubscribe`, non da una FK del DB.

### Note

- `NewsletterSubscription` implementa `ISoftDeletable` e `IAuditable`.
- L'indice `UNIQUE` della scheda è definito su `lower(email)`; poiché `Email` è normalizzata a
  lower-case già a livello di dominio (`Email.Create(raw)` non accetta valori con maiuscole), un
  `HasIndex(x => x.Email).IsUnique().HasFilter("deleted_at IS NULL")` scalare è equivalente e non
  richiede un indice su espressione (EF Core non genera nativamente `CREATE INDEX ... (lower(...))`;
  se in futuro la normalizzazione in dominio venisse rimossa, l'indice andrebbe rifatto a mano nella
  migration con `migrationBuilder.Sql(...)`).
- Double opt-in: `RequestSubscription(email, source, consentNoticeVersion)` → riga
  `PendingConfirmation` + invio di una `Delivery` dedicata (`kind = newsletter_confirmation`) →
  `ConfirmSubscription(token)` → `Subscribed` + `ConfirmedAt`; `Unsubscribe(token)` → `Unsubscribed` +
  comando che crea la riga in `MarketingSuppression` (`scope = global`, `channel = email`,
  `reason = unsubscribe_link`).
- Le rotte applicative vanno dietro il ruolo `systemadmin` (il legacy era `optionalAuth`, senza auth).
- **Migrazione**: 6 548 righe da `subscribes`; dedup per `lower(email)` (tenendo la riga più recente);
  `status = 1` → `Subscribed` (con prova di consenso di fatto assente per lo storico —
  `ConsentNoticeVersion = NULL`); `status = 0` (434 righe) → `Unsubscribed` + riga gemella in
  `notification_marketing_suppressions`; `source` dedotto dal contesto di creazione ove ricostruibile,
  altrimenti `PublicSite`; `unsubscribe_token`/`confirmation_token` generati ex-novo per ogni riga
  migrata.
