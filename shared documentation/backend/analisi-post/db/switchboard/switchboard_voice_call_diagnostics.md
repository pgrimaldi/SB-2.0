# Tabella `switchboard_voice_call_diagnostics`

> **Dominio**: C7 — `switchboard` · **sottoarea**: `voice` · **schema**: `switchboard` · **nome flat**:
> `switchboard_voice_call_diagnostics` · **forma canonica**: `switchboard.voice_call_diagnostics`.
>
> **Redesign** di [`switchboard_call_log`](../../../analisi-pre/db/switchboard_call_log.md)
> (0 righe su `local_sb`/dump di produzione, scritta solo con `SB_CALL_LOGGING_ENABLED=1`, **nessun
> lettore applicativo** nel legacy — su **nessuno** dei tre ambienti conosciuti, nemmeno quello con dati
> reali). **Tabella di puro log diagnostico**: espone `deleted_at` per uniformità ma non lo usa.
>
> **⚠️ Aggiornamento 2026-09-11** — **verificato su `summer_booking_dev`** (RDS, vedi
> [`switchboard_config.md`](../../../analisi-pre/db/switchboard_config.md)): questa **non** è una
> tabella marginale nel dominio. Ha **7 355 righe reali** (2026-04-22 → 2026-07-08, 596 sessioni di
> chiamata distinte) — è la tabella **più popolosa** dell'intero dominio Switchboard, a conferma che
> `SB_CALL_LOGGING_ENABLED=1` **è** effettivamente attivo in quell'ambiente. Non cambia la conclusione
> "nessun lettore applicativo" (confermata anche lì), ma cambia il quadro "feature marginale mai
> attivata" per l'intero dominio C7 → vedi [D-51](../_domande-committente.md) aggiornata.
>
> **Nota di perimetro.** [`docs/03 §2.4quinquies`](../../../progettazione/03-gestione-dati.md) suggerisce di
> **non** migrare `switchboard_call_log` come tabella di dominio (→ tracing/observability). È stata
> comunque creata **come tabella C7 su decisione del committente** (2026-09-10), con retention breve,
> per avere un traceback del flusso chiamata anche quando il tracing distribuito non è disponibile
> (es. incident post-mortem su un cliente specifico). Non è una dipendenza di runtime: nessun
> componente la rilegge (confermato anche sulle 7 355 righe reali).
>
> **Fonti**: [`analisi-pre/db/switchboard_call_log.md`](../../../analisi-pre/db/switchboard_call_log.md),
> [`docs/03 §2.4quinquies`](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §3.7 (30–90 giorni) / §4](../../../progettazione/80-retention.md),
> [`docs/11-modulo-privacy.md` §4 / §5](../../../progettazione/11-modulo-privacy.md),
> [`docs/31-observability.md`](../../../progettazione/31-observability.md),
> [`docs/15-logs.md`](../../../progettazione/15-logs.md).
>
> **Aggiornamento 2026-09-17 (D-50, risolta)**: il titolare fissa la retention all'estremo inferiore
> del range — **30 giorni**, azione a scadenza **cancella** (`DROP PARTITION`, come già descritto sotto)
> — [`docs/05-ottimizzazioni-db.md` §1](../../../progettazione/05-ottimizzazioni-db.md),
> [`docs/82-retention-data.md` §3.7](../../../progettazione/82-retention-data.md). Nessun cambio di
> meccanismo rispetto a quanto già disegnato in questa scheda, solo conferma del valore.

---

## Descrizione

**Una riga = un evento del flusso chiamata**: o un evento webhook Telnyx (`call.initiated`,
`call.answered`, `call.gather.ended`, `call.playback.ended`, `call.hangup`) o un passo interno dell'IVR
(`step.config_found`, `step.dtmf_received`, `step.action_forward_sip_done`, …). Serve solo alla
diagnostica manuale del percorso chiamate; **nessun processo la legge**.

Correzioni rispetto al legacy:

- **Disabilitata di default** (`SB_CALL_LOGGING_ENABLED`) + **nessun lettore** → resa **opt-in per
  stabilimento / per finestra temporale** (flag di config o feature flag), retention **30–90 giorni**
  ([`docs/80 §3.7`](../../../progettazione/80-retention.md)), poi 🗑️.
