# Tabella `notification_marketing_suppressions`

> **Dominio**: C6 — `notification` · **sottoarea**: `marketing` · **schema**: `notification` · **nome
> flat**: `notification_marketing_suppressions` · **forma canonica**:
> `notification.marketing_suppressions`.
>
> **Nuova.** È la **lista di soppressione** (opt-out) richiesta da
> [`docs/80-retention.md` §3.6 / §"a vita"](../../../progettazione/80-retention.md) e
> [`docs/11-modulo-privacy.md` §"marketing"](../../../progettazione/11-modulo-privacy.md): l'unico dato marketing
> che si conserva **a tempo indeterminato**, perché è la **prova della revoca** e serve a non ricontattare
> chi si è disiscritto (anche dopo che il contatto è stato cancellato per retention). Fonti:
> [`docs/80-retention.md` §3.6, §5, §7](../../../progettazione/80-retention.md),
> [`docs/11-modulo-privacy.md`](../../../progettazione/11-modulo-privacy.md).

---

## Descrizione

**Una riga = un indirizzo/telefono da non contattare più per marketing**, con il motivo, la data e la
fonte. Prima di ogni invio marketing (`kind IN ('newsletter','marketing_campaign','marketing_automation')`)
il modulo verifica che il destinatario **non** sia qui.

Scope: normalmente **per stabilimento** (`property_id`), ma con supporto per la soppressione
**globale** (`property_id IS NULL`) — es. hard-bounce ripetuti, reclamo spam, richiesta esplicita di
non essere contattati da nessuno stabilimento della piattaforma.

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `reason` | **enum esclusivo**: un motivo per riga (se ne emergono altri, riga separata). |
| `scope` | derivato: `property` se `property_id` valorizzato, `global` se `NULL` — nessun campo esplicito. |
| `channel` | **enum esclusivo** (`email` / `whatsapp` / `sms` / `all`): un opt-out può riguardare un canale o tutti. |

## Snapshot

