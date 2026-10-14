# Tabella `price_lists`

> Fonti: schema e dati reali (100 righe), model
> [`back-pre/app/models/pricelists.model.js`](../../back-pre/app/models/pricelists.model.js), controller
> [`pricelist.controller.js`](../../back-pre/app/controllers/pricelist.controller.js) (letto per intero,
> 380 righe), controller collegato
> [`revenue.controller.js`](../../back-pre/app/controllers/revenue.controller.js) (feature "Revenue
> Management" che legge/scrive questa stessa tabella con query SQL dirette, non tramite il model
> Sequelize), frontend `front-pre/src/components/Price/*` e la sua **copia praticamente identica** in
> `front-customer-pre/src/components/Price/*` (`PriceParent.js`, `AddPackage.js`,
> `AddAdditionalItem.js`, `SetDefaultPkgModal(Online).js` — stesso editor gestionale duplicato in entrambi
> i frontend, non solo lato pubblico), `front-pre/src/services/PriceCalcService.js`,
> `front-pre/src/components/revenue/Revenue.js`.
>
> **Nota di contesto**: a differenza di `sectors_rows`/`seasons_periods`/`configurations` (1 riga per
> `propertyId`), qui **ogni riga è un singolo pacchetto o articolo aggiuntivo** — uno stabilimento ha
> normalmente molte righe (nel campione: `type` distinto in `package` (61/100) e `additional` (39/100)).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Relazioni**: referenziata applicativamente (non FK) da `revenue.controller.js` (`priceListId`) e da
  `getLowestPrice` in `property.controller.js` (già documentato in [`properties.md`](properties.md) e
  [`seasons_periods.md`](seasons_periods.md)).
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### type
- **Tipo**: `varchar`
- **Vincoli**: nessun `ENUM` a livello DB, nessuna `CHECK` — validato solo lato frontend
- **Valori osservati**: `package` (61/100), `additional` (39/100) su campione locale; **dato reale DB dev**
  (788 righe, query diretta): `package` (518), `additional` (270) — proporzione coerente col campione
  locale (circa 66%/34%), nessuna sorpresa.
- **Descrizione**: distingue le due modalità d'uso della stessa tabella:
  - **`package`**: un pacchetto vendibile (es. "1 ombrellone + 2 lettini"), con prezzo calcolato tramite
    `mainPrices`/`detailPrices` (vedi sotto). Gestito da `AddPackage.js`/`PriceListContent.js`.
  - **`additional`**: un articolo/servizio extra acquistabile singolarmente (es. noleggio SUP a ore),
    gestito da `AddAdditionalItem.js`, con un prezzo semplice dentro `details` (non usa `mainPrices`/
    `detailPrices`). **Candidato a enumerazione**: solo 2 valori, stabili su entrambi i campioni (locale e
    DB dev) — ENUM a livello DB sufficiente, nessun bisogno di traduzione o di crescita del set.
- **Nullabile**: Sì
- **Esempio di valori**: "package", "additional"

