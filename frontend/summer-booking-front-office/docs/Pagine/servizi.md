# Servizi

Rotta `/settings/services` · vista `views/settings/services/` (`Services`, `app-services`), rotte in
`services.routes.ts`.

## Struttura

- Tab in alto come Informazioni stabilimento: `app-tab-nav` nel riquadro bianco, una route figlia per tab, caricate
  con la pagina; nomi, percorsi, larghezze e componenti in `services-tabs.ts`, chiavi
  `management.settings.services.tabs.*`:
  - "Servizi Bar" (`/settings/services/1`, `ServicesBar`, cartella `services-bar`);
  - "Servizi" (`/2`, `ServicesGeneral`, "servizi generici", cartella `services-general`);
  - "Servizi Orari" (`/3`, `ServicesHourly`, cartella `services-hourly`);
  - "Prezzi Automatici" (`/4`, `AutomaticPrices`, cartella `automatic-prices`);
  - "Prenotazione tavoli" (`/5`, `TableBooking`, cartella `table-booking`).
- Ogni cartella ha il nome di quello che mostra, mai un numero.
- Tab tutte larghe uguali, 20%: con i 966px del riquadro ogni etichetta ha 145px, per questo i nomi lunghi sono
  accorciati invece di rimpicciolire il font (en "Table Booking", fr "Réservation de table").
  - Da circa 1100px di finestra in su stanno tutte in una riga in ogni lingua; più stretto le etichette più lunghe
    allargano un po' la loro tab e in francese e tedesco Material mostra le frecce; sotto i 768px c'è la tendina.
- Ogni tab ha `unsavedChangesGuard` (`canDeactivate` in `services.routes.ts`), dove stanno i pannelli; non sulla
  pagina, altrimenti uscendo chiederebbe due volte.
- I pannelli sono in `components/management/settings/services/`, le parti comuni in
  `components/management/settings/services/shared/`.

## Tabelle

- Ogni tab è fatta come Gestione Utenti: sopra `app-page-title` con l'icona della tab, sotto `app-table` con
  "Cerca..." a sinistra e il bottone di aggiunta a destra, caselle di selezione, colonne ordinabili, elimina,
  duplica e modifica di riga (`standardRowActions`) e paginatore.
- Richiesta standard delle tabelle, risposta `Page<…Item>` come tutte le tabelle.
- Le icone dei titoli sono PNG (da 19 a 24 px; tazzina e tavolo portati a quadrato con un bordo trasparente), non
  SVG come le altre.

- Servizi Bar: titolo "Servizi bar" con la tazzina (`Services_BarTitle.png`), bottone "Aggiungi servizio bar",
  colonne Servizio, Categoria, Prezzo; righe da `POST /api/catalog/services/bar-service-list`
  (`CatalogService.barServiceList`, `Page<BarServiceItem>`).
- Servizi: titolo "Servizi" con la piuma (`Services_StandardTitle.png`), bottone "Aggiungi servizio", colonne
  Servizio, Prezzo; righe da `POST /api/catalog/services/standard-service-list` (`CatalogService.standardServiceList`,
  `Page<StandardServiceItem>`).
- Servizi Orari: titolo "Servizi orari" con l'orologio (`Services_HourlyTitle.png`), bottone "Aggiungi servizio
  orario", sola colonna Servizio; righe da `POST /api/catalog/services/hourly-service-list`
  (`CatalogService.hourlyServiceList`, `Page<HourlyServiceItem>`).
- Prezzi Automatici: titolo "Prezzi automatici" con l'euro (`Services_AutomaticPricesTitle.png`), bottone
  "Aggiungi prezzo automatico", colonne Servizio, Prezzo; righe da `POST /api/catalog/automatic-price-list`
  (`CatalogService.automaticPriceList`, `Page<AutomaticPriceItem>`).
- Prenotazione tavoli: titolo "Prenotazione servizio tavoli" con il tavolo (`Services_TableBookingTitle.png`),
  bottone "Aggiungi servizio tavoli", sola colonna Nome servizio; righe da `POST /api/catalog/table-service-list`
  (`CatalogService.tableServiceList`, `Page<TableServiceItem>`).
- Servizi orari e Prenotazione tavoli mostrano le righe così come arrivano (`[load]="catalog.hourlyServiceList"`,
  `[load]="catalog.tableServiceList"`).
- Categoria (Servizi bar): `category` delle righe, enum `BarServiceCategory` tradotto dal frontend con
  `management.settings.services.bar_services.categories.*`. Prezzo in euro nel formato della lingua.
- In tedesco "Aggiungi prezzo automatico" è "Preis hinzufügen", più corto, perché sul telefono stia su una riga.

### Elimina e duplica

- In tutte le tab, di riga e sui selezionati, come in Sconti e voucher: popup di conferma
  (`app-delete-items-popup` / `app-duplicate-items-popup`, testi `management.settings.services.<tab>.delete` /
  `duplicate`), spinner a pagina intera, poi la tabella si ricarica.
- Corpo con la sola lista degli id (es. `{ idBarServices }`), tutto o niente, 204. Le righe eliminate escono dalla
  selezione; dopo "duplica selezionati" la selezione si svuota.
- Con il 404 della tab (elemento già eliminato da un altro utente): `service.not_found` per Servizi Bar, Servizi e
  Servizi Orari, `service.automatic_price_not_found` per Prezzi Automatici, `service.table_service_not_found` per
  Prenotazione tavoli. Su Elimina, su Duplica e su Modifica (lettura del pannello e Salva) gli id escono dalla
  selezione e la tabella si ricarica (`reloadIfGone`, uscita `failed` dei popup e del pannello di modifica).