`address` è congelato per definizione (è il valore contro cui si confronta all'invio). Nessun `_snapshot`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_marketing_suppressions` | `id` | PK | — |
| `uq_..._scope_address_channel` | `(coalesce(property_id,'00000000-0000-0000-0000-000000000000'), lower(address), channel)` | UNIQUE | una riga per (scope, indirizzo, canale) |
| `ix_..._address` | `(lower(address))` | btree | verifica veloce all'invio (anche cross-property per il match globale) |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### property_id
- **Legacy**: — (il legacy non aveva soppressione)
- **Descrizione**: stabilimento per cui vale la soppressione; `NULL` = **globale** (tutti gli
  stabilimenti).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C2.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 434 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### address
- **Legacy**: — (era `subscribes.email` con `status = 0`, senza semantica di prova)
- **Descrizione**: email (lower-case) o numero E.164 da sopprimere.
- **Tipo**: `varchar(320)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: confrontato con `notification_deliveries.recipient_address_snapshot` /
  `notification_marketing_contacts.email` all'invio.

### channel
- **Legacy**: —
- **Descrizione**: canale a cui si applica l'opt-out.
- **Tipo**: `enum notification.channel_or_all` — valori: `email`, `whatsapp`, `sms`, `all`.
- **Default**: `all`.
- **Nullabile**: No.
- **Relazioni**: —.

### reason
- **Legacy**: —
- **Descrizione**: motivo della soppressione.
- **Tipo**: `enum notification.suppression_reason` — valori: `unsubscribe_link` (click sul link in
  un'email), `manual_request` (richiesta all'operatore), `spam_complaint` (feedback loop del provider),
  `hard_bounce` (indirizzo inesistente, N bounce consecutivi), `gdpr_erasure` (cancellazione GDPR:
  si tiene solo l'hash per non ricontattare), `admin`.
- **Default**: `unsubscribe_link`.
- **Nullabile**: No.
- **Relazioni**: `hard_bounce` alimentato da `notification_deliveries` `status = 'bounced'`.

### source_delivery_public_id
- **Legacy**: —
- **Descrizione**: invio da cui deriva la soppressione (il link cliccato, il bounce): tracciabilità
  della prova.
- **Tipo**: `uuid`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **id copiato** da [`notification_deliveries.public_id`](notification_deliveries.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 434 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### contact_public_id
- **Legacy**: —
- **Descrizione**: contatto marketing corrispondente al momento della soppressione (se esisteva).
  **Non** una FK: il contatto può essere cancellato per retention mentre questa riga resta a vita.
- **Tipo**: `uuid`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: id copiato da `notification_marketing_contacts.public_id`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 434 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### note
- **Legacy**: —
- **Descrizione**: testo libero (per `manual_request` / `admin`).
- **Tipo**: `varchar(500)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 434 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### suppressed_at
- **Legacy**: `subscribes.updatedAt` (all'atto del `status = 0`)
- **Descrizione**: quando è scattata la soppressione.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: —
- **Descrizione**: creazione / modifica. **`deleted_at` in pratica non si usa**: la lista di
  soppressione **non ha retention** (è la prova dell'opt-out). Una re-iscrizione volontaria crea una
  nuova evidenza di consenso in `notification_marketing_contacts`, non cancella questa riga.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at` (di fatto sempre `NULL`).

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- Tabella piccola e in sola crescita lenta. L'indice su `lower(address)` serve il controllo all'invio
  (una `SELECT EXISTS` per destinatario) — meglio ancora caricare l'intero set in una **struttura in
  memoria** (Bloom filter / HashSet) rifrescata periodicamente se i volumi di invio crescono.
- Verifica combinata: `property_id = :p OR property_id IS NULL` + `channel IN (:c, 'all')`.

## Implementazione .NET

`MarketingSuppression` è un'**entità semplice** (non `AggregateRoot`, nessun evento proprio) del modulo
`Module.Notification`, sottoarea `Marketing`, schema `notification`: è la lista di opt-out, prova della
revoca del consenso, conservata a tempo indeterminato.

### Enum propri

```csharp
// Domain/Enums/SuppressionReason.cs
public enum SuppressionReason
{
	UnsubscribeLink = 0,
	ManualRequest = 1,
	SpamComplaint = 2,
	HardBounce = 3,
	GdprErasure = 4,
	Admin = 5
}

// Domain/Enums/ChannelOrAll.cs
public enum ChannelOrAll
{
	Email = 0,
	Whatsapp = 1,
	Sms = 2,
	All = 3
}
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<SuppressionReason>("notification", "suppression_reason");
modelBuilder.HasPostgresEnum<ChannelOrAll>("notification", "channel_or_all");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<SuppressionReason>("notification.suppression_reason");
dataSourceBuilder.MapEnum<ChannelOrAll>("notification.channel_or_all");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class MarketingSuppressionConfiguration : IEntityTypeConfiguration<MarketingSuppression>
{
	public void Configure(EntityTypeBuilder<MarketingSuppression> builder)
	{
		builder.ToTable("marketing_suppressions", "notification", t => t.HasComment(
			"Lista di soppressione (opt-out) marketing: indirizzi da non ricontattare, prova della revoca del consenso."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_suppressions");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties per cui vale la soppressione; NULL = globale su tutta la piattaforma.");

		builder.Property(x => x.Address)
			.HasColumnName("address")
			.HasMaxLength(320)
			.IsRequired()
			.HasComment("Email (lower-case) o numero E.164 da sopprimere.");

		builder.Property(x => x.Channel)
			.HasColumnName("channel")
			.IsRequired()
			.HasDefaultValue(ChannelOrAll.All)
			.HasComment("Canale a cui si applica l'opt-out (email, whatsapp, sms, o tutti).");

		builder.Property(x => x.Reason)
			.HasColumnName("reason")
			.IsRequired()
			.HasDefaultValue(SuppressionReason.UnsubscribeLink)
			.HasComment("Motivo della soppressione (click unsubscribe, richiesta manuale, reclamo spam, hard bounce, cancellazione GDPR, admin).");

		builder.Property(x => x.SourceDeliveryPublicId)
			.HasColumnName("source_delivery_public_id")
			.HasComment("public_id della notification_deliveries da cui deriva la soppressione (link cliccato, bounce) — copiato, nessuna FK: deve sopravvivere alla purge della consegna.");

		builder.Property(x => x.ContactPublicId)
			.HasColumnName("contact_public_id")
			.HasComment("public_id del contatto marketing al momento della soppressione — copiato, nessuna FK: deve sopravvivere alla cancellazione del contatto per retention.");

		builder.Property(x => x.Note)
			.HasColumnName("note")
			.HasMaxLength(500)
			.HasComment("Testo libero per i motivi manual_request / admin.");

		builder.Property(x => x.SuppressedAt)
			.HasColumnName("suppressed_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Quando è scattata la soppressione.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità con le altre tabelle; di fatto sempre NULL — la lista di soppressione non ha retention.");

		builder.HasIndex(x => new { x.PropertyId, x.Address, x.Channel })
			.IsUnique()
			.HasDatabaseName("uq_notification_marketing_suppressions_scope_address_channel");
		// property_id è nullable: l'espressione reale in migration usa
		// coalesce(property_id, '00000000-0000-0000-0000-000000000000') per rendere NULL "confrontabile"
		// nell'UNIQUE — vedi nota sotto.

		builder.HasIndex(x => x.Address)
			.HasDatabaseName("ix_notification_marketing_suppressions_address");
	}
}
```

### Relazioni

- `PropertyId`: id copiato da C2 (nullable = soppressione globale), nessuna FK.
- `SourceDeliveryPublicId`, `ContactPublicId`: id copiati (non FK), **per scelta esplicita**: la
  soppressione deve restare leggibile anche dopo che la consegna o il contatto di origine sono stati
  cancellati per retention (24 mesi / 6 mesi), mentre questa tabella non scade mai. Una FK reale (anche
  `ON DELETE SET NULL`) legherebbe comunque il ciclo di vita delle due tabelle in modi non voluti.

### Note

- `MarketingSuppression` implementa `IAuditable`. Implementa anche `ISoftDeletable` per coerenza con la
  colonna `deleted_at` presente a schema (Global Query Filter incluso), ma **il dominio non espone mai un
  comando che la invoca**: non esiste un caso d'uso "cancella una soppressione" — è la prova
  dell'opt-out e resta a vita.
- L'`UNIQUE` su `(property_id, address, channel)` con `property_id` nullable richiede l'espressione
  `coalesce(property_id, '00000000-0000-0000-0000-000000000000')` per trattare tutte le righe globali
  come "stesso valore" ai fini dell'unicità (altrimenti PostgreSQL considera `NULL <> NULL` e permette
  doppioni globali) — EF Core non traduce l'espressione da `HasIndex` lambda: si aggiunge con
  `.HasAnnotation("Relational:Filter", ...)` non applicabile a un valore di colonna, quindi si rifinisce
  l'indice nella migration generata (`migrationBuilder.Sql` che sostituisce la definizione con
  `coalesce(...)`), oppure — più pulito — si introduce una colonna calcolata `scope_key uuid` (
  `GENERATED ALWAYS AS (coalesce(property_id, '00000000-...')) STORED`) e si mette l'`UNIQUE` su
  `(scope_key, lower(address), channel)`.
- `ISuppressionList.IsSuppressed(address, propertyId, channel)`: servizio di dominio con cache in
  memoria (refresh periodico o invalidazione puntuale sulla scrittura), da interrogare prima di ogni
  invio marketing; alimentato da: click sul link di disiscrizione, webhook bounce/complaint dei
  provider email, endpoint admin, evento di dominio `MarketingConsentWithdrawn`.
- **Migrazione**: `subscribes` con `status = 0` (434 righe) → righe `Reason = UnsubscribeLink`,
  `PropertyId = NULL` (scope globale), `Channel = Email`; nessun'altra fonte legacy (la tabella è
  nuova).
