# Collaborazione

Come si lavora con l'utente e dove stanno i documenti del frontend, da tenere aggiornati.

## A cosa servono questi file

- I file di `docs/File di regole/` raccolgono le decisioni operative, architetturali e SEO da rispettare nel lavoro sul
  frontend, così che un altro agente possa proseguire il progetto con le stesse regole. Si parte da `00-indice.md`.
- Non descrivono lo scaffolding iniziale, già completato.
- Le descrizioni delle pagine (cosa fa ogni pagina, non regole) stanno in `docs/Pagine/`.

## Modo di lavorare

- Si procede rigorosamente per piccoli passi, sotto la guida dell'utente: un blocco della pagina alla volta.
- Non si anticipano elementi, funzionalità o contenuti non ancora richiesti: se l'utente chiede solo header e logo, non
  si aggiungono menu, pulsanti, selettori della lingua o altro.
- Nel dubbio si chiede sempre all'utente prima di una decisione che possa cambiare il risultato, l'architettura o la
  tecnologia.
- Quando serve un'autorizzazione si spiega in 3-4 righe al massimo cosa farà l'operazione, perché serve e quali file o
  parti del progetto toccherà: non si mostra soltanto il comando tecnico.
- Immagini, loghi, icone, font e altri asset si chiedono all'utente. Si usano gli asset locali forniti, senza
  sostituirli arbitrariamente e senza dipendere dal sito di riferimento a runtime.
- Di un sito di riferimento si analizzano layout, responsive e comportamenti richiesti, senza usare pulsanti o link non
  autorizzati.
- Il riferimento si replica con confronti visivi alle stesse dimensioni, desktop e mobile: non bastano le stime.
- Si modificano solo i file pertinenti alla richiesta: le modifiche già presenti si conservano e le aree non correlate
  non si toccano.
- Niente commit, push o operazioni Git distruttive senza una richiesta esplicita. Prima di un commit si verifica che ci
  siano solo modifiche pertinenti e nessun file generato, locale o sensibile.
- Quando il contesto della chat diventa troppo esteso e può compromettere precisione o continuità, si avvisa l'utente in
  anticipo e gli si consiglia chiaramente di aprire una nuova chat, che riparte da `docs/File di regole/00-indice.md`.

## Documentazione

- Descrive solo lo stato attuale: niente storia, date, provenienza, vecchi nomi o rotte, note come "prima era" o
  changelog. La storia la tiene git.
- I documenti si aggiornano nello stesso passo della modifica che li riguarda: una regola nuova o cambiata va nel file
  di regole del suo tema; contratti delle API: vedi `api-e-mock.md`; catalogo dei wrapper `WRAPPER_COMPONENTS.docx`:
  vedi `componenti-e-wrapper.md`.
- I documenti del frontend (`.md` e `.docx`) stanno in `docs/`, non nella radice del progetto: le regole in
  `docs/File di regole/`, le descrizioni delle pagine in `docs/Pagine/`.
- `.gitignore` esclude i `.md` e i `.docx` che stanno direttamente in `docs/`, tranne quelli versionati, elencati come
  eccezioni: `docs/API_CONTRACTS.md`, `docs/README_DEPLOY.md`, `docs/SECURITY_TODO.md`, `docs/SEO_OPTIMIZATION_TODO.md`
  e `docs/MESSAGE_FORMAT.docx`. Un nuovo documento da versionare va aggiunto a quelle eccezioni.
- Le cartelle `docs/File di regole/` e `docs/Pagine/` sono versionate.
- Il README di deploy (`docs/README_DEPLOY.md`) contiene soltanto istruzioni di pubblicazione chiare, senza riferimenti
  locali.
- I documenti condivisi con il backend stanno in `shared documentation/`, alla radice del repository (fuori dal progetto
  Angular), e sono versionati. Tra questi: `API_CONTRACTS.docx` (tutte le API), `AUTH_API_CONTRACTS.docx` (contratto
  breve di accesso, rinnovo e uscita) e `AWS_CODEPIPELINE_GITHUB_SETUP.md` (pipeline AWS collegata a GitHub).
