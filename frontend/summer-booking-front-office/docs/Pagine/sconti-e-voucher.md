# Sconti e voucher

Rotta `/settings/vouchers`, vista `views/settings/vouchers/` (`Vouchers`).

## Struttura

- Pagina tabella come Impostazioni magazzino (mixin `table-page`, senza riquadro): `app-page-title` "Sconti e voucher"
  con l'icona `Vouchers_Title.svg`, poi `app-table`.
- La tabella ha il bottone "Aggiungi sconto/voucher", le caselle con duplica ed elimina selezionati e le azioni di riga
  elimina, duplica e modifica (`standardRowActions`). Niente ricerca.

## Tabella

- Righe `VoucherItem` (`entities/settings/vouchers/`; `DiscountType` in `entities/enums/`).
- Colonne Sconto (il nome, o il codice se manca), Valido dal e Valido fino al (date UTC mostrate come giorno, vuote senza
  periodi), Tipo di sconto tradotto, Valore con % o € (`Intl.NumberFormat`). Senza un ordinamento scelto, per nome. I titoli
  delle due date sono lunghi quanto basta perché sul telefono la data si legga intera, in tutte le lingue.

## Elimina e duplica

- Di riga e sui selezionati, funzionano come in Impostazioni magazzino: gli stessi popup di conferma
  (`app-delete-items-popup`, `app-duplicate-items-popup`, con `translationKey="management.settings.vouchers.delete"` /
  `.duplicate`; il nome per una riga, il numero per la selezione).
- Dopo la conferma: spinner a pagina intera, tabella ricaricata, errore in "Operazione non riuscita" (icona
  `GeneralIcon_Warning.svg`).
- Elimina e duplica sono tutto o niente. Le copie hanno nome e codice con " copia", sono spente e mai usate.
- Con 404 `pricing.discount_not_found` (elemento già eliminato da un altro utente), su Elimina, su Duplica e su Modifica
  (lettura del pannello e Salva), gli id escono dalla selezione e la tabella si ricarica (`reloadIfGone`, uscita
  `failed` dei popup e del pannello di modifica).

## Popup "Aggiungi nuovo sconto/voucher"

- `app-voucher-add-item-panel` (`components/management/settings/vouchers/voucher-add-item-panel/`): un `BasePopup` largo
  `min(42.3125rem, 95vw)` che scorre oltre il 95% dell'altezza.
- Contenuto:
  - nome e codice dello sconto, affiancati (uno sotto l'altro se non c'è spazio);
  - riquadro "Periodi di validità" (`app-section-title` con "Aggiungi periodo"). Ogni periodo ha le righe `app-form-row`
    Periodo (Dal/al con `app-filled-date-field`, date UTC), Giorni della settimana (`app-filled-multi-select` con la
    casella "Tutti", giorni ISO 1-7, elenco condiviso `DAYS_OF_WEEK` in `entities/shared/days-of-week.ts`) e Fasce orarie
    facoltative (Dalle/alle con `app-filled-time-field`, `HH:mm`). "Aggiungi periodo" si spegne a 50 periodi, "Aggiungi
    fascia oraria" a 20 fasce dello stesso periodo;
  - riquadro "Opzioni": tipo, valore con % o €, applica a (sotto), numero massimo di utilizzi;
  - interruttore "Abilita sconto/voucher".
