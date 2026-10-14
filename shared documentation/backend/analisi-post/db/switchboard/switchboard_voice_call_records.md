# Tabella `switchboard_voice_call_records`

> **Dominio**: C7 — `switchboard` · **sottoarea**: `voice` · **schema**: `switchboard` · **nome flat**:
> `switchboard_voice_call_records` · **forma canonica**: `switchboard.voice_call_records`.
>
> **Redesign** di [`switchboard_call_register`](../../../analisi-pre/db/switchboard_call_register.md)
> (0 righe in `local_sb` e in produzione — ma **verificato 2026-09-11**: 214 righe reali su
> `summer_booking_dev`). [`docs/03 §2.4quinquies`](../../../progettazione/03-gestione-dati.md) indica
> esplicitamente: *"andrebbe modellata come vera tabella eventi con `switchboard_config_id`, esito e
> durata (dominio `switchboard`, sottoarea `voice`)"*.
>
> **Fonti**: [`analisi-pre/db/switchboard_call_register.md`](../../../analisi-pre/db/switchboard_call_register.md),
> [`analisi-pre/db/switchboard_call_log.md`](../../../analisi-pre/db/switchboard_call_log.md)
> (nomi degli eventi/step Telnyx), [`docs/03 §2.4quinquies`](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §3.7 / §"tabella riassuntiva"](../../../progettazione/80-retention.md),
> [`docs/11-modulo-privacy.md` §4 / §5](../../../progettazione/11-modulo-privacy.md).
>
> **Aggiornamento 2026-09-17 (D-50, risolta) — retention DECISA dal titolare, sostituisce i "12 mesi
> poi 🔒" sotto**: **6 mesi**, azione a scadenza **cancella** (hard-delete diretto, niente
> pseudonimizzazione intermedia del numero) — [`docs/05-ottimizzazioni-db.md` §1](../../../progettazione/05-ottimizzazioni-db.md),
> [`docs/82-retention-data.md` §3.7](../../../progettazione/82-retention-data.md). Il job `PurgeCallRecords`
> e l'indice `..._retention` restano validi nel meccanismo (scansione/partizione per `started_at`), ma
> l'azione a 6 mesi è `DROP PARTITION`/`DELETE`, non una pseudonimizzazione di `caller_number` seguita
> da una fase successiva. Il dubbio D.L. 7/2015 (72 mesi) resta aperto in [D-51](../_domande-committente.md)
> e potrebbe comunque imporre di allungare questa finestra.

---

## Descrizione

