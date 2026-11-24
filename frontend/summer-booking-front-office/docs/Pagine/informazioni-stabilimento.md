# Informazioni stabilimento

Rotta `/settings/bathhouse-configuration/1` … `/3`, vista `views/settings/bathhouse-configuration/`
(`BathhouseConfiguration`).

## Struttura

- Pagina a tab fatta con il wrapper `app-tab-nav`: ogni tab è una rotta figlia con la sua pagina.
- Le tab sono l'intestazione del riquadro bianco delle impostazioni (mixin `settings-panel`). La pagina della tab aperta
  sta nello stesso riquadro, quindi le sue sezioni si separano con i titoli di sezione e non con altri riquadri.
- La voce in `SETTINGS_PAGES` ha `loadChildren`, che carica con la pagina le sue rotte
  (`bathhouse-configuration.routes.ts`): le tab non finiscono nel pacchetto iniziale, si scaricano con la pagina.
- Rotte e nomi delle tab vengono da `BATHHOUSE_CONFIGURATION_TABS` (`bathhouse-configuration-tabs.ts`), che importa le
  pagine delle tab per nome in testa al file (niente `import()` con il percorso per ogni tab).
- La tab 1 è quella di partenza: senza numero, o con un numero che non c'è, si apre la tab 1. Il refresh riapre la tab
  dell'URL.
- La guardia delle modifiche non salvate sta su ogni tab e non sulla pagina, altrimenti uscendo chiederebbe due volte.
- Ogni tab legge e salva da sé, con la sua API e i suoi bottoni sotto a destra: "Modifica" sblocca la tab e lascia il
  posto ad "Annulla" e "Salva". I bottoni sono piccoli e larghi 8rem.

## Tab

- Tre tab: "Informazioni Generali", "Servizi", "Media"
  (`management.settings.bathhouse_configuration.tabs.general_information`, `.services`, `.media`), larghe 34%, 33% e
  33% della barra (`widthPercent`).
- Una tab non diventa mai più stretta del suo nome: se i nomi non stanno, le tab si allargano e Material mostra le frecce.
- Sui telefoni (`width < 48rem`) le tab diventano una tendina che occupa tutta l'intestazione del riquadro.

## Tab 1 – Informazioni Generali

- Componente `BathhouseGeneralSettings`, modello `BathhouseGeneralInformation` in
  `entities/settings/bathhouse-configuration/` per il form e il salvataggio; la lettura è
  `BathhouseSelectedGeneralInformation`, lo stesso modello con `version`.
