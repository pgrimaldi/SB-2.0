# Architettura

Ambito del progetto, dipendenze ammesse, nomi, cartelle per dominio, entità, services e behaviours, struttura delle
classi e commenti.

## Ambito

- Il progetto è solo il frontend Angular 22 in `frontend/summer-booking-front-office`.
- Non si creano backend, API server, database, migrazioni, progetti .NET o backend segnaposto.
- Se emerge una dipendenza dal backend, si descrive il contratto o la si segnala all'utente, senza implementare il
  backend.
- Credenziali, secret, chiavi AWS e informazioni sensibili non vanno mai nel frontend né nel repository: vedi
  `sicurezza.md`.
- La build non mette script o stili inline in `index.html` (`optimization.styles.inlineCritical: false` in
  `angular.json`), per la Content Security Policy: vedi `sicurezza.md`.

## Dipendenze

- Si resta su Angular 22 (supportato fino a giugno 2028: 12 mesi attivi e 12 di LTS). Si aggiorna solo dentro la 22,
  quando esce una correzione di sicurezza che ci riguarda.
- Niente `npm audit fix --force` né versioni maggiori degli strumenti.
- Nessuna libreria nuova senza il permesso dell'utente; una dipendenza si aggiunge solo se necessaria e da fonti
  ufficiali. Controllo delle dipendenze con `npm audit`: vedi `sicurezza.md`.
- Oltre allo stack di base (pacchetti Angular, Material compreso, RxJS e ngx-translate) sono ammesse solo queste
  librerie, ognuna usata in un solo modo:
  - **TipTap** (MIT, motore ProseMirror) per il testo formattato, perché Material non ha un editor: solo attraverso il
    wrapper `app-rich-text-field`. Solo i pacchetti ufficiali che servono, tutti alla stessa versione: `@tiptap/core`,
    `@tiptap/pm`, `@tiptap/extensions` e `@tiptap/extension-document`, `-paragraph`, `-text`, `-hard-break`, `-bold`,
    `-italic`, `-underline`, `-list`, `-link`. Niente StarterKit, che porta estensioni non usate. Non pesa sul
    caricamento iniziale: entra solo nel pacchetto della pagina che lo usa.
  - **`@angular/google-maps`** (pacchetto ufficiale del team Angular, MIT, porta con sé `@types/google.maps`) per le
    mappe, perché Material non le ha: solo attraverso il wrapper `app-location-map`. Lo script di Google lo carica una
    volta sola `GoogleMapsBehaviour` (`behaviours/maps/`); il caricamento iniziale dell'app non cambia. Chiave di Maps:
    vedi `sicurezza.md`.
  - **`flag-icons`** (MIT) per le bandiere del prefisso del telefono: solo le immagini SVG che la build copia in
    `assets/flags/`, mai il CSS della libreria. Dettagli: vedi `asset-e-icone.md`.
- Come sono fatti `app-rich-text-field` e `app-location-map`: vedi `componenti-e-wrapper.md`.

## Codice pulito

- Codice pulito e mantenibile è un requisito prioritario: il vecchio software si rifà perché disordinato e non
  mantenibile, e questo non deve diventarlo.
- Si seguono le convenzioni ufficiali (Angular style guide, HTML semantico, SCSS moderno). Minimo codice necessario,
  leggibile, senza duplicazioni, hack, `!important` o workaround non giustificati.
- Ogni eccezione (es. `::ng-deep` dentro un wrapper) è motivata da un commento.
- Componenti standalone, TypeScript strict, nomi chiari, componenti piccoli e focalizzati.
- Niente dipendenze, astrazioni o cartelle preventive senza una necessità reale.
- Una logica che appartiene a un dominio resta nel dominio: niente servizi globali monolitici né duplicazioni evidenti.

## Controlli del compilatore

- In `tsconfig.json` sono scritti in modo esplicito, anche quelli che TypeScript 6 e Angular 22 attivano già di
  default, perché chi legge la configurazione (persone e strumenti) li veda: `strict`, `noUnusedLocals`,
  `noUnusedParameters`.
- Per i template: `strictTemplates`, `typeCheckHostBindings` e la diagnostica estesa di Angular come errore
  (`extendedDiagnostics.defaultCategory: "error"`: es. una funzione non chiamata in un evento, un `??` inutile).
