# Module.Chatbot — assistente AI per l'app gestionale

> Assistente conversazionale rivolto agli utenti di stabilimento (app gestionale), che risponde a
> domande sulla piattaforma appoggiandosi a un **modello LLM intercambiabile** e a una **base di
> conoscenza documentale** (RAG). Sorgente legacy: [`front-chatbot/`](../../../../../back-chatbot/), branch
> separato non ancora integrato in `back-pre`/`backend/` — i controller utili sono in
> [`app/controllers/chatbotManager.controller.js`](../../../../../back-chatbot/app/controllers/chatbotManager.controller.js),
> il resto del codice di quella cartella è superato e va ignorato.
>
> Complemento a [35-integrazioni-esterne.md §1](35-integrazioni-esterne.md) (pattern
> contratto-astratto/implementazione-concreta per vendor esterni, applicato qui al provider LLM),
> [pre/linee-guida-implementative-dotnet.md §8-9](pre/linee-guida-implementative-dotnet.md) (dove
> vivono template e localizzazione), [17-secrets.md](17-secrets.md) (dove va registrata la chiave API
> del provider) e [03-gestione-dati.md §3.1bis](03-gestione-dati.md) (convenzione di naming tabelle per
> dominio, qui **C8 — Chatbot**).
>
> **Nota**: questo documento descrive il modulo per come deve essere portato nel nuovo backend. La
> vecchia funzionalità che interrogava un archivio esterno di prenotazioni storiche **non fa parte del
> refactoring** ed è trattata solo al §8 per motivare la rimozione.

---

## 0. Cos'è, a chi si rivolge

Un assistente conversazionale integrato nell'app gestionale che aiuta l'utente di stabilimento a:

1. capire **come si usa la piattaforma** (domande guida/how-to: "Come faccio a...", "Dove trovo...");
2. in prospettiva, interrogare **dati operativi correnti** tramite tool dedicati e tipizzati (non nel
   perimetro di questo documento: vedi §8 per cosa non riproporre).

Il modello non ha memoria conversazionale tra un turno e l'altro (ogni chiamata è stateless: la
cronologia viene **salvata** ma non viene reinviata come contesto al turno successivo — vedi §9).

---

## 1. Flusso end-to-end (stato legacy)

| Endpoint legacy | File | Cosa fa |
|---|---|---|
| `POST /api/sb/chatbot/chat` | [`chatbotManager.controller.js`](../../../../../back-chatbot/app/controllers/chatbotManager.controller.js) → [`chatbotManagerService.js`](../../../../../back-chatbot/app/Service/chatbotManagerService.js) | Valida il messaggio, verifica il rate limit giornaliero per stabilimento, costruisce il prompt di sistema, invoca il provider LLM (con eventuale round-trip di tool calling), salva la coppia domanda/risposta, restituisce la risposta |
| `POST /api/sb/chatbot/history` | stessi file, funzione `getHistory` | Restituisce lo storico delle conversazioni per lo stabilimento dell'utente autenticato, limitato da configurazione |

Entrambi gli endpoint richiedono utente autenticato (JWT) e sono associati allo stabilimento
dell'utente (`propertyId` derivato da `req.userId`, mai passato dal client).

---

## 2. Provider LLM intercambiabile

Requisito esplicito per il nuovo modulo: **il modello LLM usato deve poter cambiare** (per
aggiornamento del provider, confronto costi/qualità, o deprecazione di un modello) **senza toccare la
logica applicativa**.

Il legacy implementa già l'idea con un **factory pattern** ([`llmFactory.js`](../../../../../back-chatbot/app/utils/llmFactory.js)):
una variabile d'ambiente (`CHATBOT_MANAGER_LLM`) seleziona per nome una tra le implementazioni
registrate (oggi due varianti Gemini), ciascuna sottoclasse di una base comune
([`llmManagerBase.helper.js`](../../../../../back-chatbot/app/utils/llmManagerBase.helper.js)) che espone
`generateContent(userMessage, tools)`.

Nel nuovo modulo va applicato lo stesso principio di [35-integrazioni-esterne.md §1](35-integrazioni-esterne.md):

- **Contratto astratto** in `Chatbot/Application/Abstractions` — `IChatCompletionProvider` (o simile),
  espresso in termini di dominio (messaggio utente, system prompt, tool disponibili → risposta testuale
  + eventuali tool call), **senza** riferimenti all'SDK del vendor.
- **Implementazione concreta** in `Chatbot/Infrastructure/ExternalApi/` per ciascun provider (Gemini
  oggi; in futuro OpenAI, Claude, un modello self-hosted, ...), una classe per vendor/modello.
