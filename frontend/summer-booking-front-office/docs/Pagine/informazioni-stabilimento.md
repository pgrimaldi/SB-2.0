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
- Ogni tab salva da sé, con il suo Modifica / Annulla / Salva e la sua API. Il bottone "Modifica" è piccolo e largo 8rem.

## Tab

- Tre tab: "Informazioni Generali", "Servizi", "Media"
  (`management.settings.bathhouse_configuration.tabs.general_information`, `.services`, `.media`), larghe 34%, 33% e
  33% della barra (`widthPercent`).
- Una tab non diventa mai più stretta del suo nome: se i nomi non stanno, le tab si allargano e Material mostra le frecce.
- Sui telefoni (`width < 48rem`) le tab diventano una tendina che occupa tutta l'intestazione del riquadro.

## Tab 1 – Informazioni Generali

- Componente `BathhouseGeneralSettings`, modello `BathhouseGeneralInformation` in
  `entities/settings/bathhouse-configuration/` (una entity per tab).
- Campi bianchi con l'etichetta sopra, su righe:
  - Nome stabilimento 35% (obbligatorio, `requiredText`, con l'asterisco), Indirizzo 45%, Cap 20%;
  - Città, Provincia, Regione e Nazione un quarto ciascuno;
  - Telefono, Whatsapp ed Email un terzo ciascuno (i due numeri con `isPhoneField` e `phoneNumberWithoutPrefix`:
    solo cifre e spazi, al massimo 32 caratteri, `MAX_BATHHOUSE_PHONE_LENGTH`, come il backend, che non ha un prefisso
    per questi numeri);
  - Sito web, Facebook e Instagram un terzo ciascuno;
  - Descrizione su tutta la riga;
  - Orario di apertura (due `app-filled-time-field` "Dalle" e "Alle", `HH:mm`, con `strictTimeRange`: "Alle" dopo "Dalle",
    come vuole il backend), Cancellazione e Animali, un terzo ciascuno;
  - Regolamento su tutta la riga.
- Lunghezze massime come il backend e il database, costanti in `BathhouseGeneralInformation` (`MAX_LENGTH`, il campo
  non accetta caratteri oltre): 255 per Nome stabilimento, Indirizzo, Città, Provincia, Regione, Sito web, Facebook e
  Instagram (`MAX_BATHHOUSE_TEXT_LENGTH`); 16 per il Cap (`MAX_BATHHOUSE_POSTAL_CODE_LENGTH`); 255 per l'Email
  (`MAX_EMAIL_LENGTH`), che ha anche `emailAddress`.
- Descrizione e Regolamento: al massimo 5000 caratteri visibili (`MAX_BATHHOUSE_RICH_TEXT_LENGTH`, `maxRichTextLength`),
  più stretto dei 20.000 caratteri di HTML del backend; oltre il limite l'errore va sotto il campo e Salva si spegne.
- Descrizione e Regolamento usano `app-rich-text-field` (grassetto, corsivo, sottolineato, elenco puntato e numerato,
  link), alte 6.25rem (`--rich-text-field-height`) con lo scorrimento interno. Icone provvisorie `GeneralIcon_Bold.svg` …
  `GeneralIcon_TextLink.svg`, in attesa di quelle della grafica.
- Cancellazione: tendina con le scelte dell'enum `CancellationPolicy` (Gratuita entro 48 ore, Gratuita entro 24 ore,
  Nessuna). L'API usa invece `isFreeCancellation` e `cancellationNoticeHours`, come il backend, e la tab converte: la
  tendina usa `[value]` / `(valueChange)` e non `[formField]`; ore diverse lette dal backend lasciano la tendina vuota.
- Animali: tendina con l'enum `PetPolicy` (Ammessi, Non ammessi, Su richiesta); enum e non booleano perché le tendine
  dell'app hanno valori stringa. Non è mai vuota: parte da "Non ammessi" (`PetPolicy.NotAllowed`), il default del
  database e del backend, che non accetta `null`.
- Nazione: tendina con le nazioni del backend (`lookups/countries`), nel suo ordine; senza nazione parte dalla prima
  voce della lista. Il valore è il codice ISO (`code`), così ogni voce resta legata alla sua nazione (es. per una
  bandiera, `assets/flags/<code in minuscolo>.svg`).
- Le nazioni si caricano a parte dai dati della tab: se arrivano dopo, la prima voce si sceglie allora (solo se lo
  stabilimento non ne ha una e l'utente non l'ha scelta). Se non si caricano, l'errore compare solo sotto Nazione
  (`DropdownList`, come le altre tendine) e il resto della tab funziona. Dopo un errore, aprire la tendina in Modifica
  (clic, Invio o Spazio: `(openRequest)` di `app-filled-select`) rilegge le nazioni; con la lista già caricata non parte
  nessuna chiamata.
- Se il salvataggio risponde `catalog.country_not_found` (nazione non più nel catalogo), l'errore va sotto Nazione
  (`apiFieldError`) e sparisce appena si sceglie un'altra nazione.
- Sotto, a destra, "Modifica" sblocca i campi e lascia il posto ad Annulla e Salva. In tutte e tre le tab "Modifica"
  resta spento finché i dati della tab non sono arrivati (`isLoaded`): se il caricamento fallisce non si può salvare
  una tab vuota sopra i dati veri. Senza Nome stabilimento Salva
  è spento.
- Sui telefoni i campi vanno uno per riga, tranne Cap (spostato sotto Città) con Provincia e Regione con Nazione, a metà
  riga ciascuno (Città da sola, così anche i nomi lunghi delle nazioni non si tagliano). Le righe passano i loro campi a
  un'unica griglia di sei colonne (`display: contents`), senza ripetere il markup.