- **`event_type` a doppia natura** (eventi Telnyx `call.*` **e** passi `step.*`) → due colonne:
  `event_source` enum (`telnyx` / `ivr_step`) + `event_name` stringa (grezza, non tipizzabile).
  **Confermato dai dati reali**: `summer_booking_dev` mostra **30 valori distinti** di `event_type`
  (`call.initiated` 860×, `call.hangup` 815×, `step.config_not_found` 636×, … fino a `call.hold`/
  `call.unhold` 1× ciascuno — elenco completo nella scheda legacy), un vocabolario troppo largo e
  in crescita per essere un enum di dominio.
- **`property_id`/`switchboard_config_id` risolti in modo incoerente riga per riga** (nuova osservazione,
  non nel legacy): su `summer_booking_dev`, **79%** delle righe ha `property_id NULL` e **68%** ha
  `switchboard_config_id NULL`, **anche** su eventi dove il contesto era già stato risolto in righe
  precedenti della stessa sessione. Nel target questi restano campi opzionali (coerente coi dati reali),
  ma la correlazione primaria per ricostruire una sessione deve essere `telnyx_call_session_id` /
  `call_record_id`, **non** `config_id` (troppo spesso assente).
- **`raw_payload` senza redazione** (numeri di telefono, payload Telnyx completo, dato personale, senza
  TTL) → `raw_payload jsonb` **con pseudonimizzazione** dei numeri prima della scrittura + retention
  esplicita; `raw_payload` troncabile/omissibile via config.
- **Scritture fire-and-forget** (`.catch(console.error)`, perdite silenziose) → scrittura bufferizzata
  best-effort ma con metrica di scarto ([`docs/31`](../../../progettazione/31-observability.md)).
- **Non ripulita da `deleteConfig`** → job `PurgeDiagnosticLogs` mensile
  ([`docs/80 §"job"`](../../../progettazione/80-retention.md)).
- `+ call_record_id` per agganciare gli eventi alla chiamata di dominio
  ([`switchboard_voice_call_records`](switchboard_voice_call_records.md)).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `event_source` | **enum esclusivo** — `telnyx` (evento webhook) **o** `ivr_step` (passo interno). |
| `direction` | **enum esclusivo** (nullable) — `incoming` / `outgoing` (grezzo Telnyx), tipizzato solo per comodità di filtro. |
| `event_name` | **non** enum: passthrough di nomi evento Telnyx + costanti `step.*` — stringa diagnostica. |

## Snapshot

Non applicabile (log grezzo).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_switchboard_voice_call_diagnostics` | `id` | PK | — |
| `ix_..._session` | `(telnyx_call_session_id, created_at)` | — | ricostruzione del flusso di una chiamata |
| `ix_..._call_record` | `(call_record_id) WHERE call_record_id IS NOT NULL` | parziale | eventi di una chiamata di dominio |
| `ix_..._config_time` | `(config_id, created_at DESC)` | — | eventi recenti di un centralino |
| `ix_..._purge` | `(created_at)` | — | `PurgeDiagnosticLogs` (30–90 gg) |

## Campi

### id
- **Legacy**: `switchboard_call_log.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### config_id
- **Legacy**: `switchboard_call_log.switchboard_config_id` (FK `_ibfk_4`)
- **Descrizione**: centralino risolto dall'handler; `NULL` finché il contesto non è risolto
  (`config_not_found`).
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`switchboard_configs`](switchboard_configs.md)`.id`, `ON DELETE SET NULL`.

### property_id
- **Legacy**: `switchboard_call_log.property_id` (FK `_ibfk_3`)
- **Descrizione**: stabilimento risolto dall'handler (denormalizzato).
- **Tipo**: `uuid` (= `public_id` di [`catalog_properties`](../catalog/catalog_properties.md)).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: riferimento cross-modulo per valore.

