# Impostazioni magazzino

Rotta `/settings/warehouse-setting`, vista `views/settings/warehouse-setting/` (`WarehouseSetting`).

## Struttura

- Pagina tabella con il layout del mixin `table-page` di `styles/_table-page.scss`.
- Titolo "Impostazioni magazzino" (`management.settings.warehouse.title`) con l'icona del Magazzino
  (`GeneralIcon_Warehouse.svg`).
- `app-table` con tutte le parti visibili: ricerca, caselle con "Duplica selezione" ed "Elimina selezione", azioni di
  riga e il bottone "Aggiungi articolo" (`management.settings.warehouse.create`).

## Tabella

- Usa la stessa API del Magazzino (`warehouse/list`). Non usa i filtri dell'header: manda `filters: []`.
- Colonne Articoli, Totale e Soglia (`totalQuantity` e `thresholdQuantity`, intestazione
  `management.warehouse.table.threshold`).

## Popup "Aggiungi articolo"

- `app-form-warehouse-add-item-popup` (`components/management/settings/warehouse-setting/form-warehouse-add-item-popup/`,
  testi `management.settings.warehouse.form.*`).
- Campi:
  - Articolo: tendina `app-filled-select` con gli articoli dello stabilimento (solo id e nome), chiesti a ogni apertura.
    Ogni apertura annulla la richiesta della precedente, così una risposta vecchia in ritardo non sostituisce l'elenco
    nuovo;
  - Immettere la quantità (numero);
  - Soglia (numero, vuoto = `null`);
  - interruttore Abilita avviso di soglia: si accende solo con una soglia maggiore di 0, altrimenti è bloccato, e si
    spegne da solo se la soglia torna vuota o 0.
- Annulla e la X chiudono il popup. A ogni apertura il form è vuoto.
- Aggiungi funziona solo con un articolo scelto e una quantità maggiore di 0. Il corpo è il `new WarehouseItem()` riempito
  dall'utente più `idProperty`.
- Durante la chiamata il bottone mostra lo spinner (`[isLoading]` di `app-button`), i campi sono bloccati (`inert`), X e
  Annulla non fanno nulla ed Esc o il clic fuori non chiudono il popup.
- Se riesce il popup si chiude ed emette `added`, con cui la pagina ricarica la tabella (`(added)="table.reload()"`).
- Se fallisce resta aperto con i dati inseriti: l'errore di un campo va sotto quel campo, gli altri (403, 404, rete)
  sopra i bottoni (`submit()` + `apiFieldErrorsOrGeneral`).

## Popup "Modifica articolo"

- La matita di una riga apre `app-form-warehouse-edit-item-popup` (`form-warehouse-edit-item-popup/`, accanto all'altro):
  stesse misure, campi e regole, ma titolo "Modifica articolo" (`form.edit_title`) e bottone "Salva" (`form.save`).
- Si apre con i valori della riga (`[item]`: totale, soglia e `isThresholdWarningActive`, che `warehouse/list`
  restituisce per questo).
- L'articolo è mostrato ma bloccato (`[disabled]` di `app-filled-select`), senza chiamare `article-combobox`.
- Salva manda la riga `WarehouseItem` con i valori modificati più `idProperty`, con lo stesso spinner e la stessa
  gestione degli errori; se riesce emette `saved` e la pagina ricarica la tabella.
- I due popup sono separati per scelta, anche se molto codice è uguale, per poterli far evolvere ciascuno per conto suo.

## Elimina

- Il cestino di una riga ed "Elimina selezione" (più righe scelte, anche su pagine diverse) aprono
  `app-delete-items-popup` (`components/management/delete-items-popup/`), condiviso con altre pagine (es. Sconti e
  voucher, Gestione utenti).
- La pagina gli passa `translationKey` (il gruppo di testi con `cancel`, `confirm`, `one.title`, `one.text` con
  `{{name}}`, `many.title`, `many.text` con `{{count}}`), `action` (la funzione `(idProperty, idItems) =>
  Observable<void>` che chiama la sua API) e `pathIcon` (l'icona d'errore della pagina).
- Il popup usa `app-confirm-popup`: "Vuoi eliminare l'articolo Lettino?" per una riga, "Vuoi eliminare i 3 articoli
  selezionati?" per la selezione (testi `management.settings.warehouse.delete.*`).
- Elimina manda un id per la riga, tutti quelli scelti per la selezione: tutto o niente.
- Appena confermato il popup si chiude e la pagina è coperta da `app-page-spinner` fino alla risposta.
- Se riesce gli id cancellati escono dalla selezione della tabella (`[(selection)]="idSelectedItems"`) e la tabella si
  ricarica; se fallisce compare il popup d'errore (vedi `api-e-mock.md`, errori delle azioni).

## Duplica

- Il bottone duplica di una riga e "Duplica selezione" aprono `app-duplicate-items-popup`
  (`components/management/duplicate-items-popup/`, stessi input; conferma con il bottone blu, testi
  `management.settings.warehouse.duplicate.*`).
- Tutto o niente; il nome delle copie lo decide il backend. Stesso spinner a pagina intera e stesso popup d'errore.
- Dopo "Duplica selezione" la selezione si svuota, poi la tabella si ricarica.

## API

In `WarehouseService`:

- `POST /api/warehouse/list` (`list`): corpo standard delle tabelle, risposta `Page<WarehouseItem>`.
- `POST /api/warehouse/article-combobox` (`articleCombobox`): corpo `{ idProperty }`, gli articoli della tendina.
- `POST /api/warehouse/add-warehouse-item` (`addWarehouseItem(idProperty, item)`): `WarehouseItem` più `idProperty`.
- `POST /api/warehouse/edit-warehouse-item` (`editWarehouseItem(idProperty, item)`): `WarehouseItem` più `idProperty`.
- `POST /api/warehouse/delete-warehouse-item` (`deleteWarehouseItems`): corpo `{ idProperty, idItems }`.
- `POST /api/warehouse/duplicate-warehouse-item` (`duplicateWarehouseItems`): corpo `{ idProperty, idItems }`.
