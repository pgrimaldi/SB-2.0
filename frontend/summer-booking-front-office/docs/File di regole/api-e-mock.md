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
- Un codice di errore nuovo va aggiunto all'enum `ApiErrorCode`, nel catalogo di `API_CONTRACTS.docx`, nei mock e
  nelle traduzioni `error.` di ogni lingua.
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
- Le rotte delle API nuove stanno sempre sotto la radice che il backend ha già per quei dati (es.
  `/api/catalog/services`, `/api/catalog/table-services`, `/api/catalog/sectors`), anche quelle di elenco, eliminazione
  e duplicazione.

## Documenti delle richieste al backend

- Ogni documento `<Pagina> - Frontend requests.md` è **comprovato dal codice**: ogni frase su cosa fa oggi il backend
  corrisponde al suo codice (classi e metodi citati), e ogni frase su cosa manda, si aspetta o mostra il frontend
  corrisponde al codice del frontend. Prima di dichiararlo finito si verifica sul codice, mai a memoria.
- È scritto **per un agente che lavora sul backend**, che deve poterlo eseguire senza sbagliare: per ogni API rotta,
  metodo, permesso, corpo e risposta con tipi e nullabilità, validazioni con `code`, `field` e `args`, ordine dei
  controlli, modifiche al database con la migration, effetti (registro, sessioni), e una sezione Verifica con prove
  concrete. Niente alternative lasciate aperte, frasi vaghe o contraddizioni tra sezioni.
- All'utente si chiede solo ciò che cambia quello che il frontend manda, riceve o mostra. Le scelte interne del
  backend (dove salvare un dato, indici, valori di default delle colonne, blocchi, caricamento dei dati iniziali) le
  decide lo sviluppatore backend: il documento lo scrive in modo esplicito ("lo decide lo sviluppatore backend: l'agente
  glielo chiede prima di scrivere codice"), tenendo fisso il contratto dell'API.
- Un'API di salvataggio chiesta al backend riceve tutto il modello letto, con le modifiche dell'utente, e non tocca mai
  dati che mancano dal corpo o che la pagina non mostra (es. il salvataggio dei servizi di Informazioni stabilimento
  riceve tutti i servizi con `isSelected` vero o falso; le altre dotazioni restano): vedi "Mai salvare sopra dati non
  letti" in `form-e-validazioni.md`.
- I documenti del collega backend (`shared documentation/backend/` e tutto quello che sta in `backend/`) non si
  modificano mai: si leggono soltanto. Nostri sono i documenti del frontend, quelli delle API che generiamo e i
  documenti delle richieste.

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
  Anche ogni nuovo caricamento (dettaglio, elenco di una select…) dipende da `DATA_RELOAD`, e una cache di pagina
  (es. `BathhouseConfigurationCacheBehaviour`) scarta le sue risposte quando cambia.
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
- `toApiProblem` tiene solo `args` semplici (testi e numeri; un testo oltre 200 caratteri si taglia con "…"). Un elenco di testi o di numeri (es. i
  nomi dei listini di `pricing.period_in_use`, le aliquote di `catalog.vat_rate_not_allowed`) diventa un testo solo,
  separato da virgole: i primi 20, poi "…", tagliato con "…" oltre i 200 caratteri.
- **Il frontend riflette gli errori del backend**: per ogni API si leggono nel codice del backend tutti gli errori che
  può dare (validatori, `DomainException`, permessi) e ognuno va in `ApiErrorCode`, nelle 7 lingue, nei mock, nei
  contratti e collegato al punto giusto dello schermo (sotto il campo, sopra i bottoni, nell'avviso). Se il backend ha
  già un codice per una situazione si usa quello, mai uno diverso chiesto da noi. Un codice che arriva senza `errors[]`
  ma riguarda un campo va sotto quel campo con `apiFieldError` (es. `service-field-errors.ts` di Servizi).
- **Quello che il backend vuole obbligatorio o limita è obbligatorio e limitato anche nel form** (asterisco, errore sotto
  il campo, Salva spento): stessi obblighi, stessi valori minimi e massimi, stesse lunghezze. Un limite che l'utente
  decide di non mettere nel form (es. 50 varianti in Servizi Bar) si scrive nel documento della pagina, e il suo errore
  del backend si vede lo stesso.
- I codici del backend sono nell'enum `ApiErrorCode` di `entities/errors/api-error-codes.ts`, un membro per codice
  (es. `ValidationOutOfRange = 'validation.out_of_range'`), lo stesso catalogo di `API_CONTRACTS.docx`; ognuno ha la sua
  traduzione in `error.`. Componenti, mock e test usano sempre il membro (`ApiErrorCode.ValidationOutOfRange`), mai la
  stringa: se un codice cambia, si cambia solo nell'enum. `API_ERROR_CODES` è l'elenco ricavato dall'enum.
- Anche i mock rispondono in Problem Details (`problem()` in `services/mocks/errors/problem.mock.ts`) come
  `ProblemDetailsWriter` del backend: `title` uguale al `code`, `type` `https://errors.summerbooking/<code con / al posto
  dei punti>`, `args` solo quando ci sono, `errors[]` per i campi con il loro `args` (anche `{}`) e `traceId`, e solo
  con i codici del catalogo (`ApiErrorCode`).
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
- Fanno eccezione gli elenchi che il backend ha già (`GET /api/catalog/lookups/...`), usati così come sono con la loro
  entità: es. le nazioni, `GET /api/catalog/lookups/countries` → `Country` (`code`, `name`); la pagina fa le opzioni
  della tendina dai loro campi.
- Una sola tendina degli utenti per tutta l'app: `POST /api/identity/user-combobox`. Quali utenti restituire lo
  decide il backend.
- La risposta è un array di `ComboboxItem` (`entities/combobox/combobox-item.ts`, `{ id, value }`: `id` è quello che
  si salva, `value` il testo mostrato), che il componente trasforma in `SelectOption`.
- Il componente richiede l'elenco a ogni apertura del suo popup, pannello o tab; una risposta lenta di un'apertura
  precedente non sostituisce quella nuova. Gli elenchi `lookups` del backend (es. le nazioni) si leggono invece una volta
  per pagina e restano nella cache della pagina (es. `BathhouseConfigurationCacheBehaviour.countries()`).
- Ogni tendina che legge la sua lista da un'API usa la classe unica `DropdownList` (`behaviours/forms/dropdown-list.ts`),
  creata con la funzione che chiama l'API, `ErrorTextBehaviour` e, se serve, una funzione da eseguire dopo ogni
  risposta (es. scegliere la prima voce): `items()` le voci, `isRead()` se sono di una risposta, `loadError()` il testo dell'errore, `load()` la lettura,
  `retry()` la rilettura dopo un errore, `clear()` al reset, `destroy()` in `ngOnDestroy`. Mai segnali e subscription
  scritti a mano per una tendina.
- Se la lettura fallisce, sotto il campo compare il messaggio dell'errore (`[error]="<lista>.loadError() ?? ..."`,
  testo di `ErrorTextBehaviour.instant` dal `code`) e la tendina non mostra la sua dicitura (`placeholder` vuoto). Mai
  una lista vuota in silenzio.
- Dopo un errore, quando l'utente prova ad aprire la tendina (clic, Invio o Spazio: `(openRequest)` dei wrapper
  `app-filled-select`, `app-filled-multi-select`, `app-filled-icon-select`, legato a `<lista>.retry()`) la lista si
  rilegge: se riesce l'errore sparisce e compaiono le voci. Con la lista già letta non parte nessuna chiamata. Una
  tendina in sola lettura o disabilitata non rilegge.