### call_record_id
- **Legacy**: — (il log legacy non si agganciava al registro)
- **Descrizione**: chiamata di dominio a cui l'evento appartiene, quando risolvibile.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`switchboard_voice_call_records`](switchboard_voice_call_records.md)`.id`,
  `ON DELETE CASCADE`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 7.355 righe storiche, e dal 2026-10-01 il motivo è un altro: non è più che il registro chiamate sia `pending` (quella spec è attiva e scrive 214 righe), ma che **fra le due tabelle non esiste una chiave**. Il registro legacy ha quattro colonne — id, chiamante, destinazione, data — e nessun identificativo Telnyx; il log ha gli identificativi ma nessun riferimento al registro. Descrivono inoltre gambe diverse della stessa chiamata. Correlare per chiamante e vicinanza temporale non è una chiave: con una finestra di 60 secondi si ottengono 9.118 coppie candidate per 7.355 righe. Dal nuovo backend in avanti la colonna si popola normalmente. Voce M15 di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### event_source
- **Legacy**: parte di `switchboard_call_log.event_type` (prefisso `step.` o meno)
- **Descrizione**: origine dell'evento.
- **Tipo**: `enum switchboard.call_event_source` — valori: `telnyx`, `ivr_step`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### event_name
- **Legacy**: `switchboard_call_log.event_type` (`varchar(50)`)
- **Descrizione**: nome evento grezzo — `call.initiated` / `call.gather.ended` / … per `telnyx`;
  `step.config_found` / `step.dtmf_received` / … per `ivr_step`. Non tipizzato (vocabolario aperto,
  passthrough).
- **Tipo**: `varchar(64)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### direction
- **Legacy**: `switchboard_call_log.direction` (`varchar(20)`)
- **Descrizione**: verso della chiamata (`incoming` / `outgoing`), grezzo Telnyx.
- **Tipo**: `enum switchboard.call_direction_raw` — valori: `incoming`, `outgoing`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### caller_number / callee_number
- **Legacy**: `switchboard_call_log.from_number` / `.to_number` (`varchar(30)`)
- **Descrizione**: numeri chiamante/chiamato (E.164). **Pseudonimizzati** prima della scrittura
  (hash/troncamento) — [`docs/11 §4`](../../../progettazione/11-modulo-privacy.md).
- **Tipo**: `varchar(64)` ciascuno (contiene il valore pseudonimizzato).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### dtmf_digits
- **Legacy**: `switchboard_call_log.dtmf_digits` (`varchar(20)`)
- **Descrizione**: cifre DTMF raccolte all'evento `call.gather.ended` (`""` = timeout senza input).
- **Tipo**: `varchar(20)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### telnyx_call_session_id / telnyx_call_control_id / telnyx_call_leg_id
- **Legacy**: `switchboard_call_log.call_session_id` / `.call_control_id` / `.call_leg_id` (`varchar(150)`)
- **Descrizione**: identificatori Telnyx per correlare gli eventi di una stessa chiamata.
- **Tipo**: `varchar(150)` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `ix_..._session`.

### handler_result
- **Legacy**: `switchboard_call_log.handler_result` (`json`)
- **Descrizione**: valore di ritorno di `handleIncomingCall` o l'oggetto dati del passo. `jsonb`
  passthrough diagnostico, mai interrogato per sotto-campo.
- **Tipo**: `jsonb`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### raw_payload
- **Legacy**: `switchboard_call_log.raw_payload` (`json`)
- **Descrizione**: corpo del webhook Telnyx così come ricevuto, **con numeri pseudonimizzati**. Vuoto
  per le righe `ivr_step`. Omissibile/troncabile via config.
- **Tipo**: `jsonb`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### error_message
- **Legacy**: `switchboard_call_log.error_message` (`text`)
- **Descrizione**: messaggio d'errore se l'handler/il passo ha lanciato.
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: `switchboard_call_log.created_at` (`timestamps: true`, `updatedAt: false`)
- **Descrizione**: istante dell'evento. `updated_at` / `deleted_at` esposti per uniformità ma **non
  usati** (righe immutabili, cancellate in blocco dal job di retention a 30–90 giorni).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: —.

## Valutazione JSON

`handler_result` e `raw_payload` **restano `jsonb`**: sono payload di un sistema esterno (Telnyx) di
puro log diagnostico — [`docs/03 §3.6 punto 5`](../../../progettazione/03-gestione-dati.md) (documento di
terzi, passthrough) e la nota che i `raw_payload` di puro log **non andrebbero migrati affatto**. Qui
sono conservati per scelta esplicita ma con retention breve e pseudonimizzazione: nessun tentativo di
scomporli in colonne.

## Suggerimenti per le prestazioni

