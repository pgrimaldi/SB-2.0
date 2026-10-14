# Linee guida implementative .NET

> Riferimento operativo per la riscrittura del backend in **.NET 10 (Web API) + EF Core**.
> Traduce in scelte concrete i principi di [architettura-evolutiva-microservizi.md](architettura-evolutiva-microservizi.md)
> (monolite modulare pronto per un futuro split in microservizi).
> Regola-bussola trasversale: **config/infrastruttura trasversale → host; contenuto/proprietà → modulo;
> astrazioni → shared puro; implementazioni con dipendenze → shared infrastruttura**.

---

## 1. Struttura della soluzione

Organizzazione **primaria per dominio** (bounded context), **secondaria per layer** (Clean Architecture
dentro ogni modulo). Non progetto unico, non split a soli layer.

```
SummerBooking.sln
├── src/
│   ├── Bootstrap/
│   │   └── SummerBooking.Api                 // HOST Web API: Program.cs, DI, middleware, Swagger, auth
│   ├── Modules/
│   │   ├── Booking/          (C1)
│   │   ├── Configuration/    (C2)  // pannello manager
│   │   ├── Payment/          (C3)
│   │   ├── Fiskaly/          (C4)
│   │   ├── Identity/         (C5)
│   │   ├── Notification/     (C6)  // email, whatsapp, template
│   │   └── Switchboard/      (C7)
│   └── BuildingBlocks/
│       ├── SummerBooking.SharedKernel        // PURO, senza dipendenze
│       └── SummerBooking.BuildingBlocks.Infrastructure   // opzionale: infra condivisa (Redis, event bus...)
└── tests/
    ├── SummerBooking.ArchitectureTests        // NetArchTest: difende i confini
    └── <Modulo>.UnitTests
```

### Tipi di progetto
| Progetto | SDK | Note |
|---|---|---|
| `Api` (host) | `Microsoft.NET.Sdk.Web` | **Unico progetto eseguibile** |
| Moduli-dominio | `Microsoft.NET.Sdk` (class library) | Anche se contengono endpoint; per esporre controller aggiungono `<FrameworkReference Include="Microsoft.AspNetCore.App" />` |
| `SharedKernel` | class library | Puro, senza dipendenze |
| `BuildingBlocks.Infrastructure` | class library | Con dipendenze (EF, Redis…) |
| Test | class library test (xUnit) | Non finiscono nel deploy |

### Dosaggio pragmatico (2-3 dev)
Parti con **1 progetto per modulo** (layer come cartelle interne). Spezzi un modulo in
`Domain`/`Application`/`Infrastructure` separati **solo quando cresce** (es. Booking). Il numero di
progetti non conta: contano i **confini** (regole §5) e la **proprietà dei dati** (uno schema per modulo).

---

## 2. Struttura interna di un modulo

Layer come cartelle. Esempio `Booking`:

```
SummerBooking.Booking/
├── Domain/                    // cuore: nessuna dipendenza (solo SharedKernel)
│   ├── Entities/              // Booking, Reservation... (con id e ciclo di vita)
│   ├── ValueObjects/          // Money, DateRange, Percentage... (identificati dal valore, immutabili)
│   ├── Enums/
│   ├── Events/                // eventi di dominio (BookingConfirmed) — record al passato
│   ├── Services/              // domain service: logica che coinvolge più aggregati
│   └── Exceptions/            // eccezioni tipizzate (agnostiche rispetto alla lingua)
├── Application/               // casi d'uso: orchestra il dominio, NIENTE dettagli tecnici
│   ├── Services/              // application service (o Commands/Queries se si adotta CQRS)
│   ├── Abstractions/          // PORTE: IBookingRepository, IUnitOfWork, IPricingApi...
│   ├── Dtos/
│   └── Validators/
├── Infrastructure/           // i "come" tecnici: implementa le porte
│   ├── Persistence/
│   │   ├── BookingDbContext.cs       // schema "booking"
│   │   ├── Configurations/           // IEntityTypeConfiguration (Fluent API)
│   │   ├── Repositories/
│   │   └── Migrations/
│   └── ExternalServices/             // client verso servizi esterni
├── Endpoints/  (o Presentation/)     // i controller/minimal endpoint del modulo
├── Resources/                        // .resx/.json di localizzazione del modulo
└── BookingModule.cs                  // AddBookingModule(): registra DI + endpoint del modulo
```

