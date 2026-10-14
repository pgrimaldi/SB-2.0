# API su intervalli archiviati — errore specifico

> **Stato**: nuovo requisito tecnico (interno, 2026-09-18), derivato dalla politica di retention già
> **validata dal titolare** in [`docs/80-retention.md`](../../progettazione/80-retention.md) (elaborazione tecnica)
> e [`docs/82-retention-data.md`](../../progettazione/82-retention-data.md) (verbale delle scelte del proprietario —
> fonte autorevole per i tempi esatti). Seconda scheda di `analisi-post/bl/`: come [`01-monitoraggio-inattivita-stabilimenti.md`](01-monitoraggio-inattivita-stabilimenti.md),
> è logica di business **nuova**, senza controparte nel legacy (`back-pre` non ha mai avuto archiviazione:
> tutto resta a tempo indeterminato nel DB caldo).

## 1. Requisito

Quando un'API viene interrogata per un **intervallo di date** i cui dati sono stati **archiviati**
(spostati su storage freddo e rimossi dal DB operativo, per §3 "Azione a scadenza: 📦" di
`80-retention.md`), non deve rispondere con una lista vuota o un 404 generico indistinguibile da "l'entità
non esiste" — deve restituire un **errore specifico**, machine-readable, che segnali che i dati esistono
ma **non sono più online** perché la richiesta cade (in tutto o in parte) oltre la finestra operativa di
quel dominio.

**Perché non basta una lista vuota**: chi chiama (frontend gestionale, integrazioni esterne, supporto)
deve poter distinguere "0 risultati nel periodo" da "periodo fuori dalla finestra online, dati altrove" —
altrimenti un report o uno storico sembra silenziosamente incompleto invece di segnalare il limite.

## 2. Regola (decisa dal committente, 2026-09-18)

- Ogni endpoint che accetta un **filtro di intervallo** (`from`/`to`, o un identificatore di stagione) su
  un dominio soggetto ad archiviazione **conosce la propria finestra online corrente** (§4) e, prima di
  eseguire la query, verifica se l'intervallo richiesto la supera.
- **Intervallo interamente oltre la finestra online** → errore specifico (§3), nessuna query eseguita.
- **Intervallo parzialmente oltre la finestra** (es. `from` in zona archiviata, `to` in zona online) →
  risposta **parziale**: l'API serve la porzione ancora online e segnala esplicitamente (in `meta` o
  equivalente) quale parte è stata esclusa perché archiviata, invece di rifiutare l'intera richiesta.
- La finestra online è **per dominio/tabella**, non globale: un intervallo può essere valido per le
  prenotazioni (`booking_reservations`, online più a lungo) e già archiviato per le notifiche in-app
  (`notification_deliveries`, finestra corta) — vedi §4. La finestra è **uguale per tutti gli
  stabilimenti**: nessuna variazione per piano di licenza o accordo commerciale.
- La finestra è calcolata dallo **stato reale del DB** al momento della richiesta (non dal valore nominale
  di policy): se il job di archiviazione è in ritardo, `args.onlineFrom` riflette la finestra effettiva,
  evitando falsi errori quando il job non è ancora passato.
- **Il dato non è perso**: `80-retention.md` §0.3 definisce l'archiviazione come spostamento su storage
  freddo, non cancellazione. Il recupero di un intervallo archiviato resta però un **processo manuale/
  interno** (nessun endpoint self-service di "dissotterramento" in questa fase).
- **Endpoint di reportistica/aggregati** (statistiche, revenue) che coprono sia dati online sia dati già
  compattati in aggregati (es. contatori di consumo, §4): il comportamento (restituire l'aggregato invece
  dell'errore) si stabilisce **caso per caso, endpoint per endpoint** — non c'è una regola unica valida
  per tutti i domini.

## 3. Contratto d'errore (Problem Details)

Segue lo standard già definito in
[`docs/pre/standard-risposta-api-dotnet.md`](../../progettazione/pre/standard-risposta-api-dotnet.md) §3 (RFC 9457
esteso con `code`/`args`), stesso principio "il backend restituisce codici, il frontend traduce":

```jsonc
{
  "type": "https://errors.summerbooking/data/archived-range",
  "status": 410,                                    // Gone: la risorsa è esistita, è stata rimossa dal DB operativo
  "code": "data.archived_range",
  "args": {
    "domain": "booking.reservations",
    "requestedFrom": "2022-06-01",
    "requestedTo": "2022-08-31",
    "onlineFrom": "2024-01-01"                       // limite corrente della finestra online per il dominio
  },
  "traceId": "00-abc123...",
  "title": "Requested range is outside the online retention window"
}
```

- `status`: **410 Gone**, non 404 — la risorsa è esistita ed è stata rimossa dal DB operativo, non "mai
  trovata". Estende la tabella degli status HTTP di `standard-risposta-api-dotnet.md` §5 (che oggi non
  elenca 410).
- `code` namespacato e stabile: `data.archived_range` (coerente con `booking.umbrella_overlap` ecc. dello
  standard esistente).
- `args.domain` identifica la tabella/dominio logico (§4), utile perché una stessa richiesta può toccare
  più domini con finestre diverse (es. un report che unisce prenotazioni e pagamenti).
- `args.onlineFrom` è **calcolato al momento della risposta** (la finestra scorre nel tempo, non è una
  data fissa) — il frontend può mostrarla per spiegare il limite ("dati disponibili da…").

## 4. Finestre online per dominio (vista sintetica ad uso API)

