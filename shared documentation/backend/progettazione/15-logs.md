# Log applicativi

> Documento di indirizzo su **logging** nel nuovo backend .NET 10: libreria/i, dove finiscono i log,
> come integrarli nel codice, e un modello di **quantità di log** (verbosità) attivabile/disattivabile
> per ambiente e per modulo. Complemento a
> [pre/linee-guida-implementative-dotnet.md](pre/linee-guida-implementative-dotnet.md) (confini tra
> layer, a cui il posizionamento del logging nel codice si allinea),
> [pre/standard-risposta-api-dotnet.md](pre/standard-risposta-api-dotnet.md) (`traceId` di correlazione
> in Problem Details) e [infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) (§3.5 CloudWatch,
> §3.7 job/worker separati dal web). Risponde alla voce "Gestione log" di [note.md](note.md).
>
> **Documento vivo**: da estendere quando emergono nuovi casi.

---

## 0. Principio guida

Tre strati distinti, da non confondere:

| Strato | Cosa | Regola |
|---|---|---|
| **Astrazione** | `ILogger<T>` (`Microsoft.Extensions.Logging`) | Il codice dei moduli dipende **solo** da questa. Mai da Serilog/NLog direttamente. |
| **Provider** | Serilog / OpenTelemetry | Configurato **una volta nell'host `Boot`**, invisibile ai moduli. |
| **Sink** | dove finiscono i log (stdout, file, CloudWatch, Seq…) | Scelta infrastrutturale, cambiabile senza toccare i moduli. |

Coerente con la regola-bussola trasversale delle linee guida implementative: config/infrastruttura
trasversale → host; i moduli restano agnostici rispetto a "come" e "dove" si logga.

---

## 1. Libreria — decisione

**Scelta: `ILogger<T>` nel codice → provider Serilog nell'host, formato JSON strutturato su `stdout`,
affiancato da OpenTelemetry per il tracing distribuito.**

| Opzione | Pro | Contro | Decisione |
|---|---|---|---|
| Console JSON nativo (`AddJsonConsole`) | zero dipendenze, JSON strutturato | poco arricchimento, niente request-logging sintetico, filtri base | Scartata: sufficiente solo come minimo assoluto |
| **Serilog** (`Serilog.AspNetCore`, `Serilog.Formatting.Compact`) | ecosistema sink ampio, enricher, `LoggingLevelSwitch` a runtime, `UseSerilogRequestLogging`, destructuring/redazione | una dipendenza in più nell'host | **Scelta** |
| **OpenTelemetry Logs** (OTLP exporter) | log+trace+metriche unificati, correlazione automatica `TraceId`/`SpanId`, sampling nativo | ecosistema log meno maturo di Serilog da solo | **Affiancata** a Serilog, per il tracing (utile già ora per correlare col `traceId` API, indispensabile quando si estrarranno moduli in microservizi) |
| NLog / log4net | maturi | API meno idiomatica nel .NET moderno | Scartata |

Pacchetti host: `Serilog.AspNetCore`, `Serilog.Formatting.Compact`, `Serilog.Enrichers.Environment`,
`Serilog.Sinks.Console`; dev opzionale `Serilog.Sinks.Seq`. Per OTel: `OpenTelemetry.Extensions.Hosting`,
`OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`.

```csharp
// Boot/Program.cs
builder.Host.UseSerilog((ctx, sp, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)            // livelli da appsettings/env
    .Enrich.FromLogContext()
    .Enrich.WithProperty("service", "api")
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(new RenderedCompactJsonFormatter())); // JSON su stdout
```

---

## 2. Dove salvarli — decisione

**Scelta: l'app scrive sempre su `stdout`, mai su file.** Il filesystem del container Fargate è
effimero; un file di log lo riempie e non serve a nulla in un ambiente scale-out. Lo shipping lo fa
l'infrastruttura, non l'app.

