# Tabella `online_reservations`

> Fonti: schema e dati reali (16 righe), model
> [`back-pre/app/models/onlinereservation.model.js`](../../back-pre/app/models/onlinereservation.model.js)
> (ORM `db.onlinereservation`), controller
> [`onlinereservation.controller.js`](../../back-pre/app/controllers/onlinereservation.controller.js)
> (`addonlinereservation` → `POST /api/sb/addonlinereservation`: upsert per `propertyId`, aggiorna
> anche `properties.paymentOptions` e `properties.onlineReservation` 0/1; `viewonlinereservation` →
> `POST /api/sb/viewonlinereservation`), lettura in
> [`sysadmin.controller.js`](../../back-pre/app/controllers/sysadmin.controller.js)
> `getOnlineReservationStatusByAdmin` e in `booking.controller.js` `getOnlinereservationList`.
> Frontend `front-pre` (gestionale): pannello "Prenotazione online" (abilita/disabilita, regole);
> `front-customer-pre`: il sito pubblico legge questo stato per mostrare o meno il form di
> prenotazione.
>
> **Nota di contesto** — **configurazione del canale "prenotazione online" per stabilimento** (una
> riga per `propertyId`): se il cliente può prenotare da solo, con quali restrizioni (solo mappa /
> solo periodo / solo location), e i dati carta per l'incasso. È **configurazione (dominio C2)**, non
> transazionale — parzialmente sovrapposta a `properties.onlineBookingOptions` /
> `properties.paymentOptions`. Inclusa qui perché fa parte della feature "Prenotazioni" (canale
> online).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### propertyId
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK). Chiave logica: `findOne({ where: { propertyId } })` in
  upsert. Indicizzato.
- **Esempio di valori**: 122, 110

### createdBy
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `users.id` che ha configurato.
- **Esempio di valori**: 122, 226

### isDisable
- **Tipo**: `tinyint(1)` — default `0` (`false`)
- **Nullabile**: Sì
- **Descrizione**: `true` = prenotazione online **disattivata** per lo stabilimento. `addonlinereservation`
  sincronizza di conseguenza `properties.onlineReservation` (`isDisable == true` → `0`, altrimenti `1`).
- **Esempio di valori**: `0`, `1`

### isEnableMap
- **Tipo**: `tinyint(1)` — default `0`
- **Nullabile**: Sì
- **Descrizione**: `true` = il cliente prenota scegliendo la postazione sulla **mappa**.
- **Esempio di valori**: `1`, `0`

### isEnablePeriod
- **Tipo**: `tinyint(1)` — default `0`
- **Nullabile**: Sì
- **Descrizione**: `true` = prenotazione online consentita solo per determinati **periodi**
  (`period`).
- **Esempio di valori**: `0`

### period
- **Tipo**: `longtext` — array JSON (`null`/`[]` quando `isEnablePeriod = 0`)
- **Nullabile**: Sì
- **Descrizione**: elenco dei periodi/date-range ammessi per la prenotazione online (quando
  `isEnablePeriod`). `viewonlinereservation` fa `JSON.parse(period)`.
- **Campi** (per elemento): `{ startDate, endDate }` (o `name` del periodo stagionale).
- **Esempio di valori**:
  `[{"startDate":"2026-06-01","endDate":"2026-06-30"},{"startDate":"2026-09-01","endDate":"2026-09-15"}]`

### isEnableLocation
- **Tipo**: `tinyint(1)` — default `0`
- **Nullabile**: Sì
- **Descrizione**: `true` = prenotazione online limitata a un sottoinsieme di **postazioni**
  (`locations`).
- **Esempio di valori**: `0`

### locations
- **Tipo**: `longtext` — array JSON (`null`/`[]` quando `isEnableLocation = 0`)
- **Nullabile**: Sì
- **Descrizione**: identificativi/etichette delle postazioni ammesse alla prenotazione online (quando
  `isEnableLocation`).
- **Esempio di valori**: `["A1","A2","A3","B1"]` oppure `[12,13,14]`

### paymentMethod
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: metodo di pagamento configurato per il canale online. Nel campione sempre `null`
  (la configurazione reale dei pagamenti vive in `properties.paymentOptions`, aggiornato dallo stesso
  endpoint).
- **Deprecato**: parzialmente — colonna non valorizzata nei dati; ridondante con
  `properties.paymentOptions`.
- **Esempio di valori**: (nel campione `null`; valore atteso tipo `"stripe"` / `"pay_at_property"`)

### cvc / cardHodler / cardNumber / expiry
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: dati **carta di credito in chiaro** (numero, intestatario — refuso "Hodler", CVC,
  scadenza) accettati e salvati tali e quali da `addonlinereservation`/`viewonlinereservation`. Nel
  campione **sempre `null`** → funzionalità di fatto non usata.
- **Deprecato**: **Sì** (colonne inutilizzate — sempre `null` nel campione) — e comunque **da
  eliminare**: salvare PAN/CVC in chiaro è una violazione PCI-DSS. Vedi
  [`docs/01-criticita-e-indicazioni-backend.md`](../../docs/01-criticita-e-indicazioni-backend.md).
- **Esempio di valori**: (sempre `null`; la colonna non deve esistere)

### email
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì. Indicizzato.
- **Descrizione**: email associata alla configurazione. **Sempre `null`** nel campione → colonna di
  fatto inutilizzata.
- **Deprecato**: parzialmente — non valorizzata.
- **Esempio di valori**: (nel campione `null`; atteso un indirizzo tipo `"prenotazioni@stabilimento.it"`)

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize standard (`createdAt` a volte `null` sui record più
  vecchi)
- **Nullabile**: Sì
- **Esempio di valori**: `2025-05-26 12:25:44`

---

## Osservazioni verificate nel codice (criticità)

- **Dati carta in chiaro**: `cardNumber`, `cvc`, `expiry`, `cardHodler` — colonne per PAN/CVC in
  chiaro. Inutilizzate nel campione ma da rimuovere tassativamente (PCI-DSS). Vedi anche
  `docs/01-criticita-e-indicazioni-backend.md`.
- **Sovrapposizione con `properties`**: lo stesso endpoint scrive `properties.paymentOptions` e
  `properties.onlineReservation` (0/1) **oltre** a questa riga → tre punti di verità per "prenotazione
  online abilitata/pagamenti online" (`online_reservations.isDisable`, `properties.onlineReservation`,
  `properties.onlineBookingOptions`).
- **Doppia negazione**: `isDisable` (true = disattivato) mentre `properties.onlineReservation` usa
  1 = attivo → facile invertire la logica.
- **`period` / `locations` array JSON in `longtext`**, `JSON.parse` senza guardie.
- **`paymentMethod` / `email` colonne non popolate.**
- **`createdAt` nullo** su alcune righe (upsert che non lo tocca).
- **Classificazione / accorpamento in `properties`**: dominio **C2 (Catalogo & configurazione)**, non
  C1. Consigliato **accorparla in `properties`** (o in una `property_online_booking_settings` 1:1),
  eliminando la tripla fonte di verità e le colonne carta. Dettaglio in
  [`docs/03-gestione-dati.md` §5](../../docs/03-gestione-dati.md).