**Una riga = una chiamata gestita dal centralino** (dominio, non log diagnostico). Sostituisce il
"registro chiamate" legacy — che aveva solo `from` / `to` / `datetime`, nessun collegamento a
config/property, nessun esito, nessuna durata, e si popolava **solo** dall'evento Telnyx
`call.initiated` del B-leg SIP (se quell'evento non arrivava, lo storico restava vuoto). Serve al
client softphone per lo storico "chi mi ha chiamato" e agli operatori/report per volumi ed esiti.

Correzioni rispetto al legacy:

- **`from` / `to` / `datetime` soltanto** → tabella eventi completa: `config_id`, `direction`,
  `outcome`, timestamp di ciclo di vita (`started_at`/`answered_at`/`ended_at`), `duration_seconds`
  (generata), percorso IVR, esito dell'inoltro.
- **`to` = stringa `sip:<username>@<domain>`** con match esatto (se dominio o formato cambiavano fra
  scrittura e lettura, lo storico "spariva") → `direction` + `sip_destination` esplicito + FK a
  `config_id` (join stabile, non match di stringa). **Confermato reale, non solo teorico**: su
  `summer_booking_dev`, **34/214 righe (16%)** — concentrate in una finestra di 3 giorni (probabile
  regressione temporanea di una build) — hanno `to` in un formato che `getSipCallHistory` non
  troverebbe mai (username SIP senza `sip:`/dominio, o addirittura un numero E.164 al posto del SIP
  URI). La FK stabile elimina strutturalmente questa classe di bug.
- **Nessuna finestra di retention** → `+ public_id`, retention **12 mesi** poi 🗑️ / 🔒 del numero
  ([`docs/80 §3.7`](../../../progettazione/80-retention.md)); dubbio D.L. 7/2015 (72 mesi) →
  [D-51](../_domande-committente.md).
- **Endpoint pubblico senza rate limiting** (`getSipCallHistory` autentica con `sip_username` +
  `sip_password` in chiaro) → verifica del segreto risolto + rate limiting.
- **Non ripulita da `deleteConfig`** → `ON DELETE CASCADE` da `config_id` **oppure** conservazione
  post-cessazione secondo retention (le due opzioni in [D-51](../_domande-committente.md)).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `direction` | **enum esclusivo** — `inbound` (chiamata gestita) **o** `outbound` (B-leg del nostro forward). |
| `outcome` | **enum esclusivo** — un solo esito finale per chiamata. |
| `forward_result` | **enum esclusivo** (nullable) — valorizzato solo se c'è stato un tentativo di `forward_sip`. |
| — | nessun gruppo di flag booleani → nessun `enum[]`. |

## Snapshot

`sip_destination`: SIP URI dell'endpoint dello stabilimento **congelato all'istante della chiamata**
(nel legacy era `callEvent.to`). È un **fatto proprio dell'evento** (come `line_price` in C1), non
un'etichetta copiata da un'entità mutabile con FK viva → **nessun suffisso `_snapshot`**; la natura
"congelata" è qui in descrizione (criterio C1-7). `caller_number` / `callee_number` idem.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_switchboard_voice_call_records` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | id pubblico (GDPR, deep-link storico) |
| `ix_..._config_started` | `(config_id, started_at DESC)` | — | storico chiamate del centralino ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): a DB mancava perché la causa era EF: due `HasIndex` sulle stesse colonne senza nome sono lo stesso indice, e il secondo sovrascrive il primo. |
| `ix_..._history` | `(config_id, started_at DESC) WHERE direction = 'outbound'` | parziale | storico softphone "chi mi ha chiamato nelle ultime N ore" (`getSipCallHistory`) |
| `ix_..._caller` | `(property_id, caller_number, started_at DESC)` | — | ricerca per numero chiamante |
| `ix_..._session` | `(telnyx_call_session_id)` | — | correlazione A-leg / B-leg |
| `ix_..._retention` | `(started_at) WHERE deleted_at IS NULL` | parziale | purge/pseudonimizzazione a 12 mesi |

## Campi

### id
- **Legacy**: `switchboard_call_register.id`
- **Descrizione**: PK tecnica interna allo schema.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile (export GDPR: la chiamata contiene numeri di telefono = dato
  personale).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato applicativamente.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### config_id
- **Legacy**: — (il registro legacy non collegava la riga alla config)
- **Descrizione**: centralino che ha gestito la chiamata.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`switchboard_configs`](switchboard_configs.md)`.id`.

### property_id
- **Legacy**: — (ricavabile solo via il SIP username nel campo `to`)
- **Descrizione**: stabilimento (denormalizzato per scoping/retention/report senza join).
- **Tipo**: `uuid` (= `public_id` di [`catalog_properties`](../catalog/catalog_properties.md)).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: riferimento cross-modulo per valore.

### direction
- **Legacy**: dedotto da `callEvent.direction` (`switchboard_call_log.direction`)
- **Descrizione**: `inbound` = chiamata in ingresso gestita dall'IVR; `outbound` = B-leg SIP creato
  dal nostro `transfer`/`forward_sip` (l'unico che il registro legacy inseriva).
- **Tipo**: `enum switchboard.call_direction` — valori: `inbound`, `outbound`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### caller_number
- **Legacy**: `switchboard_call_register.from` (`varchar(50)`)
- **Descrizione**: numero del chiamante PSTN originale, E.164. Dato personale.
- **Tipo**: `varchar(30)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: pseudonimizzabile a fine retention (🔒 del numero).

### callee_number
- **Legacy**: — (implicito nel `to` SIP)
- **Descrizione**: numero Telnyx del centralino chiamato (per `inbound`), E.164.
- **Tipo**: `varchar(30)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### sip_destination
- **Legacy**: `switchboard_call_register.to` (`varchar(255)`, `sip:<username>@<domain>`)
- **Descrizione**: SIP URI dell'endpoint dello stabilimento verso cui è stata inoltrata la chiamata,
  congelato all'istante della chiamata.
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (solo quando c'è stato un `forward_sip`).
- **Relazioni**: —.

### outcome
- **Legacy**: — (il registro non aveva esito)
- **Descrizione**: esito finale della chiamata.
- **Tipo**: `enum switchboard.call_outcome` — valori: `answered_by_ivr`, `forwarded_answered`,
  `forwarded_no_answer`, `forwarded_busy`, `forwarded_failed`, `caller_hangup`, `rejected_inactive`,
  `no_config`, `hangup_after_timeout`, `whatsapp_only`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### forward_result
- **Legacy**: — (correlato solo via `call_session_id` nel legacy, senza persistenza)
- **Descrizione**: esito del tentativo di inoltro SIP, se avvenuto.
- **Tipo**: `enum switchboard.forward_result` — valori: `answered`, `no_answer`, `busy`, `failed`,
  `skipped_no_endpoint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (valorizzato solo se `outcome` riguarda un forward).
- **Relazioni**: —.

### ivr_path
- **Legacy**: — (i tasti premuti erano solo in `switchboard_call_log` diagnostico)
- **Descrizione**: sequenza sintetica dei tasti premuti e delle voci attraversate, es.
  `[{ "key": "2", "label": "Ristorante" }, { "key": "1", "label": "Prenotazioni" }]`. Piccolo array
  **snapshot**, scoped alla singola chiamata, mai interrogato per sotto-campo (diagnostica di flusso).
- **Tipo**: `jsonb`.
- **Default**: `'[]'::jsonb`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### final_menu_item_public_id
- **Legacy**: —
- **Descrizione**: ultima voce di menù raggiunta (id pubblico), per analisi "dove finiscono le
  chiamate".
- **Tipo**: `uuid`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: riferimento (per valore) a
  [`switchboard_voice_menu_items`](switchboard_voice_menu_items.md)`.public_id`.

### telnyx_call_session_id / telnyx_call_control_id / telnyx_call_leg_id
- **Legacy**: campi presenti in `switchboard_call_log` (non nel registro)
- **Descrizione**: identificatori Telnyx della chiamata: `session_id` condiviso da A-leg e B-leg (per
  correlare il fallimento del forward), `call_control_id` (comandi), `call_leg_id`. Identificatori
  esterni opachi.
- **Tipo**: `varchar(150)` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `ix_..._session` su `telnyx_call_session_id`.

### started_at / answered_at / ended_at
- **Legacy**: `switchboard_call_register.datetime` (solo l'istante di registrazione del forward)
- **Descrizione**: inizio chiamata (`call.initiated`) / risposta (`call.answered`) / fine
  (`call.hangup`).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` per `started_at`; `NULL` per gli altri.
- **Nullabile**: No / Sì / Sì.
- **Relazioni**: —.

### duration_seconds
- **Legacy**: — (nessuna durata)
- **Descrizione**: durata conversazione = `ended_at - answered_at`, in secondi. Colonna **generata**
  (`GENERATED ALWAYS AS … STORED`) o calcolata alla chiusura.
- **Tipo**: `integer`.
- **Default**: derivato.
- **Nullabile**: Sì (finché la chiamata non è chiusa / se mai risposta).
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: `switchboard_call_register` — `timestamps: false`
- **Descrizione**: creazione / aggiornamenti di stato durante la chiamata / soft-delete o
  pseudonimizzazione a fine retention (12 mesi). `deleted_at` esposto per uniformità; la
  pseudonimizzazione sostituisce `caller_number` con un token e mantiene la riga per le statistiche.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

- **`ivr_path` resta `jsonb`**: piccolo array scoped alla singola chiamata, snapshot del percorso,
  mai filtrato per sotto-campo. Alternativa (child table `..._call_ivr_steps`) sproporzionata per un
  dato puramente diagnostico. → [D-52](../_domande-committente.md).
- Tutto il resto è colonne tipizzate: il legacy `switchboard_call_register` non aveva JSON; qui si
  **aggiungono** dimensioni (esito, durata, direzione) come colonne, non come blob.

## Suggerimenti per le prestazioni

- Tabella **append-heavy** (una riga per chiamata). Con volumi reali di un centralino di stabilimento
  (decine/centinaia di chiamate al giorno) resta piccola entro la retention di 12 mesi.
- Partizionamento per `started_at` (mensile) se i volumi cross-property lo giustificano; il purge a
  12 mesi diventa `DROP PARTITION`.
- Lo storico softphone (`getSipCallHistory`) filtra `direction = 'outbound'` + finestra oraria +
  `ORDER BY started_at DESC` → coperto da `ix_..._history`.
- Scritture dal webhook Telnyx: **idempotenti** su `telnyx_call_session_id` (una riga per sessione),
  non fire-and-forget come nel legacy.

## Implementazione .NET

`VoiceCallRecord` è un'**entità figlia** append-heavy del modulo `Module.Switchboard`, area `Voice`:
una riga per chiamata gestita dal centralino (tabella di dominio, non log diagnostico — quello è
[`switchboard_voice_call_diagnostics`](switchboard_voice_call_diagnostics.md)). 0 righe su
`local_sb`/dump di produzione, ma **214 righe reali** su `summer_booking_dev`, di cui il 16%
confermano concretamente il bug di match-per-stringa che la FK a `config_id` elimina.

### Enum

```csharp
// Module.Switchboard.Domain.Enums.Voice/CallDirection.cs
public enum CallDirection
{
	Inbound = 0,
	Outbound = 1
}

// Module.Switchboard.Domain.Enums.Voice/CallOutcome.cs
public enum CallOutcome
{
	AnsweredByIvr = 0,
	ForwardedAnswered = 1,
	ForwardedNoAnswer = 2,
	ForwardedBusy = 3,
	ForwardedFailed = 4,
	CallerHangup = 5,
	RejectedInactive = 6,
	NoConfig = 7,
	HangupAfterTimeout = 8,
	WhatsappOnly = 9
}

// Module.Switchboard.Domain.Enums.Voice/ForwardResult.cs
public enum ForwardResult
{
	Answered = 0,
	NoAnswer = 1,
	Busy = 2,
	Failed = 3,
	SkippedNoEndpoint = 4
}
```

```csharp
// SwitchboardDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<CallDirection>("switchboard", "call_direction");
modelBuilder.HasPostgresEnum<CallOutcome>("switchboard", "call_outcome");
modelBuilder.HasPostgresEnum<ForwardResult>("switchboard", "forward_result");
```

```csharp
// Module.Switchboard.Infrastructure DI
dataSourceBuilder.MapEnum<CallDirection>("switchboard.call_direction");
dataSourceBuilder.MapEnum<CallOutcome>("switchboard.call_outcome");
dataSourceBuilder.MapEnum<ForwardResult>("switchboard.forward_result");
```

### Configuration

```csharp
namespace Module.Switchboard.Infrastructure.Configurations.Voice;

public sealed class VoiceCallRecordConfiguration : IEntityTypeConfiguration<VoiceCallRecord>
{
	public void Configure(EntityTypeBuilder<VoiceCallRecord> builder)
	{
		builder.ToTable("switchboard_voice_call_records", "switchboard", t => t.HasComment(
			"Una riga per chiamata gestita dal centralino: esito, durata, percorso IVR — tabella di dominio, non log diagnostico."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_voice_call_records");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile (export GDPR: la chiamata contiene numeri di telefono, dato personale).");

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.IsRequired()
			.HasComment("Centralino che ha gestito la chiamata.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping/retention/report senza join.");

		builder.Property(x => x.Direction)
			.HasColumnName("direction")
			.HasColumnType("switchboard.call_direction")
			.IsRequired()
			.HasComment("inbound = chiamata in ingresso gestita dall'IVR; outbound = B-leg SIP creato dal nostro forward_sip.");

		builder.Property(x => x.CallerNumber)
			.HasColumnName("caller_number")
			.HasMaxLength(30)
			.HasComment("Numero del chiamante PSTN originale in E.164 — dato personale, pseudonimizzabile a fine retention.");

		builder.Property(x => x.CalleeNumber)
			.HasColumnName("callee_number")
			.HasMaxLength(30)
			.HasComment("Numero Telnyx del centralino chiamato (inbound), in E.164.");

		builder.Property(x => x.SipDestination)
			.HasColumnName("sip_destination")
			.HasMaxLength(255)
			.HasComment("SIP URI dell'endpoint dello stabilimento verso cui è stata inoltrata la chiamata, congelato all'istante della chiamata.");

		builder.Property(x => x.Outcome)
			.HasColumnName("outcome")
			.HasColumnType("switchboard.call_outcome")
			.IsRequired()
			.HasComment("Esito finale della chiamata.");

		builder.Property(x => x.ForwardResult)
			.HasColumnName("forward_result")
			.HasColumnType("switchboard.forward_result")
			.HasComment("Esito del tentativo di inoltro SIP, valorizzato solo se l'outcome riguarda un forward.");

		builder.Property(x => x.IvrPath)
			.HasColumnName("ivr_path")
			.HasColumnType("jsonb")
			.HasDefaultValueSql("'[]'::jsonb")
			.HasComment("Sequenza sintetica dei tasti premuti e delle voci attraversate, snapshot scoped alla singola chiamata.");

		builder.Property(x => x.FinalMenuItemPublicId)
			.HasColumnName("final_menu_item_public_id")
			.HasComment("public_id dell'ultima voce di menù raggiunta, per analisi di dove finiscono le chiamate.");

		builder.Property(x => x.TelnyxCallSessionId)
			.HasColumnName("telnyx_call_session_id")
			.HasMaxLength(150)
			.HasComment("Id di sessione Telnyx condiviso da A-leg e B-leg, per correlare il fallimento del forward.");

		builder.Property(x => x.TelnyxCallControlId)
			.HasColumnName("telnyx_call_control_id")
			.HasMaxLength(150)
			.HasComment("Id Telnyx per i comandi sulla chiamata.");

		builder.Property(x => x.TelnyxCallLegId)
			.HasColumnName("telnyx_call_leg_id")
			.HasMaxLength(150)
			.HasComment("Id Telnyx del leg di chiamata.");

		builder.Property(x => x.StartedAt)
			.HasColumnName("started_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Istante di inizio chiamata (evento call.initiated).");

		builder.Property(x => x.AnsweredAt)
			.HasColumnName("answered_at")
			.HasComment("Istante di risposta (evento call.answered).");

		builder.Property(x => x.EndedAt)
			.HasColumnName("ended_at")
			.HasComment("Istante di fine chiamata (evento call.hangup).");

		builder.Property(x => x.DurationSeconds)
			.HasColumnName("duration_seconds")
			.HasComment("Durata conversazione (ended_at - answered_at) in secondi.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_switchboard_voice_call_records_public_id");

		builder.HasIndex(x => new { x.ConfigId, x.StartedAt })
			.HasDatabaseName("ix_switchboard_voice_call_records_config_started");

		builder.HasIndex(x => new { x.ConfigId, x.StartedAt })
			.HasFilter("direction = 'outbound'")
			.HasDatabaseName("ix_switchboard_voice_call_records_history");

		builder.HasIndex(x => new { x.PropertyId, x.CallerNumber, x.StartedAt })
			.HasDatabaseName("ix_switchboard_voice_call_records_caller");

		builder.HasIndex(x => x.TelnyxCallSessionId)
			.HasDatabaseName("ix_switchboard_voice_call_records_session");

		builder.HasIndex(x => x.StartedAt)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_switchboard_voice_call_records_retention");

		builder.HasOne(x => x.Config)
			.WithMany(c => c.CallRecords)
			.HasForeignKey(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_voice_call_records_config")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

`VoiceCallRecord` implementa `ISoftDeletable`/`IAuditable`; `deleted_at` è esposto per uniformità ma la
retention primaria è la **pseudonimizzazione** di `caller_number` a 12 mesi (job dedicato, non un
soft-delete). `ix_switchboard_voice_call_records_config_started` e `..._caller`/`..._session` sono
indici multi-colonna semplici (nessun `IsUnique`); `..._history` e `..._retention` sono parziali. Gli
indici `(config_id, started_at DESC)` / `(property_id, caller_number, started_at DESC)` si esprimono in
EF come `HasIndex` su tupla + `.IsDescending(...)` per l'ordine reale del `DESC` dichiarato in scheda,
da confermare in fase implementativa secondo il piano di query effettivo (Npgsql onora comunque l'ordine
ASC/DESC misto se specificato).

### Note di migrazione

- Handler webhook Telnyx: upsert **idempotente** per `telnyx_call_session_id` (una riga per sessione,
  non un insert per evento come il legacy); transizioni di stato `initiated → answered → ended` con
  calcolo di `duration_seconds` alla chiusura.
- `GetSipCallHistory`: autenticazione via segreto SIP risolto
  ([`switchboard_configs.sip_password_secret_ref`](switchboard_configs.md)) + rate limiting,
  proiezione `caller_number` + `started_at` su `ix_switchboard_voice_call_records_history`.
- Job `PurgeCallRecords` (mensile): pseudonimizza `caller_number` oltre 12 mesi, poi hard-delete secondo
  policy ([`docs/80 §"job"`](../../../progettazione/80-retention.md)); la scelta fra 12 mesi e 72 mesi (dubbio
  D.L. 7/2015) resta [D-51](../_domande-committente.md).
- 0 righe legacy → nessun backfill da quelle copie; con le 214 righe reali di `summer_booking_dev`, le
  34 righe (16%) con `to` in formato non riconoscibile (username senza `sip:`/dominio, o un E.164 al
  posto del SIP URI) vanno riconciliate a `config_id` per correlazione temporale/property, non per match
  di stringa sul valore originale.
