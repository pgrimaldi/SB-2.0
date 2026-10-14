# Standard di codifica del backend .NET

> Convenzioni di scrittura del codice per `backend/summerbooking-backend`, ricavate dal codice **già
> presente** nella solution (`Module.ZTest` come modulo di riferimento, `ArchitectureTests` come regole
> eseguibili) e dalle decisioni già prese nei documenti collegati. Questo documento **non ripete**
> l'architettura (vedi [linee-guida-implementative-dotnet.md](pre/linee-guida-implementative-dotnet.md)),
> il formato delle risposte API (vedi [standard-risposta-api-dotnet.md](pre/standard-risposta-api-dotnet.md))
> o la strategia di test (vedi [02-strategia-di-test.md](02-strategia-di-test.md)): raccoglie le regole di
> **stile e struttura a livello di singolo file/classe** che un nuovo modulo deve rispettare per essere
> indistinguibile da `Module.ZTest`.
>
> Riferimento normativo, non descrittivo: dove il codice esistente diverge da questo documento, è il codice
> a dover essere corretto (o il documento ad andare aggiornato esplicitamente, con motivazione).

---

## 1. Formattazione

- **Indentazione a tab**, non spazi — coerente con tutti i file `.cs` già presenti.
- **File-scoped namespace** (`namespace Module.ZTest.Domain.Entities;`), mai il blocco con graffe.
  L'unica eccezione tollerata è codice-scheletro temporaneo non ancora sviluppato (es. lo stub
  `Module.Auth/Infrastructure/DbContext.cs`): va convertito a file-scoped non appena il file prende contenuto.
- `<ImplicitUsings>enable</ImplicitUsings>` e `<Nullable>enable</Nullable>` in ogni `.csproj` — zero warning
  ammessi su entrambi (gate CI, vedi [02-strategia-di-test.md §15](02-strategia-di-test.md)).
- `dotnet format --verify-no-changes` pulito prima di ogni PR.
- Un tipo pubblico per file, nome del file = nome del tipo (eccezione: DTO strettamente correlati che stanno
  insieme in un unico file `Dtos`, es. `ReservationDto` + `CreateReservationRequest` in `ReservationDto.cs`).

## 2. Naming

| Elemento | Convenzione | Esempio |
|---|---|---|
| Interfacce | prefisso `I` | `IReservationRepository`, `IDateTimeProvider` |
| Campi privati | `_camelCase` | `_repository`, `_validator` |
| Classi sealed di default | `sealed class` / `sealed record` a meno che la classe sia esplicitamente pensata per l'ereditarietà | `sealed class Reservation`, `sealed record DateRange` |
| Metodi asincroni | suffisso `Async`, ultimo parametro `CancellationToken ct = default` | `GetByIdAsync(Guid id, CancellationToken ct = default)` |
| Application service (use case) | suffisso `Service`, in `Application/Services/` | `ReservationService` |
| Repository | suffisso `Repository`, interfaccia in `Application/Abstractions/`, implementazione in `Infrastructure/Repositories/` | `IReservationRepository` / `ReservationRepository` |
| Validator | suffisso `Validator`, in `Application/Validators/` | `CreateReservationValidator` |
| Mapping EF | suffisso `Configuration`, in `Infrastructure/Configurations/` | `ReservationConfiguration` |
| `DbContext` di modulo | classe `DbContext` dentro `<Modulo>.Infrastructure` (namespace la disambigua) | `Module.ZTest.Infrastructure.DbContext` |
| Eccezioni di dominio | suffisso della causa + `Exception`, in `Domain/Exceptions/` | `ReservationNotFoundException` |
| Eventi di dominio | participio passato, in `Domain/Events/`, `record` che implementa `IDomainEvent` | `ReservationCreatedEvent` |
| Punto di composizione del modulo | classe statica `Module` con metodo `Add<Nome>Module(this IServiceCollection)` | `AddZTestModule()` |
| Endpoint del modulo | classe statica `Endpoints` con metodo `Map<Nome>Endpoints(this IEndpointRouteBuilder)` | `MapZTestEndpoints()` |
| Namespace | coincide sempre con il percorso di cartella (`Module.ZTest.Application.Services` ⇔ `Module.ZTest/Application/Services/`) | — |
| Test | nome del metodo = comportamento in italiano, verbo + condizione + esito | `Confirm_su_prenotazione_annullata_solleva` |

Namespace = cartella non è solo una convenzione estetica: `ArchitectureTests` verifica le dipendenze per
prefisso di namespace, quindi un file nella cartella sbagliata **non viene coperto** dalle regole di confine.

## 3. Dominio (Domain)