- API (tutte `POST /api/catalog/…`):
  - Servizi bar: `services/delete-bar-service-item`, `services/duplicate-bar-service-item` (`idBarServices`);
  - Servizi: `services/delete-standard-service-item`, `services/duplicate-standard-service-item` (`idStandardServices`);
  - Servizi orari: `services/delete-hourly-service-item`, `services/duplicate-hourly-service-item`
    (`idHourlyServices`);
  - Prezzi automatici: `delete-automatic-price-item`, `duplicate-automatic-price-item` (`idAutomaticPrices`);
  - Prenotazione tavoli: `delete-table-service-item`, `duplicate-table-service-item` (`idTableServices`).

## Parti comuni dei pannelli (`services/shared/`)

- Le tab dei pannelli restano componenti separati per tipo di servizio (potranno cambiare) e usano questi
  sottocomponenti:
  - `app-services-translations`: Nome e Descrizione nella lingua dell'utente, obbligatori, e le altre lingue nei
    riquadri "Altre lingue", sull'elenco `translations` del form; nello stesso file `SERVICE_LANGUAGES`;
  - `app-services-highlighted`: "Metti in evidenza" con la sua (i), sul campo `isHighlighted`;
  - `app-services-images`: sezione Immagine servizio; riceve l'array delle icone (`ServicesImagesIcon`, valore e
    immagine), l'icona scelta, l'anteprima dell'immagine caricata e `isReadonly`; emette `iconChosen` e
    `imageChosen`; generico sul tipo del valore; il riquadro di caricamento accetta solo png, jpeg e webp fino a 5 MB,
    come il backend (`IMAGE_UPLOAD_TYPES`, `MAX_IMAGE_UPLOAD_MEGABYTES`): un file più grande non passa e sotto il
    riquadro compare "Il file supera i 5 MB"; un file vuoto non passa (`isUploadable`) e compare il messaggio di
    `catalog.invalid_image`, come lo darebbe il backend; l'input `error` mostra lì sotto un errore dell'icona;
  - `app-services-popup-alternatives`: sezione Varianti, sull'elenco `variants` del form; nome e prezzo di ogni
    variante sono obbligatori (asterisco);
  - `app-services-price-vat`: Prezzo e IVA (%) affiancati, sui campi `price` e `vatRate`; con `isPriceMandatory`
    l'asterisco sul prezzo;
  - `app-services-period-prices`: "Abilita variazioni di prezzo per periodo" con la sua (i) e un pannello per
    periodo; riceve `isPeriodPricingEnabled`, `periodPrices` e i periodi della stagione;
  - `app-services-limits`: sezione Limiti, sul campo `idSectors`;
  - `app-services-booking-option`: tutta la sezione Opzioni di prenotazione (titolo, bottone, riquadri, (i)),
    sull'elenco `bookingOptions` del form;
  - `app-services-availability-options` / `app-services-availability-option`: Opzioni disponibilità dei servizi
    orari.
- Varianti e opzioni ricevono un pezzo del form del pannello, che quindi le valida, le salva e le modifica; sotto
  ogni campo mostrano gli errori della validazione, sotto l'elenco un errore che riguarda l'elenco intero (es. troppe
  varianti). Lo stesso per i prezzi per periodo.
- Regole comuni delle varianti e delle opzioni di prenotazione, uguali a quelle del backend: `service-form-rules.ts`
  (`serviceVariantRules`, `serviceBookingOptionRules`). I limiti sono costanti delle entità
  (`MAX_SERVICE_NAME_LENGTH`, `MAX_SERVICE_DESCRIPTION_LENGTH`, `MAX_SERVICE_PRICE`,
  `MAX_SERVICE_VARIANT_NAME_LENGTH`, `MAX_SERVICE_BOOKING_QUANTITY`).
- Codici del backend senza `errors[]` che riguardano un campo: `service-field-errors.ts` li mette sotto il campo
  (`catalog.vat_rate_not_allowed` sotto IVA, con le aliquote ammesse; `service.icon_not_found` sotto le icone).

### Nome e descrizione nelle lingue

- La lingua scelta per l'app è sempre la principale: Nome e Descrizione in quella lingua; sotto ciascuno "Altre
  lingue" (`app-expansion-panel`, chiuso) con una riga per ognuna delle altre sei lingue.
- A sinistra "Nome Inglese" o "Descrizione Inglese" (testi `translated_name` e `translated_description` con
  `{{language}}`, il nome della lingua in quella dell'utente da `Intl.DisplayNames`); a destra il campo su una riga,
  lungo quanto la riga. Solo la Descrizione principale è su più righe.
- Etichette tutte larghe 10.5rem (la più lunga, del tedesco), così i campi sono allineati.
- Ordine: italiano, inglese, francese, tedesco, spagnolo, arabo, cinese (`SERVICE_LANGUAGES`). Le traduzioni sono
  l'array `translations` con `language`, `serviceName`, `description`.

### Prezzi per periodo (`app-services-period-prices`)

- Acceso, un `app-expansion-panel` per ogni periodo della stagione corrente con le sue date e un prezzo; la data di
  fine va sotto quella di inizio quando lo spazio non basta, come in Stagioni e periodi.
- Senza periodi un testo rimanda a Stagioni e periodi.
- La (i) dice che con l'opzione abilitata valgono i prezzi dei periodi, altrimenti il prezzo base (parti in
  grassetto fra `**` nella traduzione).