- Non si usano `noUncheckedIndexedAccess` né `exactOptionalPropertyTypes`.

## Nomi

- Tutti i selettori dei componenti usano il prefisso `app-` (es. `app-card`, `app-header`); il prefisso della futura
  libreria si deciderà quando verrà estratta.
- Nome della classe e selettore derivano sempre dal nome del file, senza prefissi aggiunti né suffisso `.component`:
  `login-dialog.ts` → `LoginDialog`, `app-login-dialog`; `button.ts` → `Button`; `auth.service.ts` → `AuthService`;
  `auth.behaviour.ts` → `AuthBehaviour`; direttiva `equal-height.ts` → `EqualHeight`, `[appEqualHeight]`.
- Unica eccezione: il componente radice `app.component.ts` (`AppComponent`, `app-root`).
- Una sola classe Angular per file (es. `carousel-slide.ts` separato da `carousel.ts`).
- La cartella di un componente ha lo stesso nome del suo file (`dialogs/alert-popup/alert-popup.ts`).
- Il nome di un wrapper descrive il componente, non la zona dell'app dove si usa (`app-card`, non `app-card-hub`).
- Un campo o una variabile che contiene un id comincia con `id` (`idArticle`, `idPeriod`, `idItems`); una quantità
  (totale, disponibili, soglia…) finisce con `Quantity` (`totalQuantity`, `availableQuantity`, `thresholdQuantity`).
  Vale anche per i campi delle API.
- Fanno eccezione i nomi delle colonne del database copiati nei mock (`publicId`, `propertyId`, `lowStockThreshold`),
  `total` di `Page`, che conta le righe, e `key` dei periodi tariffari (`SeasonPeriodItem`, `SeasonPeriodIntervalItem`),
  un numero che parte da 0, come nel backend; le entità che lo riferiscono (prezzi e disponibilità per periodo) lo
  chiamano `idPeriod`.
- La landing dell'hub sta in `src/app/views/hub/home/` con `home.ts`, `home.html` e `home.scss`: questo naming resta.

## Commenti

- Niente commenti che ripetono quello che si capisce leggendo la riga (es. `/** Soglia dell'articolo; vuota è null. */`
  sopra `threshold`): si usa un nome esplicativo (`thresholdQuantity`, `idArticle`).
- Un commento si scrive solo per spiegare qualcosa di poco chiaro: il perché di una scelta, un workaround o un
  comportamento di Material, motivi di sicurezza, dettagli del contratto con il backend, l'ordine delle icone di un
  wrapper. Esempio: il commento di `services/api/time-zone.interceptor.ts`, che spiega perché l'header va solo alle
  nostre API e a cosa serve con le date in UTC.
- Niente commenti sulle entità e sui loro campi, nemmeno sulle regole dei valori (date UTC, nomi degli enum .NET, `page`
  da 1): stanno nel contratto delle API.
- Niente commenti che dicono cosa è un tipo o un modello o dove si usa (es. `/** Answer of POST /api/identity/auth/login
  … */`, `/** The form of the edit popup */`): si trova con l'IDE.
- Niente commenti che descrivono cosa fa un componente, un behaviour o un popup, che spiegano le opzioni dei dialog di
  Material o il flusso di un metodo.
- Nel codice mai riferimenti all'utente, a date, al mockup o alla grafica (es. "as in the mockup").
- Nel dubbio il commento non si scrive.

## Cartelle per dominio

- `views/<dominio>/<pagina>/`: pagine e composizione delle route.
- `styles/`: stili SCSS dell'app condivisi da più pagine: vedi `html-scss-e-layout.md`.
- `components/<dominio>/`: componenti riutilizzabili specifici del dominio.
- **Ogni form in popup o pannello è un componente dedicato**, in `components/<dominio>/<sezione>/<pagina>/` (es.
  `components/management/settings/vouchers/voucher-add-item-panel/`,
  `components/management/settings/warehouse-setting/form-warehouse-add-item-popup/`): la pagina contiene solo
  `<app-… [(open)]="…" />`, mai il popup e i suoi campi. Componenti dichiarativi: vedi `componenti-e-wrapper.md`.