- I campi che il backend restituisce `null` restano vuoti, come un form da compilare; i mock partono con tutti i campi
  `null`.

## Tab 2 – Servizi

- Componente `BathhouseServicesSettings`. Mostra i servizi del backend come caselle di spunta in quattro sezioni fisse,
  sempre in quest'ordine: Accessibilità e Comfort, Sport e attività, Ristorazione e extra, Spiaggia (enum
  `BathhouseServiceGroup`).
- Ogni servizio va nella sezione del suo `categoryCode`, nell'ordine della risposta. Sezione e ordine li decide il
  backend: il frontend non ha tabelle di id.
- Modifica sblocca le caselle, Salva manda i servizi.

## Tab 3 – Media

- Componente `BathhouseMediaSettings`. Contiene la mappa della posizione, Immagini (immagine principale e galleria di al
  massimo 30 foto), il link del tour virtuale e Documenti PDF (Listino Prezzi e Menù).
- Mappa: `app-location-map`, segnaposto su `latitude` / `longitude` della GET con zoom 15; senza posizione l'Italia con
  zoom 5.
- Foto e PDF seguono lo standard dei caricamenti (vedi `api-e-mock.md`): le aree delle immagini e i bottoni "Carica"
  sono sempre attivi e ogni file scelto parte subito. Le API rispondono con tutta la scheda: la tab ne tiene i media e
  li passa alla cache (`keepMedia`).
- Un PDF presente mostra il nome del file (`priceListPdfFileName`, `menuPdfFileName`; un nome lungo si accorcia con i
  puntini) e "Apri il documento".
- Ogni foto caricata e ogni PDF presente hanno la piccola X in alto a destra (`app-icon-button`), che chiede conferma
  (`app-confirm-popup`, "Elimina immagine" / "Elimina documento") e poi chiama la cancellazione.
- Nelle anteprime vanno solo indirizzi https o dell'app. Una foto della galleria con l'url vuoto o non sicuro resta al
  suo posto senza anteprima (icona delle immagini) ma con la X: conta nei 30 posti e si può cancellare.
- Prima di mandare un file la tab controlla tipo, file non vuoto e peso (`isUploadable`: png, jpeg, webp fino a 5 MB;
  PDF fino a 10 MB) e il posto libero nella galleria, con gli stessi codici di errore del backend (`catalog.invalid_image`, `catalog.invalid_pdf`,
  `catalog.gallery_full`). Gli errori compaiono sotto Immagini o sotto Documenti PDF.
- Modifica, Annulla e Salva valgono per il link del tour virtuale e per la posizione: form Signal Forms su
  `BathhouseMediaRequest` (`webLink` sul link, Salva spento finché non è valido). Il link ha al massimo 500 caratteri,
  come il backend e il database (`MAX_VIRTUAL_TOUR_URL_LENGTH`, `MAX_LENGTH`: il campo non accetta caratteri oltre).
- In Modifica un clic sulla mappa sposta il segnaposto (`positionClicked`, coordinate arrotondate a 6 decimali come le
  tiene il backend); in sola lettura il clic non fa nulla.
- Salva manda `virtualTourUrl` (vuoto = `null`), `latitude` e `longitude`. Gli errori del backend compaiono sotto il
  campo (link) o sotto la mappa (coordinate), gli altri sotto la tab.

## Cache della pagina

- `BathhouseConfigurationCacheBehaviour` (`behaviours/settings/`) sta nei `providers` di `BathhouseConfiguration`: le tab
  ne condividono una sola istanza, che muore con la pagina.
- Ogni API si chiama la prima volta che una tab la chiede; tornando sulla tab si riusa la risposta. Uscendo dalla pagina
  e rientrando si richiama. Una chiamata fallita non resta in cache.
- La cache tiene i dati del server, non quelli scritti e non salvati: cambiando tab con modifiche in sospeso compare il
  popup delle modifiche non salvate.

## API

Tutte in `CatalogService`: lo stabilimento lo prende il backend dal token, e le tab aspettano solo l'utente
autenticato.

- `GET /api/catalog/property-info/bathhouse-configuration-general`: i campi della tab 1.
- `GET /api/catalog/lookups/countries`: le voci di Nazione, API del backend usata così com'è (aperta anche senza
  accesso). Risponde `Country` (`entities/shared/country.ts`): `code`, codice ISO 3166-1 alpha-2 (es. `DE`), e `name`,
  già tradotto dal backend nella lingua di `Accept-Language`; le voci arrivano nell'ordine del backend
  (`catalog.countries.sort_order`).
- `POST /api/catalog/property-info/save-bathhouse-configuration-general`: Salva della tab 1.
- `GET /api/catalog/property-info/bathhouse-configuration-services`: i servizi della tab 2.
- `POST /api/catalog/property-info/save-bathhouse-configuration-services`: Salva della tab 2.
- `GET /api/catalog/property-info/bathhouse-configuration-media`: i media della tab 3.
- `POST /api/catalog/property-info/save-bathhouse-configuration-media`: Salva della tab 3.
- `POST` / `DELETE /api/catalog/property-info/main-photo`: immagine principale.
- `POST /api/catalog/property-info/photos`, `DELETE /api/catalog/property-info/photos/{id}`: foto della galleria.
- `POST` / `DELETE /api/catalog/property-info/documents/{price-list|menu}`: PDF del listino e del menù.

## Comportamenti

- Durante il caricamento i campi sono bloccati; un errore di lettura compare sotto i campi.
- Ogni tab chiede prima di perdere i dati scritti (`UnsavedChangesBehaviour`).