### details
- **Tipo**: dichiarato `Sequelize.JSON`; colonna fisica `longtext` con `CHECK json_valid(details)`
- **Descrizione**: struttura **diversa a seconda di `type`**, verificata sui dati reali:
  - Quando `type = "package"`:
    - **PackageName** / **AdditionalItem**: nome del pacchetto (bilingue, IT/EN)
    - **PackageItemSelected**: array `{ it, name, quantity }` — gli articoli del magazzino
      (`warehouses.items`, vedi [`warehouses.md`](warehouses.md)) inclusi nel pacchetto con relativa
      quantità
    - **MaxPeopleCount**: numero massimo di persone coperte dal pacchetto
    - **IncludedServices**: servizi inclusi (riferimento a `services`/`bathouses.services`)
    - **enableOnlineReservation**: `boolean` — se il pacchetto è prenotabile online dal sito pubblico.
      Consumato da `property.controller.js:getLowestPrice` (già documentato in
      [`seasons_periods.md`](seasons_periods.md)): solo i pacchetti con questo flag a `true` concorrono al
      calcolo del prezzo minimo mostrato in ricerca.
    - **sectorRowsPkg** / **selectedRowsData**: riferimento ai settori (`sectors_rows.sectors[].name`, vedi
      [`sectors_rows.md`](sectors_rows.md)) a cui il pacchetto è applicabile — di nuovo per **nome
      stringa**, non per id, stesso accoppiamento debole già segnalato per `periods[].name`.
    - **defaultPackage**: `boolean`/stringa `"true"`/`"false"` — marca il pacchetto "predefinito" dello
      stabilimento. Letto con `JSON_UNQUOTE(JSON_EXTRACT(details,'$.defaultPackage'))='true'` sia da
      `getLowestPrice` sia dall'intera feature Revenue Management (`revenue.controller.js`, vedi sotto) per
      individuare **quale pacchetto usare come base per i calcoli di prezzo suggerito**.
    - **defaultPackageOnline**: analogo a `defaultPackage` ma per il contesto online — significato esatto
      non distinguibile con certezza dal solo codice letto in questa scheda (nessun controller lo legge
      esplicitamente nei file esaminati); da chiarire prima della riscrittura.
    - **setReducedPrice**, **selectedAreaItems**, **areaItemsFlag**, **isCumulativeFlag**: **correzione
      rispetto alla versione precedente di questa scheda** — non sono affatto vestigiali/di effetto
      ignoto: sono usati realmente lato **frontend**, solo non elaborati da alcun controller backend
      (nessuna logica server-side li consuma, ma questo non significa che siano inutilizzati):
      - **areaItemsFlag**: attivato da una checkbox "area personalizzata/di settore" nel form pacchetto
        (`AddPackage.js`, presente identico in `front-pre` e `front-customer-pre`, handler
        `_selectAreaItem` righe 276-283); se vero, il pacchetto viene riconosciuto come "Area Package" in
        fase di prenotazione.
      - **selectedAreaItems**: gli articoli scelti per quell'area; la sua **lunghezza** è usata come
        segnale per riconoscere un Area Package (`LocationItemListChildren.js` righe 3223-3238, commento
        esplicito nel codice sorgente).
      - **setReducedPrice**: array di fasce data con prezzi ridotti
        (`{startDate, endDate, additionalData: [{name, value}]}`), consumato da `getReducedPrices` in
        `front-customer-pre/src/services/BookingCalcService.js` (righe 11-32): se la data prenotata rientra
        in una fascia, il calcolo usa quel prezzo ridotto al posto dello standard.
      - **isCumulativeFlag**: salvato insieme al pacchetto (`PriceParent.js` riga 334) ma non risulta
        collegato agli altri 3 campi nella stessa area UI — effetto specifico non determinato con certezza
        in questa scheda.
      - Tutti e 4 vanno quindi considerati **Deprecato: No** — sono usati realmente lato client, solo non
        elaborati/validati lato backend (il backend li accetta e restituisce senza rielaborarli, come
        `packageItemString`).
  - Quando `type = "additional"`: struttura più semplice — `{ name, it, enableOnlineReservation,
    enableOnlineOnly, packageSelection, selectedPackages: [] }`. `packageSelection`/`selectedPackages`
    permettono di **vincolare un articolo aggiuntivo a uno o più pacchetti** (venduto solo se il cliente ha
    già scelto uno dei pacchetti indicati).
- **Nullabile**: Sì
- **Esempio di valori**: per `type="package"`:
  `{"PackageName":"Pacchetto Family","AdditionalItem":"","PackageItemSelected":[{"it":12,"name":"Umbrella",
  "quantity":1},{"it":15,"name":"Sunbed","quantity":2}],"MaxPeopleCount":4,"enableOnlineReservation":true,
  "defaultPackage":"true","defaultPackageOnline":"false","areaItemsFlag":false,"selectedAreaItems":[],
  "isCumulativeFlag":false,"setReducedPrice":[]}`; per `type="additional"`:
  `{"name":"Noleggio SUP","it":"Noleggio SUP","enableOnlineReservation":true,"enableOnlineOnly":false,
  "packageSelection":false,"selectedPackages":[]}`

### mainPrices
- **Tipo**: `Sequelize.JSON` (solo per `type = "package"`)
- **Descrizione**: array `[{ name, value }]` — un prezzo base per settore (`name` = nome del settore da
  `sectors_rows`, `value` = prezzo stagionale forfettario). Usato come fallback/prezzo di riferimento
  rispetto ai prezzi più granulari di `detailPrices`.
