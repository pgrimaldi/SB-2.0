# Strategia di test del backend .NET

> Riferimento operativo per i test automatici del nuovo backend
> (**.NET 10 + xUnit + EF Core**, monolite modulare — vedi
> [linee-guida-implementative-dotnet.md](pre/linee-guida-implementative-dotnet.md)).
> Estende [§11 Architecture tests](pre/linee-guida-implementative-dotnet.md) descrivendo **tutti gli altri
> livelli di test** che affiancano `ArchitectureTests`, già presente in
> [`backend/summerbooking-backend/ArchitectureTests/`](../../../backend/summerbooking-backend/ArchitectureTests/).
>
> **Ambito del documento:** solo test che hanno senso **in pipeline e a regime**, cioè che restano utili per
> tutta la vita del prodotto e che possono fallire una build. Sono esclusi i test *transitori* di migrazione
> (es. confronto di parità delle risposte con il backend Node legacy): utili durante la riscrittura, ma
> destinati a essere buttati e quindi fuori da questa strategia.

---

## 0. Principi

1. **Ogni test deve poter far fallire la build.** Un test che nessuno guarda quando è rosso è codice morto:
   o entra in un gate di CI, o non si scrive.
2. **Piramide, non clessidra.** Molti test veloci sul dominio, pochi test lenti end-to-end. Il costo di
   manutenzione cresce con la distanza dal codice testato.
3. **Determinismo assoluto.** Niente `DateTime.Now` (usare `IDateTimeProvider`, già presente in
   [`Application/Abstractions/IDateTimeProvider.cs`](../../../backend/summerbooking-backend/Module.ZTest/Application/Abstractions/IDateTimeProvider.cs)),
   niente `Thread.Sleep`, niente dipendenza dall'ordine di esecuzione, niente rete verso servizi reali.
   Un test intermittente ("flaky") va riparato o cancellato lo stesso giorno: erode la fiducia nell'intera
   suite più di quanto la copra.
4. **Il test descrive il comportamento, non l'implementazione.** Se un refactoring interno che non cambia il
   comportamento rompe 40 test, quei test erano scritti male.
5. **Separazione per velocità, non solo per tipo.** I test che non hanno bisogno di Docker/DB vivono in
   progetti diversi da quelli che ne hanno bisogno, così il gate veloce resta veloce.

### Layout dei progetti di test

Coerente con la struttura `tests/` di [§1 delle linee guida](pre/linee-guida-implementative-dotnet.md):

```
tests/
├── ArchitectureTests           // §1  regole di confine e convenzioni (già esistente)
├── <Modulo>.UnitTests          // §2  dominio + §3 application  → nessuna I/O
├── Boot.CompositionTests       // §5  DI, endpoint mappati, autorizzazione
├── IntegrationTests            // §4  HTTP reale + DB reale (Testcontainers)
│   ├── ...                     // §6  migrazioni EF
│   ├── ...                     // §8  concorrenza / atomicità
│   └── ...                     // §9  idempotenza webhook
├── ContractTests               // §7  snapshot OpenAPI
└── perf/                       // §11 script di carico (k6 / NBomber)
```

### Tabella riepilogativa

| # | Livello | Progetto | Strumenti | Durata tipica | Stage pipeline |
|---|---|---|---|---|---|
| 1 | Architettura e convenzioni | `ArchitectureTests` | NetArchTest | < 5 s | Gate PR |
| 2 | Dominio | `<Modulo>.UnitTests` | xUnit | ms | Gate PR |
| 3 | Application (use case) | `<Modulo>.UnitTests` | xUnit + fake | ms | Gate PR |
| 4 | Integrazione HTTP + DB | `IntegrationTests` | WebApplicationFactory + Testcontainers | 1-5 min | Gate PR (o merge) |
| 5 | Composition root / DI | `Boot.CompositionTests` | WebApplicationFactory | < 10 s | Gate PR |
| 6 | Autorizzazione per endpoint | `Boot.CompositionTests` + `IntegrationTests` | WebApplicationFactory | s | Gate PR |
| 7 | Contratto API (OpenAPI) | `ContractTests` | snapshot/Verify | s | Gate PR |
| 8 | Migrazioni EF | `IntegrationTests` | EF Core + Testcontainers | s | Gate PR |
| 9 | Concorrenza / atomicità | `IntegrationTests` | Testcontainers | s-min | Merge su main |
| 10 | Idempotenza webhook | `IntegrationTests` | Testcontainers | s | Merge su main |
| 11 | Property-based | `<Modulo>.UnitTests` | CsCheck / FsCheck | s | Gate PR |
| 12 | Mutation testing | (sui progetti unit) | Stryker.NET | 10-40 min | Notturno |
| 13 | Carico / performance | `perf/` | k6 o NBomber | min | Notturno su staging |
| 14 | E2E utente | `e2e/` | Playwright | min | Notturno / pre-release |
| 15 | Gate statici e sicurezza | — | analyzers, coverage, CodeQL, ZAP | s-min | Gate PR + notturno |