- Su telefono (sotto 48rem, `PHONE_QUERY` col `matchMedia`) le date hanno il mese abbreviato (`month: 'short'`) e
  le due date di un intervallo stanno sulla stessa riga, allineate, ognuna in un riquadro con bordo nero leggero
  su tutti i lati.
  - Se lo spazio delle date è sotto 14rem (telefoni da 360px, misurato sul cinese, la lingua più lunga) la data di
    fine va sotto l'inizio nello stesso riquadro, in tutte le lingue. Su desktop nulla cambia.
- `periodPrices` è facoltativo: senza, ogni periodo mostra sotto le date quello che chi lo usa mette in
  `<ng-template appServicesPeriodContent let-period>` (direttiva `ServicesPeriodContent`), es. le Opzioni
  disponibilità del servizio orario.

### Limiti (`app-services-limits`)

- "Servizio acquistabile nelle seguenti file/settori": `app-filled-multi-select` a scelta multipla su `idSectors`,
  vuota = "Tutte le file/settori".
- Le voci sono i settori dello stabilimento, che il componente chiede da sé a `SectorService.sectorCombobox`
  (`POST /api/catalog/sectors/sector-combobox`) ogni volta che compare, cioè a ogni apertura della tab Prezzi; come la
  tendina Fila/settore della pagina Prenotazioni online (`online-booking-options-panel`).

### Le (i)

- Icona `GeneralIcon_Info.svg` su un cerchio bianco con un'ombra leggera (mixin `info-button` di
  `styles/_info-button.scss`, vedi `html-scss-e-layout.md`); nel popup `GeneralIcon_InfoTitle.svg`.
- Le (i) e i riquadri apribili restano usabili anche in sola lettura.

## Comportamento comune dei pannelli

### Pannelli di aggiunta

- `app-side-panel` con le tab interne `app-tab-group`. Un solo oggetto per tutto il pannello e un solo form
  (`draftForm`) passato alle tab: cambiare tab non perde i dati scritti.
- Bar, servizi generici e orari: a ogni apertura parte solo `GET /api/catalog/tariff-periods` (stagione corrente; il
  `key` numerico di ogni periodo è l'`idPeriod` dei prezzi) per la tab Prezzi: un prezzo vuoto per periodo, che non
  conta come dato scritto. Nel frattempo i campi non si usano e Salva è spento; un errore compare sotto.
- Se la lettura dei periodi fallisce l'interruttore dei prezzi per periodo resta spento e bloccato (`disabled`
  finché i periodi non arrivano); il resto del servizio si salva lo stesso e riaprendo il pannello si riprova.
- Salva (`isSaveDisabled` di `app-side-panel`) è spento finché mancano Nome e Descrizione nella lingua
  dell'utente: asterisco, "Campo obbligatorio" sotto il campo dopo averlo toccato, `requiredText` solo sulla
  traduzione principale. Servizi Bar e Servizi hanno anche altri campi obbligatori (sotto).
- Ogni coppia Dalle / alle ha `timeRange`: un "alle" che non viene dopo il "dalle" (anche la stessa ora) dà l'errore
  sotto i due orari e Salva resta spento.
- Salva manda tutto il form all'API di creazione (risposta 204, in Servizi Bar, Servizi e Servizi Orari l'id del nuovo servizio): spinner
  sul bottone e pannello bloccato; se riesce il pannello si chiude e la tabella si ricarica (`(created)`).
- Se fallisce il pannello resta aperto con i dati: errori dei campi di primo livello sotto il campo, gli altri in
  fondo alla tab, sopra i bottoni.
- Chiudere con dati scritti chiede conferma (modifiche non salvate); anche un riquadro di opzioni aggiunto conta
  come dato scritto.

### Pannelli di modifica

- Si aprono dalla matita della riga, in sola lettura con il solo bottone **Modifica** (bottoni propri nello spazio
  `appSidePanelButtons` di `app-side-panel`), come Informazioni stabilimento.
- Una GET per tab, con l'id nella query string, chiamata la prima volta che la tab si apre e non più finché il
  pannello resta aperto. Durante una GET i campi della sua tab sono bloccati e Modifica è spento. Se fallisce la
  lettura all'apertura il pannello si chiude e compare `app-read-error-popup` (il messaggio del `code`, Chiudi e
  Riprova, che riapre il pannello; per un 404 solo Chiudi); se fallisce una tab letta dopo, al posto dei suoi campi
  compaiono il messaggio e Riprova (`retryTab()`), e il pannello resta aperto.
- I dati letti sono il confronto per le modifiche non salvate (`pristine`, aggiornato tab per tab).
- Controllo di versione, una versione per tab: ogni GET di tab porta `version`, che il pannello tiene per quella tab
  (`versions`) e che il Salva della tab rimanda nell'header `If-Match`; salvare una tab non cambia la versione delle
  altre. Il Salva risponde con la nuova versione nell'`ETag`, che il pannello non usa perché si chiude.
- Con 409 `resource.version_conflict` (un altro utente ha cambiato i dati della tab dopo la lettura) compare
  `app-version-conflict-popup`, la tab torna in sola lettura e si rilegge con la sua GET come all'apertura
  (`reloadTab`): l'utente rifà le modifiche. Il messaggio sopra i bottoni non compare. Se la rilettura fallisce valgono
  le regole delle letture qui sopra.
- La richiesta al backend è in
  `shared documentation/Modifiche per il backend/impostazioni/Servizi - Frontend requests.md`.
