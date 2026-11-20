# Impostazioni magazzino

Rotta `/settings/warehouse-setting`, vista `views/settings/warehouse-setting/` (`WarehouseSetting`).

## Struttura

- Pagina tabella con il layout del mixin `table-page` di `styles/_table-page.scss`.
- Titolo "Impostazioni magazzino" (`management.settings.warehouse.title`) con l'icona del Magazzino
  (`GeneralIcon_Warehouse.svg`).
- `app-table` con tutte le parti visibili: ricerca, caselle con "Duplica selezione" ed "Elimina selezione", azioni di
  riga e il bottone "Aggiungi articolo" (`management.settings.warehouse.create`).

## Tabella

- Righe da `POST /api/catalog/inventory-items-list` (`WarehouseService.inventoryItemsList`, righe
  `WarehouseSettingItem`), con la richiesta standard delle tabelle e `filters: []`. La pagina Magazzino usa invece
  `warehouse/list`, che ha anche la disponibilità.
- Senza un ordinamento scelto gli articoli sono per nome, in ordine naturale; ordinando per Soglia quelli senza soglia
  stanno sempre in fondo.
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
- Aggiungi funziona solo con un articolo scelto e una quantità maggiore di 0. **Somma la quantità** al totale
  dell'articolo scelto: non crea un articolo.
- Il popup non mostra soglia e avviso dell'articolo, quindi li cambia solo se l'utente li compila: il corpo è
  `WarehouseItemAddRequest` (`entities/warehouse/warehouse-item-add-request.ts`) con `thresholdQuantity` `null` se la
  soglia resta vuota e `isThresholdWarningActive` `null` se l'utente non tocca l'interruttore (`dirty()` del campo), e il
  backend lascia com'erano i valori `null`.
- Durante la chiamata il bottone mostra lo spinner (`[isLoading]` di `app-button`), i campi sono bloccati (`inert`), X e
  Annulla non fanno nulla ed Esc o il clic fuori non chiudono il popup.
- Se riesce il popup si chiude ed emette `added`, con cui la pagina ricarica la tabella (`(added)="table.reload()"`).
- Se fallisce resta aperto con i dati inseriti: l'errore di un campo va sotto quel campo, gli altri (403, 404, rete)
  sopra i bottoni (`submit()` + `apiFieldErrorsOrGeneral`).

## Popup "Modifica articolo"

- La matita di una riga apre `app-form-warehouse-edit-item-popup` (`form-warehouse-edit-item-popup/`, accanto all'altro):
  stesse misure, campi e regole, ma titolo "Modifica articolo" (`form.edit_title`) e bottone "Salva" (`form.save`).
- A ogni apertura legge l'articolo con `GET /api/catalog/inventory-items/{id}` (`selectedArticle`, con l'`idArticle`
  della riga `[item]`): finché la risposta non arriva i campi sono bloccati e Salva è spento; se la lettura fallisce il
  messaggio compare sopra i bottoni e Salva resta spento.
