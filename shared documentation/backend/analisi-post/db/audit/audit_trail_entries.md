# Tabella `audit_trail_entries`

> **Dominio**: `Module.Audit` — `audit` · **sottoarea**: — · **schema**: `audit` · **nome flat**:
> `audit_trail_entries` · **forma canonica**: `audit.trail_entries`.
>
> Tabella **nuova** (nessuna legacy: il legacy non ha alcun registro delle azioni; `booking_logs`, a
> dispetto del nome, è il contatore dei numeri di prenotazione). Fonti:
> [`docs/86-modulo-audit.md`](../../../progettazione/86-modulo-audit.md) (decisione e regole),
> [`analisi-pre/azioni/stabilimento/13-gestionale-impostazioni.md` §3](../../../analisi-pre/azioni/stabilimento/13-gestionale-impostazioni.md)
> (pagina «Tutti i log»), [`docs/15-logs.md` §2](../../../progettazione/15-logs.md) (log di sicurezza e degli
> amministratori in DB), [`docs/80-retention.md`](../../../progettazione/80-retention.md) (retention).
>
>
> ✅ **Creata il 2026-10-07** (`Module.Audit`, migration `InitialCreate`, applicata al DB di sviluppo): 28
> partizioni (ottobre 2026 – dicembre 2028 + default), PK, UNIQUE e tutti gli indici sotto. **Non ancora
> fatti**: i permessi DB (solo `INSERT`/`SELECT` all'utente applicativo, da impostare al provisioning dei
> ruoli DB) e il job delle partizioni future (PROMEMORIA-85-4).
>
> ✅ **2026-10-13**: gestore dell'evento, API ([`docs/88-api-audit.md`](../../../progettazione/88-api-audit.md)),
> valore `audit` in `audit.source_module` (migration `AddAuditSourceModule`) e le tre policy di retention
> (migration `AddAuditRetentionPolicies` di Privacy). Paginazione a **offset**, non a cursore, per coerenza
> con il resto del progetto.
>
> **Nota sulla voce "Legacy"**: tabella senza antecedente — **tutti i campi sono `— (nuovo)`**.

---

## Descrizione

**Registro append-only trasversale** di "chi ha fatto cosa": una riga per azione rilevante compiuta da
un operatore, da un utente in fase di autenticazione o dal systemadmin, in **qualunque** modulo. Le righe
arrivano **solo** come integration event `AuditEntryRecorded` pubblicati dai moduli tramite outbox:
nessun modulo scrive in questo schema e Audit non conosce il dominio degli altri.

Tre categorie (`category`): `operator` (pagina «Tutti i log» dello stabilimento), `security` (login,
blocchi, reset, 2FA, gestione utenti e ruoli), `platform` (azioni del systemadmin, impersonazione).

Ciò che serve alla pagina è **fotografato** nell'evento al momento dell'azione (nome dell'operatore,
numero di prenotazione, postazione): la lettura non chiama gli altri moduli e la voce resta leggibile
anche se l'oggetto viene poi cancellato.

**Non** contiene i log tecnici di dominio (`fiskaly_receipt_logs`, `payment_webhook_events`,
`notification_deliveries`, …), che restano nei rispettivi moduli — regola in
[`docs/86-modulo-audit.md` §1](../../../progettazione/86-modulo-audit.md).

## Indici