- **Selezione del provider tramite configurazione** (`appsettings`/variabile d'ambiente, come oggi),
  non hardcoded. Da valutare se rendere l'override **per stabilimento**, estendendo la configurazione
  del §6 con un campo `llm_provider` — oggi il nome del modello è passato al factory ma non è
  persistito per property.
- Cambiare provider deve restare un cambio **solo nell'Infrastructure** del modulo, mai
  nell'Application né negli endpoint (stesso beneficio di testabilità/sostituibilità già descritto in
  [35-integrazioni-esterne.md §1.3](35-integrazioni-esterne.md)).

---

## 3. Ricerca sulla documentazione (RAG)

Le risposte alle domande guida si appoggiano a uno **store di ricerca documentale esterno** (nel
legacy, il file search del provider Gemini, referenziato per nome tramite configurazione), popolato con
la documentazione utente della piattaforma.

Va tenuto **disaccoppiato dalla scelta del provider LLM**: è un secondo tool esterno, con un proprio
contratto astratto (`IKnowledgeSearchProvider` o incorporato nello stesso `IChatCompletionProvider` se
il provider LLM lo offre nativamente), così è possibile cambiare motore di generazione e base
documentale in modo indipendente.

---

## 4. Prompt di sistema e regole di comportamento

Il legacy definisce un prompt di sistema con regole esplicite (oggi in
[`llmManagerBase.helper.js`](../../../../../back-chatbot/app/utils/llmManagerBase.helper.js)):

- risposte **fattuali**, nessuna invenzione; se l'informazione non è nota, dichiararlo;
- lingua di risposta determinata dalla lingua rilevata nel messaggio dell'utente (vedi §5);
- **testo semplice**, niente Markdown (il frontend gestionale non lo renderizza);
- **rifiuto di argomenti fuori tema** (non pertinenti alla piattaforma), con messaggio predefinito
  bilingue;
- le istruzioni di sistema **non sono sovrascrivibili** da input dell'utente;
- i riferimenti a immagini nella documentazione (placeholder con un identificativo univoco) vanno
  riportati **carattere per carattere**, poi riscritti in un formato consumato dal frontend per
  mostrare l'immagine — da verificare con il team frontend se questo meccanismo va mantenuto tale e
  quale o sostituito da un riferimento immagine standard lato API.

Per [pre/linee-guida-implementative-dotnet.md §8](pre/linee-guida-implementative-dotnet.md) (template
vivono nell'Infrastructure del modulo che li usa), il prompt di sistema va trattato come **risorsa
versionata nell'Infrastructure del modulo Chatbot**, non come stringa sparsa nel codice applicativo —
così resta modificabile e testabile senza toccare la logica di orchestrazione. Se in futuro serve
personalizzarlo per stabilimento, la stessa regola indica **DB** come collocazione (per-property,
editabile da pannello), non file.

---

## 5. Lingua della risposta

Il legacy rileva la lingua **indovinandola** da un elenco di parole chiave nel messaggio dell'utente
(default italiano se nessuna corrispondenza). È un'euristica fragile e già ridondante: l'utente ha una
lingua preferita nota (`languageCode`, usata ad esempio per i messaggi di rate limit — vedi §6). Nel
nuovo modulo, seguendo [pre/linee-guida-implementative-dotnet.md §9](pre/linee-guida-implementative-dotnet.md),
la lingua di risposta dovrebbe derivare dalla **preferenza utente già nota** (o da
`Accept-Language`/cultura corrente), non da un'euristica sul testo del singolo messaggio.

---

## 6. Rate limiting e configurazione per stabilimento

Ogni stabilimento ha una configurazione dedicata (creata al primo utilizzo se assente, con valori di
default):

- **`maxCallPerDay`**: numero massimo di richieste al chatbot consentite per giorno solare, per
  stabilimento. Oltre soglia, l'endpoint `chat` risponde con un messaggio di limite raggiunto
  (localizzato IT/EN) invece di invocare il provider LLM.
- **`maxReturnedHistory`**: numero massimo di voci di storico restituite da `history` (il chiamante può
  chiederne meno, mai di più).

Il conteggio delle chiamate odierne è oggi una `COUNT` sullo storico filtrato per data di
aggiornamento — da rivalutare in ottica performance/concorrenza nel nuovo modulo (contatore
incrementale o cache, invece di una query aggregata ad ogni richiesta).

---

## 7. Persistenza

Due tabelle, già censite (con dati reali di produzione) in `analisi-pre/db/` in attesa di essere
formalizzate nello schema del dominio **C8**:

- [`chatbot_history`](../analisi-pre/db/chatbot_history.md): una riga per ogni scambio
  domanda/risposta (`propertyId`, testo utente, testo risposta, l'app di provenienza — oggi solo
  quella gestionale, il valore per un futuro canale lato cliente finale è già previsto ma non
  utilizzato —, autore, timestamp).
- [`chatbot_manager_config`](../analisi-pre/db/chatbot_manager_config.md): una riga per stabilimento
  con i limiti del §6.

Nel passaggio dedicato alle tabelle del dominio C8, andranno rinominate secondo la convenzione di
[03-gestione-dati.md §3.1bis](03-gestione-dati.md) (schema `chatbot`, nomi al plurale senza prefisso di
dominio ridondante, es. `chatbot.history`, `chatbot.manager_configs`).

---

## 8. Cosa non entra nel refactoring

Il codice legacy include anche uno strumento (function calling) che permetteva al modello di
interrogare un **archivio esterno di prenotazioni di una stagione passata**, non più in uso: il modello
generava in linguaggio naturale una condizione di filtro, il backend la ripuliva con una lista di
pattern/parole chiave vietate e la eseguiva come clausola `WHERE` su una connessione dedicata e
in sola lettura a quell'archivio.

**Questa funzionalità non va portata nel nuovo modulo.** Va rimosso di conseguenza:

- la dichiarazione del tool di interrogazione dati e la relativa gestione del round-trip di function
  calling nell'orchestrazione della risposta;
- la connessione secondaria al database e le sue credenziali dedicate;
- la logica di sanitizzazione della condizione generata dal modello.

Motivazione, utile anche come principio per l'eventuale futuro tool su dati operativi reali (§0 punto
2): generare un filtro/query libera tramite LLM e ripulirla con una lista di pattern vietati resta un
approccio fragile, indipendentemente da quanto accurata sia la sanitizzazione. Se in futuro il chatbot
deve accedere a dati operativi (es. prenotazioni correnti), l'accesso va esposto come tool con
**parametri tipizzati** su un set predefinito di query/filtri (o richiamando l'Application layer del
modulo proprietario dei dati, es. Booking), mai come `WHERE` libera costruita dal modello.