- **Nullabile**: Sì
- **Esempio di valori**: `[{"name":"Fila 1","value":"120"},{"name":"Fila 2","value":"100"}]`

### detailPrices
- **Tipo**: `Sequelize.JSON` (solo per `type = "package"`)
- **Descrizione**: array di oggetti con il dettaglio tariffario per periodo, verificato sui dati reali:
  `{ id, name, half_day_price, half_day_weekend_price, daily_price, weekend_price, days_7_price,
  days_14_price, monthly_price, edit_price_data: [{ date, day, price }] }`.
  - **name** è il nome del periodo tariffario (es. `"Alta stagione"`) — matchato per substring (`LIKE`)
    contro `seasons_periods.periods[].name`, come già documentato in
    [`seasons_periods.md`](seasons_periods.md#osservazioni-verificate-nel-codice-criticità).
  - **edit_price_data**: override di prezzo per singole date specifiche all'interno del periodo (deroghe
    puntuali, es. Ferragosto), verificato come struttura presente ma il consumo lato backend non è stato
    tracciato in questa scheda (nessun controller esaminato la legge; presumibilmente calcolata solo lato
    frontend al momento della prenotazione).
  - `getLowestPrice` calcola il minimo tra `daily_price` e `half_day_price` su tutte le voci il cui `name`
    matcha il periodo corrente, solo se `details.enableOnlineReservation = true` e `type = 'package'`
    (query SQL con `JSON_TABLE`, vedi `property.controller.js:1064-1110`).
- **Nullabile**: Sì
- **Esempio di valori**: `[{"id":1,"name":"Alta stagione","half_day_price":"25","half_day_weekend_price":
  "30","daily_price":"45","weekend_price":"50","days_7_price":"280","days_14_price":"520",
  "monthly_price":"1100","edit_price_data":[{"date":"15/08/2024","day":"Thursday","price":"60"}]}]`

### priceCalculation
- **Tipo**: `varchar`
- **Valori osservati (campione locale, 100 righe)**: `"proportional"` (77/100), `"Daily price with
  weekend"` (16/100), `"daily"` (7/100)
- **Correzione importante (dato reale DB dev, 788 righe, query diretta)**: i valori reali in produzione
  sono **più numerosi e diversi** da quanto suggeriva il campione locale: `"proportional"` (629),
  `"daily"` (72), `""` (59, stringa vuota), `"Daily price with weekend"` (13), `"logarithmic"` (11 — **non
  menzionato affatto nella versione precedente di questa scheda**), `"proportional price"` (3), `"Daily
  price"` (1). Diverse di queste sono verosimilmente **varianti/refusi dello stesso concetto** scritte in
  momenti diversi dello sviluppo, non valori semanticamente distinti: `"Daily price"` vs. `"Daily price
  with weekend"`, e `"proportional"` vs. `"proportional price"`. `"logarithmic"` è invece un metodo di
  calcolo genuinamente diverso, non ancora documentato in questa scheda — andrebbe tracciato nel codice
  frontend di calcolo prezzo (stesso modulo che gestisce `"proportional"`/`"daily"`) prima della
  riscrittura, per capire se è tuttora raggiungibile da UI o solo un residuo storico.
- **Descrizione**: metodo di calcolo del prezzo di una prenotazione multi-giorno rispetto ai valori in
  `detailPrices` — non è stato tracciato in questa scheda l'algoritmo esatto lato frontend per ciascuna
  modalità (richiederebbe un'analisi dedicata del modulo di calcolo prenotazione, fuori perimetro di questa
  tabella). **Candidato a enumerazione**: fortemente candidato — insieme di valori chiuso e ricorrente, ma
  "sporco" (refusi/varianti e stringa vuota) da ripulire in fase di normalizzazione: consigliata una
  piccola tabella di lookup con FK (non un `ENUM` diretto) proprio per poter mappare esplicitamente le
  varianti storiche (`"Daily price"` → `"Daily price with weekend"`, `"proportional price"` →
  `"proportional"`) a un identificativo canonico in fase di migrazione dati, cosa che un `ENUM` non
  permetterebbe di documentare.
