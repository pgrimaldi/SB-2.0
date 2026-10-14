# Tabella `notification_marketing_contacts`

> **Dominio**: C6 — `notification` · **sottoarea**: `marketing` · **schema**: `notification` · **nome
> flat**: `notification_marketing_contacts` · **forma canonica**: `notification.marketing_contacts`.
>
> **Nuova** ([D-46](../_domande-committente.md)). Rubrica marketing **per stabilimento**: raccoglie in
> un unico posto ciò che il legacy teneva sparso e senza consenso —
> [`contact_list_member.contactId`](../../../analisi-pre/db/contact_list_member.md) (un `customers.id`,
> `NULL` sul 13%), gli array email grezzi `recipientList` / `recipients` di
> [`marketing_email_campaign`](../../../analisi-pre/db/marketing_email_campaign.md) /
> [`marketing_automation`](../../../analisi-pre/db/marketing_automation.md), l'auto-iscrizione da
> `bookings.isMarketingCampaign` (`addCustomer`). Fonti:
> [`docs/03-gestione-dati.md` §2.4quater, §3.1bis](../../../progettazione/03-gestione-dati.md),
> [`docs/11-modulo-privacy.md` §"marketing"](../../../progettazione/11-modulo-privacy.md),
> [`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md).

---

## Descrizione

**Una riga = un contatto marketing di uno stabilimento**, identificato dall'**email** (chiave
naturale). Opzionalmente collegato al cliente C5 (`customer_public_id`) quando il contatto è anche in
anagrafica. Porta lo **stato del consenso marketing**, la sua **fonte**, il **timestamp**, e un
**token di disiscrizione** stabile per il link "unsubscribe" nelle email.

Le liste ([`notification_marketing_contact_lists`](notification_marketing_contact_lists.md)) e i
destinatari delle campagne ([`notification_marketing_campaign_recipients`](notification_marketing_campaign_recipients.md))
puntano al **contatto**, non più a un `customers.id` grezzo.

### Che cos'è (e cosa NON è)

**Non è una copia della rubrica clienti.** [`identity_customer_customers`](../identity/identity_customer_customers.md)
(C5) è la rubrica completa di uno stabilimento (~224 k righe in prod: chiunque abbia prenotato o si
sia registrato). `notification_marketing_contacts` è il **sottoinsieme che è entrato in un pubblico
marketing di quella property**, con lo stato di consenso — tipicamente centinaia/qualche migliaio di
righe, non tutta la rubrica.

`customer_public_id` è un **riferimento vivo** a C5 (nullable). `full_name` / `phone` / `email` sono
**denormalizzati** sulla riga (aggiornati per evento da C5 quando il contatto è collegato) così il
percorso d'invio non fa join cross-modulo.

### Criterio di creazione delle righe

Una riga è **inserita/aggiornata** (upsert per `(property_id, lower(email))`) quando un indirizzo
entra in un flusso marketing della property:

| Trigger | `consent_source` | Note |
|---|---|---|
| **Auto-iscrizione da prenotazione** — `bookings.isMarketingCampaign = true` alla creazione della prenotazione → il cliente confluisce nella lista di default `"Marketing"` della property (ex `addCustomer`) | `booking_flag` | `is_seed_from_booking = true`, `customer_public_id` collegato, `consent_status` = `unknown`/`granted` secondo il testo del flag mostrato al cliente (**[D-46](../_domande-committente.md)**) |
| **Aggiunta / import manuale** dal pannello Marketing → l'operatore digita o importa indirizzi, o seleziona clienti dalla rubrica | `manual_import` / `admin` | `customer_public_id` valorizzato solo se l'email risolve un cliente C5 della property |
| **Creazione di una campagna con lista di indirizzi grezzi** (ex `recipientList` JSON) → ogni indirizzo diventa un contatto | `campaign_recipient_list` | serve a far valere consenso e soppressione anche su questi destinatari, che nel legacy li aggiravano |
| **Disiscrizione** (click su `unsubscribe_token`) su un contatto che non esisteva ancora come riga | invariato | crea/aggiorna la riga con `consent_status = withdrawn` + riga in [`notification_marketing_suppressions`](notification_marketing_suppressions.md) |

**Non** crea righe qui: la registrazione utente, la sola presenza in rubrica C5, l'iscrizione alla
**newsletter** globale (che vive separata in
[`notification_marketing_newsletter_subscriptions`](notification_marketing_newsletter_subscriptions.md),
senza `property_id`).

Correzioni rispetto al legacy:

- **`contactId` = `customers.id` con 13% `NULL`** → chiave `(property_id, email)`, FK opzionale al
  cliente.
- **Nessun consenso tracciato** ([`docs/11 §"marketing"`](../../../progettazione/11-modulo-privacy.md): "oggi
  non c'è prova che il consenso sia mai stato dato") → i 4 elementi della prova richiesta dall'art. 7
  GDPR: `consent_status` (stato), `consent_source` (fonte), `consent_at` (quando),
  `consent_notice_version` (versione dell'informativa accettata). **Base giuridica e disegno da
  validare con DPO/legale** ([D-46](../_domande-committente.md)).
- **Verifica consenso solo alla creazione lista** → nel target si verifica **al momento dell'invio**
  ([`docs/11 §"marketing"`](../../../progettazione/11-modulo-privacy.md)): un contatto `withdrawn` o presente in
  [`notification_marketing_suppressions`](notification_marketing_suppressions.md) → `skipped`.
- **Nessun token di disiscrizione** → `unsubscribe_token` (random, per-contatto).
- **Email non normalizzate, duplicati** → `email` in lower-case, `UNIQUE (property_id, email)`.

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `consent_status` | **enum esclusivo**: `unknown` / `granted` / `withdrawn` (uno stato per contatto). |
| `consent_source` | **enum esclusivo**: come è entrato il contatto/consenso. |
| `is_seed_from_booking` | booleano indipendente (contatto creato dall'auto-iscrizione da prenotazione vs import manuale). |

## Snapshot

Non applicabile (i valori congelati all'invio stanno su
[`notification_marketing_campaign_recipients`](notification_marketing_campaign_recipients.md)).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_marketing_contacts` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | API/URL, GDPR |
| `uq_..._property_email` | `(property_id, lower(email)) WHERE deleted_at IS NULL` | UNIQUE parziale | un contatto per email per stabilimento |
| `uq_..._unsubscribe_token` | `(unsubscribe_token)` | UNIQUE | risoluzione del link di disiscrizione |
| `ix_..._customer` | `(customer_public_id) WHERE customer_public_id IS NOT NULL` | btree parziale | GDPR: dati marketing di una persona |
| `ix_..._consent` | `(property_id, consent_status)` | btree | segmenti "consenso attivo" per l'invio |

## Campi

### id
- **Legacy**: — (nuovo; il concetto era spalmato su `contact_list_member` / array email)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da
  [`notification_marketing_contact_list_members.contact_id`](notification_marketing_contact_list_members.md),
  [`notification_marketing_campaign_recipients.contact_id`](notification_marketing_campaign_recipients.md).

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile (API/URL, export GDPR).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### property_id
- **Legacy**: `contact_list.propertyId` (il contatto ereditava lo scoping dalla lista)
- **Descrizione**: stabilimento proprietario del contatto.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2; [D-01](../_domande-committente.md).

### email
- **Legacy**: `customers.email` (via `contactId`) / elemento di `recipientList`
- **Descrizione**: indirizzo email del contatto (chiave naturale), normalizzato lower-case + trim.
- **Tipo**: `varchar(320)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE (property_id, lower(email))`.

### full_name / phone
- **Legacy**: `customers.first_name`+`last_name` / `customers.phone`
- **Descrizione**: nome e telefono del contatto (denormalizzati per l'invio WhatsApp/SMS e per la
  personalizzazione, senza join a C5 in fase di invio).
- **Tipo**: `varchar(160)` · `varchar(32)`.
- **Default**: `NULL` · `NULL`.
- **Nullabile**: Sì / Sì.
- **Relazioni**: aggiornati per evento da C5 quando il contatto è collegato a un cliente.

### customer_public_id
- **Legacy**: `contact_list_member.contactId` (`customers.id`, 13% `NULL`)
- **Descrizione**: cliente C5 corrispondente, se in anagrafica.
- **Tipo**: `uuid` (= `public_id` di `identity_customer_customers`).
- **Default**: `NULL`.
- **Nullabile**: Sì (contatti importati / da campagna senza cliente).
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_customer_customers`](../identity/identity_customer_customers.md).

### consent_status
- **Legacy**: — (nessun consenso tracciato)
- **Descrizione**: stato del consenso all'invio di comunicazioni marketing.
- **Tipo**: `enum notification.marketing_consent_status` — valori: `unknown` (storico migrato senza
  prova), `granted`, `withdrawn`.
- **Default**: `unknown`.
- **Nullabile**: No.
- **Relazioni**: `withdrawn` ⇒ riga anche in [`notification_marketing_suppressions`](notification_marketing_suppressions.md);
  all'invio, `unknown`/`withdrawn` → `skipped` (policy da confermare, [D-46](../_domande-committente.md)).

### consent_source
- **Legacy**: dedotto dal punto di ingresso
- **Descrizione**: come il contatto/consenso è stato acquisito.
- **Tipo**: `enum notification.consent_source` — valori: `booking_flag` (`bookings.isMarketingCampaign`
  alla prenotazione), `public_signup` (form "offerte" sulla pagina pubblica della property),
  `manual_import` (import/aggiunta dal pannello Marketing), `campaign_recipient_list` (indirizzo di
  una lista grezza di campagna), `admin`, `unknown` (storico migrato).
- **Default**: `unknown`.
- **Nullabile**: No.
- **Relazioni**: —.

### consent_at
- **Legacy**: —
- **Descrizione**: data/ora del consenso (o della revoca, se `withdrawn`).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: decorrenza retention marketing (24 mesi dall'ultimo contatto/consenso,
  [`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md)).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 7.181 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### consent_notice_version