- `components/shared/`: componenti davvero trasversali. Solo `components/shared/ui/` andrà nella futura libreria; le
  altre cartelle restano dell'app perché conoscono le sue traduzioni, immagini o configurazione:
  - `layout/` (es. `appEqualHeight`);
  - `forms/`: validatori dei form (vedi `form-e-validazioni.md`);
  - `i18n/`: la libreria dei messaggi `message-format` (vedi `traduzioni-e-lingue.md`);
  - `tables/`: `standardRowActions`, i bottoni di riga standard (vedi `tabelle.md`).
- `components/shared/ui/<famiglia>/`: i wrapper dei componenti UI, pensati per una futura libreria esportabile. Le loro
  regole: vedi `componenti-e-wrapper.md`.
- `DATA_RELOAD` sta nella libreria, in `components/shared/ui/data/`: la libreria definisce il contratto, l'app lo
  riempie in `app.config.ts`.
- `components/i18n/`: strumenti di traduzione dell'app che dipendono da ngx-translate, fuori dalla libreria (es.
  `appI18nText`): vedi `traduzioni-e-lingue.md`.
- `entities/<dominio>/`: modelli ed entità del dominio.
- `services/api/<dominio>/`: client delle API.
- `services/mocks/<dominio>/`: mock delle API, separati per dominio (vedi `api-e-mock.md`).
- `behaviours/<dominio>/`: logica e stato del dominio non legati a un template (es. `behaviours/auth/auth.behaviour.ts`
  → `AuthBehaviour`, la sessione dell'utente).

## Livelli

- Le cartelle seguono i livelli di Clean: `entities/` è il dominio, `services/api/` l'infrastruttura (HTTP, interceptor,
  conversione delle risposte), `behaviours/` la logica applicativa.
- È una struttura ispirata a Clean, non una Clean Architecture rigorosa: le entità non importano nulla dagli altri
  livelli, service e behaviours importano le entità, e non ci sono interfacce intermedie, per non aggiungere astrazioni
  vuote.
- Un argomento trasversale non crea una cartella nuova accanto a queste: diventa un gruppo dentro ciascun livello. Es.
  gli errori: `entities/errors/` (`ApiProblem`), `services/api/errors/` (`toApiProblem`), `behaviours/errors/` (la
  traduzione).

## Entità, request e response

- `entities/` contiene solo tipi, costanti e le classi dei form con i loro valori di partenza: niente logica, niente
  dipendenze da Angular.
- **Ogni request e ogni response delle API sta in `entities`**, insieme al modello del form che la riproduce e alle
  costanti dei suoi campi, nella cartella della sua sezione e pagina (es. `entities/settings/email-configuration/`,
  `entities/settings/contact-support/`, `entities/warehouse/`).
- Nelle pagine e nei componenti restano solo i tipi della grafica, come i testi tradotti.
- Ogni API con un corpo vero ha la sua request con un nome: niente tipi scritti direttamente nei service.
- **Niente oggetti con un solo valore**: una request o una response fatta di un solo valore semplice (un id, una
  stringa, un numero) non ha un'entità. Il service riceve o restituisce il valore stesso e costruisce da sé il corpo
  o la query string (es. `deleteVoucherItems(idVouchers)` manda `{ idVouchers }`).
- La pagina tiene quel valore in una sua variabile con il nome del valore (es. `GET api/system/get-stripe-information`
  restituisce la stringa e la pagina la tiene in `stripeAccountId`).
- Le tabelle usano tutte `PageRequest`, senza un'interfaccia propria per ogni API: vedi `tabelle.md`.
- Le entità di servizio (forme di dati usate da behaviours e service) stanno in `entities`: quelle generali in
  `entities/shared/` (es. `DateRange`, `DatePart`, `Language`), quelle di un argomento nella sua cartella (es.
  `entities/auth/session.ts`, `tab-message.ts`, `logout-outcome.ts`).
- I tipi esclusivi dei mock restano nel file del mock o in `services/mocks/<dominio>/`, mai in `entities/`; i mock
  possono importare i contratti reali dell'app senza duplicarli.
- Il codice di libreria (`components/shared/ui` e le altre parti di libreria come `message-format`) non dipende dalle
  entità dell'app.
