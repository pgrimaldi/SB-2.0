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
  riordino/allarme, flag attivo.
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

**Calcoli**: nessuno. **Eventi**: **nessuno** (nessuna email/notifica, nemmeno al superamento della
soglia — la soglia è solo un dato visualizzato in UI).

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
- **Eventi**: **nessuno**. La "soglia" (`threshold`) non genera alcun avviso/email/notifica: è solo un
  indicatore visivo. Da valutare nella riscrittura un evento di sotto‑scorta.
