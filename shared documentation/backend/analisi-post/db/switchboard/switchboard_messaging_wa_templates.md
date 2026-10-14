# Tabella `switchboard_messaging_wa_templates`

> **Dominio**: C7 — `switchboard` · **sottoarea**: `messaging` · **schema**: `switchboard` · **nome
> flat**: `switchboard_messaging_wa_templates` · **forma canonica**: `switchboard.messaging_wa_templates`.
>
> **Redesign** di [`switchboard_wa_template`](../../../analisi-pre/db/switchboard_wa_template.md)
> (0 righe in `local_sb` e in produzione — ma **verificato 2026-09-11**: 3 righe reali, tutte
> `category=UTILITY`/`meta_status=APPROVED`, su `summer_booking_dev`, a conferma che il formato
> `components` ipotizzato è accurato). Cache/audit locale dei **template WhatsApp Business registrati su
> Meta** dallo stabilimento, usati dall'azione IVR `send_whatsapp`
> ([`switchboard_voice_menu_actions`](switchboard_voice_menu_actions.md)) — **nessuno dei 3
> template reali risulta però ancora agganciato** a un'azione IVR in quell'ambiente.
>
> **Distinta da C6 `notification_templates`**: quelli sono i template del modulo Notification (email /
> WhatsApp transazionali e marketing, sintassi `{{ mustache }}`, renderer Razor). Questi sono
> **definizioni Meta** (formato `components`, ciclo di vita `PENDING`/`APPROVED` gestito da Meta) del
> centralino — [`docs/03 §3.1bis`](../../../progettazione/03-gestione-dati.md) tiene il centralino
> Telnyx/WhatsApp Business in C7.
>
> **Fonti**: [`analisi-pre/db/switchboard_wa_template.md`](../../../analisi-pre/db/switchboard_wa_template.md),
> [`docs/03-gestione-dati.md` §2.4quinquies / §2.7 / §3.6 punto 5](../../../progettazione/03-gestione-dati.md),
> [`docs/12-lingue-e-date.md`](../../../progettazione/12-lingue-e-date.md),
> [`docs/80-retention.md` §3.7](../../../progettazione/80-retention.md).

---

## Descrizione

**Una riga = un template WhatsApp** creato su Meta per uno stabilimento. Il flusso legacy: crea prima
su Meta (`POST /{WABA}/message_templates`), poi salva la riga con `meta_template_id` e
`meta_status = PENDING`; `refreshWaTemplateStatus` rilegge lo stato da Meta. **La tabella non governa
l'invio**: `send_whatsapp` (IVR) e l'endpoint API accettano un `templateName` libero — la sorgente di
verità è Meta.

Correzioni rispetto al legacy:

- **`meta_status ENUM('PENDING','APPROVED','REJECTED')`** troppo stretto (Meta espone anche `PAUSED`,
  `DISABLED`, `IN_APPEAL`, `PENDING_DELETION`: `refreshWaTemplateStatus` scriveva `toUpperCase()`
  grezzo → errore/troncamento) → **`meta_status` enum passthrough esteso** + `meta_status_raw` grezzo.
- **Nessun vincolo di unicità** `(property_id, template_name, language)` benché Meta lo imponga sul
  WABA → `UNIQUE` parziale.
- **`components json` non validato** → `jsonb` (resta blob — vedi §"Valutazione JSON") + **validazione
  al write** (forma base Meta + vocabolario chiuso dei segnaposto, [`docs/03 §2.7`](../../../progettazione/03-gestione-dati.md))
  + **colonne di riepilogo derivate** per la lista del pannello senza parsare il blob.
- **Disallineamento su `deleteConfig`** (cancellava le righe ma non i template su Meta) → `deleted_at`
  + reconcile job; la cancellazione dal pannello elimina anche su Meta (come `deleteWaTemplate`).
