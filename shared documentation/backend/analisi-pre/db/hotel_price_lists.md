# Tabella `hotel_price_lists`

> Fonti: schema completo (`SHOW CREATE TABLE`) + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **388 righe**, query completa; `id` fino a 421). Feature nuova (aprile 2026). Model
> [`back-pre/app/models/hotelPriceList.model.js`](../../../../../../back-pre/app/models/hotelPriceList.model.js)
> (`sequelize.define("hotel_price_lists", ...)`), associazioni in
> [`back-pre/app/models/index.js:408-409,415-418`](../../../../../../back-pre/app/models/index.js), controller
> [`back-pre/app/controllers/hotel/hotelPriceList.controller.js`](../../../../../../back-pre/app/controllers/hotel/hotelPriceList.controller.js)
> (letto per intero, 288 righe) e service
> [`back-pre/app/Service/hotelPriceListService.js`](../../../../../../back-pre/app/Service/hotelPriceListService.js)
> (letto per intero, 277 righe), DTO
> [`back-pre/app/models/dto/hotel.dto.js`](../../../../../../back-pre/app/models/dto/hotel.dto.js) (classi
> `HotelPriceListDto`/`HotelPriceListItemDto`), frontend
> `front-pre/src/data/actions/managerActions.js:415-445` (`_createHotelPriceList`,
> `_getHotelPriceList`, `_updateHotelPriceList`, `_deleteHotelPriceList`) +
> `front-pre/src/components/agreements/PriceList/PriceList.jsx`,
> `front-modern-pre/src/remote/SummerBooking/Agreements/AgreementsRestApi.ts` (solo lettura, vedi
> criticità).
>
> **Nota di contesto**: listini/voucher venduti da una struttura partner (`hotels`, vedi
> [`hotels.md`](hotels.md)) — **struttura dati praticamente identica a `price_lists`** (stesso set di
> campi `details`/`mainPrices`/`detailPrices`/`priceCalculation`/`packageItemString`/`holidayDates`/
> `suggestedPriceDetails`, stesso commento nel model sorgente: "Price list configuration for hotels,
> similar to property price_lists"), con l'aggiunta di 3 campi specifici per la gestione voucher
> (`isVoucher`, `minBookingDays`, `maxVoucherQuantity`) assenti in `price_lists`. Per la semantica di
> dettaglio dei campi JSON condivisi con `price_lists`, vedi [`price_lists.md`](price_lists.md) — qui si
> documentano solo le differenze e i campi specifici.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): `1` … `421` per 388 righe.

### hotelId
- **Tipo**: `int`, `NOT NULL`
- **Vincoli**: **FK reale** verso `hotels.id` (`ON UPDATE CASCADE`)
- **Descrizione**: struttura partner a cui appartiene il listino/voucher.
- **Nullabile**: No
- **Esempio di valori** (prod): `2`, `6`, `8`, `10` — range `1` … `111`.

### isVoucher
- **Tipo**: `tinyint(1)`, default `0`
- **Valori osservati** (prod, 388 righe): `1` (310, voucher), `0` (78, listino "pacchetto") → in
  produzione i listini hotel sono in stragrande maggioranza voucher.
- **Descrizione**: distingue le due modalità d'uso della riga:
  - **`isVoucher = 0`**: un listino "pacchetto" ordinario, semanticamente identico a un `price_lists`
    (`type = "package"`/`"additional"`) — stesso schema `details`/`mainPrices`/`detailPrices`.
  - **`isVoucher = 1`**: un **voucher** — un buono/coupon che il cliente può riscattare presso la
    struttura partner nell'ambito della convenzione, con vincoli d'uso specifici (`minBookingDays`,
    `maxVoucherQuantity`, vedi sotto).
- **Nullabile**: Sì (di fatto sempre valorizzato per via del default)
- **Esempio di valori**: 0, 1

### type
- **Tipo**: `varchar(50)`
- **Valori osservati** (prod, 388 righe): `package` (355), `additional` (33). *(nota storica:)* `dato reale DB dev`
  (68 righe): `package` (44), `additional` (24) — proporzione più equilibrata di quanto suggerisse il
  campione locale, ma insieme di valori confermato invariato (2 soli valori) — stessa semantica di
  [`price_lists.type`](price_lists.md). **Candidato a enumerazione**: solo 2 valori, stabili su entrambi i
  campioni — ENUM a livello DB sufficiente (nessun bisogno di traduzione/crescita).
