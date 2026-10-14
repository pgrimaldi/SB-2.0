# Tabella `notification_templates`

> **Dominio**: C6 — `notification` · **sottoarea**: — (trasversale ai canali) · **schema**:
> `notification` · **nome flat**: `notification_templates` · **forma canonica**: `notification.templates`.
>
> **Catalogo unico dei template di messaggio.** Unifica i tre motori/cataloghi di template del legacy
> ([`docs/03-gestione-dati.md` §2.7, §3.1bis](../../../progettazione/03-gestione-dati.md)):
> [`custom_templates`](../../../analisi-pre/db/custom_templates.md) (103 righe — riepilogo prenotazione
> email/WhatsApp, sintassi `@token` sostituita **nel browser**),
> [`hotel_message_templates`](../../../analisi-pre/db/hotel_message_templates.md) (0 righe in prod —
> report convenzione, `{token}` nell'oggetto e `@token` nel corpo), e il corpo con `@token` di
> [`marketing_automation.textFields`](../../../analisi-pre/db/marketing_automation.md) /
> [`marketing_email_campaign.message`](../../../analisi-pre/db/marketing_email_campaign.md). Fonti:
> [`docs/03 §2.7`](../../../progettazione/03-gestione-dati.md),
> [`docs/01-…backend.md` §8](../../../progettazione/01-criticita-e-indicazioni-backend.md),
> [`docs/12-lingue-e-date.md` §1.5](../../../progettazione/12-lingue-e-date.md).

---

## Descrizione

Una riga = **un template** (oggetto + corpo con segnaposto) per uno scopo, un canale e una lingua.
I template sono **dati editabili dal gestore** (non `.resx`): stanno a DB
([`docs/12 §1.5`](../../../progettazione/12-lingue-e-date.md), criterio "chi scrive e quando"). Sono usati per:
il riepilogo/preventivo inviato al cliente, il report periodico alla struttura convenzionata, il corpo
di campagne ed automazioni marketing.

Correzioni rispetto al legacy:

- **Tre sintassi di segnaposto** (`@token` × 3 motori diversi, `{token}`) → **una sola**:
  `{{ nome_variabile }}` (mustache), [`docs/03 §2.7 punto 1`](../../../progettazione/03-gestione-dati.md).
- **Rendering nel browser** (`String.replace`, nessun escaping) → **un solo renderer server-side**
  (Razor, auto-escaping per canale), [`docs/03 §2.7 punto 2`](../../../progettazione/03-gestione-dati.md).
- **Segnaposto sconosciuto / valore mancante** → il legacy lasciava il token letterale o scartava la
  riga; qui → **stringa vuota** (o placeholder di debug in non-prod), mai `undefined`
  ([`docs/03 §2.7 punto 4`](../../../progettazione/03-gestione-dati.md)).
- **Vocabolario dei segnaposto ammessi**: **chiuso e validato** alla `create`/`update` (rifiuto se
  contiene un token fuori vocabolario canonico), [`docs/03 §2.7 punto 5`](../../../progettazione/03-gestione-dati.md).
  Il vocabolario canonico (`establishment_name`, `customer_full_name`, `booking_date_from`, …) è una
  **costante/enum di codice** con `.resx` per l'etichetta editor — **non** una tabella ([D-48](../_domande-committente.md)).
- **`custom_templates.type` `1`/`2`** (canale) e **`hotel_message_templates.lang`** → `channel` +
  `culture` espliciti.
- **`status` `0`/`1`** (ennesima reimplementazione "attivo/non attivo") → `deleted_at` + `is_active`.
- **`isDefault` senza transazione** (finestra con 0 o 2 default) → indice UNIQUE parziale
  `(property_id, template_kind, channel, culture) WHERE is_default` + gestione atomica.
- **`subject` default `"Oggetto da cambiare"`** che finiva come oggetto reale → `subject` nullable,
  validazione "non vuoto" per i `channel` che lo richiedono.
- **`createdBy` sempre `NULL`** → `created_by_user_id` reale (best-effort in migrazione).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `channel` | **enum esclusivo**: un template rende per **un** canale (o `any` = renderer sceglie l'escaping dal canale d'invio, caso `hotel_message_templates`). `custom_templates.type` `1`/`2` → `email`/`whatsapp`. |
| `template_kind` | **enum esclusivo**: uno scopo per template. |
| `is_active` / `is_default` | **booleani indipendenti** (concetti diversi): un template può essere attivo e non default. `is_default` unico per `(property_id, template_kind, channel, culture)`. |
| `culture` | non enum: codice cultura BCP-47 (insieme "di libreria", [`docs/12 §2`](../../../progettazione/12-lingue-e-date.md)). |

## Snapshot

Non applicabile: i valori **congelati** all'invio (testo renderizzato) stanno su
[`notification_deliveries.rendered_body`](notification_deliveries.md), non qui.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_templates` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | riferito da `notification_deliveries` e dagli endpoint gestionale |
| `ix_..._lookup` | `(property_id, template_kind, channel, culture) WHERE is_active AND deleted_at IS NULL` | btree parziale | selezione del template per scopo/canale/lingua ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): a DB mancava perché la causa era EF: due `HasIndex` sulle stesse colonne senza nome sono lo stesso indice, e il secondo sovrascrive il primo. |
| `uq_..._default` | `(property_id, template_kind, channel, culture) WHERE is_default AND deleted_at IS NULL` | UNIQUE parziale | un solo default per combinazione |

## Campi

### id
- **Legacy**: `custom_templates.id` / `hotel_message_templates.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da `notification_deliveries.template_id`,
  `notification_template_attachments.template_id`,
  `notification_marketing_email_campaigns.template_id`,
  `notification_marketing_automations.template_id`.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile; usato in API/URL gestionale e riferito da `notification_deliveries`.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### property_id
- **Legacy**: `custom_templates.property_id` / `hotel_message_templates.propertyId`
- **Descrizione**: stabilimento proprietario del template.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` = template **di piattaforma** / seed di sistema, usato come fallback).
- **Relazioni**: **`public_id` (uuid) copiato** da C2; [D-01](../_domande-committente.md).

### template_kind
- **Legacy**: dedotto dalla tabella (`custom_templates` → `reservation_summary`;
  `hotel_message_templates` → `agreement_report`; `marketing_automation`/`campaign` → `automation`/`campaign`)
- **Descrizione**: scopo del template.
- **Tipo**: `enum notification.template_kind` — valori: `reservation_summary`, `agreement_report`
  (report periodico alla **struttura partner** di una convenzione — C2 ha rinominato "hotel" in
  `agreement`, [`catalog_agreement_partners`](../catalog/catalog_agreement_partners.md)),
  `campaign`, `automation`, `transactional`, `generic`.
- **Default**: `generic`.
- **Nullabile**: No.
- **Relazioni**: `campaign`/`automation` sono referenziati da
  [`notification_marketing_email_campaigns`](notification_marketing_email_campaigns.md) /
  [`notification_marketing_automations`](notification_marketing_automations.md).

### channel
- **Legacy**: `custom_templates.type` (`1` email / `2` WhatsApp) · canale scelto all'invio per `hotel_message_templates`
- **Descrizione**: canale per cui è pensato il contenuto (l'escaping del renderer segue questo).
- **Tipo**: `enum notification.template_channel` — valori: `email`, `whatsapp`, `sms`, `push`, `any`.
- **Default**: `email`.
- **Nullabile**: No.
- **Relazioni**: `any` = il renderer applica l'escaping del canale d'invio effettivo (usato per i
  template report, condivisi email/WhatsApp).

### culture
- **Legacy**: `hotel_message_templates.lang` (`"it"`, default) · `custom_templates` non aveva una
  colonna lingua (righe multiple con titoli diversi)
- **Descrizione**: cultura del contenuto, formato BCP-47.
- **Tipo**: `varchar(10)`.
- **Default**: `'it'`.
- **Nullabile**: No.
- **Relazioni**: la selezione a runtime segue la catena `richiesta → default property → 'it'`.

### title
- **Legacy**: `custom_templates.title` / `hotel_message_templates.name`
- **Descrizione**: etichetta identificativa (tendina di selezione), **non** il contenuto.
- **Tipo**: `varchar(160)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: unicità applicativa su `(property_id, template_kind, channel, culture, title)` fra i
  non cancellati.

### subject
- **Legacy**: `custom_templates.subject` (`DEFAULT 'Oggetto da cambiare'`) / `hotel_message_templates.subject`
- **Descrizione**: oggetto del messaggio con segnaposto `{{ … }}` (rilevante per `email`/`push`).
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (obbligatorio in validazione per `channel IN ('email','push')`).
- **Relazioni**: —.

### body
- **Legacy**: `custom_templates.text` / `hotel_message_templates.text` / `marketing_automation.textFields`
- **Descrizione**: corpo del messaggio con segnaposto `{{ … }}` (mustache). Per `email` è HTML; per gli
  altri canali è testo. Nessun HTML pre-renderizzato dal client.
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: i segnaposto sono validati contro il vocabolario canonico alla `create`/`update`.

### placeholder_keys_used
- **Legacy**: — (nel legacy nessuna validazione)
- **Descrizione**: elenco (denormalizzato, ricavato dal parsing del `subject`+`body`) dei segnaposto
  canonici realmente usati — per l'editor ("questo template richiede: nome cliente, date, totale") e
  per il controllo di compatibilità con il `subject_type` d'invio.
- **Tipo**: `text[]` (valori dell'enum canonico dei segnaposto).
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: mantenuto in sync in applicazione ad ogni salvataggio.

### is_active
- **Legacy**: `custom_templates.status` (`1`/`0`)
- **Descrizione**: template selezionabile dal gestore.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: i lettori filtrano `is_active AND deleted_at IS NULL`.

### is_default
- **Legacy**: `custom_templates.isDefault` / `hotel_message_templates.isDefault`
- **Descrizione**: template predefinito per la combinazione `(property_id, template_kind, channel, culture)`.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE` parziale; promozione/demozione atomica (il legacy `custom_templates` non
  usava transazione, `hotel_message_templates` sì — si adotta quest'ultimo comportamento). Non si può
  togliere l'ultimo default se esistono template attivi (regola da `hotel_message_templates`).

### created_by_user_id
- **Legacy**: `custom_templates.createdBy` (`NULL` in prod) / `hotel_message_templates.createdBy`
- **Descrizione**: utente che ha creato il template.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5.

### created_at / updated_at / deleted_at
- **Legacy**: `createdAt` / `updatedAt` (Sequelize) / `status = 0` · `isDeleted`
- **Descrizione**: creazione / modifica / soft-delete (ex `status = 0` di `custom_templates`,
  `isDeleted` di `hotel_message_templates`). Retention: soft-deleted purgati dopo la finestra
  ([`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md)).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun `jsonb`. `custom_templates.attachment` (array URL) → giunzione
