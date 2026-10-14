# Tabella `switchboard_voice_menu_actions`

> **Dominio**: C7 — `switchboard` · **sottoarea**: `voice` · **schema**: `switchboard` · **nome flat**:
> `switchboard_voice_menu_actions` · **forma canonica**: `switchboard.voice_menu_actions`.
>
> **Redesign** di [`switchboard_menu_action`](../../../analisi-pre/db/switchboard_menu_action.md)
> (0 righe in `local_sb` e in produzione — ma **verificato 2026-09-11**: 1 riga reale su
> `summer_booking_dev`, `action_type=forward_sip` con `action_config={}` **vuoto**, che conferma
> **esattamente** l'analisi "contratto morto" sotto; nessuna riga reale `send_whatsapp` osservata in
> nessun ambiente). Azione eseguita quando il chiamante seleziona una
> [voce di menù](switchboard_voice_menu_items.md).
>
> **Fonti**: [`analisi-pre/db/switchboard_menu_action.md`](../../../analisi-pre/db/switchboard_menu_action.md),
> [`analisi-pre/db/switchboard_config.md`](../../../analisi-pre/db/switchboard_config.md)
> (il `forward_sip` legge i parametri di forward dalla **config**, non da `action_config`),
> [`docs/03-gestione-dati.md` §2.4quinquies / §2.5 (no JSON di dominio) / §3.6 punto 5](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga = un'azione** collegata a una voce di menù. Una voce può avere più azioni, eseguite in
sequenza per `execution_order`; un'azione **terminale** (`hangup`, `forward_sip`) interrompe il ciclo,
`send_whatsapp` no.

Correzioni rispetto al legacy:

- **`action_config json`** (blob polimorfo per `action_type`) → **de-jsonizzato in colonne tipizzate**
  ([`docs/03 §2.5`](../../../progettazione/03-gestione-dati.md)):
  - per `hangup` / `forward_sip`: nel legacy `action_config` era **contratto morto**
    (`forward_sip` legge tutto da [`switchboard_configs`](switchboard_configs.md):
    `sip_username`/`sip_domain`/`forward_caller_id_mode`/…). Nessuna colonna nuova: l'azione è solo il
    `action_type`.
  - per `send_whatsapp`: `templateName` / `language` / `recipientPhone` / `bodyParameters` /
    `headerParameters` → colonne `wa_template_id` (FK), `wa_template_name_snapshot`, `wa_language`,
    `wa_recipient_mode`, `wa_recipient_phone_e164`, `wa_body_params` (jsonb — vedi §"Valutazione JSON").
- **`action_type`** era già `ENUM` nativo MySQL con whitelist **duplicata** nel controller → `enum` PG,
  vocabolario unico.
- **`send_whatsapp` senza `templateName` → skip silenzioso** → `CHECK` che impone `wa_template_id`
  quando `action_type = 'send_whatsapp'`.
- **Nessun `button_parameters`** nel path IVR (l'endpoint API li supportava) → `wa_button_params` jsonb
  opzionale, per parità.

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `action_type` | **enum esclusivo** — una sola azione per riga; ogni valore ha un ramo di integrazione dedicato (`hangup` → Telnyx hangup; `forward_sip` → inoltro SIP; `send_whatsapp` → template Meta). |
| `is_terminal` | **derivato** da `action_type` (`hangup`/`forward_sip` = `true`, `send_whatsapp` = `false`): **non** una colonna scrivibile, eventualmente colonna generata. |
| `wa_recipient_mode` | **enum esclusivo** — `caller` (numero del chiamante) **o** `fixed` (`wa_recipient_phone_e164`), mai entrambi. |
| — | nessun gruppo di flag booleani → nessun `enum[]`. |

## Snapshot

`wa_template_name_snapshot`: nome del template **così come registrato su Meta**, congelato accanto alla
FK viva `wa_template_id`. È il valore effettivamente inviato a Meta (`template.name`); si conserva
perché il template potrebbe essere cancellato o rinominato lato Meta e l'azione deve restare
tracciabile (criterio C1-7 / C3-8: `_snapshot` sulle etichette copiate da un'entità mutabile con FK
viva accanto).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_switchboard_voice_menu_actions` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | id pubblico (API pannello `/action/:actionId`) |
| `ix_..._item_order` | `(menu_item_id, execution_order) WHERE deleted_at IS NULL` | parziale | esecuzione ordinata delle azioni di una voce (`processDtmfInput`, `getMenuTree`) |
| `ix_..._wa_template` | `(wa_template_id) WHERE wa_template_id IS NOT NULL` | parziale | integrità / impatto della cancellazione di un template |

## Campi

### id
- **Legacy**: `switchboard_menu_action.id`
- **Descrizione**: PK tecnica interna allo schema.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### public_id
- **Legacy**: — (il legacy usava l'`id` numerico nelle rotte `/action/:actionId`)
- **Descrizione**: id pubblico stabile per l'API del pannello.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato applicativamente.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### menu_item_id
- **Legacy**: `switchboard_menu_action.menu_item_id` (`int NOT NULL`, FK `_ibfk_1`)
- **Descrizione**: voce di menù a cui è collegata l'azione.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`switchboard_voice_menu_items`](switchboard_voice_menu_items.md)`.id`,
  `ON DELETE CASCADE`.

### property_id
- **Legacy**: — (ricavabile via `menu_item → config → property`)
- **Descrizione**: stabilimento (denormalizzato per scoping multi-tenant senza join).
- **Tipo**: `uuid` (= `public_id` di [`catalog_properties`](../catalog/catalog_properties.md)).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: riferimento cross-modulo per valore.

### action_type
- **Legacy**: `switchboard_menu_action.action_type` (`enum('hangup','forward_sip','send_whatsapp') NOT NULL`)
- **Descrizione**: tipo di azione. `hangup` → chiude (terminale); `forward_sip` → inoltra al SIP
  endpoint dello stabilimento con i parametri di forward della **config** (terminale); `send_whatsapp`
  → invia un template WhatsApp (non terminale).
- **Tipo**: `enum switchboard.menu_action_type` — valori: `hangup`, `forward_sip`, `send_whatsapp`
  (commento del model: "extensible").
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### execution_order
- **Legacy**: `switchboard_menu_action.execution_order` (`int NOT NULL DEFAULT 0`)
- **Descrizione**: ordine di esecuzione delle azioni della stessa voce (`ASC`).
- **Tipo**: `integer`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### wa_template_id
- **Legacy**: `switchboard_menu_action.action_config.templateName` (stringa libera, non validata)
- **Descrizione**: template WhatsApp da inviare (solo per `action_type = 'send_whatsapp'`). FK viva:
  se il template esiste ancora, l'azione ne segue lingua/categoria.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (obbligatorio se `send_whatsapp` — `CHECK (action_type <> 'send_whatsapp' OR wa_template_id IS NOT NULL)`).
- **Relazioni**: **FK** → [`switchboard_messaging_wa_templates`](switchboard_messaging_wa_templates.md)`.id`,
  `ON DELETE RESTRICT` (non si cancella un template referenziato da un'azione).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 1 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### wa_template_name_snapshot
- **Legacy**: `action_config.templateName`
- **Descrizione**: nome del template su Meta congelato all'associazione (vedi §"Snapshot").
- **Tipo**: `varchar(512)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (solo `send_whatsapp`).
- **Relazioni**: affiancato alla FK `wa_template_id`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 1 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### wa_language
- **Legacy**: `action_config.language` (default `"it"`)
- **Descrizione**: codice lingua del template nel formato Meta. **Non** enum di dominio: codice "di
  libreria" come `country`/`timezone` ([`docs/03 §2.4`](../../../progettazione/03-gestione-dati.md),
  [`docs/12-lingue-e-date.md`](../../../progettazione/12-lingue-e-date.md)).
- **Tipo**: `varchar(10)`.
- **Default**: `'it'`.
- **Nullabile**: Sì (solo `send_whatsapp`).
- **Relazioni**: dovrebbe combaciare con `wa_template.language` (validato in applicazione).

### wa_recipient_mode
- **Legacy**: `action_config.recipientPhone` presente/assente (default = numero del chiamante)
- **Descrizione**: a chi inviare il template. `caller` = numero PSTN del chiamante corrente; `fixed` =
  `wa_recipient_phone_e164`.
- **Tipo**: `enum switchboard.wa_recipient_mode` — valori: `caller`, `fixed`.
- **Default**: `'caller'`.
- **Nullabile**: Sì (solo `send_whatsapp`).
- **Relazioni**: `CHECK (wa_recipient_mode <> 'fixed' OR wa_recipient_phone_e164 IS NOT NULL)`.

### wa_recipient_phone_e164
- **Legacy**: `action_config.recipientPhone`
- **Descrizione**: destinatario fisso (E.164) quando `wa_recipient_mode = 'fixed'`.
- **Tipo**: `varchar(20)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 1 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### wa_body_params
- **Legacy**: `action_config.bodyParameters` (array, default `[]`)
- **Descrizione**: parametri posizionali del corpo del template (`{{1}}`, `{{2}}`, …) da passare a
  Meta all'invio. Ogni elemento: `{ type: "text", text: "..." }` (nel path IVR il legacy supporta solo
  `text`). Piccolo array **snapshot** di argomenti dell'API di terzi — vedi §"Valutazione JSON".
- **Tipo**: `jsonb`.
- **Default**: `'[]'::jsonb`.
- **Nullabile**: Sì (solo `send_whatsapp`).
- **Relazioni**: —.

### wa_header_params / wa_button_params
- **Legacy**: `action_config.headerParameters` (array, default `[]`); `button_parameters` **assente**
  nel path IVR legacy
- **Descrizione**: parametri di header e di bottone del template (stessa natura di `wa_body_params`).
  `wa_button_params` è nuovo, per parità con l'endpoint API `sendWaTemplateMessage`.
- **Tipo**: `jsonb` ciascuno.
- **Default**: `'[]'::jsonb`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: `switchboard_menu_action` — `timestamps: false`
- **Descrizione**: creazione / modifica / soft-delete (azione rimossa; cancellata a cascata con la
  voce o la config).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

- **`action_config` intero → eliminato**: scomposto in colonne tipizzate (`hangup`/`forward_sip` non
  ne avevano bisogno — contratto morto; `send_whatsapp` diventa 6 colonne + i 3 array di parametri).
- **`wa_body_params` / `wa_header_params` / `wa_button_params` restano `jsonb`**: sono **liste di
  argomenti dell'API Meta Cloud** (formato `components[].parameters` di `POST /messages`), passthrough
  puro, mai interrogate per sotto-campo, forma definita e versionata da Meta
  ([`docs/03 §3.6 punto 5`](../../../progettazione/03-gestione-dati.md), stesso criterio di
  [`switchboard_messaging_wa_templates`](switchboard_messaging_wa_templates.md)`.components`
  e analogo all'eccezione read-model [D-49]). Alternativa (child table ordinata per posizione) =
  over-engineering per parametri che sono quasi sempre 0–3 valori statici. → [D-52](../_domande-committente.md).

## Suggerimenti per le prestazioni

- Poche righe per centralino. L'esecuzione runtime carica le azioni di **una** voce ordinate per
  `execution_order` (`ix_..._item_order`), incluse nella proiezione ad albero cache-ata di
  `getMenuTree`.
- `send_whatsapp` è **non terminale** e fire-and-forget verso Meta via outbox/worker
  ([`docs/20-jobs.md`](../../../progettazione/20-jobs.md)): non blocca il flusso vocale.

## Implementazione .NET

`VoiceMenuAction` è un'**entità figlia** del modulo `Module.Switchboard`, area `Voice`: azione eseguita
alla selezione di una [voce di menù](switchboard_voice_menu_items.md), in sequenza per
`execution_order`. `IsTerminal` è **derivato** da `action_type` (non colonna scrivibile): esposto come
proprietà calcolata sull'entità (`ActionType is MenuActionType.Hangup or MenuActionType.ForwardSip`),
non come `HasComputedColumnSql` — evita di dover tenere un'espressione SQL sincronizzata con l'enum. 0
righe su `local_sb`/dump di produzione, ma **1 riga reale** su `summer_booking_dev`
(`action_type = forward_sip`, `action_config = {}` vuoto) conferma esattamente l'analisi di "contratto
morto" sotto.

### Enum

```csharp
// Module.Switchboard.Domain.Enums.Voice/MenuActionType.cs
public enum MenuActionType
{
	Hangup = 0,
	ForwardSip = 1,
	SendWhatsapp = 2
}

// Module.Switchboard.Domain.Enums.Voice/WaRecipientMode.cs
public enum WaRecipientMode
{
	Caller = 0,
	Fixed = 1
}
```

```csharp
// SwitchboardDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<MenuActionType>("switchboard", "menu_action_type");
modelBuilder.HasPostgresEnum<WaRecipientMode>("switchboard", "wa_recipient_mode");
```

```csharp
// Module.Switchboard.Infrastructure DI
dataSourceBuilder.MapEnum<MenuActionType>("switchboard.menu_action_type");
dataSourceBuilder.MapEnum<WaRecipientMode>("switchboard.wa_recipient_mode");
```

### Configuration

```csharp
namespace Module.Switchboard.Infrastructure.Configurations.Voice;

public sealed class VoiceMenuActionConfiguration : IEntityTypeConfiguration<VoiceMenuAction>
{
	public void Configure(EntityTypeBuilder<VoiceMenuAction> builder)
	{
		builder.ToTable("switchboard_voice_menu_actions", "switchboard", t => t.HasComment(
			"Azione eseguita quando il chiamante seleziona una voce di menù dell'IVR, eseguita in sequenza per execution_order."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_voice_menu_actions");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile per l'API del pannello.");

		builder.Property(x => x.MenuItemId)
			.HasColumnName("menu_item_id")
			.IsRequired()
			.HasComment("Voce di menù a cui è collegata l'azione.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping multi-tenant senza join.");

		builder.Property(x => x.ActionType)
			.HasColumnName("action_type")
			.HasColumnType("switchboard.menu_action_type")
			.IsRequired()
			.HasComment("Tipo di azione: hangup (chiude, terminale), forward_sip (inoltra al SIP endpoint con i parametri della config, terminale), send_whatsapp (invia un template, non terminale).");

		builder.Property(x => x.ExecutionOrder)
			.HasColumnName("execution_order")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Ordine di esecuzione delle azioni della stessa voce, ascendente.");

		builder.Property(x => x.WaTemplateId)
			.HasColumnName("wa_template_id")
			.HasComment("Template WhatsApp da inviare (solo send_whatsapp); FK viva, l'azione ne segue lingua/categoria finché il template esiste.");

		builder.Property(x => x.WaTemplateNameSnapshot)
			.HasColumnName("wa_template_name_snapshot")
			.HasMaxLength(512)
			.HasComment("Nome del template su Meta congelato all'associazione, indipendente dal ciclo di vita della FK viva.");

		builder.Property(x => x.WaLanguage)
			.HasColumnName("wa_language")
			.HasMaxLength(10)
			.HasDefaultValue("it")
			.HasComment("Codice lingua del template nel formato Meta; codice di libreria, non enum di dominio.");

		builder.Property(x => x.WaRecipientMode)
			.HasColumnName("wa_recipient_mode")
			.HasColumnType("switchboard.wa_recipient_mode")
			.HasDefaultValue(WaRecipientMode.Caller)
			.HasComment("A chi inviare il template: numero del chiamante corrente o wa_recipient_phone_e164 fisso.");

		builder.Property(x => x.WaRecipientPhoneE164)
			.HasColumnName("wa_recipient_phone_e164")
			.HasMaxLength(20)
			.HasComment("Destinatario fisso in E.164 quando wa_recipient_mode = fixed.");

		builder.Property(x => x.WaBodyParams)
			.HasColumnName("wa_body_params")
			.HasColumnType("jsonb")
			.HasDefaultValueSql("'[]'::jsonb")
			.HasComment("Parametri posizionali del corpo del template Meta, passthrough dell'API di terzi.");

		builder.Property(x => x.WaHeaderParams)
			.HasColumnName("wa_header_params")
			.HasColumnType("jsonb")
			.HasDefaultValueSql("'[]'::jsonb")
			.HasComment("Parametri di header del template Meta, passthrough dell'API di terzi.");

		builder.Property(x => x.WaButtonParams)
			.HasColumnName("wa_button_params")
			.HasColumnType("jsonb")
			.HasDefaultValueSql("'[]'::jsonb")
			.HasComment("Parametri di bottone del template Meta, passthrough dell'API di terzi.");

		builder.ToTable(t =>
		{
			t.HasCheckConstraint("ck_switchboard_voice_menu_actions_wa_template",
				"action_type <> 'send_whatsapp' OR wa_template_id IS NOT NULL");
			t.HasCheckConstraint("ck_switchboard_voice_menu_actions_wa_recipient",
				"wa_recipient_mode <> 'fixed' OR wa_recipient_phone_e164 IS NOT NULL");
		});

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_switchboard_voice_menu_actions_public_id");

		builder.HasIndex(x => new { x.MenuItemId, x.ExecutionOrder })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_switchboard_voice_menu_actions_item_order");

		builder.HasIndex(x => x.WaTemplateId)
			.HasFilter("wa_template_id IS NOT NULL")
			.HasDatabaseName("ix_switchboard_voice_menu_actions_wa_template");

		builder.HasOne(x => x.MenuItem)
			.WithMany(i => i.Actions)
			.HasForeignKey(x => x.MenuItemId)
			.HasConstraintName("fk_switchboard_voice_menu_actions_menu_item")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.WaTemplate)
			.WithMany()
			.HasForeignKey(x => x.WaTemplateId)
			.HasConstraintName("fk_switchboard_voice_menu_actions_wa_template")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
```

`VoiceMenuAction` implementa `ISoftDeletable`/`IAuditable`. `WaTemplate` (FK) vive in
[`switchboard_messaging_wa_templates`](switchboard_messaging_wa_templates.md), stesso
schema `switchboard` ma modulo/area `Messaging`: FK reale comunque valida (stesso schema, cross-area),
`ON DELETE RESTRICT` — non si cancella un template referenziato da un'azione.

### Note di migrazione

- `ExecuteMenuAction`: `switch` su `MenuActionType` — `forward_sip` legge i parametri di forward da
  `SwitchboardConfig` (non dall'azione, `action_config` era contratto morto per questo tipo);
  `send_whatsapp` compone il payload Meta da `wa_*` + `wa_body_params`/`wa_header_params`/
  `wa_button_params` e lo spedisce come `Delivery` in outbox (canale WhatsApp) nella stessa transazione
  del comando IVR — fire-and-forget verso Meta via worker, non blocca il flusso vocale
  ([`docs/20-jobs.md`](../../../progettazione/20-jobs.md)).
- 0 righe legacy → nessun backfill da quelle copie; con righe reali (`action_config` json), parsing →
  colonne: per `hangup`/`forward_sip` nessuna colonna da popolare; per `send_whatsapp` se
  `templateName` non è risolvibile a un `wa_template_id` esistente → `wa_template_id NULL` +
  `wa_template_name_snapshot` valorizzato comunque + warning di migrazione (il `CHECK` impone
  `wa_template_id NOT NULL` solo a regime, non durante l'import storico).
