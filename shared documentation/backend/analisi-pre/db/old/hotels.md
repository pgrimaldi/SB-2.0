# Tabella `hotels`

> Fonti: schema completo (`SHOW CREATE TABLE`) e dati reali (100 righe), model
> [`back-pre/app/models/hotel.model.js`](../../back-pre/app/models/hotel.model.js) (`sequelize.define("hotels", ...)`),
> associazioni in
> [`back-pre/app/models/index.js:397-424`](../../back-pre/app/models/index.js), controller
> [`back-pre/app/controllers/hotel/hotel.controller.js`](../../back-pre/app/controllers/hotel/hotel.controller.js)
> (letto per intero, 382 righe) e service
> [`back-pre/app/Service/hotelService.js`](../../back-pre/app/Service/hotelService.js) (letto per intero
> nelle parti di configurazione, 754 righe totali), DTO
> [`back-pre/app/models/dto/hotel.dto.js`](../../back-pre/app/models/dto/hotel.dto.js), routing
> [`back-pre/app/routes/hotel.routes.js`](../../back-pre/app/routes/hotel.routes.js), frontend
> `front-pre/src/components/agreements/**` (sezione "Agreements"/Convenzioni) e
> `front-modern-pre/src/pages/Agreements/StructuresList/{Structures,CreateStructure}.tsx` +
> `StructureDetail/HotelInfoUpdate.tsx` (via `remote/SummerBooking/Agreements/AgreementsRestApi.ts`).
>
> **Nota di contesto**: nonostante il nome, **non è un modulo di prenotazione alberghiera** (stanze,
> check-in/check-out) e non è un'entità indipendente dallo stabilimento beach-club. È l'anagrafica delle
> **strutture partner** ("convenzioni") con cui uno stabilimento (`property`) ha un accordo commerciale —
> un cliente che prenota il beach-club può in aggiunta consumare un listino/voucher di una di queste
> strutture. Verificato: `hotels.propertyId` è `NOT NULL` con FK reale verso `properties.id`
> (`ON UPDATE CASCADE`) — un hotel esiste solo come figlio di una property, non ha identità propria. Il
> collegamento con l'effettivo consumo in una prenotazione avviene tramite `bookings.hotelId` (colonna
> della tabella `bookings` stessa, commento a livello DB "Foreign key to hotels table for convention
> agreement") — vedi la nota di dominio C1/C2 più sotto.
>
> Rispetto alle altre 17 tabelle già documentate in questa cartella, questo modulo è **strutturato
> diversamente e più recente**: usa un vero layer Service (non solo controller+Sequelize diretto), DTO con
> validazione, FK dichiarate a livello DB, commenti SQL su ogni colonna, e **verifica di ownership reale
> tramite `req.userId` → `user.propertyId`** invece di fidarsi di un `propertyId` inviato dal client — a
> differenza del pattern IDOR sistemico già documentato per quasi tutte le altre tabelle di questo dominio
> (vedi [01-criticita-e-indicazioni-backend.md §5.1](../../docs/01-criticita-e-indicazioni-backend.md)).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### propertyId
- **Tipo**: `int`, `NOT NULL`
- **Vincoli**: **FK reale** verso `properties.id` (`ON UPDATE CASCADE`, nessun `ON DELETE` esplicito →
  di fatto `RESTRICT`, il default MySQL) — a differenza della quasi totalità delle altre tabelle di questo
  dominio, qui la relazione è vincolata a livello DB, non solo applicativo.
- **Descrizione**: lo stabilimento che ha registrato la struttura partner. Risolto **sempre lato server**
  da `req.userId` (mai dal body della richiesta) in `hotelService.js` (`getUserPropertyId`, righe 17-26) —
  usato per lo scoping di ownership in create/list/details/update/delete.
- **Nullabile**: No
- **Esempio di valori**: 96 — su una query estesa a tutte le 25 righe esistenti, 24 su 25 appartengono a
  questa singola property: dato fortemente concentrato, coerente con un DB di sviluppo/test più che con un
  campione realistico multi-tenant.

### name
- **Tipo**: `varchar(255)`, `NOT NULL`
- **Descrizione**: nome della struttura partner (es. "H. Cormoran", "Residence Lido Europa").
  Unico campo davvero obbligatorio in creazione (`HotelDto.validateCreate`, `hotel.dto.js:44-48`).