[`notification_template_attachments`](notification_template_attachments.md). `placeholder_keys_used` è
un `text[]` **derivato** (non un modello di dominio): insieme piccolo, sempre letto con la riga,
ricostruibile dal parsing del corpo.

## Suggerimenti per le prestazioni

- Volume minimo (~100 righe in prod, decine per property). L'indice parziale di lookup copre l'unico
  path d'accesso ("dammi il template `reservation_summary` / `email` / `it` di questa property").
- Il template compilato (Razor) va in **cache in memoria** per `template_id` + `updated_at` (il legacy
  ri-compilava l'EJS da disco ad ogni destinatario, [`docs/01 §8`](../../../progettazione/01-criticita-e-indicazioni-backend.md)).
- Aggiungere una lingua = `INSERT` di righe (`culture` esiste già), nessuna modifica di schema.

## Implementazione .NET

`MessageTemplate` è un'**entità principale** del modulo `Module.Notification` (schema `notification`):
non è owned da nessun'altra entità, ha vita propria (creazione/modifica dal gestore, soft-delete) ed è
referenziata da FK interne di altre tabelle dello stesso schema (`notification_deliveries.template_id`,
`notification_template_attachments.template_id`, e dalle schede marketing fuori da questo batch). Non
eredita `AggregateRoot`/non solleva eventi di dominio: le transizioni interessanti (invio) vivono su
`Delivery`.

### Enum propri

```csharp
// Domain/Enums/TemplateKind.cs
public enum TemplateKind
{
	ReservationSummary = 0, AgreementReport = 1, Campaign = 2, Automation = 3,
	Transactional = 4, Generic = 5
}

// Domain/Enums/TemplateChannel.cs
public enum TemplateChannel { Email = 0, Whatsapp = 1, Sms = 2, Push = 3, Any = 4 }
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<TemplateKind>("notification", "template_kind");
modelBuilder.HasPostgresEnum<TemplateChannel>("notification", "template_channel");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<TemplateKind>("notification.template_kind");
dataSourceBuilder.MapEnum<TemplateChannel>("notification.template_channel");
```

`PlaceholderKey` **non** è un `ENUM` Postgres (la colonna `placeholder_keys_used` è `text[]` semplice,
non un array di enum nativo — pattern B "vocabolario di codice", [D-48](../_domande-committente.md)): è
solo un `enum` C# con risorsa `.resx` per l'etichetta editor, usato in Application per validare
`subject`/`body` e per (de)serializzare la colonna:

```csharp
// Domain/Enums/PlaceholderKey.cs — vocabolario canonico dei segnaposto, NON un ENUM Postgres
public enum PlaceholderKey
{
	EstablishmentName = 0, CustomerFullName = 1, BookingDateFrom = 2, BookingDateTo = 3,
	BookingTotalAmount = 4, ReservationNumber = 5, PropertyContactPhone = 6,
	// … resto del vocabolario canonico, vedi PlaceholderKey.resx per le etichette editor
}
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class MessageTemplateConfiguration : IEntityTypeConfiguration<MessageTemplate>
{
	private static readonly ValueConverter<List<PlaceholderKey>, string[]> PlaceholderKeysConverter = new(
		keys => keys.Select(k => k.ToString()).ToArray(),
		values => values.Select(v => Enum.Parse<PlaceholderKey>(v)).ToList());

	private static readonly ValueComparer<List<PlaceholderKey>> PlaceholderKeysComparer = new(
		(a, b) => a!.SequenceEqual(b!),
		l => l.Aggregate(0, (h, k) => HashCode.Combine(h, k)),
		l => l.ToList());

	public void Configure(EntityTypeBuilder<MessageTemplate> builder)
	{
		builder.ToTable("templates", "notification", t => t.HasComment(
			"Catalogo unico dei template di messaggio: oggetto + corpo con segnaposto per uno scopo, un canale e una lingua."));

		builder.HasKey(x => x.Id).HasName("pk_notification_templates");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, usato in API/URL gestionale e riferito da notification_deliveries.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_notification_templates_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. NULL = template di piattaforma/seed di sistema.");

		builder.Property(x => x.TemplateKind)
			.HasColumnName("template_kind")
			.HasDefaultValue(TemplateKind.Generic)
			.IsRequired()
			.HasComment("Scopo del template (riepilogo prenotazione, report convenzione, campagna, automazione, transazionale, generico).");

		builder.Property(x => x.Channel)
			.HasColumnName("channel")
			.HasDefaultValue(TemplateChannel.Email)
			.IsRequired()
			.HasComment("Canale per cui è pensato il contenuto; 'any' = il renderer applica l'escaping del canale d'invio effettivo.");

		builder.Property(x => x.Culture)
			.HasColumnName("culture")
			.HasMaxLength(10)
			.HasDefaultValue("it")
			.IsRequired()
			.HasComment("Cultura del contenuto, formato BCP-47.");

		builder.Property(x => x.Title)
			.HasColumnName("title")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Etichetta identificativa mostrata nella tendina di selezione (non è il contenuto).");

		builder.Property(x => x.Subject)
			.HasColumnName("subject")
			.HasMaxLength(255)
			.HasComment("Oggetto del messaggio con segnaposto {{ … }}; obbligatorio in validazione per i canali email/push.");

		builder.Property(x => x.Body)
			.HasColumnName("body")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Corpo del messaggio con segnaposto {{ … }} mustache: HTML per email, testo per gli altri canali.");

		builder.Property(x => x.PlaceholderKeysUsed)
			.HasColumnName("placeholder_keys_used")
			.HasColumnType("text[]")
			.HasConversion(PlaceholderKeysConverter)
			.Metadata.SetValueComparer(PlaceholderKeysComparer);
		builder.Property(x => x.PlaceholderKeysUsed)
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Segnaposto canonici realmente usati in subject+body, derivati dal parsing e mantenuti in sync ad ogni salvataggio.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Template selezionabile dal gestore; i lettori filtrano is_active AND deleted_at IS NULL.");

		builder.Property(x => x.IsDefault)
			.HasColumnName("is_default")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Template predefinito per la combinazione (property_id, template_kind, channel, culture).");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione del template.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete: ex status=0 di custom_templates / isDeleted di hotel_message_templates.");

		builder.HasIndex(x => new { x.PropertyId, x.TemplateKind, x.Channel, x.Culture })
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_templates_lookup");

		builder.HasIndex(x => new { x.PropertyId, x.TemplateKind, x.Channel, x.Culture })
			.IsUnique()
			.HasFilter("is_default AND deleted_at IS NULL")
			.HasDatabaseName("uq_notification_templates_default");

		// query filter soft-delete — applicato una volta nel DbContext, non qui
	}
}
```

`MessageTemplate` implementa `ISoftDeletable` (`deleted_at`, con Global Query Filter) e `IAuditable`
(`created_at`/`updated_at`).

### Note di migrazione

- `custom_templates.type` `1`→`Email`, `2`→`Whatsapp`; `hotel_message_templates` → sempre
  `channel = Any`, `template_kind = AgreementReport`.
- `marketing_automation.textFields` / `marketing_email_campaign.message` → righe con
  `template_kind = Automation` / `Campaign` (import gestito insieme alle schede marketing fuori batch).
- Conversione segnaposto: `@token` (×3 motori) e `{token}` → `{{ canonical }}` tramite mappa statica
  verso `PlaceholderKey` ([`docs/03 §2.7`](../../../progettazione/03-gestione-dati.md)); un token fuori mappa
  viene rimosso e loggato per revisione manuale post-migrazione (mai bloccante in batch).
- `subject = 'Oggetto da cambiare'` (default legacy mai sostituito) → importato come `NULL`, non come
  stringa letterale.
- `status = 0` → `deleted_at = updatedAt` (best-effort); `isDeleted` di `hotel_message_templates`
  idem.
- `createdBy` quasi sempre `NULL` in `custom_templates` → `created_by_user_id = NULL`, nessuna euristica
  di attribuzione in migrazione.
- `placeholder_keys_used` **non** importato dal legacy (non esisteva): calcolato al primo salvataggio
  post-migrazione via parsing del `body` convertito.