---

## 3. Cosa va in ogni layer

### Domain
- **Entities**: oggetti con identità (`Id`) e ciclo di vita. **Contengono la logica di business** propria
  del concetto (metodi che cambiano stato proteggendo le invarianti: `booking.ApplyDiscount()`, `Cancel()`).
- **ValueObjects**: concetti identificati dal **valore**, immutabili, auto-validanti, con comportamento
  (`Money.Add`, `DateRange.Overlaps`). Due VO sono uguali se tutti i valori coincidono.
- **Events (di dominio)**: fatti accaduti, al passato, immutabili (`BookingConfirmed`). Sollevati
  dall'entità al cambio di stato; gestiti in Application o tradotti in *integration event* per altri moduli.
- **Services (di dominio)**: logica che **coinvolge più aggregati** e non appartiene a una sola entità
  (`AvailabilityService`, `PricingService`).
- **Regola d'oro**: modello **ricco**, non anemico. Le regole stanno nelle entità/VO/domain service, **non**
  in service applicativi che trattano le entità come sacchi di dati (è il pattern che ha prodotto il codice attuale).
- Il Domain **non usa** EF, ASP.NET, né le Resources: resta **agnostico rispetto a persistenza e lingua**.

### Application
- Orchestra i casi d'uso: carica l'aggregato dal repository → chiama il metodo di dominio → salva →
  pubblica eventi. **Non contiene regole di dominio.**
- Definisce le **porte** (interfacce) che l'Infrastructure implementerà.
- **CQRS (Commands/Queries) è facoltativo**: con EF Core non serve. Per 2-3 dev parti con **application
  service** (metodi); adotti Commands/Queries + MediatR **solo** se vuoi le pipeline behavior
  (validazione/logging/transazioni) o su casi d'uso complessi.

### Infrastructure
Tutto ciò che parla con **DB, rete, filesystem, sistemi esterni**:
- `DbContext`, `IEntityTypeConfiguration`, repository, migrazioni, value converter;
- client esterni dietro interfaccia (Stripe, Fiskaly, email, WhatsApp, storage);
- implementazioni cross-cutting tecniche (cache Redis, event bus, outbox, PDF/template rendering).

### Endpoints (Presentation)
- I controller / minimal endpoint **del modulo**. Prendono input, chiamano Application, restituiscono DTO.
- **Non** referenziano Infrastructure.

---

## 4. EF Core: mapping delle entità

- **Usa le stesse classi di dominio** con EF Core: **niente modello di persistenza separato** (duplicazione inutile).
- **Niente attributi EF nel Domain** (`[Table]`, `[Column]`, `[Key]`): accoppiano il dominio allo schema,
  non esprimono tutto (chiavi composite, owned type, indici) e obbligano comunque alla Fluent API →
  configurazione spezzata. Tieni le entità **pulite**.
- **Tutto il mapping in `Infrastructure/Persistence/Configurations/`** via Fluent API
  (`IEntityTypeConfiguration<T>`): chiavi, relazioni, indici, **schema del modulo**, value conversion.
- **Value object** → **owned types** (`OwnsOne`/`OwnsMany`).
- Incapsulamento (setter privati, collezioni read-only) → **backing field** + costruttore privato per la
  materializzazione. Le invarianti (obbligatorietà, lunghezze) esprimile come regole nel costruttore/metodi,
  non come annotation.
- **Un `DbContext` + uno schema per modulo** (`booking`, `payment`…). **Zero FK/JOIN cross-schema**;
  riferimenti fra moduli **per ID**.

---

## 5. Confini e dipendenze (regole non negoziabili)

Direzione: **le dipendenze puntano verso l'interno, verso il Domain.**

```
 Endpoints ──► Application ──► Domain ◄── (SharedKernel)
                   ▲              ▲
                   └── Infrastructure ┘   (implementa le porte di Application; qui vive EF)
```
(freccia `A ──► B` = "A referenzia B")

