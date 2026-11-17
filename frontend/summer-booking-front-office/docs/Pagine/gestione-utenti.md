# Gestione utenti

Rotta `/settings/user-management`, vista `views/settings/user-management/` (`UserManagement`). Sotto-pagina Log:
`/settings/user-management/logs`, vista `views/settings/user-management/user-management-logs/` (`UserManagementLogs`).

## Struttura

- Pagina tabella come Impostazioni magazzino (mixin `table-page`): `app-page-title` "Gestione Utenti", senza icona
  (manca quella della grafica), poi `app-table`.
- A destra del titolo, sopra "Aggiungi utente", il bottone "Vedi tutti i log" con la cartellina bianca
  `UserManagement_LogWhite.svg` (stesso disegno della "Log" di riga), che apre la schermata Log.

## Tabella

- Colonne Email, Cellulare (prefisso e numero) e Tipo di account, tutte ordinabili.
- Tipo di account è `role`, enum `UserRole` con i codici dei ruoli del personale di `RoleCodes` del backend (es.
  `cashoperator`), tradotto con `management.settings.user_management.role.*`.
- Barra di ricerca sotto il titolo (`isSearchable`, inviata come `search`); senza caselle.
- Azioni di riga:
  - "Log": bottone a parola (`TableRowAction.text`) con a sinistra la cartellina `UserManagement_Log.svg`; apre la
    schermata Log già filtrata su quell'utente.
  - Cestino e matita (`standardRowActions` con `delete` ed `edit`).
- Sulla riga del Titolare (`isOwner` = ruolo `superadmin`) il cestino non c'è e la matita è spenta con il tooltip "Il
  titolare non può essere modificato".

## Elimina

- Il cestino apre la conferma di `app-delete-items-popup` ("Vuoi eliminare l'utente <email>?", testi
  `management.settings.user_management.delete`).
- Poi chiama l'API di cancellazione con lo spinner a pagina intera e ricarica la tabella.
- Un errore va in "Operazione non riuscita" (es. `account.role_not_assignable` per il Titolare); un utente che non c'è
  più dà 404 `account.not_found`.

## Popup "Aggiungi nuovo utente"

- Bottone "Aggiungi utente" → `app-user-add-item-popup`
  (`components/management/settings/user-management/user-add-item-popup/`), form `CreateUserRequest`, titolo con
  `app-section-title`.
- Campi:
  - Email (obbligatoria) con la casella "Abilita" accanto (`emailIsContact`);
  - Prefisso: `app-filled-icon-select` largo 7rem con bandiera e prefisso. Voci di `PhonePrefixBehaviour`: tutti i paesi
    di `PhonePrefix`, l'Italia prima e già scelta, poi in ordine di nome nella lingua dell'app. Scrive in
    `phoneCountryCode` il prefisso senza `+` (es. `39`), mandato solo se c'è il numero;
  - Cellulare (`phone`): solo cifre e spazi, al massimo 32 caratteri, con la casella "Abilita" accanto
    (`phoneIsContact`). Le caselle "Abilita" restano sulla riga del campo anche con l'errore sotto il campo;
  - Password (obbligatoria), nascosta con l'occhietto (`autocomplete="new-password"`);
  - Tipo di account (`role`, obbligatorio con l'asterisco, `required`): i ruoli che il backend permette di assegnare dal
    pannello, quindi senza Titolare e Website admin.
- Accanto all'etichetta Password, la (i) (`app-icon-button` con `GeneralIcon_Info.svg` e `@include info.info-button`,
  nello spazio `appFilledTextFieldLabelAction`) apre `app-info-popup` con il titolo "Password" e le regole, una per riga:
  "Almeno 8 caratteri", "Almeno 1 Maiuscola", "Almeno 1 Minuscola", "Almeno 1 numero"
  (`management.settings.user_management.form.password_rules`).
