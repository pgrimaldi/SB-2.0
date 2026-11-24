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
- Righe da `POST /api/catalog/price-lists/packages-list` (`PriceListService.packageList`): il nome è nella lingua
  dell'utente, e ricerca e ordine (naturale, per nome anche senza un ordinamento scelto) lavorano su quello.
- Cestino di riga ed "elimina selezionati": `app-delete-items-popup`, poi
  `POST /api/catalog/price-lists/delete-package-item`
  (`{ idPackages }`, tutto o niente, 204).
- Duplica di riga e "duplica selezionati": `app-duplicate-items-popup`, poi
  `POST /api/catalog/price-lists/duplicate-package-item` (stesso corpo, tutto o niente, 204; i nomi delle copie li sceglie
  il backend: "Ombrellone copia", poi "Ombrellone copia 2", in ogni lingua con il suo suffisso).
- Un pacchetto che è l'ultimo di un articolo aggiuntivo (senza, varrebbe per tutti i pacchetti) non si elimina: 409
  `pricing.price_list_in_use`, con il nome del pacchetto (`priceListName`) e gli articoli aggiuntivi (`additionalItems`) nel
  messaggio del popup. Nei mock: "Ombrellone famiglia e 4 lettini" (con "Sedia regista").
- Un pacchetto che non si può duplicare (IVA non più ammessa, prezzi per un periodo eliminato, dati di una fila o un settore
  eliminato, un'area eliminata): 422 `pricing.duplicate_vat_rate_not_allowed`, `pricing.duplicate_period_not_found`,
  `pricing.duplicate_area_not_found` o `pricing.duplicate_sector_not_found`, con il nome del pacchetto (`priceListName`) nel messaggio del popup. Nei mock: "Tenda e 2
  sdraio" (IVA 20%), "Gazebo e 4 lettini" (area), "Prima fila e 2 lettini" (periodo; salvandolo quel prezzo si toglie e si può duplicare) e "Lettino singolo" (settore; si salva e
  si duplica dopo averlo tolto da "Settori e file").
- Con 404 `pricing.price_list_not_found` (pacchetto già eliminato da un altro utente), su Elimina, su Duplica e su
  Modifica (lettura del pannello e Salva), gli id escono dalla selezione e la tabella si ricarica (`reloadIfGone`, uscita
  `failed` dei popup e del pannello di modifica).
- "Aggiungi pacchetto" apre `app-package-add-item-panel`, la matita `app-package-edit-item-panel`.

## Pannelli dei pacchetti

- Tutti e due in `components/management/settings/catalog/`: `app-side-panel` + `app-tab-group` come i pannelli di
  Servizi, modulo `PackageAddItemRequest`, conferma "Modifiche non salvate".
- Le loro API prendono lo stabilimento dal token.
- Le due tab, "Info Generali" e "Listino", sono le stesse nei due pannelli.

### Aggiungi

- Parte vuoto. "Crea pacchetto" è spento finché manca il nome nella lingua dell'utente o è aperto il riquadro del
  prezzo stagionale ridotto (i valori del riquadro entrano nel pacchetto solo con Aggiungi o Salva del riquadro).
- Manda tutto il modulo a `POST /api/catalog/price-lists/create-package` (`PriceListService.createPackage`, 204).

### Modifica

- Si apre in sola lettura, come i pannelli dei Servizi, con il pacchetto letto da
  `GET /api/catalog/price-lists/get-package` (`PriceListService.selectedPackage`, `PackageSelectedItem` = `idPackage`
  e i campi del modulo), insieme a `POST /api/catalog/sectors/sector-combobox`, `GET /api/catalog/price-lists/get-package-articles` (gli articoli, vedi
  Info Generali) e `GET /api/catalog/tariff-periods` (i periodi della stagione corrente; il loro `key` numerico è l'`idPeriod` delle righe dei prezzi).
- Le lingue senza nome restano vuote; le righe dei prezzi sono una per settore e una per settore e periodo, con i
  prezzi salvati o vuote.
- I prezzi salvati che il pannello non mostra (righe per tutte le file, senza periodo o di un periodo sconosciuto) restano da parte e
  ripartono invariati con "Salva".
- Se la lettura fallisce il pannello si chiude e compare `app-read-error-popup` (il messaggio del `code`, Chiudi e
  Riprova, che riapre il pannello e rilegge; per un 404 solo Chiudi).
- "Modifica" sblocca tutte e due le tab (un solo salvataggio) e segna subito i campi letti già sbagliati (es. il nome che manca
  nella lingua dell'utente); "Annulla" riporta i valori letti, chiedendo prima se ci sono modifiche.
- Chiudendo il pannello la lettura in corso si interrompe. L'ordine di settori e aree non conta come modifica.
- "Salva" (spento, come "Crea pacchetto", finché manca il nome o è aperto il riquadro di un prezzo stagionale
  ridotto) manda tutto il modulo e `idPackage` a `POST /api/catalog/price-lists/update-package`
  (`PriceListService.updatePackage`, 204).
- Controllo di versione: la lettura porta `version` (in `PackageSelectedItem`), che il pannello tiene da parte e
  "Salva" rimanda nell'header `If-Match` (`updatePackage(idPackage, request, version)`; la risposta ha la nuova
  versione nell'`ETag`). Con 409 `resource.version_conflict` (un altro utente ha cambiato il pacchetto dopo la
  lettura) niente va sopra i bottoni: la tabella si ricarica (`reloadIfChanged`), il pannello rilegge pacchetto, settori e
  periodi come all'apertura, in sola lettura e sulla stessa tab, e a lettura riuscita compare `app-version-conflict-popup`;
  l'utente rifà le modifiche. Se la nuova lettura fallisce compare solo il suo errore (pannello chiuso e
  `app-read-error-popup`).