- **Aggregate root**: `sealed class`, costruttore **privato**, creazione tramite **factory statica**
  (`public static Reservation Create(...)`) che valida le invarianti e lancia (`ArgumentException`,
  `ArgumentOutOfRangeException`) sui parametri non validi. Nessun setter pubblico: le transizioni di stato
  sono metodi con nome di dominio (`Confirm()`, `Cancel()`), mai `SetStatus(x)`.
- **Value Object**: `sealed record`, immutabile, invarianti verificate nel costruttore, comportamento
  proprio (`DateRange.Overlaps`), mai un DTO travestito.
- **Eventi di dominio**: `sealed record` che implementa `IDomainEvent` (marker con `OccurredOnUtc`),
  accumulati in una collezione privata dell'aggregato ed esposti come `IReadOnlyCollection`
  (`DomainEvents`), con un metodo `ClearDomainEvents()` per lo scarico dopo la pubblicazione.
- **Eccezioni**: `sealed`, un tipo per condizione di errore, messaggio in italiano per i log — mai un
  `Exception` generico per un errore di dominio prevedibile.
- **Enum**: valori numerati esplicitamente (`Pending = 0`), mai lasciati impliciti, per rendere sicuro un
  futuro storage/serializzazione.
- **Divieti strutturali** (validati o da validare in `ArchitectureTests`): nessuna dipendenza da
  `Microsoft.EntityFrameworkCore` o `Microsoft.AspNetCore`; nessun attributo di mapping EF (`[Table]`,
  `[Column]`, `[Key]`) sulle entità; nessun uso diretto di `DateTime.Now`/`DateTime.UtcNow` fuori da
  `SystemDateTimeProvider` (si passa sempre da `IDateTimeProvider`); nessun `Guid.NewGuid()` — usare
  `Guid.CreateVersion7()` nella factory (vedi [identificatori-e-uuid.md](pre/identificatori-e-uuid.md)).

## 4. Application

- **Porte** (`Application/Abstractions/`): solo interfacce, zero implementazione, zero dipendenza da EF/HTTP.
  L'Application dipende solo da queste astrazioni, mai da un tipo concreto di Infrastructure.
- **Service**: orchestra soltanto — valida (delegando al Validator), carica/richiama il dominio, persiste,
  mappa in DTO. Non contiene regole di business: se un `if` esprime una regola del dominio (non
  un'orchestrazione), va spostato in un metodo dell'aggregato o in un domain service.
- **DTO** (`Application/Dtos/`): `sealed record`, mai l'entità di dominio esposta all'esterno. Un DTO di
  lettura espone una factory `FromEntity(Entity e)` che fa la mappatura in un unico punto.
- **Validator**: metodo `Validate(...)` che ritorna la lista di errori (`IReadOnlyList<string>`) per l'uso
  compositivo, più eventualmente un `ValidateAndThrow(...)` per l'uso diretto. Nessuna dipendenza da
  Infrastructure. Se il progetto adotta FluentValidation, la collocazione resta la stessa
  (`Application/Validators/`, un `AbstractValidator<T>` per DTO di input).

## 5. Infrastructure

- **Repository**: implementa 1:1 la porta definita in Application; con EF Core reale usa il `DbContext` del
  modulo e delega la conferma a `SaveChangesAsync` (mai `SaveChanges` sincrono).
- **Configurations**: una classe per entità che implementa `IEntityTypeConfiguration<T>` (Fluent API); i
  Value Object si mappano con `OwnsOne`/`OwnsMany`. Le costanti di mapping (lunghezze massime, precisione
  decimale, nome tabella/schema) stanno qui, non sparse nell'entità.
- **Infras** (servizi infrastrutturali trasversali al modulo: clock, storage, notifiche): un'implementazione
  per astrazione, registrata nel `Module.cs` del modulo.
- **ExternalApi**: un client per servizio esterno, sempre dietro `IHttpClientFactory` (mai `new HttpClient()`
  diretto), URL costruiti con `Uri.EscapeDataString` sui parametri, mai concatenazione di stringa grezza con
  input non validato — la stessa disciplina già richiesta per SQL (vedi
  [01-criticita-e-indicazioni-backend.md §3.1](01-criticita-e-indicazioni-backend.md)).
- **DbContext**: uno per modulo, schema dedicato, mai referenziato fuori da `Infrastructure` del proprio
  modulo e mai da un altro modulo (regole già coperte da `ModuleConventionArchitectureTests`).

## 6. Endpoints

- Minimal API, classe statica `Endpoints` con estensione `Map<Nome>Endpoints`, gruppo di rotte con
  `MapGroup(...)` e `WithTags("<Modulo> - <Risorsa>")` per la UI Swagger.
- La lambda dell'endpoint riceve i servizi Application via DI sui parametri del delegate (niente
  `IServiceProvider` risolto a mano), esegue, mappa le eccezioni di dominio note in risultati HTTP
  (`Results.NotFound()`, ecc.) e ritorna sempre un DTO o un `Results.*`, mai un'entità.