- Le regole le controlla il form con `passwordRules(path.password, PASSWORD_MIN_LENGTH)` (costante 8 in
  `entities/settings/user-management/create-user-request.ts`, usata da entrambi i popup): errore sotto il campo e Salva
  spento finché non sono rispettate. Il backend chiede da 8 a 128 caratteri e una password non comune.
- "Salva" in basso a destra, spento finché email o password mancano: chiama la creazione con lo spinner sul bottone e i
  campi bloccati.
- Se riesce, il popup si chiude e la tabella si ricarica. Se fallisce, gli errori dei campi vanno sotto i campi (es.
  `validation.password_too_short`), gli altri sopra il bottone (es. `account.email_taken`, 422
  `account.role_not_assignable`).
- La password non resta in memoria dopo la chiusura: il form si svuota a ogni apertura e chiusura.
- Chiede prima di perdere i dati scritti (`UnsavedChangesBehaviour`).

## Popup "Modifica utente"

- La matita apre `app-user-edit-item-popup` (copia del popup di aggiunta, input `idUser`).
- All'apertura legge l'utente e riempie il form, con i campi bloccati e Salva spento durante la chiamata; se fallisce
  l'errore va sopra Salva (es. `account.role_not_assignable` per il Titolare, 404 `account.not_found`).
- Tipo di account obbligatorio come nell'aggiunta.
- La password non è obbligatoria: vuota = resta quella salvata (testo "Password impostata: scrivi per cambiarla"); se
  scritta vale `passwordRules`.
- Salva chiama l'aggiornamento, poi chiude e ricarica la tabella.
- Chiede prima di perdere le modifiche (confronto con i dati caricati).

## Schermata Log

- Sotto-pagina di `SETTINGS_PAGES` come il Cestino del Database. Freccia indietro e `app-page-title` "Log" con la
  cartellina `UserManagement_LogTitle.svg` come icona, nel blu del titolo di Gestione utenti.
- `app-table` con, sotto il titolo, la tendina delle date (`app-date-range-select`: Tutte le date, Oggi, Questa
  settimana, Ultimi sette giorni, Ultimo mese, Ultimi sei mesi; periodo in `filters` come `datetimeFrom` /
  `datetimeTo`), "Filtri", la ricerca ed "Esporta" e "Stampa" in fondo alla riga.
- Colonne, tutte ordinabili: ID prenotazione, ID utente, Nome completo, Postazione, Azione e Data e ora (ora locale).
  Azione è il codice del backend tradotto con `management.settings.user_management.logs.actions.<modulo>.<azione>`, o il
  codice stesso se manca la traduzione.

### Apertura da una riga

- "Log" di riga passa id ed email dell'utente nello stato della navigazione (`AuditLogUserState`,
  `{ logUser: { id, value } }`, mai nell'URL); la pagina Log li mette nel filtro Utente (chip "Utente: <email>",
  toglibile come gli altri).
- Appena risponde `user-combobox` (chiamata una volta all'apertura), l'email lascia il posto a nome e cognome
  dell'utente con lo stesso id, nella chip e nella tendina, senza ricaricare la tabella (`filters` cambia solo se cambiano
  i valori). Senza risposta resta l'email.
- Finché quell'utente resta nel filtro, la tendina Utente del popup Filtri contiene solo lui (`onlyUsers` di
  `app-audit-log-filters-popup`), senza chiamare di nuovo `user-combobox`. Tolta la chip, con "Azzera filtri" o togliendo
  la spunta, la tendina torna a mostrare tutti gli utenti.

### Popup Filtri

- `app-audit-log-filters-popup` (`components/management/settings/user-management/audit-log-filters-popup/`), copia di
  quello del Database: Azione e Utente a scelta multipla, Annulla / Applica. Largo 48rem, Azione più larga di Utente.