---

## 9. Punti aperti per il nuovo modulo

- **Contesto conversazionale**: oggi ogni messaggio è processato senza la cronologia dei turni
  precedenti (salvata ma non reinviata al modello) — valutare se serve un contesto multi-turno reale.
- **Log applicativi**: il legacy logga in chiaro l'intera risposta del chatbot a ogni chiamata (utile in
  fase di sviluppo, da rimuovere/ridurre a debug mirato in produzione — vedi [15-logs.md](15-logs.md)).
- **Canale lato cliente finale**: lo storico prevede già un valore per un'app diversa da quella
  gestionale, mai implementata — da chiarire se rientra nello scope del nuovo modulo o resta fuori.
- **Segreti**: chiave API del provider LLM e nome dello store di ricerca documentale vanno registrati e
  gestiti secondo [17-secrets.md](17-secrets.md) (oggi solo variabili d'ambiente sul branch separato,
  non ancora censite in quel documento).
- **Override del provider per stabilimento**: vedi §2 — da decidere se necessario o se un'unica
  configurazione globale è sufficiente.

---

## 10. File legacy di riferimento (`front-chatbot/`, branch a parte)

| File | Ruolo |
|---|---|
| [`app/controllers/chatbotManager.controller.js`](../../../../../back-chatbot/app/controllers/chatbotManager.controller.js) | Endpoint HTTP: validazione richiesta, codici di stato, delega al service |
| [`app/Service/chatbotManagerService.js`](../../../../../back-chatbot/app/Service/chatbotManagerService.js) | Orchestrazione: rate limit, configurazione, chiamata al provider LLM, storico |
| [`app/utils/llmFactory.js`](../../../../../back-chatbot/app/utils/llmFactory.js) | Factory di selezione del provider LLM da configurazione |
| [`app/utils/llmManagerBase.helper.js`](../../../../../back-chatbot/app/utils/llmManagerBase.helper.js) | Base comune: prompt di sistema, rilevamento lingua, dichiarazione dei tool |
| [`app/utils/llmManagerGemini3Flash.helper.js`](../../../../../back-chatbot/app/utils/llmManagerGemini3Flash.helper.js), [`app/utils/llmManagerGemini2FlashLite.helper.js`](../../../../../back-chatbot/app/utils/llmManagerGemini2FlashLite.helper.js) | Implementazioni concrete per i due modelli oggi supportati |
| [`app/models/dto/chatbotRequest.dto.js`](../../../../../back-chatbot/app/models/dto/chatbotRequest.dto.js), [`app/models/dto/chatbotResponse.dto.js`](../../../../../back-chatbot/app/models/dto/chatbotResponse.dto.js) | Validazione input e forma delle risposte |
| [`app/models/chatbotHistory.model.js`](../../../../../back-chatbot/app/models/chatbotHistory.model.js), [`app/models/chatbotManagerConfig.model.js`](../../../../../back-chatbot/app/models/chatbotManagerConfig.model.js) | Modelli Sequelize delle due tabelle (§7) |
