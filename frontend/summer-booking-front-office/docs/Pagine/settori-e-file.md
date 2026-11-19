# Settori e file

Rotta `/settings/rows-and-sectors` · vista `views/settings/rows-and-sectors/` (`RowsAndSectors`,
`app-rows-and-sectors`); etichetta del menu `management.settings.menu.sectors`.

## Struttura

- Pagina con tabella come Impostazioni magazzino: `app-page-title` "Settori e file" con
  `GeneralIcon_CalendarFilled.svg`, poi `app-table`.
- Testi `management.settings.rows_and_sectors.*`.

## Tabella

- Ricerca, selezione per le azioni multiple, bottone "Aggiungi settore/fila", colonna Nome ordinabile, azioni di
  riga elimina, duplica e modifica.
- Si carica con `SectorService.sectorList` (`POST /api/catalog/sectors-list`, richiesta e risposta standard delle
  tabelle, righe `SectorItem` { idSector, sectorName }).
- È un'API nuova, chiesta al backend in
  `shared documentation/Modifiche per il backend/impostazioni/Settori e file - Frontend requests.md` (sezione 1); in
  locale rispondono i mock (`services/mocks/catalog/sectors.mock.ts`), per tutte le API della pagina.
- Errore di caricamento in `app-table-error-popup`.

## Azioni di riga e sui selezionati

- Elimina (di riga e sui selezionati): `app-delete-items-popup` (testi
  `management.settings.rows_and_sectors.delete.*`), poi:
  - cestino di riga: `SectorService.deleteSector` (`DELETE /api/catalog/sectors/{id}`, 204);
  - "Elimina selezione" (compare con almeno due righe scelte): `SectorService.deleteSectors`
    (`POST /api/catalog/sectors/bulk-delete` con `{ idSectors }`, tutto o niente, 204), API nuova chiesta al backend
    (sezione 2 del documento delle richieste).
  - Spinner a pagina intera, poi tabella ricaricata e righe tolte dalla selezione.
  - Un errore, per esempio 404 `catalog.sector_not_found` o 409 `catalog.sector_in_use`
    (`error.catalog.sector_in_use` con nome e utilizzi), va nel popup "Operazione non riuscita". Il 409 di
    `DELETE /api/catalog/sectors/{id}` non ha il nome del settore: la pagina aggiunge agli `args` il nome della riga
    (`withSectorName`); quello di `bulk-delete` lo ha già.
- Duplica (di riga e sui selezionati): `app-duplicate-items-popup` (testi
  `management.settings.rows_and_sectors.duplicate.*`), poi `SectorService.duplicateSectors`
  (`POST /api/catalog/sectors/bulk-duplicate` con `{ idSectors }`, tutto o niente, 204; i nomi delle copie li
  sceglie il backend), API nuova chiesta al backend (sezione 3 del documento delle richieste).
  - Spinner a pagina intera, poi tabella ricaricata; dopo "duplica selezionati" la selezione si svuota.

## Popup Aggiungi (`app-sector-add-popup`)

- In `components/management/settings/rows-and-sectors/sector-add-popup/`: `BasePopup` largo 25rem, titolo
  "Nome settore/fila"; testi `management.settings.rows_and_sectors.add_popup.*`.
- Un `app-filled-text-field` con segnaposto "Fila 1", obbligatorio con `requiredText`, al massimo 100 caratteri
  con `metadata(…, MAX_LENGTH, …)` (`[maxLength]` non si può legare con `[formField]`).
- Annulla e Salva a metà riga.
- Il form è un `SaveSectorRequest` { name }, la richiesta del backend.
- Salva chiama `SectorService.addSector` (`POST /api/catalog/sectors` con `{ name }`, 201 con il settore creato
  `Sector` { id, name, sortOrder }), con spinner e campo bloccato.
- Se riesce il popup si chiude e la tabella si ricarica. Un errore su `name` va sotto il campo, anche il 409
  `catalog.sector_name_taken` (nome già usato), che arriva senza `errors[]`; gli altri sopra i bottoni.
- Annulla e la X chiedono conferma se c'è un nome scritto.

## Popup Modifica (`app-sector-edit-popup`)

- In `components/management/settings/rows-and-sectors/sector-edit-popup/`: copia del popup di aggiunta, riempito
  con il nome della riga senza chiamate; testi `management.settings.rows_and_sectors.edit_popup.*`.
- Salva con il nome invariato chiude senza chiamate; altrimenti chiama `SectorService.updateSector`
  (`PUT /api/catalog/sectors/{id}` con l'`idSector` della riga nell'indirizzo e `{ name }`, 200 con il settore
  salvato `Sector`).
- Spinner, errori e domanda su Annulla come nel popup di aggiunta; 404 `catalog.sector_not_found` (settore eliminato
  nel frattempo) sopra i bottoni.
