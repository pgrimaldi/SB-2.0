# Tabelle

Pagine a tabella, `app-table` che si carica da sola, richiesta e risposta standard, parti e bottoni della tabella,
azioni di riga, errore di caricamento, misure e campi di ricerca.

## Pagina a tabella

- La pagina mette in cima `app-page-title` (vedi `componenti-e-wrapper.md`) e sotto `app-table`.
- Il layout di titolo e tabella è il mixin `table-page` di `src/app/styles/_table-page.scss`:
  `@include page.table-page(warehouse);` genera `.warehouse` (il `<main>`) e `.warehouse__title` (il suo
  `app-page-title`). Il mixin è dell'app, non della libreria.
- La pagina non mette margine sotto la tabella: il paginatore è l'ultima cosa della pagina.

## app-table si carica da sola

- La vista passa ad `app-table` solo `columns`, `params` (`{}`, oppure `null` finché l'utente non è collegato: la
  tabella aspetta), eventualmente `filters` e `load`: l'API del proprio service, passata così com'è (`[load]="warehouse.list"`).
- Il metodo del service passato a `load` è una proprietà a freccia, così mantiene `this` (vedi il commento in
  `WarehouseService.list`).
- Caricamento, pagina corrente, righe per pagina, ricerca, ordinamento, ritorno a pagina 1 ed errori li gestisce solo
  la tabella.
- Righe per pagina: 10 per tutte le tabelle; 20, 50 o 100 con il selettore del paginatore. Per un numero fisso senza
  selettore c'è `isPageSizeSelectorHidden`.
- Ricerca: `isSearchable`, inviata a `load` come `search`. In tutte le tabelle accetta al massimo 100 caratteri
  (`TABLE_SEARCH_MAX_LENGTH`, il limite delle liste del backend, `searchMaxLength` di `app-table`): il campo non accetta
  caratteri oltre e, arrivati al limite, sotto la barra compare "Al massimo 100 caratteri" (`table.search.max_length`,
  le stesse parole di `invalidate.max_length`). Ordinamento: colonne `sortable`, inviato come `sortField` e
  `sortDirection`.

## Richiesta e risposta standard

- Ogni tabella chiama una `POST` con lo stesso corpo `PageRequest` (`entities/pagination/page.ts`), anche quando non ha
  ricerca, filtri od ordinamento; nessuna interfaccia per la singola API.
- Campi: `page` da 1; `pageSize` 10 (predefinito), 20, 50 o 100; `search` con il testo cercato o `''`
  (il backend lo cerca in alcuni campi specifici); `sortField` con il campo da ordinare o `''`; `sortDirection`
  `Ascending` / `Descending`.
- `filters`: array di `{ field, value }` composto dalla pagina, `[]` se non ce ne sono. Un oggetto per campo, con
  `value` sempre array di stringhe anche per un solo valore (es. `{ field: FilterField.BookingTypology, value:
  ['Daily', 'SevenDays'] }`).
- I nomi dei campi dei filtri stanno solo nell'enum `FilterField` (`entities/enums/filter-field.ts`), con i nomi del
  backend: pagine, behaviour, mock e test usano il membro, mai la stringa. Un filtro nuovo aggiunge prima il suo membro.
- I valori dello stesso campo sono alternative; un campo senza scelte non si manda.
- Risposta `Page<T>`: `{ total, rows }`. La paginazione la fa il server.
- Contratti delle API, date UTC e nomi degli enum: vedi `api-e-mock.md`.

## Parti della tabella

- Due stili: quello di default e quello dell'app con `[useAppTheme]="true"`; le pagine usano quello di default. I due
  stili hanno le stesse dimensioni (`table.scss`): cambiano solo colori e dettagli, mai lo spazio occupato.
- La tabella base mostra tutte le sue parti già pronte; ogni tabella nasconde quelle che non le servono mettendo a
  `true` i flag (tutti `false` per default): `areMassiveActionsHidden` (caselle delle righe e, con più righe scelte,
  duplica ed elimina delle righe scelte) e `isCreateButtonHidden` (bottone crea accanto al titolo).
- Caselle delle righe senza duplica ed elimina: `[isSelectable]="true"`; le righe scelte con `[(selection)]`.
- Il titolo (`[title]`, sempre con il binding) e la sua icona, quella della sezione, si passano solo se servono.
- Le azioni della barra sopra le righe si scrivono dentro `<app-table>`: `appTableIconAction` (a icona, a sinistra) e
  `appTableTextAction` (con testo, a destra), ciascuna con il suo gestore nella pagina.
- Altri spazi dentro `<app-table>`: `appTableHeadingAction` (nella riga del titolo, prima della ricerca) e
  `appTableFilterBar` (una riga intera sotto la riga del titolo).
- La pagina decide cosa fare con gli eventi della tabella (`duplicateSelected`, `deleteSelected`, `create`…).

## Bottoni di riga