Riferimento **sintetico**, non sostitutivo: i tempi esatti e le loro motivazioni/decorrenze restano in
`80-retention.md` §3/§7 e nel verbale `82-retention-data.md` §3/§6 (colonna "Considerazioni / scelta del
proprietario" = decisione finale). Qui solo i domini che espongono un'API con filtro di intervallo dove
questo errore è rilevante:

| Dominio (API) | Tabelle | Finestra online (DB caldo) | Poi | Fonte |
|---|---|---|---|---|
| Prenotazioni ombrellone — testata | `booking_reservations` | stagione corrente **+ N‑2** (estesa dal titolare rispetto a N‑1) | 📦 + pseudonimizzazione PII | `82-retention-data.md` §3.1 riga 1, chiarimento Q1/Q2 |
| Prenotazioni ombrellone — dettaglio (griglie prezzo, snapshot) | `booking_lines` e affini | stagione corrente **+ N‑2** | 🗑️ (il dettaglio, non la testata) | idem, chiarimento Q1/Q2 |
| Prenotazioni di soli servizi | `booking_service_lines` e affini | fino a fine stagione **+ stagione precedente** | 📦 testata economica / 🗑️ dettaglio | `82-retention-data.md` §3.1 riga 3 |
| Contatori consumo inventario (report occupazione) | `booking_resource_consumptions` | stagione corrente + N‑1, poi aggregato mensile/stagione | 🗑️ dettaglio (resta l'aggregato) | `82-retention-data.md` §3.1 riga 4 |
| Assenze / sharing / blocchi mappa | tabelle assenze/sharing | N‑2 (estesa dal titolare) | 🗑️ | `82-retention-data.md` §3.1, chiarimento Q1/Q2 |
| Transazioni e movimenti pagamento | `payment_transaction_*` | 24 mesi | 📦 fino a 10 anni | `82-retention-data.md` §3.2 riga 1 |
| Scontrini fiscali elettronici | `fiskaly_receipts` e affini | **36 mesi** (il titolare ha esteso la proposta iniziale ~24 mesi; vale il valore della riga specifica, non il cheat sheet generico C4 che bundla anche movimenti/rimborsi a 24 mesi) | 📦 fino a 10 anni | `82-retention-data.md` §3.3 riga 1 |
| Anagrafica cliente finale (storico cliente) | `identity_customer_customers` | **48 mesi** dall'ultima prenotazione (estesa dal titolare rispetto a 24–36) | 🔒 pseudonimizzazione | `82-retention-data.md` §3.4 riga 2 |
| Notifiche in-app (storico notifiche stabilimento) | `notification_deliveries`/`notification_inapp_messages` | **6 mesi** (il titolare ha scelto il limite inferiore del range 6–12) | 🗑️ | `82-retention-data.md` §3.6 riga 2 |
| Registro chiamate ricevute (centralino) | `switchboard_*` | **6 mesi** (il titolare ha dimezzato la proposta di 12 mesi) | 🔒/🗑️ | `82-retention-data.md` §3.7 riga 1 |
| Storico conversazioni chatbot | `chatbot_history`-equivalente | 12 mesi | 🔒/🗑️ | `82-retention-data.md` §3.8 riga 1 |

> Questa tabella **decade** ad ogni futura revisione di `80`/`82`: non duplicare i valori altrove, solo
> linkare a questa scheda (o direttamente a `82-retention-data.md`) dal codice/commenti.

> **Aggiornamento 2026-09-21 — le stesse finestre ora esistono come dati.** Dal 2026-09-21 i valori qui
> sopra sono seminati in
> [`privacy_retention_policies`](../db/privacy/privacy_retention_policies.md) (§ Seed): una riga per
> `schema.tabella`, con durata, unità e azione. **È da lì che il middleware deve leggere la finestra
> nominale**, non da una costante in codice né da questa tabella — che resta documentazione per capire
> *perché* i numeri sono quelli.
>
> Resta valido quanto detto in §2: `args.onlineFrom` si calcola sullo **stato reale del DB**, non sul
> valore nominale di policy. Le due cose convivono così: la policy dice *entro quando* il job avrebbe
> dovuto archiviare, il DB dice *fin dove* ha effettivamente archiviato, e l'`onlineFrom` restituito al
> client è il secondo. Se il job è in ritardo la finestra reale è più larga di quella nominale, e serve
> più dati, non meno — mai un 410 su dati che ci sono ancora.
>
> Le durate in **stagioni** (le righe C1 di questa tabella) usano `duration_unit = seasons`, non una
> conversione in mesi: il motivo è spiegato nel § Seed di quella scheda.

## 5. Riferimenti

- [`docs/80-retention.md`](../../progettazione/80-retention.md) — politica di retention (elaborazione tecnica),
  §3 tabellone per categoria, §6 job di archiviazione, §7 cheat sheet dei tempi.
- [`docs/82-retention-data.md`](../../progettazione/82-retention-data.md) — verbale con le scelte finali del
  titolare (fonte autorevole per i valori esatti; alcune extend i tempi proposti in `80-retention.md`).
- [`docs/pre/standard-risposta-api-dotnet.md`](../../progettazione/pre/standard-risposta-api-dotnet.md) — formato
  Problem Details/`code` seguito in §3 di questa scheda.
- [`docs/20-jobs.md`](../../progettazione/20-jobs.md) — i job di archiviazione che spostano i dati (determinano il
  valore reale di `onlineFrom` in ogni momento, §2).
