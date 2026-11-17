# API e mock

Regole per le chiamate al backend: servizi e mock, ambienti, contratti, date e header, errori, file, combobox e id.

## Servizi e mock

- I componenti dipendono dai servizi API dell'app (`services/api/<dominio>/`), mai direttamente dai mock.
- Servizi e mock sono separati per dominio (i mock in `services/mocks/<dominio>/`) e hanno la stessa forma dei
  contratti reali.
- I mock entrano nell'app solo tramite interceptor, `services/mocks/mock-interceptors.ts`, senza server separati e
  senza flag a runtime per spegnerli.
- La configurazione `production` sostituisce quel file con `mock-interceptors.none.ts` (`fileReplacements` in
  `angular.json`): il codice dei mock (account di test, dati finti) non arriva mai nel JavaScript di produzione.
- I mock rispondono agli errori come il backend: vedi "Errori delle API".

## Dati dei mock

- Nessuna password leggibile: il mock tiene solo l'hash SHA-256 e lo confronta con quello della password digitata.
- Dal campione del database del backend si prende solo la **struttura** (tabelle, campi, significato), mai i valori:
  potrebbero essere dati di clienti reali. Tutti i valori sono inventati.
- Ogni riga appartiene a uno stabilimento e il mock restituisce solo quelle dello stabilimento richiesto, come il
  backend.

## Configurazioni di build e ambienti

- Configurazioni di `angular.json`: `development` (serve locale, mock attivi), `dev` (ambiente DEV pubblicato,
  ottimizzato, mock attivi), `production` (mock spenti) e `local-backend` (sviluppo contro il backend vero, mock
  spenti: vedi `test-e-verifiche.md`).
- La pipeline sceglie la configurazione con la variabile `BUILD_CONFIGURATION` del progetto CodeBuild (vedi
  `git-e-deploy.md`).
- I file `environment` (`environment.ts`, `environment.dev.ts`, `environment.prod.ts`) tengono URL del backend, flag e
  valori non sensibili di DEV e PROD.
- Secret nei file `environment` e chiave di Google Maps: vedi `sicurezza.md`.

## Contratti e documenti delle API

- I documenti condivisi con il backend stanno in `shared documentation/`, alla radice del repository, e sono
  versionati:
  - `API_CONTRACTS.docx`: tutte le API, con in fondo il catalogo di tutti i codici di errore, da far confermare al
    backend;
  - `AUTH_API_CONTRACTS.docx`: contratto breve di accesso, rinnovo e uscita (per ogni API nome, indirizzo, payload,
    risposta con i cookie, errori);
  - `API - Full list.md`: le API raggruppate per pagina, con rotta, payload e risposta in JSON e in tabella con i tipi;
  - `Modifiche per il backend/<area>/<Pagina> - Frontend requests.md`: le richieste al backend di una pagina, cioè
    cosa deve creare o cambiare perché la pagina funzioni (payload, risposta, regole, errori attesi).
- `docs/API_CONTRACTS.md` (versionato): tutte le API usate dal frontend, divise per sezione, con rotta, payload e
  risposta in JSON e in tabella.
- Per ogni API: payload e risposta con la tabella (nome, tipo, descrizione) e l'esempio JSON; un corpo vuoto si
  dichiara esplicitamente. Gli esempi di richiesta e risposta usano dati inventati e coerenti tra loro.
- Per ogni API c'è anche la tabella degli errori possibili (status, `code`, quando) con un esempio JSON dell'errore.
- I documenti si aggiornano tutti a ogni cambio di rotta, payload o risposta. A sviluppo finito resta solo
  `API_CONTRACTS.docx`.
- Un codice di errore nuovo va aggiunto in `API_ERROR_CODES`, nel catalogo di `API_CONTRACTS.docx`, nei mock e nelle
  traduzioni `error.` di ogni lingua.
- Richiesta standard delle tabelle (`PageRequest`, risposta `Page<T>`): vedi `tabelle.md`.
- Stabilimento: nessuna API manda `idProperty`, nemmeno tabelle e tendine: il backend prende lo stabilimento dal
  token. Una chiamata che deve aspettare l'utente collegato controlla `this.auth.user()?.propertyId`, senza mandarlo.
- I mock usano lo stabilimento dell'utente di prova (`DEMO_PROPERTY`).

## Collegamento con il backend

- Del backend interessa solo il collegamento: rotta, metodo, campi della richiesta e della risposta, status, codici
  di errore e controlli che fa su quello che riceve. Non come salva o elabora i dati.
