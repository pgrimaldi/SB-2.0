# Indice delle regole del frontend

Da leggere **sempre**, all'inizio di ogni conversazione. Contiene le regole che valgono per qualunque richiesta e dice
quali altri file leggere in base alla richiesta. I file di regole stanno in questa cartella (`docs/File di regole/`), le
descrizioni delle pagine in `docs/Pagine/`.

## Regole che valgono sempre

- Si risponde in italiano, in breve, un passo alla volta. Si fa solo quello che è stato chiesto e non si propone il
  passo successivo: lo decide l'utente. Nel dubbio si chiede prima di decidere.
- Il commit e il push li fa l'utente: mai crearli senza una richiesta esplicita.
- Si lavora solo sul frontend Angular 22 in `frontend/summer-booking-front-office/`. Del backend interessa solo il
  collegamento (rotte, campi, risposte, codici di errore), non come lavora dentro.
- Ogni elemento grafico passa da un wrapper in `components/shared/ui/`; prima Angular Material, poi un componente custom.
- Una modifica si allinea ovunque nello stesso passo: entità, servizi, mock, test (solo API e casi d'uso), le 7 lingue,
  i documenti (contratti delle API, catalogo dei wrapper, questi file di regole, `docs/TODO.md`, i documenti di richieste
  al backend).
- Form: Signal Forms, errori sotto i campi, Salva spento finché il form non è valido, spinner e campi bloccati durante
  ogni chiamata, avviso prima di perdere le modifiche. Mai salvare sopra dati che non sono stati letti: Modifica e
  Salva restano spenti finché la lettura dal server non è riuscita, e un popup o pannello di modifica la cui lettura
  fallisce si chiude e mostra `app-read-error-popup` (mai un form vuoto). Ogni salvataggio di dati letti manda la
  versione letta (`If-Match`): con 409 `resource.version_conflict` avviso; i popup sempre modificabili e quelli senza
  una lettura loro si chiudono, gli altri form rileggono i dati.
- Tabelle: `app-table` si carica da sola con la richiesta standard delle tabelle.
- Ogni cancellazione chiede **sempre** conferma prima di chiamare l'API (popup rosso: `app-confirm-popup` o
  `app-delete-items-popup` per le tabelle): righe di tabella, file e immagini caricati, sfondi, qualunque dato salvato.
  Le X dentro un form che tolgono un valore senza chiamare un'API (es. un'icona del reticolo, una data dei festivi)
  non chiedono: la modifica la salva Salva e la toglie Annulla.
- Mai id del database scritti nel codice, mai un oggetto che contiene un solo valore, mai stringhe libere come opzioni.
- Ogni confronto di testi si fa **sempre** sui testi senza spazi all'inizio e alla fine, ovunque (componenti,
  behaviour, servizi, mock): uguaglianze, "nome già usato", "ci sono modifiche?", ricerche e ordinamenti. Le funzioni
  sono in `components/shared/forms/text-comparison.ts`: `sameText` e `sameTextIgnoringCase` per due testi,
  `comparableJson` per due modelli (testi senza spazi ai lati, un testo vuoto conta come `null`); per cercare e ordinare
  `trim()` su tutti e due i lati. Non riguarda i valori tecnici che nessuno scrive (codici, enum, id, chiavi, URL,
  date ISO, nomi dei campi).
- Il frontend riflette il backend: ogni errore che un'API può dare ha il suo codice, la sua traduzione e il suo posto
  sullo schermo (se il backend ha già un codice si usa quello), e quello che il backend vuole obbligatorio o limita lo è
  anche nel form. Un orario di fine viene sempre dopo quello di inizio. Dettagli in `api-e-mock.md` e
  `form-e-validazioni.md`.
- Mai stringhe scritte a mano nel codice per valori con un significato (codici di errore, nomi di campi, scelte): si usa
  il membro di un enum o una costante (es. `ApiErrorCode.ValidationOutOfRange`). Se una stringa sembra inevitabile, prima
  si dice all'utente dove e perché e si cerca una soluzione insieme.
- Testi mai tagliati né a capo a metà parola, in tutte e 7 le lingue (italiano, inglese, francese, spagnolo, tedesco,
  cinese, arabo da destra a sinistra).
