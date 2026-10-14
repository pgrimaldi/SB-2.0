# Ottimizzazioni DB — schema `analisi-post/db/`

> Raccolta schematica di tutte le ottimizzazioni proposte dopo la revisione trasversale delle 152 schede
> in [`../analisi-post/db/`](../analisi-post/db/) (12 domini), incrociate con le finestre di retention
> **confermate dal titolare** in [`82-retention-data.md`](82-retention-data.md) (che aggiorna
> [`80-retention.md`](80-retention.md)). Nessun file di `analisi-post/db/` è stato modificato: questo è
> l'elenco di interventi da valutare, prioritizzare e riportare nelle singole schede.
>
> Lo schema di partenza è già maturo (enum PG nativi, indici parziali `WHERE deleted_at IS NULL`,
> `public_id` UUIDv7, `numeric(12,2)`, alcuni `EXCLUDE USING gist`/`enum[]` già corretti). Qui ci sono
> solo i **gap** individuati.

---

## 0. Glossario minimo

| Concetto | In breve |
|---|---|
| **Partizionamento per range** (`PARTITION BY RANGE`) | Una tabella logica diventa N tabelle fisiche (partizioni) per intervallo di una colonna (di solito temporale). Vantaggi: *partition pruning* (una query con filtro sulla colonna ignora le partizioni fuori range) e retention a costo zero (`DROP PARTITION`). Vincolo: ogni `PRIMARY KEY`/`UNIQUE` deve includere la colonna di partizionamento. |
| **BRIN** (Block Range Index) | Indice che riassume min/max per **blocchi di pagine fisiche** (non riga per riga come un B-tree). Pochi KB anche su milioni di righe; utile su colonne temporali correlate all'ordine di inserimento (tabelle append-only). Non sostituisce un B-tree per lookup puntuali. |
| **`DROP`/`DETACH PARTITION`** | Operazione di metadata (millisecondi): elimina un'intera partizione senza generare dead tuple né bloat, a differenza di un `DELETE`/`UPDATE` di massa che lascia righe morte da recuperare con `VACUUM`. |
| **Generated column** (`GENERATED ALWAYS AS (...) STORED`) | Colonna calcolata dal motore DB da altre colonne **della stessa riga**, nella stessa transazione di scrittura: non può mai andare fuori sincrono, elimina job di riconciliazione. Non può referenziare altre righe/tabelle. |
| **`EXCLUDE USING gist`** | Vincolo che generalizza `UNIQUE` a "nessuna coppia di righe si sovrappone" — tipico per intervalli di date (richiede estensione `btree_gist`). |
| **`ON CONFLICT` (upsert)** | `INSERT ... ON CONFLICT (...) DO UPDATE/NOTHING` — idempotenza garantita dal DB, senza la finestra di race condition di un `SELECT` poi `INSERT` applicativo. |
| **Covering index (`INCLUDE`)** + **index-only scan** | Colonne "appoggiate" nell'indice (non nella chiave) che permettono di rispondere alla query senza toccare la tabella (heap), se tutte le colonne richieste sono nell'indice. |

---

## 1. Retention confermata dal titolare → impatto sulle tabelle DB

Deltas rispetto alla baseline tecnica di `80-retention.md`, dalle scelte del titolare in
[`82-retention-data.md`](82-retention-data.md). Solo le voci che cambiano una raccomandazione tecnica
(finestra di partizionamento/retention); mappatura da nome legacy a tabella del nuovo schema.

