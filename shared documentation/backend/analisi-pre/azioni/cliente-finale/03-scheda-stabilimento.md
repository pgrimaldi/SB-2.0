# cliente-finale — Scheda stabilimento

> Area funzionale: pagina pubblica del singolo stabilimento con i tab **Panoramica**, **Tour virtuale**,
> **Disponibilità**, **Servizi**, **Leggi prima di prenotare**, la galleria foto, "Vedi sulla mappa",
> il box "Prenotazione servizi" (Listino / Menu / Eventi) e la CTA "Prenota ora".
> **Schermata**: p. 7 / `Pag stabilimento - prenotazione da mappa.png`. Breadcrumb:
> Home > Italia > Calabria > Montauro > On the beach Club.
>
> Logica di business correlata: [`business-logic/systemadmin/01-gestione-stabilimenti.md`](../../business-logic/systemadmin/01-gestione-stabilimenti.md)
> §3.2 (`getPropertyWithBathHouseDetails`, mascheramento contatti se non autenticato) e §7
> (`getOnlineReservationStatusByAdmin`).

---

## 1. Apertura della scheda stabilimento (tab Panoramica)

**Descrizione**: dalla ricerca o da un link diretto l'utente apre la scheda; di default vede
"Panoramica" con descrizione struttura, galleria, indirizzo, mini-mappa, box prenotazione servizi e un
estratto di "Disponibilità".
**Schermata**: p. 7.
**Ruolo**: ospite e cliente autenticato.

**Input → API** (`sb/getPropertyWithBathHouseDetails`; risoluzione slug: `sb/getPropertyIdAndBathhouseFromSlug`)
- `propertyId` (int) **o** `slug` (string)
- `bathHouseId` (int) — `0` ammesso se sconosciuto
- `userId` (int, opzionale) — se loggato: abilita flag "mi piace" e contatti in chiaro
- `languageCode` (int)

**Output ← API**
- `property` ({ id, slug, name, description, address: { street, zip, city, province, region, country },
  phonePrefix, phone, email, whatsapp, lat, lng }) — **contatti mascherati** (`maskEmail`/`maskName`/
  `maskPhone`) se la richiesta **non** è autenticata
- `photos` (string[]) — URL foto per la galleria
- `virtualTourUrl` (string|null) — per il tab "Tour virtuale"
- `services` (array) — `{ key, label, icon, category }` per il tab "Servizi"
- `bathHouse` ({ id, name, phone, email, whatsapp, services[] })
- `onlineReservation` ({ isEnableMap (0/1), period, locations[], isDisable (0/1) }) — configurazione
  prenotazione online
- `isConfiguration` (0/1) — esiste una mappa spiaggia configurata
- `superAdminOnlineFlag` / `isAdminDisableOnLinePayment` (bool) — se l'admin ha bloccato il pagamento
  online (vedi azione 7)
- `commissionAmount` ({ onlineCommission, iframeCommission }) — percentuali usate nel calcolo prezzo
  (vedi [`04-prenotazione-postazioni.md`](04-prenotazione-postazioni.md))
- `isLike` (bool) — solo se `userId` presente
- `paymentOptions` (object) — modalità di pagamento accettate (tutto/acconto/in struttura)
- `priceFrom` (number) — "A partire da …"

**Note**: se `isConfiguration = 0` o `onlineReservation.isEnableMap = 0`, la UI mostra la scheda senza
mappa/prenotazione ("Scopri di più" al posto di "Prenota ora").

---

## 2. Cambio tab: "Tour virtuale"

**Descrizione**: l'utente apre il tour virtuale a 360° dello stabilimento.
**Schermata**: p. 7 (tab "Tour virtuale").
**Ruolo**: ospite.

**Input → API**: nessuna nuova chiamata — usa `property.virtualTourUrl` già ricevuto (iframe/embed).
Se il tour è caricato a parte: `sb/getVirtualTour` con `{ propertyId }` → `{ tourUrl, provider }`.

**Output ← API**: n/a.

---

## 3. Cambio tab: "Disponibilità" (mappa spiaggia)

**Descrizione**: l'utente apre il tab con selettore date, toggle **Stagionale**, selettore
**Giornaliero/…**, pulsante "Modifica ricerca" e la mappa spiaggia con le postazioni.
**Schermata**: p. 7 (sezione "Disponibilità", "Scegli il tuo posto in spiaggia"); mappa pp. 29, 35.
**Ruolo**: ospite.

**Input → API**: vedi [`04-prenotazione-postazioni.md`](04-prenotazione-postazioni.md) azioni 1–3
(`sb/viewconfimaps`, `sb/get-booked-location`, `sb/checkLocationIsAvailable`).