- Il form (`WarehouseItemStock`) parte da totale, soglia e avviso letti (`totalQuantity`, `lowStockThreshold`,
  `lowStockAlertEnabled`, l'avviso spento se la soglia non è maggiore di 0); codice e nome letti restano da parte.
- L'articolo è mostrato ma bloccato (`[disabled]` di `app-filled-select`, con il nome della riga), senza chiamare
  `article-combobox`.
- Salva chiama `PUT /api/catalog/edit-warehouse-item/{id}` (`updateArticle`), che sostituisce tutto l'articolo: manda
  `WarehouseItemUpdateRequest` con `articleCode` e `name` letti, invariati, e totale, soglia e avviso del popup. Stesso
  spinner e stessa gestione degli errori (sotto i campi quelli su `totalQuantity`, `lowStockThreshold`,
  `lowStockAlertEnabled`); se riesce emette `saved` e la pagina ricarica la tabella.
- Le modifiche non salvate si contano confrontando il form con i valori letti (`comparableJson`).
- I due popup sono separati per scelta, anche se molto codice è uguale, per poterli far evolvere ciascuno per conto suo.

## Elimina

- Il cestino di una riga ed "Elimina selezione" (più righe scelte, anche su pagine diverse) aprono
  `app-delete-items-popup` (`components/management/delete-items-popup/`), condiviso con altre pagine (es. Sconti e
  voucher, Gestione utenti).
- La pagina gli passa `translationKey` (il gruppo di testi con `cancel`, `confirm`, `one.title`, `one.text` con
  `{{name}}`, `many.title`, `many.text` con `{{count}}`), `action` (la funzione `(idItems) => Observable<void>`
  che chiama la sua API) e `pathIcon` (l'icona d'errore della pagina).
- Il popup usa `app-confirm-popup`: "Vuoi eliminare l'articolo Lettino?" per una riga, "Vuoi eliminare i 3 articoli
  selezionati?" per la selezione (testi `management.settings.warehouse.delete.*`).
- Una sola API per entrambi: `POST /api/catalog/delete-warehouse-item` (`deleteArticles`), con l'id della riga per il
  cestino e con tutti gli id scelti per "Elimina selezione", tutto o niente.
- Un articolo usato nei listini non si elimina: 409 `inventory.item_in_use` ("L'articolo Lettino è usato nei listini
  … e non si può eliminare…"), con il nome dell'articolo in `articleName`. La pagina aggiunge al messaggio `usedIn`, i
  nomi di `priceLists` e `agreementPriceLists` uniti (`withUsedIn`).
- Con 404 `inventory.item_not_found` (articolo già eliminato da un altro utente) gli id escono dalla selezione e la
  tabella si ricarica (`reloadIfGone`, uscita `failed` del popup).
- Appena confermato il popup si chiude e la pagina è coperta da `app-page-spinner` fino alla risposta.
- Se riesce gli id cancellati escono dalla selezione della tabella (`[(selection)]="idSelectedItems"`) e la tabella si
  ricarica; se fallisce compare il popup d'errore (vedi `api-e-mock.md`, errori delle azioni).

## Duplica

- Il bottone duplica di una riga e "Duplica selezione" aprono `app-duplicate-items-popup`
  (`components/management/duplicate-items-popup/`, stessi input; conferma con il bottone blu, testi
  `management.settings.warehouse.duplicate.*`).
- Una sola API per entrambi: `POST /api/catalog/duplicate-warehouse-item` (`duplicateWarehouseItems`), con l'id della
  riga per il bottone duplica e con tutti gli id scelti per "Duplica selezione", tutto o niente.
- Ogni copia ha lo stesso tipo di articolo, la stessa quantità totale, la stessa soglia e lo stesso avviso, e non è in
  nessun listino; il nome lo decide il backend ("Lettino copia", poi "Lettino copia 2"). Stesso spinner a pagina
  intera e stesso popup d'errore.
- Dopo "Duplica selezione" la selezione si svuota, poi la tabella si ricarica.
- Con 404 `inventory.item_not_found` (articolo già eliminato da un altro utente) gli id escono dalla selezione e la
  tabella si ricarica, come per Elimina (`reloadIfGone`, uscita `failed` del popup).

## API

In `WarehouseService` (lo stabilimento lo prende il backend dal token):

- `POST /api/catalog/inventory-items-list` (`inventoryItemsList`): corpo standard delle tabelle, risposta
  `Page<WarehouseSettingItem>` (`idArticle`, `name`, `totalQuantity`, `thresholdQuantity`).
  È un'API nuova, chiesta al backend in
  `shared documentation/Modifiche per il backend/impostazioni/Impostazioni magazzino - Frontend requests.md`
  (sezione 1); in locale risponde il mock.
- `POST /api/catalog/article-combobox` (`articleCombobox`): POST senza corpo, gli articoli della tendina in ordine
  naturale di nome (la usa anche il Listino). È un'API nuova, chiesta al backend in
  `shared documentation/Modifiche per il backend/impostazioni/Impostazioni magazzino - Frontend requests.md`
  (sezione 2); in locale risponde il mock (`services/mocks/warehouse/warehouse.mock.ts`).
- `POST /api/catalog/add-warehouse-item` (`addWarehouseItem(request)`): corpo `WarehouseItemAddRequest`
  (`idArticle`, `totalQuantity` da aggiungere, `thresholdQuantity` e `isThresholdWarningActive`, `null` = lascia com'è),
  risposta 204; 404 `inventory.item_not_found` per un articolo che non c'è più. È un'API nuova, chiesta al backend in
  `shared documentation/Modifiche per il backend/impostazioni/Impostazioni magazzino - Frontend requests.md`
  (sezione 3); in locale risponde il mock (`services/mocks/warehouse/warehouse.mock.ts`).
- `GET /api/catalog/inventory-items/{id}` (`selectedArticle(idArticle)`): risposta `WarehouseSelectedItem`
  (`InventoryItemDto` del backend); 404 `inventory.item_not_found`.
- `PUT /api/catalog/edit-warehouse-item/{id}` (`updateArticle(idArticle, request)`): corpo
  `WarehouseItemUpdateRequest` (`SaveInventoryItemRequest` del backend: tutto l'articolo), risposta 200 con l'articolo;
  404 `inventory.item_not_found`, 409 `inventory.name_taken`. È `PUT /api/catalog/inventory-items/{id}` del backend con
  un'altra rotta, chiesta in
  `shared documentation/Modifiche per il backend/impostazioni/Impostazioni magazzino - Frontend requests.md`
  (sezione 4); in locale risponde il mock. La GET invece è un'API del backend usata così com'è.
- `POST /api/catalog/delete-warehouse-item` (`deleteArticles(idItems)`): corpo `{ idItems }` (un id per il cestino di
  una riga, tutti quelli scelti per "Elimina selezione"), tutto o niente, 204; 404 `inventory.item_not_found`, 409
  `inventory.item_in_use` con `articleName`, `priceLists` e `agreementPriceLists`. È un'API nuova, chiesta al backend in
  `shared documentation/Modifiche per il backend/impostazioni/Impostazioni magazzino - Frontend requests.md`
  (sezione 5); in locale risponde il mock.
- `POST /api/catalog/duplicate-warehouse-item` (`duplicateWarehouseItems(idItems)`): corpo `{ idItems }` (un id per il
  bottone duplica di una riga, tutti quelli scelti per "Duplica selezione"), tutto o niente, 204; 404
  `inventory.item_not_found`. È un'API nuova, chiesta al backend in
  `shared documentation/Modifiche per il backend/impostazioni/Impostazioni magazzino - Frontend requests.md`
  (sezione 6); in locale risponde il mock.