- Campi `filled` con l'etichetta sopra, su righe:
  - Nome stabilimento 35% (obbligatorio, `requiredText`, con l'asterisco), Indirizzo 45%, Cap 20%;
  - Città, Provincia, Regione e Nazione un quarto ciascuno;
  - Telefono, Whatsapp ed Email un terzo ciascuno;
  - Sito web, Facebook e Instagram un terzo ciascuno;
  - Descrizione su tutta la riga;
  - Orario di apertura ("Dalle" e "Alle"), Cancellazione e Animali, un terzo ciascuno;
  - Regolamento su tutta la riga.
- Lunghezze massime come il backend, costanti in `BathhouseGeneralInformation` (`MAX_LENGTH`, il campo non accetta
  caratteri oltre): 255 per Nome stabilimento, Indirizzo, Città, Provincia, Regione, Sito web, Facebook e Instagram
  (`MAX_BATHHOUSE_TEXT_LENGTH`); 16 per il Cap (`MAX_BATHHOUSE_POSTAL_CODE_LENGTH`); 255 per l'Email
  (`MAX_EMAIL_LENGTH` di `entities/shared/email-address.ts`).
- Email: `emailAddress`, cioè vuota oppure con una sola `@`, né all'inizio né alla fine, come il backend.
- Telefono e Whatsapp: `isPhoneField` e `phoneNumberWithoutPrefix`, solo cifre e spazi, al massimo 32 caratteri
  (`MAX_BATHHOUSE_PHONE_LENGTH`), come il backend, che non ha un prefisso per questi numeri. Il campo accetta anche
  caratteri oltre il limite: l'errore va sotto il campo e Salva si spegne.
- Orario di apertura: due `app-filled-time-field` "Dalle" e "Alle" (`HH:mm`) con `timeRange`: con tutti e due gli
  orari "Alle" deve venire dopo "Dalle" (uguale non va), come vuole il backend.
- Descrizione e Regolamento: `app-rich-text-field` (grassetto, corsivo, sottolineato, elenco puntato e numerato, link),
  alte 6.25rem (`--rich-text-field-height`) con lo scorrimento interno. Al massimo 5000 caratteri visibili,
  formattazione esclusa (`MAX_BATHHOUSE_RICH_TEXT_LENGTH`, `maxRichTextLength`): oltre il limite l'errore va sotto il
  campo e Salva si spegne. Il backend conta invece l'HTML (al massimo 20.000 caratteri): con molta formattazione il
  suo `validation.max_length` può arrivare e va sotto il campo. Icone provvisorie `GeneralIcon_Bold.svg` …
  `GeneralIcon_TextLink.svg`, in attesa di quelle della grafica.
- Cancellazione: tendina con le scelte dell'enum `CancellationPolicy` (Gratuita entro 48 ore, Gratuita entro 24 ore,
  Nessuna). L'API usa invece `isFreeCancellation` e `cancellationNoticeHours`, come il backend, e la tab converte: la
  tendina usa `[value]` / `(valueChange)` e non `[formField]`; ore diverse lette dal backend lasciano la tendina vuota.
  Gli errori del backend su `isFreeCancellation` o `cancellationNoticeHours` vanno sotto la tendina.
- Animali: tendina con l'enum `PetPolicy` (Ammessi, Non ammessi, Su richiesta); enum e non booleano perché le tendine
  dell'app hanno valori stringa. Non è mai vuota: parte da "Su richiesta" (`PetPolicy.OnRequest`), come il backend
  per uno stabilimento senza scheda; il backend non accetta `null`.
- Nazione: tendina con le nazioni del backend (`lookups/countries`), nel suo ordine. Il valore è il codice ISO (`code`),
  così ogni voce resta legata alla sua nazione (es. per una bandiera, `assets/flags/<code in minuscolo>.svg`). Senza
  nazione salvata parte dalla prima voce della lista; una nazione salvata che non è tra le voci lascia la tendina vuota.
- Le nazioni si caricano a parte dai dati della tab, con `DropdownList` come le altre tendine: se arrivano dopo, la
  prima voce si sceglie allora (solo se lo stabilimento non ne ha una e l'utente non l'ha scelta). Se la lettura
  fallisce, l'errore compare solo sotto Nazione e il resto della tab funziona. Dopo l'errore, aprire la tendina in
  Modifica (clic, Invio o Spazio: `(openRequest)` di `app-filled-select`, legato a `retry()`) rilegge le nazioni; con la
  lista già caricata non parte nessuna chiamata, e in sola lettura la tendina non rilegge. Con l'errore la tendina
  non mostra la sua dicitura ("Scegli"), come le altre tendine.
- Se il salvataggio risponde `catalog.country_not_found` (codice che non è nel catalogo del backend), l'errore va sotto
  Nazione (`apiFieldError`) e sparisce appena si sceglie un'altra nazione.
- Salva manda tutti i campi della tab, cambiati o no, ed è spento finché il form non è valido (es. senza Nome
  stabilimento). Gli errori del backend su un campo (`errors[]`) vanno sotto quel campo, gli altri in fondo alla tab.
- Sui telefoni i campi vanno uno per riga, tranne Cap (spostato sotto Città) con Provincia e Regione con Nazione, a metà
  riga ciascuno (Città da sola, così anche i nomi lunghi delle nazioni non si tagliano). Le righe passano i loro campi a
  un'unica griglia di sei colonne (`display: contents`), senza ripetere il markup.