- Confrontando una pagina con il backend, ogni differenza si decide con l'utente caso per caso: usare l'API del
  backend così com'è, adattando il frontend senza differenze per chi usa il gestionale, oppure chiedere una nuova
  API nel documento di richieste della pagina (`shared documentation/Modifiche per il backend/`).
- Le API usate così come sono stanno nella sezione "API esistenti usate così come sono" dello stesso documento.

## Date, orari e header

- Date verso e dal server sempre in ISO 8601 UTC.
- Un giorno senza ora (es. `validFrom` di un periodo, scelto con `app-filled-date-field`) viaggia come quel giorno a
  mezzanotte UTC (`2026-08-01T00:00:00.000Z`), senza conversione di fuso.
- Le API del backend che usano giorni senza ora (es. le date della stagione e i periodi tariffari) li mandano e li
  ricevono come `2026-08-01`: il service li converte da e verso la mezzanotte UTC, così le pagine lavorano sempre con
  lo stesso formato.
- Un orario senza data (es. le fasce orarie dei voucher, con `app-filled-time-field`) è il testo `HH:mm` sulle 24 ore
  (`09:00`, ore 00–23, minuti 00–59), mostrato sulle 24 ore in ogni lingua (`timeInput` / `timeOptionLabel` di
  `DATE_FORMATS`).
- I giorni della settimana sono numeri ISO 8601: 1 = lunedì … 7 = domenica.
- Due interceptor (`services/api/time-zone.interceptor.ts`, `services/api/language.interceptor.ts`) aggiungono, solo
  verso la nostra API, il fuso dell'utente (nome IANA, es. `Europe/Rome`) nell'header `X-Time-Zone` e la lingua scelta
  nell'app (`it`, `en`, `fr`, `es`, `de`, `zh`, `ar`, non quella del browser) nell'header `Accept-Language`.
- Il server può rispondere con dati tradotti, quindi ogni caricamento di dati del server si rifà quando cambia la
  lingua: chi carica dati legge il segnale `DATA_RELOAD` (`components/shared/ui/data/data-reload.ts`, fornito in
  `app.config.ts` con la lingua corrente), come fa `app-table` (ricarica la pagina corrente, senza tornare a pagina 1).
  Anche ogni nuovo caricamento (dettaglio, elenco di una select…) dipende da `DATA_RELOAD`.
- Le enum viaggiano con i nomi dei membri .NET (es. `FullDay`), tranne i ruoli degli utenti (`UserRole`), che usano i
  codici di `RoleCodes` del backend (es. `cashoperator`).
- Il prefisso del telefono viaggia senza `+` (es. `39`): `PhonePrefix` (`entities/enums/phone-prefix.ts`) dà il
  prefisso di ogni paese (codice ISO 3166-1 alpha-2) e si importa ovunque serva un prefisso con la bandiera.

## Errori delle API

- Il backend risponde con Problem Details (RFC 9457) e un `code` stabile (es. `auth.invalid_credentials`), che è
  **direttamente la chiave di traduzione** sotto `error.` (`error.auth.invalid_credentials`,
  `error.validation.end_before_start`), senza conversioni.
- La traduzione passa sempre da `ErrorTextBehaviour` (`behaviours/errors/`): `text(problem)` dà il messaggio,
  `key(problem)` la chiave da passare ad `appI18nText` (con `[params]="problem.args"`), `instant(code, args)` il testo
  per un `computed`. I codici non ancora tradotti mostrano `error.unknown`.
- `args` riempie i segnaposto: `{{name}}` nei testi semplici; `{date, date, long}`, `{amount, number, currency}`,
  `{count, plural, …}` per date, importi e plurali (vedi `traduzioni-e-lingue.md`).
- I codici del backend sono in `entities/errors/api-error-codes.ts` (`API_ERROR_CODES`, lo stesso catalogo di
  `API_CONTRACTS.docx`) e ognuno ha la sua traduzione in `error.`.
- Anche i mock rispondono in Problem Details (`problem()` in `services/mocks/errors/problem.mock.ts`), con `code`,
  `errors[]` per i campi e `traceId` come il backend, e solo con i codici del catalogo (`ApiErrorCode`).
- Ogni errore di una chiamata diventa un `ApiProblem` con `toApiProblem` (`services/api/errors/`); i campi inviati
  dal server si tengono solo se ben formati.