Tabella **partizionata per mese** su `occurred_at` (`PARTITION BY RANGE`): ogni `PRIMARY KEY`/`UNIQUE`
deve includere la colonna di partizione ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md),
stesso schema delle tabelle di consumo di Booking, [`docs/06-partizioni-manutenzione.md`](../../../progettazione/06-partizioni-manutenzione.md)).

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_audit_trail_entries` | `(id, occurred_at)` | PK | requisito del partizionamento |
| `uq_audit_trail_entries_source_event` | `(source_event_id, occurred_at)` | UNIQUE | deduplica della consegna *at-least-once* dell'outbox |
| `ix_..._property_time` | `(property_id, occurred_at DESC, id DESC) WHERE property_id IS NOT NULL` | parziale | pagina «Tutti i log»: elenco dello stabilimento ordinato per data |
| `ix_..._property_actor_time` | `(property_id, actor_user_id, occurred_at DESC) WHERE property_id IS NOT NULL` | parziale | pulsante «Log» del singolo utente |
| `ix_..._actor_time` | `(actor_user_id, occurred_at DESC) WHERE actor_user_id IS NOT NULL` | parziale | storico di sicurezza di un account (anche senza stabilimento: consumer, systemadmin) |
| `ix_..._category_time` | `(category, occurred_at DESC)` | — | consultazione `platform`/`security` dal back-office, retention per categoria |
| `brin_..._occurred_at` | `(occurred_at)` | BRIN | estrazioni per periodo dentro una partizione |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica (insieme a `occurred_at`).
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK composita `(id, occurred_at)`.
- **Note**: niente `public_id`: la voce non ha un URL proprio. Esposto nelle API come `id` (chiave di riga
  per il frontend), mai usato come parametro.

### source_event_id
- **Legacy**: — (nuovo)
- **Descrizione**: id dell'integration event `AuditEntryRecorded` che ha generato la riga.
- **Tipo**: `uuid` (UUIDv7 generato dal modulo che pubblica).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: rende idempotente la scrittura: una seconda consegna dello stesso evento viene ignorata
  (`ON CONFLICT DO NOTHING`).

### occurred_at
- **Legacy**: — (nuovo)
- **Descrizione**: istante in cui l'azione è avvenuta nel modulo di origine (non quello di scrittura qui).
- **Tipo**: `timestamptz`.
- **Default**: nessuno (arriva dall'evento).
- **Nullabile**: No.
- **Relazioni**: colonna di partizione.

### recorded_at
- **Legacy**: — (nuovo)
- **Descrizione**: istante di scrittura in questa tabella. La differenza con `occurred_at` misura il
  ritardo dell'outbox.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### category
- **Legacy**: — (nuovo)
- **Descrizione**: famiglia dell'azione; decide chi la può leggere e la retention.
- **Tipo**: enum PG `audit.entry_category` — valori: `operator`, `security`, `platform`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### source_module
- **Legacy**: — (nuovo)
- **Descrizione**: modulo che ha pubblicato l'evento.
- **Tipo**: enum PG `audit.source_module` — valori: `identity`, `catalog`, `booking`, `payment`,
  `fiskaly`, `fiscalprint`, `notification`, `switchboard`, `chatbot`, `privacy`, `infra`, `audit` (export del
  registro, aggiunto il 2026-10-13).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### action_code
- **Legacy**: — (nuovo)
- **Descrizione**: codice stabile dell'azione, `<modulo>.<azione_al_passato>` (`booking.created`,
  `identity.login_failed`, `fiskaly.receipt_issued`). Il frontend lo traduce e ne sceglie l'icona.
- **Tipo**: `varchar(100)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: **non** un enum PG: il catalogo cresce con le API di ogni modulo e un nuovo codice non deve
  richiedere una migration di Audit. Il catalogo vive come costanti nel contratto in Shared
  ([`docs/86-modulo-audit.md` §5](../../../progettazione/86-modulo-audit.md)).

### outcome
- **Legacy**: — (nuovo)
- **Descrizione**: esito dell'azione. Serve soprattutto agli eventi di sicurezza (login fallito,
  OTP errato).
- **Tipo**: enum PG `audit.entry_outcome` — valori: `succeeded`, `failed`.
- **Default**: `'succeeded'`.
- **Nullabile**: No.
- **Relazioni**: —.

### property_id
- **Legacy**: — (nuovo)
- **Descrizione**: stabilimento in cui è avvenuta l'azione.
- **Tipo**: `uuid` — `public_id` della `property` (C2), copiato.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` per azioni senza stabilimento: login di un consumer, azioni di piattaforma
  non legate a uno stabilimento).
- **Relazioni**: nessuna FK cross-modulo. Discriminatore di tenant per la lettura.

### actor_kind
- **Legacy**: — (nuovo)
- **Descrizione**: chi ha compiuto l'azione.
- **Tipo**: enum PG `audit.actor_kind` — valori: `user`, `system`, `external_client`, `anonymous`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `anonymous` per i tentativi di login su un account inesistente; `system` per job e
  automatismi; `external_client` per i partner M2M.

### actor_user_id
- **Legacy**: — (nuovo)
- **Descrizione**: account che ha compiuto l'azione.
- **Tipo**: `uuid` — `public_id` di [`identity_access_users`](../identity/identity_access_users.md).
- **Default**: nessuno.
- **Nullabile**: Sì (valorizzato solo se `actor_kind = user`).
- **Relazioni**: riferimento cross-modulo per valore, nessuna FK.

### actor_name_snapshot / actor_role_snapshot
- **Legacy**: — (nuovo)
- **Descrizione**: nome e cognome e codice ruolo dell'operatore **al momento dell'azione** (colonna
  «nome e cognome operatore» della pagina).
- **Tipo**: `varchar(200)` / `varchar(50)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.

### impersonator_user_id
- **Legacy**: — (nuovo)
- **Descrizione**: se l'azione è stata compiuta da un systemadmin in impersonazione ("accesso come
  gestore"), il suo account. L'azione resta attribuita all'utente impersonato (`actor_user_id`), ma si
  sa chi l'ha davvero compiuta.
- **Tipo**: `uuid` — `public_id` di `identity_access_users`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: per valore, nessuna FK.

### target_type / target_id
- **Legacy**: — (nuovo)
- **Descrizione**: oggetto principale dell'azione (`booking`, `customer`, `user`, `receipt`,
  `price_list`, …) e il suo `public_id`. Serve alla freccia che porta alla pagina dell'oggetto.
- **Tipo**: `varchar(50)` / `uuid`.
- **Default**: nessuno.
- **Nullabile**: Sì (entrambi; un login non ha oggetto).
- **Relazioni**: riferimento polimorfo per valore, nessuna FK
  ([`docs/pre/identificatori-e-uuid.md` §1](../../../progettazione/pre/identificatori-e-uuid.md)).

