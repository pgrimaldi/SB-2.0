# Form e validazioni

Signal Forms, validatori comuni, messaggi ed errori sotto i campi, aspetto dei campi, modifiche non salvate, spinner e
campi bloccati durante le chiamate API, campi data e coppie Dal / Al.

## Signal Forms

- I form usano i Signal Forms di Angular (`@angular/forms/signals`): il form è un signal con un oggetto
  (`form(model, (path) => { … })`) e i campi dei wrapper si collegano con `[formField]`.
- Le regole sono quelle di Angular (`required`, `min`, `max`, `minLength`, `maxLength`…) più le nostre generiche in
  `components/shared/forms/validators.ts`.
- Le regole dei Signal Forms non controllano i campi in sola lettura (`readonly`), disattivati o nascosti.

## Validatori comuni

- `requiredText(path)`: per ogni testo obbligatorio, al posto di `required`, perché conta come vuoto anche un testo
  fatto solo di spazi. Dà lo stesso errore `required` ("Campo obbligatorio").
- `greaterThan(path, limite)`: il valore deve superare il limite (`invalidate.greater_than`); anche un campo vuoto è
  un errore, così "superiore a 0" vale anche come obbligatorio.
- `passwordRules(path, lunghezza)`: almeno quella lunghezza, una maiuscola, una minuscola e un numero, in quest'ordine
  (`invalidate.min_length`, `invalidate.missing_uppercase`, `invalidate.missing_lowercase`,
  `invalidate.missing_digit`). Il campo vuoto lo lascia a `requiredText`.
- `minItems(path, numero)`: una lista con meno elementi è un errore (`invalidate.min_items`, "Deve essere definito
  almeno {{count}} {{field}}"), mostrato sotto la sua sezione.
- `phoneNumber(path, lunghezza)`: un numero con qualcosa che non è una cifra o uno spazio (il prefisso ha il suo
  campo) dà `invalidate.phone_digits`; più lungo di `lunghezza` caratteri, spazi compresi, dà
  `invalidate.phone_max_length` (es. `PHONE_MAX_LENGTH`, 32, il limite del backend).
- `phoneNumberWithoutPrefix(path, lunghezza)`: le stesse regole per un numero che non ha accanto il campo del prefisso;
  il primo errore è `invalidate.phone_digits_without_prefix` ("Solo numeri e spazi, senza +").
- `timeRange(inizio, fine)`: due orari `HH:mm`; se ci sono tutti e due la fine deve venire dopo l'inizio (anche uguali è
  un errore). L'errore va su tutti e due i campi, ognuno col suo testo (`invalidate.time_before_end`,
  `invalidate.time_after_start`), così si vede sotto quello che l'utente ha cambiato.
- `webLink(path)`: un testo che non è un link completo http o https (`invalidate.web_link`). Il campo vuoto lo lascia
  a `requiredText`.
- `emailAddress(path)`: un'email come la controlla il backend, cioè una sola `@`, né prima né ultima
  (`invalidate.email`). Il campo vuoto lo lascia a `requiredText`. La lunghezza massima è `MAX_EMAIL_LENGTH` (255,
  `entities/shared/email-address.ts`). Lo stesso controllo fuori da un form (es. nei mock) è `isEmailAddress(testo)`.
- `maxRichTextLength(path, massimo)`: un testo di `app-rich-text-field` (HTML) con più di `massimo` caratteri visibili;
  la formattazione non conta (`invalidate.max_length`, "Al massimo {{max}} caratteri"). L'editor non blocca la
  scrittura: l'errore va sotto il campo e Salva si spegne.
- Lunghezza massima di un campo di testo: `metadata(path, MAX_LENGTH, () => LIMITE)`, con il limite del backend in una
  costante dell'entità. Il wrapper riceve `maxLength` e il campo non accetta caratteri oltre il limite.
- `noOverlap(path, altri)` / `noOverlaps(path, altri)`: un intervallo di giorni (o una lista di intervalli) che ha un
  giorno in comune con un altro (`invalidate.overlap`).
- `isTextTaken(testo, altri)`: vero se il testo è uguale a uno degli altri, senza badare a maiuscole e spazi agli
  estremi (ogni confronto di testi ignora gli spazi agli estremi: vedi `00-indice.md`). Serve ai controlli "nome già usato" che il form fa da sé con `validate`, con un errore della pagina sotto
  `invalidate.` (es. il nome di un periodo in Stagioni e periodi).