| Ambiente | Sink | Dettaglio |
|---|---|---|
| Dev locale | Console human-readable + (opz.) container **Seq** | `docker run datalust/seq`: query strutturate senza toccare AWS |
| Staging/Prod | Console JSON → log driver **`awslogs`** (o **FireLens**/Fluent Bit) → **CloudWatch Logs** | un **log group per servizio** (`/summerbooking/api`, `/summerbooking/worker`, `/summerbooking/jobs`), stream per task |
| Archivio lungo | CloudWatch Logs → **subscription filter** → Firehose → **S3** (gzip/Parquet) | retention CloudWatch **90 gg** ✅ (fissata dal titolare, [80-retention.md §3.9](80-retention.md)), **poi S3** con lifecycle IA/Glacier — coerente con la stima CloudWatch (~$10-20/mese) di `infrastruttura-aws.md` §4 |
| Audit fiscale/contabile (scontrini Fiskaly, movimenti) | **tabella DB dedicata** ✅ (scelta esplicita del titolare: *"Ok in tabella"*, non uno stream esterno), **nel modulo proprietario** (`fiskaly_receipt_logs`, `payment_ledger_*`) | retention di anni allineata al fiscale, **non** mischiare con i log diagnostici |
| Azioni degli operatori (chi ha fatto cosa, pagina «Tutti i log») | **DB**, modulo **Audit** (`audit_trail_entries`, categoria `operator`) — [86-modulo-audit.md](86-modulo-audit.md) | **12 mesi** ✅ (2026-10-13, D-66) |
| Log di autenticazione/sicurezza applicativa (login, lock, reset, 2FA) | **DB** ✅, modulo **Audit** (categoria `security`) | **6 mesi** ✅ *(proposta: 6–12)*; OTP cancellato subito dopo consumo/scadenza |
| Log accessi amministratori di sistema | registro immutabile: modulo **Audit** (categoria `platform`), utente applicativo con solo `INSERT`/`SELECT` | **12 mesi** (decisione del 2026-10-07; il Provv. Garante 27/11/2008 chiede ≥ 6) |

**Scartato**: sink che scrive direttamente su CloudWatch via SDK AWS — accoppia l'app ad AWS, aggiunge
un punto di fallimento di rete sul percorso caldo, duplica quello che il log driver del container fa
già gratis.

> **Nota sulle ultime righe** — audit fiscale, azioni degli operatori, log di autenticazione e degli amministratori **non** sono log diagnostici:
> vivono in tabelle del DB con una retention propria (validata in
> [80-retention.md §3.9](80-retention.md)), quindi rientrano nei job di retention di
> [80-retention.md §6](80-retention.md) e nel seed di
> [`privacy_retention_policies`](../analisi-post/db/privacy/privacy_retention_policies.md), non nella
> lifecycle di CloudWatch. La regola "l'app scrive su stdout" resta valida per tutto il resto.

---

## 3. Come integrarli nel codice — decisioni

- **Structured logging sempre, mai interpolazione di stringa**:
  `_logger.LogInformation("Prenotazione {BookingId} confermata per {PropertyId}", id, propertyId);`
  (con `$"..."` si perdono i campi queryabili e si rischia di loggare PII senza accorgersene).
- **Correlazione con lo standard API**: `Activity.Current.TraceId` (W3C Trace Context) deve coincidere
  con il `traceId` nel Problem Details di `standard-risposta-api-dotnet.md` §3 — dall'errore
  restituito al client si risale al log in un click. In Serilog: `Enrich.WithSpan()`/enricher OTel.
- **Riga di riepilogo per richiesta**: `app.UseSerilogRequestLogging()` — una riga (metodo, path,
  status, durata) al posto delle righe rumorose di Kestrel. Non si scrive a mano nei controller.
- **Un solo punto per le eccezioni non gestite**: l'`IExceptionHandler` globale già previsto dallo
  standard risposte API è l'unico posto che logga stack completo + `traceId`, e restituisce
  `code = server.unexpected` senza dettagli interni.
- **Scope per unità di lavoro**:
  `using (_logger.BeginScope(new Dictionary<string,object>{ ["BookingId"]=id, ["PropertyId"]=pid }))`
  — tutti i log dentro l'operazione ereditano quei campi.
- **Categoria = namespace**: `ILogger<ConfirmBookingHandler>` → categoria
  `SummerBooking.Booking.Application.ConfirmBookingHandler`; permette filtri per modulo
  (`SummerBooking.Booking`) — è la base del §5.
