# Traduzioni e lingue

Regole per i testi tradotti, le 7 lingue, l'arabo da destra a sinistra, le chiavi, i messaggi ICU e le date mostrate.

## Strumenti

- Le traduzioni runtime usano `ngx-translate`. L'italiano è la lingua iniziale e di fallback; il cambio lingua avviene
  senza ricaricare la pagina.
- I file sono `src/assets/i18n/<codice>.json`, letti con percorsi assoluti sotto `/assets/i18n/` (compatibili con S3
  e CloudFront).
- Gli strumenti di traduzione dell'app che dipendono da ngx-translate stanno in `components/i18n/` (es. `appI18nText`),
  fuori dalla libreria.
- Niente testi dell'interfaccia scritti nel codice quando si possono tradurre.
- Se il volume cresce, le traduzioni si possono separare per dominio senza duplicare componenti.
- I wrapper non traducono (vedi `componenti-e-wrapper.md`): il gruppo di traduzione dei loro testi fissi ha gli stessi
  nomi dei campi dell'interfaccia `texts` (es. `AlertPopupTexts` con `close` → gruppo `alert` con `close`, passato
  con `[texts]="'alert' | translate"`).

## Lingue

- Sette lingue, una per valore dell'enum `Language` (`entities/shared/language.ts`): `it`, `en`, `fr`, `es`, `de`,
  `zh` (cinese semplificato), `ar` (arabo). Le date del cinese usano `zh-CN`, quelle dell'arabo `ar-SA`.
- La lista è `LanguageBehaviour.languages`, con la bandiera di ogni lingua in `src/assets/images/flag-<paese>.svg`
  (`flag-gb.svg` per l'inglese, `flag-cn.svg` per il cinese, `flag-sa.svg` per l'arabo; quella spagnola senza stemma).
- Il font Outfit non ha i caratteri cinesi e arabi: il browser li prende dal font di sistema.
- Una nuova lingua tocca: l'enum, la lista con la bandiera, `DATE_LOCALES` di `DateLanguageBehaviour`, il nome della
  lingua in `language.names` di ogni file, il preload in `src/index.html` e la CloudFront Function di
  `README_DEPLOY.md` (`LANGUAGES` e `PUBLIC_ROUTES`).
- Un testo che non si riesce a tradurre si scrive `PLACEHOLDER`.
- Nessun errore resta senza testo: ogni messaggio d'errore che l'app può mostrare (codici del backend sotto `error.`,
  controlli del frontend sotto `invalidate.`, avvisi delle pagine) ha la sua traduzione in tutte e 7 le lingue. Se il
  testo definitivo non c'è ancora, se ne scrive uno provvisorio, preciso e adatto al contesto.
- La lingua si cambia solo nell'hub (header pubblico): nell'area riservata non si cambia e resta quella scelta
  nell'hub.

## Arabo, da destra a sinistra

- `LanguageOption.isRightToLeft`: `LanguageBehaviour.use` mette `dir="rtl"` sulla pagina e aggiorna `Directionality`
  del CDK (`valueSignal` e `change`), così anche popup, menu e calendari di Material si girano senza ricaricare.
- Si specchia tutta l'interfaccia (logo a destra, menu e passaggi da destra a sinistra, X dei popup a sinistra), non
  numeri, date, codici, loghi e icone di oggetti (linee guida di Material, Apple e W3C).
- Nel nostro SCSS niente proprietà sinistra/destra: si usano quelle logiche (`margin-inline-start`,
  `padding-inline-end`, `inset-inline-end`, `text-align: start`…); restano fisiche solo quelle simmetriche.
- Le frecce che indicano una direzione si specchiano con `:host-context([dir='rtl'])` (giorno precedente/successivo
  del calendario, freccia del pannello espandibile, frecce del carosello).
- Anche il carosello è specchiato: prima slide a destra, "avanti" verso sinistra, con frecce, trascinamento e tasti
  freccia girati (`app-carousel` legge `Directionality`).
- Cifre sempre occidentali, 0-9, anche in arabo: le date usano il calendario gregoriano con le cifre `latn`
  (`DateLanguageBehaviour`); i numeri dei testi `ar` usano già le cifre 0-9.

## Telefoni, email e codici