- Messaggi generici di Angular: `invalidate.min` "Il valore minimo è {{min}}", `invalidate.max` "Il valore massimo è
  {{max}}".

## Messaggi degli errori

- Il testo lo dà `ValidationTextBehaviour` (`behaviours/validation/`): `message(field, chiaveNomeCampo)` restituisce il
  testo tradotto del primo errore (chiave `invalidate.<kind>`, con i valori dell'errore e il nome del campo, es.
  `management.settings.warehouse.form.field_names.quantity` = "della quantità").
- Il messaggio compare solo dopo che l'utente ha cambiato o lasciato il campo e segue la lingua. Un'eccezione: nei pannelli
  di modifica di Servizi e Listino, premendo Modifica, i campi letti già sbagliati si segnano subito
  (`draftForm().markAsTouched()` in `startEdit`), perché tengono spento Salva.
- Per i campi di una lista, dove non si può tenere un signal per campo, il template legge lo stesso testo con
  `text(field, chiaveNomeCampo)`.
- Il wrapper riceve il testo con `[error]`: bordo rosso da 2px, sfondo rosato e messaggio rosso
  (`--color__layout__error`) sotto il campo.
- Codici di errore del backend e loro testi `error.<code>`: vedi `api-e-mock.md`.

## Errori sotto i campi

- Ogni errore sta sotto il campo che lo genera, mai solo in un messaggio generico. Le validazioni sono reattive: il
  bottone che salva è spento finché il form non è valido. Un'eccezione: nei due popup di Sconti e voucher Salva resta
  acceso, e premendolo il popup scorre fino al primo errore (`showFirstError`,
  `components/shared/forms/first-error.ts`).
- Anche gli errori del backend su un campo (`errors[]` di Problem Details, con `field` uguale al nome nella richiesta)
  vanno sotto quel campo: si invia con `submit()` dei Signal Forms e, se la chiamata fallisce, l'azione restituisce
  `apiFieldErrorsOrGeneral(problem, form, …)` (`behaviours/validation/api-field-errors.ts`; questi errori hanno
  `kind` = `API_ERROR_KIND`).
- Lo fanno tutti i form con chiamate, e ogni campo, interruttore compreso, ha il suo `[error]`.
- Di un errore del backend `ValidationTextBehaviour` mostra il testo `error.<code>` (`ErrorTextBehaviour.instant`);
  Angular lo toglie appena il campo cambia.
- `submit()` non invia nulla se un campo è sbagliato e segna tutti i campi come toccati, così ogni errore si vede.
- Un errore che non ha un campo dove andare finisce nel messaggio generale: sotto il riquadro resta solo l'errore che
  non riguarda un campo (es. rete assente). Di un errore dentro un elenco (es. `variants[0].price`) il messaggio generale
  è il testo del suo `code`, non "Richiesta non valida". Vale anche per un campo del form senza un posto per il suo errore (una scelta
  `app-radio-group`, un campo non mostrato): il pannello lo passa come ultimo argomento di `apiFieldErrorsOrGeneral`
  (es. `hiddenTableServiceFields`), così Salva non resta spento senza spiegazione.
- Un codice del backend che arriva senza `errors[]` ma riguarda un solo campo (es. `pricing.period_name_taken`, nome
  già usato; `account.email_taken` sotto Email) il form lo mette sotto quel campo, restituendo da `submit()` `apiFieldError(campo, codice)`. Un errore del
  backend su un campo si crea solo con `apiFieldError`, mai scrivendo l'oggetto con il suo `kind`.
- Un errore su un id (un campo che comincia con `id`, come `idPeriod`) non va mai sotto un campo ma nel messaggio
  generale: l'id non si vede e il form resterebbe bloccato senza spiegazione.
- Il backend non segnala mai un id in `errors[]`: un id non valido annulla l'operazione con un errore generale (codici:
  vedi `api-e-mock.md`).

## Campi obbligatori e aspetto dei campi