- **`category` / `meta_status`** già `ENUM` MySQL → `enum` PG.

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `category` | **enum esclusivo** — `marketing` / `utility` / `authentication` (vocabolario chiuso Meta; impatta pricing e regole d'invio). |
| `meta_status` | **enum esclusivo** — un solo stato di approvazione alla volta (passthrough Meta, non limitato a 3). |
| `header_format` (derivata) | **enum esclusivo** — un header ha un solo formato (`none` / `text` / `image` / `video` / `document`). |
| `has_buttons` (derivata) | booleano; nessun gruppo di flag correlati → nessun `enum[]`. |
| `language` | **non** enum: codice "di libreria" stile BCP-47 (come `country`/`timezone`). |

## Snapshot

`components` è di fatto uno **snapshot della definizione template così come vive su Meta** (immutabile
lato nostro: esistono solo create / refresh-status / delete, nessun update). **Nessun suffisso
`_snapshot`** sul nome della colonna: non è un'etichetta copiata da un'entità mutabile con FK viva
accanto (criterio C1-7), ma il documento stesso di un sistema esterno. La sua natura di copia
cache/audit è in descrizione.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_switchboard_messaging_wa_templates` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | id pubblico (API `/wa-templates/:propertyId/:templateId`) |
| `uq_..._name_lang` | `(property_id, lower(template_name), language) WHERE deleted_at IS NULL` | UNIQUE parziale | unicità imposta da Meta sul WABA (mancava) ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): indice su espressione, scritto a mano nella migration (da riportare in un eventuale nuovo consolidamento). |
| `ix_..._config` | `(config_id) WHERE deleted_at IS NULL` | parziale | elenco template di uno stabilimento (`listWaTemplates`) |
| `ix_..._meta_id` | `(meta_template_id) WHERE meta_template_id IS NOT NULL` | parziale | refresh stato da Meta |
| `ix_..._status` | `(property_id, meta_status)` | — | filtro "solo approvati" nel pannello |

## Campi

### id
- **Legacy**: `switchboard_wa_template.id`
- **Descrizione**: PK tecnica interna allo schema.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da `wa_template_id` di
  [`switchboard_voice_menu_actions`](switchboard_voice_menu_actions.md).

### public_id
- **Legacy**: — (il legacy usava l'`id` numerico nelle rotte)
- **Descrizione**: id pubblico stabile per l'API del pannello.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato applicativamente.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### config_id
- **Legacy**: `switchboard_wa_template.property_id` (FK `_ibfk_1` → `properties.id`)
- **Descrizione**: centralino proprietario del template.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`switchboard_configs`](switchboard_configs.md)`.id`,
  `ON DELETE CASCADE` (il legacy cancellava in `deleteConfig`, solo lato DB).

### property_id
- **Legacy**: `switchboard_wa_template.property_id`
- **Descrizione**: stabilimento (denormalizzato per scoping multi-tenant e unicità `(property_id, name, language)`).
- **Tipo**: `uuid` (= `public_id` di [`catalog_properties`](../catalog/catalog_properties.md)).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: riferimento cross-modulo per valore.

### template_name
- **Legacy**: `switchboard_wa_template.template_name` (`varchar(512) NOT NULL`)
- **Descrizione**: nome del template su Meta (minuscolo con underscore, unico per WABA). È il valore
  passato come `template.name` all'invio e alla cancellazione su Meta.
- **Tipo**: `varchar(512)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: parte dell'`UNIQUE` parziale; copiato in `wa_template_name_snapshot` di
  [`switchboard_voice_menu_actions`](switchboard_voice_menu_actions.md).

### language
- **Legacy**: `switchboard_wa_template.language` (`varchar(10) NOT NULL DEFAULT 'it'`)
- **Descrizione**: codice lingua del template nel formato Meta (`language.code`). Codice "di libreria",
  non enum di dominio.
- **Tipo**: `varchar(10)`.
- **Default**: `'it'`.
- **Nullabile**: No.
- **Relazioni**: parte dell'`UNIQUE` parziale.

### category
- **Legacy**: `switchboard_wa_template.category` (`varchar(30) NOT NULL DEFAULT 'UTILITY'`)
- **Descrizione**: categoria Meta del template (impatta pricing e regole d'invio).
- **Tipo**: `enum switchboard.wa_template_category` — valori: `marketing`, `utility`, `authentication`.
- **Default**: `'utility'`.
- **Nullabile**: No.
- **Relazioni**: —.

### components
- **Legacy**: `switchboard_wa_template.components` (`json NOT NULL`)
- **Descrizione**: array dei *components* del template nel formato Meta Cloud API
  (`HEADER`/`BODY`/`FOOTER`/`BUTTONS`, con `format`, `text`, `example`, `buttons[]`). Salvato verbatim,
  inviato verbatim a Meta, riletto verbatim. **Mai interrogato per sotto-campo.**
- **Tipo**: `jsonb`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: validazione al write (forma Meta + segnaposto nel vocabolario canonico chiuso —
  [`docs/03 §2.7`](../../../progettazione/03-gestione-dati.md)); **non** decomposto in tabelle (vedi
  §"Valutazione JSON").

### meta_status
- **Legacy**: `switchboard_wa_template.meta_status` (`enum('PENDING','APPROVED','REJECTED') NOT NULL DEFAULT 'PENDING'`)
- **Descrizione**: stato di approvazione lato Meta. Inizializzato a `pending`; aggiornato da
  `refreshWaTemplateStatus`.
- **Tipo**: `enum switchboard.wa_template_status` — valori: `pending`, `approved`, `rejected`,
  `paused`, `disabled`, `in_appeal`, `pending_deletion` (passthrough Meta, esteso oltre i 3 legacy).
- **Default**: `'pending'`.
- **Nullabile**: No.
- **Relazioni**: —.

### meta_status_raw
- **Legacy**: — (il legacy scriveva `toUpperCase()` grezzo nell'ENUM, con rischio troncamento)
- **Descrizione**: stringa di stato **così come restituita da Meta**, per gestire valori nuovi non
  ancora mappati nell'enum.
- **Tipo**: `varchar(40)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### meta_template_id
- **Legacy**: `switchboard_wa_template.meta_template_id` (`varchar(50)`)
- **Descrizione**: id assegnato da Meta alla creazione; usato per interrogare `/{id}`. Identificatore
  esterno opaco.
- **Tipo**: `varchar(64)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### meta_rejection_reason
- **Legacy**: `switchboard_wa_template.meta_rejection_reason` (`text`)
- **Descrizione**: motivo di rifiuto restituito da Meta (`rejected_reason`), popolato da
  `refreshWaTemplateStatus`.
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### header_format / has_buttons / buttons_count / body_param_count / body_preview
- **Legacy**: — (nuove, **derivate** da `components` alla scrittura)
- **Descrizione**: colonne di riepilogo **read-only** per la lista del pannello senza parsare il blob a
  ogni render. `header_format` enum (`none`/`text`/`image`/`video`/`document`); `has_buttons` bool;
  `buttons_count` / `body_param_count` int; `body_preview` = primi ~120 char del testo `BODY`. **Non
  autorevoli**: la verità resta `components` + Meta.
- **Tipo**: `enum switchboard.wa_header_format` / `boolean` / `smallint` / `smallint` / `varchar(160)`.
- **Default**: derivati.
- **Nullabile**: `header_format` No (`none`); gli altri No / No / Sì.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: `switchboard_wa_template` — `timestamps: false`
- **Descrizione**: creazione / ultimo refresh di stato / soft-delete. Un reconcile job allinea la
  cancellazione su Meta (il legacy `deleteConfig` disallineava). Retention: vivono con lo stabilimento;
  🗑️ alla cessazione ([`docs/80 §3.7`](../../../progettazione/80-retention.md)).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

**`components` resta `jsonb`** — non si spezza in tabelle plain
([`docs/03 §3.6 punto 5`](../../../progettazione/03-gestione-dati.md); la scheda legacy lo argomenta in
dettaglio):

1. Lo schema è **di Meta**, versionato ed evolutivo (carousel, OTP, limited-time offer, MPM…):
   normalizzare = trasformare ogni modifica dell'API Meta in una migrazione.
2. È **passthrough puro, scrittura una-tantum**: ricevuto dal body, inoltrato verbatim, salvato
   verbatim, riletto verbatim. Nessun campo interno viene mai letto/trasformato.
3. La **fonte di verità è Meta**; la riga è copia cache/audit.
4. **Nessun accesso relazionale** (mai "tutti i template con bottone URL").

Cosa si fa **invece** del blob grezzo: tipo `jsonb` (non `json`/`text`), **validazione al write**
(forma Meta + vocabolario chiuso dei segnaposto), e le **colonne di riepilogo derivate** sopra. →
[D-52](../_domande-committente.md).

## Suggerimenti per le prestazioni

- Poche righe per stabilimento. La lista del pannello usa le colonne di riepilogo, non il parsing di
  `components`.
- `refreshWaTemplateStatus` è un job (o on-demand) che tocca `meta_status` / `meta_status_raw` /
  `meta_rejection_reason` — nessun impatto sul percorso IVR.
- L'invio (`send_whatsapp`) **non** legge questa tabella a runtime (passa `template_name` + `language`
  a Meta); la FK `wa_template_id` sull'azione serve solo a integrità/tracciabilità in configurazione.

## Implementazione .NET

`MessagingWaTemplate` è un'**entità figlia** del modulo `Module.Switchboard`, area `Messaging`: cache/
audit locale dei template WhatsApp Business registrati su Meta per uno stabilimento, referenziata dalla
FK `wa_template_id` di
[`switchboard_voice_menu_actions`](switchboard_voice_menu_actions.md) (cross-area, stesso
schema — FK reale). 0 righe su `local_sb`/dump di produzione, ma **3 righe reali**
(`category=utility`/`meta_status=approved`) su `summer_booking_dev` confermano il formato `components`
ipotizzato; nessuna delle tre risulta ancora agganciata a un'azione IVR in quell'ambiente.

### Enum

```csharp
// Module.Switchboard.Domain.Enums.Messaging/WaTemplateCategory.cs
public enum WaTemplateCategory
{
	Marketing = 0,
	Utility = 1,
	Authentication = 2
}

// Module.Switchboard.Domain.Enums.Messaging/WaTemplateStatus.cs
public enum WaTemplateStatus
{
	Pending = 0,
	Approved = 1,
	Rejected = 2,
	Paused = 3,
	Disabled = 4,
	InAppeal = 5,
	PendingDeletion = 6
}

// Module.Switchboard.Domain.Enums.Messaging/WaHeaderFormat.cs
public enum WaHeaderFormat
{
	None = 0,
	Text = 1,
	Image = 2,
	Video = 3,
	Document = 4
}
```

```csharp
// SwitchboardDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<WaTemplateCategory>("switchboard", "wa_template_category");
modelBuilder.HasPostgresEnum<WaTemplateStatus>("switchboard", "wa_template_status");
modelBuilder.HasPostgresEnum<WaHeaderFormat>("switchboard", "wa_header_format");
```

```csharp
// Module.Switchboard.Infrastructure DI
dataSourceBuilder.MapEnum<WaTemplateCategory>("switchboard.wa_template_category");
dataSourceBuilder.MapEnum<WaTemplateStatus>("switchboard.wa_template_status");
dataSourceBuilder.MapEnum<WaHeaderFormat>("switchboard.wa_header_format");
```

### Configuration

```csharp
namespace Module.Switchboard.Infrastructure.Configurations.Messaging;

public sealed class MessagingWaTemplateConfiguration : IEntityTypeConfiguration<MessagingWaTemplate>
{
	public void Configure(EntityTypeBuilder<MessagingWaTemplate> builder)
	{
		builder.ToTable("switchboard_messaging_wa_templates", "switchboard", t => t.HasComment(
			"Cache/audit locale di un template WhatsApp Business registrato su Meta per uno stabilimento; non governa l'invio, la fonte di verità resta Meta."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_messaging_wa_templates");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile per l'API del pannello.");

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.IsRequired()
			.HasComment("Centralino proprietario del template.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping e unicità (property_id, name, language).");

		builder.Property(x => x.TemplateName)
			.HasColumnName("template_name")
			.HasMaxLength(512)
			.IsRequired()
			.HasComment("Nome del template su Meta (minuscolo con underscore, unico per WABA); valore passato a template.name all'invio/cancellazione.");

		builder.Property(x => x.Language)
			.HasColumnName("language")
			.HasMaxLength(10)
			.IsRequired()
			.HasDefaultValue("it")
			.HasComment("Codice lingua del template nel formato Meta (language.code); codice di libreria, non enum di dominio.");

		builder.Property(x => x.Category)
			.HasColumnName("category")
			.HasColumnType("switchboard.wa_template_category")
			.IsRequired()
			.HasDefaultValue(WaTemplateCategory.Utility)
			.HasComment("Categoria Meta del template, impatta pricing e regole d'invio.");

		builder.Property(x => x.Components)
			.HasColumnName("components")
			.HasColumnType("jsonb")
			.IsRequired()
			.HasComment("Array dei components del template nel formato Meta Cloud API, salvato e inviato verbatim, mai interrogato per sotto-campo.");

		builder.Property(x => x.MetaStatus)
			.HasColumnName("meta_status")
			.HasColumnType("switchboard.wa_template_status")
			.IsRequired()
			.HasDefaultValue(WaTemplateStatus.Pending)
			.HasComment("Stato di approvazione lato Meta, aggiornato dal refresh periodico.");

		builder.Property(x => x.MetaStatusRaw)
			.HasColumnName("meta_status_raw")
			.HasMaxLength(40)
			.HasComment("Stringa di stato così come restituita da Meta, per gestire valori nuovi non ancora mappati nell'enum.");

		builder.Property(x => x.MetaTemplateId)
			.HasColumnName("meta_template_id")
			.HasMaxLength(64)
			.HasComment("Id assegnato da Meta alla creazione, usato per interrogare lo stato.");

		builder.Property(x => x.MetaRejectionReason)
			.HasColumnName("meta_rejection_reason")
			.HasColumnType("text")
			.HasComment("Motivo di rifiuto restituito da Meta, popolato dal refresh di stato.");

		builder.Property(x => x.HeaderFormat)
			.HasColumnName("header_format")
			.HasColumnType("switchboard.wa_header_format")
			.IsRequired()
			.HasDefaultValue(WaHeaderFormat.None)
			.HasComment("Formato dell'header del template, derivato da components alla scrittura — colonna di riepilogo non autorevole.");

		builder.Property(x => x.HasButtons)
			.HasColumnName("has_buttons")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se il template ha bottoni, derivato da components alla scrittura — colonna di riepilogo non autorevole.");

		builder.Property(x => x.ButtonsCount)
			.HasColumnName("buttons_count")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Numero di bottoni del template, derivato da components alla scrittura.");

		builder.Property(x => x.BodyParamCount)
			.HasColumnName("body_param_count")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Numero di segnaposto nel corpo del template, derivato da components alla scrittura.");

		builder.Property(x => x.BodyPreview)
			.HasColumnName("body_preview")
			.HasMaxLength(160)
			.HasComment("Primi ~120 caratteri del testo BODY, derivato da components per la lista del pannello senza parsare il blob.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_switchboard_messaging_wa_templates_public_id");

		builder.HasIndex(x => new { x.PropertyId, x.TemplateName, x.Language })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_switchboard_messaging_wa_templates_name_lang");

		builder.HasIndex(x => x.ConfigId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_switchboard_messaging_wa_templates_config");

		builder.HasIndex(x => x.MetaTemplateId)
			.HasFilter("meta_template_id IS NOT NULL")
			.HasDatabaseName("ix_switchboard_messaging_wa_templates_meta_id");

		builder.HasIndex(x => new { x.PropertyId, x.MetaStatus })
			.HasDatabaseName("ix_switchboard_messaging_wa_templates_status");

		builder.HasOne(x => x.Config)
			.WithMany(c => c.WaTemplates)
			.HasForeignKey(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_messaging_wa_templates_config")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

`MessagingWaTemplate` implementa `ISoftDeletable`/`IAuditable`. L'indice
`uq_switchboard_messaging_wa_templates_name_lang` usa `lower(template_name)` in scheda (unicità
case-insensitive imposta da Meta sul WABA): in EF va espresso come indice su espressione SQL
(`.HasMethod(...)`/`HasIndex` con `HasDatabaseName` e un `IndexAttribute`/migration override, oppure una
colonna calcolata `template_name_normalized` mantenuta dall'entità) — non esiste un mapping diretto
`HasIndex(x => x.TemplateName)` con funzione applicata; da rifinire in migration.

### Note di migrazione

- `IWhatsAppTemplateProvider` (Infrastructure): `CreateTemplate`/`GetTemplate`/`DeleteTemplate` su Meta
  Cloud API. Validatore di `components` (forma Meta + `IPlaceholderVocabulary` per il vocabolario chiuso
  dei segnaposto, [`docs/03 §2.7`](../../../progettazione/03-gestione-dati.md)) calcola anche le colonne di
  riepilogo (`header_format`, `has_buttons`, `buttons_count`, `body_param_count`, `body_preview`) alla
  scrittura.
- Reconcile job: allinea `deleted_at` con la cancellazione su Meta (il legacy `deleteConfig`
  disallineava, cancellando solo lato DB); refresh periodico di `meta_status`/`meta_status_raw`/
  `meta_rejection_reason` via `refreshWaTemplateStatus`.
- 0 righe legacy → nessun backfill da quelle copie; **3 righe reali** su `summer_booking_dev` migrabili
  1:1 se quell'ambiente va portato al nuovo schema, incluso il ricalcolo delle colonne di riepilogo dal
  `components` esistente.
