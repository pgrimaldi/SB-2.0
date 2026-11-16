# Sconti e voucher

Rotta `/settings/vouchers`, vista `views/settings/vouchers/` (`Vouchers`).

## Struttura

- Pagina tabella come Impostazioni magazzino (mixin `table-page`, senza riquadro): `app-page-title` "Sconti e voucher"
  con l'icona `Vouchers_Title.svg`, poi `app-table`.
- La tabella ha il bottone "Aggiungi sconto/voucher", le caselle con duplica ed elimina selezionati e le azioni di riga
  elimina, duplica e modifica (`standardRowActions`). Niente ricerca.

## Tabella

- Righe `VoucherItem` (`entities/settings/vouchers/`; `DiscountType` in `entities/enums/`).
- Colonne Sconto, Valido dal e al (date UTC mostrate come giorno), Tipo di sconto tradotto, Valore con % o €
  (`Intl.NumberFormat`).

## Elimina e duplica

- Di riga e sui selezionati, funzionano come in Impostazioni magazzino: gli stessi popup di conferma
  (`app-delete-items-popup`, `app-duplicate-items-popup`, con `translationKey="management.settings.vouchers.delete"` /
  `.duplicate`; il nome per una riga, il numero per la selezione).
- Dopo la conferma: spinner a pagina intera, tabella ricaricata, errore in "Operazione non riuscita" (icona
  `GeneralIcon_Warning.svg`).

## Popup "Aggiungi nuovo sconto/voucher"

- `app-voucher-add-item-panel` (`components/management/settings/vouchers/voucher-add-item-panel/`): un `BasePopup` largo
  `min(42.3125rem, 95vw)` che scorre oltre il 95% dell'altezza.
- Contenuto:
  - nome dello sconto;
  - riquadro "Periodi di validità" (`app-section-title` con "Aggiungi periodo"). Ogni periodo ha le righe `app-form-row`
    Periodo (Dal/al con `app-filled-date-field`, date UTC), Giorni della settimana (`app-filled-multi-select` con la
    casella "Tutti", giorni ISO 1-7, elenco condiviso `DAYS_OF_WEEK` in `entities/shared/days-of-week.ts`) e Fasce orarie
    facoltative (Dalle/alle con `app-filled-time-field`, `HH:mm`);
  - riquadro "Opzioni": tipo, valore con % o €, applica a, numero massimo di utilizzi;
  - interruttore "Abilita sconto/voucher".
- Form `VoucherAddItemRequest` con i Signal Forms: nome obbligatorio; almeno un periodo; per ogni periodo Dal, al e
  almeno un giorno; valore obbligatorio (percentuale intera da 1 a 100, importo con 2 decimali); utilizzi almeno 1.
- Salva chiama l'aggiunta con lo spinner sul bottone, poi chiude il popup e ricarica la tabella.
- Con dati scritti chiede prima di chiudere (`UnsavedChangesBehaviour`).
- Dal mockup vengono stile e posizioni, non le misure: bottoni, titoli, etichette e campi hanno quelle standard dei
  wrapper.

## Popup di modifica

- La matita apre `app-voucher-edit-item-panel` (`components/management/settings/vouchers/voucher-edit-item-panel/`) e
  gli passa l'`idVoucher` della riga.
- All'apertura (effect su `open`, perché il componente nasce con la pagina e `ngOnInit` scatta una volta sola) legge lo
  sconto e riempie il form campo per campo. Durante il caricamento i campi sono bloccati e Salva è spento; se fallisce
  l'errore compare sopra i bottoni.
- Le modifiche non salvate si confrontano con i dati caricati.
- Per il resto è una copia identica del popup di aggiunta (codice duplicato voluto, accorpamento nel TODO), con tutti i
  campi modificabili.
- Salva chiama l'aggiornamento con l'`idVoucher` della risposta della GET (non quello della riga), poi chiude il popup e
  ricarica la tabella.

## Icone

`GeneralIcon_Add.svg`, `GeneralIcon_DuplicateSelected.svg`, `GeneralIcon_DeleteSelected.svg`, `GeneralIcon_Close.svg`
(X dei popup e del periodo), `GeneralIcon_Calendar.svg`, `GeneralIcon_Clock.svg`, `GeneralIcon_AddBlue.svg`,
`GeneralIcon_Delete.svg`.

## API

In `VoucherService`:

- `POST /api/vouchers/voucher-list` (`voucherList`): corpo standard `PageRequest`, risposta `Page<VoucherItem>`.
- `POST /api/vouchers/delete-voucher-item` (`deleteVoucherItems`): corpo `DeleteVoucherItemsRequest` =
  `{ idProperty, idVouchers }`, risposta 204.
- `POST /api/vouchers/duplicate-voucher-item` (`duplicateVoucherItems`): corpo `DuplicateVoucherItemsRequest` =
  `{ idProperty, idVouchers }`, risposta 204.
- `POST /api/vouchers/add-voucher-item` (`addVoucherItem`): corpo = form + `idProperty`, utilizzi vuoti mandati come 1,
  risposta 204.
- `GET /api/vouchers/get-selected-item?idVoucher=…` (`selectedItem`): risposta `VoucherSelectedItem`.
- `POST /api/vouchers/update-voucher-item` (`updateVoucherItem`): corpo = form `VoucherUpdateItemRequest` + `idProperty`,
  utilizzi vuoti mandati come 1, risposta 204.