- **Nullabile**: Sì
- **Esempio di valori**: "proportional", "daily", "logarithmic", "Daily price with weekend"

### monthlySetting / weekendSetting
- **Tipo**: `varchar`
- **Valori osservati (campione locale, 100 righe)**: costanti, sempre rispettivamente `"effective duration
  of the month"` (100/100) e `"consider weekend saturday and sunday"` (100/100) in tutto il campione —
  ipotizzati nella versione precedente di questa scheda come vestigiali/placeholder di un'opzione mai
  realmente esposta all'utente finale.
- **Correzione importante (dato reale DB dev, 788 righe, query diretta)**: l'ipotesi di costanza è
  **smentita** dal DB dev reale, che mostra invece campi realmente variabili in produzione:
  - `monthlySetting`: `"effective duration of the month"` (695), `""` (59, stringa vuota), `"monthly"`
    (30), `"actual duration of the months"` (3), `"other"` (1) — **5 valori distinti**, non 1.
  - `weekendSetting`: `"consider weekend saturday and sunday"` (682), `""` (59, stringa vuota), `"consider
    weekend only Sunday"` (43), `"consider saturday and sunday as a weekend"` (4) — **4 valori distinti**,
    non 1; le ultime due varianti sono probabilmente lo stesso concetto scritto in due modi diversi
    (entrambe "sabato e domenica sono weekend"), da consolidare in fase di normalizzazione.
  - Questi campi **non sono quindi vestigiali/costanti** come ipotizzato dalla scheda originale (basata su
    un campione locale limitato): vanno mantenuti come opzioni realmente configurabili nella riscrittura,
    non ridotti a costanti di sistema. Nota anche per confronto: in `hotel_price_lists` (vedi
    [`hotel_price_lists.md`](hotel_price_lists.md)) questi stessi campi risultano invece **davvero
    costanti** sul DB dev (68/68 righe) — la differenza fra le due tabelle è quindi reale, non un
    artefatto di campionamento.
- **Candidato a enumerazione**: sì per entrambi — insieme di valori piccolo e ricorrente ma con refusi/
  varianti da consolidare; consigliata una tabella di lookup con FK (non `ENUM` diretto) per poter
  documentare esplicitamente il mapping delle varianti storiche verso un valore canonico, oltre a gestire
  la stringa vuota come "non impostato" invece che come valore letterale.
- **Nullabile**: Sì
- **Esempio di valori**: `monthlySetting`: "effective duration of the month", "monthly", "actual duration
  of the months"; `weekendSetting`: "consider weekend saturday and sunday", "consider weekend only Sunday"

### createdBy / propertyId
- **Tipo**: `int`
- **Vincoli**: nessuna FK
- **Descrizione**: come per le altre tabelle di questo dominio — `createdBy` dichiarato dal client,
  `propertyId` è la chiave di lookup usata da tutte le funzioni del controller senza verifica di ownership
  (vedi criticità sotto).
- **Nullabile**: Sì
- **Esempio di valori**: `createdBy`: 210; `propertyId`: 4460

### isDeleted
- **Tipo**: `boolean`, default `false`
- **Valori osservati (campione locale)**: `0` (78/100), `1` (22/100); **dato reale DB dev** (788 righe):
  `1` (468), `0` (320) — proporzione invertita rispetto al campione locale: sul DB dev **la maggioranza
  delle righe è soft-cancellata** (~59%), non una minoranza come suggeriva il campione locale (~22%).
- **Descrizione**: soft-delete. `deletepricelist` (`pricelist.controller.js:298-335`) non cancella la riga
  ma imposta `isDeleted` — coerente con `allpricelist` che filtra sempre `isDeleted: false`.
- **Nullabile**: Sì
- **Esempio di valori**: 0, 1

