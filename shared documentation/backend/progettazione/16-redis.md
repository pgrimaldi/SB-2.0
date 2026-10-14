# Cache (Redis) — predisposizione e attivazione

> Documento di indirizzo su **caching** nel nuovo backend .NET 10: se serve, per cosa, con quale
> astrazione nel codice e con quale criterio si decide di attivarla davvero in infrastruttura.
> Applica lo stesso schema a strati di [15-logs.md](15-logs.md) (astrazione/provider/sink) al caso
> cache. Complemento a [pre/standard-risposta-api-dotnet.md §7](pre/standard-risposta-api-dotnet.md)
> (cosa NON va in cache), [pre/linee-guida-implementative-dotnet.md §7](pre/linee-guida-implementative-dotnet.md)
> (dove vive `ICache` nella solution) e [infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md)
> (ElastiCache Redis, oggi opzionale).
>
> **Documento vivo**: da aggiornare quando si decide di attivare Redis davvero (§5) o emergono nuovi casi.

---

## 0. Principio guida

**Predisposizione subito, infrastruttura Redis dopo.** Stessi tre strati del logging:

| Strato | Cosa | Scelta |
|---|---|---|
| **Astrazione** | `ICache` nello **SharedKernel** | Da definire subito; i moduli dipendono **solo** da questa |
| **Provider** | Implementazione concreta, registrata **una volta nell'host/Boot** | **In-memory** (`IMemoryCache`) per ora |
| **Sink** | Dove vive fisicamente la cache | Nessun ElastiCache finché non c'è un bisogno reale misurato |

Coerente con la regola-bussola trasversale delle linee guida implementative: astrazioni → shared puro;
implementazioni con dipendenze (Redis incluso) → shared infrastruttura o host, mai nel Domain.

---

## 1. Perché non subito — cosa dicono già gli altri documenti

- [refactoring-analisi-A-vs-B.md §6.5](pre/refactoring-analisi-A-vs-B.md): nel legacy Redis era già
  presente e usato sulle "viste calde", ma è un **palliativo a tetto basso** — aiuta le letture
  ripetute, **non** le query che filtrano/ordinano dentro i JSON né il write-path. Con lo schema
  relazionale nuovo (PostgreSQL, non più JSON) buona parte della pressione che imponeva Redis nel
  legacy si riduce alla radice con indici propri: la cache serve a scaricare letture ripetute, non a
  compensare un modello dati inefficiente.
- [infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md): ElastiCache Redis è segnato **opzionale**
  (1 nodo `t4g.micro` → ~$15/mese) accanto a ECS Fargate + RDS. Baseline è **1 task** in bassa
  stagione, 3-6 in alta stagione (§3.1) — con un solo task attivo la cache in-memory non ha problemi
  di coerenza tra istanze; il problema si pone solo scalando orizzontalmente.
- Introdurre subito un componente in più (Redis) significa aggiungere costo fisso, un nuovo punto di
  failure di rete e complessità di invalidazione **prima** di sapere quali viste ne hanno davvero
  bisogno. Meglio partire dalla predisposizione e attivare quando le metriche di produzione lo
  richiedono.

---

## 2. Predisposizione — l'astrazione `ICache`

**Scelta**: interfaccia unica nello SharedKernel, i moduli non sanno mai se sotto c'è memoria locale o
Redis.

```csharp
// SummerBooking.SharedKernel/Caching/ICache.cs
public interface ICache
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);
}
```

- Nessuna dipendenza da `StackExchange.Redis`/`IDistributedCache` nello SharedKernel: solo tipi propri
  (coerente con la regola "SharedKernel puro" di
  [linee-guida-implementative-dotnet.md §7](pre/linee-guida-implementative-dotnet.md) — se Redis
  finisse lì, ogni Domain se lo trascinerebbe come dipendenza transitiva).
- **Chiavi namespacate per modulo**: `booking:{propertyId}:availability:{date}`,
  `catalog:{propertyId}:services:{locale}` — evita collisioni quando più moduli condividono la stessa
  cache fisica, e rende leggibile un'invalidazione mirata.
- **TTL sempre esplicito** nel chiamante (nessun default silenzioso): chi mette in cache decide per
  quanto un dato stantio è accettabile.

### Provider di default: in-memory

```csharp
// Boot/Program.cs (o BuildingBlocks.Infrastructure)
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICache, InMemoryCache>(); // wrapper sottile su IMemoryCache
```

`InMemoryCache` è l'unica implementazione presente finché Redis non è attivo: nessun pacchetto Redis
referenziato, nessun costo, nessuna infrastruttura da tenere su.

### Uso nei moduli (Application/Infrastructure, mai nel Domain)

```csharp
var availability = await _cache.GetOrSetAsync(
    $"booking:{propertyId}:availability:{date:yyyy-MM-dd}",
    ct => _availabilityRepository.ComputeAsync(propertyId, date, ct),
    ttl: TimeSpan.FromMinutes(5));
```

Stesso principio di [15-logs.md §7](15-logs.md): la cache è un dettaglio tecnico di **Infrastructure**
(o di un application service che orchestra una repository), mai qualcosa che il Domain conosce.

---

## 3. Cosa NON va in cache (richiamo)

Da [standard-risposta-api-dotnet.md §7](pre/standard-risposta-api-dotnet.md), decisione già presa:

