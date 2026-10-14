# Server MCP — dove vivono, come si integrano

> Se e quando il backend deve esporre proprie funzionalità come **tool MCP** (Model Context Protocol) a
> client AI esterni (Claude Desktop, un IDE agent, un'automazione interna basata su LLM), questo
> documento risponde alla domanda: **il codice del tool va nel dominio a cui appartiene, o
> centralizzato in INFRA?** — e descrive come integrare un server MCP in questo backend **.NET 10**
> modulare senza rompere i confini già stabiliti.
>
> Complemento a [pre/linee-guida-implementative-dotnet.md §5, §6, §10](pre/linee-guida-implementative-dotnet.md)
> (confini tra moduli, comunicazione mai HTTP tra moduli, pattern "meccanismo nell'host / contenuto nel
> modulo" già usato per Swagger), [pre/autenticazione-autorizzazione-dotnet.md §3, §7](pre/autenticazione-autorizzazione-dotnet.md)
> (schema `ApiBearer`, client M2M key/secret), [35-integrazioni-esterne.md](35-integrazioni-esterne.md)
> (stesso principio contratto-astratto/implementazione-concreta, qui applicato al lato **server** invece
> che client), [26-chatbot.md §8](26-chatbot.md) (perché un tool ha parametri tipizzati, mai una query
> libera generata dal modello — principio esteso qui a tutta l'esposizione MCP), [34-multitenant.md §1.1](34-multitenant.md)
> (scoping dal contesto/token, mai dalla richiesta), [20-jobs.md](20-jobs.md) (outbox per gli effetti
> collaterali), [31-observability.md §3.2](31-observability.md) (metriche di dominio) e
> [17-secrets.md](17-secrets.md) (dove registrare eventuali credenziali dei client MCP).
>
> **Documento vivo**: aggiungere una riga alla tabella §10 per ogni nuovo tool esposto o client MCP
> registrato.

---

## 0. Risposta diretta

**Nel dominio di competenza, non in INFRA.** Un tool MCP è un caso d'uso applicativo esposto con un
protocollo diverso dal REST — esattamente come un endpoint minimal API, chiama solo l'**Application**
del proprio modulo e non conosce nulla degli altri domini. Accentrarli in `Module.Infra` significherebbe
far dipendere INFRA dall'Application di Booking, Catalog, Payment, ecc.: la stessa violazione che la
regola "i moduli non si referenziano tra loro" ([linee-guida §5, §6](pre/linee-guida-implementative-dotnet.md))
vieta esplicitamente per qualunque altra comunicazione cross-modulo.

`Module.Infra` resta proprietario di una cosa sola e specifica, riusata da questo documento: il
**registro dei client M2M** (`ApiClient` / `IntegrationApp`, già scaffoldato — è l'ex `keysecret`),
perché quello *è* per natura trasversale a tutte le integrazioni esterne, incluse quelle MCP (§5).

---

## 1. Cos'è, in questo contesto

MCP (Model Context Protocol) è un protocollo JSON-RPC con cui un'applicazione espone a un client AI
(un LLM con supporto al tool-calling) un insieme di **tool** (funzioni tipizzate invocabili), **resource**
(dati leggibili) ed eventualmente **prompt** (template riusabili). Il trasporto rilevante per un backend
web è **Streamable HTTP**: un endpoint HTTP che il client chiama per elencare/invocare i tool.

Due scenari distinti, da non confondere:

| Scenario | Chi chiama chi | Dove si tratta |
|---|---|---|
| **Questo documento** — il backend espone tool MCP verso l'esterno (Claude Desktop, un agente di automazione interno, un IDE agent che assiste chi sviluppa/gestisce la piattaforma) | Un client AI esterno → il backend (il backend è **server MCP**) | qui |
| Il modulo **Chatbot** (C8) usa un LLM con **function calling** per rispondere agli utenti dell'app gestionale | Il modulo Chatbot → un provider LLM esterno (il backend è **client** del provider, non server MCP) | [26-chatbot.md](26-chatbot.md) |

Sono complementari, non alternativi: il Chatbot gira **in-process** nel backend, quindi i suoi tool
restano normali chiamate all'Application del modulo Booking/Catalog (nessun bisogno del protocollo MCP,
che serve per attraversare un confine di processo/rete verso un client esterno). Se in futuro serve che
il Chatbot stesso "senta" il layer MCP (es. per riusare la stessa lista di tool esposta agli agenti
esterni), può consumare gli **stessi contratti Application** dei tool MCP — non il protocollo MCP in sé.

---

## 2. Principio: la stessa regola già usata per Swagger

[linee-guida-implementative-dotnet.md §10](pre/linee-guida-implementative-dotnet.md) risolve un problema
identico per Swagger — "centinaia di endpoint, un documento per dominio" — con **meccanismo nell'host,
contenuto nel modulo**. Stessa spartizione per MCP:

| Cosa | Dove | Perché |
|---|---|---|
| Bootstrap del server MCP (SDK, endpoint HTTP `/mcp`, discovery degli assembly) | **Boot** (host) | Infrastruttura trasversale, non contenuto — nessun modulo deve sapere *come* viene esposto via MCP |
| Schema/policy di autenticazione MCP | **Boot** (host) | Stesso posto degli schemi `UserBearer`/`ApiBearer` ([autenticazione-autorizzazione-dotnet.md §4](pre/autenticazione-autorizzazione-dotnet.md)) |
| Definizione dei singoli **tool** (nome, schema input/output, chiamata all'Application) | **modulo proprietario della capability** (`Module.Booking`, `Module.Catalog`, …) | Stesso motivo per cui gli endpoint REST del modulo Booking vivono in `Module.Booking/Endpoints`, non in un modulo API centrale |
| Registro dei **client MCP** autorizzati (chi può connettersi, con quale scope) | **`Module.Infra`** | È la stessa cosa che già fa per gli altri client M2M (`ApiClient`/`IntegrationApp`) — un registro, non della logica di dominio |

Non esiste (e non va creato) un `Module.Mcp` che aggrega la logica applicativa di più domini: sarebbe
l'equivalente di un modulo che raccoglie tutti i controller REST di tutti i domini — la ragione stessa
per cui il progetto è modulare per dominio.

---

## 3. Layout dentro un modulo

Accanto agli `Endpoints/` REST già previsti da [linee-guida-implementative-dotnet.md §2](pre/linee-guida-implementative-dotnet.md):

```
Module.Booking/
├── Application/
│   ├── Abstractions/
│   ├── Services/
│   │   └── AvailabilityService.cs          // stesso caso d'uso richiamato da REST e da MCP
│   └── Dtos/
│       └── AvailabilityQuery.cs / AvailabilityResult.cs
├── Endpoints/
│   ├── Endpoints.cs                         // minimal API REST esistenti
│   └── Mcp/
│       └── BookingMcpTools.cs               // tool MCP — stesso Application, altro trasporto
└── Module.cs
```

```csharp
// Module.Booking/Endpoints/Mcp/BookingMcpTools.cs
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Module.Booking.Endpoints.Mcp;

[McpServerToolType]
public sealed class BookingMcpTools(AvailabilityService availabilityService)
{
    [McpServerTool(Name = "booking.search_availability")]
    [Description("Cerca la disponibilità di ombrelloni/postazioni per uno stabilimento in un intervallo di date.")]
    public async Task<AvailabilityResult> SearchAvailability(
        AvailabilityQuery query, CancellationToken ct)
        => await availabilityService.SearchAsync(query, ct);   // stesso Application degli endpoint REST
}
```

Vale **la stessa regola** già imposta dagli `ArchitectureTests` sugli `Endpoints` REST
([ModuleConventionArchitectureTests.cs](../../../backend/summerbooking-backend/ArchitectureTests/ModuleConventionArchitectureTests.cs)):
un tool non referenzia `Module.X.Infrastructure` né restituisce entità di dominio, solo DTO
dell'Application. Le regole esistenti coprono già `Endpoints/Mcp/` perché filtrano per
`ResideInNamespaceStartingWith($"{modulo}.Endpoints")` — nessuna modifica richiesta a quel file quando
si aggiunge la sottocartella.

Nome del tool: **prefisso di dominio** (`booking.search_availability`, non `search_availability`) — sia
per leggibilità lato client AI (che vede tool di moduli diversi in un elenco unico), sia perché rende
visibile a colpo d'occhio, nel registro §10, a quale modulo appartiene.

---

## 4. Registrazione (host + modulo)

Nel progetto **Boot** (composition root, come per Swagger e per gli schemi di autenticazione):

```csharp
// Boot/Program.cs
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(typeof(Module.Booking.Endpoints.Mcp.BookingMcpTools).Assembly)
    .WithToolsFromAssembly(typeof(Module.Catalog.Endpoints.Mcp.CatalogMcpTools).Assembly);
    // un .WithToolsFromAssembly per ogni modulo che espone tool — stessa lista di assembly già
    // referenziati da Boot per DI/endpoint REST, nessun nuovo riferimento di progetto

// ...

app.MapMcp("/mcp").RequireAuthorization("Mcp");   // policy dedicata, vedi §5
```

`Module.cs` di ogni modulo **non cambia**: continua a registrare solo Application/Infrastructure/DbContext
del modulo (`AddBookingModule()`); i tool sono scoperti per **assembly scanning** dall'SDK MCP lato host,
stesso principio con cui Swagger raccoglie i group name senza che i moduli sappiano di Swagger.

> SDK: pacchetti NuGet ufficiali `ModelContextProtocol` + `ModelContextProtocol.AspNetCore`
> (Model Context Protocol C# SDK, sviluppato con Anthropic). Verificare la versione corrente al momento
> dell'implementazione: è un SDK relativamente giovane, le API di registrazione (`AddMcpServer`,
> `WithHttpTransport`, `MapMcp`) sono quelle stabilizzate alla stesura di questo documento ma vanno
> confermate contro il changelog del pacchetto.

---

## 5. Autenticazione e autorizzazione

Il caso d'uso iniziale è **M2M**: un'automazione interna o un agente aziendale che chiama il backend per
conto del sistema, non per conto di un singolo utente finale che si logga interattivamente. Per questo
si riusa **integralmente** lo schema già previsto per i client esterni
([autenticazione-autorizzazione-dotnet.md §3, §7](pre/autenticazione-autorizzazione-dotnet.md)):

- Il client MCP **scambia key/secret per un JWT** (`ApiBearer`, stesso endpoint `external/token` già
  previsto per gli altri consumer M2M) e lo passa come header `Authorization` sulle chiamate HTTP al
  server MCP.
- Policy dedicata sull'host, non una nuova rispetto a quella esistente — stessa forma di `ExternalApi`,
  eventualmente con un claim/scope in più per distinguere "può chiamare l'API REST" da "può chiamare
  tool MCP":
  ```csharp
  options.AddPolicy("Mcp", p => p
      .AddAuthenticationSchemes("ApiBearer")
      .RequireAuthenticatedUser()
      .RequireClaim("scope", "mcp"));
  ```
- **Registro dei client**: riusa `Module.Infra.ApiClient`/`IntegrationApp` — aggiungere i valori
  necessari all'enum (es. `IntegrationApp.McpAutomation`, uno per agente/integrazione se servono scope
  differenziati), esattamente come oggi per `OfSale`/`Transactions`/`TeamSystem`. Non serve una tabella
  nuova: è lo stesso concetto ("chi può autenticarsi come M2M verso di noi"), un'altra riga di
  vocabolario.

### 5.1 Nota sullo standard — delega utente (non serve ora)

La specifica MCP raccomanda, per i transport HTTP remoti, **OAuth 2.1** con discovery
dell'authorization server (RFC 8414/9728): pensato per il caso in cui un agente agisce **per conto di un
utente umano** che deve dare consenso esplicito (es. un manager che collega Claude al proprio pannello e
autorizza l'accesso ai *suoi* dati). È un flusso diverso da client_credentials — introduce consenso,
token legati all'utente, scoping per singolo utente invece che per client applicativo.

**Non serve implementarlo ora**: il primo uso previsto è M2M (automazioni/agenti interni), coperto da
`ApiBearer` come sopra. Se in futuro emerge il requisito "un utente finale/manager collega un client AI
di terzi al proprio account", va introdotto come nuovo flusso OAuth 2.1 dedicato — coerente con
l'approccio già adottato altrove nel progetto (`UserBearer`/`ApiBearer` come schemi separati, mai un
meccanismo unico forzato a coprire casi diversi) — non serve prevederlo architetturalmente adesso.

### 5.2 Scoping: dal token, mai dal parametro del tool

Stessa regola di [34-multitenant.md §1.1](34-multitenant.md) (tenant dal contesto, non dalla richiesta),
**ancora più importante qui**: il "chiamante" di un tool è un LLM che genera i parametri a partire da un
prompt — non va mai fidato per determinare *quale* stabilimento/tenant sta interrogando. Lo
`property_id`/scope va letto dal claim del token del client MCP (o, nello scenario §5.1, dal token
utente-delegato), mai da un campo `propertyId` passato nell'input del tool.

---

## 6. Cosa un tool può fare (e cosa no)

- **Parametri tipizzati**, JSON Schema generato dal DTO dell'Application (l'SDK MCP lo genera dai tipi
  .NET del metodo `[McpServerTool]`) — mai un parametro libero tipo `filter: string` interpretato come
  query. È lo stesso principio già scritto per il Chatbot
  ([26-chatbot.md §8](26-chatbot.md)) a proposito del tool di ricerca dati rimosso dal refactoring:
  l'LLM sceglie **quale tool** e **quali valori per parametri predefiniti**, mai una condizione/query
  generata liberamente ed eseguita lato server.
- **Un tool = un caso d'uso Application esistente** (o nuovo, ma comunque un caso d'uso vero, non un
  wrapper 1:1 su una singola query SQL): stessa logica, stessa validazione, stessa autorizzazione che
  avrebbe l'equivalente endpoint REST — il tool è un altro modo di **arrivarci**, non un secondo layer
  con regole proprie.
- **Operazioni non idempotenti** (es. un tool che crea una prenotazione): `idempotencyKey` deterministica
  nel contratto, stessa regola di [35-integrazioni-esterne.md §3](35-integrazioni-esterne.md) — un
  agente può ripetere una tool call (retry, o semplicemente il modello che "prova di nuovo").
- **Effetti collaterali** (notifica, email) dopo una tool call critica → **outbox** ([20-jobs.md](20-jobs.md)),
  mai eseguiti in linea nella risposta del tool.
- **Operazioni distruttive/sensibili** (cancellazioni, refund, modifiche di configurazione): valutare se
  esporle affatto come tool MCP, o solo in lettura — un tool è per definizione richiamabile da un
  processo che decide autonomamente *quando* chiamarlo.

---

## 7. Audit e osservabilità

- Ogni tool call è **loggata**: `client_id` (o utente delegante, §5.1), nome tool, parametri (redatti se
  contengono dati sensibili — [15-logs.md](15-logs.md)), esito, latenza.
- Metrica per tool (conteggio, tasso d'errore, latenza) come le altre **metriche di dominio**
  ([31-observability.md §3.2](31-observability.md)), taggata almeno per nome tool e modulo — non per
  parametro (cardinalità, stessa regola §3.3 di quel documento).
- **Rate limit per client**: stesso concetto già presente per il Chatbot
  ([26-chatbot.md §6](26-chatbot.md), lì per stabilimento) applicato qui per client MCP — un agente con
  un prompt storto può generare un volume di chiamate anomalo in pochi secondi.

---

## 8. Test

- Gli `ArchitectureTests` esistenti (§3) coprono già `Endpoints/Mcp/` senza modifiche, essendo
  parametrizzati per modulo e basati su prefisso di namespace.
- Integrazione: il server MCP si testa come un endpoint HTTP in più, con lo stesso approccio delle altre
  API ([02-strategia-di-test.md](02-strategia-di-test.md)) — qui il backend è il **server**, non il
  client, quindi non c'è un provider esterno da stubbare con WireMock.Net (quel pattern resta per le
  integrazioni **uscenti**, [35-integrazioni-esterne.md](35-integrazioni-esterne.md)).
- Un test che invoca il tool MCP end-to-end (list tools → call tool) e verifica che risponda con lo
  stesso risultato dell'endpoint REST equivalente, quando esiste in parallelo.

---

## 9. Checklist per un nuovo tool MCP

- [ ] Vive in `Module.<Dominio>/Endpoints/Mcp/`, **non** in `Module.Infra`.
- [ ] Nome con prefisso di dominio (`<dominio>.<azione>`).
- [ ] Input/output: DTO dell'Application del modulo, mai entità di dominio.
- [ ] Chiama un caso d'uso Application esistente (o ne crea uno vero, non un wrapper su query diretta).
- [ ] Autenticazione: policy `Mcp` (schema `ApiBearer`, o il futuro flusso utente-delegato §5.1 se
      applicabile).
- [ ] Scoping (`property_id`/tenant) letto dal **token**, mai da un parametro del tool.
- [ ] Se non idempotente: `idempotencyKey` deterministica nel contratto.
- [ ] Effetti collaterali via **outbox**, non in linea.
- [ ] Log/audit della chiamata + metrica di dominio.
- [ ] Rate limit per client verificato/dimensionato.
- [ ] Client MCP registrato in `Module.Infra.ApiClient` con lo scope corretto.
- [ ] Riga aggiunta alla tabella tool §10.

---

## 10. Registro (documento vivo)

### Tool esposti

| Tool | Modulo | Scope richiesto | Stato |
|---|---|---|---|
| *(nessuno ancora — Application/Endpoints dei moduli non sono ancora implementati, solo la persistenza è scaffoldata)* | | | |

### Client MCP registrati (`Module.Infra.ApiClient` / `IntegrationApp`)

| Client | `IntegrationApp` | Scope | Note |
|---|---|---|---|
| *(nessuno ancora)* | | | |