- **Nullabile**: Sì
- **Esempio di valori**: "package", "additional"

### details / mainPrices / detailPrices / priceCalculation / monthlySetting / weekendSetting /
### packageItemString / allowPublicHolidays / holidayDates / suggestedPriceDetails / packageName
- **Descrizione**: **stessa struttura e stessa semantica** dei campi omonimi di `price_lists`, già
  documentati in dettaglio in [`price_lists.md`](price_lists.md) — verificato sui dati reali che
  `details.PackageItemSelected` referenzia gli stessi nomi di articolo (`Umbrella`, `Sunbed`, `Cabin`,
  ecc.) e `detailPrices[].edit_price_data` ha la stessa forma `{date, day, price}`. `monthlySetting`/
  `weekendSetting` risultano **costanti** anche qui (`"effective duration of the month"` / `"Consider
  weekend saturday and sunday"` su tutte le 100 righe — nota: qui con la "C" maiuscola, leggera differenza
  di capitalizzazione rispetto alla stringa minuscola di `price_lists`, entrambe comunque hardcoded lato
  frontend). `priceCalculation` osservato: `proportional` (98/100), `daily` (2/100). **Dato reale DB dev**
  (68 righe, query diretta): `priceCalculation` è **sempre e solo** `"proportional"` su tutte le 68 righe
  (nessuna riga `"daily"`) — a differenza di `price_lists.priceCalculation`, che sullo stesso DB dev mostra
  invece 7 valori diversi (vedi [`price_lists.md`](price_lists.md)): qui il campo si conferma
  effettivamente costante in produzione, non solo nel campione locale. Anche `monthlySetting`
  (sempre `"effective duration of the month"`, 68/68) e `weekendSetting` (sempre `"Consider weekend
  saturday and sunday"`, con la "C" maiuscola, 68/68) risultano **costanti anche sul DB dev reale** — a
  differenza degli stessi campi in `price_lists`, dove il DB dev reale ha invece smentito la costanza
  ipotizzata dal campione locale. La differenza di comportamento fra le due tabelle (qui costante, in
  `price_lists` variabile) è quindi confermata e non un artefatto di campionamento.