### packageItemString
- **Tipo**: `Sequelize.JSON`
- **Descrizione**: etichetta compatta bilingue generata **lato client** (non dal backend) da
  `PriceCalcService.js:checkPackageString` (righe 792-811) a partire da `PackageItemSelected` — concatena
  `quantità+iniziali` di ogni articolo incluso nel pacchetto (es. `"2SDB+1OMB"`) usando una tabella di
  sigle predefinite (`PACKAGE_INITIALS`) con fallback a iniziali auto-generate dal nome per articoli
  custom. Salvata così com'è nel DB (il backend la accetta e la restituisce senza rielaborarla), usata per
  mostrare un'etichetta breve del pacchetto in punti dell'interfaccia dove lo spazio è limitato (es. celle
  di calendario/planning). **Meccanismo di generazione confermato nel dettaglio**: prodotto da
  `PriceCalcService.checkPackageString` (`front-pre/src/services/PriceCalcService.js` righe 792-811)
  usando la costante `PACKAGE_INITIALS` definita in
  `front-pre/src/components/Common/PriceListConstant.js` (righe 60-68) — un array di oggetti
  `{name, eng_initial, ita_initial}` con sigle fisse per gli articoli noti (es. Umbrella→U/O, Sunbed→S/L,
  SpecialCabin→SC/CS). Per ogni articolo con `quantity > 0` nel pacchetto, la funzione concatena
  quantità+sigla (es. `"2S+1U"` = 2 lettini + 1 ombrellone). Per articoli custom non presenti nella
  tabella, il fallback usa le iniziali maiuscole delle parole del nome (funzione `getInitials`), **usate
  identiche in entrambe le lingue** (nessuna vera traduzione per gli articoli custom).
- **Nullabile**: Sì (250 righe `null` su 788 nel DB dev)
- **Esempio di valori**: `{"english_initial":"2U+2C","italian_initial":"2O+2C"}`,
  `{"english_initial":"1U","italian_initial":"1O"}`

### allowPublicHolidays
- **Tipo**: `boolean`, default `false`
- **Valori osservati (campione locale)**: `0` (87/100), `1` (13/100); **dato reale DB dev** (788 righe):
  `0` (747), `1` (41) — proporzione coerente col campione locale (~5% vs ~13%, stesso ordine di grandezza,
  minoranza in entrambi i casi).
- **Descrizione**: se il pacchetto ha una tariffa dedicata per i giorni festivi, definita in `holidayDates`.
- **Nullabile**: Sì
- **Esempio di valori**: 0, 1

### holidayDates
- **Tipo**: `Sequelize.JSON`, default `[]`
- **Descrizione**: elenco delle date festive con relativa tariffa (rilevante solo se
  `allowPublicHolidays = true`). **Correzione rispetto alla versione precedente di questa scheda**: la
  struttura era stata dichiarata "non ispezionata per mancanza di campioni non vuoti" — questo era
  **falso/incompleto**: sul DB dev esistono campioni reali non vuoti, ed è semplicemente un **array di
  stringhe data** in formato `"DD/MM/YYYY"`, ad es. `["12/04/2023","20/04/2023","22/04/2023",
  "22/05/2023"]`, **senza alcun prezzo associato** (a differenza di quanto ipotizzato/lasciato intendere in
  precedenza per analogia con `edit_price_data`) — la tariffa festiva è presumibilmente applicata altrove
  nel calcolo (non dentro questo campo). **Raccomandazione per la riscrittura**: essendo una lista piatta
  di date legate a un singolo `price_lists.id`, sarebbe meglio normalizzata in una tabella figlia
  `price_list_holiday_date(price_list_id, holiday_date)` invece che in un campo JSON — elimina il parsing
  applicativo, permette query dirette (es. "quanti pacchetti hanno una tariffa festiva il 15 agosto") e
  l'indicizzazione della data.
- **Nullabile**: Sì (default `[]`)
- **Esempio di valori**: `[]`, `["12/04/2023","20/04/2023","22/05/2023"]`

### suggestedPriceDetails
- **Tipo**: `Sequelize.JSON`, `NOT NULL` nel model ma di fatto quasi sempre vuoto/assente nei dati locali
  (feature recente — vedi sotto)