- Dopo un salvataggio riuscito la tab e la cache tengono i valori come li salva il backend: testi senza spazi ai lati e
  un testo vuoto `null`.
- I campi che il backend restituisce `null` restano vuoti, come un form da compilare. I mock partono con tutti i campi
  `null`, tranne `isFreeCancellation` (`false`, Cancellazione "Nessuna") e `petPolicy` (`OnRequest`).

## Tab 2 – Servizi

- Componente `BathhouseServicesSettings`, modello `BathhouseServiceItem`; la lettura è `BathhouseSelectedServices`
  (`services` e `version`). Mostra i servizi del backend come caselle di
  spunta (`app-checkbox`, con l'icona `iconUrl`, solo se è un indirizzo https o dell'app, e il nome) in quattro sezioni
  fisse con `app-section-title`, sempre in
  quest'ordine: Accessibilità e Comfort, Sport e attività, Ristorazione e extra, Spiaggia (enum
  `BathhouseServiceGroup`). Tre servizi per riga, uno sui telefoni.
- Ogni servizio va nella sezione del suo `categoryCode`, nell'ordine della risposta. Sezione e ordine li decide il
  backend: il frontend non ha tabelle di id. Una sezione senza servizi non compare; un servizio con un altro
  `categoryCode` non si vede e al salvataggio resta com'è.
- Modifica sblocca le caselle; Salva, sempre attivo, manda tutti i servizi letti (`services`), ognuno con tutti i
  suoi campi e `isSelected` vero o falso come l'ha lasciato l'utente. Gli errori del salvataggio compaiono in fondo
  alla tab.
- Una modifica non salvata è un servizio spuntato o tolto rispetto a quelli letti.

## Tab 3 – Media

- Componente `BathhouseMediaSettings`, modelli `BathhouseSelectedMedia` (lettura: `BathhouseMedia`, con
  `BathhousePhoto`, e `version`) e `BathhouseMediaRequest` (salvataggio). Ha quattro sezioni, ognuna con il suo
  titolo: Posizione (la mappa), Immagini (immagine principale e galleria di al massimo 30 foto), Link Tour virtuale e
  Documenti PDF (Listino Prezzi e Menù).
- Mappa: `app-location-map` (Google Maps, caricato da `GoogleMapsBehaviour`), segnaposto su `latitude` / `longitude`
  della GET con zoom 15; senza posizione l'Italia con zoom 5. Senza chiave di Google Maps o se lo script non si carica,
  al posto della mappa compare "Impossibile caricare la mappa" (`media.position.map_error`).
- Foto e PDF seguono lo standard dei caricamenti (vedi `api-e-mock.md`): le aree delle immagini e i bottoni "Carica"
  sono sempre attivi, anche fuori da Modifica, e ogni file scelto parte subito, uno alla volta, con `app-page-spinner`
  che blocca la pagina; il primo errore ferma la fila.
- Le API di foto e PDF sono quelle del backend e rispondono con tutta la scheda dello stabilimento: `CatalogService` ne
  legge solo i media (`PropertyInfoMedia`, compresi link del tour e posizione), con la galleria ordinata per
  `sortOrder`; la tab li mostra e li passa alla cache (`keepMedia`).
- Immagine principale: un solo file, che sostituisce quello di prima. Galleria: righe da 4 (2 sui telefoni); le caselle
  vuote completano la riga, con almeno una libera finché le foto non sono 30, e accettano più file insieme: partono
  quelli validi finché c'è posto, per gli altri compare l'errore (`catalog.invalid_image` o `catalog.gallery_full`).
- Prima di mandare un file la tab controlla tipo, file non vuoto e peso (`isUploadable`: png, jpeg, webp fino a 5 MB;
  PDF fino a 10 MB) e il posto libero nella galleria, con gli stessi codici di errore del backend
  (`catalog.invalid_image`, `catalog.invalid_pdf`, `catalog.gallery_full`). Questi errori, e quelli del backend sui
  file, compaiono sotto Immagini o sotto Documenti PDF.