- I campi obbligatori si segnano a vista con `[isMandatory]="true"` (asterisco rosso dopo l'etichetta) sui wrapper con
  etichetta: `app-text-field`, `app-password-field`, `app-filled-text-field`, `app-filled-number-field`,
  `app-filled-textarea`, `app-filled-select`, `app-filled-icon-select`, `app-filled-date-field`,
  `app-filled-time-field`, `app-rich-text-field`.
- I campi dei form (testo, numero, area di testo, tendina) sono bianchi con un bordo sottile blu tenue
  (`--color__layout__text__soft`) quando si possono modificare, grigi con il testo grigio quando sono in sola lettura
  o disattivati, perché l'utente lo capisca subito.
- I campi riempiti dal riempimento automatico del browser (nomi, indirizzi, password salvate) tengono i colori
  dell'app (regola globale in `styles.scss`, contro l'azzurro o il giallo di Chrome, Edge e Safari e il filtro di
  Firefox). Il riempimento automatico resta attivo.

## Mai salvare sopra dati non letti

- Un form che modifica dati letti dal server non si apre in modifica e non si salva finché quella lettura non è
  riuscita: "Modifica" e "Salva" restano spenti (es. `isLoaded`, o la tab tra quelle caricate) e anche `edit()` e
  `save()` non fanno niente senza i dati. Se la lettura fallisce restano spenti: mai mandare al backend valori vuoti o
  di partenza al posto di quelli veri.
- Vale per ogni form di modifica, pagine, tab, pannelli e popup, anche futuri. Prima di consegnare un form di creazione
  o modifica si controlla che cosa parte quando la lettura fallisce.
- Quello che il backend ha e il form non mostra si legge e si rimanda uguale, così un Salva non lo cancella: campi in più
  nell'entità (es. `weekdays` e `maxPerSlot` delle opzioni di prenotazione di Servizi), righe nascoste (es.
  `hiddenPeriodPrices`), un valore fuori dalle scelte di una tendina o di una griglia (es. `intervalMinutes`,
  `otherIcon`), che resta finché l'utente non ne sceglie un altro. Se l'API è nostra, i campi si chiedono nel documento
  delle richieste.
- Un popup o pannello di modifica non mostra mai il form vuoto: se la lettura all'apertura fallisce si chiude e
  compare `app-read-error-popup` (`components/management/read-error-popup/`: "Errore", il messaggio del `code`,
  Chiudi e Riprova, che riapre la modifica e rilegge; per un 404, quando l'elemento non c'è più, solo Chiudi). Il
  componente ha `readError` (passato a `[(problem)]`) e `retryRead()`, che riapre il popup. Nei pannelli che leggono
  una tab alla volta (Servizi) questo vale per la lettura all'apertura; una tab letta dopo che fallisce mostra al
  posto dei campi il messaggio e Riprova (`retryTab()`), senza chiudere il pannello.
- Controllo di versione (contro due utenti che salvano la stessa cosa): ogni lettura che un form poi risalva porta
  `version`, una per ogni salvataggio (per esempio una per tab); il salvataggio la manda nell'header `If-Match`
  (`ifMatch(version)` di `services/api/versions/version-headers.ts`) e restituisce la nuova versione letta
  dall'`ETag` (`versionOf`), che il form tiene se resta aperto per altri salvataggi. Con 409
  `resource.version_conflict` compare `app-version-conflict-popup` (`components/management/version-conflict-popup/`,
  testo `error.resource.version_conflict`) e le modifiche dell'utente si perdono:
  - un popup o pannello sempre modificabile si chiude e i dati nuovi si vedono riaprendolo; se è aperto da una tabella,
    la pagina la ricarica (`reloadIfChanged` sull'uscita `failed`);
  - un form con il bottone Modifica rilegge i dati e torna in sola lettura, così Salva non si preme per sbaglio; se è aperto
    da una tabella la pagina ricarica anche quella (`reloadIfChanged`). Nei pannelli di Servizi e Listino l'avviso compare dopo la
    rilettura; se la rilettura fallisce compare solo il suo errore;
  - un popup senza una lettura sua (Settori e file, periodi e intervalli di Stagioni e periodi) si chiude e la lista si
    ricarica;
  - una pagina sempre modificabile (Sharing) rilegge i dati.

  Nei mock la versione è un hash dei dati coperti (`versionOf`, `versionConflict`, `savedWithVersion` di
  `services/mocks/versions/versions.mock.ts`). La richiesta al backend va nel documento delle richieste di ogni pagina
  (modello: sezione 7 di `Impostazioni magazzino - Frontend requests.md`).
- Vale anche per le API di salvataggio chieste al backend: cambiano solo i dati che la pagina ha letto e mostrato, mai
  quelli che l'utente non ha visto.
- Salva rimanda **sempre tutto il modello letto** dal backend: i campi e le voci che l'utente ha cambiato con il nuovo
  valore, tutti gli altri com'erano. Mai solo i campi cambiati, mai solo le voci selezionate di una lista (es. la tab
  Servizi rimanda tutti i servizi letti, ognuno con `isSelected` vero o falso), mai un corpo a cui manca qualcosa che il
  backend potrebbe cancellare. Vale per pagine, tab, pannelli e popup, anche futuri.