## Caricamento di file

- Nelle gallerie e nelle aree media (foto, PDF) ogni file parte appena viene scelto o trascinato, con la sua API
  (`multipart/form-data`, campo `file`), senza aspettare un Salva.
- Più file scelti insieme partono uno alla volta, in fila, con `app-page-spinner` che blocca tutto finché non
  finiscono.
- Le aree di caricamento delle gallerie e dei media sono sempre attive, anche fuori dalla modalità Modifica. Un'area
  che fa parte di un form con Modifica (es. lo sfondo di Configura mappa) è attiva solo in Modifica.
- Ogni elemento caricato ha una piccola X in alto a destra che, dopo la conferma, chiama la sua API di cancellazione.
- Prima di mandare il file il frontend controlla tipo, file non vuoto e peso con `isUploadable`
  (`behaviours/files/uploadable-file.ts`), come il backend (e per una galleria il posto libero), con i codici di
  errore del backend. I mock usano la stessa regola.
- Tipi e pesi sono costanti condivise: `IMAGE_UPLOAD_TYPES` (png, jpeg, webp) e `MAX_IMAGE_UPLOAD_MEGABYTES` (5 MB,
  lo stesso limite per tutte le immagini) in `entities/shared/image-upload-types.ts`, `PDF_UPLOAD_TYPES` in
  `entities/shared/pdf-upload-types.ts`, con i tipi MIME dell'enum `UploadContentType`
  (`entities/enums/upload-content-type.ts`). Il peso massimo dei PDF sta nell'entità della pagina (es.
  `MAX_BATHHOUSE_PDF_MEGABYTES`). Gli stessi elenchi danno l'`accept` del campo che sceglie il file.
- Un form con un solo allegato manda invece il file insieme agli altri dati, con il suo Salva. Se il file ha una sua API
  che vuole l'id dell'elemento (es. l'immagine di un servizio), parte dopo i dati: la creazione risponde con il solo id
  (stringa JSON). Se il file fallisce i dati restano salvati: il form si chiude e un avviso dice perché.

## Id del database

- Il frontend non scrive id del backend (servizi, categorie, righe di catalogo…) in tabelle, mappe o condizioni.
- Se una scelta dipende da un dato del database (es. la sezione e l'ordine di un servizio), la dà il backend nella
  risposta e la tiene nel suo database; i valori iniziali si scrivono nelle richieste al backend della pagina.