**Output ← API**: vedi stesso file.

---

## 4. Cambio tab: "Servizi"

**Descrizione**: l'utente consulta l'elenco completo dei servizi/dotazioni dello stabilimento,
raggruppati per categoria e con icone.
**Schermata**: p. 7 (tab "Servizi").
**Ruolo**: ospite.

**Input → API**: nessuna nuova chiamata se `property.services` è già completo; altrimenti
`sb/getPropertyServices` con `{ propertyId, languageCode }`.

**Output ← API**
- `serviceGroups` (array) — `{ categoryKey, categoryLabel, items: [ { key, label, icon, note? } ] }`

---

## 5. Cambio tab: "Leggi prima di prenotare"

**Descrizione**: l'utente legge le condizioni/informazioni che lo stabilimento vuole comunicare prima
della prenotazione (regolamento, policy animali, orari, ecc.).
**Schermata**: p. 7 (tab "Leggi prima di prenotare").
**Ruolo**: ospite.

**Input → API** (`sb/getPropertyBookingNotes` / campo dedicato di `viewonlinereservation`)
- `propertyId` (int)
- `languageCode` (int)

**Output ← API**
- `bookingNotes` (string, HTML/markdown) — testo redatto dal gestore
- `cancellationPolicy` (string) — se gestita a parte
- `checkInInfo` / `rules` (string)

---

## 6. Apertura della galleria foto

**Descrizione**: cliccando su una foto della griglia si apre il visualizzatore a schermo intero con
navigazione avanti/indietro.
**Schermata**: p. 7 (griglia foto, frecce ‹ ›).
**Ruolo**: ospite.

**Input → API**: nessuna — usa `photos[]` già ricevuto. Se la galleria è paginata:
`sb/getPropertyPhotos` con `{ propertyId, page, pageSize }` → `{ photos[], total }`.

**Output ← API**: n/a.

---

## 7. Verifica se il pagamento online è abilitato dall'amministratore

**Descrizione**: al caricamento della scheda la UI verifica se un systemadmin ha disabilitato la
prenotazione/pagamento online per quello stabilimento (abbonamento scaduto/sospeso).
**Schermata**: nessun elemento visibile — determina se mostrare o meno il flusso "Prenota ora".
**Ruolo**: ospite e cliente autenticato.

**Input → API** (`sb/getOnlineReservationStatusByAdmin`)
- `propertyId` (int)

**Output ← API**
- `isAdminDisableOnLinePayment` (bool) — semantica da normalizzare (vedi business-logic §7): la UI la
  usa per abilitare/disabilitare la CTA di pagamento
- `message` (string) — "No data found" se lo stabilimento non ha configurazione

**Note**: chiamata condivisa con il gestionale; nella riscrittura conviene inglobarla in
`getPropertyWithBathHouseDetails`.

---

## 8. "Vedi sulla mappa" (posizione geografica)

**Descrizione**: dalla mini-mappa in Panoramica l'utente apre la posizione dello stabilimento su mappa
(zoom, indicazioni stradali).
**Schermata**: p. 7 (riquadro "Vedi sulla mappa").
**Ruolo**: ospite.

**Input → API**: nessuna — usa `property.lat`/`property.lng`/`address` già ricevuti (link a
provider di mappe o mappa embedded).

**Output ← API**: n/a.

---

## 9. Apertura del box "Prenotazione servizi" (Listino / Menu / Eventi)

**Descrizione**: dal box "Prenotazione servizi" l'utente apre uno fra **Listino**, **Menu**, **Eventi**,
oppure "Prenota ora" per i servizi.
**Schermata**: p. 7 (box con pulsanti Listino, Menu, Eventi, "Prenota ora").
**Ruolo**: ospite.

**Input → API**: apre le azioni descritte in
[`05-prenotazione-servizi-e-listino.md`](05-prenotazione-servizi-e-listino.md) (Listino/Menu) e in
[`06-eventi.md`](06-eventi.md) (Eventi).

**Output ← API**: n/a qui.

---

## 10. "Prenota ora" (postazione in spiaggia)

**Descrizione**: il pulsante "Prenota ora" in alto nella scheda porta l'utente al flusso di scelta
postazione sulla mappa.
**Schermata**: p. 7 (bottone "Prenota ora" accanto al nome, e "Scegli il tuo posto in spiaggia").
**Ruolo**: ospite.

**Input → API**: avvia [`04-prenotazione-postazioni.md`](04-prenotazione-postazioni.md).

**Output ← API**: n/a qui.
