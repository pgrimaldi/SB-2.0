# Routing e autenticazione

Router, pagine pubbliche e private, guard, layout del gestionale (header e menu) e sezione Impostazioni con il suo
pannello.

## Router

- Angular Router senza hash routing; le route stanno in `app.routes.ts`.
- I link di navigazione sono link HTML crawlable: mai navigazione importante con `div` o `span` cliccabili.
- Le route pubbliche si raggiungono direttamente da un URL stabile e leggibile (regole degli URL pubblici: vedi
  `seo.md`).

## Pagine pubbliche

- Sono localizzate con prefisso lingua: `/it/home`, `/en/home`, `/fr/home`, `/es/home`, `/de/home`, `/zh/home`,
  `/ar/home` (landing pubblica). `/` e `/home` reindirizzano a `/<lingua preferita>/home`.
- La lingua scelta si salva in `localStorage` (`sb.language`) tramite `LanguageBehaviour` (`behaviours/i18n/`); la
  lingua nell'URL ha sempre la precedenza.
- Un prefisso di lingua non supportato mostra la 404.

## Pagine private

- Le pagine private (area gestionale) non hanno prefisso lingua e usano la lingua salvata.
- Le route protette usano guard di autenticazione.
- L'area gestionale usa il CSR e non si indicizza: vedi `seo.md`.

## Guard

- Tutte le route del gestionale sono figlie di un'unica route protetta da `authGuard`, che carica
  `views/layout/layout.ts` (`Layout`).
- La home dell'hub ha `guestGuard`: chi ha già una sessione aperta (ripresa all'avvio con il refresh) va direttamente
  su `/beachmap`.

## Layout del gestionale

- `Layout` mostra `app-management-header` e `app-management-menu` (`components/management/`) e le pagine (es.
  `views/beachmap/`, `views/warehouse/`) nel suo `<router-outlet />`: le pagine non ripetono header e menu.
- Ogni pagina del gestionale ha il suo `<main>`.
- Menu: da 1376px in su è la colonna di icone a sinistra; sotto quella larghezza (720p e simili, tablet, smartphone) è
  la barra in basso con icona e nome di ogni pagina. Soglia: `$menu-bar` in `styles/_breakpoints.scss`.
- Ogni nuova pagina del gestionale va aggiunta tra i figli di questa route. La funzione CloudFront serve già tutte le
  pagine riservate e non va toccata: vedi "Regola SPA di CloudFront" in `git-e-deploy.md`.

## Header del gestionale

- Le date (`app-datepicker`), il bottone Oggi e la tendina della parte della giornata sono commentati, con i loro
  import e il test del bottone Oggi (`it.skip`).
- Resta solo la ricerca, larga 28rem e al centro dell'header (testo "Cerca...": vedi `tabelle.md`).
- `ManagementFiltersBehaviour` dà le scelte dell'header come `filters` (`datetimeFrom`, `datetimeTo`,
  `bookingDayType`), ma nessuna tabella lo usa: tutte mandano `filters: []`.
- L'ingranaggio dell'header apre la sezione Impostazioni.

## Sezione Impostazioni

- La route `settings` carica `views/settings/settings.ts` (`Settings`): a sinistra il pannello `app-settings-menu`
  (`components/management/settings/settings-menu/`, voci ricevute con `[links]`), accanto la pagina scelta nel suo
  `<router-outlet />`.
- L'elenco delle pagine è uno solo: `SETTINGS_PAGES` in `views/settings/settings-pages.ts` (percorso sotto `/settings`
  e chiave di traduzione `management.settings.menu.*`). Da lì vengono sia le voci del pannello sia le route figlie di
  `settings`.
- Ogni pagina sta in `views/settings/<pagina>/`. Una voce senza componente ha già la sua route, che mostra il
  contenuto vuoto; quando la pagina esiste si aggiunge alla sua voce:
  - `loadComponent` per una pagina semplice;
  - `loadChildren` per una pagina con rotte proprie (es. le tab), caricate insieme alla pagina dal suo
    `<pagina>.routes.ts`;
  - `subPages` per altre schermate della pagina fuori dal menu (es. il Cestino, `/settings/database/trash`).
- Il percorso può ripetere il nome della sezione: es. `views/settings/warehouse-setting/` (`WarehouseSetting`,
  `app-warehouse-setting`), indirizzo `/settings/warehouse-setting`.
- Cosa fa ogni pagina: vedi `docs/Pagine/`.

## Pannello del menu Impostazioni

- È largo quanto il menu del gestionale completamente aperto (9.375rem).
- Voci, spaziature e testo seguono il mockup della grafica: voci alte 2.25rem, 0.4375rem tra una voce e l'altra,
  0.5625rem sopra la prima, testo da 0.6875rem con 0.75rem di margine.
- Ogni voce sta su una riga e le 15 pagine stanno in una schermata da 1280×720.
- Il pannello non è allineato riga per riga con il menu del gestionale.
- Tra il menu del gestionale e il pannello c'è l'ombra dell'header proiettata a destra
  (`0.375rem 0 1.25rem rgb(18 45 87 / 18%)`), che il menu getta su ciò che ha accanto, pagina o pannello.
- Dove il menu è la barra in basso (sotto i 1376px) l'ombra non c'è e il pannello finisce sopra la barra (sottrae
  `--layout-bottom-bar-height`).
- La voce della pagina aperta e, dove c'è un mouse, quella sotto il puntatore hanno lo sfondo grigio del tema
  (`--color__tertiary__theme`).
- Sotto i 64rem (tablet e telefono) il pannello è una striscia sopra la pagina che scorre in orizzontale; ogni testo è
  centrato nello spazio riservato alla sua traduzione più lunga, come le etichette della barra in basso.