- **Legacy**: — (nessuna prova di consenso)
- **Descrizione**: versione dell'**informativa privacy** vigente e accettata all'atto del consenso
  (es. `"2026-01"`, `"v3"`). Insieme a `consent_source` (fonte), `consent_at` (quando) e — dove c'è un
  double opt-in — al record di conferma, costituisce la **prova del consenso** richiesta dall'art. 7
  GDPR ("il titolare deve essere in grado di dimostrare che l'interessato ha prestato il consenso").
  IP e user-agent dell'opt-in **non** stanno qui (minimizzazione): vanno in un log di audit dedicato,
  con retention breve.
- **Tipo**: `varchar(40)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`unknown` per tutto lo storico migrato — nessuna informativa tracciabile).
- **Relazioni**: la lista delle versioni informativa è un vocabolario di configurazione, non una
  tabella di dominio.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 7.181 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### unsubscribe_token
- **Legacy**: — (nessun link di disiscrizione)
- **Descrizione**: token opaco stabile per il link "annulla iscrizione" nelle email marketing.
- **Tipo**: `varchar(64)`.
- **Default**: generato (random 256 bit, base64url).
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`; la disiscrizione porta `consent_status = withdrawn` + riga di soppressione.

### is_seed_from_booking
- **Legacy**: implicito (`addCustomer` dal flusso prenotazione)
- **Descrizione**: contatto creato dall'auto-iscrizione alla lista di default quando
  `bookings.isMarketingCampaign` era attivo.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### last_contacted_at