- **Modifica** rende modificabile la tab aperta e mostra **Annulla** (rimette i valori letti e torna in sola
  lettura) e **Salva**, che manda la sola tab aperta alla sua API con l'id (risposta 204).
- Spinner su Salva e pannello bloccato; se riesce il pannello si chiude e la tabella si ricarica (`(updated)`); se
  fallisce resta aperto in modifica, errori dei campi di primo livello sotto il campo, gli altri sopra i bottoni.
- I campi obbligatori (nome e descrizione nella lingua dell'utente) contano solo sulla tab Info Generali, gli altri
  controlli (prezzi, coppie Dalle / alle con `timeRange`) solo sulla loro tab.
- Cambiare tab in modifica con modifiche non salvate chiede "Modifiche non salvate" (Resta: si rimane sulla tab,
  la barra la segna di nuovo; Esci senza salvare: la tab torna ai valori letti e si cambia tab); senza modifiche
  si esce dalla modifica e si cambia tab. La X, Esc e il cambio di pagina chiedono lo stesso.

## Servizi Bar

### Aggiungi ("Aggiungi nuovo servizio bar")

- `bar-service-add-item-panel/` (`BarServiceAddItemPanel`); tab "Info Generali", "Prezzi" e "Prenotazioni" larghe
  34%, 33%, 33% come in Informazioni stabilimento.
- Form `BarServiceAddItemRequest`: traduzioni, categoria, icona, Metti in evidenza, prezzi, varianti, prezzi per
  periodo, settori, acquisto online, opzioni di prenotazione.
- Salva: `POST /api/catalog/services/create-bar-service` (`CatalogService.createBarService`), che risponde con l'id del
  servizio creato; poi, se c'è, l'immagine (sotto).
- Obbligatori, come nel backend: Nome e Descrizione nella lingua dell'utente, il nome nelle altre lingue che hanno una
  descrizione (`otherTranslationRules`: l'errore "Scrivi anche il nome in questa lingua" è sotto la descrizione, perché il
  nome può stare in un riquadro chiuso), Prezzo, e nome e prezzo di ogni variante. I limiti dell'IVA (tra 0 e 100), delle
  varianti (50) e delle opzioni di prenotazione (20) non sono nel form: si vede l'errore del backend.
  Limiti: nome 255 caratteri, descrizione 10.000, nome della variante 120, ogni prezzo fino a 100.000 (anche quelli per
  periodo), numeri delle opzioni di prenotazione da 1 a 32.767 con il massimo di articoli non minore del minimo.
- Un errore dell'API su un campo apre la tab di quel campo (`showTabOf`), dove si vede.

#### Immagine

- L'immagine scelta dal computer parte con Salva, dopo i dati: `POST /api/catalog/services/{id}/image`
  (`CatalogService.uploadServiceImage`, multipart, campo `file`), nell'aggiunta con l'id della risposta, nella modifica
  dopo il Salva di Info Generali. Il Salva manda l'icona com'era: la toglie il caricamento, così se fallisce resta.
- Se il caricamento fallisce il servizio resta salvato: il pannello si chiude, la tabella si ricarica e compare
  `app-service-image-error-popup` ("Immagine non caricata", con il messaggio del `code` e Chiudi).
- In modifica il riquadro mostra l'immagine che il servizio ha già (`imageUrl` di
  `get-bar-service-general-info`), finché non si sceglie un'icona o un'altra immagine; Annulla la rimette.
- La X sull'immagine (`isImageRemovable` e `imageRemoveRequest` di `app-services-images`): un'immagine non ancora salvata
  si toglie senza chiedere e torna l'icona di prima; quella salvata, in Modifica, si elimina subito dopo la conferma
  (`app-confirm-popup`, `DELETE /api/catalog/services/{id}/image`, `CatalogService.deleteServiceImage`), con lo spinner a
  pagina intera e l'errore sotto l'immagine. In sola lettura la X è spenta.

#### Tab Info Generali (`ServiceGeneralInfoTab`)

- Nome e Descrizione nelle lingue (`app-services-translations`).
- Categoria: tendina dell'enum `BarServiceCategory`, con le 12 categorie dell'enum `BarCategory` del backend e nel suo
  ordine (Bar, Pizzeria, Ristorante, Friggitoria, Paninoteca, Antipasti, Primi piatti, Secondi piatti, Dolci, Frutta,
  Gelateria, Bevande), nel form `category`; il backend ne controlla la validità.
- Immagine servizio: 22 icone, elenco fisso del frontend senza API, enum `BarServiceIcon` con file
  `Services_Bar<Nome>.png` (PNG grigi), nel form `icon`; il valore va al backend.
  - Compresa la tazzina grigia `Services_BarCoffee.png`; quella blu, `Services_BarTitle.png`, è del titolo della
    tab.
  - In alternativa un'immagine caricata con `app-file-drop-zone`: scegliere l'una toglie l'altra.
- Metti in evidenza, con la (i): con l'opzione abilitata, chi prenota un servizio spiaggia vede questo servizio e
  lo aggiunge alla prenotazione.

#### Tab Prezzi (`ServicePricesTab`)

- Fatta con `app-services-price-vat`, `app-services-popup-alternatives`, `app-services-period-prices` e
  `app-services-limits`, in quest'ordine, poi l'interruttore "Abilita acquisto online" (`isOnlinePurchaseEnabled`).