- Gli errori che non arrivano dal backend ricevono un codice del frontend (`FRONTEND_ERROR_CODES`):
  `network.unavailable`, `server.unavailable` (502/503/504 dell'infrastruttura), `server.unexpected`,
  `client.unexpected`.
- Gli errori controllati dal frontend hanno le chiavi sotto `invalidate.`: le regole generiche, usate in tutti i form,
  direttamente sotto (`invalidate.greater_than`, "Il valore {{field}} deve essere superiore a {{limit}}"), quelle
  specifiche nel gruppo della loro pagina e del loro campo (es. `invalidate.email_configuration.smtp_password.retype`).
- `title` e `type` dell'errore non si mostrano mai all'utente.
- Gli errori dei campi (`errors[]`) vanno sotto il loro campo: vedi `form-e-validazioni.md`.

## Errori delle azioni

- L'errore di una chiamata partita da una tabella (interruttore, duplica, elimina… di una o più righe) o da un bottone
  che non salva un form (es. scaricare un file) compare in `app-alert-popup`: icona di avviso della pagina, titolo
  "Operazione non riuscita" (`alert.error_title`), il messaggio del `code` (`ErrorTextBehaviour`) e "Ho capito"
  (`alert.close`).
- Gli errori dei form restano sotto il loro campo: vedi `form-e-validazioni.md`.
- Errore nel caricare una tabella (`app-table-error-popup`): vedi `tabelle.md`.

## API che restituiscono un file

- Il servizio chiede la risposta intera come file (`observe: 'response'`, `responseType: 'blob'`) e aggiunge
  `catchError(readBlobError)` (`services/api/errors/read-blob-error.ts`).
- Con `blob` Angular dà come file anche il corpo di un errore: `readBlobError` lo rilegge come JSON (al massimo 64 KB),
  così `toApiProblem` trova il `code` come per le altre API.
- Il file si salva con `FileDownloadBehaviour.save(risposta, nomeDiRiserva)` (`behaviours/files/`). Il nome viene da
  `Content-Disposition` (prima `filename*` in UTF-8, poi `filename`); senza nome si usa quello di riserva.
- Il nome del server non è affidabile e si ripulisce: solo l'ultima parte (niente cartelle come `../`), senza
  caratteri non validi, al massimo 150 caratteri.
- Se il backend sta su un dominio diverso dal frontend, deve esporre l'header
  (`Access-Control-Expose-Headers: Content-Disposition`): va scritto nelle richieste al backend della pagina.
- Il salvataggio passa da un link temporaneo con `download` e un indirizzo `blob:` revocato subito dopo, senza cambiare
  la CSP.

## Combobox

- Le API delle tendine sono `POST /api/<area>/<cosa>-combobox` senza corpo e senza paginazione.
  Unica eccezione `GET /api/catalog/countries-combobox`, che non dipende dallo stabilimento.
- Una sola tendina degli utenti per tutta l'app: `POST /api/identity/user-combobox`. Quali utenti restituire lo
  decide il backend.
- La risposta è un array di `ComboboxItem` (`entities/combobox/combobox-item.ts`, `{ id, value }`: `id` è quello che
  si salva, `value` il testo mostrato), che il componente trasforma in `SelectOption`.
- Il componente richiede l'elenco a ogni apertura del suo popup, pannello o tab; una risposta lenta di un'apertura
  precedente non sostituisce quella nuova.
- Se la lettura fallisce, sotto il campo compare il messaggio dell'errore (`[error]` del wrapper, testo di
  `ErrorTextBehaviour.instant` dal `code`, in un segnale `<lista>LoadError` del componente) e la tendina non mostra la
  sua dicitura (`placeholder` vuoto). Mai una lista vuota in silenzio. Il messaggio sparisce alla lettura successiva.

## Caricamento di file

- Nelle gallerie e nelle aree media (foto, PDF) ogni file parte appena viene scelto o trascinato, con la sua API
  (`multipart/form-data`, campo `file`), senza aspettare un Salva.
- Più file scelti insieme partono uno alla volta, in fila, con `app-page-spinner` che blocca tutto finché non
  finiscono.
- Le aree di caricamento sono sempre attive, anche fuori dalla modalità Modifica.
- Ogni elemento caricato ha una piccola X in alto a destra che, dopo la conferma, chiama la sua API di cancellazione.
- Prima di mandare il file il frontend controlla tipo e peso (e per una galleria il posto libero), con i codici di
  errore del backend.
- Un form con un solo allegato manda invece il file insieme agli altri dati, con il suo Salva.

## Id del database

- Il frontend non scrive id del backend (servizi, categorie, righe di catalogo…) in tabelle, mappe o condizioni.
- Se una scelta dipende da un dato del database (es. la sezione e l'ordine di un servizio), la dà il backend nella
  risposta e la tiene nel suo database; i valori iniziali si scrivono nelle richieste al backend della pagina.
