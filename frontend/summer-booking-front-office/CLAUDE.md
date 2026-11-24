# Summer Booking front office

**Prima di ogni altra cosa leggi sempre `docs/File di regole/00-indice.md`**: contiene le regole che valgono sempre e
indica quali altri file di regole leggere in base alla richiesta. Le descrizioni delle pagine sono in `docs/Pagine/`.

## Comportamento

- Rispondi in italiano, in modo breve e chiaro: prima la risposta, poi i dettagli solo se servono.
- Un passo alla volta: fai solo quello che è stato chiesto, riporta il risultato e fermati. Il passo successivo lo
  decide l'utente. Nel dubbio fai una domanda breve invece di decidere da solo.
- Commit, push e operazioni git che cambiano la storia li fa l'utente.
- Quando una decisione nuova vale anche per il futuro, scrivila nel file di regole del suo tema (`docs/File di regole/`)
  oltre che nella memoria: i file sono versionati e li leggono anche il collega e gli altri agenti.
- Credenziali: non chiedere, ricevere o scrivere chiavi AWS, la chiave di Google Maps o password reali. Nei test si usano
  solo i valori di prova del progetto (mock).
- Il sito di riferimento `devhub.summerbooking.it` si usa solo in lettura; l'accesso lo fa l'utente.
- Il server dell'utente su `localhost:4200` non si avvia, non si ferma e non si cita: per le verifiche c'è la porta 4300.
- Dal campione del database del backend si prende solo la struttura; i valori dei mock sono sempre inventati.

## Comandi

Dalla cartella `frontend/summer-booking-front-office/`:

- Controllo dei tipi: `npx tsc -p tsconfig.app.json --noEmit`
- Lint: `npm run lint`
- Test: `npx ng test --watch=false`
- Build: `npx ng build --configuration development`
- Tutto insieme prima della consegna: `npm run verify`
- Prettier solo sui file modificati: `npx prettier --write --end-of-line auto <file>` (mai su tutta `src/app`)
- Server con i mock: `npm start`; con il backend locale: `npm run start:backend`

## Verifica nel browser

- Avvia la configurazione `front-office-check` di `.claude/launch.json` (porta 4300) e aspetta che risponda.
- Sessione finta con i mock: in `localStorage` metti `sb.mock.sb_rt` (oggetto con `refreshToken`, `remember: true` e
  `expiresAt`), `sb.signed-in` = `'true'` e `sb.language`, poi apri la pagina due volte.
- Controlla a 1280×720 e a 412px di larghezza; per lingua e RTL anche in arabo.
- Alla fine togli `sb.mock.sb_rt`, `sb.signed-in`, `sb.session-name` e `sb.language`, torna alla misura desktop e ferma
  il server.

## Strumenti utili

- `/code-review` per rivedere una modifica, `/security-review` per la sicurezza, `/simplify` per ripulire il codice.
- Agenti in parallelo solo per lavori grandi e separabili (es. leggere o riscrivere molti documenti).
- I documenti delle API (`docs/API_CONTRACTS.md`, `shared documentation/API_CONTRACTS.docx`,
  `shared documentation/API - Full list.md`) e il catalogo dei wrapper (`docs/WRAPPER_COMPONENTS.docx`) si rigenerano
  con script Python che oggi non sono nel repository.