- **Descrizione**: **non gestito dal CRUD standard di `pricelist.controller.js`** (assente da create/
  update/list "classici") — è l'output della feature **Revenue Management**
  (`revenue.controller.js`, letta per intero nelle parti attive): un algoritmo di "prezzo suggerito"
  giornaliero che combina, per ciascun pacchetto **marcato `defaultPackage: true`** in `details`:
  - previsioni meteo (`RevenueService.getRainByDate`, lat/lon presi da `bathouses.latitude/longitude`),
  - stagionalità (`RevenueService.getSeason`),
  - se il giorno successivo è un weekend (`RevenueService.isNextDayWeekend`),
  - disponibilità/occupazione dello stabilimento in quella data (`RevenueService.calculateAvailability`),

  per produrre uno sconto/prezzo suggerito rispetto al prezzo listino (`daily_price`/`weekend_price` di
  `detailPrices`). Il risultato è **scritto di nuovo nella stessa colonna** con una query SQL diretta
  (`UPDATE price_lists SET suggestedPriceDetails = ...`, bypassando il model Sequelize), organizzato per
  anno: `{ "<anno>": [{ date, packages: [{ id, newUniqueId, name, daily_price, weekend_price,
  suggestedPrice, isSuggestedPriceApplied, isOriginalPriceApplied, discountPercentage,
  isDiscountApplied }] }] }`. Il gestore visualizza questi dati nella pagina "Revenue"
  (`front-pre/src/components/revenue/Revenue.js`) e può "applicare" un prezzo suggerito tramite
  `applySuggestedPrice`, che marca `isSuggestedPriceApplied`/`isOriginalPriceApplied` per singolo
  pacchetto/data. È inoltre letto anche nel flusso di creazione prenotazione lato gestionale
  (`front-pre/src/components/reserveArticle/ReserveArticle.js:1195`), a conferma che il prezzo suggerito
  applicato influenza il prezzo effettivamente proposto in fase di prenotazione, non solo la pagina Revenue
  — il punto esatto in cui il prezzo "applicato" sovrascrive il prezzo standard nel calcolo finale della
  prenotazione non è stato tracciato riga per riga in questa scheda (richiederebbe l'analisi dedicata del
  modulo booking).
- **Nullabile**: Sì — **dato reale DB dev** (788 righe): `null` su 775 righe, valorizzato solo su 13 (feature
  usata da pochissime proprietà, coerente con quanto già ipotizzato).
- **Vedi anche**: [`revenue_notifications`](revenue_notifications.md) — la **configurazione per
  stabilimento** della stessa feature Revenue Management (interruttori, banner sconto sulla pagina
  pubblica, contatori giornalieri); collocata anch'essa in **C2** sottoarea `pricing`.
- **Esempio di valori (struttura confermata dal DB dev, nessuna correzione necessaria rispetto al testo
  sopra)**: `{"2024":[{"date":"15/08/2024","packages":[{"id":123,"newUniqueId":"abc123","name":
  "Pacchetto Family","daily_price":"45","weekend_price":"50","suggestedPrice":"55",
  "isSuggestedPriceApplied":true,"isOriginalPriceApplied":false,"discountPercentage":0,
  "isDiscountApplied":false}]}]}`
- **Deprecato**: No

### packageName
- **Tipo**: `varchar`, default `""`
- **Valori osservati (campione locale)**: sempre stringa vuota (100/100 nel campione); **confermato dal
  DB dev reale**: sempre stringa vuota su **tutte le 788 righe**, nessuna eccezione — la costanza non è un
  artefatto del campione locale, è un dato di produzione confermato.
- **Descrizione**: campo dichiarato nel model ma **mai valorizzato nei dati osservati**; possibile
  duplicato mai utilizzato di `details.PackageName` (che è dove il nome del pacchetto è effettivamente
  salvato) — da verificare se rimuovibile nella riscrittura. **Nota di confronto**: a differenza di questo
  campo, `hotel_price_lists.packageName` (vedi [`hotel_price_lists.md`](hotel_price_lists.md)) **è
  realmente popolato** su parte delle sue righe — non generalizzare questa conclusione di "campo morto"
  all'omonimo campo dell'altra tabella.
- **Nullabile**: Sì
- **Esempio di valori**: `""` (unico valore osservato su tutte le 788 righe)
- **Deprecato**: Sì — campo mai valorizzato in produzione (0/788 righe con un valore non vuoto), probabile
  duplicato morto di `details.PackageName`