- In sola lettura: modulo `readonly`, caselle e scelte multiple `disabled`, tendine delle quantità secondo lo stato
  dei loro campi; nei prezzi stagionali ridotti spenti bottone, matite e cestini (griglie in sola lettura);
  "Modifica prezzi intermedi" spento.

### Salvataggio (tutti e due i pannelli)

- Spinner e pannello bloccato. Se riesce, il pannello si chiude e la tabella si ricarica.
- Se fallisce, il pannello resta aperto con i dati scritti. Ogni campo del modulo ha il suo `[error]`: un errore
  del backend su un campo va sotto quel campo (`translations` sotto il nome, `reducedSeasonalPrices` sotto i
  prezzi ridotti, `sectorPrices` e `periodPrices` sotto la loro sezione).
- `pricing.calculation_method_not_found` (sotto "Calcolo del prezzo") e `catalog.vat_rate_not_allowed` (sotto IVA, con le
  aliquote ammesse) arrivano senza `errors[]`.
- Un errore su un campo apre la sua tab (`panelTabOf` di `package-panel-tabs.ts`, il primo campo sbagliato), come in Servizi.
- "Annulla" toglie anche gli errori del backend sotto i campi (`clearApiFieldErrors`), nei due pannelli di modifica.
- `inventory.item_not_found`: gli articoli si rileggono e quelli che non ci sono più escono dal pacchetto
  (`withExistingItems` di `package-items.ts`).
- Errori dentro una lista o un oggetto (es. `translations[0].packageName`, `reducedSeasonalPrices[0].endsOn`),
  di rete o del server: sopra i bottoni. In Aggiungi gli errori della lettura degli articoli si vedono in Info Generali, quelli
  dei settori e periodi in Listino, quelli del salvataggio in tutte e due.
- I nomi e il testo dei servizi inclusi fatti di soli spazi si mandano `null`.
- Limiti del backend lasciati fuori dal modulo: 50 articoli e 500 righe di prezzi per periodo (il loro errore compare come
  errore del campo).

## Tab "Info Generali" (`app-package-general-info-tab`)

- Nome con "Altre lingue" come Servizi. Ogni nome al massimo 255 caratteri (`MAX_PACKAGE_NAME_LENGTH` di
  `PackageTranslation`, come il backend): il campo non lascia scrivere oltre.
- Tendine "Numero max di articoli aggiuntivi" e "Numero max di persone": "Nessun limite" (`packages.form.no_limit`, `null`) e
  da 1 a 15 (costanti `MAX_PACKAGE_*_QUANTITY` di `PackageAddItemRequest`); un valore salvato più alto è una scelta in più. Le tendine lavorano con testi, il modulo tiene il
  numero.
- "Articoli che compongono il pacchetto": una riga per ognuno dei 24 articoli del backend (`InventoryArticle`), letti all'apertura
  del pannello con `GET /api/catalog/price-lists/get-package-articles` (`PriceListService.packageArticles`, `PackageArticle`);
  con più elementi dello stesso articolo in magazzino una riga ciascuno, con il nome dopo quello dell'articolo.
  - Tendina "Nessuno", da 1 a 10 e l'eventuale quantità salvata più alta; il modulo tiene `items` (`PackageItemQuantity`:
    `idInventoryItem`, `quantity`), nell'ordine delle righe (`orderedItems` di `package-items.ts`).
  - Un articolo senza elementi in magazzino (nessuno, o quantità 0) ha la riga spenta con "Non in magazzino", e sopra le righe
    "Gli articoli in grigio non hanno elementi in magazzino."; se il pacchetto lo ha già, la riga resta usabile con lo stesso
    testo come errore.
  - Gli articoli del pacchetto senza riga (nomi liberi) restano nel modulo e tornano uguali.
  - Aggiungi: se la lettura fallisce il messaggio va sopra i bottoni e si riprova alla prossima apertura della tab.
