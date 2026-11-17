# Configura mappa

Rotta `/settings/map-configuration` · vista `views/settings/map-configuration/` (`MapConfiguration`,
`app-map-configuration`).

## Struttura

- Riquadri bianchi (`settings-panel`) larghi come Stagioni, uno per sezione, ognuno con il suo Modifica che
  diventa Annulla e Salva:
  - "Imposta sfondo" (`app-map-background-panel`);
  - "Imposta reticolo" (`app-map-grid-panel`).
- I due pannelli sono in `components/management/settings/map-configuration/`.
- In fondo alla pagina, fuori dai riquadri e allineato alla fine, il bottone rosso "Reimposta mappa".
- Testi `management.settings.map_configuration.*`.

## Caricamento

- All'apertura la pagina fa una sola chiamata, `MapService.settings` (`GET /api/catalog/map/settings`, in un effetto
  con `DATA_RELOAD`), e passa la risposta (`MapSettings`) ai due riquadri con `[settings]` e l'eventuale errore con
  `[loadError]`.
- Finché la risposta manca Modifica è spento. Se la chiamata fallisce Modifica resta spento e il messaggio compare
  sopra i bottoni di entrambi i riquadri.
- Imposta sfondo sceglie la casella che ha il nome `background.name` (con un nome sconosciuto nessuna casella) o,
  con `isCustom`, mostra `background.url` come sfondo caricato.
- Imposta reticolo riempie i tre campi da `rows`, `columns` e `tiltDegrees` (se arrivano null, i valori di
  `DEFAULT_MAP_GRID`) e i bottoni delle icone con i nomi ricevuti; quelli degli articoli sono abbinati per
  `variant`, non per ordine.

## Icone sconosciute e X di svuotamento

- Un nome d'icona che quel bottone non offre (fuori da `choicesOf`) mostra nel bottone la X rossa
  `MapConfiguration_CloseRed.svg` e sotto "Icona non esistente" (`invalidate.unknown_icon`), anche in sola lettura.
  Salva resta spento finché non la si svuota o se ne sceglie un'altra.
- Ogni bottone con un'icona (anche sconosciuta) ha in alto a destra una piccola X rossa (`app-icon-button` con
  `MapConfiguration_CloseRed.svg`, cerchio bianco da 1.25rem con ombra, sopra l'angolo del bottone).
- La X svuota subito l'icona (`null`, conta come modifica, Annulla chiede); compare solo in Modifica ed è spenta
  durante il salvataggio.

## Imposta sfondo (`app-map-background-panel`)

- Titolo con l'icona `MapConfiguration_Palette.svg`.
- "Sfondo mappa": 4 caselle, una sola scelta (bordo blu e spunta `MapConfiguration_Chosen.svg`), una riga anche sul
  telefono.
  - Le prime due mostrano in piccolo `MapConfiguration_BackgroundBeach.png` e
    `MapConfiguration_BackgroundBoardwalk.png`; le altre due sono vuote, in attesa delle immagini, e non si possono
    scegliere.
  - Al passaggio del mouse (non sul tocco) aprono sotto la casella un'anteprima grande come una finestrella
    (40rem, cornice bianca con bordo scuro sottile, overlay CDK `cdkConnectedOverlay` che resta dentro lo schermo),
    anche in sola lettura.
- "Sfondo personalizzato": `app-file-drop-zone` (solo jpg, jpeg e png, `accept="image/jpeg,image/png"`) e
  l'anteprima dell'immagine scelta.
- L'immagine scelta in Modifica parte subito, senza Salva, con `MapService.uploadBackground`
  (`POST /api/catalog/map/background`, multipart con il campo `file`).
  - L'anteprima usa subito l'immagine già nel browser, senza riscaricarla, con `app-spinner` sopra e la sezione
    bloccata.
  - Se riesce la sezione torna da sola in sola lettura come dopo Salva; se fallisce resta in Modifica con
    l'anteprima di prima e il messaggio del codice d'errore sotto il riquadro (`map.invalid_image`).
  - Un file oltre 5 MB (`MAX_MAP_BACKGROUND_MEGABYTES`) non parte: sotto il riquadro "Il file supera i 5 MB"
    (`invalidate.max_file_size`).
- Sopra l'anteprima, in alto a destra (a sinistra in arabo), il cestino rosso in un cerchio bianco
  (`app-icon-button` con `MapConfiguration_DeleteRed.svg`) chiama subito `MapService.removeBackground`
  (`DELETE /api/catalog/map/background`), senza Salva né popup di conferma; la sezione torna in sola lettura.
  È spento in sola lettura e durante le chiamate.
- Scegliere una casella toglie l'immagine caricata e viceversa (come il backend, dove lo sfondo del catalogo scarta
  quello caricato).