- **Off di default**: nessun costo di scrittura finché il logging non è attivato per uno stabilimento.
- Quando attivo: scrittura in **batch bufferizzato** (non una `INSERT` per evento), tabella
  **partizionata per `created_at`** (settimanale) → il purge a 30–90 giorni è `DROP PARTITION`.
- Preferibile comunque instradare questi eventi su **CloudWatch/OpenTelemetry**
  ([`docs/15-logs.md`](../../../progettazione/15-logs.md), [`docs/31`](../../../progettazione/31-observability.md));
  la tabella è un ripiego, non il canale primario.
- Nessun indice "largo": solo correlazione per sessione e purge per data.

## Implementazione .NET

`VoiceCallDiagnosticEvent` è un'**entità figlia write-only** (puro log, nessun lettore applicativo) del
modulo `Module.Switchboard`, area `Voice`: non implementa `ISoftDeletable`/`IAuditable` pieni — espone
`created_at`/`updated_at`/`deleted_at` solo per uniformità di schema, ma le righe sono immutabili e
cancellate in blocco dal job di retention, mai da un flusso di dominio. **Non** è una tabella marginale:
**7 355 righe reali** (596 sessioni) su `summer_booking_dev` la rendono la più popolosa del dominio C7,
pur restando senza lettore applicativo confermato anche lì — creata su decisione esplicita del
committente (2026-09-10) nonostante [`docs/03 §2.4quinquies`](../../../progettazione/03-gestione-dati.md)
suggerisca di non migrarla come tabella di dominio.

### Enum

```csharp
// Module.Switchboard.Domain.Enums.Voice/CallEventSource.cs
public enum CallEventSource
{
	Telnyx = 0,
	IvrStep = 1
}

// Module.Switchboard.Domain.Enums.Voice/CallDirectionRaw.cs
public enum CallDirectionRaw
{
	Incoming = 0,
	Outgoing = 1
}
```

```csharp
// SwitchboardDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<CallEventSource>("switchboard", "call_event_source");
modelBuilder.HasPostgresEnum<CallDirectionRaw>("switchboard", "call_direction_raw");
```

```csharp
// Module.Switchboard.Infrastructure DI
dataSourceBuilder.MapEnum<CallEventSource>("switchboard.call_event_source");
dataSourceBuilder.MapEnum<CallDirectionRaw>("switchboard.call_direction_raw");
```

### Configuration