- **EF Core in prod**: `Microsoft.EntityFrameworkCore.Database.Command` a **Warning** di default.
  Nel legacy la query intera (con parametri) veniva loggata in produzione ad ogni chiamata
  (`01-criticita-e-indicazioni-backend.md:338`) — da non ripetere. Si alza solo in diagnostica (§5, P3+).
- **Redazione PII/segreti**: destructuring policy Serilog o `Microsoft.Extensions.Compliance.Redaction`.
  Vietati nei log: password, token/JWT interi, PAN/CVV carte, credenziali SMTP, dati personali oltre
  l'ID. Il legacy li loggava (`01-criticita-e-indicazioni-backend.md:336`, `:516`) — rischio PCI SAQ D
  e GDPR (dati in `eu-south-1`). Un test di architettura può vietare `Console.WriteLine`/equivalenti e
  segnalare l'uso di tipi sensibili nei template dei messaggi.
- **Worker/job** (`20-jobs.md`): stesso setup, `service=worker|job`, log di inizio/fine con **conteggi**
  (`elaborati=120, falliti=3, durata=4.2s`).

---

## 4. Quantità di log — 5 profili (P1–P5)

Profili di **volume**, ortogonali alla severità (Info/Warning/Error restano i "tipi"; qui si decide
"quanto" si scrive). Un solo dial operativo: `LOG_PROFILE`.

| Profilo | Cosa viene scritto | Criterio | Volume tipico | Quando |
|---|---|---|---|---|
| **P1 – Minimo** | Solo errori non gestiti, fallimenti esterni dopo i retry, start/stop, degrado health check | "Solo ciò che **è rotto**" | decine di righe/giorno | Emergenza costi; periodo stabile; silenziare un modulo rumoroso |
| **P2 – Standard (default prod)** | P1 + 1 riga di riepilogo per richiesta HTTP + warning + **eventi di business compiuti** (prenotazione confermata, pagamento incassato, scontrino emesso, email inviata/fallita, job iniziato/finito con conteggi) | "Un **fatto di business** concluso o un problema — una riga ciascuno" | ~1–5 righe/richiesta | Esercizio normale di produzione |
| **P3 – Diagnostico** | P2 + punti di decisione ed esiti di ramo (perché un prezzo è uscito X, perché la disponibilità dice no, quale sconto è stato applicato, cache hit/miss, publish/handle event bus, dispatch outbox) | "Il **perché** di un risultato" + valori intermedi chiave (no dump) | ~10–30 righe/richiesta | Indagare un bug segnalato in prod senza full debug; attivo a tempo |
| **P4 – Debug** | P3 + entry/exit degli application service con parametri redatti, comandi EF (SQL senza parametri sensibili), metadati chiamate esterne (status, timing, correlation id — non i body), scope completo | "Ogni **passo tecnico** rilevante" | ~50–200 righe/richiesta | Staging; o prod in finestra breve con filtro stretto (un modulo/un `propertyId`/un `traceId`) |
| **P5 – Trace** | P4 + payload grezzi (body req/resp) con redazione, parametri EF completi, iterazioni di ciclo, elaborazione per-item nei batch | "**Tutto**, incluso il contenuto" | illimitato | Solo dev locale o ambiente di riproduzione isolato; mai prod-wide, sempre a tempo |

Mappa dei profili sui livelli `Microsoft.Extensions.Logging`:

| Profilo | `Default` | `SummerBooking.*` | `Microsoft.AspNetCore` | `EFCore.Database.Command` |
|---|---|---|---|---|
| P1 | Error | Error | Error | None |
| P2 | Warning | Information | Warning | Warning |
| P3 | Information | Information | Warning | Warning |
| P4 | Information | Debug | Information | Information |
| P5 | Debug | Trace | Information | Information |

---

## 5. Attivazione/disattivazione — decisioni