- **Legacy**: — (nessun tracciamento)
- **Descrizione**: ultimo invio marketing riuscito verso questo contatto; guida la retention "24 mesi
  dall'ultimo contatto".
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: aggiornato per evento da [`notification_deliveries`](notification_deliveries.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 7.181 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: `contact_list_member.created_at` (mai aggiornato) / — / hard-delete
- **Descrizione**: creazione / modifica / soft-delete. Retention: contatto senza attività da 24 mesi
  → 🗑️ (tranne la **prova di revoca**, che vive in `notification_marketing_suppressions` a vita).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON: gli array email `recipientList` / `recipients` → righe qui (deduplicate per
`(property_id, email)`). `consent_*` sono colonne tipizzate, non un blob "preferenze".

## Suggerimenti per le prestazioni

- Volume: ~30 k membri vivi in prod → dopo la deduplica per email il conteggio cala. Indici mirati
  su `(property_id, consent_status)` per i segmenti e su `customer_public_id` per il GDPR.
- La verifica consenso all'invio è una lettura per `(property_id, email)` (UNIQUE) + un controllo su
  `notification_marketing_suppressions` — entrambe O(1).
- Purge continuo delle righe orfane/scadute (job `PurgeMarketingSnapshots`,
  [`docs/80-retention.md` §4](../../../progettazione/80-retention.md)).

## Implementazione .NET

`MarketingContact` è l'**aggregate root** della rubrica marketing per stabilimento nel modulo
`Module.Notification` (schema `notification`): chiave naturale `(property_id, lower(email))`,
referenziata da FK interne di altre tabelle marketing dello stesso schema, fuori da questo batch
(`notification_marketing_contact_list_members.contact_id`,
`notification_marketing_campaign_recipients.contact_id`). Porta lo stato del consenso GDPR
(`consent_status`/`consent_source`/`consent_at`/`consent_notice_version`) verificato **al momento
dell'invio** da `IConsentGate`, non alla creazione lista. `customer_public_id` è un riferimento vivo a
C5, id copiato senza FK.

### Enum propri

```csharp
// Domain/Enums/MarketingConsentStatus.cs
public enum MarketingConsentStatus { Unknown = 0, Granted = 1, Withdrawn = 2 }

// Domain/Enums/ConsentSource.cs
public enum ConsentSource
{
	BookingFlag = 0, PublicSignup = 1, ManualImport = 2, CampaignRecipientList = 3,
	Admin = 4, Unknown = 5
}
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<MarketingConsentStatus>("notification", "marketing_consent_status");
modelBuilder.HasPostgresEnum<ConsentSource>("notification", "consent_source");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<MarketingConsentStatus>("notification.marketing_consent_status");
dataSourceBuilder.MapEnum<ConsentSource>("notification.consent_source");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class MarketingContactConfiguration : IEntityTypeConfiguration<MarketingContact>
{
	public void Configure(EntityTypeBuilder<MarketingContact> builder)
	{
		builder.ToTable("marketing_contacts", "notification", t => t.HasComment(
			"Rubrica marketing per stabilimento: un contatto identificato dall'email, con stato di consenso, fonte e token di disiscrizione."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_contacts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, usato in API/URL e nell'export GDPR.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_notification_marketing_contacts_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.Email)
			.HasColumnName("email")
			.HasMaxLength(320)
			.IsRequired()
			.HasComment("Indirizzo email del contatto (chiave naturale), normalizzato lower-case + trim.");
		builder.HasIndex(x => new { x.PropertyId, x.Email })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_notification_marketing_contacts_property_email");

		builder.Property(x => x.FullName)
			.HasColumnName("full_name")
			.HasMaxLength(160)
			.HasComment("Nome del contatto, denormalizzato per la personalizzazione senza join a C5.");

		builder.Property(x => x.Phone)
			.HasColumnName("phone")
			.HasMaxLength(32)
			.HasComment("Telefono del contatto, denormalizzato per l'invio WhatsApp/SMS.");

		builder.Property(x => x.CustomerPublicId)
			.HasColumnName("customer_public_id")
			.HasComment("public_id di identity_customer.customers, copiato — nessuna FK cross-schema. Valorizzato quando il contatto è anche in anagrafica.");

		builder.Property(x => x.ConsentStatus)
			.HasColumnName("consent_status")
			.HasDefaultValue(MarketingConsentStatus.Unknown)
			.IsRequired()
			.HasComment("Stato del consenso all'invio di comunicazioni marketing.");

		builder.Property(x => x.ConsentSource)
			.HasColumnName("consent_source")
			.HasDefaultValue(ConsentSource.Unknown)
			.IsRequired()
			.HasComment("Come il contatto/consenso è stato acquisito.");

		builder.Property(x => x.ConsentAt)
			.HasColumnName("consent_at")
			.HasComment("Data/ora del consenso, o della revoca se withdrawn; decorrenza della retention marketing (24 mesi).");

		builder.Property(x => x.ConsentNoticeVersion)
			.HasColumnName("consent_notice_version")
			.HasMaxLength(40)
			.HasComment("Versione dell'informativa privacy accettata all'atto del consenso, prova ex art. 7 GDPR insieme a fonte e data.");

		builder.Property(x => x.UnsubscribeToken)
			.HasColumnName("unsubscribe_token")
			.HasMaxLength(64)
			.IsRequired()
			.HasComment("Token opaco stabile per il link di disiscrizione nelle email marketing.");
		builder.HasIndex(x => x.UnsubscribeToken)
			.IsUnique()
			.HasDatabaseName("uq_notification_marketing_contacts_unsubscribe_token");

		builder.Property(x => x.IsSeedFromBooking)
			.HasColumnName("is_seed_from_booking")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("True se il contatto è nato dall'auto-iscrizione alla lista di default (bookings.isMarketingCampaign).");

		builder.Property(x => x.LastContactedAt)
			.HasColumnName("last_contacted_at")
			.HasComment("Ultimo invio marketing riuscito verso questo contatto; guida la retention 24 mesi dall'ultimo contatto.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione del contatto.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete: retention 24 mesi senza attività (la prova di revoca resta a vita in notification_marketing_suppressions).");

		builder.HasIndex(x => x.CustomerPublicId)
			.HasFilter("customer_public_id IS NOT NULL")
			.HasDatabaseName("ix_notification_marketing_contacts_customer");

		builder.HasIndex(x => new { x.PropertyId, x.ConsentStatus })
			.HasDatabaseName("ix_notification_marketing_contacts_consent");
	}
}
```

`MarketingContact` implementa `ISoftDeletable` e `IAuditable`.

### Note di migrazione

- Da `contact_list_member` (join `customers` per email/nome/telefono/`customer_public_id`) e dagli array
  `recipientList`/`recipients` di `marketing_email_campaign`/`marketing_automation`: deduplica per
  `(property_id, lower(email))`, upsert.
- `consent_status = Unknown` per tutto lo storico migrato (nessuna prova di consenso nel legacy);
  `consent_source` dedotto dal punto di ingresso: `BookingFlag` per i membri della lista di default,
  `Admin`/`ManualImport` per le altre righe di `contact_list_member`, `CampaignRecipientList` per gli
  indirizzi grezzi degli array campagna.
- `consent_notice_version = NULL` per tutto lo storico (nessuna informativa tracciabile a posteriori).
- Righe con `contactId NULL` e nessuna email risolvibile → scartate (non producibile una chiave
  naturale).
- `unsubscribe_token` generato ex novo per ogni riga migrata (random 256 bit, base64url) — non esisteva
  nel legacy.
- **`subscribes` (newsletter) NON confluisce qui**: è iscrizione globale senza `property_id`, migra in
  [`notification_marketing_newsletter_subscriptions`](notification_marketing_newsletter_subscriptions.md).
- Punto aperto ([D-46](../_domande-committente.md)): base giuridica del trattamento, se `Unknown`
  blocca l'invio in `IConsentGate.CanSend`, se il consenso debba eventualmente vivere in C5 invece che
  qui — la Configuration sopra non dipende dall'esito, la logica di gate è in Application.
