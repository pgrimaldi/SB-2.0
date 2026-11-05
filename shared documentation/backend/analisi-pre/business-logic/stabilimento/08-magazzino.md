# stabilimento — Magazzino

> **Voce di menù**: ottava icona della sidebar (`_gotoWarehouse` → `components/warehousetop/...`,
> componente `WarehouseTopParent`, rotta `/warehouse`). Gestione delle **giacenze degli articoli
> fisici** a corredo (ombrelloni, lettini, sdraio, sedie regista, gazebo, ecc.) e visione dei
> **consumi** generati dalle prenotazioni.
>
> **Ruoli**: visibile ad `admin` e a chi ha licenza non‑`basic` (escluso `marketing`, `affiliate`,
> `waiter`).
>
> **Metodo**: action file `topwarehouse.js` + `warehouse.controller.js` (156 righe: `addwarehouse`,
> `viewwarehouse`, `bookingdetails`) + `booking.controller.js:checkItemsAvailability` (controllo in
> prenotazione) + `serviceHelper.serviceReservationCounts` / `additional_items_counts_details` /
> `booking_items_counts_details` (registrazione consumi). Scheda DB:
> [`warehouses.md`](../../db/warehouses.md),
> [`booking_items_counts_details.md`](../../db/booking_items_counts_details.md),
> [`additional_items_counts_details.md`](../../db/additional_items_counts_details.md).

---

## 1. Struttura dati

- **Una sola riga `warehouses` per stabilimento**, campo `items` = **JSON array** di oggetti:
  `{ name, it, quantity, threshold, isEnable }` — nome (EN), nome (IT), quantità disponibile, soglia di
  riordino/allarme, flag `isEnable` (nel codice esclude l'articolo dal controllo di giacenza, §5; per
  il committente è l'interruttore dell'avviso soglia).
- I consumi non sono sottratti da `quantity`: la disponibilità **residua** per una data si calcola
  come `quantity − quantità già prenotata per quelle date` (letta dai contatori di prenotazione).

---

## 2. Visualizzare il magazzino

**Azione** — `POST viewwarehouse`: elenco articoli con quantità e soglia.

**Logiche**:

- Restituisce l'array `items` parsato.
- **Mascheramento**: se la richiesta **non** è autenticata (`req.user` assente), `quantity` e
  `threshold` sono restituiti **mascherati** (`maskDecimal`); se autenticata, in chiaro. (La rotta è
  ad auth opzionale.)

**Calcoli**: nessuno. **Eventi**: nessuno.

---

## 3. Modificare le giacenze

**Azione** — `POST addwarehouse` (upsert per `propertyId`): se non esiste la riga la crea, altrimenti
**sostituisce** l'intero array `items` con quello inviato (nome, IT, quantità, soglia, `isEnable`),
aggiornando `createdBy`.

**Logiche**: nessuna validazione server (quantità negative ammesse); nessun diff/merge — è una
riscrittura totale dell'array.

**Calcoli**: nessuno. **Eventi**: **nessuno** lato server (nessuna email/notifica). La soglia però non
è solo un dato visualizzato: il gestionale la usa per l'avviso «in esaurimento» in mappa (§4bis).

---

## 4. Consumi da prenotazioni

**Azione** — `POST warehouse/bookingdetails` (`bookingdetails`): mostra, per articolo e per data,
quante unità sono impegnate dalle prenotazioni.

**Logiche**: incrocia `warehouses.items` con i contatori di consumo
(`booking_items_counts_details` per gli articoli inclusi nei pacchetti,
`additional_items_counts_details` per gli articoli aggiuntivi), popolati alla creazione/modifica di
ogni prenotazione (`serviceReservationCounts` / helper dedicati).

**Calcoli**: per articolo/data, `impegnato = Σ quantità delle prenotazioni attive`;
`residuo = quantity − impegnato`.

**Eventi**: nessuno (lettura).

---

## 4bis. Avviso «in esaurimento» in mappa (lato client)

**Dove**: gestionale, mappa (`components/reserveArticle/ReserveArticle.js` → `callWarehouseAPI` in
`front-pre/src/services/WarehouseItemService.js:44-57`), sezione **News** della mappa.

