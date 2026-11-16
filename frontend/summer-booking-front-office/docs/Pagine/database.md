# Database

Rotta `/settings/database`, vista `views/settings/database/` (`Database`). Il Cestino è la stessa pagina sulla rotta
`/settings/database/trash`.

## Struttura

- Titolo della pagina (`app-page-title`, come le altre pagine con tabella) e, nella riga del titolo a destra, il bottone
  "Cestino" (blu scuro come gli altri bottoni blu, testo bianco, icona `GeneralIcon_DeleteWhite.svg`, alto quanto la
  ricerca), che apre la schermata Cestino.
- Sotto, `app-table` con la ricerca, le caselle delle righe (`[isSelectable]="true"` senza i bottoni duplica ed elimina,
  righe scelte in `idSelectedBookings`) e le colonne ordinabili Cliente, Tipologia prenotazione, Dal, al, Convenzioni,
  Stato.
- La ricerca cerca nel nome del cliente, in qualunque punto ("gia" trova Gianni Riva e Marco Giallo).
- La pagina scrive le date (giorni a mezzanotte UTC, mostrati in UTC nel formato della lingua) e traduce gli enum
  `ReservationKind` e `ReservationStatus` prima di darli alla tabella.
- Errore di caricamento in `app-table-error-popup`.

## Periodo

- A sinistra della ricerca, la scelta del periodo (`app-date-range-select`, nello spazio `appTableHeadingAction`).
- Voci: Tutte le date, Oggi, Questa settimana (da lunedì a domenica), Prossimi sette giorni (da lunedì a domenica della
  settimana dopo), Prossimo mese (tutto il mese successivo), Prossimi sei mesi (i sei mesi interi dal successivo),
  calcolate da `behaviours/dates/date-periods.ts`; oppure un intervallo dal calendario con "Conferma".
- Il periodo va in `filters` come `datetimeFrom` / `datetimeTo` (giorni UTC, `dateRangeFilters`); "Tutte le date" manda
  `[]`.
- La combobox è alta quanto la ricerca (1.625rem, 2.25rem da 1920px).

## Popup Filtri

- Bottone "Filtri" accanto al periodo (bianco con il bordo, come la combobox), che apre
  `app-existing-booking-filters-popup` (`components/management/settings/database/`): largo 32rem, in alto con lo sfondo
  scuro, X, Annulla e Applica.
- Quattro tendine a scelta multipla (`app-filled-multi-select`):
  - Tipologia prenotazioni (Giornaliera, Più giorni, 7 giorni, 14 giorni, Mensile, Stagionale);
  - Prenotazioni con convenzioni (Sì, No);
  - Strutture convenzionate, dall'API `affiliated-hotel-combobox`, chiesta a ogni apertura del popup annullando la
    richiesta precedente;
  - Stato pagamento (In attesa di pagamento, Pagato totalmente, Pagato parzialmente).
- Gli enum delle tendine fisse stanno in `entities/database/existing-booking-filters.ts`; il backend li controllerà.
- Applica manda in `filters`, insieme al periodo, un oggetto per ogni tendina con almeno una scelta, con i valori in un
  array (`bookingTypology`, `agreementBooking`, `idAffiliatedHotel` con gli id degli hotel, `paymentStatus`).
- Annulla e la X chiudono senza cambiare nulla; riaprendo il popup si ritrovano i filtri applicati.

## Etichette dei filtri

- Con almeno una scelta, sotto la riga della ricerca compare la riga delle etichette (`app-filter-chips` nello spazio
  `appTableFilterBar`): una per tendina usata, "Nome breve: valore" (Tipologia, Convenzioni, Strutture, Stato
  pagamento) o "Nome breve: Più opzioni" con più valori; testi lunghi con i puntini.
- La X di un'etichetta toglie le scelte di quella tendina.
- "Azzera filtri" azzera scelte del popup, periodo ("Tutte le date") e ricerca (`clearSearch()` della tabella), con un
  solo caricamento da pagina 1.
- Il bottone Filtri mostra nel pallino quante tendine sono usate.
- Con le etichette il titolo ha meno spazio sotto (0.5rem) e, da 1920px, la pagina meno spazio sopra (0.5rem): a 1080p
  con 10 righe la pagina finisce a 870px e resta tutta visibile, come senza etichette (856px).

## Cancella dati

- In fondo alla riga della ricerca, a destra, il bottone rosso "Cancella dati" (`app-button` nello spazio
  `appTableHeadingAction`, spinto a destra con `order` e `margin-inline-start: auto`; rosso `--color__layout__error`,
  alto quanto la ricerca).
- Alla sua sinistra "Annulla selezione" (`app-button` chiaro con il bordo blu), che svuota le righe scelte.
- Entrambi sono spenti e sbiaditi (rosso al 40% per "Cancella dati") finché non si sceglie almeno una riga.
- Il clic su "Cancella dati" apre `app-confirm-delete-booking-popup` (`components/management/settings/database/`), cioè
  `app-confirm-popup` con la X e il campo password (`app-filled-text-field`, `autocomplete="current-password"`,
  obbligatorio): titolo "Inserisci password per completare l'operazione", Annulla e Conferma (rosso).
