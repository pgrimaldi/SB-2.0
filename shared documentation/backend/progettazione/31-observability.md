# Osservabilità (log, metriche, tracce)

> Come si rende osservabile il nuovo backend **.NET 10** (monolite modulare su AWS ECS): quali segnali
> si raccolgono, con che stack, dove si collocano nel codice, quali dashboard/alert servono e in che
> ordine si adotta il tutto.
>
> Approfondisce ed estende la nota iniziale di sessione; complemento a [15-logs.md](15-logs.md)
> (logging strutturato, profili di verbosità, redazione PII), [pre/standard-risposta-api-dotnet.md §3](pre/standard-risposta-api-dotnet.md)
> (`traceId` di correlazione nel formato errore), [02-strategia-di-test.md §13](02-strategia-di-test.md)
> (soglie dei test di carico, riusate come soglie di alert), [20-jobs.md](20-jobs.md) (osservabilità
> dei job), [34-multitenant.md](34-multitenant.md) (dimensione `tenant` sulle metriche) e
> [../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) (CloudWatch, X-Ray, ADOT sidecar).
>
> **Documento vivo**: da estendere quando il primo modulo reale (oltre `Module.ZTest`) va in produzione
> e quando si definiscono SLO formali.

---

## 0. Principio guida

Tre segnali distinti, **un solo id di correlazione** che li lega.

| Segnale | Domanda a cui risponde | Costo | Priorità |
|---|---|---|---|
| **Log strutturati** | *Cosa è successo esattamente in questa richiesta?* | Basso | Subito ([15-logs.md](15-logs.md)) |
| **Metriche** | *Il sistema sta bene? Sta peggiorando?* | Basso (base) / medio (dominio) | Presto |
| **Tracce distribuite** | *Dov'è il tempo speso / dov'è il guasto lungo la catena di chiamate?* | Medio (campionabile) | Quando il primo modulo va in produzione |

Regole trasversali:

- **Un solo `traceId`** (`Activity.TraceId` di .NET = `trace_id` di OpenTelemetry) presente in ogni log,
  in ogni metrica exemplar e in ogni traccia. È lo stesso `traceId` restituito nel Problem Details
  ([pre/standard-risposta-api-dotnet.md §3](pre/standard-risposta-api-dotnet.md)): dal messaggio
  d'errore mostrato all'utente si arriva alla traccia completa senza passaggi manuali.
- **Niente stack self-hosted** (Grafana/Tempo/Loki/Prometheus gestiti da noi) quando l'infrastruttura è
  già interamente AWS: si paga in complessità operativa ciò che CloudWatch/X-Ray danno gestito.
- **Nessuna dashboard senza un alert**: una dashboard che nessuno guarda non serve; ogni pannello
  importante ha una soglia che genera una notifica.
- **La strumentazione sta nell'Infrastructure del modulo**, mai nel Domain — vale per le metriche di
  dominio come per tutto il resto ([pre/linee-guida-implementative-dotnet.md §3](pre/linee-guida-implementative-dotnet.md)).

---

## 1. Stack tecnico

| Strato | Scelta | Note |
|---|---|---|
| **SDK applicativo** | OpenTelemetry .NET SDK | Auto-strumentazione per ASP.NET Core, `HttpClient`, EF Core, Npgsql; `Meter`/`Activity` per i segnali custom |
| **Logging** | `ILogger<T>` → Serilog (JSON) con enricher OTel | Vedi [15-logs.md](15-logs.md); l'enricher inietta `trace_id`/`span_id` in ogni riga |
| **Collector** | AWS Distro for OpenTelemetry (**ADOT**) come **sidecar** del task ECS | Riceve OTLP dall'app, esporta verso CloudWatch (log/metriche) e X-Ray (tracce). Un solo punto di configurazione degli export |
| **Backend metriche/log** | CloudWatch (Embedded Metric Format per le metriche custom, Log Insights per le query) | |
| **Backend tracce** | AWS X-Ray | Service map, analisi latenza per segmento |
| **Alerting** | CloudWatch Alarms → SNS → email / Slack | Un topic SNS per severità (`page` vs `notify`) |
| **Dashboard** | CloudWatch Dashboards, **una per modulo** | Stesso raggruppamento di Swagger ([pre/linee-guida-implementative-dotnet.md §10](pre/linee-guida-implementative-dotnet.md)) e dei profili di log |