---

## 1. Architecture e convention test (estensione di quelli esistenti)

**Stato attuale.** Esistono
[`IdentityArchitectureTests`](../../../backend/summerbooking-backend/ArchitectureTests/IdentityArchitectureTests.cs),
[`BootArchitectureTests`](../../../backend/summerbooking-backend/ArchitectureTests/BootArchitectureTests.cs),
[`SharedArchitectureTests`](../../../backend/summerbooking-backend/ArchitectureTests/SharedArchitectureTests.cs):
coprono **solo le dipendenze tra layer e tra progetti**. Con lo stesso strumento (NetArchTest, costo di
esecuzione trascurabile) si possono difendere anche le *convenzioni*, che sono la parte di Clean
Architecture che il compilatore non vede quando i layer sono cartelle e non progetti.

### 1.1 Lacune da colmare subito

| Lacuna | Regola da aggiungere |
|---|---|
| Manca `ZTestArchitectureTests` | Il modulo template è coperto solo dalla regola "non dipende da Boot": vanno replicate le 3 regole Domain/Application già scritte per Auth |
| `Endpoints` può vedere `Infrastructure` | `Endpoints` deve dipendere solo da `Application` (servizi + DTO) e mai da `Infrastructure` |
| `Endpoints` può restituire entità di dominio | Nessun tipo in `Endpoints` deve dipendere da `Domain.Entities`: si esce sempre con un DTO |
| Nessun vincolo sul `DbContext` | Solo `Infrastructure` può dipendere dal `DbContext`; nessun modulo può dipendere dal `DbContext` di un altro modulo |
| Regole per modulo scritte a mano | Con l'aggiunta di nuovi moduli le regole vanno **parametrizzate** (`[Theory]` su tutti gli assembly `Module.*`), altrimenti un modulo nuovo nasce non coperto |

### 1.2 Convenzioni consigliate

- **Immutabilità del dominio**: i tipi in `Domain.Entities` e `Domain.ValueObjects` devono essere `sealed` e
  non avere setter pubblici (rende strutturale l'invariante già rispettata da
  [`Reservation`](../../../backend/summerbooking-backend/Module.ZTest/Domain/Entities/Reservation.cs)).
- **Niente orologio di sistema sparso**: nessun tipo, tranne `SystemDateTimeProvider`, può usare
  `DateTime.Now` / `DateTime.UtcNow` / `DateTimeOffset.Now`. Difende la testabilità di tutta la logica su date
  (stagioni, listini, periodi di prenotazione).
- **Naming e collocazione**: `*Service` in `Application.Services`, `*Repository` in
  `Infrastructure.Repositories`, `*Validator` in `Application.Validators`, `*Configuration` (EF) in
  `Infrastructure.Configurations`.
- **Namespace = cartella**, per evitare che un file finisca nel layer sbagliato senza che nessuno se ne
  accorga.
