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
- Errore di caricamento in `app-table-error-popup`.

## Azioni di riga e sui selezionati

- Elimina (di riga e sui selezionati): `app-delete-items-popup` (testi
  `management.settings.rows_and_sectors.delete.*`), poi `SectorService.deleteSectorItems`
  (`POST /api/catalog/delete-sector-item` con `{ idProperty, idSectors }`, tutto o niente, 204).
  - Spinner a pagina intera, poi tabella ricaricata e righe tolte dalla selezione.
  - Un errore, per esempio 409 `catalog.sector_in_use` (`error.catalog.sector_in_use` con nome e utilizzi), va nel
    popup "Operazione non riuscita".
- Duplica (di riga e sui selezionati): `app-duplicate-items-popup` (testi
  `management.settings.rows_and_sectors.duplicate.*`), poi `SectorService.duplicateSectorItems`
  (`POST /api/catalog/duplicate-sector-item` con `{ idProperty, idSectors }`, tutto o niente, 204; i nomi delle
  copie li sceglie il backend).
  - Spinner a pagina intera, poi tabella ricaricata; dopo "duplica selezionati" la selezione si svuota.

## Popup Aggiungi (`app-sector-add-popup`)

- In `components/management/settings/rows-and-sectors/sector-add-popup/`: `BasePopup` largo 25rem, titolo
  "Nome settore/fila"; testi `management.settings.rows_and_sectors.add_popup.*`.
- Un `app-filled-text-field` con segnaposto "Fila 1", obbligatorio con `requiredText`, al massimo 100 caratteri
  con `metadata(…, MAX_LENGTH, …)` (`[maxLength]` non si può legare con `[formField]`).
- Annulla e Salva a metà riga.
- Salva chiama `SectorService.addSectorItem` (`POST /api/catalog/add-sector-item` con `{ idProperty, sectorName }`,
  204), con spinner e campo bloccato.
- Se riesce il popup si chiude e la tabella si ricarica. Un errore su `sectorName` va sotto il campo, gli altri
  (es. 409 `catalog.sector_name_taken`) sopra i bottoni.
- Annulla e la X chiedono conferma se c'è un nome scritto.

## Popup Modifica (`app-sector-edit-popup`)

- In `components/management/settings/rows-and-sectors/sector-edit-popup/`: copia del popup di aggiunta, riempito
  con il nome della riga senza chiamate; testi `management.settings.rows_and_sectors.edit_popup.*`.
- Salva con il nome invariato chiude senza chiamate; altrimenti chiama `SectorService.updateSectorItem`
  (`POST /api/catalog/update-sector-item` con la riga `SectorItem` e `idProperty`, 204).
- Spinner, errori e domanda su Annulla come nel popup di aggiunta.