| Meccanismo | Come | Uso |
|---|---|---|
| **Env var, senza redeploy** | `LOG_PROFILE=P2` da SSM Parameter Store, tradotto allo startup nel set di filtri §4; override puntuale standard .NET `Logging__LogLevel__SummerBooking.Booking=Debug` | Cambio di profilo per rilascio/incidente pianificato — richiede restart del task (rolling, ~30s) |
| **Runtime, senza restart** | `LoggingLevelSwitch` di Serilog esposto via endpoint `POST /admin/log-level { scope, level, ttlMinutes }`, protetto `systemadmin`, **TTL obbligatorio con auto-revert** (max 60 min) | Alzare un modulo a Debug durante un'indagine live, senza rischiare di dimenticarlo acceso |
| **Filtro mirato** | `ILogEventFilter` custom: profilo alto solo se `propertyId`/`traceId` in whitelist | Dettaglio alto su un solo tenant/richiesta senza inondare il resto |
| **Sampling** | 100% errori ed eventi di business; 1–10% delle righe di riepilogo richiesta sotto carico (nativo OpenTelemetry; custom in Serilog) | Leva di costo quando il volume esplode |
| **Default per ambiente** | `dev = P4` (P5 on-demand), `staging = P3`, `prod = P2` | Switch sempre disponibile ovunque via i meccanismi sopra |

---

## 6. Quantità di log per modulo — decisione

**Scelta: profilo di default globale + override per modulo, con un floor minimo per i moduli critici
che non scende mai sotto il profilo globale.**

```jsonc
// appsettings.json
"LoggingProfiles": {
  "Default": "P2",
  "Modules": {
    "Payment":       "P3",   // soldi: mai sotto il "perché"
    "Fiskaly":       "P3",   // obbligo fiscale: tracciabilità alta
    "Identity":      "P3",   // sicurezza/auth: tentativi, lock, anomalie
    "Notification":  "P2",   // fallimenti sempre visibili (vedi nota sotto)
    "Booking":       "P2",   // alto traffico: P2 tiene i costi sotto controllo
    "Configuration": "P2"
  }
}
```

```csharp
static void ApplyLoggingProfiles(ILoggingBuilder logging, IConfiguration cfg)
{
    var section = cfg.GetSection("LoggingProfiles");
    var @default = section["Default"] ?? "P2";

    foreach (var module in KnownModules)   // "Booking", "Payment", "Fiskaly", ...
    {
        var profile = section[$"Modules:{module}"] ?? @default;
        var levels = LogProfiles.Resolve(profile);          // tabella P1..P5 → livelli (§4)
        logging.AddFilter($"SummerBooking.{module}", levels.AppLevel);
    }
}
```

**Perché Payment / Fiskaly / Identity con floor P3**: coerente con quanto già rilevato in
`01-criticita-e-indicazioni-backend.md` — errori sul percorso fiscale/revenue **loggati e ignorati**,
fallimenti WhatsApp/email **senza log né alert**. Per questi moduli il costo di un evento perso è molto
più alto del costo di storage del log: il floor non scende quando il profilo globale scende (es.
emergenza costi → `LOG_PROFILE=P1` ovunque, ma Payment/Fiskaly/Identity restano almeno a P2/P3) —
si applica non facendo scendere l'override modulo sotto una soglia hard-coded in `LogProfiles.Resolve`.

