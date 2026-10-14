# Tabella `notification_marketing_campaign_recipients`

> **Dominio**: C6 — `notification` · **sottoarea**: `marketing` · **schema**: `notification` · **nome
> flat**: `notification_marketing_campaign_recipients` · **forma canonica**:
> `notification.marketing_campaign_recipients`.
>
> Sostituisce l'array JSON `recipientList` di
> [`marketing_email_campaign`](../../../analisi-pre/db/marketing_email_campaign.md) (i **destinatari
> effettivi**, congelati alla creazione della campagna) e — per le automazioni — l'array `recipients`
> di [`marketing_automation`](../../../analisi-pre/db/marketing_automation.md). Fonti:
> [`docs/03-gestione-dati.md` §2.4quater](../../../progettazione/03-gestione-dati.md),
> [`docs/11-modulo-privacy.md` §"marketing"](../../../progettazione/11-modulo-privacy.md),
> [`docs/80-retention.md` §3.6, §4](../../../progettazione/80-retention.md).

---

## Descrizione

**Una riga = un destinatario di un invio marketing** (campagna o automazione). È lo **snapshot** dei
destinatari al momento dell'invio, con l'esito per singolo indirizzo e il collegamento alla riga di
[`notification_deliveries`](notification_deliveries.md) che ha eseguito la consegna.

Il legacy iterava su `recipientList` senza registrare nulla per destinatario (un fallimento era solo
`console.error`, la campagna passava comunque a `sent`). Qui ogni destinatario ha una riga con stato.