```csharp
namespace Module.Switchboard.Infrastructure.Configurations.Voice;

public sealed class VoiceCallDiagnosticEventConfiguration : IEntityTypeConfiguration<VoiceCallDiagnosticEvent>
{
	public void Configure(EntityTypeBuilder<VoiceCallDiagnosticEvent> builder)
	{
		builder.ToTable("switchboard_voice_call_diagnostics", "switchboard", t => t.HasComment(
			"Evento del flusso chiamata (webhook Telnyx o passo interno IVR); log diagnostico write-only, nessun lettore applicativo."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_voice_call_diagnostics");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.HasComment("Centralino risolto dall'handler; NULL finché il contesto non è risolto (config_not_found).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; risolto dall'handler, spesso NULL sui dati reali.");

		builder.Property(x => x.CallRecordId)
			.HasColumnName("call_record_id")
			.HasComment("Chiamata di dominio a cui l'evento appartiene, quando risolvibile.");

		builder.Property(x => x.EventSource)
			.HasColumnName("event_source")
			.HasColumnType("switchboard.call_event_source")
			.IsRequired()
			.HasComment("Origine dell'evento: webhook Telnyx o passo interno dell'IVR.");

		builder.Property(x => x.EventName)
			.HasColumnName("event_name")
			.HasMaxLength(64)
			.IsRequired()
			.HasComment("Nome evento grezzo (call.initiated, step.dtmf_received, ...), vocabolario aperto non tipizzato.");

		builder.Property(x => x.Direction)
			.HasColumnName("direction")
			.HasColumnType("switchboard.call_direction_raw")
			.HasComment("Verso della chiamata grezzo Telnyx (incoming/outgoing).");

		builder.Property(x => x.CallerNumber)
			.HasColumnName("caller_number")
			.HasMaxLength(64)
			.HasComment("Numero chiamante pseudonimizzato prima della scrittura.");

		builder.Property(x => x.CalleeNumber)
			.HasColumnName("callee_number")
			.HasMaxLength(64)
			.HasComment("Numero chiamato pseudonimizzato prima della scrittura.");

		builder.Property(x => x.DtmfDigits)
			.HasColumnName("dtmf_digits")
			.HasMaxLength(20)
			.HasComment("Cifre DTMF raccolte all'evento call.gather.ended (stringa vuota = timeout senza input).");

		builder.Property(x => x.TelnyxCallSessionId)
			.HasColumnName("telnyx_call_session_id")
			.HasMaxLength(150)
			.HasComment("Id di sessione Telnyx per correlare gli eventi di una stessa chiamata.");

		builder.Property(x => x.TelnyxCallControlId)
			.HasColumnName("telnyx_call_control_id")
			.HasMaxLength(150)
			.HasComment("Id Telnyx per i comandi sulla chiamata.");

		builder.Property(x => x.TelnyxCallLegId)
			.HasColumnName("telnyx_call_leg_id")
			.HasMaxLength(150)
			.HasComment("Id Telnyx del leg di chiamata.");

		builder.Property(x => x.HandlerResult)
			.HasColumnName("handler_result")
			.HasColumnType("jsonb")
			.HasComment("Valore di ritorno dell'handler o dato del passo IVR, passthrough diagnostico mai interrogato per sotto-campo.");

		builder.Property(x => x.RawPayload)
			.HasColumnName("raw_payload")
			.HasColumnType("jsonb")
			.HasComment("Corpo del webhook Telnyx come ricevuto, con numeri pseudonimizzati; vuoto per le righe ivr_step.");

		builder.Property(x => x.ErrorMessage)
			.HasColumnName("error_message")
			.HasColumnType("text")
			.HasComment("Messaggio d'errore se l'handler/il passo ha lanciato un'eccezione.");

		builder.HasIndex(x => new { x.TelnyxCallSessionId, x.CreatedAt })
			.HasDatabaseName("ix_switchboard_voice_call_diagnostics_session");

		builder.HasIndex(x => x.CallRecordId)
			.HasFilter("call_record_id IS NOT NULL")
			.HasDatabaseName("ix_switchboard_voice_call_diagnostics_call_record");

		builder.HasIndex(x => new { x.ConfigId, x.CreatedAt })
			.HasDatabaseName("ix_switchboard_voice_call_diagnostics_config_time");

		builder.HasIndex(x => x.CreatedAt)
			.HasDatabaseName("ix_switchboard_voice_call_diagnostics_purge");

		builder.HasOne(x => x.Config)
			.WithMany()
			.HasForeignKey(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_voice_call_diagnostics_config")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(x => x.CallRecord)
			.WithMany(r => r.DiagnosticEvents)
			.HasForeignKey(x => x.CallRecordId)
			.HasConstraintName("fk_switchboard_voice_call_diagnostics_call_record")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

### Note di migrazione

- Feature flag `Switchboard:CallDiagnostics:Enabled` per property + finestra temporale (il legacy era
  on/off globale via `SB_CALL_LOGGING_ENABLED`); scrittura via channel bufferizzato +
  `IHostedService` di flush, best-effort ma con metrica di scarto (non `.catch(console.error)` silente
  come nel legacy).
- Pseudonimizzatore dei numeri (hash con pepper) applicato a `caller_number`/`callee_number`/
  `raw_payload` **prima** della persistenza, non a posteriori.
- Job `PurgeDiagnosticLogs` (mensile): `DROP PARTITION` oltre la finestra di 30–90 giorni
  ([`docs/80 §"job"`](../../../progettazione/80-retention.md)).
- 0 righe su `local_sb`/dump di produzione → nessun backfill da quelle copie; le **7 355 righe reali**
  su `summer_booking_dev` confermano che il logging è realmente attivo in quell'ambiente, ma restano
  fuori da qualunque backfill del nuovo schema (dati diagnostici, non di dominio, già coperti da
  retention breve). In fase implementativa resta aperta la scelta — indicata dalla scheda come
  rivedibile — se creare comunque questa tabella o instradare questi eventi solo su tracing/observability
  (CloudWatch/OpenTelemetry, [`docs/31`](../../../progettazione/31-observability.md)), dato che nessun
  componente la rilegge oggi in nessuno dei tre ambienti conosciuti.