- Numeri di telefono, email e codici restano da sinistra a destra anche in una pagina in arabo: `dir="ltr"` sul loro
  elemento (es. il telefono dell'assistenza).
- Il numero di telefono resta intero da sinistra a destra, compresi prefisso `+` e gruppi separati da spazi; il valore
  non si riordina e non si converte.
- Nei campi si usa `[isPhoneField]="true"` di `app-filled-text-field`: applica `type="tel"` (tastiera telefonica) e
  `dir="ltr"` solo all'input, senza cambiare la direzione dell'etichetta o del layout. Riferimento:
  [W3C, gestione dei contenuti bidirezionali](https://www.w3.org/International/articles/inline-bidi-markup/).
- Anche i numeri negativi restano da sinistra a destra (`-45`, non `45-`): `[isSigned]="true"` di
  `app-filled-number-field` applica `dir="ltr"` solo all'input, che in arabo resta allineato a destra come gli altri
  campi.

## Chiavi di traduzione

- Tutte minuscole, annidate per significato (`home.hero.title`), con le parole composte separate da `_` (`full_day`,
  `logout_failed`, `invalidate.email_configuration.smtp_password.retype`): niente trattini né camelCase.
- È lo stesso stile dei codici d'errore del backend (`error.auth.invalid_credentials`) e ogni parte è un nome valido in
  TypeScript. Chiavi degli errori: vedi `api-e-mock.md`.
- Un gruppo letto da TypeScript (`translate.stream('gruppo')`) ha un'interfaccia con i nomi delle chiavi (es.
  `PeriodTexts`): si scrive `texts.full_day` invece di `texts['full_day']` e un nome sbagliato si vede in compilazione.
- Le chiavi sono nomi interni: cambiarle non tocca indirizzi, `id`, ancore (es. `#about-us`) o testi visibili.

## Test delle traduzioni

- `behaviours/i18n/translations.spec.ts` controlla tutte le lingue di `LanguageBehaviour.languages` (una lingua nuova
  è controllata appena aggiunta, con il suo file in `assets/i18n/`).
- Controlla: stesse chiavi in ogni lingua, nessun testo vuoto, stessi segnaposto `{{nome}}`, e sotto `error.`
  esattamente i codici conosciuti (`error.unknown`, `API_ERROR_CODES` e `FRONTEND_ERROR_CODES`).
- Segnala anche i messaggi ICU non validi e gli argomenti o i tipi diversi tra le lingue.

## Plurali, numeri, valute e date nei testi (ICU)

- Sintassi ICU MessageFormat, interpretata dalla nostra libreria `components/shared/i18n/message-format/`, collegata a
  ngx-translate in `app.config.ts` con `MessageFormatCompiler`.
- La libreria non ha dipendenze né `eval` (compatibile con una CSP rigida) ed è pensata per essere estratta: niente
  import fuori dalla cartella, l'app usa solo `index.ts`. È documentata in `docs/MESSAGE_FORMAT.docx` (versionato), da
  aggiornare a ogni modifica della libreria.
- Si usa solo dove serve:
  - `{count, plural, =0 {nessun posto} one {# posto} other {# posti}}`: le categorie dipendono dalla lingua, `other` è
    obbligatorio, `#` è il numero;
  - `{n, number}`, `{n, number, integer}`, `{n, number, percent}`;
  - `{amount, number, currency}`: valuta dall'argomento `currency` (codice ISO 4217), altrimenti EUR;
  - `{day, date, short|medium|long|full}` e `{at, time, …}`.
- In un testo ICU anche i valori semplici si scrivono `{name}`; tutti gli altri testi restano con `{{name}}` di
  ngx-translate. Gli apostrofi sono sempre testo ("L'ombrellone").
- Un istante ISO 8601 (`2026-08-01T10:00:00Z`) si mostra nel fuso dell'utente, un giorno (`2026-08-01`) così com'è.
- Un argomento mancante resta scritto com'è (`{count}`); un messaggio non valido resta testo.

## Stesso layout in tutte le lingue

- **Requisito obbligatorio**: allineamenti e dimensioni identici in tutte le lingue.
- Per i testi visibili (titoli, paragrafi, voci di menu, etichette dei pulsanti) si usa `appI18nText`
  (`components/i18n/i18n-text/`), che riserva lo spazio di tutte le traduzioni, senza sceglierle per numero di
  caratteri: anche un testo più corto può occupare più righe.
- Le riserve sono pseudo-elementi di span nascosti: il testo visibile resta solo quello della lingua corrente.
- Prima di riservare, le traduzioni più larghe dell'italiano si accorciano mantenendo il concetto: la riserva diventa
  quella italiana e la pagina italiana non cambia. Si accorcia la traduzione invece di cambiare il layout.
- Un testo senza alternative più corte che allarga solo il suo bottone, senza spostare altro, resta con il `translate`
  normale.
- Esempi: nel Database "Annulla selezione", "Cancella dati", "Svuota cestino" ed "Elimina definitivamente" usano
  `appI18nText`, mentre il titolo, "Cestino", "Filtri" e "Ripristina" no; in Fiscalizzazione lo usa l'avviso rosso di
  "Non accetto".
- Il controllo si fa misurando le posizioni degli elementi nelle 7 lingue a 360×800 e 1920×1080 (es. iframe della
  stessa app con `sb.language` in `localStorage`).

## Date mostrate

- Niente librerie di date: adattatore nativo di Angular Material (`provideNativeDateAdapter`) e `Intl` del browser; i
  calcoli (oggi, ±giorni, confronti) con i metodi di `DateAdapter`.
- Il locale dell'adattatore lo imposta `DateLanguageBehaviour` per tutta l'app (avviato in `app.config.ts`): i
  componenti di data, come `app-datepicker`, non conoscono la lingua e usano l'adattatore che ricevono.
- Le date si scrivono come nella lingua: locale dalla lingua impostata nel browser (es. `en-US`, `en-AU`, `it-CH`),
  altrimenti da `DATE_LOCALES` in `behaviours/i18n/date-language.behaviour.ts` (inglese `en-GB`). Una nuova lingua
  aggiunge lì il suo locale.

## Testi del calendario di Material

- I comandi del calendario ("Mese precedente", "Apri calendario"…) sono tradotti nel gruppo `calendar.*` di ogni
  lingua, perché Material li mostra anche come tooltip delle frecce (`matTooltip`).
- `CalendarTextsBehaviour` (`behaviours/i18n/`) li passa a `MatDatepickerIntl` a ogni cambio lingua. Non parte con
  l'app: `MatDatepickerIntl` porta con sé tutto il calendario di Material (circa 150 kB), che la home non deve
  scaricare.
- Lo avvia nel costruttore, con `inject(CalendarTextsBehaviour)`, chi mostra un calendario: nel gestionale
  `ManagementHeader`, in una futura pagina pubblica la pagina stessa. È unico per tutta l'app: avviarlo più volte non
  fa altro.
- `app-datepicker` importa solo `MatDatepicker` e `MatDatepickerInput`: il modulo intero fornirebbe un suo
  `MatDatepickerIntl` con i testi inglesi di Material.
- Il bundle iniziale resta sotto il budget di 500 kB di `angular.json`: si controlla con la build di produzione quando
  si aggiunge qualcosa che parte con l'app.