Override runtime granulare per modulo: stesso meccanismo del §5 (`Logging__LogLevel__SummerBooking.Payment=Debug`
o l'endpoint admin con `scope: "Payment"`), per alzare un solo modulo durante un picco di segnalazioni
senza toccare gli altri.

---

## 7. Dove mettere le categorie a livello di codice — decisione

Non è un dial ulteriore: ogni layer dell'architettura (§ `linee-guida-implementative-dotnet.md`)
produce naturalmente un sottoinsieme delle categorie P1–P5.

| Layer | Categorie di sua competenza | Perché lì e non altrove |
|---|---|---|
| **Domain** (entità, VO, domain service) | **Nessuna** — niente `ILogger` iniettato | Stessa regola di EF/ASP.NET (linee guida §3): il Domain resta puro/testabile. Il fatto di business non lo logga l'entità: lo **solleva come Domain Event** (`BookingConfirmed`); chi gestisce l'evento lo logga. |
| **Application** (use case handler) | **P2** (1 riga a chiusura dell'use case/sull'evento gestito) + **P3** (punti di decisione: perché uno sconto non si applica, perché la disponibilità dice no) | Unico layer che vede sia l'intento (comando) sia l'esito (risultato del dominio): sede naturale del "cosa" e del "perché". |
| **Infrastructure** (repository, client esterni, cache, EF) | **P4** (passo tecnico: comando SQL, chiamata HTTP esterna con status/timing, cache hit/miss) + **P1** (fallimento esterno dopo i retry, es. `onFinalFailure` Polly verso Stripe/Twilio) | È dove vive il "come" tecnico e dove i retry/timeout falliscono davvero; l'Application non deve sapere che sotto c'è un client HTTP che ha ritentato N volte. |
| **Endpoints/middleware host** | **P2** (riga di riepilogo richiesta — automatica, `UseSerilogRequestLogging`) + **P1** (eccezioni non gestite — un solo punto, l'`IExceptionHandler` globale) | Cross-cutting: non si ripete per ogni controller. Il controller resta senza log propri (chiama Application e basta). |
| **Nessun layer fisso (temporaneo/mirato)** | **P5** (payload completi, iterazioni) | Middleware dev-only per body req/resp, o `LogTrace` ad-hoc in Infrastructure durante un'indagine, rimossi dopo. Se restano permanenti, dietro guardia `if (_logger.IsEnabled(LogLevel.Trace))` per non pagare il costo di costruzione del messaggio quando disattivato. |

Esempi:
- `_logger.LogInformation("Prenotazione {BookingId} confermata", id)` → nell'**handler Application**,
  dopo `booking.Confirm()`; **non** dentro `Booking.Confirm()`.
- `_logger.LogWarning("Chiamata a Stripe fallita dopo {Attempts} tentativi", n)` → nell'**adapter
  Infrastructure** (`StripePaymentClient`); in Application al più diventa un'eccezione tipizzata che
  l'`IExceptionHandler` traduce in Problem Details.
- `_logger.LogDebug("Query disponibilità: {Sql}", sql)` → generato **da EF Core stesso** (categoria
  `Microsoft.EntityFrameworkCore.Database.Command`), non scritto a mano.

Mantiene valida la regola dei confini (linee guida §5): le dipendenze — `ILogger` incluso — puntano
verso l'interno solo fino all'Application; il Domain non la riceve mai, coerente con la testabilità
richiesta da [02-strategia-di-test.md](02-strategia-di-test.md).

---

## 8. Cosa non loggare mai

Password, token/JWT interi, PAN/CVV carte, credenziali SMTP, dati personali oltre l'ID minimo
necessario. Il codice legacy violava questa regola in più punti (`01-criticita-e-indicazioni-backend.md:336`,
`:337-339`, `:516`) — vincolo esplicito per il nuovo backend, applicato via destructuring
policy/redazione (§3) e, se possibile, un test di architettura dedicato.

---

## 9. Riepilogo delle decisioni

1. **Libreria**: `ILogger<T>` nel codice, provider **Serilog** nell'host, JSON su stdout; **OpenTelemetry**
   affiancato per il tracing/correlazione.
2. **Destinazione**: sempre `stdout` → **CloudWatch Logs** (log group per servizio) → archivio **S3**
   via subscription filter, retention CloudWatch **90 gg** (fissata dal titolare, §2). Audit fiscale e
   log di autenticazione **in tabella DB** con retention propria (6 mesi gli auth), non nel flusso
   diagnostico — rientrano nei job di [80-retention.md §6](80-retention.md).
3. **Integrazione**: structured logging con template, correlazione `traceId` = `Activity.TraceId`,
   riga di riepilogo richiesta automatica, un solo `IExceptionHandler` globale, EF Command a Warning
   in prod, redazione PII/segreti obbligatoria.
4. **Quantità**: 5 profili **P1–P5** (minimo → trace), non livelli di severità ma di volume, con
   criterio di scrittura e volume tipico per ciascuno.
5. **Attivazione**: env var (`LOG_PROFILE`, senza redeploy) + `LoggingLevelSwitch` runtime con TTL
   (senza restart) + filtro mirato per tenant/richiesta + sampling sotto carico.
6. **Per modulo**: profilo di default globale + override per modulo, con **floor P3** permanente su
   Payment/Fiskaly/Identity (moduli dove un evento perso costa più dello storage del log).
7. **Per layer**: Domain senza logging (usa Domain Event); P2/P3 in Application; P4/P1 in
   Infrastructure; P2/P1 cross-cutting in Endpoints/middleware; P5 solo temporaneo/dev.
8. **Costo**: a regime (P2 + floor P3 sui moduli critici) **~20–40 $/anno** di log diagnostici in
   produzione; P4/P5 sostenibili solo a tempo (§10).

---

## 10. Stima dei costi annui per profilo

> Stima del 2026-10-13. Prezzi di listino indicativi (eu-south-1), da confermare con l'AWS Pricing
> Calculator prima del budget.

### 10.1 Base

Volumi reali del 2026 (stagione praticamente conclusa) dal DB migrato, **aumentati del 50%**:

| Dato | 2026 attuale | +50% |
|---|---|---|
| Prenotazioni | 155.651 | **~233.000** |
| Addebiti di pagamento | 23.160 | ~35.000 |
| Invii di notifiche | 116.527 | ~175.000 |
| Messaggi in-app | 157.661 | ~236.000 |
| Scontrini (Fiskaly + stampante fiscale) | 5.649 | ~8.500 |

Ipotesi:

- **~50 richieste API per prenotazione** (gestionale + canale online) → **~11,7 M richieste/anno**.
  È l'ipotesi che pesa di più: se il gestionale fa polling sulla mappa può raddoppiare, e con essa
  tutti i costi sotto.
- Stagionalità: agosto vale ~31% dell'anno → picco di **~117.000 richieste/giorno**.
- Dimensione media di una riga JSON arricchita: ~0,5 KB (P1–P3), ~0,8 KB (P4, testo SQL), ~2 KB (P5,
  corpi delle richieste).
- Prezzi: ingestione CloudWatch Logs ~0,57 $/GB; 90 giorni in CloudWatch (§2); poi Firehose → S3
  Glacier compresso.

### 10.2 Costo per profilo (attivo tutto l'anno in produzione)

| Profilo | Righe/richiesta | Volume/anno | Ingestione | CloudWatch 90 gg + Firehose + S3 | **Totale/anno** |
|---|---|---|---|---|---|
| **P1** | decine al giorno | ~10 MB | ~0 $ | ~0 $ | **~0 $** |
| **P2** puro | ~2 | ~12 GB | ~7 $ | ~1 $ | **~8 $** |
| **P2 come configurato** (§6: Payment/Fiskaly/Identity a P3, ~20% del traffico) | — | ~30 GB | ~17 $ | ~2 $ | **~20 $** |
| **P3** | ~15 | ~90 GB | ~50 $ | ~5 $ | **~55 $** |
| **P4** | ~100 | ~940 GB | ~535 $ | ~50 $ | **~585 $** |
| **P5** | ~300 | ~7 TB | ~4.000 $ | ~385 $ | **~4.400 $** |

### 10.3 Lettura

- Il **free tier** di CloudWatch Logs (5 GB/mese) assorbe quasi tutto P2 fuori stagione: in pratica si
  paga solo tra giugno e settembre.
- **P4/P5 non sono profili da tenere accesi** (§5, a tempo con TTL). In alta stagione: P4 su tutto il
  sistema ~5 $/giorno, un'ora su un solo modulo costa centesimi; P5 ~40 $/giorno su tutto il sistema,
  per questo resta solo in dev.
- **Staging (P3) e dev**: traffico trascurabile, pochi $/anno.
- **Logs Insights** (0,005 $/GB letto): con P2/P3 ogni query costa frazioni di centesimo.
- **Esclusi** dalla stima i log in tabella DB (audit, Fiskaly, sicurezza — §2): sono storage RDS, non
  CloudWatch.
- A regime: **~20–40 $/anno** di log diagnostici (forbice per il possibile raddoppio delle richieste),
  ben dentro la voce CloudWatch di [infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) §4, che
  comprende anche allarmi e metriche.