- Nessuna dipendenza da `Infrastructure` (vale anche solo come `using`: la regola guarda i tipi referenziati,
  ma un `using` non necessario va comunque rimosso in review).

## 7. Registrazione DI (`Module.cs`)

- Un'unica classe statica `Module` per modulo con un solo metodo pubblico `Add<Nome>Module`.
- Lifetime espliciti e motivati dal reale bisogno di stato: componenti senza stato mutabile condiviso
  (repository in-memory di esempio, provider) come `Singleton`; servizi applicativi che orchestrano una
  richiesta come `Scoped`. Con un `DbContext` reale il repository che lo usa **deve** essere `Scoped` (mai
  `Singleton` che cattura uno `Scoped` — vedi il test di composizione in
  [02-strategia-di-test.md §5](02-strategia-di-test.md)).
- Il commento sopra la classe indica sempre come Boot la invoca (`builder.Services.AddXModule();`), per
  chi legge il modulo isolatamente.

## 8. Commenti e documentazione XML

- `<summary>` in italiano su ogni tipo pubblico e su ogni membro il cui scopo non sia ovvio dal nome;
  spiega **perché** quel tipo esiste o **quale vincolo architetturale** rispetta (es. "non conosce
  persistenza né HTTP"), non ripete il nome in prosa.
- Niente commenti che descrivono l'ovvio riga per riga. Un commento inline è ammesso solo per un vincolo
  nascosto, un workaround puntuale o un rimando esplicito a un altro documento (pattern già usato in
  `DbContext.cs` di esempio: "In un modulo reale erediterebbe da... ApplyConfigurationsFromAssembly").
- Riferimenti incrociati ai documenti in `docs/` quando la scelta implementativa deriva da una decisione lì
  descritta (es. `// vedi 01-criticita-e-indicazioni-backend.md §3.4` accanto al ricalcolo server-side di un
  importo).

## 9. Cosa NON fare (raccolto dalle criticità del legacy)

Regole che esistono specificamente per non reintrodurre i pattern descritti in
[01-criticita-e-indicazioni-backend.md](01-criticita-e-indicazioni-backend.md):

- Nessuna query SQL costruita per concatenazione di stringhe: LINQ/EF Core, o `FromSqlInterpolated` con
  parametri nominati.
- Nessun controller/endpoint che legge l'id di una risorsa scoped-per-tenant dal body/query senza verificarlo
  contro l'identità dell'utente autenticato (anti-IDOR, vedi anche
  [02-strategia-di-test.md §6.2](02-strategia-di-test.md)).
- Nessun importo finanziario accettato as-is dal client: sempre ricalcolato server-side da un servizio di
  dominio dedicato.
- Nessun `async void` o `.Select(async x => ...)`/`.map(async ...)` non atteso: usare sempre
  `await Task.WhenAll(...)` su collezioni di operazioni indipendenti.
- Nessun importo monetario come `float`/`string`: sempre `decimal`.
- Nessun `catch` vuoto: un'eccezione o si gestisce esplicitamente o si lascia propagare al gestore globale
  (vedi [standard-risposta-api-dotnet.md](pre/standard-risposta-api-dotnet.md)).
- Nessun segreto letterale nel codice o nei file versionati (vedi la sezione dedicata al secret management
  nella prossima revisione operativa/architetturale).

## 10. Checklist rapida per un nuovo modulo

Riprende ed è coerente con la *definition of done* di
[02-strategia-di-test.md §17](02-strategia-di-test.md):

- [ ] Struttura di cartelle identica a `Module._Template` (`Domain/Application/Infrastructure/Endpoints`).
- [ ] Riferimenti: `Shared` (contratti) e `BuildingBlocks.Infrastructure` (persistenza e implementazioni comuni),
      usato solo dalla cartella `Infrastructure` ([linee guida §7](pre/linee-guida-implementative-dotnet.md)).
- [ ] `Module.cs` con `Add<Nome>Module`, lifetime motivati.
- [ ] `Endpoints.cs` con `Map<Nome>Endpoints`, `WithTags`, nessun tipo di dominio nella firma pubblica.
- [ ] Aggregate root `sealed`, factory statica, nessun setter pubblico.
- [ ] DTO come `sealed record` con `FromEntity` dove serve una lettura.
- [ ] Porte in `Application/Abstractions`, implementate in `Infrastructure`.
- [ ] Nessuna violazione rilevata da `dotnet test tests/ArchitectureTests`.
- [ ] `dotnet format --verify-no-changes` pulito, zero warning nullable.