- I bottoni di ogni riga li definisce la pagina nel suo `.ts` e li passa con `[rowActions]` (`TableRowAction`: nome
  tradotto, icona e funzione di ciascuno); senza, la colonna "Azioni" non c'è.
- Per elimina, duplica e modifica si usa `standardRowActions` (`components/shared/tables/standard-row-actions.ts`),
  passando solo le funzioni dei bottoni che servono: nomi tradotti di `table.actions` e icone li mette la funzione,
  sempre in quell'ordine (es. `standardRowActions<Item>({ edit: (item) => … })` dà il solo "Modifica").
- Un secondo parametro facoltativo dice, bottone per bottone, su quali righe il bottone non c'è (`isHidden`, il suo
  spazio resta) o è spento (`isDisabled`, con il tooltip `disabledLabel`).
- L'interruttore di riga è `rowToggle`, prima dei bottoni: mostra sempre `isEnabled` della riga.

## Azioni di tabella

- Ogni azione di tabella (interruttore, duplica, elimina, di una riga o delle righe scelte) usa lo spinner a pagina
  intera: `app-page-spinner` copre la pagina fino alla risposta, anche se dura poco, e intanto un'altra azione non
  parte.
- Elimina e duplica chiedono conferma con `app-delete-items-popup` / `app-duplicate-items-popup`
  (`components/management/`): la pagina passa `translationKey` (il gruppo di testi), `action` (la funzione che chiama
  la sua API) e `pathIcon`. Appena confermato, il popup si chiude e parte lo spinner.
- `idItems` sono gli elementi da eliminare, di solito gli id delle righe; `app-delete-items-popup` accetta qualunque
  tipo e lo passa uguale ad `action` (es. Stagioni e periodi passa la `key` del periodo o gli intervalli con la `key`
  del loro periodo).
- Dopo la risposta la tabella si ricarica (`reload()`): le righe eliminate escono dalla selezione e dopo "duplica
  selezionati" la selezione si svuota. Dopo l'interruttore, riuscito o no, la tabella rilegge le righe e mostra lo
  stato del server.
- L'errore di un'azione compare in un popup, non nella tabella: vedi "Errori delle azioni" in `api-e-mock.md`.
- Se l'eliminazione fallisce, `app-delete-items-popup` mostra l'errore come sempre e lo passa anche alla pagina con
  `(failed)` (facoltativo): la pagina può ricaricare quando serve, es. Stagioni e periodi dopo un 404 (elemento già
  eliminato da qualcun altro) ricarica i periodi.

## Errore di caricamento

- Ogni pagina con `app-table` ascolta `(loadError)` e mostra `app-table-error-popup`
  (`components/shared/ui/dialogs/table-error-popup/`) con i testi `table.load_error.*` (titolo "Errore", testo,
  Chiudi, Riprova).
- Riprova chiama `reload()` della tabella (`<app-table #table …>` e `(retry)="table.reload()"`), che rifà la stessa
  richiesta mantenendo pagina, ricerca e ordinamento.
- Il popup è generico: titolo e testi arrivano già tradotti e i bottoni si nascondono con `isCloseButtonHidden` /
  `isRetryButtonHidden`.

## Misure e schermo

- Con 10 righe tutto deve stare in una schermata da 1280×720, anche con tutte le parti della tabella visibili (riga del
  titolo con ricerca e bottone crea, barra dei bottoni sulle righe scelte): per questo intestazione e righe sono alte
  2.75rem.
- Sotto i 1376px il menu del gestionale è una barra in basso: lì, a 1280×720, la pagina può scorrere e il paginatore
  resta sopra la barra.
- Da 1920px la pagina deve stare in una finestra reale di uno schermo 1080p (al netto della barra delle
  applicazioni e delle barre del browser restano circa 880-930px): intestazione e righe 3.5rem, testo 1rem;
  paginatore, ricerca e bottoni non cambiano. Si misura quindi anche con altezze minori della risoluzione.
- Con più righe dello schermo scorre la pagina e il paginatore resta fermo in fondo allo schermo.
- Se una pagina ha qualcosa di fisso in fondo, imposta `--table-paginator-offset` (il layout del gestionale lo fa già
  per la barra del menu in basso, insieme a `--layout-bottom-bar-height`).
- Ogni colonna ha almeno lo spazio della sua intestazione (titolo e freccia di ordinamento, lo calcola la tabella):
  quando non c'è spazio per tutte, la tabella scorre di lato al suo interno, mai la pagina.

## Campi di ricerca

- Il testo grigio di ogni campo di ricerca è "Cerca..." (en "Search...", fr "Rechercher...", es "Buscar...", de
  "Suchen...", zh "搜索...", ar "بحث..."), senza dire dove si cerca.
- Vale per le tabelle (`table.search.placeholder`), per la ricerca dell'header
  (`management.header.search.placeholder`) e per ogni nuovo campo di ricerca.
