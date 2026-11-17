# Listino

Rotta `/settings/catalog` · vista `views/settings/catalog/` (`Catalog`, `app-catalog`), rotte in `catalog.routes.ts`.

## Struttura

- Due tab con `app-tab-nav` dentro il riquadro bianco, larghe metà ciascuna, elencate in `CATALOG_TABS`
  (`catalog-tabs.ts`), testi `management.settings.catalog.tabs.*`:
  - "Pacchetti" (`/settings/catalog/1`, `Packages`);
  - "Articoli/Servizi aggiuntivi" (`/settings/catalog/2`, `AdditionalItems`, `app-additional-items`).
- Ogni tab ha `unsavedChangesGuard` (`canDeactivate` in `catalog.routes.ts`), come le tab di Servizi.
- Mock: `services/mocks/catalog/price-lists.mock.ts`.

## Tab Pacchetti

- Titolo "Pacchetti" (`app-page-title` senza icona) e `app-table` come Settori e file: ricerca, selezione,
  bottone "Aggiungi pacchetto", una colonna Nome (`packageName`), azioni di riga cancella, duplica e modifica.
- Righe da `POST /api/catalog/packages-list` (`PriceListService.packageList`).
- Cestino di riga ed "elimina selezionati": `app-delete-items-popup`, poi `POST /api/catalog/delete-package-item`
  (`{ idPackages }`, tutto o niente, 204).
- Duplica di riga e "duplica selezionati": `app-duplicate-items-popup`, poi
  `POST /api/catalog/duplicate-package-item` (stesso corpo, tutto o niente, 204; i nomi delle copie li sceglie
  il backend).
- "Aggiungi pacchetto" apre `app-package-add-item-panel`, la matita `app-package-edit-item-panel`.

## Pannelli dei pacchetti

- Tutti e due in `components/management/settings/catalog/`: `app-side-panel` + `app-tab-group` come i pannelli di
  Servizi, modulo `PackageAddItemRequest`, conferma "Modifiche non salvate".
- Le loro API prendono lo stabilimento dal token.
- Le due tab, "Info Generali" e "Listino", sono le stesse nei due pannelli.

### Aggiungi

- Parte vuoto. "Crea pacchetto" è spento finché manca il nome nella lingua dell'utente o è aperto il riquadro del
  prezzo stagionale ridotto (i valori del riquadro entrano nel pacchetto solo con Aggiungi o Salva del riquadro).
- Manda tutto il modulo a `POST /api/catalog/packages/create-package` (`PriceListService.createPackage`, 204).

### Modifica