- **Nessun attributo EF nel Domain** (regola d'oro n. 6 delle linee guida), verificabile come assenza di
  dipendenza da `System.ComponentModel.DataAnnotations.Schema` nei namespace `Domain`.

```csharp
[Fact]
public void Gli_endpoint_non_dipendono_da_Infrastructure()
{
    var result = Types.InAssembly(ArchAssemblies.ZTest)
        .That().ResideInNamespaceStartingWith("Module.ZTest.Endpoints")
        .ShouldNot().HaveDependencyOn("Module.ZTest.Infrastructure")
        .GetResult();

    Assert.True(result.IsSuccessful, Format(result));
}

[Fact]
public void Nessuno_usa_l_orologio_di_sistema_fuori_dal_provider()
{
    // NetArchTest non intercetta le chiamate statiche: qui serve un analyzer Roslyn
    // (es. banned API: DateTime.Now) oppure una regola di lint — vedi §15.
}
```

> ⚠️ **Limite noto**: NetArchTest ragiona su *dipendenze tra tipi*, non su *chiamate a membri statici*.
> Divieti come `DateTime.Now`, `Guid.NewGuid()` o `Console.WriteLine` vanno espressi con
> `BannedApiAnalyzers` (§15), non con NetArchTest.

**Criterio di fallimento:** una sola violazione = build rossa, nessuna soglia, nessuna eccezione non
documentata. Le eccezioni legittime si esprimono come allow-list esplicita dentro il test, con commento.

---

## 2. Unit test di dominio

**Perché.** È il livello con il miglior rapporto valore/costo: verifica le regole di business pure, senza
DB, senza HTTP, senza DI. Esegue in millisecondi, quindi può girare a ogni salvataggio.

**Cosa verifica.** Le invarianti degli aggregate root e dei value object:

- `Reservation.Create`: rifiuta `guestId` vuoto, rifiuta prezzo ≤ 0, calcola `TotalPrice = pricePerNight × notti`,
  emette `ReservationCreatedEvent`, nasce in stato `Pending`.
- `Reservation.Confirm`: consentito solo da `Pending`; da `Cancelled`/`Completed` solleva.
- `Reservation.Cancel`: vietato su `Completed`.
- `DateRange`: `from > to` non costruibile, `Nights` corretto anche a cavallo di mese/anno, periodo di
  durata zero gestito esplicitamente.
- `ClearDomainEvents` svuota la collezione e `DomainEvents` è realmente read-only.

**Come si scrivono.**

```csharp
public class ReservationTests
{
    [Fact]
    public void Create_calcola_il_totale_sulle_notti_del_periodo()
    {
        var periodo = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 4));

        var reservation = Reservation.Create(Guid.CreateVersion7(), periodo, pricePerNight: 50m);

        Assert.Equal(150m, reservation.TotalPrice);   // 3 notti
        Assert.Equal(ReservationStatus.Pending, reservation.Status);
        Assert.Single(reservation.DomainEvents);
    }

    [Fact]
    public void Confirm_su_prenotazione_annullata_solleva()
    {
        var reservation = UnaPrenotazione();          // test data builder
        reservation.Cancel();

        Assert.Throws<InvalidOperationException>(() => reservation.Confirm());
    }
}
```

**Convenzioni.** Un `Assert` concettuale per test; nome del test = comportamento in italiano
(`Confirm_su_prenotazione_annullata_solleva`), coerente con lo stile già usato in `ArchitectureTests`; dati
costruiti da **builder** (`UnaPrenotazione()`), mai da letterali copiati in 30 test — altrimenti aggiungere
un parametro obbligatorio al costruttore diventa un refactoring di mezz'ora.

**Copertura attesa.** Il `Domain` è l'unico layer per cui ha senso pretendere una copertura alta (≥ 90%):
è piccolo, puro e ad alta densità di regole. Vedi il gate di §15.

---

## 3. Unit test dell'Application (use case)

**Perché.** Verifica l'*orchestrazione*: sequenza delle chiamate, gestione degli errori, traduzione
dominio → DTO. Le porte esistono già
([`IReservationRepository`](../../../backend/summerbooking-backend/Module.ZTest/Application/Abstractions/IReservationRepository.cs),
`IDateTimeProvider`), quindi il layer è testabile in isolamento senza alcun mocking framework.

**Cosa verifica** (su
[`ReservationService`](../../../backend/summerbooking-backend/Module.ZTest/Application/Services/ReservationService.cs)):

- validatore che fallisce → `ArgumentException` e **nessuna scrittura** sul repository;
- `HasOverlapAsync` vero → `InvalidOperationException` e nessun `AddAsync`/`SaveChangesAsync`;
- percorso felice → `AddAsync` **e** `SaveChangesAsync` chiamati, DTO restituito coerente con l'entità;
- `GetAsync` su id inesistente → `ReservationNotFoundException`;
- il `CancellationToken` viene propagato a tutte le chiamate asincrone.

**Doppi di test: fake, non mock.** Preferire un fake in memoria riusabile a un mock configurato test per
test: è più leggibile, non si rompe a ogni refactoring interno e serve anche come documentazione della porta.

```csharp
internal sealed class FakeReservationRepository : IReservationRepository
{
    private readonly List<Reservation> _items = new();
    public int SaveChangesCount { get; private set; }

    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_items.SingleOrDefault(r => r.Id == id));

    public Task AddAsync(Reservation reservation, CancellationToken ct = default)
    { _items.Add(reservation); return Task.CompletedTask; }

    public Task<bool> HasOverlapAsync(Guid guestId, DateRange period, CancellationToken ct = default)
        => Task.FromResult(_items.Any(r => r.GuestId == guestId && r.Period.Overlaps(period)));

    public Task SaveChangesAsync(CancellationToken ct = default)
    { SaveChangesCount++; return Task.CompletedTask; }
}
```

Usare un mocking framework (NSubstitute/Moq) solo dove serve **verificare un'interazione** difficile da
osservare altrimenti (es. "la notifica è stata inviata una volta sola").

**Cosa NON verificare qui.** La correttezza delle query EF (§4) e i codici di stato HTTP (§4): a questo
livello si testa la logica, non l'infrastruttura.

---

## 4. Test di integrazione HTTP + DB reale

**Perché.** È l'unico livello che valida ciò che unit test e architecture test non possono vedere: mapping
EF, migrazioni, traduzione LINQ → SQL, serializzazione JSON, pipeline dei middleware, formato degli errori,
codici di stato. Il DB del progetto è **MySQL** (vedi [00-struttura.md](00-struttura.md)): usare MySQL
reale via Testcontainers, **mai** il provider InMemory di EF, che accetta query che il DB reale rifiuta e
non applica vincoli di unicità né transazioni.

**Infrastruttura.**

```csharp
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly MySqlContainer _db = new MySqlBuilder()
        .WithImage("mysql:8.4")
        .WithDatabase("summerbooking_test")
        .Build();

    public WebApplicationFactory<Boot.Program> Factory { get; private set; } = default!;
    public HttpClient Client { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        Factory = new WebApplicationFactory<Boot.Program>()
            .WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:Default", _db.GetConnectionString()));

        Client = Factory.CreateClient();

        using var scope = Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ZTestDbContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync() { Factory.Dispose(); await _db.DisposeAsync(); }
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture> { }
```

> `Boot.Program` è già `public` ([Program.cs](../../../backend/summerbooking-backend/Boot/Program.cs)), quindi
> `WebApplicationFactory<Boot.Program>` funziona senza `InternalsVisibleTo`. Serve il pacchetto
> `Microsoft.AspNetCore.Mvc.Testing`.

**Isolamento tra test.** Un container per l'intera collection (avviarlo per test costa decine di secondi);
isolamento dei dati tramite **transazione con rollback** a fine test oppure **dati unici per test** (guest id
generati). Non affidarsi a un `TRUNCATE` globale se i test girano in parallelo.

**Cosa verificare.**

- Contratto HTTP per endpoint: `201 + Location` sulla creazione, `200` sul get, `404` su id inesistente,
  `400` con corpo d'errore nel formato standard (vedi
  [standard-risposta-api-dotnet.md](pre/standard-risposta-api-dotnet.md)).