### booking_number_snapshot / spot_label_snapshot
- **Legacy**: — (nuovo)
- **Descrizione**: numero di prenotazione e codice della postazione **al momento dell'azione**
  (colonne «ID prenotazione» e «postazione» della pagina). Ricercabili dal campo «Ricerca».
- **Tipo**: `varchar(50)` / `varchar(100)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.

### metadata
- **Legacy**: — (nuovo)
- **Descrizione**: dettagli specifici dell'azione (campi cambiati, importo, motivo del fallimento).
- **Tipo**: `jsonb`.
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: mai password, token, OTP, dati di carta; dati personali solo se servono a capire l'azione.

### client_ip / client_user_agent
- **Legacy**: — (nuovo)
- **Descrizione**: IP e user-agent della richiesta che ha generato l'azione (eventi `security` e
  `platform`).
- **Tipo**: `inet` / `varchar(400)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: dato personale; segue la retention della categoria.

### trace_id
- **Legacy**: — (nuovo)
- **Descrizione**: `traceId` della richiesta, per risalire ai log diagnostici
  ([`docs/15-logs.md`](../../../progettazione/15-logs.md)).
- **Tipo**: `varchar(55)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: presenti per uniformità di schema. `created_at` coincide con `recorded_at`;
  `updated_at` non cambia mai; `deleted_at` non è usato.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: —.
- **Note**: registro append-only; la sola rimozione è l'hard-delete della retention.

## Enumerazioni introdotte

| Enum PG | Valori |
|---|---|
| `audit.entry_category` | `operator`, `security`, `platform` |
| `audit.source_module` | `identity`, `catalog`, `booking`, `payment`, `fiskaly`, `fiscalprint`, `notification`, `switchboard`, `chatbot`, `privacy`, `infra`, `audit` |
| `audit.entry_outcome` | `succeeded`, `failed` |
| `audit.actor_kind` | `user`, `system`, `external_client`, `anonymous` |

## Valutazione JSON

`metadata jsonb` è l'unica colonna libera: il contenuto varia per `action_code` e non è mai usato per
filtrare. Tutto ciò che la pagina filtra o mostra in colonna è tipizzato.

## Suggerimenti per le prestazioni

- **Alto volume, sola scrittura**: ogni azione di ogni operatore genera una riga. Partizioni mensili
  create in anticipo dal job di manutenzione delle partizioni
  ([`docs/06-partizioni-manutenzione.md`](../../../progettazione/06-partizioni-manutenzione.md)).
- La pagina filtra **sempre** per `property_id` e intervallo di date: `ix_..._property_time` più il
  *partition pruning* su `occurred_at` leggono poche partizioni.
- Paginazione a **offset** (`page`/`pageSize` ≤ 100, decisione del 2026-10-13, come nel resto del progetto):
  il `COUNT` e le pagine lontane costano di più, ma su una lista sempre filtrata per stabilimento i volumi
  restano contenuti. Se un giorno servisse, l'indice `(property_id, occurred_at DESC, id DESC)` regge anche
  una paginazione a cursore.
- La ricerca libera su `actor_name_snapshot`, `booking_number_snapshot`, `spot_label_snapshot` è sempre
  combinata col filtro di stabilimento e data; un indice trigram si valuta solo se i tempi reali lo
  richiedono.
- **Retention**: categorie con durate diverse nella stessa partizione → `DELETE` per categoria dal
  motore di retention; la partizione intera si elimina (`DROP`) quando supera la durata massima fra le
  categorie.

## Implementazione .NET

Registro append-only `AuditTrailEntry` del modulo `Module.Audit`, schema `audit`: entità immutabile,
creata solo dal gestore dell'evento `AuditEntryRecorded`, nessun metodo di modifica. **Non** implementa
`IAuditable` né `ISoftDeletable` (stesso pattern di
[`privacy_compliance_audit_logs`](../privacy/privacy_compliance_audit_logs.md)).

- Il contratto `AuditEntryRecorded` (con le costanti dei codici azione) sta in **Shared**: i moduli
  pubblicano, nessuno referenzia `Module.Audit`.
- Scrittura con `INSERT … ON CONFLICT (source_event_id, occurred_at) DO NOTHING`.
- **Permessi DB**: l'utente applicativo ha su `audit` solo `INSERT` e `SELECT`; il `DELETE` è del solo
  ruolo del job di retention.
- Partizionamento dichiarato in migration con `migrationBuilder.Sql(...)` (EF non lo modella), come per
  le tabelle partizionate di Booking.

### Note di migrazione

Tabella **nuova**, nessun dato legacy. Policy di retention per categoria in `privacy_retention_policies`
(`security` 6 mesi, `platform` 12 mesi, `operator` 12 mesi — [D-66](../_domande-committente.md)), seminate
dalla migration `AddAuditRetentionPolicies` di Privacy.