- **Nullabile**: No
- **Esempio di valori**: "Hotel Mario copia", "Grand Hotel" — "Hotel Mario copia" compare più volte nella
  query estesa, probabile dato duplicato/di test (coerente con la funzione `duplicateHotel`, che genera
  automaticamente nomi con suffisso "copia").

### city / postalCode / province / streetName / streetNumber / phone / email / note
- **Tipo**: `varchar`/`text`, tutti opzionali (`allowNull: true`)
- **Descrizione**: dati anagrafici/di contatto della struttura partner, mostrati nelle schede
  "Strutture"/"Structures" del gestionale. Nessuna delle due implementazioni frontend esaminate valida
  formato email/telefono lato UI in modo bloccante oltre a quanto imposto dal browser. **Candidato a
  enumerazione** per `province`: se il vocabolario si limita alle sigle delle province italiane (insieme
  codificato e relativamente stabile, ~110 valori), una tabella di lookup con FK sarebbe preferibile a un
  `varchar` libero, per garantire coerenza dei valori ed eventuali traduzioni/etichette estese.
- **Nullabile**: Sì (tutti)
- **Esempio di valori**: per lo più `NULL` su tutte le 25 righe (15-18 righe su 25 a seconda del campo);
  quando presenti: `city`="Rome", `postalCode`="00100", `province`="RM", `streetName`="Via Roma",
  `streetNumber`="123". **Dati sporchi/di test individuati** su una query estesa: `city`/altri campi
  indirizzo con valore letterale "string" (placeholder di test, non un vero indirizzo); `phone` con un
  valore palesemente errato "+39 39284792387489279384789234" (troppo lungo per essere un numero di telefono
  valido); `email` con indirizzi reali tipo "mariohotel@example.com" ma anche "m.accattoli@qbitsoft.it" —
  quest'ultima è l'email di un membro del team di sviluppo, segno di un test manuale e non di un dato di
  produzione; `note` con valori "test", "Test 2", "Simple note" — ulteriore conferma della natura di
  ambiente di sviluppo/test di questo DB.

### isDeleted
- **Tipo**: `tinyint(1)`, default `0`
- **Valori osservati**: `0` (89/100), `1` (11/100)
- **Descrizione**: soft-delete. `deleteHotel` (`hotelService.js:243-274`) imposta `isDeleted: true` e, in
  modo esplicito e verificato, **effettua anche un soft-delete a cascata su tutti i `hotel_price_lists`
  collegati** (`db.hotelPriceList.update({isDeleted:true}, {where:{hotelId:id, isDeleted:false}})`,
  righe 261-264) — un cascade applicativo corretto, non gestito dal DB (nessun `ON DELETE CASCADE` nella
  FK, cosa comunque coerente con l'uso del solo soft-delete).
- **`includeDeleted`**: più punti dell'API (`getHotelList`, `getHotelById`, `hotelExists`,
  `getHotelPriceListsByHotelId`) accettano esplicitamente un flag `includeDeleted` per poter comunque
  risolvere/mostrare un hotel (o i suoi listini) già soft-cancellato quando referenziato da una
  prenotazione esistente — un dettaglio di design corretto per non "rompere" la visualizzazione di
  prenotazioni storiche che referenziano una struttura nel frattempo rimossa dal catalogo.
- **Nullabile**: Sì
- **Esempio di valori**: su una query estesa a tutte le 25 righe esistenti: `1` in 19 casi, `0` in 6 —
  proporzione invertita rispetto al campione da 100 righe citato sopra (dove `0` era maggioritario);
  coerente con un DB di sviluppo dove molte righe di test vengono create e poi soft-cancellate.

### createdAt / updatedAt
- **Tipo**: `datetime`, popolati da Sequelize (`defaultValue: Sequelize.NOW`) — standard.
- **Nullabile**: Sì
- **Esempio di valori**: datetime standard, es. "2023-09-14 11:30:00"

---

## Endpoint (tutti `POST`, tutti sotto `/api/sb/hotel*`)