Registrazione (una volta, nell'host `Boot`):

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("summerbooking-api")
                             .AddAttributes(new KeyValuePair<string, object>[]
                             {
                                 new("deployment.environment", env.EnvironmentName)
                             }))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddNpgsqlInstrumentation()
        .AddMeter("SummerBooking.*")          // metriche di dominio dei moduli
        .AddOtlpExporter())
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation(o => o.RecordException = true)
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(o => o.SetDbStatementForText = false) // no SQL con parametri
        .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.1)))       // vedi §5
        .AddOtlpExporter());
```

---

## 2. Log — cosa aggiunge l'osservabilità a [15-logs.md](15-logs.md)

Il logging strutturato è già deciso e descritto in [15-logs.md](15-logs.md). Qui si aggiungono solo i
punti di integrazione:

- **Correlazione**: enricher OTel su Serilog → ogni riga porta `trace_id`, `span_id`, e le baggage
  rilevanti (`tenant_id`, `user_id`, `module`).
- **Destinazione**: `stdout` → CloudWatch Logs (via ADOT o awslogs driver) → S3 per l'archiviazione a
  lungo termine, coerente con [15-logs.md](15-logs.md). Nessun file di log locale.
- **Query operative**: CloudWatch Log Insights come strumento di indagine (`filter trace_id = "..."`,
  `stats count() by module, level`); i saved query più usati vanno versionati in `infra/`.
- **Log dei job**: i `BackgroundService`/worker loggano sullo stesso stack con `module` = modulo
  proprietario del job e un campo `job_name` ([20-jobs.md](20-jobs.md)).

---

## 3. Metriche

### 3.1 Metriche di base (gratis dall'auto-strumentazione)

- **HTTP server** (RED): `http.server.request.duration` (istogramma), conteggio richieste, per
  `route` + `status`. Da qui derivano rate, error rate, p50/p95/p99 per endpoint e per modulo.
- **HTTP client**: durata e esito delle chiamate uscenti verso i servizi esterni (Stripe, Fiskaly,
  Telnyx, SendGrid, S3) — la base per gli alert di §6 sulle integrazioni.
- **Runtime .NET**: GC, heap, thread pool, `ThreadPool` queue length, exception rate.
- **EF Core / Npgsql**: durata query, connessioni attive/in attesa dal pool, timeout.
- **Kestrel / connessioni**: connessioni attive, code.

Tutte taggate almeno con `module` (dal segmento di route / assembly dell'endpoint) e, dove ha senso,
`tenant` a **bassa cardinalità** (vedi §3.3).

### 3.2 Metriche di dominio (esplicite, una `Meter` per modulo)

Emesse con `System.Diagnostics.Metrics.Meter`, registrata nell'**Infrastructure del modulo** che
possiede l'evento. Nome `Meter` = `SummerBooking.<Modulo>`; nomi metrica `snake_case` con prefisso di
dominio.

| Modulo | Metrica | Tipo | A cosa serve |
|---|---|---|---|
| Booking | `booking_created_total` | Counter (per `channel`: online/operatore, `tenant`) | Volume prenotazioni; anomalie di calo |
| Booking | `booking_hold_expired_total` | Counter | Hold scaduti senza conferma (indica frizione nel checkout) |
| Booking | `booking_overbooking_prevented_total` | Counter | Volte che il constraint di esclusione ha bloccato una doppia prenotazione ([03-gestione-dati.md](03-gestione-dati.md)) |
| Payment | `payment_attempt_total` | Counter (per `outcome`: ok/declined/error) | Tasso di fallimento pagamenti |
| Payment | `payment_gateway_latency` | Histogram | Latenza Stripe lato nostro |
| Payment | `payment_webhook_processed_total` | Counter (per `type`, `outcome`) | Webhook Stripe elaborati / falliti / duplicati scartati |
| Fiskaly | `receipt_issued_total` / `receipt_failed_total` | Counter | Emissione scontrini; un fallimento qui è ad alta priorità |
| Notification | `notification_sent_total` | Counter (per `channel`: email/sms/whatsapp/inapp, `outcome`) | Deliverability |
| Jobs (tutti) | `job_run_total` (per `job_name`, `outcome`), `job_duration` | Counter + Histogram | Salute dei job schedulati ([20-jobs.md](20-jobs.md)) |
| Outbox | `outbox_pending`, `outbox_dispatch_lag_seconds` | Gauge / Histogram | Ritardo di elaborazione degli effetti collaterali; se cresce, gli eventi non partono |
| Auth | `auth_login_failed_total`, `auth_token_refresh_total` | Counter | Credential stuffing, salute del flusso di refresh |

### 3.3 Cardinalità — regole per non far esplodere i costi

- **Mai** come dimensione di metrica: id di risorsa (bookingId, customerId), email, IP, URL con id
  inline, `trace_id`.
- `tenant` come dimensione **solo** su un numero ristretto di metriche di business chiave (`booking_created_total`,
  `payment_attempt_total`, `receipt_*`), non su tutte — con ~500 tenant è cardinalità accettabile solo
  se contenuta a poche metriche. Per il resto, il breakdown per tenant si fa da log/tracce on-demand.
- `route` deve essere il **template** (`/api/hub/bookings/{id}`), mai l'URL concreto.

---

## 4. Tracce distribuite

- **Auto-span**: richiesta HTTP in ingresso → handler → query EF → chiamata `HttpClient` uscente. Già
  coperto dall'auto-strumentazione.
- **Span custom** solo dove aggiungono informazione non deducibile: l'elaborazione di un evento di
  dominio dall'outbox, un blocco di calcolo prezzo, una saga (es. `ErasureRequest` di
  [11-modulo-privacy.md §5](11-modulo-privacy.md)). `ActivitySource` = `SummerBooking.<Modulo>`.
- **Attributi utili sugli span** (bassa cardinalità o esplicitamente ad alta ma solo su span, non
  metriche): `tenant.id`, `module`, `booking.channel`, `payment.outcome`, `idempotency.hit`.
- **Eccezioni**: `RecordException = true` sull'auto-strumentazione ASP.NET Core; l'eccezione di dominio
  tipizzata ([pre/standard-risposta-api-dotnet.md §6](pre/standard-risposta-api-dotnet.md)) finisce
  come evento sullo span con il suo `code`.
- **Propagazione futura**: quando un modulo viene estratto in microservizio, il context W3C
  `traceparent` viaggia già sull'`HttpClient` — la traccia resta continua senza modifiche al codice
  ([pre/architettura-evolutiva-microservizi.md](pre/architettura-evolutiva-microservizi.md)).

---

## 5. Campionamento e costo

- **Tracce**: `ParentBased(TraceIdRatioBased(0.10))` come default — 10% delle richieste in successo.
  **Tutte** le richieste in errore (5xx, o 4xx "di business" rilevanti) vengono forzate a
  campionamento 100% tramite un `Sampler` custom / tail-sampling nell'ADOT collector. Così si tiene
  sotto controllo il costo di X-Ray senza perdere visibilità sugli incidenti.
- **Metriche**: aggregazione a 1 minuto; le metriche di dominio a bassa frequenza si possono lasciare
  a 1 minuto senza problemi di costo.
- **Log**: la verbosità è già governata dai 5 profili di [15-logs.md](15-logs.md); in produzione il
  default è `Information` con floor `Debug`/`P3` permanente sui moduli critici (Payment, Fiskaly).
- **Revisione trimestrale** della spesa CloudWatch/X-Ray: se un segnale costa più di quanto informa, si
  taglia (riduzione ratio di sampling, drop di metriche inutilizzate).

---

## 6. Dashboard e alert

### 6.1 Dashboard

Una dashboard **per modulo**, stessa struttura:

1. **RED della API del modulo**: rate, error rate, p95/p99 per endpoint.
2. **Dipendenze esterne** del modulo: latenza ed error rate per ogni `HttpClient` (Stripe, Fiskaly…),
   stato del circuit breaker ([35-integrazioni-esterne.md §2](35-integrazioni-esterne.md)).
3. **Metriche di dominio** del modulo (§3.2).
4. **Job & outbox** di competenza del modulo: ultimo run, esito, `outbox_dispatch_lag_seconds`.
5. **DB**: durata query p95, connessioni pool in attesa.

Una dashboard **"Overview"** aggregata per la vista a colpo d'occhio (error rate globale, p95 globale,
outbox lag globale, job falliti nelle ultime 24h).

### 6.2 Alert (soglie iniziali)

Soglie allineate a quelle dei test di carico ([02-strategia-di-test.md §13](02-strategia-di-test.md):
p95 < 300 ms, error rate < 1%). Due severità: **page** (intervento immediato) e **notify** (guardare a
orario d'ufficio).

| Alert | Condizione | Severità |
|---|---|---|
| Error rate API elevato | 5xx rate > 1% su 5 min (per modulo) | page |
| Latenza API degradata | p95 > 300 ms per 10 min (per modulo) | notify |
| Latenza API grave | p95 > 1 s per 5 min | page |
| Pagamenti falliti | `payment_attempt_total{outcome="error"}` > soglia su 15 min | page |
| Scontrino Fiskaly fallito | `receipt_failed_total` > 0 su 5 min | page |
| Webhook Stripe non elaborati | backlog o error rate webhook > soglia | page |
| Circuit breaker aperto | breaker su un client esterno critico in stato `Open` | page |
| Outbox in ritardo | `outbox_dispatch_lag_seconds` p95 > 60 s per 10 min | notify (page se > 300 s) |
| Job fallito | `job_run_total{outcome="failed"}` > 0 (job che toccano soldi) | page |
| Job non eseguito | nessun `job_run_total` per un job nella finestra attesa | notify |
| Connessioni DB sature | pool in attesa > 0 per 5 min | notify |
| Login falliti anomali | `auth_login_failed_total` oltre baseline (possibile credential stuffing) | notify |
| Spesa osservabilità | budget CloudWatch/X-Ray mensile > soglia | notify |

### 6.3 Health check e sonde

- `/health/live` (il processo è vivo) e `/health/ready` (DB raggiungibile, migrazioni applicate,
  dipendenze critiche ok) via `Microsoft.Extensions.Diagnostics.HealthChecks` — usati dall'ALB/ECS per
  il routing e il replacement dei task.
- Un **canary sintetico** (CloudWatch Synthetics) su 1-2 percorsi critici (login, lettura mappa) ogni
  N minuti: rileva un'indisponibilità prima che lo faccia un utente.

---

## 7. Ordine di adozione

1. **Structured logging** — già deciso ([15-logs.md](15-logs.md)), costo minimo. Enricher OTel per il
   `trace_id` fin da subito.
2. **Metriche HTTP di base + runtime** — solo auto-strumentazione + export OTLP: minuti di lavoro.
   Prima dashboard "Overview" e i 3-4 alert fondamentali (error rate, p95, health).
3. **ADOT sidecar** nella task definition ECS + export verso CloudWatch/X-Ray.
4. **Tracing distribuito** con campionamento — quando il primo modulo reale (oltre `Module.ZTest`) va
   in produzione.
5. **Metriche di dominio + dashboard per modulo + alert completi** — modulo per modulo, insieme alla
   sua messa in produzione. Payment e Fiskaly per primi per severità.
6. **Canary sintetici + SLO formali** — quando il servizio ha traffico reale e una baseline.

---

## 8. Cosa NON fare

- Non introdurre Prometheus/Grafana/Tempo self-hosted "perché è lo standard": l'infra è AWS, il costo è
  operativo.
- Non mettere id di risorsa o PII come dimensioni di metrica (§3.3) né come tag di traccia a tappeto.
- Non loggare a `Debug`/`Trace` in produzione su tutti i moduli per "stare tranquilli": esistono i
  profili e il `LoggingLevelSwitch` con TTL ([15-logs.md](15-logs.md)).
- Non creare dashboard senza almeno un alert collegato.
- Non attivare il tracing al 100% in produzione: il costo X-Ray cresce linearmente col traffico.