- Persistenza effettiva: dopo il `POST`, rileggere dal DB e verificare i valori scritti (non fidarsi della
  risposta, che potrebbe venire dalla memoria).
- **Nessun campo sensibile in risposta** (password hash, chiavi, id interni non previsti dal contratto):
  regressione documentata in [01-criticita §mass-assignment](01-criticita-e-indicazioni-backend.md).
- **Mass assignment**: inviare campi non previsti (`role`, `ownerId`, `price`) e verificare che vengano
  **ignorati**, non applicati.
- Gestione globale delle eccezioni: nessuna risposta contiene stack trace o dettagli interni
  (punto 8 delle indicazioni in [01-criticita](01-criticita-e-indicazioni-backend.md#L940)).

**Servizi esterni.** Stripe, Fiskaly, Telnyx, Meta e le API meteo
([`WeatherApiClient`](../../../backend/summerbooking-backend/Module.ZTest/Infrastructure/ExternalApi/WeatherApiClient.cs))
**non vanno mai chiamati** in test: si sostituisce l'`HttpClient` con un handler stub o si usa WireMock.Net.
Un test che dipende dalla rete di un fornitore è un test che diventerà rosso di venerdì sera per motivi non
tuoi.

---

## 5. Test del composition root (DI)

**Perché.** Intercetta a costo quasi nullo la classe di errore più fastidiosa in un monolite modulare: il
servizio che qualcuno dimentica di registrare in `Add<Modulo>Module()`, o il lifetime sbagliato. Senza
questo test l'errore compare **a runtime**, sulla prima richiesta all'endpoint, spesso in staging.

**Cosa verificare.**

```csharp
[Fact]
public void Il_container_si_costruisce_e_valida_tutti_i_servizi()
{
    var factory = new WebApplicationFactory<Boot.Program>()
        .WithWebHostBuilder(b => b.UseDefaultServiceProvider(o =>
        {
            o.ValidateOnBuild = true;    // tutte le dipendenze sono risolvibili
            o.ValidateScopes  = true;    // nessuno scoped catturato da un singleton
        }));

    Assert.NotNull(factory.Services.GetRequiredService<ReservationService>());
}
```

Aggiungere:

- **ogni modulo registra ciò che i suoi endpoint richiedono**: enumerare i parametri dei delegate degli
  endpoint mappati e verificare che siano risolvibili;
- **coerenza dei lifetime**: `ReservationService` è `Scoped` mentre repository e provider sono `Singleton`
  ([Module.cs](../../../backend/summerbooking-backend/Module.ZTest/Module.cs)) — `ValidateScopes` intercetta la
  combinazione inversa, che con un `DbContext` reale sarebbe un bug di concorrenza silenzioso;
- **configurazione obbligatoria presente**: l'app non parte se manca una connection string o una chiave
  richiesta (fail fast, invece di un `null` che esplode a metà di un pagamento);
- **health check risponde** (`/health` è già mappato in [Program.cs](../../../backend/summerbooking-backend/Boot/Program.cs)).

---

## 6. Test di autorizzazione per endpoint

**Perché.** È la contromisura esplicitamente richiesta al
[punto 10 delle indicazioni](01-criticita-e-indicazioni-backend.md#L948), contro le due regressioni
osservate nel legacy: middleware di auth **presente nel codice ma disattivato**, e **downgrade di
autorizzazione** (endpoint sensibile aperto a un ruolo più basso). Vedi anche
[autenticazione-autorizzazione-dotnet.md](pre/autenticazione-autorizzazione-dotnet.md).

Si articola in due test complementari.

### 6.1 Test di copertura (meta-test, veloce, senza DB)

Enumera **tutti** gli endpoint realmente mappati e fallisce se uno non è protetto e non è in una allow-list
esplicita di endpoint pubblici. È il test che rende impossibile "dimenticare" l'autorizzazione su un
endpoint nuovo: chi aggiunge una rotta pubblica deve dichiararlo, con una riga e un commento.

```csharp
[Fact]
public void Ogni_endpoint_e_protetto_o_dichiarato_pubblico()
{
    // rotte volutamente anonime: cambiare questa lista è una decisione, non una dimenticanza
    var pubbliche = new[] { "/health", "/", "/swagger/{documentName}/swagger.json", "/api/auth/login" };

    var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
        .OfType<RouteEndpoint>();

    var scoperti = endpoints
        .Where(e => e.Metadata.GetMetadata<IAuthorizeData>() is null)
        .Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is null)
        .Select(e => e.RoutePattern.RawText!)
        .Where(p => !pubbliche.Contains(p))
        .ToArray();

    Assert.True(scoperti.Length == 0, "Endpoint senza autorizzazione: " + string.Join(", ", scoperti));
}
```

### 6.2 Test di comportamento (per endpoint sensibile, con DB)

Per ogni endpoint che tocca denaro, dati personali o configurazione:

| Scenario | Atteso |
|---|---|
| Nessun token | `401` |
| Token valido, ruolo insufficiente | `403` |
| Token valido, risorsa di **un altro tenant/proprietario** | `404` (o `403`), mai `200` — anti-IDOR |
| Token scaduto / firma non valida | `401` |
| Ruolo corretto e risorsa propria | `200` |

Il caso IDOR è quello che vale di più: è la famiglia di vulnerabilità più diffusa nel backend legacy e
l'unica che nessun altro livello di test intercetta.

---

## 7. Contract test: snapshot dell'OpenAPI

**Perché.** L'API è consumata da più frontend (`front-app-pre`, `front-customer-pre`, `front-modern-pre`,
`front-admin/` — pannello systemadmin).
Un rename di campo o un cambio di tipo che passa inosservato in PR si scopre in produzione. Lo snapshot
trasforma ogni modifica del contratto in una **decisione esplicita e revisionabile in diff**.

**Come.** Per ogni documento Swagger dichiarato in `ApiDocs`
([Program.cs](../../../backend/summerbooking-backend/Boot/Program.cs)) si scarica lo `swagger.json` dal test server
e lo si confronta con un file approvato versionato in repo:

```csharp
[Theory]
[InlineData("auth")]
public async Task Il_contratto_OpenAPI_non_e_cambiato(string documento)
{
    var json = await client.GetStringAsync($"/swagger/{documento}/swagger.json");

    await Verify(json)                       // Verify.Xunit
        .UseFileName($"openapi-{documento}")
        .UseDirectory("Approved");
}
```

Quando il contratto cambia volutamente, si aggiorna il file approvato **nella stessa PR**: il reviewer vede
il diff del contratto accanto al diff del codice.

**Estensione utile:** un test che verifica che ogni endpoint mappato abbia un `documento` Swagger
associato — evita gli endpoint "fantasma", presenti ma non documentati (nel legacy erano una fonte di
endpoint dimenticati e non protetti).

---

## 8. Test sulle migrazioni EF Core

**Perché.** Due incidenti classici, entrambi rilevabili in pochi secondi:

1. il modello è stato modificato ma **non è stata generata la migrazione** → in locale funziona (DB già
   allineato a mano), in staging no;
2. la migrazione non è applicabile su un DB vuoto (dipendenze tra migrazioni, SQL custom sbagliato).

```csharp
[Fact]
public async Task Tutte_le_migrazioni_si_applicano_su_un_db_vuoto()
{
    await using var ctx = NuovoContext(connectionStringDelContainerVuoto);
    await ctx.Database.MigrateAsync();
    Assert.Empty(await ctx.Database.GetPendingMigrationsAsync());
}

[Fact]
public async Task Il_modello_non_ha_modifiche_prive_di_migrazione()
{
    await using var ctx = NuovoContext();
    var differ   = ctx.GetService<IMigrationsModelDiffer>();
    var snapshot = ctx.GetService<IMigrationsAssembly>().ModelSnapshot!.Model;

    var differenze = differ.GetDifferences(
        snapshot.GetRelationalModel(), ctx.GetService<IDesignTimeModel>().Model.GetRelationalModel());

    Assert.True(differenze.Count == 0,
        "Modello modificato senza migrazione: eseguire dotnet ef migrations add.");
}
```

**Nota per questo progetto**: dato che si ristruttura la base dati (uno schema per modulo, §12 delle
indicazioni), questi test vanno replicati **per ogni `DbContext` di modulo**, non solo per uno.

---

## 9. Test di concorrenza e atomicità

**Perché.** Il [punto 6 delle indicazioni](01-criticita-e-indicazioni-backend.md#L940) segnala che nel
legacy **nessuna** sequenza verifica→scrittura è atomica: disponibilità, stock, contatore voucher,
pagamento→booking→scontrino. Sono bug che non si manifestano mai in test sequenziali e sempre in alta
stagione.

**Come.** Test di integrazione (serve il DB reale, con i suoi lock e vincoli) che lanciano N richieste in
parallelo sulla stessa risorsa e verificano che **solo una vinca**:

```csharp
[Fact]
public async Task Due_prenotazioni_sovrapposte_in_parallelo_ne_creano_una_sola()
{
    var richiesta = new CreateReservationRequest(guestId, dal, al, 50m);

    var esiti = await Task.WhenAll(Enumerable.Range(0, 10)
        .Select(_ => client.PostAsJsonAsync("/api/ztest/reservations", richiesta)));

    Assert.Equal(1, esiti.Count(r => r.StatusCode == HttpStatusCode.Created));
    Assert.Equal(9, esiti.Count(r => r.StatusCode == HttpStatusCode.Conflict));
}
```

**Casi da coprire:** doppia prenotazione dello stesso ombrellone/periodo; doppio addebito sullo stesso
carrello; consumo doppio dello stesso voucher; emissione doppia dello scontrino fiscale; decremento sotto
zero di uno stock.

**Attenzione:** il test è significativo solo se la protezione è **nel DB** (vincolo di unicità, `SELECT …
FOR UPDATE`, transazione, token di concorrenza). Un controllo applicativo "verifica poi scrivi" passa i test
sequenziali e fallisce questo — che è esattamente il motivo per cui esiste.

---

## 10. Test di idempotenza dei webhook

**Perché.** [Punto 7 delle indicazioni](01-criticita-e-indicazioni-backend.md#L940): i provider (Stripe,
Fiskaly, Telnyx, Meta) **rispediscono** gli eventi. Senza idempotenza si generano doppi incassi, doppi
scontrini, doppie email.

**Cosa verificare, per ogni webhook:**

| Scenario | Atteso |
|---|---|
| Stesso evento (stesso id provider) inviato due volte | Un solo effetto; seconda risposta `200` (non un errore: il provider ritenterebbe) |
| Firma assente o non valida | `401`/`400`, nessun effetto |
| Payload di un evento non gestito | `200`, ignorato senza errore |
| Eventi ricevuti fuori ordine | Stato finale coerente |
| Evento con id già visto ma corpo diverso | Rifiutato o loggato, mai applicato silenziosamente |

La deduplicazione va verificata **sull'id evento del provider**, non sullo stato derivato dell'entità: è la
distinzione esplicitamente richiesta dalle indicazioni ed è ciò che questo test deve inchiodare.

---

## 11. Property-based test

**Perché.** Sulle logiche combinatorie (date, prezzi, sconti) gli esempi scritti a mano coprono i casi a cui
si pensa; i property-based generano centinaia di casi a cui non si pensa: periodi di durata zero, a cavallo
di anno, con `from == to`, importi che sfiorano l'arrotondamento.

**Dove applicarli** (non ovunque — solo dove esiste una proprietà matematica vera):

- `DateRange.Overlaps` è **simmetrico**: `a.Overlaps(b) == b.Overlaps(a)`;
- `Nights` è sempre `≥ 0` e coerente con la differenza di date;
- il totale è **monotono** rispetto al prezzo per notte e al numero di notti;
- applicare uno sconto non produce mai un totale negativo né superiore all'imponibile;
- somma degli importi di riga == totale documento, per qualunque combinazione di righe (invariante fiscale).

```csharp
[Fact]
public void La_sovrapposizione_e_simmetrica()
    => Gen.Select(GenDateRange, GenDateRange)
          .Sample((a, b) => a.Overlaps(b) == b.Overlaps(a));   // CsCheck
```

Quando un property-based test trova un controesempio, si **congela quel caso come unit test** (§2): serve a
non riperderlo e a documentare il bug.

---

## 12. Mutation testing

**Perché.** Non aggiunge test: misura se quelli esistenti servono davvero. Stryker.NET altera il codice
(inverte condizioni, cambia costanti, rimuove chiamate) e verifica che qualche test diventi rosso. Un
mutante sopravvissuto = una riga coperta dalla copertura ma **non verificata** da nessuna asserzione — cioè
copertura finta, il rischio principale quando si introduce un gate di coverage (§15).

**Come usarlo senza farsi male:**

- solo sui progetti `Domain` e `Application` (dove i test sono veloci e la logica è densa);
- esecuzione **notturna o settimanale**, mai nel gate di PR: dura decine di minuti;
- soglie: `break` sotto 60%, `low` 70%, `high` 80% di mutation score, alzate gradualmente;
- si introduce **dopo** che §2 e §3 hanno una base decente, altrimenti produce solo rumore.

---

## 13. Test di carico e performance

**Perché.** Il carico dell'applicativo è stagionale e concentrato: i problemi di performance emergono tutti
insieme, nel momento peggiore. Un test di carico in pipeline non serve a "misurare la velocità", serve a
**intercettare la regressione**: la query che ieri era 20 ms e oggi è 400 ms perché qualcuno ha rimosso un
`Include` o introdotto una N+1.

**Come.**

- Script k6 (o NBomber, se si preferisce restare in C#) versionati in `perf/`, sugli endpoint critici:
  ricerca disponibilità, creazione prenotazione, pagamento, lista configurazioni.
- Esecuzione **notturna su staging** con dati realistici, non sul gate di PR (rumorosa e lenta).
- **Soglie che fanno fallire il job**, altrimenti è solo un grafico che nessuno guarda:
  es. `p(95) < 300ms`, `http_req_failed < 1%`.
- Complementare e più economico: un test di integrazione che **conta le query** eseguite da un endpoint
  (interceptor EF) e fallisce se superano una soglia — intercetta le N+1 nel gate di PR, in millisecondi,
  senza bisogno di generare carico.

---

## 14. Test E2E utente

**Perché.** Verificano il flusso completo attraverso l'interfaccia, che è l'unica cosa che l'utente vede.

**Quando.** Fase successiva, quando esisterà il nuovo frontend: prima di allora non hanno un bersaglio
stabile.

**Regole per non farsi male** (l'E2E è il livello che più facilmente diventa un costo netto):

- pochi test, solo sui **percorsi che generano fatturato**: prenotazione completa, pagamento, check-in;
- selettori stabili (`data-testid`), mai testo o classi CSS;
- nessuna attesa a tempo: solo attese su condizione (Playwright lo fa nativamente);
- ambiente dedicato con dati inizializzati via API, non via UI;
- esecuzione notturna e pre-release, non su ogni PR.

---

## 15. Gate statici, copertura e sicurezza

Non sono "test" in senso stretto ma occupano lo stesso posto in pipeline: fanno fallire la build.

| Gate | Strumento | Quando | Soglia |
|---|---|---|---|
| Warning come errori | `<TreatWarningsAsErrors>` + analyzers .NET | Build | Zero warning |
| Formattazione | `dotnet format --verify-no-changes` | Gate PR | Nessuna differenza |
| Nullable | `<Nullable>enable</Nullable>` (già attivo) | Build | Zero warning |
| API vietate | `Microsoft.CodeAnalysis.BannedApiAnalyzers` | Build | `DateTime.Now`, `Guid.NewGuid()` (usare `CreateVersion7`, vedi [identificatori-e-uuid.md](pre/identificatori-e-uuid.md)), `Console.*` |
| Copertura | coverlet (già referenziato) + ReportGenerator | Gate PR | Domain ≥ 90%, Application ≥ 80%, **non deve mai scendere** rispetto a main |
| Pacchetti vulnerabili | `dotnet list package --vulnerable --include-transitive` | Gate PR + notturno | Zero High/Critical |
| Segreti nel repo | gitleaks / secret scanning | Gate PR | Zero (nel legacy erano committate chiavi Fiskaly e Stripe) |
| Analisi statica sicurezza | CodeQL | Notturno | Zero High |
| Scansione dinamica | OWASP ZAP baseline su staging | Notturno | Zero High |

**Sulla copertura:** usare la soglia come *rete*, non come obiettivo. La metrica utile è "la copertura non
scende rispetto a `main`"; l'obiettivo assoluto ha senso solo sul `Domain`, dove la logica è densa e pura.
Chi insegue una percentuale scrive test che chiamano codice senza verificarlo — ed è esattamente ciò che il
mutation testing (§12) smaschera.

---

## 16. Organizzazione della pipeline

| Stage | Contenuto | Budget di tempo | Blocca |
|---|---|---|---|
| **Build** | compilazione, analyzers, format, nullable | < 2 min | sempre |
| **Gate PR — veloce** | §1 architettura, §2 dominio, §3 application, §5 DI, §6.1 copertura auth, §7 contratto, §11 property-based, coverage, pacchetti vulnerabili, segreti | **< 5 min** | sempre |
| **Gate PR — integrazione** | §4 integrazione, §6.2 autorizzazione, §8 migrazioni | < 15 min | sempre (se l'infrastruttura Docker in CI è affidabile; altrimenti su merge) |
| **Merge su main** | tutto il precedente + §9 concorrenza, §10 idempotenza | < 25 min | sempre |
| **Notturno** | §12 mutation, §13 carico, §14 E2E, CodeQL, ZAP | libero | apre un issue, non blocca la merge |

**Regola di igiene:** il gate veloce deve restare sotto i 5 minuti. Se supera stabilmente quella soglia,
gli sviluppatori smettono di aspettarlo e iniziano a mergiare "tanto poi si vede" — a quel punto la pipeline
esiste ma non protegge più nulla.

---

## 17. Ordine di adozione consigliato

1. **§1** — completare architecture e convention test: costo quasi nullo, infrastruttura già in piedi.
2. **§2 + §3** — unit test di dominio e application sul modulo template
   [`Module.ZTest`](../../../backend/summerbooking-backend/Module.ZTest/), così da avere anche per i test un
   modello replicabile come lo è oggi la struttura delle cartelle.
3. **§5** — validazione del container: mezz'ora di lavoro, elimina un'intera classe di errori a runtime.
4. **§4** — infrastruttura di integrazione (Testcontainers + `WebApplicationFactory`): è l'investimento
   principale, e abilita §6.2, §8, §9, §10.
5. **§6** — autorizzazione: è il gate che risponde direttamente alle criticità di sicurezza rilevate nel
   legacy.
6. **§7 + §8** — contratto API e migrazioni, appena il primo modulo reale espone endpoint e schema.
7. **§9 + §10** — concorrenza e idempotenza, insieme ai moduli Payment e Fiskaly.
8. **§11 → §15** — property-based, mutation, carico, E2E: quando la base è stabile.

### Definition of done di un modulo nuovo

Un modulo non è "finito" finché non ha:

- [ ] regole di confine in `ArchitectureTests` (§1), incluse le convenzioni;
- [ ] unit test su tutte le invarianti degli aggregate (§2);
- [ ] unit test su tutti i percorsi d'errore degli use case (§3);
- [ ] almeno un test di integrazione per endpoint esposto (§4);
- [ ] test di autorizzazione per ogni endpoint non pubblico (§6);
- [ ] snapshot OpenAPI approvato (§7);
- [ ] test di migrazione sul proprio `DbContext` (§8);
- [ ] test di concorrenza per ogni sequenza verifica→scrittura (§9);
- [ ] test di idempotenza per ogni webhook esposto (§10).