- Prezzo (obbligatorio) e IVA (%) affiancati, con "€" scritto accanto ai campi in euro, come nelle altre pagine. L'IVA
  vuota vuol dire senza aliquota; un'aliquota non ammessa la rifiuta il backend (`catalog.vat_rate_not_allowed`).
- Varianti: "Aggiungi nuova variante", righe Nome variante + Prezzo (tutti e due obbligatori) + cestino.
- Su telefono i riquadri della tab hanno 0.75rem di margine ai lati, così l'interruttore dei prezzi per periodo e
  la sua (i) stanno su una riga a 412px.

#### Tab Prenotazioni (`ServiceBookingsTab`)

- Riceve dal pannello l'elenco `bookingOptions` del form (`ServiceBookingOption[]`).
- Sezione "Opzioni di prenotazione": "Aggiungi opzioni di prenotazione" aggiunge un riquadro (come i periodi di
  Sconti e voucher, con la X in alto per toglierlo; all'apertura nessun riquadro) con:
  - Periodo Dal / al (`dateFrom`, `dateTo`);
  - Orari prenotazioni da gestionale e Orari prenotazioni online, Dalle / alle (`app-filled-time-field`,
    `managementStartTime`/`managementEndTime`, `onlineStartTime`/`onlineEndTime`);
  - Numero massimo di prenotazioni all'ora e al giorno;
  - Intervallo di tempo (enum `BookingTimeInterval`: 15, 30, 45 minuti, 1 e 2 ore; nel form i minuti,
    `intervalMinutes`, con `BOOKING_TIME_INTERVAL_MINUTES`), con la (i) accanto all'etichetta: quanto tempo deve passare
    tra un'ordinazione e l'altra dello stesso servizio. Minuti diversi da questi già salvati non si vedono nella tendina e
    restano finché non si sceglie un valore;
  - Numero minimo e massimo di articoli per prenotazione.
- Ogni opzione ha anche `weekdays` (enum `Weekday`) e `maxPerSlot`, che il form non mostra: tornano al backend come letti
  (`[]` e `null` per le opzioni nuove), così un Salva non li cancella.
- I campi delle coppie sono larghi uguali (8.5rem) e le parole Dal / al / Dalle / alle hanno una larghezza comune,
  così stanno in due colonne allineate; un campo da solo (numeri e Intervallo di tempo) occupa lo spazio di una
  coppia. Su telefono i campi riempiono la riga.

### Modifica ("Modifica servizio bar")

- `bar-service-edit-item-panel/` (`BarServiceEditItemPanel`): copia del pannello di aggiunta, input `idBarService`,
  stesse tre tab e stesso form `BarServiceAddItemRequest`.
- GET (`idBarService` nella query string):
  - Info Generali, all'apertura: `GET /api/catalog/services/get-bar-service-general-info`
    (`BarServiceSelectedGeneralInfo`, le lingue mancanti restano vuote);
  - Prezzi: `GET /api/catalog/services/get-bar-service-prices` insieme a `catalog/tariff-periods`
    (`BarServiceSelectedPrices`, i prezzi sui periodi della stagione corrente);
  - Prenotazioni: `GET /api/catalog/services/get-bar-service-booking-options` (`BarServiceSelectedBookingOptions`:
    `bookingOptions`, l'elenco `ServiceBookingOption[]`, e `version`).
- Ogni risposta delle GET ha in più `version` (vedi "Pannelli di modifica").
- Salvataggi (`POST /api/catalog/services/…`): `update-bar-service-general` (`BarServiceUpdateGeneralRequest`),
  `update-bar-service-prices` (`BarServiceUpdatePricesRequest`), `update-bar-service-booking`
  (`BarServiceUpdateBookingRequest`, l'elenco intero delle opzioni).
- Un'icona salvata fuori dalle 22 non si vede nella griglia: il suo nome resta in `otherIcon` e torna con il Salva di Info
  Generali, finché non si sceglie un'icona o un'immagine.
- In sola lettura: campi `readonly` dei Signal Forms (interruttori e tendina dei settori `disabled`) e, con l'input
  `isReadonly` delle tre tab, spenti icone, caricamento immagine, aggiungi/togli varianti e opzioni.

## Servizi (servizi generici)

### Aggiungi ("Aggiungi nuovo servizio")

- `general-service-add-item-panel/` (`GeneralServiceAddItemPanel`), stessa struttura del pannello del bar, form
  `StandardServiceAddItemRequest`, titolo `management.settings.services.standard_services.add_title`.
- Tab Info Generali (`GeneralServiceGeneralInfoTab`, `general-service-general-info-tab`, 34%): come quella del bar
  senza Categoria, con `app-services-translations`, `app-services-images` e `app-services-highlighted`.
- Tab Prezzi (`GeneralServicePricesTab`, `general-service-prices-tab`, 33%): come quella del bar senza Varianti,
  con `app-services-price-vat`, `app-services-period-prices` e `app-services-limits`, e "Abilita acquisto online"
  per ultimo.
- Tab Prenotazioni (`GeneralServiceBookingsTab`, `general-service-bookings-tab`, 33%): come quella del bar, tutta
  la sezione `app-services-booking-option` sull'elenco `bookingOptions`.
- Il form ha anche `price`, `vatRate`, `isPeriodPricingEnabled`, `periodPrices`, `idSectors`,
  `isOnlinePurchaseEnabled` e `bookingOptions`.
- Obbligatori, limiti, errori del backend e immagine come in Servizi Bar (prezzo obbligatorio, nome nelle lingue con una
  descrizione, X e caricamento dell'immagine, `app-service-image-error-popup`).
- Nella modifica (bar, servizi standard e servizi orari), Salva della tab Prezzi rimanda anche i prezzi per periodo (e
  per i servizi orari le disponibilità) letti per periodi che la tab non mostra, es. non più nella stagione corrente
  (`hiddenPeriodPrices`, `hiddenPeriodAvailabilities`): niente di quello che è stato letto si perde.
- Icone: 21 PNG grigi 29×29, ridotti tutti dello stesso fattore, `Services_Standard<Nome>.png`, enum
  `StandardServiceIcon` (Surfing, Surfboard, Sailing, Kitesurfing, Volleyball, Baseball, Basketball, Cricket,
  SportsField, Tennis, Pool, Locker, Lock, Cabin, Shower, Baby, Accessible, Parking, Pets, Motorcycle, Bicycle);
  nel form `icon`.
- Salva: `POST /api/catalog/services/create-standard-service` (`CatalogService.createStandardService`), che risponde con
  l'id del servizio creato; poi, se c'è, l'immagine.

### Modifica ("Modifica servizio")

- `general-service-edit-item-panel/` (`GeneralServiceEditItemPanel`): copia del pannello di modifica del bar senza
  Categoria e Varianti, input `idStandardService`, form `StandardServiceAddItemRequest`, titolo
  `standard_services.edit_title`.
- GET: `get-standard-service-general-info`, `get-standard-service-prices` insieme a `catalog/tariff-periods`,
  `get-standard-service-booking-options` (`GET /api/catalog/services/…`; risposte `StandardServiceSelectedGeneralInfo`,
  `StandardServiceSelectedPrices` e `StandardServiceSelectedBookingOptions`, tutte con `version`).
- Salvataggi: `update-standard-service-general`, `update-standard-service-prices`, `update-standard-service-booking`
  (`POST /api/catalog/services/…`; corpi `StandardServiceUpdateGeneralRequest`, `StandardServiceUpdatePricesRequest`,
  `StandardServiceUpdateBookingRequest` con `idStandardService`).

## Servizi Orari

### Aggiungi ("Aggiungi nuovo servizio orario")

- `app-hourly-service-add-item-panel`, tab Info Generali, Prezzi e Prenotazioni (34/33/33). Form
  `HourlyServiceAddItemRequest` (`entities/settings/services/`).
- Info Generali (`app-hourly-service-general-info-tab`): come quella del servizio generale (nomi e descrizioni
  nelle lingue, Immagine servizio con le icone `StandardServiceIcon`, Metti in evidenza).
- Prezzi (`app-hourly-service-prices-tab`), in quest'ordine:
  - Prezzo (obbligatorio, 0-100.000) e IVA (`app-services-price-vat`); l'IVA ha l'asterisco (`isVatMandatory`) finché non
    ce l'hanno tutte le disponibilità in vigore e delle opzioni di prenotazione, almeno una, come nel backend;
  - "Abilita variazioni di prezzo per periodo" con `app-services-period-prices` senza `periodPrices`: acceso, ogni
    periodo ha sotto le date le sue Opzioni disponibilità (`periodAvailabilities`); spento, ce n'è un blocco solo che
    vale per tutta la stagione (`availabilityOptions`);
  - Limiti e "Abilita acquisto online".
- Si mandano solo le disponibilità in vigore, l'altro elenco è `[]` (`withAvailabilitiesInForce`): il backend ne tiene un
  tipo solo.
- Prenotazioni (`app-hourly-service-bookings-tab`): sezione "Opzioni di prenotazione" come negli altri servizi
  (titolo, bottone blu "Aggiungi opzioni di prenotazione", un riquadro per opzione con la X, nessuno all'apertura).
  - Ogni riquadro ha la riga Periodo Dal/al (`app-filled-date-field`, "al" con `minDate` = "Dal", allineati con
    Dalle/alle) e sotto lo stesso blocco `app-services-availability-option` dei periodi.
  - Entità `HourlyServiceBookingOption`, elenco `bookingOptions`: `ServiceBookingOption` più `availabilityOption`. Il form
    mostra solo date e disponibilità; gli altri campi tornano come sono stati letti.
  - Sul telefono la sezione e i riquadri hanno meno margini ai lati.
- Salva: `POST /api/catalog/services/create-hourly-service` (`CatalogService.createHourlyService`), ripulito a ogni livello
  con `onlyFieldsOf` (anche periodi, opzioni e fasce); risponde con l'id del servizio creato, poi parte l'immagine.
- Nome nelle lingue con una descrizione, limiti dei testi, errori dell'icona e immagine come in Servizi Bar.

### Opzioni disponibilità (`app-services-availability-options`)

- Titolo, bottone chiaro "Aggiungi opzioni di prenotazione" con il + blu `GeneralIcon_AddBlue.svg`, un riquadro per
  opzione con la X; all'apertura una vuota per la stagione e per ogni periodo, con tutti i giorni scelti.
- Ogni riquadro è `app-services-availability-option`, usato anche dai riquadri della tab Prenotazioni:
  - Orari disponibilità Dalle/alle;
  - Giorni disponibili: sette caselle Lun-Dom, quattro per riga, numeri ISO 1-7 da `DAYS_OF_WEEK`;
  - Fasce orarie disponibili: casella e prezzo in € per 10, 15, 30, 45 min, 1 hr, 1 hr e 30 min, 2 e 3 hr (enum
    `ServiceTimeSlot`), più Personalizzato con casella, minuti e prezzo;
  - IVA (%), Max prenotazioni per fascia oraria e l'interruttore Fasce orarie fisse.
- Entità `ServicePeriodAvailability`, `ServiceAvailabilityOption`, `ServiceTimeSlotPrice`.
- Controlli come nel backend (`serviceAvailabilityRules`): fine dopo l'inizio, almeno un giorno, prezzo obbligatorio
  nelle fasce accese (0-100.000), minuti della personalizzata 1-1.440 e obbligatori se è accesa o ha un prezzo, massimo
  prenotazioni 1-32.767. Accendendo una fascia il campo che manca si segna subito.
- Durate del backend non integrate (4, 5 e 6 ore): non si mostrano e tornano come sono state lette, in fondo a
  `timeSlotPrices`.
- Quando il riquadro è più stretto di 20rem (telefoni) il prezzo va sotto la sua fascia e i giorni due per riga; la
  tab riduce i margini dei periodi con `--expansion-panel-content-padding`.

### Modifica ("Modifica servizio orario")

- `app-hourly-service-edit-item-panel`, come quello del servizio generale, input `idHourlyService`.
- API sotto `/api/catalog/services/…`.
- GET (`idHourlyService` nella query string): `get-hourly-service-general-info`, `get-hourly-service-prices`
  insieme a `catalog/tariff-periods`, `get-hourly-service-booking-options`.
- Salvataggi: `update-hourly-service-general`, `update-hourly-service-prices`, `update-hourly-service-booking`;
  entità `HourlyServiceSelectedGeneralInfo`, `HourlyServiceSelectedPrices`, `HourlyServiceSelectedBookingOptions`
  (tutte con `version`), `HourlyServiceUpdate*Request`.
- Il form si blocca con `disabled(path, …)` (caselle e interruttori non hanno `readonly`); le tab Prezzi e
  Prenotazioni ricevono `isReadonly` per spegnere "Aggiungi" e le X.
- Le opzioni lette si completano con tutte le otto fasce (`availabilityOf`); senza giorni valgono tutti, e dove non ce ne
  sono se ne mostra una vuota, come nell'aggiunta (anche in un'opzione di prenotazione con `availabilityOption` `null`).
- Un servizio letto senza IVA (esente) può restare senza.

## Prezzi Automatici

### Aggiungi ("Aggiungi nuovo prezzo automatico")

- `app-automatic-price-add-item-panel`, due tab larghe uguali: Info Generali e "Prezzi e Validità" (chiave
  `management.settings.services.form.tabs.prices_validity`). Form `AutomaticPriceAddItemRequest`.
- Il titolo in francese e tedesco è accorciato ("Nouveau prix automatique", "Neuer automatischer Preis") perché sul
  telefono non vada a capo.
- Info Generali (`app-automatic-price-general-info-tab`): nomi e descrizioni nelle lingue e Immagine servizio,
  senza Metti in evidenza. Le icone sono quelle del bar (`BarServiceIcon`) seguite da quelle degli altri servizi
  (`StandardServiceIcon`); `icon` vale una delle due (i nomi non si sovrappongono).
- Prezzi e Validità (`app-automatic-price-prices-validity-tab`), in quest'ordine:
  - Prezzo e IVA (`app-services-price-vat`);
  - "Abilita acquisto online";
  - "Periodi validità" (`app-automatic-price-validity-periods`): bottone blu "Aggiungi periodo validità", un
    riquadro con la X per periodo, nessuno all'apertura, con Periodo Dal/al, Giorni della settimana (sette caselle
    Lun-Dom su una riga, numeri ISO da `DAYS_OF_WEEK`, testi brevi dei servizi orari) e Fasce orarie Dalle/alle
    con il cestino e "Aggiungi fascia oraria" come nei voucher;
  - "Validità sui servizi" (`app-automatic-price-service-validity`): un `app-expansion-panel` per Servizi bar,
    Servizi e Servizi orari con "Seleziona tutti" e una casella per servizio, tre per riga; poi la casella
    "Abilita prezzo automatico per ogni singolo servizio" con la (i): attiva, il prezzo si applica e si inserisce
    per ogni singolo servizio aggiunto alla prenotazione; spenta, una sola volta per singolo ordine;
  - Limiti (`app-services-limits`).
- I servizi arrivano all'apertura da `GET /api/catalog/get-automatic-price-services`, senza parametri
  (`AutomaticPriceServices`, tre elenchi di `ComboboxItem`): durante la lettura le tab sono bloccate e Salva è
  spento; se fallisce compare l'errore e i pannelli restano vuoti.
- Entità `AutomaticPriceValidityPeriod`, `AutomaticPriceTimeSlot`.
- Salva: `POST /api/catalog/create-automatic-price` (`CatalogService.createAutomaticPrice`), ripulito a ogni
  livello con `onlyFieldsOf`.

### Modifica ("Modifica prezzo automatico")

- `app-automatic-price-edit-item-panel`, come quello dei servizi orari ma con le due tab, input `idAutomaticPrice`.
- GET (`idAutomaticPrice` nella query string): `get-automatic-price-general-info`;
  `get-automatic-price-prices-validity` insieme a `get-automatic-price-services`.
- Salvataggi: `update-automatic-price-general`, `update-automatic-price-prices-validity`; entità
  `AutomaticPriceSelectedGeneralInfo`, `AutomaticPriceSelectedPricesValidity` (tutte e due con `version`),
  `AutomaticPriceUpdate*Request`.
- Il form si blocca con `disabled(path, …)`; la tab Prezzi e Validità riceve `isReadonly` per spegnere "Aggiungi
  periodo validità", le X, "Aggiungi fascia oraria" e i cestini. Il bottone secondario spento di `app-button`
  lascia l'icona blu piena: è il comportamento del wrapper.

## Prenotazione tavoli

### Aggiungi ("Aggiungi nuovo servizio tavoli")

- `app-table-service-add-item-panel` (titolo `table_booking.add_title`), tab "Info Generali" e "Disponibilità e
  Prenotazioni" (50% e 50%). Form `TableServiceAddItemRequest`.
- Info Generali (`app-table-service-general-info-tab`): nome e descrizione con `app-services-translations`,
  obbligatori nella lingua dell'utente, e "Metti in evidenza" con la sua (i) (`app-services-highlighted`); niente
  immagine servizio.
- Disponibilità e Prenotazioni (`app-table-service-availability-bookings-tab`): le tre sezioni sotto.
- Validazioni come negli altri pannelli: nome e descrizione e le coppie Dalle / alle (`timeRange`).
- Salva si accende con il form valido e chiama `POST /api/catalog/create-table-service`
  (`CatalogService.createTableService`, `TableServiceAddItemRequest`).

#### Disponibilità (`app-table-service-availabilities`)

- Come i periodi dei Prezzi Automatici: "Aggiungi disponibilità", un riquadro con la X per disponibilità, Periodo
  Dal/al e "Giorni e fasce orarie" con "Applica stessi orari per tutti i giorni" e i sette giorni, ognuno con
  casella e Dalle/alle.
- Con la casella spuntata gli orari comuni si copiano in tutti i giorni e quelli dei giorni si spengono; senza la
  spunta sono spenti gli orari comuni (regole `disabled` nel form del pannello).

#### Disponibilità tavoli e posti (`app-table-service-capacity`)

- "Gestisci la disponibilità per" con `app-radio-group` sull'enum `TableServiceAvailabilityMode` (`Tables`,
  `Seats`).
- Con Numero tavoli: righe "N° tavoli … da … posti" con il cestino e "Aggiungi tavoli". Con Numero posti: solo il
  campo dei posti totali.
- Cambiando scelta i valori scritti (righe dei tavoli e posti totali) spariscono. Se ce ne sono, prima chiede
  conferma con `app-confirm-popup` ("Attenzione", "Cambiare il modo in cui viene gestita la disponibilità
  cancellerà le impostazioni attuali.", Annulla / Cambia rosso, testi in `form.tables.mode_change`).
  - Cambia svuota; Annulla, X o Esc rimettono la scelta di prima con i suoi valori.

#### Opzioni di prenotazione (`app-table-service-booking-options`)

- "Durata media del pasto": `app-filled-select` sull'enum `TableServiceMealDuration`, da 30 minuti a 3 ore.
- "Numero massimo di prenotazioni per fascia oraria".
- "Consenti prenotazione": `app-radio-group` sull'enum `TableServiceBookingPayment` (`None`, `Deposit`).
- Solo con "Con caparra confirmatoria", sotto una riga: "Gestione caparra" con la sua (i) e la tendina sull'enum
  `TableServiceDepositMode` (`Total`, `PerPerson`, Totale di default) e "Importo caparra" in %.
  - La (i): Totale, una sola volta per il totale delle persone/posti; A persona, per ogni persona/posto.
- Passando a "Senza alcun pagamento" con dati scritti nella caparra (importo, o "A persona") chiede conferma con
  `app-confirm-popup` ("Attenzione", testi in `form.tables.payment_change`): Cambia rimette Totale e svuota
  l'importo; Annulla, X o Esc tornano alla caparra con i suoi dati. Da "Senza alcun pagamento" alla caparra
  nessun popup.
- L'interruttore "Abilita prenotazione online".

#### Entità

- `TableServiceAddItemRequest`: `translations`, `isHighlighted`, `availabilities`, `availabilityMode`,
  `tableGroups`, `totalSeatQuantity`, `averageMealDuration`, `maxBookingPerTimeSlotQuantity`, `bookingPayment`,
  `depositMode`, `depositPercentage`, `isOnlineBookingEnabled`.
- `TableServiceAvailability`: `dateFrom`, `dateTo`, `isSameTimeForAllDays`, `startTime`, `endTime`, `days`.
- `TableServiceAvailabilityDay`: `dayOfWeek`, `isEnabled`, `startTime`, `endTime`.
- `TableServiceTableGroup`: `tableQuantity`, `seatPerTableQuantity`.

### Modifica ("Modifica servizio tavoli")

- `app-table-service-edit-item-panel` (titolo `table_booking.edit_title`), come il pannello di modifica dei Prezzi
  Automatici.
- GET (`?idTableService=`): `GET /api/catalog/get-table-service-general-info` e
  `GET /api/catalog/get-table-service-availability-bookings` (`TableServiceSelectedGeneralInfo`,
  `TableServiceSelectedAvailabilityBookings`, tutte e due con `version`); le disponibilità lette si completano con
  tutti e sette i giorni.
- Salvataggi: `POST /api/catalog/update-table-service-general` e
  `POST /api/catalog/update-table-service-availability-bookings` (`TableServiceUpdate*Request` con
  `idTableService`).
- Il form si blocca con `disabled(path, …)`; la tab Disponibilità e Prenotazioni riceve `isReadonly` per spegnere
  "Aggiungi disponibilità", le X, "Aggiungi tavoli" e i cestini (la (i) resta attiva).