- Conferma chiama lo spostamento nel cestino con lo spinner su Conferma e il popup bloccato. Password sbagliata sotto il
  campo (`auth.invalid_credentials` in `errors[]`), altri errori sopra i bottoni.
- Con 2xx il popup si chiude ed emette `confirmed`: la pagina svuota la selezione, ricarica la tabella e mostra
  `app-message-popup` senza titolo, "Gli elementi selezionati sono stati spostati nel cestino", con il solo bottone Ok.
- Chiudendo il popup la password si cancella.
- Nel popup c'è un campo nome utente nascosto e in sola lettura (`autocomplete="username"`, con l'email dell'utente
  collegato, mai inviato), come consiglia Chrome per i moduli di sola password: senza, il gestore delle password del
  browser scrive l'email nella ricerca della tabella. Per lo stesso motivo i campi di ricerca hanno
  `autocomplete="off"`.

## Cestino

- Rotta `/settings/database/trash`: la stessa pagina `Database` su una sua route (`subPages` di `SETTINGS_PAGES`, dato
  `isTrash`). Nel menu resta attiva la voce Database.
- Carica la tabella dall'API della lista del cestino (stesso corpo e stessa risposta della lista della pagina Database).
- Cambiano solo:
  - la riga del titolo: freccia che torna alla pagina Database, titolo "Cestino" in azzurro, bottone rosso "Svuota
    cestino";
  - i bottoni a destra della ricerca: "Annulla selezione", poi "Ripristina" ed "Elimina definitivamente" (azzurri con
    icona), tutti spenti e sbiaditi senza righe scelte.
- Periodo, Filtri, etichette e ricerca sono gli stessi della pagina Database.
- Le righe di entrambe le liste (`ExistingBookingItem`) hanno `isTrashed`: `false` nella lista delle prenotazioni, `true`
  in quella del cestino.

### Svuota cestino

- Apre prima la conferma (`app-confirm-popup` con la X, titolo rosso `isTitleDanger`, testo su due righe "Stai svuotando
  il cestino. / Tutti i dati saranno eliminati definitivamente. L'azione è irreversibile.", Annulla e Conferma rosso).
- Conferma chiude il popup e chiama lo svuotamento: spinner su tutta la pagina (`app-page-spinner`), poi la tabella si
  ricarica. Se riesce le righe scelte si svuotano; se fallisce l'errore va nel popup "Operazione non riuscita"
  (`app-alert-popup`, icona `GeneralIcon_Warning.svg`).
- È spento quando il cestino è vuoto (`isTrashEmpty`): totale 0 della lista del cestino letta senza ricerca né filtri,
  così una ricerca senza risultati non lo spegne. Dopo Ripristina o Elimina definitivamente riusciti la pagina rilegge
  il totale senza ricerca né filtri; dopo Svuota cestino riuscito lo spegne subito.

### Ripristina ed Elimina definitivamente

- Ripristina chiama il ripristino delle righe scelte: spinner su tutta la pagina, poi la tabella si ricarica. Se riesce
  le righe scelte si svuotano e le prenotazioni tornano nella pagina Database; se fallisce, il messaggio compare nel
  popup "Operazione non riuscita".
- Elimina definitivamente apre lo stesso popup di Svuota cestino (titolo "Elimina definitiva", testo "Stai eliminando
  definitivamente gli elementi selezionati.", Annulla e Conferma); Conferma chiama la cancellazione definitiva, con
  spinner, ricarica ed errori come Ripristina.

## Telefono

- Sotto 48rem la pagina ha solo 1rem sopra il titolo (invece dei 4.125rem delle pagine con tabella).
- Sotto il titolo tre righe intere: periodo (si allarga) con Filtri, la ricerca, "Annulla selezione" e "Cancella dati" a
  metà ciascuno.

## Da fare

- Il paginatore in alto.

## API

In `DatabaseService`:

- `POST /api/database/existing-booking-list` (`existingBookingList`): corpo `PageRequest` come tutte le tabelle,
  risposta `Page<ExistingBookingItem>`.
- `POST /api/database/trash-item-list` (`trashItemList`): stesso corpo e stessa risposta.
- `POST /api/database/affiliated-hotel-combobox` (`affiliatedHotelCombobox(idProperty)`): `ComboboxItem[]`.
- `POST /api/database/move-to-trash` (`moveToTrash`): corpo `{ idProperty, idBookings, password }`.
- `POST /api/database/empty-trash` (`emptyTrash(idProperty)`): corpo con il solo `idProperty`.
- `POST /api/database/restore-booking-item` (`restoreBookingItem`): corpo `RestoreBookingItemRequest` (`idProperty` e
  l'array `idBookings` delle righe scelte, `entities/database/`).
- `POST /api/database/delete-selected-item` (`deleteSelectedItem`): corpo `DeleteSelectedItemRequest`, stessi campi di
  Ripristina.