- "Servizi inclusi (opzionale)": testo libero (`includedServices`), al massimo 1.000 caratteri
  (`MAX_PACKAGE_INCLUDED_SERVICES_LENGTH`).
- "Aree abilitate": scelta multipla da `POST /api/catalog/sectors/sector-area-combobox`
  (`SectorService.sectorAreaCombobox`, `idSectorAreas`).
- "Settori e file": scelta multipla da `POST /api/catalog/sectors/sector-combobox` (`idSectors`; vuota = "Tutti i settori
  e file").
- Un settore o un'area scelti che non ci sono più restano nella loro scelta come "Settore/fila non più disponibile"
  (`packages.form.missing_sector`) e "Area non più disponibile" (`missing_area`), per toglierli: finché ci sono, Salva e "Crea
  pacchetto" rispondono 404 `catalog.sector_not_found` o `map.spot_not_found` e i due elenchi si rileggono. Nei mock: "Lettino
  singolo" (settore) e "Gazebo e 4 lettini" (area).
- I due elenchi (`sectorList` e `sectorAreaList` dei pannelli, `DropdownList`) si leggono una volta per apertura del pannello: i
  settori sono gli stessi delle righe dei prezzi, letti con una chiamata sola (`sectorRead`). In fondo quattro caselle.
- "Altre lingue" si richiude a ogni apertura del pannello.

## Tab "Listino" (`app-price-list-tab`)

- In `components/management/settings/catalog/price-list-tab/`, usata anche dai pannelli degli articoli
  aggiuntivi: riceve i campi del listino come `PriceListFields`; testi `management.settings.catalog.price_list.*`.
- Settori e periodi: il pannello Aggiungi li legge alla prima apertura della tab (se la lettura fallisce, messaggio
  sopra i bottoni e nuovo tentativo alla prossima apertura); il pannello di modifica alla sua apertura.
- Ordine: "Prezzi stagionali", contenuto del pannello (`<ng-content />`), "Prezzi per periodo", tendine e
  casella, "Festivi", "Modifica prezzi intermedi".

### Griglie dei prezzi

- "Prezzi stagionali" (una riga per settore) e "Prezzi per periodo" (una riga per settore e periodo della stagione
  corrente): sezioni `app-expansion-panel`, chiuse a ogni apertura (`isPanelOpen`); i campi si creano solo a sezione aperta.
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
- Il bottone "Prezzo stagionale ridotto" (spento finché i settori non sono letti) apre un riquadro con Dal, Al (`startsOn`, `endsOn`) e Nome obbligatori
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
- "IVA (%)" (`vatRate`, `price_list.vat`), come in Servizi: parte vuota, vuota vuol dire senza aliquota; deve essere maggiore
  di 0 e minore di 100 (`vatRange`, `invalidate.catalog.vat.range`).
- Le tre tendine qui sotto cominciano con la scelta vuota ("Nessun calcolo", "Nessuna impostazione"), che le riporta a `null`.
- "Impostazione del mensile" (`monthlySetting`, `PriceMonthlySetting`: Durata effettiva del mese, che per il backend sono 31
  giorni; Giorni reali del mese; 30 giorni).
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
- Al massimo 100 giorni (`MAX_PUBLIC_HOLIDAY_QUANTITY`, `maxPublicHolidays`, `invalidate.catalog.public_holidays.max`).
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
  periodo (es. "Fila 1 Bassa stagione", "Altre file Alta stagione"). Con un settore solo le "Altre file" non ci sono
  (`[hasOtherSectors]`).
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
- Righe da `POST /api/catalog/price-lists/additional-services-articles-list` (`PriceListService.additionalItemList`, righe
  `AdditionalItem`): nome nella lingua dell'utente e ordine naturale, come i pacchetti.
- Cestino ed "elimina selezionati": `POST /api/catalog/price-lists/delete-additional-services-article-item`.
- Duplica e "duplica selezionati": `POST /api/catalog/price-lists/duplicate-additional-services-article-item` (i nomi delle
  copie li sceglie il backend: "Lettino copia", poi "Lettino copia 2").
- Corpo `{ idAdditionalItems }`, uno o più id, tutto o niente, 204.
- Un articolo aggiuntivo che non si può duplicare: gli stessi 422 dei pacchetti (IVA, periodo, settore), con il suo nome nel
  messaggio del popup. Nei mock: "Tenda" (IVA 20%), "Posto auto" (periodo) e "Cabina giornaliera" (settore); salvandoli, periodo
  e settore si tolgono e si possono duplicare.
- Con 404 `pricing.price_list_not_found` (elemento già eliminato da un altro utente), su Elimina, su Duplica e su Modifica
  (lettura del pannello e Salva), gli id escono dalla selezione e la tabella si ricarica (`reloadIfGone`, uscita
  `failed` dei popup e del pannello di modifica).

### Pannello Aggiungi (`app-additional-item-add-item-panel`)

- In `components/management/settings/catalog/additional-item-add-item-panel/`: `app-side-panel` senza tab, modulo
  `AdditionalItemAddItemRequest`, conferma "Modifiche non salvate".
- "Seleziona articolo", obbligatoria: articoli del magazzino da `POST /api/catalog/article-combobox`
  (`idArticle`).
- "Pacchetti": scelta multipla da `POST /api/catalog/price-lists/package-combobox` (`idPackages`; vuota = "Tutti i pacchetti").
- Un articolo o un pacchetto scelti che non ci sono più restano nelle loro tendine come "Articolo non più disponibile"
  (`additional_items.form.missing_article`) e "Pacchetto non più disponibile" (`missing_package`), da cambiare o togliere
  (`optionsWithMissing` di `options-with-missing.ts`).
- Caselle "Prenotazioni online", "Solo online" (indipendenti, come nei pacchetti) e, in una cornice, "Imposta come articolo assegnabile su mappa"
  (`isMapAssignable`).
- Poi il titolo "Listino" e `app-price-list-tab`, senza prezzi stagionali ridotti.
- Le due tendine e le righe dei prezzi (`sector-combobox` e `catalog/tariff-periods`) si leggono a ogni apertura del
  pannello.
- "Crea articolo/servizio" è spento finché manca l'articolo o non sono letti settori e periodi (si riprova riaprendo il
  pannello); manda tutto il modulo a
  `POST /api/catalog/price-lists/create-additional-services-article-item` (`PriceListService.createAdditionalItem`, 204).