- **Produzione**: **nessun valore in produzione** — tutti i record hanno `NULL` o stringa vuota `''`
  (colonna inclusa nell'elenco delle colonne vuote in produzione, verifica 2026-09-07).

### createdAt / updatedAt
- **Tipo**: `datetime`, nessun default DB — standard.
- **Nullabile**: Sì
- **Esempio di valori**: `2023-11-02 14:37:09`

---

## Osservazioni verificate nel codice (criticità)

- **IDOR verificato**: tutte le funzioni di `pricelist.controller.js` (`addpricelist`, `updatepricelist`,
  `deletepricelist`, `pricelistdetail`) operano su `req.body.propertyId`/`req.body.id` senza alcuna verifica
  di ownership contro l'utente autenticato — stesso pattern sistemico del dominio C2.
- **`revenue.controller.js` bypassa completamente l'ORM**: legge e scrive `price_lists` con query SQL
  dirette (`sequelize.query` + `QueryTypes.SELECT/UPDATE`) invece di usare il model `Pricelists` — un
  secondo punto di scrittura sulla stessa tabella, con la propria logica di validazione (o assenza di
  essa), che nella riscrittura andrà consolidato in un unico livello di accesso dati per evitare
  divergenze tra i due percorsi.
- **Codice morto significativo**: `revenue.controller.js` contiene **due funzioni commentate** quasi
  identiche alle rispettive versioni attive (`calculateSuggestedPrice` righe 15-198,
  `applySuggestedPrice` righe 516-609) — versioni precedenti dell'algoritmo lasciate nel file. Come già
  visto per `fiskalyAutoreceipt`/`createAutoReceipt` in [`properties.md`](properties.md), vanno ignorate
  come fonte e rimosse nella riscrittura per non generare confusione.
- **Cache Redis non invalidata da `revenue.controller.js`**: `addpricelist`/`updatepricelist`/
  `deletepricelist` invalidano esplicitamente la cache di gruppo (`clearGroupCache`), ma gli `UPDATE`
  diretti di `suggestedPriceDetails` in `revenue.controller.js` **non lo fanno** — un aggiornamento del
  prezzo suggerito può quindi non riflettersi in `allpricelist`/`pricelistdetail` finché la cache non scade
  naturalmente, un'inconsistenza da correggere nella riscrittura.
- **Editor gestionale duplicato in due frontend**: `front-pre/src/components/Price/*` e
  `front-customer-pre/src/components/Price/*` contengono componenti praticamente identici per nome e
  struttura (`PriceParent.js`, `AddPackage.js`, `AddAdditionalItem.js`, `SetDefaultPkgModal(Online).js`) —
  ulteriore conferma, oltre ai casi già documentati in `properties.md`/`roles.md`, che le due codebase
  gestionali sono **copie divergenti** piuttosto che un frontend pubblico puro + uno gestionale puro.
- **Accoppiamento per stringa libera fra tabelle collegate**: sia `detailPrices[].name` (periodo) sia
  `sectorRowsPkg`/`selectedRowsData` (settori) referenziano altre tabelle **per nome testuale**, non per
  id — stesso pattern fragile già segnalato in [`seasons_periods.md`](seasons_periods.md), da sostituire
  con FK/id nella riscrittura.
- **Campi presenti ma senza consumer backend individuato — aggiornamento**: `setReducedPrice`,
  `selectedAreaItems`, `areaItemsFlag`, `isCumulativeFlag` **non sono vestigiali**: sono realmente
  consumati lato frontend (vedi dettaglio nella sezione `details` sopra — `AddPackage.js:_selectAreaItem`
  righe 276-283, `LocationItemListChildren.js` righe 3223-3238, `BookingCalcService.js:getReducedPrices`
  righe 11-32, `PriceParent.js` riga 334); semplicemente non hanno un consumer **backend**, il che è
  normale per campi calcolati/usati solo in fase di editing e prenotazione lato client. Restano da
  chiarire con il team prodotto solo `defaultPackageOnline` (nessun consumer individuato né frontend né
  backend in questa scheda) e l'effetto specifico di `isCumulativeFlag`. `packageName` (colonna di primo
  livello, non il sotto-campo `details.PackageName`) è invece un caso diverso e **confermato vestigiale**:
  sempre stringa vuota su tutte le 788 righe del DB dev, marcato **Deprecato** nella sezione dedicata sopra.