- Le entità esistenti non si rinominano: i nomi nuovi li dà l'utente.

### Liste e form

- Una lista riceve `Page<Item>` (`total` e `rows`): ogni riga è l'entità dell'oggetto con tutte le proprietà che manda
  il backend (es. `WarehouseItem`).
- Caso 1: se aggiunta e modifica hanno gli stessi campi della lista, il form usa quell'entità. In modifica la lista
  passa la riga al popup, in aggiunta il popup parte da `new Item()` (es. Aggiungi e Modifica del magazzino con
  `WarehouseItem`).
- Caso 2: se l'oggetto da creare nel form, da mandare alle API o da ricevere è sostanzialmente diverso da uno esistente
  (più campi, o un campo con un altro significato), si crea un'entità per quell'uso (es. un futuro
  `CustomerItemUpdate`).
- Le entità dei form sono classi con i valori di partenza: tutti i campi, testi e numeri, partono da `null` (es.
  `number | null`) e un campo svuotato torna `null`. Se il campo è obbligatorio ci pensano le validazioni.
- I campi che possono essere vuoti sono dichiarati `| null` nella classe in `entities`.
- Niente tipi derivati per i form (`Omit`, `Pick`, mapped type con un elenco di nomi di campo): il form usa l'entità
  stessa. Anche le copie locali dei valori restano nel componente con l'entità (es. `opened` del popup di modifica del
  magazzino è un `WarehouseItem`).
- Esempio: a ogni apertura il popup di accesso parte da `new LoginRequest()` e alla chiusura lo svuota, così la password
  scritta non resta in memoria.
- Gli id (`idArticle`, `idPeriod`…) stanno nelle entità ma non si mostrano mai nei form, salvo richiesta.
- Il service manda solo i campi dichiarati dalla classe, con `onlyFieldsOf(Classe, valore)`
  (`services/api/only-fields-of.ts`): i tipi non tolgono a runtime i campi in più di un JSON del server, e lo spread li
  rimanderebbe tutti. Vale per ogni API che manda un'entità-classe.
- I campi che il service aggiunge al corpo vanno dopo, così non si possono sovrascrivere:
  `{ ...onlyFieldsOf(SeasonPeriodAddRequest, request), isNextSeason }`.
  Funziona perché ogni classe dà un valore di partenza a tutti i suoi campi.
- Non si fanno ipotesi su cosa fa il backend con i dati (es. se l'aggiunta crea un articolo nuovo o somma la quantità a
  uno esistente): se serve saperlo si chiede all'utente.

## Services e behaviours

- **`services/`** contiene solo il livello API:
  - `services/api/<dominio>/<nome>.service.ts` → `<Nome>Service`: chiama gli endpoint con `HttpClient` e restituisce
    le risposte tipizzate con le entità. Niente stato, niente salvataggi nel browser, niente navigazione né regole di
    business;
  - gli interceptor HTTP che servono solo a far funzionare le chiamate (es. aggiungere il token) stanno in
    `services/api/`;
  - `services/mocks/<dominio>/`: finte risposte degli stessi endpoint, servite dall'interceptor dei mock (vedi
    `api-e-mock.md`).
- **`behaviours/`** contiene la logica non legata direttamente a un template o a un componente: stato condiviso
  (sessione, lingua), salvataggi nel browser, regole e decisioni, guard delle route.
- File `<nome>.behaviour.ts` → classe `<Nome>Behaviour`; le guard tengono il loro nome (`behaviours/auth/auth.guard.ts`
  → `authGuard`).
- Flusso: **vista → service (API)**, con un behaviour in mezzo solo dove c'è logica o stato (sessione, filtri
  dell'header…). Viste, popup e behaviours usano direttamente i service HTTP.
- Niente interfacce o contratti tra behaviour e service: con un service per pagina non aggiungerebbero nulla, e Angular
  permette già di sostituire un service (test, mock).
- Un componente o una vista usa i behaviours per la logica e non chiama i service per fare logica (es. l'accesso usa
  `AuthBehaviour.login`, mai `AuthService` dal popup).