**Retention**: lo snapshot destinatari è 🗑️ a **6 mesi** dall'invio
([`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md), job `PurgeMarketingSnapshots`) —
verificando il consenso **al momento dell'invio**, non dello snapshot ([`docs/11 §"marketing"`](../../../progettazione/11-modulo-privacy.md)).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `parent_kind` | **enum esclusivo**: `campaign` / `automation` (la riga appartiene a un invio dell'uno o dell'altro). |
| `status` | **enum esclusivo**: esito per destinatario. |

## Snapshot

`email_snapshot`, `full_name_snapshot`: valori del contatto **congelati** alla creazione dell'invio
(il messaggio spedito non segue le modifiche successive alla rubrica).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_marketing_campaign_recipients` | `id` | PK | — |
| `ix_..._parent` | `(parent_kind, parent_id, status)` | btree | risultati per invio ("consegnati / falliti / saltati") |
| `ix_..._contact` | `(contact_id) WHERE contact_id IS NOT NULL` | btree parziale | "cosa ha ricevuto questo contatto" (GDPR) |
| `uq_..._dedup` | `(parent_kind, parent_id, lower(email_snapshot))` | UNIQUE | niente doppio invio allo stesso indirizzo nello stesso run |

## Campi

### id
- **Legacy**: elemento di `recipientList` / `recipients`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### parent_kind
- **Legacy**: dedotto dalla tabella contenitrice
- **Descrizione**: se il destinatario è di una campagna o di un'automazione.
- **Tipo**: `enum notification.marketing_parent_kind` — valori: `campaign`, `automation`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: tipizza `parent_id`.

### parent_id
- **Legacy**: `marketing_email_campaign.id` / `marketing_automation.id`
- **Descrizione**: invio a cui la riga appartiene.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** →
  [`notification_marketing_email_campaigns.id`](notification_marketing_email_campaigns.md) **o**
  [`notification_marketing_automations.id`](notification_marketing_automations.md) secondo `parent_kind`
  (FK condizionale via trigger / due colonne nullable in implementazione — vedi .NET). `ON DELETE CASCADE`.

### property_id
- **Legacy**: ereditato dalla campagna/automazione
- **Descrizione**: stabilimento (denormalizzato).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: dal parent.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2.

### contact_id
- **Legacy**: — (l'array era di sole stringhe email)
- **Descrizione**: contatto marketing corrispondente, se risolvibile.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (destinatario da lista email grezza non in rubrica).
- **Relazioni**: **FK interna** → [`notification_marketing_contacts.id`](notification_marketing_contacts.md) `ON DELETE SET NULL`.

### email_snapshot
- **Legacy**: elemento di `recipientList` (spesso stringhe vuote nei dati)
- **Descrizione**: indirizzo email destinatario, congelato all'invio.
- **Tipo**: `varchar(320)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: snapshot.

### full_name_snapshot
- **Legacy**: — (nel legacy non c'era)
- **Descrizione**: nome del destinatario all'invio (per personalizzazione e audit).
- **Tipo**: `varchar(160)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: snapshot.

### status
- **Legacy**: — (nessun esito per destinatario)
- **Descrizione**: esito dell'invio a questo destinatario.
- **Tipo**: `enum notification.recipient_status` — valori: `pending`, `sent`, `delivered`, `failed`,
  `bounced`, `skipped` (consenso mancante / soppresso / indirizzo vuoto).
- **Default**: `pending`.
- **Nullabile**: No.
- **Relazioni**: allineato allo `status` della `notification_deliveries` collegata.

### delivery_public_id
- **Legacy**: —
- **Descrizione**: riga di consegna che ha eseguito l'invio a questo destinatario.
- **Tipo**: `uuid`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`status = skipped` — nessuna consegna tentata).
- **Relazioni**: **id copiato** da [`notification_deliveries.public_id`](notification_deliveries.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 10.741 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### skip_reason
- **Legacy**: —
- **Descrizione**: perché il destinatario è stato saltato (`no_consent`, `suppressed`,
  `empty_address`, `invalid_address`, `duplicate`).
- **Tipo**: `enum notification.recipient_skip_reason`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 10.741 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### sent_at
- **Legacy**: —
- **Descrizione**: quando il messaggio è stato spedito a questo destinatario.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: decorrenza retention 6 mesi dello snapshot.

### created_at / updated_at / deleted_at
- **Legacy**: — (elementi di array, nessun timestamp)
- **Descrizione**: creazione / modifica. `deleted_at` esposto per uniformità; la retention 6 mesi
  hard-delete queste righe.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

È la **de-jsonizzazione** di `recipientList` / `recipients`: 1 riga per elemento, con contatto
risolto, esito e collegamento alla consegna.

## Suggerimenti per le prestazioni

- Può crescere molto per campagne grandi. **Retention aggressiva** (6 mesi) e partizionamento per mese
  di `created_at` la tengono contenuta.
- La vista "risultati campagna" usa `(parent_kind, parent_id, status)` — nessun `GROUP BY`
  sull'outbox; i contatori aggregati stanno già sulla campagna/automazione.
- Inserimento bulk alla partenza dell'invio (`INSERT … SELECT` dai membri lista con consenso).

## Implementazione .NET

`CampaignRecipient` è l'**entità figlia** (snapshot di invio) del modulo `Module.Notification`,
sottoarea `Marketing`, schema `notification`: una riga per destinatario di un invio (campagna o
automazione), con l'esito e il collegamento alla consegna.

### Scelta di modellazione per `parent_kind` / `parent_id` (polimorfismo)

La scheda (sezione Campi) documenta un **unico** `parent_id bigint` tipizzato da `parent_kind`, non due
colonne — è la stessa forma fisica del legacy (un array unico, `recipientList` o `recipients`, letto
dal contenitore). Questa Configuration mantiene **quelle due colonne esattamente**, senza introdurre
`campaign_id`/`automation_id` fisiche:

- **Nessuna FK EF** su `ParentId`: una singola colonna non può puntare relazionalmente a due tabelle
  diverse a seconda del valore di un'altra colonna — EF Core non esprime FK condizionali.
- **Integrità referenziale** applicata da un **trigger DB** (`trg_notification_marketing_campaign_recipients_parent_fk`,
  aggiunto con `migrationBuilder.Sql(...)` nella migration, non esprimibile in modo dichiarativo con
  l'API fluent) che, su `INSERT`/`UPDATE`, verifica che `parent_id` esista nella tabella indicata da
  `parent_kind` (`marketing_email_campaigns` se `campaign`, `marketing_automations` se `automation`) e
  solleva eccezione altrimenti. Il trigger sostituisce anche il cascade: la cancellazione fisica di una
  campagna/automazione non è prevista (soft-delete), quindi non serve `ON DELETE`.
- A livello applicativo, l'aggregate `EmailCampaign`/`MarketingAutomation` espone comunque una
  collezione di sola lettura (`Recipients`) mappata con una query filtrata su `(ParentKind, ParentId)`,
  non una vera navigazione EF `HasMany` (che richiederebbe la FK dichiarativa che non c'è).

Questa è una **decisione non banale**: l'alternativa (due colonne nullable `CampaignId`/`AutomationId`
con `CHECK` "esattamente una valorizzata" e due FK reali) sarebbe più idiomatica EF, ma avrebbe
richiesto un cambio di colonne rispetto a quanto già scritto nella sezione Campi della scheda — si è
preferito restare fedeli allo schema fisico lì descritto.

### Enum propri

```csharp
// Domain/Enums/MarketingParentKind.cs
public enum MarketingParentKind
{
	Campaign = 0,
	Automation = 1
}

// Domain/Enums/RecipientStatus.cs
public enum RecipientStatus
{
	Pending = 0,
	Sent = 1,
	Delivered = 2,
	Failed = 3,
	Bounced = 4,
	Skipped = 5
}

// Domain/Enums/RecipientSkipReason.cs
public enum RecipientSkipReason
{
	NoConsent = 0,
	Suppressed = 1,
	EmptyAddress = 2,
	InvalidAddress = 3,
	Duplicate = 4
}
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<MarketingParentKind>("notification", "marketing_parent_kind");
modelBuilder.HasPostgresEnum<RecipientStatus>("notification", "recipient_status");
modelBuilder.HasPostgresEnum<RecipientSkipReason>("notification", "recipient_skip_reason");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<MarketingParentKind>("notification.marketing_parent_kind");
dataSourceBuilder.MapEnum<RecipientStatus>("notification.recipient_status");
dataSourceBuilder.MapEnum<RecipientSkipReason>("notification.recipient_skip_reason");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class CampaignRecipientConfiguration : IEntityTypeConfiguration<CampaignRecipient>
{
	public void Configure(EntityTypeBuilder<CampaignRecipient> builder)
	{
		builder.ToTable("marketing_campaign_recipients", "notification", t => t.HasComment(
			"Destinatario di un invio marketing (campagna o automazione): snapshot al momento dell'invio, con esito per indirizzo."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_campaign_recipients");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ParentKind)
			.HasColumnName("parent_kind")
			.IsRequired()
			.HasComment("Se il destinatario appartiene a una campagna o a un'automazione; tipizza parent_id.");

		builder.Property(x => x.ParentId)
			.HasColumnName("parent_id")
			.IsRequired()
			.HasComment("Invio (campagna o automazione) a cui la riga appartiene — integrità referenziale condizionale applicata da trigger DB, nessuna FK EF (vedi nota sopra).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, denormalizzato dal parent — nessuna FK cross-schema.");

		builder.Property(x => x.ContactId)
			.HasColumnName("contact_id")
			.HasComment("Contatto marketing corrispondente, se risolvibile (destinatario da lista email grezza altrimenti NULL).");

		builder.Property(x => x.EmailSnapshot)
			.HasColumnName("email_snapshot")
			.HasMaxLength(320)
			.IsRequired()
			.HasComment("Indirizzo email destinatario, congelato al momento dell'invio.");

		builder.Property(x => x.FullNameSnapshot)
			.HasColumnName("full_name_snapshot")
			.HasMaxLength(160)
			.HasComment("Nome del destinatario all'invio, per personalizzazione e audit.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.IsRequired()
			.HasDefaultValue(RecipientStatus.Pending)
			.HasComment("Esito dell'invio a questo destinatario, allineato allo status della Delivery collegata.");

		builder.Property(x => x.DeliveryPublicId)
			.HasColumnName("delivery_public_id")
			.HasComment("public_id della notification_deliveries che ha eseguito l'invio a questo destinatario — copiato, nessuna FK (NULL se status = skipped).");

		builder.Property(x => x.SkipReason)
			.HasColumnName("skip_reason")
			.HasComment("Perché il destinatario è stato saltato (consenso mancante, soppresso, indirizzo vuoto/non valido, duplicato).");

		builder.Property(x => x.SentAt)
			.HasColumnName("sent_at")
			.HasComment("Quando il messaggio è stato spedito a questo destinatario; decorrenza della retention a 6 mesi dello snapshot.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità; la retention a 6 mesi esegue una purge fisica (hard-delete), non un soft-delete.");

		builder.HasIndex(x => new { x.ParentKind, x.ParentId, x.Status })
			.HasDatabaseName("ix_notification_marketing_campaign_recipients_parent");

		builder.HasIndex(x => x.ContactId)
			.HasFilter("contact_id IS NOT NULL")
			.HasDatabaseName("ix_notification_marketing_campaign_recipients_contact");

		builder.HasIndex(x => new { x.ParentKind, x.ParentId, x.EmailSnapshot })
			.IsUnique()
			.HasDatabaseName("uq_notification_marketing_campaign_recipients_dedup");

		builder.HasOne(x => x.Contact)
			.WithMany()
			.HasForeignKey(x => x.ContactId)
			.HasConstraintName("fk_notification_marketing_campaign_recipients_contact")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

### Relazioni

- `ContactId` → FK reale verso `notification_marketing_contacts` (stesso schema, entità editata in un
  batch parallelo), `ON DELETE SET NULL` (il contatto può essere cancellato per retention mentre lo
  snapshot destinatario resta, coerente con la retention differenziata).
- `ParentId`/`ParentKind` → **nessuna FK EF** (polimorfismo su singola colonna); integrità applicata da
  trigger DB — vedi nota di modellazione sopra.
- `DeliveryPublicId`: id copiato da `notification_deliveries.public_id`, nessuna FK (la consegna può
  essere purgata prima o dopo questa riga, cicli di retention indipendenti: 24 mesi vs 6 mesi).
- `PropertyId`: id copiato da C2, nessuna FK.

### Note

- `CampaignRecipient` implementa `IAuditable`. Espone `DeletedAt`/`ISoftDeletable` solo per uniformità
  di schema (Global Query Filter incluso), ma il ciclo di vita reale è: la riga vive attiva, poi allo
  scadere della retention a 6 mesi il job `PurgeMarketingSnapshots` esegue una `DELETE` fisica — non un
  soft-delete applicativo.
- Popolamento: al `SendCampaign`/`RunAutomation` → `INSERT` bulk dei destinatari (lista risolta + verifica
  `IConsentGate`/`ISuppressionList`), poi una `Delivery` per riga; lo `Status` di ciascuna riga è
  aggiornato da un handler degli eventi di consegna del provider (`DeliverySent`, `DeliveryBounced`, …).
- L'indice `UNIQUE` di dedup `(parent_kind, parent_id, lower(email_snapshot))` è tradotto sopra come
  scalare su `EmailSnapshot`: l'`email_snapshot` è già normalizzato lower-case dal dominio al momento
  della creazione della riga (stesso ragionamento delle iscrizioni newsletter), quindi l'indice scalare
  è equivalente a quello su espressione della scheda.
- **Migrazione**: esplodere `recipientList` (campagne) / `recipients` (automazioni) in una riga per
  elemento; scartare le stringhe vuote (frequenti nei dati legacy); `ContactId` risolto per
  `(property_id, email)` via `notification_marketing_contacts`; `Status = Sent` per tutto lo storico
  (nessun esito reale disponibile nel legacy — solo `console.error` non persistito); `SentAt` =
  `updatedAt` del parent (campagna/automazione).