- **Domain** → dipende da nulla (solo SharedKernel puro).
- **Application** → dipende solo da Domain. Definisce le porte. Niente EF/ASP.NET.
- **Infrastructure** → dipende da Application+Domain, **implementa** le porte. A runtime il flusso è
  invertito via **DI** (Dependency Inversion): Application chiama l'interfaccia, gira l'implementazione.
- **Endpoints** → dipende da Application. Non referenzia Infrastructure.
- **Solo l'host `Api`** referenzia Infrastructure (composition root, registra le implementazioni in DI).

Come Application "usa" Infrastructure:
```csharp
// Application: dipende solo dall'interfaccia, mai dal DbContext
public class ConfirmBookingHandler {
    private readonly IBookingRepository _repo;
    private readonly IUnitOfWork _uow;
    public ConfirmBookingHandler(IBookingRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }
    public async Task Handle(Guid id) {
        var booking = await _repo.GetById(id);   // implementazione in Infrastructure, iniettata via DI
        booking.Confirm();
        await _uow.SaveChanges();
    }
}
```

> Con i layer come **cartelle** in un solo progetto, la direzione è una **convenzione** (il compilatore non
> la impone): difendila con gli **ArchitectureTests** (§10). Con progetti separati la impone il compilatore.

---

## 6. Comunicazione tra moduli — **mai HTTP**