- **Mai i messaggi d'errore** — li possiede il frontend (`code` + `args`), o `.resx` in memoria lato
  backend se serve testo server-side. Un insieme finito di errori non giustifica un round-trip a
  Redis.
- La cache è per **contenuto di dominio su DB, read-heavy, scritto raramente** (nomi servizi,
  descrizioni di catalogo multilingua, viste aggregate costose) — non per dati che cambiano ad ogni
  richiesta o per stato che deve essere sempre fresh (es. disponibilità durante il checkout).

---

## 4. Candidati quando si attiverà (non ora)

Elenco preparatorio, da usare come checklist quando scatta un trigger di §5 — **non** implementare
finché il trigger non si verifica:

| Candidato | Perché è read-heavy/poco volatile | Modulo |
|---|---|---|
| Catalogo servizi/descrizioni multilingua | Cambia raramente (gestione manager), letto ad ogni pagina cliente | Configuration |
| Disponibilità aggregata per vista calendario (non lo slot esatto in checkout) | Tollerante a qualche minuto di staleness fuori dal flusso di pagamento | Booking |
| Configurazioni di stabilimento (prezzi base, regole, orari) | Scritte dal manager poche volte al giorno, lette ad ogni richiesta pubblica | Configuration |
| Backplane SignalR/notifiche realtime multi-istanza | Necessario **solo** se si introduce realtime e si scala a >1 istanza stabile (nel legacy Socket.io richiedeva un adapter Redis per questo) | Notification/cross-cutting |
| Distributed lock per job/cron su più istanze | Alternativa a `pg_advisory_lock` di PostgreSQL già citata in [infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md); necessaria solo con più istanze del Worker attive contemporaneamente | cross-cutting ([20-jobs.md](20-jobs.md)) |

**Esplicitamente escluso**: liste/report che filtrano o ordinano su campi che nel legacy erano dentro
JSON. Con lo schema relazionale nuovo si risolvono con indici; se il problema si ripresentasse, è un
sintomo di modellazione dati da correggere, non un caso d'uso per la cache.

---

## 5. Trigger di attivazione — decisione

**Scelta: si attiva Redis quando si verifica uno di questi eventi concreti, non preventivamente.**

| Trigger | Segnale | Azione |
|---|---|---|
| **Scaling orizzontale stabile** | ECS Fargate scala oltre 1 task **in modo continuativo** (non solo il picco di un giorno) durante l'alta stagione | Rivalutare subito per ogni dato in cache che deve essere **coerente tra istanze** (es. disponibilità): l'in-memory per-istanza diventa incoerente prima che serva davvero come cache di scarico |
| **Vista calda misurata** | Metriche/CloudWatch mostrano una query/endpoint specifico con carico DB sproporzionato rispetto al traffico, e il dato è read-heavy per natura (§4) | Introdurre Redis solo per **quella** chiave, non "in generale" |
| **Realtime multi-istanza** | Si introduce SignalR/notifiche push e si scala oltre 1 istanza | Serve un backplane condiviso: qui Redis non è più opzionale |
| **Job/cron multi-istanza** | Il Worker gira su più di un'istanza contemporaneamente | Serve un lock distribuito: Redis o `pg_advisory_lock` (valutare in quel momento quale costa meno operativamente) |

**Non sono trigger validi**: "Redis c'era nel legacy", "potrebbe servire in futuro", "costa poco
comunque" — tutti motivi che nel vecchio sistema hanno portato a introdurre un componente usato solo
in parte (§1) senza risolvere il problema di fondo.

### Attivazione pratica (quando scatta un trigger)

1. Aggiungere `SummerBooking.BuildingBlocks.Infrastructure` (se non già presente) con la dipendenza a
   `StackExchange.Redis`/`Microsoft.Extensions.Caching.StackExchangeRedis`.
2. Nuova implementazione `RedisCache : ICache` lì, **non** nello SharedKernel.
3. Cambiare solo la registrazione DI nell'host (`AddSingleton<ICache, RedisCache>()` al posto di
   `InMemoryCache`) — **zero modifiche nei moduli** che già usano `ICache`.
4. Attivare ElastiCache in [infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) (nodo `t4g.micro`
   come da stima, in subnet privata come nel diagramma §2).
5. Strumentare cache hit/miss nei log a profilo **P3** (diagnostico), come già previsto in
   [15-logs.md §4](15-logs.md) — "cache hit/miss" è esplicitamente elencato tra gli eventi P3.

---

## 6. Riepilogo delle decisioni

1. **Ora**: predisposizione soltanto — interfaccia `ICache` nello SharedKernel, implementazione
   **in-memory** nell'host, nessuna infrastruttura Redis attiva.
2. **Cosa NON va mai in cache**: messaggi d'errore (decisione già presa altrove); dati che devono
   essere sempre fresh nel percorso di pagamento/checkout.
3. **Candidati futuri**: catalogo multilingua, configurazioni di stabilimento, viste aggregate
   read-heavy, backplane realtime, lock distribuito — non implementati finché non serve.
4. **Trigger di attivazione**: scaling orizzontale stabile, vista calda misurata da metriche,
   introduzione di realtime multi-istanza, Worker multi-istanza — mai per precauzione.
5. **Attivazione**: si cambia solo il provider (`RedisCache` in `BuildingBlocks.Infrastructure` +
   registrazione DI nell'host); i moduli che già usano `ICache` non cambiano una riga.