- Un PDF presente mostra il nome del file (`priceListPdfFileName`, `menuPdfFileName`; un nome lungo si accorcia con i
  puntini) e "Apri il documento", che lo apre in una nuova scheda; senza file "Nessun file selezionato".
- Ogni foto caricata e ogni PDF presente hanno la piccola X in alto a destra (`app-icon-button`), che chiede conferma
  (`app-confirm-popup`, "Elimina immagine" / "Elimina documento") e poi chiama la cancellazione.
- Nelle anteprime e nei link vanno solo indirizzi https o dell'app (`fileLinkOf`, `behaviours/files/file-link.ts`).
  Una foto della galleria con un link non sicuro resta al suo posto senza anteprima (icona delle immagini) ma con la X:
  conta nei 30 posti e si può cancellare. Lo stesso vale per l'immagine principale (icona delle immagini, senza testo)
  e per un PDF (nome del file senza "Apri il documento"): presenti, con la X, si cancellano o si sostituiscono
  caricandone un altro. Un link `null` vuol dire file assente.
- La CSP pubblicata (`docs/README_DEPLOY.md`) accetta immagini solo dal nostro dominio, da Google e `data:`: le foto e
  le icone dei servizi su un altro dominio richiedono quel dominio in `img-src` (vedi `docs/SECURITY_TODO.md`).
- Modifica, Annulla e Salva valgono per il link del tour virtuale e per la posizione: form Signal Forms su
  `BathhouseMediaRequest` (`webLink` sul link: completo, http o https; Salva spento finché non è valido). Il link ha al
  massimo 500 caratteri, come il backend e il database (`MAX_VIRTUAL_TOUR_URL_LENGTH`, `MAX_LENGTH`: il campo non
  accetta caratteri oltre).
- In Modifica un clic sulla mappa sposta il segnaposto (`placePosition` su `(positionClicked)`, coordinate arrotondate
  a 6 decimali come le tiene il backend); in sola lettura il clic non fa nulla.
- Salva manda `virtualTourUrl` (senza spazi ai lati, vuoto = `null`), `latitude` e `longitude`. Gli errori del backend
  compaiono sotto il campo (link) o sotto la mappa (coordinate), gli altri in fondo alla tab.

## Cache della pagina

- `BathhouseConfigurationCacheBehaviour` (`behaviours/settings/`) sta nei `providers` di `BathhouseConfiguration`: le tab
  ne condividono una sola istanza, che muore con la pagina.
- Le tre GET e le nazioni si chiamano la prima volta che una tab le chiede; tornando sulla tab si riusa la risposta.
  Uscendo dalla pagina e rientrando si richiamano. Una chiamata fallita non resta in cache: tornando sulla tab si
  riprova.
- Le risposte valgono per un valore di `DATA_RELOAD` (la lingua): quando cambia, la cache le scarta e la tab aperta
  rilegge, come le altre pagine.
- Dopo un salvataggio, e dopo ogni caricamento o cancellazione di foto e PDF, la cache prende i dati salvati
  (`keepGeneralInformation`, `keepServices`, `keepMedia`) con la versione della tab: quella nuova dopo un salvataggio,
  quella di prima dopo foto e PDF. Tornando sulla tab si vedono senza richiamare la GET.
- Dopo un 409 `resource.version_conflict` la cache scarta la risposta della tab (`forgetGeneralInformation`,
  `forgetServices`, `forgetMedia`) e la tab rilegge dal backend.
- La cache tiene i dati del server, non quelli scritti e non salvati: cambiando tab con modifiche in sospeso compare il
  popup delle modifiche non salvate.

## API

Tutte in `CatalogService`. Nessuna manda lo stabilimento, che il backend prende dal token: le tab aspettano solo
l'utente autenticato.

- `GET /api/catalog/property-info/bathhouse-configuration-general`: i campi della tab 1 e `version`
  (`BathhouseSelectedGeneralInformation`).