Stesso processo → comunicazione **in-process** (l'HTTP entra solo dopo l'estrazione in microservizio, ed è
più lento). È anche la soluzione **più performante**: la scelta tra i due meccanismi è di design, non di
prestazioni (l'I/O verso DB/servizi esterni domina).

- **Integration event — asincrono (preferito)** per gli effetti collaterali cross-modulo. Il modulo
  pubblica un evento su un **event bus in-memory**; gli altri si sottoscrivono. Non si conoscono.
  ```csharp
  await _bus.Publish(new BookingConfirmed(bookingId, propertyId));
  ```
  Domani si sostituisce il bus in-memory con un broker reale: gli handler non cambiano.
- **Interfaccia pubblica — sincrono** quando serve la risposta in linea (query cross-modulo). Il modulo B
  espone un'interfaccia nei contratti condivisi; A dipende **solo dall'interfaccia**, la DI inietta B.
  ```csharp
  public interface IPricingApi { Task<Money> CalculateAsync(PricingRequest r); }
  ```

Regola: A chiama B **solo tramite il contratto** (interfaccia + DTO condivisi), **mai** Domain/Infrastructure
di B. Quando estrai B in microservizio, sostituisci l'implementazione in-process con un client HTTP/gRPC e
**il codice di A non cambia**.

Transazioni: ogni modulo scrive nella **sua** transazione (il suo `DbContext`); anche col DB unico **non**
appoggiarti a transazioni cross-modulo — coordina con eventi (+ **outbox** per affidabilità).

---

## 7. Progetti condivisi: puro vs infrastruttura

"Condiviso" non è una categoria sola: il criterio è il **peso delle dipendenze**.

| Progetto | Dipendenze | Referenziato da | Contiene |
|---|---|---|---|
| **SharedKernel** | **nessuna** (puro) | tutti, **incluso Domain** | tipi base, VO comuni, **astrazioni** (`ICache`, event base), `Result` |
| *(SharedContracts)* | nessuna/poche | i moduli | interfacce pubbliche + integration event *(può stare nel SharedKernel)* |
| **BuildingBlocks.Infrastructure** | EF, Redis, MediatR… | **solo Infrastructure dei moduli + host** | base `DbContext`, event bus, outbox, adapter Redis, interceptor |

Perché due e non uno solo: se metti Redis/EF nel SharedKernel, **il Domain di ogni modulo si trascina
Redis/EF** come dipendenza transitiva → confini distrutti. Il kernel resta **puro**; le implementazioni con
dipendenze stanno nell'infrastruttura condivisa. `BuildingBlocks.Infrastructure` è **opzionale**: si
introduce quando ci sono implementazioni infrastrutturali **riusate da più moduli**; prima si può registrare
tutto nell'host.

**Redis** (esempio del pattern): astrazione (`ICache` o `IDistributedCache` nativo) nello **Shared**;
implementazione/wiring in **BuildingBlocks.Infrastructure** o nell'host; **ogni modulo** decide cosa mettere
in cache, con **chiavi namespacate** (`booking:...`, `config:...`).

### Stato attuale nel codice (2026-10-08)

| Progetto | Pacchetti | Referenziato da | Contiene |
|---|---|---|---|
| **`Shared`** (il SharedKernel) | **nessuno** | tutti i moduli, **Domain compreso**, e Boot | contratti fra moduli (`Contracts/`: errori e codici di validazione comuni, eventi, audit, email e nomi dei segnaposto, rubrica, partner e nomi delle app partner, `IAuditable`, `ISoftDeletable`), sicurezza (`Security/`: ruoli, permessi, claim, `ICurrentUser`, `JwtOptions`), lingue e valute (`Localization/`: `AppLanguage`, `SupportedLanguages`, `CurrencyCodes`), DTO generici (`Dtos/`: `PagedResult`, `SortDirections`, `TabularFormat`/`TabularFile`), `Utils/Codes` (enum ↔ codice snake_case), astrazioni (`IDateTimeProvider`, `ISecretResolver`) e due implementazioni senza librerie (`SystemDateTimeProvider`, `AuditTrail`) |
| **`BuildingBlocks.Application`** | FluentValidation | l'Application dei moduli | `Validation/`: `EnsureValid` e conversione degli errori di FluentValidation nei codici `validation.*`, regole di campo comuni (email, telefono, prefisso, codice lingua) |
| **`BuildingBlocks.Infrastructure`** | EF Core, Npgsql, AWS SDK, ClosedXML, Configuration | l'Infrastructure dei moduli (tutti, per la persistenza) e Boot | `Persistence/` (interceptor di audit e soft-delete, convenzioni EF, converter UTC, connection string per modulo), `Retention/` (motore, archivio S3, coda delle cancellazioni), `Storage/` (client S3 unico, archivio file con hash e link firmati, URL pubblici), `Export/` (CSV/XLSX), `Secrets/` (resolver dalla configurazione) |
| **`BuildingBlocks.Infrastructure.Tests`** | xUnit | — | i test del motore di retention (ex `Shared.Tests`) |

**Regola per il codice nuovo**: interfaccia, contratto o tipo senza librerie usato da più moduli → `Shared`;
codice di Application con una libreria e riusato da più moduli (validazione) → `BuildingBlocks.Application`;
implementazione che usa una libreria esterna e serve a più moduli → `BuildingBlocks.Infrastructure`; tutto ciò che serve a un solo
modulo → la sua cartella `Infrastructure` (es. emissione dei JWT, verifica dei login social: solo Identity).

**Chi garantisce i confini**: i moduli sono un unico assembly con cartelle, quindi non il compilatore ma gli
ArchitectureTests: Shared non dipende da librerie tecniche (FluentValidation compresa) né da BuildingBlocks;
i BuildingBlocks non dipendono da moduli né da Boot; BuildingBlocks.Application non dipende da
BuildingBlocks.Infrastructure né da EF, AWS, ClosedXML, ASP.NET; il Domain dei moduli non dipende da nessun
BuildingBlocks; Domain e Application non dipendono da BuildingBlocks.Infrastructure, ClosedXML, AWS (oltre alle
regole già esistenti su EF e ASP.NET).

**Cosa resta nel modulo anche se somiglia a qualcosa di comune**: gli enum salvati in un tipo Postgres con
membri diversi per modulo (es. l'origine dei file: `identity.storage_origin` ha quattro valori,
`fiskaly`/`notification` tre), le regole di campo di un solo modulo (password, app di login), le porte
verso i file di un solo modulo (`IDocumentStorage` dei contratti, `ICustomerExportWriter`).

**Cosa resta nell'host (Boot/Web)**: bus degli eventi in-process, gestore degli errori Problem Details, utente
corrente dai claim, configurazione di autenticazione. Con un solo host stanno bene lì; vanno in BuildingBlocks
quando nascerà il secondo host (job/worker, [20-jobs.md](../20-jobs.md)), che avrà bisogno almeno del bus.

---

## 7bis. Shared e BuildingBlocks con i microservizi

Quando un modulo diventa un servizio con il proprio deploy, i progetti condivisi diventano **pacchetti NuGet**
su un feed privato (AWS CodeArtifact o GitHub Packages):

| Pacchetto | Chi lo referenzia | Come cambia |
|---|---|---|
| **Shared** (contratti) | **tutti** i servizi | raramente e in modo retrocompatibile: si aggiungono campi, non si rinominano |
| **BuildingBlocks.Infrastructure** | **solo** i servizi che usano quelle implementazioni | più spesso (aggiornamenti di EF, AWS, …); ogni servizio adotta la nuova versione quando vuole |

**Perché la separazione serve proprio lì**:
- **aggiornamenti indipendenti**: con EF dentro Shared, ogni aggiornamento di EF sarebbe una nuova versione di
  Shared per tutti i servizi, rilasciati insieme (il "monolite distribuito");
- **niente conflitti di versione**: Shared non impone versioni di librerie (il conflitto MSB3277 del
  2026-09 nasceva proprio da EF dentro Shared);
- **meno superficie**: una vulnerabilità dell'AWS SDK riguarda solo i servizi che usano S3, non tutti;
- **contratti stabili**: le interfacce in-process (`ICustomerDirectory`, `IPartnerClientAuthenticator`)
  restano in Shared e diventano client HTTP/gRPC; il codice di chi le usa non cambia.

**Affinamenti previsti al momento dell'estrazione**:
1. BuildingBlocks diviso per area (`.Persistence`, `.Storage`, `.Export`, `.Retention`, `.Messaging`), così un
   servizio senza S3 non si porta l'AWS SDK. I namespace sono già separati per cartella.
2. Contratti per servizio: ogni servizio pubblica i propri (`Identity.Contracts`, …) e Shared si riduce al
   nucleo comune (errori, sicurezza, interfacce di base degli eventi). Le cartelle `Shared/Contracts/<Modulo>`
   preparano già la divisione.
3. BuildingBlocks va tenuto **sottile e versionato**: è codice condiviso fra servizi, quindi un punto di
   accoppiamento. Si condividono i contratti, il meno possibile le implementazioni.

**Alternativa senza pacchetti**: monorepo con riferimenti diretti fra progetti e rilasci separati dei servizi.
Niente feed né versioni, ma una modifica a Shared ricompila tutti e serve disciplina per non rilasciare in
blocco. La scelta è aperta ([20-jobs.md §6](../20-jobs.md)); la separazione Shared/BuildingBlocks vale per
entrambe le strade.

---

## 8. Template (email, PDF, documenti)

Regola: **il template vive nell'Infrastructure del modulo che genera quell'output**; nessun altro modulo lo tocca.

| Cosa | Dove |
|---|---|
| Template email di default | `Notification/Infrastructure/Templates/` (embedded resource) |
| Template personalizzati per stabilimento | **DB**, schema `notification` (editabili dal pannello) |
| Motore di rendering + invio | `Notification/Infrastructure/` |
| PDF scontrino / documenti | Infrastructure del modulo che li produce (Fiskaly, …) |

Quando Booking deve mandare una mail **non renderizza nulla**: pubblica `BookingConfirmed` o chiama
`INotificationService`; è Notification che possiede template + rendering + invio.

---

## 9. Multilingua (i18n)

Il criterio non è il volume, ma **chi scrive il testo e quando**:

| Tipo di testo | Soluzione |
|---|---|
| Statico, scritto dal dev (errori, etichette, validazioni) | **`.resx` / `IStringLocalizer`** (versionato col codice, in memoria, veloce) |
| Editabile come **file** senza ricompilare l'assembly | **file JSON** di localizzazione (rideploy del solo file) |
| Editabile **live dall'admin** senza deploy | **DB** |
| Contenuto per-stabilimento / per-record (nomi servizi, descrizioni…) | **DB, tabella per (id entità + cultura)** |

- Il **meccanismo** di localizzazione (risoluzione cultura da `Accept-Language`/preferenza utente,
  `RequestLocalizationOptions`) si configura **una volta nell'host**.
- Le **stringhe statiche** stanno per-modulo (`Booking/Resources/`); le poche comuni nel SharedKernel.
- I **dati di dominio multilingua** si modellano con **tabelle di traduzione tipizzate per entità**
  (`ServiceTranslation(ServiceId, Culture, Name, Description)`, unique su `(ServiceId, Culture)`) nello
  **schema del modulo proprietario** — **non** una tabella generica `(EntityType, Field, Culture, Value)`
  (è di nuovo schema sfuso, da evitare).
- Nel caso specifico prevale il **contenuto in DB** (catalogo visto dal cliente finale). Il Domain resta
  agnostico: solleva eccezioni tipizzate, il messaggio si localizza al **bordo** (Endpoints/Application).

---

## 10. Swagger per dominio

Con centinaia di endpoint, **un documento OpenAPI per dominio** in un unico host (menù a tendina nella
stessa UI). Il **meccanismo sta nell'host**; **ogni modulo tagga i propri endpoint** col suo group name.

```csharp
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("booking", new() { Title = "Booking API", Version = "v1" });
    c.SwaggerDoc("payment", new() { Title = "Payment API", Version = "v1" });
});
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/booking/swagger.json", "Booking");
    c.SwaggerEndpoint("/swagger/payment/swagger.json", "Payment");
});
```
```csharp
[ApiExplorerSettings(GroupName = "booking")]         // sui controller
app.MapPost("/bookings", ...).WithGroupName("booking"); // minimal API
```
Bonus: già pronto per lo split — quando estrai un modulo, il suo documento diventa quello del servizio.

---

## 11. Architecture tests (difesa dei confini)

Progetto di test dedicato (`SummerBooking.ArchitectureTests`) con **NetArchTest.Rules** che ispeziona le
dipendenze via reflection e **fallisce in CI** se una regola è violata. (Blocco reale a *compile-time* solo
separando i progetti; con i layer a cartelle il compilatore non aiuta → servono questi test.)

```csharp
[Fact]
public void Domain_non_dipende_da_Infrastructure() {
    var r = Types.InAssembly(typeof(Booking).Assembly)
        .That().ResideInNamespace("SummerBooking.Booking.Domain")
        .ShouldNot().HaveDependencyOn("SummerBooking.Booking.Infrastructure")
        .GetResult();
    Assert.True(r.IsSuccessful, string.Join(", ", r.FailingTypeNames ?? new List<string>()));
}

[Fact]
public void Application_non_dipende_da_EFCore() { /* ShouldNot().HaveDependencyOn("Microsoft.EntityFrameworkCore") */ }

[Fact]
public void I_moduli_non_si_referenziano_tra_loro() { /* Booking ShouldNot HaveDependencyOn "SummerBooking.Payment" */ }
```

Regole tipiche: Domain senza Application/Infrastructure/EF/ASP.NET; Application senza EF/ASP.NET; Endpoints
senza Infrastructure; moduli che non si referenziano (solo SharedKernel/Contracts).

**Enforcement:** eseguibile a mano (`dotnet test tests/ArchitectureTests`) per un check locale — ma per
essere una **vera guardia** va in **CI a ogni PR con gate sulla merge**, altrimenti resta solo advisory.

---

## 12. Riepilogo delle regole d'oro

1. **Modulare per dominio** (asse primario), **Clean Architecture** dentro ogni modulo (asse secondario).
2. **Un `DbContext`/schema per modulo**; riferimenti cross-modulo **per ID**, mai JOIN.
3. **Dipendenze verso il Domain**; Infrastructure agganciata via DI.
4. **Comunicazione tra moduli in-process** (eventi async / interfaccia sync), **mai HTTP**; solo via contratti.
5. **SharedKernel puro**; implementazioni con dipendenze in `BuildingBlocks.Infrastructure`/host.
6. **Niente attributi EF nel Domain**; mapping in Fluent API nell'Infrastructure.
7. **Config/infrastruttura trasversale nell'host; contenuto/proprietà nel modulo.**
8. **ArchitectureTests in CI** a difesa dei confini.