| Categoria (legacy) | Tabella nuovo schema | Retention proposta | **Scelta titolare** | Impatto tecnico |
|---|---|---|---|---|
| Testata prenotazione ombrellone | `booking_reservations` | online N-1 | **N-2** ✅ confermato | Estendere la finestra online da N-1 a N-2 — dimensionare di conseguenza |
| Dettaglio prenotazione | `booking_lines` e satelliti | fine S + 1 S (~N-1) | **N-2** ✅ confermato 2026-09-17 (risolve Q1) | Stessa finestra della testata — dimensionare il job annuale `SeasonalArchive` per 3 stagioni online |
| Assenze / rivendita del posto / riserve morbide (blocchi mappa) | `booking_resale_offers`, `booking_spot_holds` e satelliti | N-1 | **N-2** ✅ confermato 2026-09-17 (risolve Q2) | Idem |
| Contatori consumo inventario | `booking_resource_consumptions`/`_availability_daily` | fine S + 1 S | "ok" + "aggregato mensile" confermato (nessun N-2 qui) | Nessun cambio: **season-based**, come già proposto in §3 |
| Spese/addebiti POS | `payment_pos_charges`/`_charge_items` | archivia 24 mesi, cancella a 10 anni (**obbligo legale**: "concorrono al conto/fiscale") | **10 anni** ✅ confermato 2026-09-17 (risolve Q4 — corregge la precedente indicazione "5 anni") | Nessun rischio legale residuo: dimensionare l'archivio per 10 anni, non 5 |
| Carte salvate cliente | `payment_customer_gateway_profiles` | cancella a chiusura account o 24 mesi inattività | **solo dopo 24 mesi, anche se l'account chiude prima** — tempistica **N-2** ✅ confermato 2026-09-17 | Il job di cleanup non deve scattare sull'evento "chiusura account", solo sul timer di inattività (~N-2 stagioni) |
| Scontrino elettronico | `fiskaly_receipts` | online 24 mesi poi archivio, 10 anni totali | **online 36 mesi** | La "tabella calda" prevista in §9.1 è più grande del previsto — dimensionare per 3 anni, non 2 |
| Log dialogo SIGN IT | `fiskaly_receipt_logs` | 12-24 mesi (ok/ko differenziati) | **12 mesi flat** | Semplifica la policy: una sola finestra, non serve necessariamente la sotto-partizione per `result` proposta in §8 |
| Log invii email — corpo | `notification_deliveries.rendered_body` | NULL dopo 90 giorni | **150 giorni** | Il job `TruncateEmailLogBodies` va ritarato a 150gg, non 90 |
| Notifiche in-app | `notification_inapp_messages` | 6-12 mesi | **6 mesi** (estremo inferiore) | Finestra di retention/partizionamento più stretta del previsto |
| Registro chiamate ricevute | `switchboard_voice_call_records` | 12 mesi | **6 mesi** | Finestra dimezzata rispetto alla baseline |
| Log diagnostico chiamata | `switchboard_voice_call_diagnostics` | 30-90 giorni | **30 giorni** (estremo inferiore) | Coerente con la partizione settimanale già proposta nella scheda |
| Log webhook WhatsApp | `switchboard_messaging_wa_event_logs` | 6-12 mesi, poi anonimizza | **6 mesi, poi cancella direttamente** (salta l'anonimizzazione) | Job più semplice: hard-delete diretto, non due fasi |
| Log auth/sicurezza applicativa | *(nuovo, non in analisi-post/db esplicitamente)* | 6-12 mesi | **6 mesi** | — |
| Anagrafica cliente finale | `identity_customer_customers` | anonimizza 24-36 mesi, cancella 10 anni | **anonimizza dopo 48 mesi** | Finestra più lunga — non cambia le raccomandazioni tecniche (RLS/trgm) già proposte per questa tabella |
| Account staff | `identity_access_users` | anonimizza +24 mesi da disabilitazione | **cancella dopo 12 mesi dalla revoca** ✅ confermato 2026-09-17 (risolve Q5): per l'audit fiscale basta il riferimento allo **stabilimento**, non serve preservare l'identità dell'**utente** | Hard-delete sicuro: le righe fiscali/ledger referenziano `property_id`, non serve conservare `identity_access_users` oltre i 12 mesi |
| Prenotazioni di soli servizi | `booking_reservations` (`reservation_kind = service`) | fine S | **N-1** ✅ confermato 2026-09-17 (risolve Q6 — "S-1" era un refuso) | Nessun cambio tecnico, solo lessicale |
| Liste contatti marketing | `notification_marketing_contact_lists`/`_contact_list_members` | 24 mesi da ultimo contatto | **clock a livello di lista** (`updated_at` della lista o ultima campagna), non per singolo membro | Il campo di riferimento per il purge è sulla **testata lista**, non sulla riga di appartenenza — indicizzare/partizionare di conseguenza |

**Non impattate**: report hotel (24 mesi "ok"), transazioni online/rimborsi (24 mesi poi archivio "ok"), account fiscale Fiskaly (archivia "ok"), cash register Fiskaly (cancella "ok"), stampa fiscale RT (12 mesi "ok"), accettazioni contrattuali (troncamento IP 24 mesi "ok"), preferiti consumer (24 mesi "ok"), configurazione stabilimento (2 anni "ok"), campagne/automazioni (24 mesi da ultimo invio "ok"), lead (12 mesi "ok"), storico chatbot (12 mesi "ok"), log applicativi (90gg poi S3 "ok").

**Nota su C2 — cataloghi master globali**: il titolare introduce una distinzione — *"se è solo dello stabilimento, cancellare alla sospensione dell'account; se serve per il portale web, conservare"*. Da applicare a `catalog_map_icons`/`catalog_service_amenities`/`catalog_service_type_icons` (master SuperAdmin, sempre conservati) vs eventuali configurazioni property-specific.

**Anagrafica pubblica stabilimento — ✅ risolto 2026-09-17 (Q3)**: alla cessazione, la scheda **resta pubblicata** ma **ridotta alle sole informazioni essenziali** (nome, foto, posizione, descrizione base) e **con la prenotazione online disattivata**; il resto della configurazione segue comunque la cascata ordinaria (`+3 mesi` cancellazione config). La cascata `DeprovisionClosedProperty` va quindi disegnata con un passo intermedio esplicito su `catalog_bathhouses`/`catalog_properties`: **non** cancellare/deindicizzare la scheda, ma disattivare `is_online_sales_suspended`-style i canali di prenotazione e ridurre i campi esposti pubblicamente prima che la configurazione sottostante venga rimossa a +3 mesi.

### 1.1 Domande al committente — tutte risolte 2026-09-17

Le contraddizioni interne e le tensioni segnalate in questa sezione sono state chiarite dal titolare in
[`retention-chiarimenti.docx`](retention-chiarimenti.docx); risposte riportate in
[`82-retention-data.md`](82-retention-data.md) (celle con nota "chiarito 2026-09-17"). Riepilogo:

| # | Domanda | Risposta |
|---|---|---|
| Q1 | Dettaglio prenotazione: N-1 o N-2? | **N-2** |
| Q2 | Assenze/rivendita/blocchi mappa: N-1 o N-2? | **N-2** |
| Q3 | Scheda pubblica dopo cessazione: rimuovere o mantenere, e con quali campi? | **Mantenere, ma solo info essenziali — niente prenotazione online** |
| Q4 | Spese POS: 10 anni (obbligo legale) o 5 anni? | **10 anni** (corretta la precedente indicazione a 5) |
| Q5 | Cancellazione account staff: come preservare l'audit trail fiscale? | **Basta il riferimento allo stabilimento, non all'utente** |
| Q6 | Cosa significa "S-1" sulle prenotazioni di soli servizi? | **Refuso, era N-1** |
| — | Tempistica sulla mappatura utente→Stripe Customer (risposta evidenziata in giallo nel docx originale) | **N-2** |

---

## 2. Partizionamento

**⚠️ Tabella rivista 2026-09-18 dopo il ricalcolo a regime di §2.1** — molte righe qui sotto erano
"proposte in scheda" guardando solo il totale storico del dump; il ricalcolo che tiene conto della
retention (§2.1) mostra che la maggior parte **non lo giustifica più**. La colonna "Partizionare?" è il
verdetto aggiornato; la colonna "Stato" resta la valutazione originale (utile per capire il ragionamento
di partenza).

| Tabella | Colonna | Retention effettiva (titolare) | Partizionare? | Stato / note |
|---|---|---|---|---|
| [`booking_resource_consumptions`](../analisi-post/db/booking/booking_resource_consumptions.md) / [`_availability_daily`](../analisi-post/db/booking/booking_resource_availability_daily.md) | `consumption_date` | fine S + 1 S, job annuale `SeasonalArchive` | ✅ **Sì — annuale, anno solare**, deciso 2026-09-18 | Unico caso dove il volume a regime resta grande (§2.1: ~1,02M righe, la retention non lo riduce). Risolve anche "le stagioni sono per-property, non un confine unico" partizionando per anno di calendario |
| [`fiskaly_receipts`](../analisi-post/db/fiskaly/fiskaly_receipts.md) | `created_at` | online **36 mesi**, mai cancellare prima di 10 anni | 🟡 **Deciso ma senza urgenza** (§9.1: calda+archivio S3) | Rompe 3 `UNIQUE`; ma dati reali (§2.1: 2.596 righe, ≤~7.800 anche nel caso peggiore) dicono che l'esecuzione può aspettare |
| [`notification_deliveries`](../analisi-post/db/notification/notification_deliveries.md) | `created_at` | corpo **150gg**, riga 12 mesi | ❌ **No** (rivisto 2026-09-18) | A regime ~61k righe (§2.1) — non giustifica l'infrastruttura di partizionamento; basta `DELETE` a lotti (una tantum) + `DELETE` mensile + BRIN |
| [`notification_inapp_messages`](../analisi-post/db/notification/notification_inapp_messages.md) | `created_at` | **6 mesi** | ❌ **No** (rivisto 2026-09-18) | A regime ~40k righe (§2.1) — stesso ragionamento di sopra |
| [`payment_transaction_charges`](../analisi-post/db/payment/payment_transaction_charges.md) | `occurred_at` | online 24 mesi, 10 anni totali | ❌ **No** | Anche nello scenario peggiore (nessuna riduzione da retention) resta ~41,5k righe (§2.1) — sotto soglia |
| [`payment_ledger_entries`](../analisi-post/db/payment/payment_ledger_entries.md) | `occurred_at` | online 24 mesi, 10 anni totali | ❌ **No** — confermato 2026-09-18 | Nessun conteggio diretto disponibile per la tabella consolidata, ma chiuso senza ulteriore verifica |
| [`payment_ledger_entry_lines`](../analisi-post/db/payment/payment_ledger_entry_lines.md) | *(nessuna colonna propria)* | segue la testata | ❌ **No** (segue `payment_ledger_entries`) | Se la testata non si partiziona, non serve nemmeno aggiungere la colonna temporale denormalizzata ipotizzata in precedenza |
| [`notification_marketing_campaign_recipients`](../analisi-post/db/notification/notification_marketing_campaign_recipients.md) | `created_at` | 6 mesi | ❌ **No** — confermato 2026-09-18 | Nessun conteggio diretto trovato (solo 25 campagne totali nel dump), chiuso senza ulteriore verifica |
| [`switchboard_voice_call_diagnostics`](../analisi-post/db/switchboard/switchboard_voice_call_diagnostics.md) | `created_at` | **30 giorni** | ✅ Già così in scheda | **0 righe nel dump di produzione** (§2.1) — nessuna urgenza pratica oggi, ma il disegno (già partizionato) resta un buon modello di riferimento per altre tabelle se servisse in futuro |
| [`switchboard_messaging_wa_event_logs`](../analisi-post/db/switchboard/switchboard_messaging_wa_event_logs.md) | `received_at` | 6 mesi | ✅ Già così in scheda | Idem — 0 righe nel dump oggi |
| `switchboard_voice_call_records` | (da verificare) | **6 mesi** | ❌ No | 0 righe nel dump di produzione (§2.1) — non serve verificare oltre per ora |
| [`fiskaly_receipt_logs`](../analisi-post/db/fiskaly/fiskaly_receipt_logs.md) | `created_at` | **12 mesi flat** | ❌ No | Solo 8.063 righe totali (§2.1) — volume trascurabile |
| [`privacy_compliance_audit_logs`](../analisi-post/db/privacy/privacy_compliance_audit_logs.md) | `occurred_at` | 1-2 anni | ❌ No | Tabella nuova, nessuno storico — scheda stessa dice "non necessario al lancio" |
| [`identity_access_refresh_tokens`](../analisi-post/db/identity/identity_access_refresh_tokens.md) | `issued_at` | 30 giorni | ❌ No | Base utenti di sole 6.680 righe (§2.1) — **BRIN** è la scelta giusta (§3), non partizionamento |
| [`chatbot_conversation_turns`](../analisi-post/db/chatbot/chatbot_conversation_turns.md) | `created_at` | non ancora definita (D-54) | ❌ No | 0 righe nel dump di produzione |

**Non partizionare** (si autolimitano in dimensione, overhead di gestione non giustificato): `identity_access_user_auth_challenges` (retention 1h), `catalog_pricing_suggested_prices` (solo dati futuri, righe passate cancellate subito).

### 2.1 Verifica sui dati reali, corretta per le politiche di retention (aggiornata 2026-09-18)

**Perché la prima versione era fuorviante**: i conteggi del dump sono **storico grezzo accumulato senza
retention** (il legacy non cancellava quasi nulla). Una volta applicata la finestra di retention decisa
(§1), la maggior parte di quello storico **non resterà mai online** — quindi il numero giusto da guardare
per decidere "questa tabella è grande/piccola **a regime**" non è il totale del dump, ma:

```
volume_a_regime ≈ tasso_di_accumulo (righe/mese) × durata_finestra_di_retention (mesi)
tasso_di_accumulo = righe_totali_nel_dump / mesi_di_storico coperti da quelle righe
```

Dove la scheda legacy riporta un intervallo `createdAt` esplicito (min…max), lo uso per calcolare il
tasso; dove non lo riporta, lo dico esplicitamente invece di indovinare.

**`booking_resource_consumptions`** — retention decisa: **N-1 ≈ 24 mesi** online (§1, "ok" confermato, non N-2):

| Fonte legacy | Righe | Intervallo `createdAt` | Mesi coperti | Tasso/mese | Volume a regime (×24 mesi) |
|---|---|---|---|---|---|
| `booking_items_counts_details` | 461.251 | 2025-01-09 → 2026-09-08 | ~20 | ~23.060 | **~553.000** (il legacy aveva già "spaccato" l'archivio a luglio 2026 — 435.427 righe già spostate altrove — questa tabella oggi copre già più o meno una finestra di retention) |
| `services_booking_counts` | 467.821 | *non riportato nella scheda* | n/d | **non stimabile** | uso il totale come limite superiore prudenziale |
| `additional_items_counts_details` | 3.055 | 2024-07-14 → 2026-09-11 | ~26 | ~118 | ~2.830 |
| `hourly_service_booking_management` | 2.381 | 2021-08-31 → 2026-09-07 | ~60 | ~40 | ~953 (era senza alcuna potatura da 5 anni) |

**Volume a regime stimato: ~1.020.000 righe** (dominato da `services_booking_counts`, dato non stimabile
con precisione). **La conclusione non cambia**: a differenza delle tabelle notification sotto, qui la
finestra di retention (24 mesi) è **paragonabile o inferiore** allo storico già presente — la retention
non "sgonfia" questa tabella, resta comunque il caso di volume più alto. 🔴 **Priorità confermata,
invariata rispetto alla stima grezza.**

**`notification_inapp_messages`** (`notification_details`) — retention decisa: **6 mesi** (§1):

`createdAt` 2022-09-27 → 2026-09-08 = **~48 mesi** per 317.300 righe → tasso ≈ **6.610 righe/mese**.
**Volume a regime (×6 mesi) ≈ 39.700 righe** — **8 volte meno** del totale storico (317.300).

**`notification_deliveries`** (`email_logs`) — retention decisa: riga intera cancellata a **12 mesi**:

Lettura parziale 2021-03-12 → 2024-06-28 (19.594 righe), storico completo stimato fino alla data del
dump (~2026-09) → **~66 mesi** per ~335.000 righe stimate → tasso ≈ **5.076 righe/mese**.
**Volume a regime (×12 mesi) ≈ 60.900 righe** — **~5,5 volte meno** del totale storico stimato.

**⚠️ Correzione importante alla conclusione precedente**: per queste due tabelle notification, **non è
più corretto dire "vanno partizionate perché sono enormi (300k+ righe)"** — a regime, dopo che la
retention è a pieno effetto, resteranno tabelle di **40-60k righe**, tutt'altro che enormi. **Il vero
motivo per partizionarle non è la dimensione a regime, è l'operazione di transizione + il job ricorrente**:
- **Una tantum**: applicare la retention per la prima volta significa cancellare **~278.000** righe da
  `notification_inapp_messages` e **~274.000** da `notification_deliveries` tutte insieme — esattamente
  il caso "`DELETE` di massa vs `DROP PARTITION`" di §4, dove la differenza conta davvero.
  Suddividere in partizioni **prima** di lanciare la potatura iniziale permette di eliminare lo storico
  in eccesso con `DROP PARTITION` invece di un `DELETE` di 250k+ righe.
- **Ricorrente**: anche a regime (40-60k righe stabili), il job mensile continua a dover togliere
  all'incirca quanto entra ogni mese (6-7k righe) — un volume che un `DELETE` mensile gestirebbe comunque
  senza problemi *a quel punto*, ma partizionare mensilmente rende comunque il job un `DROP PARTITION`
  invece di una scansione con condizione su `created_at` ogni volta.

**Rivisto ulteriormente (2026-09-18)**: a un esame più onesto, anche questa motivazione "più debole" non
regge abbastanza da giustificare l'infrastruttura di partizionamento (§14.1: DDL a mano, gestione delle
partizioni future). Quantificando meglio l'operazione una tantum: 275.000 righe cancellate su una tabella
che a regime ne avrà 40-60.000 generano un bloat temporaneo — ma restano dati "leggeri" (log/notifiche,
poche centinaia di byte a riga): l'ordine di grandezza fisico è **decine-centinaia di MB**, non i GB di
`booking_resource_consumptions`. Un `DELETE` **a lotti** (es. 5.000 righe alla volta con una breve pausa
tra un lotto e l'altro, tecnica standard per una pulizia una tantum) tiene sotto controllo sia il lock
sia il ritmo con cui `autovacuum` recupera lo spazio, senza bisogno di partizioni. E il job **ricorrente**
a regime (togliere 6-7k righe/mese da una tabella di 40-60k) è un `DELETE` mensile ordinario, proporzionato
alla dimensione della tabella.

**Conclusione corretta**: per `notification_inapp_messages`/`notification_deliveries` **non partizionare**
per ora. Basta: (a) BRIN sulla colonna temporale (già raccomandato in §3, a costo quasi zero), (b) uno
script di pulizia **a lotti** per la potatura una tantum in fase di migrazione/rollout della retention,
(c) il normale job mensile `DELETE ... WHERE created_at < ...` a regime. Si rivaluta il partizionamento
solo se la crescita reale post-lancio supera sensibilmente questa proiezione.

**`payment_transaction_charges`** — retention decisa: **24 mesi** online:

| Fonte legacy | Righe | Intervallo | Mesi | Tasso/mese | Volume a regime (×24 mesi) |
|---|---|---|---|---|---|
| `online_transaction_details` | 38.240 (**92% del totale**) | *non riportato nella scheda* | n/d | **non stimabile** | non calcolabile — è la componente dominante |
| `transactions_payments` | 2.539 | dal 2025-07-15 (integrazione recente) | ~14 | ~181 | ~4.350 (**ma tutto lo storico attuale ha già meno di 24 mesi** → oggi nessuna riga verrebbe ancora potata) |
| `card_transactions` | 708 | 2022-04-26 → 2026-09-06 | ~53 | ~13 | ~321 |

**Non posso completare questo calcolo con precisione** (manca l'intervallo `createdAt` di
`online_transaction_details`), **ma per la domanda "serve partizionare?" non serve più**: anche nello
scenario peggiore — **zero** riduzione da retention, tutte le 41.500 righe restano a regime — è comunque
un volume che non giustifica il partizionamento (un ordine di grandezza sotto
`booking_resource_consumptions`, e anche sotto i 40-60k a regime delle tabelle notification sopra, per le
quali ho appena concluso che *nemmeno lì* serve). **Verdetto: non partizionare**, indipendentemente dal
dato mancante — quel dato servirebbe solo per dimensionare un eventuale export/archiviazione a 24 mesi
(§14.1, opzione "tabella calda + archivio"), non per decidere se serve partizionare.

**`fiskaly_receipts`** — retention: mai cancellare prima di 10 anni, solo hot/cold a 36 mesi:

Nessun intervallo `createdAt` riportato nella scheda `fiskaly_receipt.md`. Anche nello scenario più
sfavorevole (tutte le 2.596 righe accumulate in un solo anno), estrapolare a 36 mesi darebbe **al
massimo ~7.800 righe** — resta un volume trascurabile in qualunque scenario ragionevole. 🟢 **Nessuna
urgenza, confermata con margine ancora più ampio di quanto stimato prima.**

**Tabelle a volume nullo/trascurabile, non impattate dal ricalcolo** (nessuna retention da applicare a
uno storico che non esiste o è già minimo): `fiskaly_receipt_logs` (8.063, ~3×`fiskaly_receipts`),
`identity_access_refresh_tokens`/`user_auth_challenges` (base utenti 6.680 account), `switchboard_*`/
`chatbot_conversation_turns` (0 righe nel dump), `privacy_compliance_audit_logs` (tabella nuova).

**Riepilogo del ricalcolo — risposta diretta: "solo `booking_resource_consumptions` è utile da
partizionare significativamente?" → Sì, dopo questa revisione è così.**

| Tabella | Volume storico (dump) | Volume **a regime** (dopo retention) | Partizionare? |
|---|---|---|---|
| `booking_resource_consumptions` | ~935.000 | **~1.020.000 (non si riduce)** | ✅ **Sì** — unico caso dove la dimensione a regime resta grande |
| `notification_inapp_messages` | 317.300 | **~39.700** | ❌ **No** — a regime è una tabella piccola; basta `DELETE` a lotti (una tantum) + `DELETE` mensile ordinario + BRIN |
| `notification_deliveries` | ~335.000 | **~60.900** | ❌ **No** — stesso ragionamento |
| `payment_transaction_charges` | ~41.500 | non calcolabile con precisione, ma già piccolo anche nello scenario peggiore (nessuna riduzione) | ❌ **No** — sotto soglia indipendentemente dal dato mancante |
| `fiskaly_receipts` | 2.596 | **≤ ~7.800** anche nello scenario peggiore | ❌ **No** — nessuna urgenza (resta valido il disegno hot+S3 già deciso, ma senza fretta) |

*Limite dell'analisi*: i tassi sono calcolati su intervalli riportati nelle schede (spesso letture
parziali con stima estrapolata) — utili per capire l'**ordine di grandezza** e correggere una conclusione
altrimenti fuorviante (specialmente per le tabelle notification), non per dimensionare capacità/storage
con precisione. Per quello serve una query reale (`MIN`/`MAX`/`COUNT` per intervallo) sul dump o
sull'ambiente di produzione corrente.

## 3. Indici BRIN

Priorità **inversa** al partizionamento: BRIN dove il partizionamento è rimandato/non ancora deciso, non dove è già pianificato (lì basta il B-tree dentro ogni partizione piccola).

| Tabella.colonna | Perché ora e non partizionamento |
|---|---|
| `identity_access_refresh_tokens.issued_at`/`expires_at` | Scheda rimanda il partizionamento; il job `DELETE` (30gg) beneficia subito di un BRIN, costo quasi nullo |
| `fiskaly_receipt_logs.created_at` | Mentre si decide/implementa la policy (12 mesi flat, §1), BRIN accelera comunque gli scan di retention |
| `privacy_compliance_audit_logs.occurred_at` | Scheda dice "non necessario al lancio" — BRIN è l'opzione a costo pressoché nullo per ora |
| `chatbot_conversation_turns.created_at` | Quando si chiuderà D-54 e si introdurrà la retention, un BRIN preesistente è già pronto |

**Non serve**: `identity_access_user_auth_challenges` e `catalog_pricing_suggested_prices` (tabelle già naturalmente piccole).

---

## 4. Retention: `DELETE`/`UPDATE` di massa vs `DROP PARTITION`

Un job notturno riduce solo la **contesa** con il traffico utenti — **non** elimina il costo strutturale: ogni riga cancellata/aggiornata genera comunque WAL (pesa su replica/backup PITR indipendentemente dall'orario) e lascia dead tuple che `autovacuum` deve recuperare (bloat che si trascina nel giorno successivo). `DROP`/`DETACH PARTITION` è un'operazione di metadata: il costo non esiste proprio, di giorno o di notte.

**Non è un problema "solo" dei job**: i job di retention sono l'esempio più visibile perché fanno
`DELETE`/`UPDATE` sulle quantità di righe più grandi, ma la fisica descritta sopra (WAL, dead tuple,
bloat, autovacuum) si applica a **qualunque** `DELETE`/`UPDATE` di massa nel sistema, non solo a quelli
schedulati per la retention. E gli effetti non restano confinati al job stesso:
- **Query concorrenti sulla stessa tabella** (comprese quelle di utenti reali, non del job) diventano più
  lente finché `autovacuum` non recupera lo spazio — un `DELETE` di retention di notte può degradare le
  prestazioni della tabella per ore il giorno dopo.
- **Replica/backup**: il volume di WAL generato va comunque replicato a eventuali read replica e
  archiviato per il PITR, indipendentemente da quando è stato prodotto.
- **Spazio su disco**: senza `VACUUM FULL` (che richiede un lock esclusivo, costoso) lo spazio liberato
  da un `DELETE` resta riutilizzabile dalla stessa tabella ma non torna al sistema operativo — il file
  fisico della tabella non si restringe.

Quindi sì, l'orario del job aiuta a non litigare con il traffico *mentre gira*, ma non evita il costo
successivo che si riversa su tutto il resto del sistema (query utente, replica, storage).

**Rivisto 2026-09-18 dopo §2.1**: solo `booking_resource_consumptions` giustifica ancora il
partizionamento. Per le tabelle notification, la correzione non è più "partizionare", ma "cancellare a
lotti" — vedi §2.1 per il ragionamento completo (a regime sono tabelle di 40-60k righe, non abbastanza
grandi da giustificare l'infrastruttura di partizionamento).

| Tabella | Job oggi | Correzione |
|---|---|---|
| `notification_deliveries` | `TruncateEmailLogBodies`: `UPDATE rendered_body=NULL` a 150gg + `DELETE` riga a 12 mesi | ❌ Non partizionare (§2.1). Per la potatura **una tantum** (~274k righe da rimuovere al primo avvio della retention): `DELETE` **a lotti** (es. 5.000 righe/iterazione con una breve pausa) invece di un unico `DELETE` massivo. A regime, il job mensile ordinario basta |
| `notification_inapp_messages` | `PurgeInAppNotifications`: `DELETE` mensile (6 mesi) | ❌ Non partizionare (§2.1). Stessa correzione: `DELETE` a lotti per la potatura una tantum (~278k righe), poi job mensile ordinario |
| `notification_marketing_campaign_recipients` | `PurgeMarketingSnapshots`: `DELETE` mensile (6 mesi) | ❌ Non partizionare — chiuso, confermato dal titolare (§2) |
| `identity_access_refresh_tokens` | `DELETE` periodico (30gg) | BRIN (§3), non partizionamento — volume non lo giustifica (base utenti 6.680, §2.1) |
| `booking_resource_consumptions`/`_availability_daily` | `SeasonalArchive`: **una volta l'anno**, volume enorme (intera stagione, tutte le property) | ✅ **Unico caso confermato** — partizionare per anno solare: l'unico job critico annuale diventa `DROP PARTITION` istantaneo |
| `fiskaly_receipt_logs` | Non ancora implementato | ❌ Non partizionare — solo 8.063 righe totali (§2.1). Un semplice job `DELETE` mensile basta |
| `switchboard_voice_call_diagnostics`/`_wa_event_logs` | — | ✅ Già corretto in scheda (`DROP PARTITION`) — ma 0 righe nel dump di produzione oggi (§2.1), nessuna urgenza pratica |
| `identity_access_user_auth_challenges` | `DELETE` ogni pochi minuti (1h) | Nessun intervento: cadenza frequente + lotti piccoli → bloat trascurabile per natura |

---

## 5. Generated column (`GENERATED ALWAYS AS ... STORED`)

**✅ Già implementate** (modello di riferimento):

| Tabella.colonna | Formula |
|---|---|
| `booking_resource_consumptions.period_mask` | `CASE booking_period WHEN 'full_day' THEN 3 WHEN 'morning' THEN 1 WHEN 'afternoon' THEN 2 END` |
| `booking_resource_availability_daily.available_qty` | `total_qty - booked_qty` |
| `catalog_pricing_discounts.code_normalized` | `upper(code)` |

### 5.1 Lista finale — ✅ deciso 2026-09-18: solo dove non esiste una modifica manuale in-place

Criterio applicato: convertire in generated column **solo** le colonne dove ho verificato nelle schede
che (a) la formula dipende solo da colonne della **stessa riga**, e (b) non esiste un percorso di
correzione che modifichi l'importo **in-place** (le correzioni contabili, dove esistono, sono già
modellate come una **nuova riga** — pattern append-only — non come un `UPDATE` del valore esistente).

**✅ Da applicare** (verificato: nessuna modifica manuale in-place trovata):

| Tabella.colonna | Formula | Verifica fatta |
|---|---|---|
| `payment_wallet_transaction_items.line_total` | `quantity * unit_price + variants_surcharge` | `payment_wallet_transactions` ha `source = 'adjustment'` ("rettifica manuale") come **nuovo movimento**, mai un `UPDATE` di un movimento esistente. Inoltre la scheda segnala un bug storico confermato nel legacy (`totalPrice` spesso `null`/incoerente) — la giustificazione più solida di tutta la lista |
| `payment_ledger_entries.gross_amount` | `net_amount + vat_amount` | `entry_kind = 'adjustment'` ("rettifica manuale al posto della riscrittura legacy") è anch'esso un **nuovo movimento** append-only, mai un `UPDATE` in-place |
| `payment_transaction_charges.gross_amount` | `paid_amount + pending_amount (+ hold_amount)` | Scheda: "la tabella è append/stato, **non si soft-elimina**"; i rimborsi sono righe separate in `payment_transaction_refunds`, non una modifica di questa riga |
| `payment_pos_charge_items.net_amount`/`vat_amount` | `net = round(line_total/(1+vat_rate/100),2)`; `vat = line_total - net` | `payment_pos_charges.status` passa a `voided` con "un documento di storno" (nuova riga `payment_ledger_entries`), mai una modifica dell'importo esistente |
| `booking_service_lines.line_total` | `(unit_price + variant_surcharge) * quantity` | Tutti e tre i fattori sono colonne della stessa riga, "totale bloccato" — nessun campo di override trovato nella scheda |

**❌ Esclusa** (motivo diverso, non "modifica manuale": dipendenza cross-tabella):

| Tabella.colonna | Perché non è un candidato valido |
|---|---|
| `booking_line_additional_items.line_total` | La formula è `quantity × unit_price × (nr. giorni se date_scope = whole_stay)` — ma il "numero di giorni" per `whole_stay` **non è una colonna di questa tabella**, si legge dalle date della riga padre `booking_lines` (`start_date`/`end_date`). Una generated column Postgres può referenziare **solo colonne della stessa riga**: qui servirebbe prima denormalizzare un conteggio giorni sulla riga stessa, altrimenti resta necessariamente calcolata dall'applicazione |

**❌ Non è un candidato**: `payment_ledger_entry_lines` → `payment_ledger_entries` (Σ righe = totale testata) è un'aggregazione **cross-riga**, non calcolabile con una generated column (serve un trigger).

**Queste colonne resteranno "solo lette" dal codice, o oggi vengono anche scritte?** Dipende dalla
colonna, ed è un punto da chiarire prima di convertirle:

| Colonna | Oggi | Dopo la conversione |
|---|---|---|
| `payment_pos_charge_items.net_amount`/`vat_amount` | **Scritte** dall'applicazione al momento dell'acquisizione dati POS | Diventano di sola lettura: PostgreSQL **rifiuta** un valore esplicito su una colonna `GENERATED ALWAYS` — va rimosso il codice che oggi le valorizza in fase di `INSERT` |
| `payment_wallet_transaction_items.line_total` | **Scritta** dall'applicazione | Idem — va tolto dal codice di inserimento |
| `payment_transaction_charges.gross_amount` | Scritta dall'applicazione | Idem |
| `payment_ledger_entries.gross_amount` | Scritta dall'applicazione | Idem |
| `booking_service_lines.line_total` | Scritta **una sola volta** alla creazione della riga, come "snapshot bloccato" — non più toccata dopo | Funzione pura di `unit_price`/`variant_surcharge`/`quantity` della stessa riga, nessun campo di override trovato |

**✅ Tutte verificate — vedi §5.1** per l'elenco finale con le fonti esatte controllate nelle schede (nessuna modifica manuale in-place trovata per le 4 tabelle payment; `booking_line_additional_items.line_total` esclusa per un motivo diverso — dipendenza dalla riga padre, non modifica manuale).

In generale: convertire una colonna oggi scritta dall'app in una generated column è un **cambio di
contratto**, non solo una migration — va rimosso il codice applicativo che la valorizza (altrimenti
l'`INSERT`/`UPDATE` fallisce con un errore Postgres), e in EF Core la property va ri-mappata con
`HasComputedColumnSql(..., stored: true)` + `ValueGeneratedOnAddOrUpdate()` (stesso pattern già mostrato
per `booking_number_sequences` in §9.5, ma lì il valore non è mai stato scritto dall'app: qui lo è).

---

## 6. `ON CONFLICT` (upsert/idempotenza)

| Dove | Stato |
|---|---|
| `fiskaly_receipt_sequences`, `fiscalprint_serial_numbers` | ✅ Già `INSERT ... ON CONFLICT (...) DO UPDATE ... RETURNING` — corretto |
| `catalog_pricing_suggested_prices` | ✅ Già upsert per riga — corretto |
| Idempotenza webhook Stripe (`payment_transaction_charges`) | ✅ **Verificato nel codice `back-pre` 2026-09-18** (vedi sotto) |

**Perché `ON CONFLICT` batte un `SELECT` poi `INSERT` applicativo**: elimina la finestra di race condition tra due webhook concorrenti per lo stesso evento (non atomico in app), ed è una sola query invece di due.

### 6.1 Quali eventi Stripe sono gestiti oggi — verificato in `back-pre/app/controllers/stripeWebhookHandler.js`

Solo **due** tipi di evento hanno una logica reale (il resto del file, righe commentate, mostra
tentativi precedenti mai attivati):

- `payment_intent.succeeded` — gestito, distingue "Platform" vs "Connected Account" verificando la firma
  con due possibili secret (`STRIPE_WEBHOOK_SECRET`/`CONNECTED_WEBHOOK_SECRET`).
- `checkout.session.completed` — gestito, solo per pagamenti via `payment_link` (aggiorna
  `bookingsSendPaymentSummary`).

**Qualunque altro evento Stripe** (`charge.refunded`, `payment_intent.payment_failed`,
`account.updated`, ecc.) arriva, la firma viene verificata, ma **non c'è nessun `if` che lo intercetta**:
la funzione arriva dritta al `return res.json({ received: true })` finale — Stripe riceve una risposta
200 (quindi non lo rimanda), ma **non succede nulla**. In particolare: **i rimborsi non sembrano essere
confermati via webhook** in questo codice — presumibilmente gestiti sincronamente nella chiamata API
diretta di rimborso, non via conferma asincrona.

**Conseguenza per lo schema `payment.webhook_events` (§6)**: basta una tabella che copra
`payment_intent.succeeded` e `checkout.session.completed` per replicare l'idempotenza che serve oggi.
Se il nuovo sistema aggiungerà la gestione di altri eventi (rimborsi, dispute, aggiornamenti account
Connect) rispetto al legacy, la tabella generica (`provider`, `external_event_id`, `payload`) già
proposta in §1.5 li copre comunque senza bisogno di ridisegnarla — non serve una tabella per tipo di
evento.

---

## 7. Covering index (`INCLUDE`) / index-only scan

Serve quando **insieme**: (1) query molto frequente, (2) filtra su poche colonne ma restituisce altre poche colonne strette (mai `text`/`jsonb` larghi), (3) niente `UPDATE` frequenti su quelle colonne extra.

**⚠️ Correzione rispetto alla prima versione**: `catalog_map_spots` **non** è il punto giusto per questo
indice — l'avevo proposto assumendo che il rendering griglia interrogasse la tabella a ogni caricamento
pagina, ma non è così: `catalog_map_spots.md` §Prestazioni dice esplicitamente che il rendering mappa è
"servito da un read model" — e `_config-snapshot-proposta.md` §4.2 conferma che è lo snapshot
`beach_map` (in `catalog_config_snapshots.payload`, jsonb) a contenere **già tutte le postazioni**
(`id`, `publicId`, `label`, `type`, `sectorId`, `gridRow`/`gridColumn`, `isArea`, `isBookable` — esattamente
le colonne che avevo proposto in `INCLUDE`), sia per il gestionale sia per il sito pubblico. La tabella
sorgente `catalog_map_spots` viene quindi interrogata dal vivo **solo**:
- dal **job di rebuild** dello snapshot quando una postazione cambia (frequenza = eventi di modifica, non
  caricamenti pagina — molto più bassa);
- dalla **validazione in scrittura** quando lo staff sposta/crea una postazione nell'editor mappa (query
  puntuale tipo `EXISTS(... WHERE property_id=... AND grid_row=... AND grid_column=...)`, che non
  beneficia di un `INCLUDE` perché non deve restituire altre colonne).

Per questi due usi l'indice `(property_id, grid_row, grid_column)` già esistente basta: **l'`INCLUDE`
qui non è giustificato**, andrebbe solo a gonfiare l'indice senza un beneficio di lettura reale.

`catalog_pricing_price_list_period_prices`/`sector_prices` **restano invece un caso valido**: a
differenza della mappa, il calcolo del prezzo **al momento della prenotazione** (date/settore/durata
specifici scelti dal cliente) non è enumerabile in un blob statico — lo snapshot `price_catalog` esiste
ma serve la **navigazione/visualizzazione** del listino prima della scelta (`_config-snapshot-proposta.md`
§4.4), mentre il calcolo autoritativo del preventivo (`POST /bookings/quote`, principio "ricalcolo
server-side obbligatorio" di `docs/03-gestione-dati.md`) interroga le tabelle normalizzate dal vivo per
garantire un prezzo sempre fresco — confermato in `catalog_pricing_price_list_period_prices.md`
§Prestazioni: *"questa tabella serve al calcolo **corrente**, non alla ricostruzione storica"*. Qui
l'`INCLUDE` ha senso.

| Dove ha senso | Dove NON conviene |
|---|---|
| `catalog_pricing_price_list_period_prices`/`sector_prices` — lookup motore prezzo **al momento del preventivo/prenotazione**, chiamato per ogni riga | ~~`catalog_map_spots`~~ — il rendering griglia legge dallo snapshot `beach_map`, non dalla tabella (vedi correzione sopra) |
| | `notification_deliveries` coda (`ix_..._pending`) — il worker fa comunque `FOR UPDATE`/`SKIP LOCKED`, tocca la heap a prescindere |
| | Tabelle con `UPDATE` frequenti sulle colonne che si vorrebbero includere (l'indice si gonfia a ogni scrittura) |

---

## 8. `EXCLUDE USING gist`

| Tabella | Oggi | Problema |
|---|---|---|
| `catalog_pricing_tariff_periods` | solo `CHECK (ends_on >= starts_on)` | Non impedisce la sovrapposizione fra righe diverse della stessa stagione |
| `catalog_property_online_allowed_periods` / `catalog_property_online_stay_limits` | idem | idem |
| `catalog_service_sale_item_time_slots` | solo `UNIQUE(sale_item_id, sort_order)` | Due fasce orarie sovrapposte sullo stesso servizio non sono bloccate |
| `booking_resale_offers` | nessuna protezione | Il legacy aveva questo identico bug (33 righe disallineate nel dump) |
| `identity_account_subscription_periods` | `UNIQUE` parziale solo su periodo aperto | Due periodi *chiusi* possono sovrapporsi |

Richiede estensione `btree_gist` + colonna generata `daterange`/`tsrange` + `EXCLUDE USING gist (chiave WITH =, range WITH &&)`.

**✅ Confermato dal titolare 2026-09-18**: due `period_group_key`/gruppi diversi **non devono mai
sovrapporsi** (regola di business confermata, nessuna eccezione nota) — il vincolo `EXCLUDE` può essere
implementato senza ulteriori verifiche di dominio. `btree_gist` è **confermata disponibile** sull'istanza
di destinazione — nessuna verifica AWS residua (chiude anche §14.5). Pronto per implementare, **ma resta
da fare la bonifica dei dati storici** prima della migrazione: se nei dati legacy esistono già periodi
sovrapposti (probabile, dato che nel legacy non c'era alcun vincolo a impedirlo), il vincolo va applicato
**dopo** aver ripulito quei casi, altrimenti la migration fallisce.

---

## 9. Fix puntuali

### 9.1 `fiskaly_receipts` — partizionamento vs 3 `UNIQUE`

**✅ Deciso 2026-09-18**: tabella calda (rolling 36 mesi, vincoli `UNIQUE` intatti, nessun partizionamento
fisico) + **archivio su S3** (non una tabella Postgres separata) — coerente con l'archiviazione del PDF
già prevista dalla scheda. Un job mensile esporta su S3 (stesso bucket/lifecycle Glacier del PDF) le
righe più vecchie di 36 mesi e le rimuove dall'operativa dopo verifica dell'export.

**Implicazione da tenere presente** (aggiornata rispetto alla prima stesura, che ipotizzava un archivio
Postgres interrogabile): con l'archivio su **S3**, una query che deve coprire uno storico oltre i 36 mesi
(es. un controllo fiscale a 8 anni) **non** è più un semplice `UNION ALL` SQL fra due tabelle — richiede
un processo di **recupero** dedicato (es. un job che re-importa temporaneamente l'intervallo di date
richiesto da S3 in una tabella di lavoro, oppure una query Athena/Glue direttamente su S3) prima di poter
rispondere. Questo è un componente operativo in più da progettare (non complesso, ma va previsto: chi lo
richiama, con quale SLA, chi ha i permessi) — non l'avevo scorporato chiaramente nella prima versione.

### 9.2 Retention "da fare" — `fiskaly_receipt_logs` / sequenze

- `fiskaly_receipt_logs`: ~~partizioni mensili da subito~~ **rivisto 2026-09-18**: dati reali (§2.1) mostrano solo 8.063 righe totali — un semplice job `DELETE` mensile (12 mesi flat, confermato dal titolare) basta, nessun partizionamento necessario.
- `fiskaly_receipt_sequences` / `fiscalprint_serial_numbers`: restano piccole per natura anche su 10 anni (1 riga/cassa/giorno) — nessun partizionamento necessario, solo un job annuale non urgente.

### 9.3 `fiskaly_response` (jsonb) — TOAST/compressione — ✅ testato su dati reali (2026-09-18)

**Dati usati**: `dump-prod-legacy/u811491364_summer_booking_fiskaly_receipt.sql` (dump reale, dump completo
della tabella legacy) — parsate le 8 istruzioni `INSERT` con un piccolo parser Python (rispetta stringhe
con escape MySQL), estratti tutti i valori della colonna `fiskaly_response`: **2609 righe totali, 2584
con payload non-`NULL`** (le altre sono verosimilmente scontrini non fiscali/annullati prima della
risposta SIGN IT). Tutti e 2584 sono JSON validi.

**Dimensioni reali**: da 1.108 a 3.006 byte, media 1.600 byte, mediana 1.574 — payload piccoli (non
"pesanti" come temevo inizialmente), con struttura JSON-in-JSON (il campo `operation` è una stringa JSON
escapata che da sola occupa ~80% del payload).

**Compressione testata riga per riga** (ogni valore compresso indipendentemente, replicando come TOAST
comprime — nessun dizionario condiviso fra righe):

| Algoritmo | Dimensione media compressa | Rapporto medio | Risparmio totale su tutte le righe |
|---|---|---|---|
| **zlib/deflate livello 6** (proxy di `pglz`, il default Postgres su `STORAGE EXTENDED`) | 763 B | 0,481 | **52,3%** |
| zlib livello 9 (massima compressione deflate) | 763 B | 0,481 | 52,3% (nessuna differenza pratica dal livello 6) |
| **LZ4 frame, livello default** (`SET COMPRESSION lz4`, PG14+) | 1.085 B | 0,683 | 32,2% |
| LZ4 frame, alta compressione (livello 9/HC) | 1.040 B | 0,655 | 35,0% |

**Risultato — opposto a quanto avevo ipotizzato**: per questo payload specifico, **il default di
Postgres (pglz via `STORAGE EXTENDED`, proxy misurato con zlib) comprime meglio di LZ4** (~52% di
risparmio contro ~32-35%) — LZ4 privilegia la velocità sul rapporto di compressione, e su payload
piccoli (1-3 KB) con struttura JSON-in-JSON non ha abbastanza materiale per sfruttare il suo vantaggio di
velocità senza perdere in rapporto.

**Decisione**: **non serve nessuna azione speciale** su `fiskaly_response` — il comportamento di default
di PostgreSQL (`STORAGE EXTENDED`, che è già il default per `jsonb`, con compressione `pglz`) coglie già
il ~52% di risparmio disponibile automaticamente, senza bisogno di impostare `SET COMPRESSION lz4` (che
sarebbe **peggiore**, misurato) né `STORAGE EXTERNAL` (che disattiverebbe la compressione, perdendo quel
52% per niente — il payload **è** comprimibile, l'ipotesi "poco comprimibile" era sbagliata). L'unica
azione che resta valida è quella già indipendente dalla compressione: **(a)** un DTO di proiezione per le
liste in EF che esclude la colonna, per non trasferire byte inutili quando non serve il dettaglio
completo della risposta SIGN IT.

### 9.3bis Altri campi `jsonb` del nuovo schema — verificati tutti (2026-09-18)

Cercati tutti i campi `jsonb` nelle 152 schede (31 file li menzionano). La maggior parte sono falsi
positivi: la scheda dice esplicitamente **"Nessun `jsonb`"** nella sezione Valutazione JSON (il campo
legacy è stato smontato in colonne/tabelle tipizzate) — coerente col principio del progetto "JSON evitato
come modello di dominio, ammesso solo come passthrough di sistema esterno". Filtrando questi, restano 4
campi `jsonb` reali di tipo "payload grezzo di terzi" (la stessa categoria di `fiskaly_response`):

| Campo | Legacy | Dati reali disponibili | Risultato |
|---|---|---|---|
| `fiskaly_receipts.fiskaly_response` | `fiskaly_receipt.fiskaly_response` | ✅ dump, 2584 righe | **Testato** — vedi sopra, comprimibile (~52%), nessuna azione |
| `fiskaly_receipt_logs.fiskaly_log_payload` | `fiskaly_receipt_log.fiskaly_log_messages` | ✅ dump, ma solo **15 righe non-NULL su 8.125** (valorizzato solo sui log di errore reale, rarissimo) | **Testato** — media **97 byte**: troppo piccolo perché TOAST tenti mai la compressione, a prescindere dalla configurazione. `lz4` peggiora addirittura la dimensione (overhead fisso del frame su input minuscoli, -23%). **Nessuna azione — il campo è irrilevante per dimensione** |
| `fiscalprint_receipts.device_response` | `booking_receipts.receipt_object` | ✅ dump, 4.487 righe (tutte non-NULL) | **Testato** — media **228 byte**, tecnicamente comprimibile al 33% (zlib) ma troppo piccolo per superare la soglia a cui Postgres tenta la compressione di un attributo TOAST-abile in pratica. **Nessuna azione — stesso motivo di sopra** |
| `catalog_config_snapshots.payload` | *(nuovo, nessun corrispondente legacy)* | ❌ Nessun dato reale — tabella derivata, non esiste nel dump legacy | **Non testabile ora.** È l'unico altro candidato plausibile: la scheda descrive `price_catalog` come potenzialmente "centinaia di pacchetti × periodi" per stabilimenti con listini grandi — potrebbe arrivare a **decine di KB**, un ordine di grandezza sopra gli altri tre. Da ri-testare con dati reali dopo il primo rilascio (stessa metodologia: estrarre un campione di payload reali e confrontare `pglz` di default vs `lz4`) |

**Conclusione**: `fiskaly_response` resta l'**unico** campo dove la compressione ha un effetto misurabile
oggi. Gli altri due passthrough legacy testabili (`fiskaly_log_payload`, `device_response`) sono
strutturalmente troppo piccoli per essere impattati da qualunque scelta di storage/compressione — non
serve configurarli in modo speciale, il default va bene. `catalog_config_snapshots.payload` è l'unico
da tenere d'occhio dopo il lancio, quando ci saranno dati reali da misurare.

**Candidati con payload reale ma non testabili per mancanza di dati**: `switchboard_voice_call_diagnostics`
(`raw_payload`/`handler_result`/`body_payload`) e `switchboard_messaging_wa_event_logs`
(`components`/`wa_body_params`) sono jsonb di payload esterno (Telnyx/WhatsApp) strutturalmente simili ai
casi sopra, ma **0 righe nel dump di produzione** (§2.1 — la feature non è live su questo ambiente) —
nessun dato reale su cui testare oggi.

### 9.4 `fiscalprint_printer_departments` — semplificazione

**⚠️ Correzione 2026-09-18**: avevo scritto che il modulo fiscalprint (C4bis) fosse "candidato a
dismissione" — è **sbagliato**, ed è un errore mio (non della documentazione di progetto, che è corretta):
`docs/84-dizionario.md` dice esplicitamente *"si mantiene come componente separato — decisione di
business del committente, indipendente dall'uso attuale: **non è candidata a dismissione**"* (meccanismo
legacy alternativo allo scontrino elettronico Fiskaly, usato da 3-4 stabilimenti — decisioni D-41/D-42 in
`_domande-committente.md`, già chiuse). L'etichetta "candidata a dismissione" nel progetto è riservata ad
altre feature (alcune di C6 — `notification_marketing_event_reminders`,
`notification_establishment_claim_requests` — e a C7/switchboard, quest'ultima solo per assenza di UI,
non per mancanza d'uso), **mai** a fiscalprint. Corretto il ragionamento sotto di conseguenza.

**In cosa consiste esattamente l'opzione (a)?** Oggi `fiscalprint_printer_departments` ha **3 indici**
sulla stessa coppia di colonne concettuale:
1. `UNIQUE(printer_id, dept_number)` — il vero identificativo di business (ogni reparto numerato è unico
   per stampante).
2. `UNIQUE(printer_id, vat_rate)` — vincolo **candidato**, non ancora confermato: assume che ogni
   aliquota IVA sia associata a **un solo** reparto per stampante (mai due reparti con la stessa aliquota
   sulla stessa cassa).
3. Un indice **non-unique** `(printer_id, vat_rate)` — stessa forma del #2, ma senza vincolo di unicità,
   usato dal "resolver" (il codice che, per stampare uno scontrino, deve trovare il reparto giusto dati
   stabilimento/stampante e aliquota IVA della riga).

**Opzione (a)** = confermare col committente/con chi conosce le regole del dominio RT che l'assunzione
dietro il vincolo #2 è sempre vera (un'aliquota non può mai essere duplicata su due reparti della stessa
stampante). Se confermato, l'indice #3 diventa superfluo: il vincolo `UNIQUE` #2 serve **da solo** sia a
garantire la regola sia a rispondere alla query del resolver — si elimina la ridondanza di avere due
indici quasi identici.

**✅ Deciso 2026-09-18: lasciare il terzo indice.** Non si persegue l'opzione (a) (nessuna richiesta di
conferma al committente, nessuna rimozione dell'indice non-unique) — si mantiene la ridondanza accettata
già presente nella scheda. Restano quindi 3 indici come oggi.

(L'opzione (b) — collassare tutto in `departments jsonb` su `fiscalprint_printer_configs` — resta
un'alternativa indipendente e più radicale, non scelta qui: si tocca solo se in futuro si vuole
semplificare oltre il livello degli indici.)

### 9.5 `booking_number_sequences` — due round-trip

Sostituire `SELECT ... FOR UPDATE` + `UPDATE` separato con:
```sql
UPDATE booking.booking_number_sequences
SET last_sequence = last_sequence + 1, updated_at = now()
WHERE property_id = @propertyId
RETURNING number_prefix, last_sequence;
```
Stessa garanzia di lock per-property, metà round-trip, nessuna modifica di schema.

**Come farlo con EF Core.** `ExecuteUpdateAsync` (l'API EF Core "nativa" per gli update bulk via LINQ,
senza caricare l'entità) **non è utilizzabile qui**: restituisce solo il numero di righe modificate, mai
i valori scritti — e serve proprio `last_sequence` appena incrementato. Va usata SQL grezza tramite
`Database.SqlQuery<T>` (introdotta in EF Core 8, pienamente supportata in EF Core 10 su questo stack),
che mappa il risultato di una query — anche una `UPDATE ... RETURNING` — su un record non registrato nel
modello, senza passare da un `DbSet`:

```csharp
// Module.Booking.Domain — nessuna registrazione nel modello, è solo il "guscio" del risultato
public sealed record BookingNumberSequenceResult(string NumberPrefix, long LastSequence);

// Module.Booking.Infrastructure.Services.BookingNumberGenerator
public sealed class BookingNumberGenerator : IBookingNumberGenerator
{
    private readonly BookingDbContext _context;

    public BookingNumberGenerator(BookingDbContext context) => _context = context;

    public async Task<string> NextAsync(Guid propertyId, CancellationToken ct)
    {
        var result = await _context.Database
            .SqlQuery<BookingNumberSequenceResult>($"""
                UPDATE booking.booking_number_sequences
                SET last_sequence = last_sequence + 1, updated_at = now()
                WHERE property_id = {propertyId}
                RETURNING number_prefix AS "NumberPrefix", last_sequence AS "LastSequence"
                """)
            .SingleAsync(ct);

        return $"{result.NumberPrefix}-{result.LastSequence:D10}";
    }
}
```

Note pratiche:
- L'interpolazione `{propertyId}` diventa un parametro Npgsql vero (protetto da SQL injection), non
  concatenazione di stringa — `SqlQuery<T>` usa `FormattableString` esattamente come
  `FromSqlInterpolated`.
- Gli alias `AS "NumberPrefix"`/`AS "LastSequence"` nella `RETURNING` sono necessari perché `SqlQuery<T>`
  mappa le colonne per **nome esatto** sulle proprietà del record: senza alias, `number_prefix`
  (snake_case) non troverebbe `NumberPrefix` (PascalCase) — a differenza delle entità mappate in
  `OnModelCreating`, qui non c'è un `HasColumnName` a fare da tramite.
- Va eseguita **nella stessa transazione/`SaveChanges`** della creazione della prenotazione (stesso
  `DbContext`, nessun `BeginTransaction` esplicito necessario se già dentro una Unit of Work/transazione
  ambient) — identico comportamento di oggi rispetto a un eventuale rollback della prenotazione dopo che
  il numero è già stato "bruciato".
- `BookingNumberSequenceResult` non va aggiunto al modello (`OnModelCreating`) né come entità né come
  keyless entity: `SqlQuery<T>` materializza direttamente dal `DbDataReader`, non richiede
  configurazione.

---

## 10. `property_id` da denormalizzare

**✅ Già fatto** (precedenti): `booking_line_additional_items`, `booking_discounts`, `fiskaly_receipt_lines`, `fiskaly_receipt_payments`, `notification_marketing_campaign_recipients`.

**🟠 Candidati reali** (1 hop dal genitore, query "per stabilimento" già documentate):

| Tabella | Hop attuale | Motivo |
|---|---|---|
| `payment_ledger_entry_lines` | `entry_id → payment_ledger_entries.property_id` | Export contabile + report IVA per riga |
| `payment_pos_charge_items` | `charge_id → payment_pos_charges.property_id` | Report IVA addebiti POS + report unificato per articolo |
| `payment_wallet_transaction_items` | `transaction_id → payment_wallet_transactions.property_id` | Solo "eventuale report" — priorità bassa, volume minimo (~50 righe) |

**❌ Scartati dopo verifica**: `booking_guests`/`booking_participants` (query per-cliente, non per-stabilimento), `catalog_pricing_price_list_items`/`sector_prices` (uso calcolo prezzo, non aggregazione), `privacy_data_subject_request_module_outcomes` (cruscotto esplicitamente per-modulo).

---

## 11. Viste / materialized view da aggiungere

**⚠️ Verifica di freschezza dati (2026-09-18)**: rileggendo con occhio critico, **due delle quattro
proposte MATERIALIZED avevano un problema reale** di staleness che la prima stesura non affrontava
esplicitamente — corretto sotto, punto per punto (§11.1 e §11.3).

### 11.0 Sono richieste reali o "dati che forse serviranno"? — verificato 2026-09-18

Nessuna di queste proposte viene da un mockup o da una lista di funzionalità richieste esplicitamente:
sono nate dal mio ragionamento durante la revisione dello schema. Prima di proporle come lavoro da fare,
ecco quanto sono effettivamente **ancorate a qualcosa di già documentato** nelle schede, verificato ora
con una ricerca mirata, contro quanto è una mia ipotesi ("potrebbe servire"):

| Vista proposta | Ancorata a | Stato |
|---|---|---|
| §11.1 `payment.mv_property_daily_summary` | Nessun riferimento diretto trovato. Esiste una vista simile già prevista — "crediti/incassi/non pagati" (ex `creditDetails`) — ma è **una cosa diversa**: quella è già risolta con una `SELECT` sulla sola `booking_reservations` (nessun bisogno di aggregare 3 tabelle payment), descritto nella scheda `booking_reservations.md` §Prestazioni | 🟡 **Speculativa** — nessuna richiesta trovata per un cruscotto "incassi aggregati per giorno" a livello payment |
| §11.2 `fiskaly.v_daily_cash_closure` | **Sì** — `fiskaly_receipt_payments.md` cita esplicitamente l'indice `(property_id, payment_type)` per "il report incassato per forma di pagamento (**chiusura cassa**)" | ✅ **Grounded** — è un requisito reale già annotato nella scheda |
| §11.3 aggiornamento `catalog_search_bathhouses.min_price` | **Sì** — la colonna esiste già nel disegno approvato (`_config-snapshot-proposta.md` §4.6); qui ho solo chiarito **come** mantenerla (non è una vista nuova, vedi correzione già fatta) | ✅ **Grounded** — non è nemmeno una proposta nuova, è un chiarimento implementativo |
| §11.4 `identity.v_active_subscriptions` | **Parzialmente** — esiste un **batch notturno reale** ("abbonamenti scaduti", `cron-disableproperty.js` nel legacy, già con un proprio indice dedicato `ix_..._expiry_scan` nella scheda `identity_account_subscriptions.md`), ma è un **job**, non una dashboard: nessun riferimento a una UI che elenchi gli abbonamenti trovato nelle schede | 🟡 **Parzialmente speculativa** — il job non ha bisogno di questa vista (ha già il suo indice), l'ho estesa a "dashboard" senza una conferma che esista |
| §11.5 `privacy.v_pending_requests_by_module` | **Sì** — è la ragione stessa per cui `privacy_data_subject_request_module_outcomes` è stata tenuta relazionale invece di un jsonb (serve una query trasversale indicizzata, motivata esplicitamente nella scheda) | ✅ **Grounded** |
| §11.6 `notification.mv_campaign_stats` | **Sì, e di più**: `notification_marketing_email_campaigns.md` ha **già** le colonne `recipients_total`/`delivered_count`/`failed_count`, descritte come "contatori di esito, aggiornati per evento da `notification_deliveries`" | ✅ **Già risolto nella scheda** — la mia proposta di "contatori denormalizzati" (dopo la correzione di staleness) è esattamente ciò che è **già disegnato**; non è né un gap né una vista da creare, **ritiro questa voce dall'elenco** |

**✅ Deciso dal titolare 2026-09-18**: procede **solo §11.2** (`fiskaly.v_daily_cash_closure`) — è l'unica
vista da implementare. Tutte le altre restano **non per ora**, incluse quelle grounded
(`privacy.v_pending_requests_by_module`, §11.5): non per mancanza di giustificazione, ma per scelta di
non investirci ora. §11.3 resta comunque un chiarimento implementativo valido (non è una vista, non
richiede una decisione separata). §11.6 resta ritirata (già risolta nella scheda, indipendentemente da
questa decisione).

### 11.1 `payment.mv_property_daily_summary` (MATERIALIZED, con correzione)

**Problema nella prima stesura**: avevo proposto refresh **notturno**. Ma se questo cruscotto serve
anche a rispondere a "quanto abbiamo incassato **oggi**" (un bisogno operativo tipico per un gestore),
un refresh notturno mostrerebbe **zero o il dato di ieri** per tutta la giornata corrente — dato
sbagliato, non solo stantio.

**Correzione**: la vista materializzata copre solo i **giorni chiusi** (`day < CURRENT_DATE`), refresh
notturno va benissimo per quelli (non cambiano più). Per **"oggi"** si affianca una query **live**, separata
e leggera, sulle sole righe del giorno corrente (poche decine/centinaia di righe per property, non un
problema di prestazioni) — il cruscotto fa `UNION ALL` fra la vista (storico) e la query live (oggi). Se
in futuro serve "oggi" sempre aggiornato al minuto anche per lo storico, si valuta un refresh più
frequente (es. ogni ora) invece che notturno, ma **non** una vista materializzata per il giorno corrente.

**A cosa serve**: oggi un cruscotto "incassi di oggi/del mese per stabilimento" dovrebbe interrogare **3
tabelle separate** con logiche di aggregazione diverse (incassi online, POS, borsellino) e sommarle in
applicazione. Questa vista pre-aggrega i tre flussi in un'unica riga per giorno/property.

**Da dove vengono i campi:**

| Colonna della vista | Sorgente | Aggregazione |
|---|---|---|
| `property_id`, `day` | comune a tutte e tre le fonti | `GROUP BY property_id, date_trunc('day', occurred_at)` |
| `online_net`, `online_vat`, `online_gross` | `payment_ledger_entries.net_amount`/`vat_amount`/`gross_amount` | `SUM(...)` |
| `pos_net`, `pos_vat` | `payment_pos_charge_items.net_amount`/`vat_amount` via `payment_pos_charges.property_id` | `SUM(...)` |
| `wallet_amount` | `payment_wallet_transactions.amount` | `SUM(...)` |
| `transaction_count` | conteggio righe di tutte e tre le fonti | `COUNT(*)` |

**Refresh**: notturno (`REFRESH MATERIALIZED VIEW CONCURRENTLY`, richiede un `UNIQUE INDEX` sulla vista
su `(property_id, day)`) — un ritardo di un giorno è accettabile per un cruscotto riepilogativo, non per
un saldo in tempo reale (quello resta `booking_reservations.paid`/`balance_due`, già mantenuto per
proiezione da evento, vedi la scheda `booking_reservations.md`).

### 11.2 `fiskaly.v_daily_cash_closure` (VIEW o MATERIALIZED)

**A cosa serve**: la "chiusura cassa" di fine turno/giornata richiesta dalla normativa sui corrispettivi
telematici (totale incassato per cassa, per giorno, diviso per aliquota IVA e forma di pagamento) — oggi
richiederebbe un join+aggregazione manuale ogni volta.

**Da dove vengono i campi:**

| Colonna della vista | Sorgente |
|---|---|
| `cash_register_id`, `fiscal_date` | `fiskaly_receipts.cash_register_id`, `created_at::date` |
| `payment_type` (contante/carta/altro) | `fiskaly_receipt_payments.card_kind`/`voucher_kind`/discriminatore pagamento |
| `vat_code`, `vat_rate` | `fiskaly_receipt_lines.vat_code` → join `fiskaly_vat_codes.rate` |
| `total_amount` | `SUM(fiskaly_receipt_payments.amount)` |
| `receipt_count` | `COUNT(DISTINCT fiskaly_receipts.id)` |

**Refresh**: `VIEW` semplice (non materializzata) se il volume per cassa/giorno resta piccolo — è
un'interrogazione a chiusura turno, non un caricamento pagina ad alta frequenza; passare a MATERIALIZED
solo se il profiling mostra che l'aggregazione live è troppo lenta su casse con migliaia di scontrini/giorno.

**Verificata 2026-09-18**: questa è l'unica delle quattro proposte MATERIALIZED (in realtà proposta come
`VIEW` semplice fin dall'inizio) che non ha problemi di freschezza — la "chiusura" è per definizione
un'azione puntuale richiesta esplicitamente dall'operatore a fine turno, non un dato che deve essere
sempre aggiornato in tempo reale in background.

### 11.3 `catalog_search_bathhouses.min_price` — **non** una materialized view (corretto 2026-09-18)

**Errore nella prima stesura**: avevo proposto una `MATERIALIZED VIEW catalog.mv_package_min_price`. È
sbagliato per un motivo strutturale, non solo di freschezza: `REFRESH MATERIALIZED VIEW` ricalcola
**sempre l'intera vista**, tutte le property insieme — non esiste un refresh incrementale "solo la riga
che è cambiata" su una MV nativa Postgres. Se il prezzo cambia spesso (Revenue Management può
aggiornare prezzi anche più volte al giorno su property diverse), un "refresh su evento" richiederebbe
ricalcolare il minimo di **tutte** le ~175 property a ogni singola modifica di una sola di esse — spreco
enorme rispetto al bisogno reale.

**Correzione**: non serve un oggetto vista/materialized view. `catalog_search_bathhouses.min_price` è
già descritta come una colonna reale aggiornata "dallo stesso handler che aggiorna il resto della card
di ricerca" — cioè un semplice:
```sql
UPDATE catalog.search_bathhouses
SET min_price = (
    SELECT MIN(...) FROM catalog.pricing_price_list_sector_prices ...
    WHERE property_id = @propertyId AND ...
)
WHERE bathhouse_public_id = @bathhousePublicId;
```
eseguito **una sola volta, per la sola property toccata**, dallo stesso handler applicativo che già
gestisce l'evento di cambio prezzo — nessun nuovo oggetto DB, nessun refresh su tutte le altre 174
property. Ho tolto questa voce dall'elenco delle viste da creare: non è un gap, è un chiarimento su come
implementare qualcosa che la scheda già prevede correttamente.

### 11.4 `identity.v_active_subscriptions` (VIEW)

**A cosa serve**: dashboard SuperAdmin "quali stabilimenti hanno l'abbonamento attivo/in scadenza/sospeso" — oggi richiederebbe join manuale fra abbonamento corrente e piano.

**Da dove vengono i campi:**

| Colonna della vista | Sorgente |
|---|---|
| `property_id`, `ends_at`, `remaining_days_balance`, `term_type` | `identity_account_subscriptions` |
| `plan_code` | `identity_account_license_plans.code` |
| `status` (derivato) | `CASE WHEN ends_at < now() THEN 'expired' WHEN ends_at < now() + interval '30 days' THEN 'expiring_soon' ELSE 'active' END` |

**Refresh**: nessuno, è una `VIEW` (non materializzata) — tabella piccola (poche centinaia di stabilimenti), query sempre live.

### 11.5 `privacy.v_pending_requests_by_module` (VIEW)

**A cosa serve**: cruscotto compliance "quali richieste GDPR sono ancora pendenti, per modulo" — è
esattamente il motivo per cui `privacy_data_subject_request_module_outcomes` **non** è stata collassata
in jsonb (serve una query trasversale indicizzata, non dentro un singolo documento).

**Da dove vengono i campi:**

| Colonna della vista | Sorgente |
|---|---|
| `request_id`, `subject_kind`, `request_type`, `requested_at` | `privacy_data_subject_requests` |
| `module_code`, `outcome`, `responded_at` | `privacy_data_subject_request_module_outcomes` |
| filtro | `WHERE outcome IS NULL OR outcome = 'pending'` |

**Refresh**: nessuno, `VIEW` — è un cruscotto di servizio, volume basso.

### 11.6 `notification.mv_campaign_stats` — ❌ ritirata 2026-09-18, non serve

Proposta iniziale scartata **due volte**: prima corretta da MV a contatori denormalizzati (stesso
ragionamento di §11.3), poi scoperto che **quei contatori esistono già nella scheda**:
`notification_marketing_email_campaigns.md` ha già `recipients_total`/`delivered_count`/`failed_count`,
esplicitamente descritte come "contatori di esito, aggiornati per evento da `notification_deliveries`" —
esattamente il pattern che avrei consigliato. Nessuna vista o contatore da aggiungere: **è già
disegnato correttamente**. Tolta dall'elenco delle viste da creare (§11.0).

---

## 12. Path "calcolo prezzo prenotazione" — dettaglio

### 12.1 Il problema esatto

Per **una** riga di preventivo (un pacchetto, in un settore, per un certo numero di notti, a partire da
una certa data) il motore prezzo deve interrogare in cascata, nell'ordine di precedenza descritto nelle
schede.

**Correzione 2026-09-18 sul riferimento alle domande aperte**: avevo scritto genericamente "D-15/D-15b" —
verificato il contenuto esatto in `_domande-committente.md`:
- **D-15b è già ✅ risolta** ("Prezzi per numero di giorni impostati manualmente — VERIFICATO"): non è più
  una domanda aperta, è la conferma del modello adottato per `catalog_pricing_price_list_stay_prices`
  (solo gli scostamenti manuali sono persistiti, il resto lo ricalcola il motore prezzo).
- **D-15 — ✅ risolta 2026-09-18 verificando il codice legacy** (`front-pre/src/services/BookingCalcService.js`,
  funzione `getReducedPrices`, e i chiamanti in `LocationItemListChildren.js`):
  ```js
  export const getReducedPrices = async (currentRow, getObj, selectedDates) => {
    for (const price of getObj.details.setReducedPrice) {
      if (/* date della finestra combaciano esattamente con selectedDates */) {
        const matchingData = price.additionalData.find(data => data.name === currentRow);
        if (matchingData) return Number(matchingData.value) || 0;   // ← nessun calcolo, solo il valore
      }
    }
    return 0;
  };
  ```
  - **`value` è un prezzo assoluto** (opzione a), non una percentuale né un delta: la funzione lo
    restituisce direttamente come `finalPrice`, senza combinarlo aritmeticamente con nessun'altra tariffa.
    Prova diretta: nel chiamante (`LocationItemListChildren.js`), `getReducedPrices(currentRow, ...)` è
    **l'alternativa** (`else if`) a `getSeasonalDatesPrices(currentRow, ...)` — la normale tariffa
    stagionale di base — entrambe assegnate allo stesso `finalPrice`: se `value` fosse una percentuale o
    un delta servirebbe comunque combinarlo con la tariffa base, cosa che il codice non fa mai.
  - **`name` si riferisce al settore**, non a un articolo/servizio/pacchetto: `currentRow` (l'argomento
    con cui si confronta `data.name`) è lo stesso identificatore usato da `getSeasonalDatesPrices` per
    selezionare la tariffa stagionale **per settore** (`mainPrices[].name`), e altrove nello stesso file
    (`selectedRowsData?.includes(currentRow)`, verifica "pacchetto limitato a settori selezionati") è
    trattato esplicitamente come identificatore di settore/riga della mappa.
  - **Conseguenza per lo schema**: `catalog_pricing_price_list_reduced_windows`/`_reduced_prices` può
    restare come disegnato (prezzo assoluto per settore in finestra), **senza** bisogno di un enum
    `adjustment_kind` — quell'opzione prudenziale suggerita nella scheda non serve più.
- **La precedenza fra le fonti è stata verificata nel codice legacy — vedi §12.6** (non è la cascata
  pulita a 5 gradini elencata di seguito, che resta qui solo come inventario delle **tabelle coinvolte**,
  non come ordine di risoluzione).

Le 5 tabelle/fonti coinvolte (l'ordine qui sotto è solo l'elenco, **non** la precedenza — quella è in
§12.6):
- `catalog_pricing_price_list_reduced_windows`/`_reduced_prices` — finestra a prezzo ridotto con **match
  esatto** sulle date della prenotazione (semantica `value` = prezzo assoluto per settore, D-15 ✅).
- `catalog_pricing_price_list_date_prices` — override per **singola data di calendario** (`kind =
  'override'`, verificato sul dump reale che **non esiste nei dati di produzione** — §12.6) o
  **festività** (`kind = 'holiday'`, confermato: nessun prezzo proprio, usa la tariffa weekend — §12.6).
- `catalog_pricing_price_list_stay_prices` — prezzo per **numero di giorni** (griglia 1-90, solo gli
  scostamenti dal calcolato sono persistiti — modello confermato, D-15b ✅; verificato che ha precedenza
  sul calcolo weekend/festivi quando presente — §12.6).
- `catalog_pricing_price_list_period_prices` — tariffa **base** per periodo tariffario.
- `catalog_pricing_price_list_sector_prices` — delta/prezzo base per **settore**.

**Attenzione**: nel legacy period_prices e sector_prices **non sono due passi separati** — sono risolti
insieme come un'unica ricerca a chiave combinata "settore × periodo" (§12.6). Il modello a 5 tabelle
separate resta una normalizzazione valida, ma la funzione/servizio che le userà dovrà **ricostruire**
questo incrocio, non aspettarsi che il legacy lo faccia in due passi distinti.

Se un preventivo ha N righe (N combinazioni pacchetto×settore×data×durata), fare questa cascata **una
riga alla volta** dall'applicazione significa fino a **5×N query** verso il database in sequenza — il
caso da manuale di **N+1**, ed è quello che l'ottimizzazione vuole eliminare. Il problema non è "una
query lenta", è "troppe query piccole in sequenza" (latenza di rete moltiplicata per 5×N).

### 12.2 Due strade per risolverlo — con un trade-off da decidere

**Opzione A — funzione SQL server-side** (quella proposta inizialmente):
```sql
CREATE FUNCTION catalog.resolve_package_price(
    p_price_list_id bigint, p_sector_id bigint, p_price_date date, p_stay_days int
) RETURNS numeric(12,2) AS $$
    SELECT COALESCE(
        (SELECT price FROM catalog.pricing_price_list_reduced_prices rp
         JOIN catalog.pricing_price_list_reduced_windows rw ON rw.id = rp.reduced_window_id
         WHERE rw.price_list_id = p_price_list_id AND rw.sector_id = p_sector_id
           AND rw.starts_on = p_price_date /* match esatto, non range */),
        (SELECT price FROM catalog.pricing_price_list_date_prices
         WHERE price_list_id = p_price_list_id AND price_date = p_price_date),
        (SELECT price FROM catalog.pricing_price_list_stay_prices
         WHERE period_price_id = (...) AND (sector_id = p_sector_id OR sector_id IS NULL)
           AND stay_days = p_stay_days ORDER BY sector_id NULLS LAST LIMIT 1),
        (SELECT daily_price FROM catalog.pricing_price_list_period_prices
         WHERE tariff_period_group = (...))
    );
$$ LANGUAGE sql STABLE;
```
- ✅ Minimo numero di round-trip possibile (1 per riga, o 1 in assoluto con la versione a lotto sotto).
- ❌ **Sposta la logica di business nel database**: la precedenza fra le 5 fonti è **la** regola di
  dominio del motore prezzo. Le schede/documenti di progetto (`_motore-di-prezzo-e-sconti.md`,
  `docs/03-gestione-dati.md` "ricalcolo server-side obbligatorio") descrivono coerentemente il motore
  prezzo come componente **Application in C#**, mai come funzione SQL. Metterla in una funzione `plpgsql`/`sql`
  significa avere la stessa logica potenzialmente in **due posti** (o spostarla del tutto fuori da C#),
  perdere gli strumenti di unit test .NET standard su di essa, e aggiungere una funzione DB che va
  versionata/migrata come le tabelle (disciplina in più, non gratuita).

**Opzione B — la logica resta in C#, si ottimizza solo il recupero dati (raccomandata)**:
si eseguono **5 query, una per tabella pricing**, ciascuna con `WHERE price_list_id = ANY(@ids) AND ...`
per **tutte le righe del preventivo insieme**, invece di 5×N query una per riga. Il motore prezzo in C#
riceve i risultati come liste e applica la precedenza in memoria (lo stesso `if`/`Coalesce` che farebbe
in SQL, ma in C#, testabile con xUnit come oggi):
```csharp
public async Task<IReadOnlyDictionary<Guid, decimal>> ResolvePricesAsync(
    IReadOnlyList<PriceQuery> queries, CancellationToken ct)
{
    var priceListIds = queries.Select(q => q.PriceListId).Distinct().ToArray();

    var reducedPrices = await _context.ReducedPrices
        .Where(x => priceListIds.Contains(x.PriceListId)).ToListAsync(ct);
    var datePrices = await _context.DatePrices
        .Where(x => priceListIds.Contains(x.PriceListId)).ToListAsync(ct);
    var stayPrices = await _context.StayPrices
        .Where(x => priceListIds.Contains(x.PriceListId)).ToListAsync(ct);
    var periodPrices = await _context.PeriodPrices
        .Where(x => priceListIds.Contains(x.PriceListId)).ToListAsync(ct);
    var sectorPrices = await _context.SectorPrices
        .Where(x => priceListIds.Contains(x.PriceListId)).ToListAsync(ct);

    // 5 query totali per l'intero preventivo (non 5×N), poi risoluzione in memoria per riga:
    return queries.ToDictionary(q => q.LineId, q => ResolveOne(q, reducedPrices, datePrices, stayPrices, periodPrices, sectorPrices));
}

private decimal ResolveOne(PriceQuery q, ...) =>
    TryReducedPrice(q, ...) ?? TryDatePrice(q, ...) ?? TryStayPrice(q, ...) ?? TryPeriodPrice(q, ...)
    ?? throw new PriceNotResolvableException(q);
```
- ✅ La regola di precedenza resta in C#, unit-testabile, un solo posto dove vive.
- ✅ Stesso beneficio pratico: da `5×N` a `5` query per l'intero preventivo, indipendentemente da N.
- 🟡 Richiede filtrare in memoria invece che con indici mirati per riga — accettabile perché il
  **numero di righe caricate per property/listino è già piccolo** (decine, non migliaia — vedi volumi
  nelle schede pricing), quindi il costo di filtrare in C# è trascurabile rispetto al risparmio di
  round-trip.

**✅ Deciso 2026-09-18: Opzione B.** La logica di precedenza resta in C#, testabile con gli strumenti
standard del progetto; il beneficio prestazionale (5 round-trip invece di 5×N) resta comunque acquisito.
L'Opzione A (funzione SQL) resta documentata sopra come alternativa scartata, non da implementare.

### 12.3 Risoluzione a lotto (indipendente dalla scelta A/B)

In entrambi i casi, il punto di ingresso dall'Application deve essere **uno solo per l'intero preventivo**,
non uno per riga:
```csharp
var prices = await _priceResolver.ResolvePricesAsync(quoteLines.Select(l => new PriceQuery(
    l.Id, l.PriceListId, l.SectorId, l.PriceDate, l.StayDays)).ToList(), ct);
```
Un preventivo con 10 righe → una singola chiamata (che internamente fa 5 query con l'Opzione B, o 1 con
l'Opzione A via `UNNEST`), non 10 chiamate separate dal controller/handler.

### 12.4 L'opzione B è più lenta? Di quanto?

**Sì, in linea di principio**: l'opzione A fa **1 round-trip** di rete verso il database per l'intero
preventivo (la funzione gira internamente lato server, senza altre andate/ritorni); l'opzione B ne fa
**5** (una per tabella pricing), sempre indipendentemente da quante righe ha il preventivo.

**Quanto pesa in pratica — stima, non un benchmark reale**: la differenza è il costo di **4 round-trip
di rete in più**. Con l'app e il database nella stessa VPC/regione AWS (il caso tipico di un'istanza RDS
usata da un servizio ECS nella stessa VPC), un round-trip di questo tipo costa tipicamente **meno di
1 ms**. Quattro in più → **qualche millisecondo** (indicativamente 2-4 ms) di latenza aggiuntiva totale
per l'intero preventivo, **non** per riga. Per confronto:
- il problema che si sta risolvendo (fare la cascata riga per riga, `5×N` query) per un preventivo di
  10 righe farebbe **50 round-trip** — anche a <1ms ciascuno, 50+ ms solo di latenza di rete, prima di
  contare l'esecuzione delle query stesse;
- una tipica risposta HTTP di questo endpoint (autenticazione, validazione, altre chiamate) è
  normalmente nell'ordine delle decine-centinaia di millisecondi.

Quindi: sì, l'opzione B è misurabilmente più lenta dell'opzione A **in isolamento** (più round-trip), ma
la differenza (pochi ms) è **trascurabile** rispetto sia al problema che si sta eliminando (decine di ms
in meno rispetto a oggi) sia alla latenza complessiva della richiesta. Non è una stima definitiva — andrebbe
confermata con un profiling reale una volta implementata — ma l'ordine di grandezza non giustifica, da
solo, la scelta di spostare la logica di prezzo in SQL (§12.2) solo per guadagnare quei pochi millisecondi.

**Via di mezzo, se in futuro servisse**: si può ridurre l'opzione B da 5 a 1-2 round-trip senza spostare
la logica in SQL, unendo le 5 query in una sola con `UNION ALL` (discriminando la tabella di provenienza
con una colonna `source`) — la precedenza resta comunque risolta in C# sui risultati, cambia solo quante
query fisiche si fanno per recuperare i dati grezzi.

### 12.5 Cache — solo se il profiling lo richiede

Una cache notturna dei prezzi per i prossimi 90 giorni (per listino/settore) è un'ottimizzazione
**successiva ed eventuale**: introduce uno strato in più da invalidare a ogni cambio prezzo (rischio di
mostrare un prezzo stantio) per un problema — la latenza del calcolo — che le opzioni A/B sopra
probabilmente risolvono già a sufficienza. Da considerare solo se il profiling post-rilascio mostra che
anche a 5 query per preventivo il calcolo resta un collo di bottiglia.

### 12.6 Precedenza fra le fonti di prezzo — verificato nel codice legacy (2026-09-18)

**Metodo**: letto per intero `front-pre/src/services/BookingCalcService.js` (in particolare
`getPackagePriceCalc`, la funzione di calcolo "normale") e verificato `PriceCalculations.js` (editor
prezzi lato gestionale) per la parte di configurazione. Risultato: **non è una cascata pulita a 5
gradini** come avevo ipotizzato dalla sola lettura delle schede — la struttura reale è diversa e più
semplice in un punto, più incerta in un altro. Riporto punto per punto cosa è confermato e cosa no.

**✅ Confermato — fonte 1 (`reduced_windows`) non è "il primo gradino di una cascata", è un percorso
alternativo scelto a monte.** `getReducedPrices` e `getSeasonalDatesPrices` (prezzo stagionale normale)
sono **alternativi fra loro**, selezionati da un flag lato UI (`getReducedPriceFlag` /
`getSeasonalDateFlag`) **prima** di entrare nel calcolo principale (`getPackagePriceCalc`). Quando il
prezzo ridotto è selezionato, **nessun'altra fonte entra in gioco**: non è che "vince" per priorità su
festività/override, è che l'intero resto del calcolo non viene nemmeno eseguito. Architetturalmente è
più simile a un interruttore a monte che a un gradino di una cascata.

**✅ Confermato — dentro il calcolo "normale", l'override per numero di giorni (fonte 3) va per primo e
blocca il resto.** In `getPackagePriceCalc`: `checkisPriceChange(day, edit_price_data)` è controllato
**per primo**; se c'è un override per quell'esatto numero di giorni, il prezzo è preso direttamente da lì
(`getSelectedRecPrice`) e **l'intera logica di weekend/festivi/scaglioni 7-14-30gg viene bypassata** per
quella porzione di soggiorno.

**✅ Confermato, e diverso da come l'avevo modellato — "festività" (fonte 2, `kind = holiday`) non è un
prezzo assoluto proprio.** Cross-verificato codice + scheda: `catalog_pricing_price_list_date_prices.md`
dice esplicitamente che per `kind = holiday` **`price` è `NULL`** ("per `holiday` non presente nel JSON
legacy"). Il codice conferma perché: in `getPackagePriceCalc`, i giorni festivi (`holidayDates`) **non
hanno un prezzo proprio** — vengono trattati come giorni al prezzo weekend
(`numberOfHoliday * weekend_price`, sia nel ramo normale sia in `calcHDWPrice` per il mezzogiorno). La
"festività" nel legacy è quindi una **riclassificazione della tariffa applicabile** (usa il prezzo
weekend anche se cade in un giorno feriale), non un override di prezzo indipendente.

**✅ Chiuso 2026-09-18 — interrogato il dump di produzione reale, il caso non risulta esistere.**
Non trovando il consumo nel codice, invece di continuare a leggere altri componenti front-end ho
interrogato direttamente `dump-prod-legacy/u811491364_summer_booking_price_lists.sql` (il dump reale
completo della tabella, 207 MB) per vedere se l'override "per data specifica" (`kind = override`,
distinto dal grid per numero di giorni) esiste davvero nei dati:

- L'esempio nella scheda `analisi-pre/db/price_lists.md` (`{"date":"15/08/2024","day":"Thursday","price":"60"}`)
  suggerisce che un override "vero" per data fissa abbia `day` come **stringa** (il nome del giorno della
  settimana), distinto dal grid per numero di giorni dove `day` è un **numero**.
- **Cercato `"day":"` (day come stringa) in tutto il dump**: **zero occorrenze** su tutta la tabella
  `price_lists` di produzione.
- **Conteggio esatto su tutto il dataset** (non solo un campione): **2.392.163** voci totali con campo
  `date` in `edit_price_data`, di cui **2.386.539** (99,76%) hanno `day` numerico nella stessa voce. Il
  residuo ~0,24% è compatibile con rumore/voci malformate nei dati, non con un pattern distinto — non ci
  sono evidenze di un secondo formato "override a data fissa" nemmeno su scala di milioni di voci.
- **Tutti** i campioni reali di `edit_price_data` osservati hanno la stessa forma:
  `{"date":"2021-03-13T00:00:00.000Z","day":1,"price":20,"item":"..."}`,
  `{"date":"...","day":2,"price":40,...}`, `{"date":"...","day":3,...}` — cioè `day` **sempre un numero
  sequenziale** (1, 2, 3…) con una `date` calcolata di conseguenza (verosimilmente
  `data_inizio_soggiorno + day - 1`), non una data fissa ricorrente. Questa è esattamente la struttura
  del grid per numero di giorni (fonte 3), non un override indipendente per una data di calendario fissa.

**Conclusione**: l'override "per data specifica" con la semantica che la scheda originale aveva
ipotizzato (un prezzo speciale per una data fissa, es. Ferragosto ogni anno, con `day` = nome del giorno)
**non risulta esistere nei dati reali di produzione**. È verosimilmente rimasto un caso teorico
nell'analisi (un solo esempio illustrativo, mai confermato su dati reali — la stessa scheda
`price_lists.md` lo segnala: "consumo lato backend non tracciato"). **Non serve continuare a cercarne la
logica di consumo**: la parte realmente in uso è già coperta dal grid per numero di giorni (fonte 3, già
risolta con D-15b). Resta comunque valido modellare `catalog_pricing_price_list_date_prices.kind =
'override'` nello schema nuovo, per completezza e per un eventuale uso futuro — ma senza precedenza da
replicare dal legacy, perché non risulta mai stata esercitata.

**✅ Scoperta strutturale — fonte 4 (`period_prices`) e fonte 5 (`sector_prices`) non sono due gradini
sequenziali nel legacy, sono un'unica ricerca combinata.** `getMatchedPkgObj` cerca una riga per la
chiave combinata **`"{settore} {periodo_tariffario}"`** (`currentRow + ' ' + seasonPeriodName`) e
restituisce **una singola riga** che porta già tutte le tariffe (`daily_price`, `weekend_price`,
`days_7_price`, `days_14_price`, `monthly_price`) specifiche di quella combinazione. Nel legacy non
esiste un "prezzo di periodo generico" poi aggiustato per settore: la tariffa è già settore-specifica fin
dall'inizio. Questo significa che il modello a 5 tabelle separate (dove `sector_prices` e
`period_prices` sono due entità distinte) **non rispecchia 1:1** come il legacy risolve il prezzo base —
è comunque una normalizzazione ragionevole (evita di duplicare le tariffe per ogni combinazione
settore×periodo), ma la logica applicativa dovrà ricostruire volontariamente questo incrocio, non
riprodurre una ricerca a chiave singola come fa il legacy.

**Precedenza risultante, per quanto verificato:**
```
1. Prezzo ridotto attivo (fonte 1)?        → usa SOLO questo, nessun'altra fonte si applica
   altrimenti:
2. Override per numero di giorni (fonte 3) che copre esattamente il soggiorno?
                                             → usa questo, bypassa weekend/festivi/scaglioni
   altrimenti, per ciascun giorno del soggiorno:
   - giorno festivo (fonte 2, kind=holiday)  → tariffa weekend (non un prezzo proprio)
   - giorno weekend                          → tariffa weekend
   - altrimenti                              → tariffa (settore × periodo) risolta come riga combinata
                                                (fonti 4+5 insieme), con scaglioni 7/14/30 giorni

(kind=override per data specifica fissa: verificato sul dump reale che NON esiste nei dati di produzione
— vedi sopra. Non partecipa alla precedenza perché non risulta mai usato.)
```

**✅ Precedenza chiusa 2026-09-18**: tutti i punti sono stati verificati, sia nel codice sia nel dump di
produzione reale. Nessun punto genuinamente aperto residuo su questo argomento.

---

## 13. Array vs tabelle

| Pattern | Quando | Esempi nello schema |
|---|---|---|
| **`enum[]`** | Vocabolario chiuso, poche voci, cambia solo con deploy | `enabled_payment_methods`, `applies_to_service_categories` |
| **`text[]` "tag descrittivi"** | Lista canonica ma **estensibile senza deploy**, nessuna FK, GIN per ricerca | `notification_templates.placeholder_keys_used` (la scheda dice esplicitamente "non è un ENUM, testo semplice"); `privacy_retention_policies.field_names` |
| **`id[]` read-model di relazione strutturata** | Cache/denormalizzazione di una vera relazione N:N | `catalog_search_bathhouses.amenity_ids` (id reali da lookup+giunzione, GIN) — diverso da un tag libero |

**✅ `booking_reservations.customer_tags` — deciso 2026-09-18: resta `text[]`.** Confermato: i tag sono pensati per essere estesi senza deploy, non un vocabolario chiuso — nessuna conversione a `enum[]`.

**✅ `catalog_service_sale_item_weekdays` — deciso 2026-09-18: procedere con il collasso in array.** `active_weekdays catalog.weekday[]` sulla tabella padre `catalog_service_sale_items`, enum `catalog.weekday` con 7 valori (`monday`…`sunday`), al posto delle 7 righe figlie con flag `is_active`.

### 13.1 Altri candidati array — ✅ approvati 2026-09-18

Emersi nella prima revisione trasversale, confermati dal titolare, stesso principio di §13:

| Tabella/campo oggi | Candidato array | Perché | Attenzione |
|---|---|---|---|
| `identity_access_users`: 4 colonne booleane parallele (`mgmt_booking_sound_enabled`, `online_booking_sound_enabled`, `mgmt_service_booking_sound_enabled`, `online_service_booking_sound_enabled`) | `enum[]` (es. `enabled_notification_sounds identity.notification_sound_scope[]`) | Stesso pattern "flag multipli sullo stesso argomento" già usato per `enabled_payment_methods` — qui non applicato, incoerenza di design | Nessuna — è un collasso diretto senza perdita di informazione |
| `identity_access_user_resource_scopes` (1 riga per risorsa concessa a un account `affiliate`) | `resource_scope_codes varchar(32)[]` su `identity_access_users` + GIN | Cardinalità piccola (~14 utenti coinvolti nei dati osservati), nessuna FK verso altre tabelle | La tabella attuale porta `created_at`/`deleted_at` **per singola assegnazione** — se serve lo storico di "quando è stata concessa/revocata" questa risorsa, la tabella resta la scelta giusta; da confermare col committente se è un requisito reale |
| `booking_quick_action_dates` (date "azioni rapide" del planner) | `booking_reservations.quick_action_dates date[]` | Nessuna FK, nessun valore di audit per riga, sempre letta/scritta insieme alla prenotazione | L'indice `(property_id, action_date)` serve la query planner-wide "azioni rapide del giorno X su tutte le prenotazioni" — con un array servirebbe un GIN su `date[]`, meno naturale di un B-tree per-riga: da valutare in base al volume reale di quella query |

### 13.2 Idea nuova (`search_keywords` su `catalog_bathhouses`) — ❌ per ora no, deciso 2026-09-18

Proposta non applicata: nessun campo "parole chiave libere" da aggiungere per la ricerca pubblica al
momento. Restano solo `name` (trigram) e `amenity_ids` (FK reali via giunzione) come criteri di ricerca.
Non si aggiungono altri campi array oltre a quelli già decisi in §13/§13.1 — **chiusa l'analisi
array vs tabelle**, nessun'altra conversione da proporre.

---

## 14. Complessità di codice, rischio prestazionale e costi aggiuntivi

Non tutte le voci di questo documento sono "vinci e basta": alcune spostano il costo altrove o lo
riducono in un punto aumentandolo in un altro. Riepilogo onesto per punto.

### 14.1 Partizionamento — cosa vuol dire "gestire manualmente", e sono davvero solo 2 opzioni?

> **Aggiornamento 2026-09-21**: il partizionamento è stato applicato e questo punto è diventato
> operativo. Le partizioni create arrivano a **fine 2030**, ed è stata aggiunta una **partizione di
> default** — quindi il fallimento descritto qui sotto ("il primo `INSERT` di quell'anno fallisce")
> **non** avviene: le righe finiscono nella default e il problema degrada in silenzio invece di
> segnalarsi. Scadenza, controlli e procedura in [`06-partizioni-manutenzione.md`](06-partizioni-manutenzione.md).

**Cosa intendo per "gestire manualmente" (dettaglio, chiarito 2026-09-18)**: EF Core non ha un metodo
fluente tipo `.PartitionBy(...)` — la sintassi `PARTITION BY RANGE`/`CREATE TABLE ... PARTITION OF` va
scritta come **SQL grezzo dentro il metodo `Up()` di una migration** (`migrationBuilder.Sql("...")`),
non generata dal designer EF a partire dall'entità. In più, una tabella partizionata per range accetta
righe solo nelle partizioni che esistono già: se ad esempio si partiziona per anno e nessuno crea la
partizione del 2029 prima che arrivi il 2029, il primo `INSERT` di quell'anno **fallisce** con un errore
Postgres a runtime. Serve quindi un meccanismo che crei le partizioni **in anticipo** — o un job
schedulato scritto ad hoc (es. "ogni dicembre crea la partizione dell'anno prossimo per tutte le tabelle
partizionate"), o l'estensione `pg_partman` che lo automatizza. Questo è il lavoro concreto dietro
"gestire manualmente": non è complesso concettualmente, ma è un componente operativo che **non esiste
oggi** nel progetto e va costruito.

**Sono davvero solo 2 opzioni (partizionamento nativo vs tabella calda+archivio)?** Quasi — manca
un'opzione "zero" che vale la pena rendere esplicita: **non fare nulla per ora**. Per la maggior parte
delle tabelle in §2, il problema (una tabella troppo grande da gestire bene) non esiste ancora oggi — è
un rischio futuro legato al volume. Se il volume reale resta sotto qualche milione di righe per i
prossimi 1-2 anni (§15), la scelta più economica è **non partizionare e non creare un archivio**, solo
monitorare la crescita e implementare una delle due strade quando il volume la giustifica davvero. Una
volta che si decide di agire, sì: restano solo le due strade strutturali, perché il problema di fondo
("una tabella fisica troppo grande per gli indici/la manutenzione/la retention") si risolve solo
dividendo fisicamente i dati per intervallo — partizionamento nativo Postgres (con la partition key
dentro le `UNIQUE`) o gestione applicativa equivalente (tabella calda + spostamento manuale/periodico
dei dati vecchi, che è esattamente cosa fa il partizionamento ma "a mano" e con più libertà su dove
finiscono i dati vecchi, es. S3 invece di un'altra tabella Postgres).

### 14.2 Generated column — non è un problema di migrazione, adesso

**Confermato**: il progetto è a inizio schema, non ancora in produzione con tabelle popolate — quindi il
rischio "riscrittura dell'intera tabella su `ALTER TABLE ADD COLUMN GENERATED`" **non si applica oggi**:
le generated column vanno semplicemente definite correttamente nella migration iniziale che crea la
tabella, a costo zero. Resta un rischio reale solo per il **futuro**: se dopo il lancio, a tabella già
popolata a volumi importanti, servisse aggiungere una NUOVA generated column non prevista oggi, quella
specifica modifica andrebbe pianificata con attenzione (non è un problema per questa fase).

### 14.3 Generated column su campi contabili — verificato

Tre punti da verificare, come richiesto:

1. **Quanto migliorano le prestazioni?** Onestamente: **quasi per niente, e non è quello il punto**. Una
   generated column `STORED` si legge esattamente alla velocità di una colonna normale (il valore è già
   calcolato e salvato, nessun calcolo a lettura). In scrittura, calcolare `net + vat` o `quantity *
   unit_price` costa qualche istruzione macchina — un tempo non misurabile rispetto al costo di un
   `INSERT`/`UPDATE` reale (I/O, WAL, indici). **Il beneficio non è velocità, è correttezza**: elimina
   strutturalmente la possibilità che la colonna derivata vada fuori sincrono dalle colonne sorgente,
   senza bisogno di un job di riconciliazione che lo verifichi periodicamente. Vale la pena riformulare
   così la motivazione anche nelle sezioni precedenti: non è un'ottimizzazione di prestazioni, è
   un'eliminazione di una classe di bug.
2. **Il campo esisteva già ed era calcolato a mano?** Verificato scheda per scheda:
   - `payment_wallet_transaction_items.line_total` — **sì, con evidenza concreta**: la scheda cita
     esplicitamente che il legacy `totalPrice` era "spesso `null` o incoerente" — è un bug storico
     confermato, non un rischio ipotetico. Qui la generated column ha la giustificazione più solida.
   - `payment_pos_charge_items.net_amount`/`vat_amount` — descritto come "scorporo calcolato in
     acquisizione", ma **senza** un riferimento esplicito a un problema storico nel legacy — è una
     misura preventiva, non la correzione di un bug già osservato.
   - `payment_transaction_charges.gross_amount` / `payment_ledger_entries.gross_amount` — colonne
     **nuove** introdotte dal redesign (non hanno un diretto precedente legacy calcolato a mano) — anche
     qui la motivazione è preventiva, non la chiusura di un problema già visto.
3. **Erano previste eccezioni per una modifica manuale? — ✅ verificato per tutte e 4 (2026-09-18)**:
   - `payment_ledger_entries` (`entry_kind = 'adjustment'`, "rettifica manuale al posto della riscrittura
     legacy") e `payment_wallet_transactions` (`source = 'adjustment'`, "rettifica manuale") gestiscono le
     correzioni come **nuova riga append-only**, mai un `UPDATE` in-place.
   - `payment_transaction_charges`: confermato "la tabella è **append/stato**, non si soft-elimina"; i
     rimborsi sono righe separate in `payment_transaction_refunds`.
   - `payment_pos_charges.status` passa a `voided` con "un documento di storno" (nuova riga
     `payment_ledger_entries`) — stesso pattern, nessuna modifica di importo in-place.

**Conclusione finale**: il rischio "serve una scrittura fuori formula" **non si applica a nessuna delle
4 tabelle** — vedi la lista definitiva in §5.1. L'unica esclusione (`booking_line_additional_items`) è
per un motivo diverso e più semplice: dipendenza da una colonna della tabella padre, non risolvibile con
una generated column senza prima denormalizzare un conteggio giorni sulla riga stessa.

### 14.4 `INCLUDE` — ❌ non applicare per ora (deciso 2026-09-18)

Nessun covering index da aggiungere in questa fase, né su `catalog_map_spots` (già escluso per il motivo
in §7) né su `catalog_pricing_price_list_period_prices`/`sector_prices` (che restava tecnicamente valido,
ma si rimanda): resta comunque documentato in §7 come possibile intervento futuro se il profiling post-
rilascio mostra che quel lookup è davvero un collo di bottiglia.

### 14.5 `btree_gist` su AWS RDS — ✅ confermato disponibile (2026-09-18)

`btree_gist` è un'estensione "contrib" standard di PostgreSQL, storicamente presente nella lista delle
estensioni **whitelisted** da AWS per RDS PostgreSQL (abilitabile con un semplice `CREATE EXTENSION
btree_gist;`, senza bisogno di permessi superuser reali — RDS gestisce questo tramite il proprio ruolo
`rds_superuser`). Non risulta un costo diretto associato all'estensione in sé (è codice bundled, non un
servizio a consumo). Quello che va verificato **specificamente sull'istanza del progetto**, perché non
lo confermo io con certezza assoluta:
- che la versione del motore PostgreSQL/Aurora scelta per il progetto la includa ancora nella lista
  whitelisted (la lista può cambiare a seconda della major version e se si userà RDS PostgreSQL "classico"
  o Aurora PostgreSQL);
- il modo più affidabile per verificarlo è direttamente sull'istanza (anche di sviluppo) con
  `SELECT * FROM pg_available_extensions WHERE name = 'btree_gist';` — se compare, è disponibile.

Il costo operativo reale non è l'estensione in sé, ma il tipo di indice che abilita: un indice **GiST**
(necessario per `EXCLUDE USING gist`) è più pesante da mantenere in scrittura di un B-tree/`UNIQUE`
equivalente — questo è un costo Postgres generico, non specifico di AWS.

### 14.6 Array al posto di tabelle — chiuso, nessun'altra conversione

Deciso in §13: nessuna ulteriore tabella da convertire in array oltre a quelle già approvate
(`catalog_service_sale_item_weekdays`, i candidati di §13.1). Il costo descritto sopra (perdita di
integrità referenziale se mai presente, codice da riscrivere con operatori array) resta valido come
promemoria per **quelle** conversioni, ma non se ne aggiungono altre.

### 14.7 In sintesi

Il partizionamento (§14.1) resta la voce con più lavoro reale di infrastruttura — ma con l'opzione "zero"
esplicitata (non fare nulla finché il volume non lo giustifica) e la decisione già presa su
`fiskaly_receipts` (§9.1/§15: tabella calda + S3). Le generated column sui campi contabili sono più
sicure di quanto stimato inizialmente (§14.3), ma il beneficio è correttezza, non velocità. `EXCLUDE
USING gist` è **confermata** (business + `btree_gist` su AWS, §8/§14.5) — resta solo la bonifica dei dati
storici come attività pratica, non come domanda aperta. `INCLUDE` e le conversioni array aggiuntive sono
chiuse per ora (§14.4/§14.6).

---

## 15. Informazioni necessarie per procedere

Per ogni voce contrassegnata 🔴/🟠 in questo documento, cosa manca prima di poter passare
dall'analisi all'implementazione — con `fiskaly_receipts` come esempio guida.

### Esempio guida — §2/§9.1 partizionamento `fiskaly_receipts` — ✅ deciso 2026-09-18

**Deciso**: tabella calda (36 mesi) + archivio su **S3** (non una tabella Postgres, non partizionamento
nativo) — vedi §9.1 aggiornato. Resta da quantificare solo il **volume reale/proiezione di crescita**
per dimensionare la frequenza del job di export mensile verso S3 (informazione utile ma non bloccante,
la strada è già scelta).

### Altre voci — stato finale (aggiornato 2026-09-18)

**Tutto è stato chiuso nel corso della revisione — nessuna voce genuinamente aperta residua.** L'ultima
(la precedenza fra le 5 tabelle pricing, §12) è stata risolta verificando codice legacy + dump di
produzione reale, vedi §12.6.

| Sezione | Stato |
|---|---|
| §2 `payment_ledger_entry_lines` | ✅ Chiuso — non serve la colonna denormalizzata ipotizzata: `payment_ledger_entries` non si partiziona (confermato), quindi nemmeno la figlia |
| §2 `booking_resource_consumptions`/`_availability_daily` | ✅ Risolto: partizionamento per anno solare (deciso) |
| §2 `switchboard_voice_call_records` | ✅ Chiuso — 0 righe nel dump di produzione (§2.1), non serve leggere la scheda nel dettaglio per ora |
| §6 idempotenza webhook | ✅ Risolto — verificato nel codice (`stripeWebhookHandler.js`): solo `payment_intent.succeeded`/`checkout.session.completed` gestiti oggi |
| §8 `EXCLUDE gist` | ✅ Risolto — business confermato (nessuna sovrapposizione mai ammessa) e `btree_gist` disponibile su AWS. Resta solo la bonifica dei dati storici come attività pratica di migrazione, non come domanda |
| §10 `property_id` | ✅ Nessuna informazione mancante — pronto |
| §11 viste | ✅ Deciso — procede solo `fiskaly.v_daily_cash_closure`, il resto non per ora |
| §12 calcolo prezzo | ✅ **Chiuso 2026-09-18** — precedenza verificata nel codice legacy + nel dump di produzione reale (§12.6): non è una cascata a 5 gradini; fonte 1 è un percorso alternativo scelto a monte, fonte 3 blocca il resto se presente, "festività" non ha prezzo proprio (usa la tariffa weekend), fonti 4+5 sono un'unica ricerca combinata, l'override per data specifica **non esiste nei dati reali** (zero occorrenze su tutto il dump `price_lists`) |
| §9.4 `fiscalprint_printer_departments` | ✅ **Chiuso 2026-09-18**: si lascia il terzo indice (nessuna richiesta di conferma al committente). Corretto anche l'errore "candidato a dismissione" — fiscalprint non lo è (vedi §9.4) |
| §13 array | ✅ Risolto — tutte le decisioni prese |

---

## 16. Riepilogo prioritizzato

> ### ✅ Implementato il 2026-09-21
>
> La nota che segue ("nessuna modifica a schema o codice è stata applicata") **non vale più**: le
> decisioni di questo documento sono state portate nel backend `backend/summerbooking-backend` e nelle
> schede di `analisi-post/db`. Stato voce per voce:
>
> | # | Area | Esito |
> |---|---|---|
> | 1 | Partizionamento consumi | ✅ **Fatto** — `PartitionResourceTablesByYear` (Booking). Le due tabelle sono partizionate per anno solare, 10 partizioni + default. PK diventata `(id, consumption_date)`. Misurato sul DB di sviluppo: 3.809.485 righe / 786 MB. **Non confrontabile con la stima di ~1,02M di §2.1**, che è un volume a regime calcolato sul dump di *produzione*; ma ne conferma la conclusione in modo più netto: applicando la finestra N-1 restano 3.794.143 righe su 3.809.485, la potatura toglie lo **0,4%**. `EXPLAIN` conferma che una query mensile legge una sola partizione |
> | 4 | `EXCLUDE gist` | 🟠 **3 su 6** — applicati su `identity_account_subscription_periods`, `catalog_property_online_stay_limits`, `catalog_service_sale_item_time_slots` (+ estensione `btree_gist`). Gli altri tre **bloccati da dati sporchi preesistenti**, come il §8 prevedeva: vedi sotto |
> | 5 | Retention job | ✅ **Fatto in precedenza** — finestre validate seminate in `privacy_retention_policies` |
> | 6 | BRIN | ✅ **Fatto** — `AddBrinTimeIndexes` su Identity, Fiskaly, Privacy, Chatbot (4 indici) |
> | 7 | Generated column | ✅ **Fatto** — 5 colonne su 5 tabelle (Payment ×4, Booking ×1). Lo scorporo IVA verificato sui valori reali |
> | 8 | `property_id` | ✅ **Fatto** — denormalizzato su `payment_ledger_entry_lines`, `payment_pos_charge_items`, `payment_wallet_transaction_items` |
> | 9 | `ON CONFLICT` | ✅ **Fatto** — nuova tabella `payment.webhook_events` ([scheda](../analisi-post/db/payment/payment_webhook_events.md)), idempotenza verificata: il secondo `INSERT` dello stesso evento non inserisce nulla |
> | 10 | Fix puntuali | ✅ Nessuna azione richiesta per §9.1/§9.3/§9.4 (decisioni già "lascia com'è"); §9.5 è un pattern di codice applicativo, non di schema, e arriverà con la BL |
> | 11 | Viste | ✅ **Fatto** — `fiskaly.v_daily_cash_closure` creata. **Corretto un errore della specifica**: vedi nota sotto |
> | 12 | Array/tabelle | 🟠 **2 su 4** — `catalog_service_sale_item_weekdays` collassata in `active_weekdays[]`, i 4 flag audio di `identity_access_users` in `enabled_notification_sounds[]` (dati travasati, non reimpostati). Le altre due restano condizionate: vedi sotto |
>
> **§11.2 — la specifica della vista era sbagliata.** Elencava in un'unica riga sia il totale per forma
> di pagamento sia la ripartizione per aliquota IVA. Presi insieme in un solo `JOIN` sono scorretti: uno
> scontrino con 2 pagamenti e 3 righe produce 6 combinazioni e la somma dei pagamenti viene contata tre
> volte. Implementata come **due aggregazioni separate unite da `UNION ALL`**, distinte da
> `breakdown_kind`. Verificato sui dati reali: i due totali riconciliano esattamente su tutti i giorni.
> Aggiunta anche la conversione a `Europe/Rome` della data fiscale — senza, uno scontrino delle 00:30
> italiane cadrebbe nel giorno precedente e la chiusura di cassa non tornerebbe.
>
> **§8 — i tre vincoli mancanti e perché.** I dati del DB di sviluppo violano già i vincoli: 41 coppie
> sovrapposte su `catalog_pricing_tariff_periods`, 9 su `catalog_property_online_allowed_periods`, 81 su
> `booking_resale_offers`. Il §8 lo prevedeva ("il vincolo va applicato **dopo** aver ripulito quei
> casi"). La bonifica **non è meccanica**: l'esempio tipico è una *"Alta stagione"* di un giorno dentro
> una *"Bassa stagione"* di 19 — su quel giorno quale tariffa vale? Deciderlo è business, non pulizia,
> e va fatto nell'ETL di migrazione. Il vincolo progettato è documentato in ciascuna scheda, pronto da
> abilitare dopo.
>
> **§13.1 — due conversioni array restano condizionate**, perché il documento stesso vi appone una
> riserva che non è stata sciolta:
> - `identity_access_user_resource_scopes` → array: la tabella porta `created_at`/`deleted_at` **per
>   singola assegnazione**. Se serve sapere quando una risorsa è stata concessa o revocata, l'array
>   cancella quell'informazione. Il documento dice "da confermare col committente se è un requisito
>   reale": non confermato, non convertito (19 righe, nessuna urgenza).
> - `booking_quick_action_dates` → `date[]`: l'indice `(property_id, action_date)` serve la query
>   "azioni rapide del giorno X su tutte le prenotazioni". Con un array servirebbe un GIN, meno naturale
>   di un B-tree; il documento dice "da valutare in base al volume reale di quella query", che non è
>   stato misurato (oggi 0 righe).
>
> **Un effetto collaterale utile**: applicare la migration di partizionamento ha fatto emergere che il
> timeout comandi di default (30 s) non basta a una migration che riscrive dati — la copia di 786 MB
> falliva a metà. Aggiunta `ModuleConnectionStrings.DesignTimeCommandTimeoutSeconds` e applicata a tutte
> e 11 le `DesignTimeDbContextFactory`, così il problema non si ripresenta su un altro modulo.

**Nota storica (pre-implementazione)**: questo documento era di sola analisi/decisione — nessuna
modifica a schema o codice era stata applicata. Le voci ✅ erano decisioni prese ma non ancora
implementate.

| # | Area | Intervento | Stato/Priorità |
|---|---|---|---|
| 1 | Partizionamento | `booking_resource_consumptions`/`_availability_daily`: annuale, anno solare — **unico caso dove il partizionamento è significativamente utile (§2.1: ~1,02M righe a regime, la retention non lo riduce)** | ✅ Deciso (§2) — unica priorità reale di partizionamento oggi |
| 2 | Partizionamento | `notification_inapp_messages`/`notification_deliveries` — ❌ **rivisto 2026-09-18: non partizionare.** A regime sono tabelle di 40-60k righe (§2.1): non giustificano l'infrastruttura. Sostituito con `DELETE` a lotti per la potatura una tantum + job mensile ordinario | ❌ Chiuso — nessun partizionamento |
| 3 | Partizionamento | `fiskaly_receipts`: tabella calda 36 mesi + archivio S3 | ✅ Deciso (§9.1) — **ma nessuna urgenza esecutiva**, dati reali confermano volume trascurabile (§2.1: ≤~7.800 righe anche nello scenario peggiore) |
| 3b | Partizionamento | `payment_transaction_charges` — ❌ **non partizionare**: anche senza il dato mancante su `online_transaction_details`, il totale attuale (~41,5k) è già sotto soglia in qualunque scenario (§2.1) | ❌ Chiuso — nessun partizionamento |
| 3c | Partizionamento | `payment_ledger_entries` / `notification_marketing_campaign_recipients` | ❌ **Chiuso, confermato dal titolare 2026-09-18: no** — non serve approfondire oltre, coerente con la conclusione già raggiunta per le tabelle comparabili |
| 4 | EXCLUDE gist | `catalog_pricing_tariff_periods`, `booking_resale_offers`, time slot, periodi online | ✅ **Business e `btree_gist` confermati (§8)** — resta solo la bonifica dei dati storici prima della migrazione |
| 5 | Retention job | Ritarare `TruncateEmailLogBodies` a 150gg, `notification_inapp_messages`/switchboard alle finestre confermate dal titolare | 🟠 Tutte le finestre chiuse (§1), pronto per implementare |
| 6 | BRIN | Tutte le colonne temporali in §3 | 🟠 Nessuna informazione mancante, pronto per implementare |
| 7 | Generated column | Lista finale in §5.1 (5 colonne approvate, 1 esclusa) — ✅ **verifica "nessuna modifica manuale in-place" completata per tutte** (§14.3) | ✅ Pronto per implementare |
| 8 | property_id | Denormalizzare su `payment_ledger_entry_lines`/`pos_charge_items` | 🟠 Nessuna informazione mancante, pronto per implementare |
| 9 | ON CONFLICT | Tabella `payment.webhook_events` — elenco eventi verificato nel codice (§6): solo `payment_intent.succeeded`/`checkout.session.completed` oggi | ✅ Pronto per implementare |
| 10 | Fix puntuali | §9 — tutte le sotto-voci decise e testate: `fiskaly_receipts` (§9.1, ✅), `fiskaly_receipt_logs`/sequenze (§9.2, ✅), `fiskaly_response` TOAST (§9.3, ✅ testato su dati reali: nessuna azione speciale, il default Postgres va già bene), `fiscalprint_printer_departments` (§9.4, ✅ lascia 3° indice), `booking_number_sequences` (§9.5, ✅ a costo zero) | ✅ Pronto per implementare |
| 11 | Viste | ✅ **Deciso 2026-09-18**: procede solo `fiskaly.v_daily_cash_closure` (§11.2). Tutte le altre non per ora (§11.1, §11.4, §11.5 — anche quelle grounded). §11.6 ritirata (già risolta nella scheda) | ✅ §11.2 pronta per implementare; resto chiuso/non per ora |
| 12 | Array/tabelle | ✅ Tutte le decisioni prese (§13): `customer_tags` resta `text[]`, `catalog_service_sale_item_weekdays` e §13.1 approvati, §13.2 non per ora — chiuso, non aggiungerne altre | ✅ Deciso — da implementare |
| 13 | `INCLUDE` | Non applicare per ora (§14.4) | ❌ Chiuso |