- In sola lettura caselle e caricamento sono spenti. Annulla con modifiche chiede conferma.
- Salva chiama `MapService.updateBackground` (`PUT /api/catalog/map/update-background`, corpo `{ backgroundName }`
  con il nome dell'enum `MapBackgroundImage`, 204) solo se la casella scelta è cambiata, altrimenti chiude la
  Modifica senza chiamate. Spinner sul bottone, sezione bloccata, errore sopra i bottoni.

## Imposta reticolo (`app-map-grid-panel`)

- Titolo con l'icona `MapConfiguration_Grid.svg`. Signal Forms su `MapGrid`
  (`entities/settings/map-configuration/map-grid.ts`).
- Tre `app-filled-number-field` affiancati (uno sotto l'altro sul telefono), tutti interi (`isDecimal` non
  impostato) e obbligatori:
  - Numero di file (`rowQuantity`, da 1 a 50);
  - Numero massimo articoli per fila (`maxRowItemQuantity`, da 1 a 80);
  - Inclinazione mappa (`tiltDegrees`, da -45 a 45 gradi; icona dell'angolo `MapConfiguration_Tilt.svg` dentro il
    campo, quarta posizione di `pathIcon` di `app-filled-number-field`; con `isSigned` accetta il segno meno).
- Valori iniziali `DEFAULT_MAP_GRID`: 1 fila, 1 articolo per fila, 0 gradi. Errori sotto i campi, Salva spento
  finché c'è un errore.
- Sotto, la riga con "Icone elementi" (stesso aspetto delle etichette dei campi, allineata al bordo delle righe;
  sul telefono da sola sopra Libero e Occupato) e le intestazioni Libero e Occupato.
- Poi le righe bordate Ombrellone e Articolo personalizzato 1-8, con i bottoni "Seleziona icona" per Libero e
  Occupato (sul telefono il nome sopra e i due bottoni sotto), spenti in sola lettura.
- Un errore del backend su `umbrellaIcons` compare sotto la riga Ombrellone, uno su `customArticleIcons` sotto le
  righe degli articoli personalizzati (`management.settings.map_configuration.grid.icons.invalid.*`); Salva resta
  spento finché non si sceglie un'altra icona della riga.

### Scelta delle icone

- In Modifica i bottoni aprono il popup condiviso `app-icon-picker-popup`: titolo "Seleziona icona", sotto es.
  "Ombrellone · Libero", griglia di caselle quadrate, l'icona scelta segnata; un clic sceglie e chiude.
- Ombrellone Libero: gli ombrelloni (`MapUmbrellaIcon`, `MapConfiguration_Umbrella1…13.svg`).
- Ombrellone Occupato: solo le postazioni miste rosse (`OCCUPIED_UMBRELLA_ICONS` del pannello: 3, 4, 9, 13, 16, 18,
  19, 32, 36), perché gli ombrelloni sono tutti verdi.
- Articoli personalizzati: le postazioni miste (`MapMixedSpotIcon`, `MapConfiguration_MixedSpot1…36.svg`).
- Gli enum stanno in `entities/enums/`. Il valore va in `MapGrid`: `umbrellaIcons` e `customArticleIcons`, ognuno
  `MapElementIcons` con `freeIcon` / `occupiedIcon`; per l'Ombrellone `occupiedIcon` è un `MapMixedSpotIcon`.
- L'icona scelta compare nel bottone prima di "Seleziona icona". La scelta conta come modifica (Annulla chiede,
  Annulla la toglie, Salva la tiene).

### Salvataggio

- Modifica, Annulla (chiede se ci sono modifiche) e Salva come Imposta sfondo.
- Salva chiama `MapService.updateConfiguration` (`PUT /api/catalog/map/update-configuration`, 204) con tutto
  `MapGrid` in una sola chiamata, icone comprese (`customArticleIcons` sono 8 `MapArticleIcons` con il loro
  `variant`, che lega ogni icona alla sua riga anche nella GET).
- Passa da `submit()`: errori del backend sotto i campi, gli altri sopra i bottoni. Senza modifiche chiude la
  Modifica senza chiamate. Spinner sul bottone e campi bloccati durante la chiamata.

## Reimposta mappa

- `app-button` Secondary, rosso del tema `--color__layout__error` dato da `mat.button-overrides` nella pagina, icona
  `MapConfiguration_Reset.svg`; spento finché la GET non ha risposto.
- Apre l'avviso `app-warning-popup` (`components/shared/ui/dialogs/warning-popup/`, variante di
  `app-confirm-popup`): triangolo rosso `MapConfiguration_Warning.svg` e titolo rosso "Reimposta mappa" sulla
  stessa riga, testo su due paragrafi, largo 42rem; testi `management.settings.map_configuration.reset_popup.*`.
- Il testo dice cosa fa il reset: articoli della mappa eliminati, reticolo a 1 fila con 1 articolo e inclinazione
  0°; sfondo e icone restano.
- Bottoni a metà riga (uno sotto l'altro sul telefono): "Annulla e mantieni la mappa configurata" e, rosso,
  "Procedi ed elimina la mappa configurata".
- Procedi chiama `MapService.reset` (`POST /api/catalog/map/reset`, senza corpo, 200 con la mappa intera
  `MapLayout`, di cui il frontend legge solo `settings`), con lo spinner sul bottone e il popup non chiudibile.
- Se riesce il popup si chiude e la pagina passa le nuove `settings` ai due riquadri, senza un'altra GET (una
  Modifica in corso si chiude). Se fallisce il messaggio resta nel popup e sparisce riaprendolo.

## Icone

- Ombrelloni: `MapConfiguration_Umbrella1.svg` … `MapConfiguration_Umbrella13.svg` ("Icona ombrellone 1…13"),
  vettoriali: cerchio a gradini, ottagono a spicchi, cornice a puntini, +, carrozzina, "+1"/"+2".
- Postazioni miste: `MapConfiguration_MixedSpot1.svg` … `MapConfiguration_MixedSpot36.svg` ("Postazione mista
  1…36"): ombrellone di paglia, lettini, sedie, ombrelloni a stella, kayak, gazebo, cabine, ombrelloni a spicchi,
  gazebo e ombrellone di paglia nella cornice marrone, altre stelle colorate.