- Un behaviour può usare i service per parlare con il backend; un service non usa mai i behaviours. Unica eccezione:
  gli interceptor possono leggere dai behaviours i dati tecnici della chiamata (es. il token da `AuthBehaviour`).

## Struttura delle classi

Ogni classe (componente, direttiva, behaviour, service) ha i membri in quest'ordine:

1. struttura: decoratore e `implements` degli hook che la classe usa davvero (`implements OnDestroy` se c'è
   `ngOnDestroy()`);
2. API pubblica del componente: `input`, `model`, `output`;
3. injection: `inject()` sempre come campo con nome, prima dello stato (i campi si inizializzano nell'ordine in cui sono
   scritti e lo stato li usa);
4. stato: segnali, `computed`, `toSignal`, query (`viewChild`, `contentChildren`), costanti, riferimenti a subscription
   e observer;
5. costruttore esplicito, solo se serve: qui stanno le registrazioni della creazione (`effect`, `afterNextRender`,
   `subscribe`, `addEventListener`), che Angular permette solo in questa fase;
6. hook di Angular nel loro ordine di esecuzione (`ngOnInit`, `ngAfterContentInit`, `ngAfterViewInit`), solo se usati;
   `ngOnChanges` non serve con gli input a segnali, `ngDoCheck` e i `*Checked` sono da evitare;
7. i nostri metodi: pubblici, poi quelli del template (`protected`), poi privati;
8. listener e subscription: i metodi o le proprietà a freccia chiamati da eventi registrati nel codice (observer,
   `addEventListener`, canali, callback delle subscription) o dai listener dell'host;
9. `ngOnDestroy()`, per ultimo.

- `npm run lint` segnala gli errori grossolani: `@typescript-eslint/member-ordering` (prima tutti i campi, poi il
  costruttore, poi i metodi; le proprietà a freccia contano come metodi) e `@angular-eslint/use-lifecycle-interface`
  (`implements` degli hook usati). L'ordine fine dentro i tre blocchi si rispetta a mano.
- Esempi: `LoginDialog`, `Carousel`, `NotFound`, `SearchField`.
- **La distruzione è sempre esplicita** in `ngOnDestroy()`: `unsubscribe()` delle subscription, `disconnect()` degli
  observer, `removeEventListener()`, chiusura di popup e canali. Non si usano `DestroyRef.onDestroy` né
  `takeUntilDestroyed`.
- Non servono pulizie per ciò che Angular chiude da solo (`toSignal`, `computed`, `effect`) né per le subscription che
  finiscono da sole (una chiamata HTTP, la chiusura di un popup).
- **Listener sull'host o su window/document**: nei metadati `host` del decoratore, mai con `@HostListener`, es.
  `host: { '(window:scroll)': 'updateHeaderState()', '(document:keydown.escape)': 'closeMenu()' }`. Tutti i binding
  dell'host stanno in un solo posto, in cima alla classe, come raccomanda la guida di stile di Angular.

## Niente stringhe come opzioni

- Due valori → un booleano con un nome che si legge come una frase (`isSubmitButton`, `hasLabelBefore`, `isHourOnly`).
- Più valori → un enum: dentro il componente se è libreria (`ButtonAppearance`, `ButtonSize`, `TableColumnAlign`,
  `MessagePartKind`, `NumberStyle`), altrimenti in `entities` (`LogoutOutcome`).
- Se il valore viaggia fuori dall'app (URL, CSS, `Intl`), l'enum ha come valori le stringhe esatte, scritte una volta
  sola (`Language`, `DatePart`, `DateStyle`, `TableColumnAlign`).
- I valori che riguardano direttamente il backend restano come sono (`SortDirection`, `TableSortDirection`,
  `SmtpSecurity`, `BookingDayType`): la regola vale per la gestione interna del frontend.
- Le stringhe che chiedono Material o l'HTML restano solo dentro il wrapper.
- Eccezione: i nomi delle chiavi dei gruppi di traduzione letti dal codice restano stringhe, perché sono le chiavi del
  JSON delle traduzioni (es. `supplement_types: Record<'none' | 'percentage' | 'fixed', string>` in
  `BookingOptionsTexts`).
- Prima di usare una stringa come opzione si chiede all'utente, spiegando perché l'alternativa non va.
