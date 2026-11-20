# Settori e file

Rotta `/settings/rows-and-sectors` · vista `views/settings/rows-and-sectors/` (`RowsAndSectors`,
`app-rows-and-sectors`); etichetta del menu `management.settings.menu.sectors`.

## Struttura

- Pagina con tabella come Impostazioni magazzino: `app-page-title` "Settori e file" con
  `GeneralIcon_CalendarFilled.svg`, poi `app-table`.
- Testi `management.settings.rows_and_sectors.*`.

## Tabella

- Ricerca, selezione per le azioni multiple, bottone "Aggiungi settore/fila", colonna Nome ordinabile in ordine
  naturale (Fila 2 prima di Fila 10, chiesto al backend nella sezione 1 del documento delle richieste; il mock fa lo
  stesso), azioni di riga elimina, duplica e modifica.
- Si carica con `SectorService.sectorList` (`POST /api/catalog/sectors-list`, richiesta e risposta standard delle
  tabelle, righe `SectorItem` { idSector, sectorName, version }: `version` è la versione del settore, che il popup
  Modifica rimanda con il Salva).
- È un'API nuova, chiesta al backend in
  `shared documentation/Modifiche per il backend/impostazioni/Settori e file - Frontend requests.md` (sezione 1); in
  locale rispondono i mock (`services/mocks/catalog/sectors.mock.ts`), per tutte le API della pagina.
- Errore di caricamento in `app-table-error-popup`.

## Azioni di riga e sui selezionati

- Elimina (di riga e sui selezionati): `app-delete-items-popup` (testi
  `management.settings.rows_and_sectors.delete.*`), poi una sola API per tutti e due: `SectorService.deleteSectors`
  (`POST /api/catalog/sectors/bulk-delete` con `{ idSectors }`: l'id della riga per il cestino, tutti gli id scelti
  per "Elimina selezione", che compare con almeno due righe scelte; tutto o niente, 204), API nuova chiesta al
  backend (sezione 2 del documento delle richieste).
  - Spinner a pagina intera, poi tabella ricaricata e righe tolte dalla selezione.
  - Un errore, per esempio 404 `catalog.sector_not_found` o 409 `catalog.sector_in_use`
    (`error.catalog.sector_in_use` con nome e utilizzi: `sectorName` arriva dal backend anche con un id solo), va nel
    popup "Operazione non riuscita".
  - Dopo `catalog.sector_not_found` (settore eliminato nel frattempo) la pagina ricarica la tabella e toglie dalla
    selezione gli id dell'azione (`reloadIfGone`): dopo "Elimina selezione" la selezione si svuota, perché con il tutto o
    niente non è stato eliminato nulla e non si sa quale id manca.
- Duplica (di riga e sui selezionati): `app-duplicate-items-popup` (testi
  `management.settings.rows_and_sectors.duplicate.*`), poi `SectorService.duplicateSectors`
  (`POST /api/catalog/sectors/bulk-duplicate` con `{ idSectors }`, tutto o niente, 204; i nomi delle copie li
  sceglie il backend), API nuova chiesta al backend (sezione 3 del documento delle richieste).
  - Spinner a pagina intera, poi tabella ricaricata; dopo "duplica selezionati" la selezione si svuota.
  - Un errore va nel popup "Operazione non riuscita"; dopo `catalog.sector_not_found` la pagina fa come per Elimina
    (`reloadIfGone` con `(failed)` del popup).

## Popup Aggiungi (`app-sector-add-popup`)

- In `components/management/settings/rows-and-sectors/sector-add-popup/`: `BasePopup` largo 25rem, titolo
  "Nome settore/fila"; testi `management.settings.rows_and_sectors.add_popup.*`.
- Un `app-filled-text-field` con segnaposto "Fila 1", obbligatorio con `requiredText`, al massimo 100 caratteri
  con `metadata(…, MAX_LENGTH, …)` (`[maxLength]` non si può legare con `[formField]`).
- Annulla e Salva a metà riga.
- Il form è un `Sector` { id, name, sortOrder }, il settore come lo restituisce il backend (parte da `new Sector()`).
- Salva chiama `SectorService.addSector(name)` (`POST /api/catalog/sectors` con `{ name }`, 201 con il settore creato
  `Sector`), con spinner e campo bloccato.
- Se riesce il popup si chiude e la tabella si ricarica. Un errore su `name` va sotto il campo, anche il 409
  `catalog.sector_name_taken` (nome già usato), che arriva senza `errors[]`; gli altri sopra i bottoni.
- Annulla e la X chiedono conferma se c'è un nome scritto (solo spazi non contano).

## Popup Modifica (`app-sector-edit-popup`)

- In `components/management/settings/rows-and-sectors/sector-edit-popup/`: copia del popup di aggiunta, riempito
  con la riga senza chiamate (`Sector` con `id` e `name` della riga); testi
  `management.settings.rows_and_sectors.edit_popup.*`.
- Il nome si confronta con quello della riga senza spazi ai lati (`sameText`), sia per la domanda su Annulla e la X sia
  per Salva: con il nome invariato Salva chiude senza chiamate; altrimenti chiama
  `SectorService.updateSector(idSector, name, version)` (`PUT /api/catalog/sectors/{id}` con l'`idSector` della riga
  nell'indirizzo, `{ name }` e la `version` della riga nell'header `If-Match`; 200 con il settore salvato `Sector` e
  la nuova versione nell'`ETag`).
- Spinner, errori e domanda su Annulla come nel popup di aggiunta; 404 `catalog.sector_not_found` (settore eliminato
  nel frattempo) sopra i bottoni, con il popup aperto: il popup passa ogni errore alla pagina con `(failed)` e la pagina
  ricarica la tabella e toglie la riga dalla selezione (`reloadIfGone`).
- Controllo di versione: con 409 `resource.version_conflict` (un altro utente ha cambiato il settore dopo la lettura
  della tabella) niente va sopra i bottoni. Non c'è una lettura del singolo settore, quindi la nuova lettura è quella
  della tabella: il popup si chiude, compare `app-version-conflict-popup` e la pagina ricarica la tabella
  (`reloadIfChanged`, con `(failed)`), con le righe come sono ora e le loro nuove versioni; l'utente riapre la matita e
  rifà la modifica.