- A differenza di `price_lists`, qui **non esiste alcun collegamento con `seasons_periods`**: il listino
  hotel non ha una tabella "stagioni" propria — i nomi dei periodi dentro `detailPrices[].name` (es. "Fila
  1 Bassa stagione") sono liberi, senza alcun matching automatico lato server con una tabella di stagioni
  (a differenza del meccanismo `getLowestPrice`/`periods[].name` documentato per `price_lists`).

**Dettaglio per singolo campo (dati reali DB dev, 68 righe)**:

- **details** (`longtext`, nullable): esempio reale
  `{"PackageName":"A7","AdditionalItem":"","PackageItemSelected":[...]}`. Deprecato: No.
- **mainPrices** (`longtext`, nullable): esempio reale
  `[{"name":"Fila 1","value":"7","halfday":"7","seasonal":"9"}]`. Deprecato: No.
- **detailPrices** (`longtext`, nullable): esempio con `edit_price_data` annidato, stessa forma
  `{date, day, price}` già documentata per `price_lists`. Deprecato: No.
- **priceCalculation** (`varchar(100)`, nullable): esempio reale sempre `"proportional"`. **Candidato a
  enumerazione**: pur essendo di fatto costante su questa tabella, condivide dominio con
  `price_lists.priceCalculation` (che ha 7 varianti reali) — se si introduce un ENUM, va dimensionato sul
  sovrainsieme dei valori osservati in entrambe le tabelle, non solo su questa. Deprecato: No.
- **monthlySetting** (`varchar`, nullable): esempio reale sempre `"effective duration of the month"`.
  Deprecato: No.
- **weekendSetting** (`varchar`, nullable): esempio reale sempre `"Consider weekend saturday and sunday"`
  (con "C" maiuscola). Deprecato: No.
- **packageItemString** (`longtext`, nullable): generato lato client da `PACKAGE_INITIALS` (vedi
  [`price_lists.md`](price_lists.md#packageitemstring) per la spiegazione completa del meccanismo) — la
  sigla rappresenta quantità+iniziale di ogni articolo del pacchetto, es. "1 Umbrella + 1 Sunbed". Esempio
  reale: `{"english_initial":"1U+1S","italian_initial":"1O+1L"}`. Deprecato: No.
- **allowPublicHolidays** (`tinyint(1)`, nullable, default `0`): **dato reale DB dev**: sempre `0` su tutte
  le 68 righe (nessun hotel ha mai attivato tariffe festive dedicate). Esempio: 0, 1 (teorico). Deprecato:
  No.
- **holidayDates** (`longtext`, nullable, default `[]`): **dato reale DB dev**: sempre `"[]"` su tutte le 68
  righe — mai popolato per nessun hotel, a conferma indiretta che `allowPublicHolidays` non è mai stato
  attivato. Esempio: `[]`. Deprecato: No.
- **suggestedPriceDetails** (`longtext`, nullable): **dato reale DB dev**: sempre `null` su tutte le 68
  righe — conferma che la feature Revenue Management (che scrive questo campo in `price_lists`, vedi
  [`price_lists.md`](price_lists.md)) non tocca affatto questa tabella. Esempio: `null`. Deprecato: No.
- **packageName** (`varchar`, nullable, default `''`): **dato reale DB dev, nota importante**: **a
  differenza di `price_lists.packageName`** (sempre stringa vuota su tutte le 788 righe, campo morto — vedi
  [`price_lists.md`](price_lists.md)), qui il campo è **realmente popolato** su parte delle righe: 27 righe
  su 68 hanno un nome reale (es. "Secondo voucher package", "Voucher Pacchetto 2 TEST", "Nuovo pacchetto"),
  41 restano vuote. Non è quindi vestigiale in questa tabella — l'ipotesi di rimozione formulata per
  `price_lists.packageName` **non si applica** a `hotel_price_lists.packageName`. Deprecato: No.

### createdBy
- **Tipo**: `int`, opzionale
- **Descrizione**: utente che ha creato il listino — impostato server-side da `req.userId` nel controller
  (`hotelPriceList.controller.js:42`, `priceListDto.createdBy = req.userId`), non dal client.
- **Nullabile**: Sì
- **Esempio di valori**: **dato reale DB dev**: sempre `199` su tutte le 68 righe (un solo utente ha creato
  tutti i listini hotel osservati finora — plausibile per una feature ancora poco usata/testata).

### minBookingDays
- **Tipo**: `int`, `DEFAULT NULL`
- **Valori osservati**: valorizzato su tutte e sole le 75 righe con `isVoucher=1` (mai su `isVoucher=0`,
  crosstab verificato con query diretta)
- **Descrizione**: durata minima di soggiorno (in giorni) richiesta perché il voucher sia applicabile.
  **Forzato a `null` lato service quando `isVoucher` è `false`**, sia in creazione sia in update
  (`hotelPriceListService.js:34,218-220`: `priceListData.isVoucher ? (...) : null`) — quindi anche se un
  client inviasse un valore per un listino non-voucher, verrebbe scartato server-side. Comportamento
  corretto e verificato, a differenza della validazione spesso assente altrove nel dominio.
- **Nullabile**: Sì
- **Esempio di valori** (prod): `NULL` su 78 righe (esattamente i non-voucher); sulle 310 voucher vale
  `3` (226 righe) o `1` (84).

### maxVoucherQuantity
- **Tipo**: `int`, `DEFAULT NULL`
- **Valori osservati**: su 75 righe voucher, solo 7 hanno un valore (le restanti 68 restano `NULL` —
  nessun limite massimo impostato)
- **Descrizione**: numero massimo di utilizzi del voucher per cliente per stagione. Stessa logica di
  azzeramento server-side per i non-voucher di `minBookingDays` (`hotelPriceListService.js:35,221-223`).
  **A differenza del meccanismo di conteggio utilizzi di [`discounts.md`](discounts.md)** (contatore
  `totalNoOfUsage` dentro il JSON, fidato dal client — criticità già documentata), qui il limite è solo
  dichiarato in questa tabella: la sua effettiva verifica/applicazione (conteggio utilizzi realizzati)
  avviene nel dominio transazionale (`hotelPaymentService.checkVoucherLimit`, tabella `hotel_payments`),
  fuori perimetro di questa scheda C2.
- **Nullabile**: Sì
- **Esempio di valori** (prod): `NULL` su 300 righe su 388 (77%); quando presente, **sempre `1000`**
  (88 righe) — il limite non è mai stato configurato con un valore realistico.

### isDeleted
- **Tipo**: `tinyint(1)`, default `0`
- **Valori osservati** (prod, 388 righe): `0` (348), `1` (40) — ~10% soft-cancellati.
- **Descrizione**: soft-delete. Impostato anche **a cascata** quando l'hotel proprietario viene
  cancellato (vedi [`hotels.md`](hotels.md)).
- **Nullabile**: Sì
- **Esempio di valori** (prod): `0`, `1`.

### createdAt / updatedAt
- **Tipo**: `datetime`, popolati da Sequelize — standard.
- **Nullabile**: Sì
- **Esempio di valori** (prod): range `2026-04-24 07:50:15` … `2026-09-07 10:06:45` — nessuna riga
  anteriore ad aprile 2026 (feature nuova).

---

## Endpoint (tutti `POST`, tutti sotto `/api/sb/hotel/pricelist*`)

| Path | Funzione | Note |
|---|---|---|
| `/api/sb/hotel/pricelist` | `createHotelPriceList` | verifica che l'hotel esista **ed appartenga alla property dell'utente** (`hotelService.hotelExists(hotelId, req.userId)`) prima di creare |
| `/api/sb/hotel/pricelist/list` | `getHotelPriceListList` | se `hotelId` è passato, verifica ownership; altrimenti risolve tutti gli hotel della property (o della property indicata, solo per `systemadmin`) e filtra per quelli |
| `/api/sb/hotel/pricelist/details` | `getHotelPriceListDetails` | dopo il fetch, verifica che l'hotel proprietario appartenga alla property dell'utente (403 altrimenti) |
| `/api/sb/hotel/pricelist/update` | `updateHotelPriceList` | stessa verifica di ownership tramite l'hotel collegato prima di scrivere |
| `/api/sb/hotel/pricelist/delete` | `deleteHotelPriceList` | soft-delete, stessa verifica di ownership |

**Pattern di ownership verificato e coerente su tutti gli endpoint**: questa tabella non ha `propertyId`
proprio (solo `hotelId`), quindi ogni operazione risolve prima l'hotel (`hotelService.hotelExists`/
`getHotelById`) e verifica che appartenga alla property dell'utente autenticato — **mai un controllo IDOR
mancante**, a differenza del pattern sistemico documentato per la maggior parte delle altre tabelle di
questo dominio.

---

## Osservazioni verificate nel codice

- **Duplicazione di struttura con `price_lists` non condivisa a livello di codice**: `hotel_price_lists`
  reimplementa integralmente lo stesso schema JSON di `price_lists` (stesso model shape, stessi nomi
  colonna) invece di riusare un tipo/schema comune — comprensibile nel legacy (evoluzione incrementale),
  ma un candidato naturale per un **value object condiviso** (es. `PriceListDefinition`) fra i due domini
  nella riscrittura .NET, invece di due entità parallele con campi identici mantenute manualmente in sync.
- **Nessun collegamento con `seasons_periods`**: segnalato sopra — un possibile gap funzionale (i nomi dei
  periodi in `detailPrices[].name` sono testo libero, non collegati a nessuna tabella stagioni, a
  differenza del meccanismo — per quanto già fragile — usato da `price_lists`/`getLowestPrice`).
- **`front-modern-pre` risulta poter solo leggere questa tabella**: verificato che
  `AgreementsRestApi.ts` chiama solo `sb/hotel/pricelist/list` — nessuna chiamata a
  `sb/hotel/pricelist` (create), `/update` o `/delete` trovata in quel file. La creazione/modifica/
  cancellazione dei listini hotel risulta quindi disponibile solo tramite le azioni Redux di `front-pre`
  (`managerActions.js:415-445`) e il relativo componente `agreements/PriceList/PriceList.jsx` — non è
  stato possibile determinare con certezza in questa scheda se `front-modern-pre` incorpori/rimandi a
  quella UI legacy per l'editing, o se semplicemente non abbia ancora l'editor completo implementato; da
  chiarire con il team prodotto prima della riscrittura, dato che è rilevante per capire quale sia
  l'esperienza utente "di riferimento" da replicare.
- **Route protette solo da `verifyToken`** (stesso rilievo di [`hotels.md`](hotels.md)): `authJwt.isManager`
  commentato su tutte le route di questo controller.