- Le azioni sono le 102 del catalogo `AUDIT_ACTIONS` (`components/management/settings/user-management/audit-actions.ts`):
  i codici che il backend scrive nel registro dello stabilimento (`AuditActionCodes`, categorie operatore e sicurezza:
  `identity.*`, `catalog.*`, `audit.*`), con un'icona `UserManagement_Action<Nome>.svg` o
  `GeneralIcon_CalendarFilled.svg` scelta per tipo di azione (aggiunta, modifica, eliminazione, duplicazione,
  ripristino, accesso…), nome tradotto in `logs.actions.<modulo>.<azione>` con l'icona prima (`SelectOption.icon`).
- Le azioni dei moduli che il backend non ha ancora (prenotazioni, pagamenti, cassa, ordini, database, prenotazioni dei
  tavoli: `booking.*`, `payment.*`, `cash.*`, `order.*`, `database.*`, `table_booking.*`) hanno già i loro testi nelle 7
  lingue ma non sono nel filtro: i codici sono chiesti al backend (sezione 9 del documento di richieste della pagina).
- L'elenco scorre, è largo quanto il nome più lungo e mai meno del campo, con ogni nome su una riga sola in ogni lingua
  (`isPanelSizedToOptions` di `app-filled-multi-select`).
- Gli utenti arrivano da `user-combobox` a ogni apertura. Azioni e utenti sono in ordine alfabetico nella lingua
  corrente.
- Le scelte (`AuditLogFilters`) vanno in `filters` come `actionCode` e `idUser` (un oggetto per campo, valori
  alternativi), compaiono come chip sotto la ricerca ("Azione: …", "Utente: …", "Più opzioni" con più scelte, X per
  toglierle) e come numero sul bottone Filtri, come nel Database.
- "Azzera filtri" (`app-filter-chips` nello spazio `appTableFilterBar`) compare con una chip, un periodo o una ricerca e
  svuota tutto: filtri del popup, `dateRange` a `null` e `table.clearSearch()`.

### Esporta e Stampa

- Chiamano le API di export e stampa con gli stessi filtri che la tabella sta mostrando (`AuditLogFiltersRequest`:
  `search`, `filters`, `sortField`, `sortDirection`, presi da `table.currentRequest()`, senza pagina), con lo spinner
  sul bottone e l'altro spento.
- Il file ricevuto si salva con `FileDownloadBehaviour` (nome da `Content-Disposition`, di riserva `log` / `log-print`).
  Aprire la stampa del browser richiederebbe `blob:` in `frame-src` della CSP.
- Qualunque errore mostra in "Operazione non riuscita" il testo fisso "Il servizio non è ancora implementato"
  (`management.settings.user_management.logs.not_implemented`), da riportare al messaggio del codice d'errore quando il
  backend avrà le API. Le due API non hanno mock.

## API

`IdentityService` (lo stabilimento lo prende il backend dal token):

- `POST /api/identity/users-list` (`usersList`): corpo standard delle tabelle, risposta `Page<UserItem>`.
- `POST /api/identity/create-user` (`createUser`): corpo `CreateUserRequest`; risposta 201 con l'utente creato
  (`UserSelectedItem`), che il popup non usa.
- `GET /api/identity/get-selected-user?idUser=…` (`selectedUser`): risposta `UserSelectedItem`, mai la password.
- `POST /api/identity/update-user` (`updateUser`): corpo `UpdateUserRequest`; risposta 200 con l'utente aggiornato
  (`UserSelectedItem`), che il popup non usa.
- `DELETE /api/identity/property-users/{id}` (`deleteUser(idUser)`): API del backend così com'è, id nell'indirizzo,
  risposta 204.
- `POST /api/identity/user-combobox` (`userCombobox`): POST senza corpo, gli utenti del filtro Utente.

`AuditService`:

- `POST /api/audit/entries-list` (`entriesList`): risposta `Page<AuditEntryItem>`.
- `POST /api/audit/export-log` (`exportLog`) e `POST /api/audit/print-log` (`printLog`): corpo
  `AuditLogFiltersRequest`, risposta un file.