- In una tab o pagina con caricamenti di file immediati (es. la tab Media di Informazioni stabilimento) il modello che
  Salva rimanda sono i campi del suo form (lì link del tour e posizione); i file si caricano e si tolgono con le loro API
  (vedi "Caricamento di file" in `api-e-mock.md`) e non passano dal Salva, la cui API non li tocca.

## Modifiche non salvate

- Quello che l'utente ha scritto e non ha salvato non si perde mai senza chiedere: vale per ogni form, di pagina o
  popup, presente e futuro.
- Ogni form implementa `UnsavedChanges` con `hasUnsavedChanges()`, che confronta i valori del form con quelli letti o
  salvati l'ultima volta (`comparableJson`, `sameText`), mai con `form().dirty()`: `dirty()` resta vero anche quando un
  valore torna com'era (un numero rimesso, un'icona scelta di nuovo) e farebbe chiedere senza nulla da perdere.
  - Un popup confronta con i valori di apertura, solo mentre è aperto.
  - Un form che non legge nulla (es. Contattaci) confronta con il modello vuoto.
  - Un campo che il server non rimanda (PIN, password) confronta con il campo vuoto: scriverlo è una modifica.
- I valori si confrontano senza spazi ai lati: aggiungere solo spazi non è una modifica, e la stessa regola vale per il
  Salva che chiude senza chiamate quando non è cambiato nulla.
- `dirty()` e `markAsDirty()` restano solo dove conta che l'utente abbia toccato un campo: mostrare il suo errore
  (`ValidationTextBehaviour`) o un comportamento della pagina legato al tocco (es. il minimo di giorni di Sharing).
- Il form si registra con `UnsavedChangesBehaviour.watch(this)` nel costruttore e `unwatch(this)` in `ngOnDestroy`
  (`behaviours/forms/`).
- Poi, senza altro codice: uscire dalla pagina chiede prima (guard `unsavedChangesGuard` su tutte le pagine del
  gestionale in `app.routes.ts`, con `canceledNavigationResolution: 'computed'` perché anche il tasto indietro del
  browser resti coerente); anche il logout dal menu chiede prima.
- Chiudere o ricaricare la scheda fa comparire la domanda del browser (`beforeunload`).
- Un popup con form usa `disableClose: true` e chiude X, Esc e clic fuori con `leave()`: durante il salvataggio non fa
  nulla, con modifiche chiede prima (`confirmDiscard`, `closeRequested()` di `BasePopup`).
- Anche Annulla chiede prima: nei popup chiama `leave()`; nei pannelli laterali manda lo stesso `closeRequest` della X;
  l'Annulla della modalità Modifica di pannelli e pagine chiama `requestCancel()`, che con modifiche chiede
  (`confirmDiscard`) e solo dopo rimette i dati salvati.
- La domanda è una sola, `app-unsaved-changes-popup` nel layout (`app-confirm-popup` rosso, testi `unsaved_changes.*`:
  "Modifiche non salvate", "Hai modifiche non salvate: se esci andranno perse.", Resta, Esci senza salvare).
- Anche i popup dei filtri contano come form (es. `app-existing-booking-filters-popup`, `app-audit-log-filters-popup`):
  le scelte diverse da quelle di apertura fanno chiedere con X, Esc, clic fuori e Annulla.
- Non si chiede nulla quando la sessione finisce senza una scelta dell'utente (scaduta, un altro utente in un'altra
  scheda): i dati di quella sessione devono sparire. Il login non è un form da proteggere.