- Niente secret, password o chiavi nel codice, nei file versionati o in chat.
- Commenti nel codice quasi assenti.
- La documentazione descrive solo lo stato attuale: niente storia, date o provenienza (la storia la tiene git).
- Prima di consegnare: controllo dei tipi, lint, test, build e verifica nel browser di quello che si vede.

## Quali file leggere

Leggi i file della riga che corrisponde alla richiesta (anche più righe se la richiesta tocca più temi).

| Se la richiesta riguarda… | Leggi |
|---|---|
| Come lavorare, documenti da aggiornare, dove stanno i documenti | `collaborazione.md` |
| Nuovo componente, servizio, behaviour, entità, cartelle, nomi, dipendenze, librerie | `architettura.md` |
| Wrapper, elementi grafici, popup, bottoni, titoli, icone dei componenti | `componenti-e-wrapper.md` |
| Form, popup con campi, validazioni, messaggi di errore dei campi, Salva/Annulla, date Dal/Al, orari Dalle/Alle, spinner | `form-e-validazioni.md` |
| Tabelle, elenchi, paginazione, filtri, ordinamento, ricerca, azioni di riga | `tabelle.md` e `api-e-mock.md` |
| API, chiamate al backend, mock, contratti, documenti delle richieste al backend, combobox, caricamento di file, codici di errore, download | `api-e-mock.md` |
| Traduzioni, lingue, arabo/RTL, testi, date e numeri mostrati | `traduzioni-e-lingue.md` |
| Stili, SCSS, layout, misure, responsive, risoluzioni, mockup della grafica | `html-scss-e-layout.md` |
| Immagini, icone, font, asset, bandiere | `asset-e-icone.md` |
| Login, token, sessione, password, CSP, header di sicurezza, link esterni, chiavi | `sicurezza.md` |
| Test, verifiche, lint, Prettier, backend locale, consegna | `test-e-verifiche.md` |
| Rotte, nuova pagina, menu, layout del gestionale, sezione Impostazioni, guard | `routing-e-autenticazione.md` e `architettura.md` |
| Git, `.gitignore`, build per ambiente, deploy, CloudFront, PWA | `git-e-deploy.md` |
| SEO, pagine pubbliche, home dell'hub | `seo.md` |
| Una pagina per nome (es. "Gestione utenti", "Listino") | la pagina in `docs/Pagine/` (tabella sotto) e i file dei temi toccati |

## Pagine

| Pagina | Descrizione |
|---|---|
| Informazioni stabilimento | `docs/Pagine/informazioni-stabilimento.md` |
| Gestione utenti (con la schermata Log) | `docs/Pagine/gestione-utenti.md` |
| Stagioni e periodi | `docs/Pagine/stagioni-e-periodi.md` |
| Settori e file | `docs/Pagine/settori-e-file.md` |
| Configura mappa | `docs/Pagine/configura-mappa.md` |
| Listino | `docs/Pagine/listino.md` |
| Servizi | `docs/Pagine/servizi.md` |
| Sconti e voucher | `docs/Pagine/sconti-e-voucher.md` |
| Sharing | `docs/Pagine/sharing.md` |
| Prenotazioni online | `docs/Pagine/prenotazioni-online.md` |
| Fiscalizzazione | `docs/Pagine/fiscalizzazione.md` |
| Configurazione email | `docs/Pagine/configurazione-email.md` |
| Assistenza | `docs/Pagine/assistenza.md` |
| Database (con il Cestino) | `docs/Pagine/database.md` |
| Impostazioni magazzino | `docs/Pagine/impostazioni-magazzino.md` |

Magazzino e Beachmap non hanno ancora una descrizione: si parte dal codice in `src/app/views/`.

## Altri documenti

- `docs/API_CONTRACTS.md` e `shared documentation/API_CONTRACTS.docx`: tutte le API con richieste, risposte ed errori.
- `shared documentation/Modifiche per il backend/<area>/<Pagina> - Frontend requests.md`: cosa il backend deve fare,
  una pagina per documento.
- `docs/WRAPPER_COMPONENTS.docx`: tutti i wrapper con i loro parametri.
- `docs/TODO.md`, `docs/SECURITY_TODO.md`, `docs/SEO_OPTIMIZATION_TODO.md`, `docs/POSSIBLE_FUTURE_REFACTORING.md`:
  punti aperti.
- `docs/README_DEPLOY.md`: pubblicazione su AWS.