- Form `VoucherAddItemRequest` con i Signal Forms: nome (al massimo 120 caratteri) e codice (60) obbligatori; per ogni
  periodo Dal, al e almeno un giorno; in ogni fascia oraria i due orari obbligatori e "alle" dopo "dalle" (`timeRange`,
  errore sotto i due orari); valore obbligatorio, più di 0 con 2 decimali (fino a 100 la percentuale, fino a 100.000
  l'importo); utilizzi da 1 a 2.147.483.647, vuoto = illimitati (partono da 1).
- **Salva resta acceso anche con il form non valido**, un'eccezione alla regola dei form: premendolo si vedono tutti gli
  errori e il popup scorre fino al primo (`showFirstError`, `components/shared/forms/first-error.ts`), anche quando l'errore
  arriva dal backend sotto un campo. L'errore dei giorni della settimana compare appena si tolgono tutti.
- **Codice da digitare o sconto automatico** (`isCodeRequired`): il popup non lo mostra. Uno sconto nuovo ha sempre un
  codice da digitare; in modifica il valore letto torna uguale. Con il codice i periodi sono facoltativi (senza, vale
  sempre); per uno sconto automatico ne serve almeno uno.
- **Applica sconto/voucher a** (`app-voucher-scope`, `components/management/settings/vouchers/voucher-scope/`, nei due
  popup): Tutto, Solo alcune scelte, Tutto tranne alcune scelte (`DiscountScope`: `All`, `Custom`, `WithoutCustom`). Con
  le ultime due compaiono le tendine a scelta multipla Categorie (`DiscountServiceCategory`, con i nomi delle tab di
  Servizi e Listino) e Settori e file (`POST /api/catalog/sectors/sector-combobox`, letta la prima volta che compare in
  ogni apertura), con almeno una scelta fra le due. Con Tutto si salvano vuote (`withScope`), anche se prima erano
  scelte. Con 404 `catalog.sector_not_found` il messaggio compare sopra i bottoni e i settori si rileggono: quelli
  scelti che non ci sono più si vedono come "Settore/fila non più disponibile".
- Giorni della settimana e fasce orarie sono di ogni periodo. Il canale (gestionale e online) non viaggia.
- Salva chiama l'aggiunta con lo spinner sul bottone, poi chiude il popup e ricarica la tabella. Il 409
  `pricing.discount_code_taken` compare sotto il codice.
- Con dati scritti chiede prima di chiudere (`UnsavedChangesBehaviour`).
- Dal mockup vengono stile e posizioni, non le misure: bottoni, titoli, etichette e campi hanno quelle standard dei
  wrapper.

## Popup di modifica

- La matita apre `app-voucher-edit-item-panel` (`components/management/settings/vouchers/voucher-edit-item-panel/`) e
  gli passa l'`idVoucher` della riga.
- Il titolo è "Modifica sconto/voucher".
- All'apertura (effect su `open`, perché il componente nasce con la pagina e `ngOnInit` scatta una volta sola) legge lo
  sconto e riempie il form campo per campo. Durante il caricamento i campi sono bloccati e Salva è spento; se fallisce
  il popup si chiude e compare `app-read-error-popup` (il messaggio del `code`, Chiudi e Riprova, che riapre il popup e
  rilegge; per un 404 solo Chiudi).
- Le modifiche non salvate si confrontano con i dati caricati, senza guardare l'ordine di categorie e settori
  (`comparableVoucher`, `voucher-scope/comparable-voucher.ts`, nei due popup).
- Per il resto è una copia identica del popup di aggiunta (codice duplicato voluto, accorpamento nel TODO), con tutti i
  campi modificabili.
- Salva chiama l'aggiornamento con l'`idVoucher` della risposta della GET (non quello della riga), poi chiude il popup e
  ricarica la tabella. Il 422 `pricing.discount_max_uses_below_used` compare sotto il numero massimo di utilizzi.
- Controllo di versione: la lettura porta `version` (in `VoucherSelectedItem`) e il Salva la rimanda nell'header
  `If-Match`. Con 409 `resource.version_conflict` (un altro utente ha cambiato lo sconto dopo la lettura) compare
  `app-version-conflict-popup`, il pannello si chiude e la pagina ricarica la tabella (`reloadIfChanged`): per rifare
  le modifiche l'utente riapre la matita, che legge lo sconto com'è ora.

## Icone

`GeneralIcon_Add.svg`, `GeneralIcon_DuplicateSelected.svg`, `GeneralIcon_DeleteSelected.svg`, `GeneralIcon_Close.svg`
(X dei popup e del periodo), `GeneralIcon_Calendar.svg`, `GeneralIcon_Clock.svg`, `GeneralIcon_AddBlue.svg`,
`GeneralIcon_Delete.svg`.

## API

In `VoucherService` (lo stabilimento lo prende il backend dal token):

- `POST /api/catalog/discounts/voucher-list` (`voucherList`): corpo standard `PageRequest`, risposta `Page<VoucherItem>`.
- `POST /api/catalog/discounts/delete-voucher-item` (`deleteVoucherItems`): corpo `{ idVouchers }`, risposta 204.
- `POST /api/catalog/discounts/duplicate-voucher-item` (`duplicateVoucherItems`): corpo `{ idVouchers }`, risposta 204.
- `POST /api/catalog/discounts/add-voucher-item` (`addVoucherItem`): corpo = form, risposta 204.
- `GET /api/catalog/discounts/get-selected-item?idVoucher=…` (`selectedItem`): risposta `VoucherSelectedItem` con
  `version`.
- `POST /api/catalog/discounts/update-voucher-item` (`updateVoucherItem(request, version)`): corpo = form
  `VoucherUpdateItemRequest` e header `If-Match` con la versione letta; risposta 204 con l'header `ETag` della nuova
  versione (`updateVoucherItem` restituisce la versione); 409 `resource.version_conflict`.

Tutte e sei sono chieste al backend in
`shared documentation/Modifiche per il backend/impostazioni/Sconti e voucher - Frontend requests.md`.