## Spinner e campi bloccati durante le chiamate

- Ogni bottone o voce che fa partire una chiamata API mostra lo spinner finché la risposta non arriva, anche se dura
  mezzo secondo (`[isLoading]` di `app-button`; altrove `app-spinner`), e intanto non si può premere di nuovo.
- Finché il server non risponde, tutti i campi del form sono in sola lettura o disattivati: inserimento, modifica,
  cancellazione, duplicazione, invio, salvataggio, login, in qualsiasi form o tabella.
- Con i Signal Forms si usa la regola `readonly(path, { when: () => <chiamata in corso> })`; nei popup e dove i wrapper
  non hanno `readonly`, `inert` sul contenitore dei campi.
- Checkbox, toggle e selezione multipla non hanno `readonly` (resterebbero cliccabili): nei form che li contengono si
  usa `disabled(path, …)`, che per i Signal Forms ha lo stesso effetto (validazioni saltate, valori inviati) e lo
  stesso aspetto grigio.
- Lo stato "in corso" vive finché vive la chiamata, non finché è aperto il popup: se l'elemento sparisce e ricompare
  durante la chiamata (es. il popup di login chiuso e riaperto) lo spinner si rivede.
- Un errore arrivato per un tentativo il cui popup è stato chiuso non compare nel popup riaperto.
- Azioni di tabella (interruttore, duplica, elimina): spinner a pagina intera, vedi `tabelle.md`.

## Coppie Dal / Al e Dalle / Alle

- In ogni form la fine di una coppia non viene mai prima dell'inizio. Giorni: il "Al" non prima del "Dal", uguali vanno
  bene (es. 01/06–01/06), salvo dove il backend lo vieta o l'utente dice altrimenti. Orari: l'"Alle" **sempre dopo** il
  "Dalle", mai la stessa ora (come vuole il backend). Ogni coppia nuova segue la stessa regola.
- Orari: ogni coppia di `app-filled-time-field` ha `timeRange` e ognuno dei due campi ha il suo `[error]`; in una riga
  aggiunta (es. una fascia oraria) i due orari sono anche obbligatori. Esc non svuota mai un orario. Nei pannelli
  che salvano una tab alla volta il controllo vale solo sulla tab aperta, come gli altri (`applyWhen` su
  `selectedTab()`).
- Giorni: sotto.

## Campi data e coppie Dal / Al

- I campi data sono in sola lettura di default: la data si sceglie dal calendario. Un campo che accetta date scritte a
  mano le legge con `DateLanguageBehaviour.parse`.
- Coppie "Dal / Al", per tutti i date picker: il "Dal" non ha mai limiti, il "Al" non può venire prima del "Dal".
  `app-period-calendar`, `app-date-range-select` e `app-datepicker` lo fanno già da soli.
- Con due `app-filled-date-field` la pagina lega solo il "Al" con `[minDate]` al valore del "Dal" (es.
  `[minDate]="period.validFrom().value()"`), che si aggiorna da sé a ogni nuovo "Dal": i giorni prima sono spenti nel
  calendario.
- Se il "Dal" passa oltre il "Al" già scelto, il wrapper svuota il "Al" (un giorno fuori da `minDate` / `maxDate` viene
  sempre svuotato) e l'utente lo sceglie di nuovo.
- Se c'è anche un altro limite (es. i giorni della stagione) vale il più stretto; i giorni già presi da altri periodi
  si spengono con `disabledPeriods`. Ogni nuova coppia fa lo stesso.
- Un periodo di giorni dei componenti comuni si chiama `startsOn` / `endsOn`, come nel backend: i periodi di
  `app-period-calendar`, i `disabledPeriods` di `app-period-calendar` e `app-filled-date-field`, `DayRange` di
  `noOverlap` / `noOverlaps` (`components/shared/forms/validators.ts`) e le entità che li usano (es.
  `SeasonPeriodInterval`, `PackageReducedSeasonalPrice`, `OnlineBookingPeriod`). Le altre coppie usano i nomi della loro
  API (es. `validFrom` / `validTo` dei voucher, `dateFrom` / `dateTo` di Servizi e Prenotazioni online).
- Formato delle date mostrate e adattatore nativo: vedi `traduzioni-e-lingue.md`; date UTC nelle API: vedi
  `api-e-mock.md`.