- `GET /api/catalog/lookups/countries`: le voci di Nazione, API del backend usata così com'è (aperta anche senza
  accesso). Risponde `Country` (`entities/shared/country.ts`): `code`, codice ISO 3166-1 alpha-2 (es. `DE`), e `name`,
  già tradotto dal backend nella lingua di `Accept-Language`; solo le nazioni attive, nell'ordine del backend
  (`catalog.countries.sort_order`, poi il codice).
- `POST /api/catalog/property-info/save-bathhouse-configuration-general`: Salva della tab 1, con tutti i campi.
- `GET /api/catalog/property-info/bathhouse-configuration-services`: `{ services, version }`, i servizi della tab 2
  (`BathhouseSelectedServices`).
- `POST /api/catalog/property-info/save-bathhouse-configuration-services`: Salva della tab 2, `{ services }` con tutti i
  servizi letti, ognuno con `isSelected`.
- `GET /api/catalog/property-info/bathhouse-configuration-media`: i media della tab 3 e `version`
  (`BathhouseSelectedMedia`).
- `POST /api/catalog/property-info/save-bathhouse-configuration-media`: Salva della tab 3 (`BathhouseMediaRequest`).
- `POST` / `DELETE /api/catalog/property-info/main-photo`: immagine principale.
- `POST /api/catalog/property-info/photos`, `DELETE /api/catalog/property-info/photos/{id}` (`id` = `idPhoto`): foto
  della galleria.
- `POST` / `DELETE /api/catalog/property-info/documents/{price-list|menu}` (enum `BathhouseDocument`): PDF del listino
  e del menù.
- I tre salvataggi mandano la versione letta nell'header `If-Match` e rispondono 204 senza corpo, con la nuova
  versione nell'header `ETag`; 409 `resource.version_conflict` se nel frattempo i dati sono cambiati. I caricamenti
  mandano il file in `multipart/form-data`, campo `file`; le API di foto e PDF rispondono 200 con la scheda.
- Le sei API `bathhouse-configuration-*` e il loro controllo di versione sono richiesti al backend in
  `shared documentation/Modifiche per il backend/impostazioni/Informazioni stabilimento - Frontend requests.md`, con
  le API esistenti che la pagina usa così come sono. In locale rispondono i mock di
  `services/mocks/catalog/catalog.mock.ts`.

## Comportamenti

- Durante la lettura la tab è bloccata (`inert`). Se la lettura fallisce il messaggio del `code` compare in fondo alla
  tab e i campi restano vuoti (la tab Servizi senza sezioni); tornando sulla tab si riprova.
- In tutte e tre le tab "Modifica" resta spento finché i dati della tab non sono arrivati (`isLoaded`, controllato anche
  in `edit()`): se la lettura fallisce non si può salvare una tab vuota sopra i dati veri.
- Durante il salvataggio Salva mostra lo spinner, Annulla è spento e i campi sono bloccati; se riesce, la tab torna in
  sola lettura con i valori salvati.
- Controllo di versione, una versione per tab: la GET della tab porta `version`, la tab la tiene e il suo Salva la
  rimanda nell'header `If-Match`; dopo un salvataggio riuscito tiene la nuova versione dell'`ETag` per il Salva
  successivo. Ogni versione copre solo quello che scrive il Salva della sua tab: salvare una tab non cambia le altre,
  e foto e PDF non cambiano la versione della tab Media.
- Con 409 `resource.version_conflict` (un altro utente ha cambiato i dati della tab dopo la lettura) compare
  `app-version-conflict-popup` e la tab torna in sola lettura e rilegge i suoi dati: l'utente rifà le modifiche. Il
  messaggio in fondo alla tab non compare; se la rilettura fallisce vale la regola delle letture qui sopra.
- Gli errori generali (es. 403 `catalog.property_required`, 404 `catalog.property_not_found`, 403 `auth.forbidden`)
  compaiono in fondo alla tab; quelli di foto e PDF sotto la loro sezione.
- Ogni tab chiede prima di perdere i dati scritti (`UnsavedChangesBehaviour`): uscendo dalla pagina, cambiando tab e
  premendo Annulla.