- Si apre in sola lettura, come i pannelli dei Servizi, con il pacchetto letto da
  `GET /api/catalog/packages/{idPackage}` (`PriceListService.selectedPackage`, `PackageSelectedItem` = `idPackage`
  e i campi del modulo), insieme a `POST /api/catalog/sector-combobox` e `GET /api/catalog/tariff-periods` (i periodi
  della stagione corrente; il loro `key` numerico è l'`idPeriod` delle righe dei prezzi).
- Le lingue senza nome restano vuote; le righe dei prezzi sono una per settore e una per settore e periodo, con i
  prezzi salvati o vuote.
- I prezzi salvati di settori o periodi che il pannello non mostra (es. di un'altra stagione) restano da parte e
  ripartono invariati con "Salva".
- Se la lettura fallisce il messaggio compare sopra i bottoni e "Modifica" resta spento.
- "Modifica" sblocca tutte e due le tab (un solo salvataggio); "Annulla" riporta i valori letti, chiedendo prima se
  ci sono modifiche.
- "Salva" (spento, come "Crea pacchetto", finché manca il nome o è aperto il riquadro di un prezzo stagionale
  ridotto) manda tutto il modulo e `idPackage` a `POST /api/catalog/packages/update-package`
  (`PriceListService.updatePackage`, 204).
- In sola lettura: modulo `readonly`, caselle e scelte multiple `disabled`, tendine delle quantità secondo lo stato
  dei loro campi; nei prezzi stagionali ridotti spenti bottone, matite e cestini (griglie in sola lettura);
  "Modifica prezzi intermedi" spento.

### Salvataggio (tutti e due i pannelli)

- Spinner e pannello bloccato. Se riesce, il pannello si chiude e la tabella si ricarica.
- Se fallisce, il pannello resta aperto con i dati scritti. Ogni campo del modulo ha il suo `[error]`: un errore
  del backend su un campo va sotto quel campo (`translations` sotto il nome, `reducedSeasonalPrices` sotto i
  prezzi ridotti, `sectorPrices` e `periodPrices` sotto la loro sezione).
- Errori dentro una lista o un oggetto (es. `translations[0].packageName`, `reducedSeasonalPrices[0].endsOn`),
  di rete o del server: sopra i bottoni.

## Tab "Info Generali" (`app-package-general-info-tab`)

- Nome con "Altre lingue" come Servizi. Ogni nome al massimo 255 caratteri (`MAX_PACKAGE_NAME_LENGTH` di
  `PackageTranslation`, come il backend): il campo non lascia scrivere oltre.
- Tendine "Numero max di articoli aggiuntivi" e "Numero max di persone" da 1 a 15; Ombrellone, Lettino, Sdraio,
  Sedia regista, Cabina e Tenda da 1 a 10 (costanti `MAX_PACKAGE_*_QUANTITY` di `PackageAddItemRequest`).
  Le tendine lavorano con testi, il modulo tiene il numero nei campi `*Quantity`.
- "Servizi inclusi (opzionale)": testo libero (`includedServices`).
- "Aree abilitate": scelta multipla da `POST /api/catalog/map/sector-area-combobox` (`idSectorAreas`).
- "Settori e file": scelta multipla da `POST /api/catalog/sector-combobox` (`idSectors`; vuota = "Tutti i settori
  e file").
- Le due scelte multiple si rileggono a ogni apertura della tab. In fondo quattro caselle.

## Tab "Listino" (`app-price-list-tab`)

- In `components/management/settings/catalog/price-list-tab/`, usata anche dai pannelli degli articoli
  aggiuntivi: riceve i campi del listino come `PriceListFields`; testi `management.settings.catalog.price_list.*`.
- Settori e periodi: il pannello Aggiungi li legge alla prima apertura della tab (se la lettura fallisce, messaggio
  sopra i bottoni e nuovo tentativo alla prossima apertura); il pannello di modifica alla sua apertura.
- Ordine: "Prezzi stagionali", contenuto del pannello (`<ng-content />`), "Prezzi per periodo", tendine e
  casella, "Festivi", "Modifica prezzi intermedi".

### Griglie dei prezzi

- "Prezzi stagionali" (una riga per settore) e "Prezzi per periodo" (una riga per settore e periodo della stagione
  corrente): sezioni `app-expansion-panel`, chiuse all'apertura della tab.
- Le griglie sono larghe quanto la loro sezione, nei colori delle tabelle dell'app: testi blu, righe alternate
  bianche e azzurre (`--color__layout__highlight`) separate da linee sottili (anche quelle dei prezzi ridotti).
- Scorrono da sole con intestazione e prima colonna fisse.
- Limite dei prezzi: 1.000.000 (`MAX_PRICE_LIST_PRICE` di `PackageAddItemRequest`, come il backend). Le regole sono
  in `price-limits.ts` (`maxPrices` sulla lista con le funzioni che leggono i prezzi di una riga), usate dai quattro
  pannelli per `sectorPrices`, `periodPrices`, `intermediatePrices` e, nei pacchetti, `reducedSeasonalPrices`: un
  prezzo oltre il massimo mostra `invalidate.catalog.price.max` sotto la sua tabella e spegne il salvataggio.

### Prezzi stagionali ridotti (solo pacchetti)

- I pannelli dei pacchetti mettono nello `<ng-content />` `app-package-reduced-price`: quanti prezzi ridotti si
  vogliono (`reducedSeasonalPrices`, tipo `PackageReducedSeasonalPrice[]`).
- Il bottone "Prezzo stagionale ridotto" apre un riquadro con Dal, Al (`startsOn`, `endsOn`) e Nome obbligatori
  (Nome al massimo 120 caratteri, `MAX_REDUCED_SEASONAL_PRICE_NAME_LENGTH`, come il backend).
- I periodi non si sovrappongono: nei due calendari i giorni degli altri prezzi ridotti sono spenti
  (`disabledPeriods`), Al si ferma al giorno prima del prezzo ridotto successivo e il modulo del riquadro ha
  `noOverlap` (messaggio `packages.form.reduced_seasonal.overlap` sotto i campi, Aggiungi/Salva spento).
- Aggiungi crea sotto il bottone, prima di "Prezzi per periodo", una sezione `app-expansion-panel` chiusa,
  intitolata "Prezzi stagionali ridotti: nome (Dal – Al)" (`packages.form.reduced_seasonal.section_title`), con la
  stessa griglia di "Prezzi stagionali" (una riga per settore, colonne mezza giornata e stagionale: `sectorPrices`
  di ogni prezzo ridotto), e chiude il riquadro.
- Nell'intestazione di ogni sezione (titolo che va a capo se lungo, `--expansion-panel-header-height: auto`):
  - la matita riapre il riquadro con nome e date della sezione, che sparisce finché il riquadro è aperto; Salva
    cambia solo nome e date, i prezzi restano; la X lascia la sezione com'era;
  - il cestino chiede conferma (`app-confirm-popup` rosso, "Vuoi davvero cancellare questa sezione e tutti i dati
    inseriti?", testi `packages.form.reduced_seasonal.delete`) e poi toglie la sezione.
- Le sezioni sono sempre in ordine di Dal: dopo ogni Aggiungi o Salva del riquadro e alla lettura del pacchetto
  nel pannello di modifica.
- Finché il riquadro è aperto il bottone "Prezzo stagionale ridotto", le matite e i cestini sono spenti.
- Il riquadro e la sezione che sta modificando (`editedIndex`: posizione del prezzo ridotto, fine della lista per
  uno nuovo, null da chiuso) li tiene il pannello: restano cambiando tab e contano per le modifiche non salvate.

### Tendine e casella

- "Calcolo del prezzo" (`calculationMethod`, enum `PriceCalculationMethod`: Prezzo proporzionale, giornaliero,
  logaritmico, giornaliero con weekend).
- "Impostazione del mensile" (`monthlySetting`, `PriceMonthlySetting`: Durata effettiva del mese, 30 giorni).
- "Impostazione del weekend" (`weekendSetting`, `PriceWeekendSetting`: sabato e domenica, solo domenica).
- Testi in `catalog.price_list.calculation_methods`, `catalog.price_list.monthly_settings` e
  `catalog.price_list.weekend_settings`.
- Casella "Prezzo cumulativo". Calcolo del prezzo e Prezzo cumulativo hanno ciascuno la sua (i) con
  `app-info-popup`.

### Festivi (`app-public-holidays`)

- In `components/management/settings/catalog/public-holidays/`, dopo le tendine; testi
  `catalog.price_list.public_holidays`.
- Nell'intestazione, prima del titolo (slot `appExpansionPanelStart`), il checkbox `isPublicHolidaysIncluded`; il
  clic non apre né chiude la sezione. Niente `[formField]` ma `[checked]` / `(checkedChange)`, perché il valore
  cambia solo dopo la conferma: mostra `isChecked`, `linkedSignal` del valore; spento in sola lettura.
- Spuntarlo accende subito l'opzione. Toglierlo apre `app-confirm-popup` rosso (testi `public_holidays.disable`),
  anche con la lista vuota:
  - Disattiva toglie la spunta, chiude e spegne la sezione e svuota `publicHolidayDates`;
  - Annulla, la X o Esc rimettono la spunta e lasciano tutto com'era.
- La sezione si apre solo con il checkbox acceso (`[disabled]` di `app-expansion-panel`: da spento titolo e freccia
  grigi, l'intestazione non apre) e si chiude a ogni cambio del checkbox (`linkedSignal`).
- Dentro, "Aggiungi festivi" apre `app-public-holiday-add-popup` con `app-day-calendar`: un clic sceglie un giorno,
  un altro lo toglie; i giorni già nella lista sono grigi; Annulla e la X chiedono conferma se ci sono giorni
  scelti; Conferma è spento senza giorni.
- Conferma aggiunge i giorni a `publicHolidayDates` (giorni UTC ISO, in ordine, senza doppioni; il pannello di
  modifica li ordina alla lettura).
- La lista si vede come le griglie dei prezzi (colonna Data, righe alternate), con un cestino per riga che toglie
  il giorno subito. Bottone e cestini spenti in sola lettura.

### Modifica prezzi intermedi (`app-intermediate-prices`)

- In `components/management/settings/catalog/intermediate-prices/`, ultima sezione della tab, gestita come Festivi.
- Checkbox `isIntermediatePricesIncluded` nell'intestazione (`[checked]` / `(checkedChange)`, `isChecked`); la
  sezione si apre solo con la spunta e una scelta nella tendina, e si chiude a ogni cambio del checkbox.
- Togliere la spunta apre `app-confirm-popup` rosso (testi `price_list.intermediate_prices.disable`): solo
  Disattiva la toglie e svuota `intermediatePrices`.
- Il checkbox è spento in sola lettura e, nei pannelli dei pacchetti, finché il pacchetto non ha il nome nella
  lingua principale (`!!packageName()`, passato ad `app-price-list-tab` come `[isIntermediatePricesEnabled]`,
  predefinito `true`: gli articoli aggiuntivi, che non hanno un nome, lo usano sempre).
- Nell'intestazione, a sinistra della freccia (slot `appExpansionPanelAction`), una tendina `app-filled-select`
  ("Seleziona"): una scelta "Fila 1" per ogni periodo della stagione corrente, poi una "Altre file" per ogni
  periodo (es. "Fila 1 Bassa stagione", "Altre file Alta stagione").
  - Testi `price_list.intermediate_prices.first_row` / `other_rows` con `{{period}}` (il nome del periodo è un
    dato); le scelte le fa `app-intermediate-prices` dai periodi che riceve (`[periods]`), valore = posizione.
  - Attiva solo con la spunta, vuotata a ogni cambio del checkbox (`linkedSignal`). La scelta non si salva: dice
    solo quali prezzi mostrare.
- Dentro, una griglia come quelle dei prezzi (intestazione fissa, righe alternate, scorre da sola) con 90 righe:
  - Nome/articolo: il nome del pacchetto nella lingua principale (`packageName`) o dell'articolo scelto
    (`articleName`), passato come `[itemName]`;
  - Giorni: da 1 a 90 (`MAX_INTERMEDIATE_PRICE_DAY_QUANTITY`);
  - Prezzo: `app-filled-number-field` con `[value]` / `(valueChange)`, non `[formField]`.
- La lista `intermediatePrices` di `PackageIntermediatePrice` {idPeriod, isFirstRow, dayQuantity, price} tiene solo
  i prezzi scritti, in ordine `byIntermediatePrice`, così aprire una scelta non cambia il modulo; il pannello di
  modifica li ordina alla lettura.
- Il prezzo dei giorni 7, 14 e 30 (`PERIOD_PRICE_FIELDS`) va anche, uguale, nella colonna "Prezzo 7 giorni",
  "Prezzo 14 giorni" o "Prezzo mensile" di "Prezzi per periodo", nelle righe di quel periodo della prima fila
  (Fila 1 = il primo settore, `idFirstSector` di `app-price-list-tab`) o di tutte le altre (Altre file), anche
  quando si cancella. Al contrario no.
- Un errore della lista compare sotto la sezione.
- Sul telefono il titolo va a capo (`--expansion-panel-header-height: auto`), la tendina va su una riga sua sotto
  il titolo, larga fino alla freccia (`--expansion-panel-header-wrap: wrap`, `--expansion-panel-actions-order: 1`,
  `--expansion-panel-actions-basis: calc(100% - 1rem)`); la griglia ha celle e campo più stretti e meno margine ai
  lati, così le tre colonne ci stanno senza spezzare le parole.
- La tab finisce con 1.25rem di spazio, così l'ultima sezione non tocca la fine del pannello.

## Tab Articoli/Servizi aggiuntivi (`AdditionalItems`, `app-additional-items`)

- Copia della tab Pacchetti: titolo "Articoli/servizi aggiuntivi", `app-table` con ricerca, selezione, una colonna
  Nome (`additionalItemName`), azioni di riga cancella, duplica e modifica, bottone "Aggiungi articoli/servizi
  aggiuntivi"; testi `management.settings.catalog.additional_items.*`.
- Righe da `POST /api/catalog/additional-services-articles-list` (`PriceListService.additionalItemList`, righe
  `AdditionalItem`).
- Cestino ed "elimina selezionati": `POST /api/catalog/delete-additional-services-article-item`.
- Duplica e "duplica selezionati": `POST /api/catalog/duplicate-additional-services-article-item`.
- Corpo `{ idAdditionalItems }`, tutto o niente, 204.

### Pannello Aggiungi (`app-additional-item-add-item-panel`)

- In `components/management/settings/catalog/additional-item-add-item-panel/`: `app-side-panel` senza tab, modulo
  `AdditionalItemAddItemRequest`, conferma "Modifiche non salvate".
- "Seleziona articolo", obbligatoria: articoli del magazzino da `POST /api/warehouse/article-combobox`
  (`idArticle`).
- "Pacchetti": scelta multipla da `POST /api/catalog/package-combobox` (`idPackages`; vuota = "Tutti i pacchetti").
- Caselle "Prenotazioni online", "Solo online" e, in una cornice, "Imposta come articolo assegnabile su mappa"
  (`isMapAssignable`).
- Poi il titolo "Listino" e `app-price-list-tab`, senza prezzi stagionali ridotti.
- Le due tendine e le righe dei prezzi (`sector-combobox` e `catalog/tariff-periods`) si leggono a ogni apertura del
  pannello.
- "Crea articolo/servizio" è spento finché manca l'articolo; manda tutto il modulo a
  `POST /api/catalog/create-additional-services-article-item` (`PriceListService.createAdditionalItem`, 204).
- Spinner e pannello bloccato; se riesce il pannello si chiude e la tabella si ricarica, se fallisce gli errori
  vanno sotto i campi o sopra i bottoni.

### Pannello Modifica (`app-additional-item-edit-item-panel`)

- In `components/management/settings/catalog/additional-item-edit-item-panel/`, come il pannello Aggiungi, titolo
  "Modifica articolo/servizio aggiuntivo".
- A ogni apertura legge l'articolo aggiuntivo con
  `GET /api/catalog/get-additional-services-article-item?idAdditionalItem=` (`PriceListService.selectedAdditionalItem`,
  risposta `AdditionalItemSelectedItem`), le due tendine, i settori e i periodi; si apre in sola lettura.
- "Modifica" sblocca i campi; "Annulla" riporta i valori letti, chiedendo prima se ci sono modifiche.
- "Salva" manda tutto il modulo con `idAdditionalItem` a
  `POST /api/catalog/update-additional-services-article-item` (`PriceListService.updateAdditionalItem`, 204),
  compresi i prezzi letti di settori o periodi che il pannello non mostra; se riesce il pannello si chiude e la
  tabella si ricarica.