| Path | Funzione | Note |
|---|---|---|
| `/api/sb/hotel` | `createHotel` | crea, `propertyId` sempre da `req.userId` |
| `/api/sb/hotel/list` | `getHotelList` | lista con conteggi aggregati (vedi sotto), ricerca per nome, paginazione, ordinamento whitelisted |
| `/api/sb/hotel/details` | `getHotelDetails` | dettaglio singolo hotel |
| `/api/sb/hotel/update` | `updateHotel` | aggiornamento parziale (ogni campo mantiene il valore esistente se non fornito, pattern `??`) |
| `/api/sb/hotel/delete` | `deleteHotel` | soft-delete + cascata su `hotel_price_lists` |
| `/api/sb/hotel/delete/bulk` | `deleteHotelsBulk` | soft-delete multiplo, risultato per-id (successo/fallimento) |
| `/api/sb/hotel/duplicate` | `duplicateHotel` | duplica hotel (nome → `"<originale> copia"`, contatti azzerati) **e tutti i suoi listini/voucher attivi** |
| `/api/sb/hotel/duplicate/bulk` | `duplicateHotelsBulk` | duplicazione multipla, risultato per-id |

`getHotelList` calcola, per ciascun hotel, tre contatori tramite **subquery correlate** direttamente nella
query Sequelize (`hotelService.js:107-119`, non calcolati in memoria):
- **totalVouchers**: numero di `hotel_price_lists` con `isVoucher=1` collegati.
- **usedVouchers**: numero di `hotel_payments` (non cancellati) che referenziano un `hotel_price_lists`
  con `isVoucher=1` — cioè quante volte i voucher di quell'hotel sono stati effettivamente consumati in
  una prenotazione (dominio C1, tabella `hotel_payments` non documentata in questa scheda C2).
- **usedPriceLists**: come sopra ma per i listini non-voucher.

Questi tre contatori sono anche gli unici campi accettati come chiave di ordinamento
(`orderableFields = ["name","totalVouchers","usedVouchers","usedPriceLists"]`, riga 90) — scelta
esplicita per evitare `ORDER BY` su colonne arbitrarie non whitelisted.

---

## Osservazioni verificate nel codice

- **Ownership reale, non IDOR**: a differenza di quasi tutte le altre tabelle di questo dominio,
  `propertyId` non viene mai letto da `req.body` in nessuna delle funzioni di `hotel.controller.js` —
  sempre risolto da `req.userId` tramite `getUserPropertyId`. Un utente non può quindi, a differenza dei
  pattern IDOR già documentati altrove, operare su hotel di una property diversa dalla propria semplicemente
  cambiando un id nel payload.
- **Ma il controllo di ruolo a livello di route è disattivato**: tutte le route di `hotel.routes.js`
  (incluse quelle su questa tabella) sono protette solo da `authJwt.verifyToken`; il middleware
  `authJwt.isManager` è **commentato** su ogni singola route (`/*, authJwt.isManager*/`, verificato riga
  per riga da riga 32 a riga 382). Combinato col punto precedente, l'impatto pratico è più limitato del
  solito pattern IDOR (nessun accesso cross-property), ma **qualunque utente autenticato della property**
  (anche un ruolo non gestionale, es. bagnino/cassiere — vedi [`roles.md`](roles.md)) può oggi
  creare/modificare/cancellare le strutture convenzionate e i loro listini, non solo i ruoli manageriali
  come l'intento del codice commentato suggerisce.
- **Un solo record MySQL locale con 16 property distinte** su 100 hotel campionati — nessuna anomalia,
  riportato solo per contesto quantitativo (non tutte le property hanno strutture convenzionate).

---

## Nota architetturale C1/C2 (per la riscrittura)

Questa tabella (insieme a `hotel_price_lists` e `hotel_message_templates`) rappresenta la parte di
**configurazione/catalogo (C2)** del dominio "Convenzioni": anagrafica delle strutture partner e dei loro
listini/voucher, gestita con CRUD proprio e scoping per property. È **isolabile e mappabile direttamente**
su un bounded context .NET "Hotel Configuration". La parte transazionale (quale prenotazione beach-club ha
consumato quale voucher/listino — tabelle `hotel_payments`/`hotel_reports`, non documentate in questa
scheda) **non** è un dominio C1 indipendente: nel codice attuale è creata/cancellata come effetto
collaterale del ciclo di vita della prenotazione beach-club in `booking.controller.js`, non tramite un
endpoint dedicato di questo modulo — va quindi modellata, nella riscrittura, come un value/child object
dell'aggregato `Booking` (C1) che consulta in sola lettura il catalogo qui descritto (C2), non come una
sua sezione scritta da questo stesso modulo.