**Logiche e calcoli**: al cambio delle date selezionate il client legge `viewwarehouse` e
`sb/booking/counts` sulle date della mappa; per ogni articolo, se `quantity − total < threshold`
aggiunge il messaggio «<nome IT> in esaurimento» / «<name> running out» agli avvisi
(`reservationWarning`). `total` è la somma delle prenotazioni dell'intervallo, non il minimo giorno
per giorno, e ignora la fascia. Il confronto non legge `isEnable`: l'avviso compare per tutti gli
articoli.

**Eventi**: nessuno lato server; l'avviso è ricalcolato a ogni apertura e non viene memorizzato.

---

## 5. Controllo disponibilità in fase di prenotazione

**Dove**: dentro `POST /api/sb/booking` (solo per i **nuovi** inserimenti), funzione
`checkItemsAvailability`.

**Logiche e calcoli**:

1. Legge la riga `warehouses` della property.
2. Costruisce `warehouseTotals` = somma delle `quantity` per **nome normalizzato** (minuscolo,
   trim), **saltando** gli articoli con `isEnable === 0`. È presente un **alias hardcoded**:
   `"sedia regista"` → `"Directors Chair"` (da portare a dato/lookup nella riscrittura).
3. Somma le **quantità richieste** dalla prenotazione: dagli articoli inclusi nei pacchetti
   (`packageDetails[].quantity`) e dagli articoli aggiuntivi (`additionalList[].value`), per nome
   normalizzato, sulle date della prenotazione.
4. Sottrae le quantità **già prenotate** per quelle stesse date (contatori di prenotazione).
5. Se per qualche articolo `richiesto > (totale − già prenotato)` → la prenotazione è **bloccata**
   con "Insufficient inventory for selected items" e l'elenco degli articoli in carenza
   (`shortages`).

**Limiti noti**: il controllo non è atomico rispetto a prenotazioni concorrenti (vedi
[`docs/01`](../../../progettazione/01-criticita-e-indicazioni-backend.md) §11.7); si applica solo ai nuovi
inserimenti, non alle modifiche; l'accoppiamento per **nome stringa** (con alias hardcoded) è fragile.

**Eventi**: nessuno diretto; il blocco impedisce la creazione della prenotazione.

---

## 6. Riepilogo "calcoli / logiche / eventi" della voce Magazzino

- **Calcoli**: `residuo = quantity − impegnato`; `warehouseTotals` per nome normalizzato;
  confronto `richiesto` vs `disponibile` in prenotazione.
- **Logiche**: una riga JSON per stabilimento; upsert con riscrittura totale dell'array; esclusione
  articoli `isEnable = 0`; normalizzazione nomi + alias hardcoded; mascheramento quantità/soglia se non
  autenticato; controllo disponibilità solo su nuove prenotazioni e non atomico.
- **Eventi**: **nessuno** lato server. La soglia (`threshold`) produce solo l'avviso «in esaurimento»
  calcolato dal client nella sezione News della mappa (§4bis); nessuna email o notifica.

> **Decisione del committente (2026-10)**
>
> - **Avviso soglia** (A1, A2): `isEnable` («Abilita») è l'interruttore dell'avviso soglia, non
>   l'esclusione dal controllo di giacenza; l'esclusione del §5 punto 2 sparisce. L'avviso scatta quando
>   la disponibilità del **giorno corrente** scende **sotto** la soglia, compare nella campanella e nella
>   sezione News della mappa, lo ricevono i ruoli che vedono il magazzino (non cameriere, affiliato,
>   marketing) e si invia **una volta**, di nuovo solo dopo che la scorta è risalita sopra la soglia.
> - **Elenco fisso** (A3): gli articoli principali sono un elenco fisso di piattaforma; il gestore crea
>   solo quelli con «Altro». L'elenco non si amplia dal pannello.
> - **Eliminazione** (A4): bloccata se l'articolo è usato in pacchetti (anche listini convenzione), con
>   l'elenco dei pacchetti.
> - Quantità 0 ammessa, soglia ≤ quantità.
>
> Dettaglio in `documentazione-locale/catalog-api-analisi.md` §4.1.