- Nome e quantità non sono nel pannello: il backend dà il nome dell'articolo e quantità 1, e 100 come quantità massima di ogni
  pacchetto.
- Spinner e pannello bloccato; se riesce il pannello si chiude e la tabella si ricarica, se fallisce gli errori
  vanno sotto i campi o sopra i bottoni. Senza `errors[]`, sotto il loro campo: `inventory.item_not_found` (Seleziona articolo)
  e `pricing.package_not_found` (Pacchetti), che rileggono la loro tendina, `catalog.vat_rate_not_allowed` (IVA) e
  `pricing.calculation_method_not_found` (Calcolo del prezzo).

### Pannello Modifica (`app-additional-item-edit-item-panel`)

- In `components/management/settings/catalog/additional-item-edit-item-panel/`, come il pannello Aggiungi, titolo
  "Modifica articolo/servizio aggiuntivo".
- A ogni apertura legge l'articolo aggiuntivo con
  `GET /api/catalog/price-lists/get-additional-services-article-item?idAdditionalItem=` (`PriceListService.selectedAdditionalItem`,
  risposta `AdditionalItemSelectedItem`), le due tendine, i settori e i periodi; si apre in sola lettura. Se la lettura
  fallisce il pannello si chiude e compare `app-read-error-popup`, come per i pacchetti.
- "Modifica" sblocca i campi e segna subito quelli letti già sbagliati; "Annulla" riporta i valori letti, chiedendo prima se ci
  sono modifiche. Chiusura durante la lettura, 409 e ordine dei pacchetti come nei pacchetti.
- "Salva" manda tutto il modulo con `idAdditionalItem` a
  `POST /api/catalog/price-lists/update-additional-services-article-item` (`PriceListService.updateAdditionalItem`, 204),
  compresi i prezzi letti di settori o periodi che il pannello non mostra; se riesce il pannello si chiude e la
  tabella si ricarica. Errori come in Aggiungi.
- Quello che il pannello non ha lo tiene il backend: nome e traduzioni (cambiano solo se cambia l'articolo), quantità, quantità
  massime dei pacchetti già collegati, persone massime, predefinito, settori abilitati e prezzi ridotti.
- Controllo di versione come per i pacchetti: `version` in `AdditionalItemSelectedItem`, rimandata nell'header
  `If-Match` (`updateAdditionalItem(idAdditionalItem, request, version)`); con 409 `resource.version_conflict`
  `app-version-conflict-popup`, la tabella ricaricata e una nuova lettura dell'articolo aggiuntivo, delle tendine, dei settori e dei
  periodi, come all'apertura.
