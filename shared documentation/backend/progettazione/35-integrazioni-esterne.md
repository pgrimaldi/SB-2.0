# Integrazioni con sistemi esterni

> Come si integrano Stripe, Fiskaly, Twilio/WhatsApp, SendGrid, S3 e le altre API di terzi nel nuovo
> backend **.NET 10**: dove vive il codice di integrazione, come si rende resiliente e come si evitano
> effetti duplicati (doppio addebito, doppia email) sui fallimenti di rete.
>
> Complemento a [pre/linee-guida-implementative-dotnet.md §3, §6](pre/linee-guida-implementative-dotnet.md)
> (confini tra layer, eventi + outbox), [pre/standard-risposta-api-dotnet.md](pre/standard-risposta-api-dotnet.md)
> (`IExceptionHandler` globale, Problem Details), [pre/autenticazione-autorizzazione-dotnet.md §8](pre/autenticazione-autorizzazione-dotnet.md)
> (webhook autenticati via firma, non JWT), [17-secrets.md](17-secrets.md) (dove stanno le credenziali
> dei provider), [20-jobs.md](20-jobs.md) (outbox), [31-observability.md §6](31-observability.md)
> (metriche/alert per dipendenza esterna), [02-strategia-di-test.md §4](02-strategia-di-test.md)
> (WireMock.Net sugli `HttpClient`, provider mai contattati nei test) e
> [01-criticita-e-indicazioni-backend.md §6](01-criticita-e-indicazioni-backend.md) (il webhook
> "payment link" che ritenta all'infinito nel legacy).
>
> **Documento vivo**: aggiungere una riga nella tabella §5 per ogni nuovo provider.

---

## 0. Principio guida

Per ogni sistema esterno, **due parti nello stesso modulo che possiede l'integrazione**: un contratto
astratto in `Application`, espresso in termini di **dominio**; un'implementazione concreta in
`Infrastructure/ExternalApi`, che è l'**unico** punto a conoscere il vendor. Il resto (resilienza,
idempotenza, mappatura errori, webhook) sta tutto nell'implementazione concreta.

---

## 1. Il layer dedicato: contratto astratto + implementazione concreta

### 1.1 Contratto astratto — `Application/Abstractions/`

Espresso in termini di dominio, **non** del vendor:

```csharp
public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(Money amount, PaymentMethodToken token, string idempotencyKey, CancellationToken ct);
    Task<PaymentResult> RefundAsync(PaymentReference reference, Money amount, CancellationToken ct);
}
```

Non `IStripeClient.CreatePaymentIntent(...)`: se domani si aggiunge un secondo processore di pagamento
(o si cambia provider per un mercato diverso), l'Application **non cambia una riga**.

### 1.2 Implementazione concreta — `Infrastructure/ExternalApi/`

L'unica che conosce il vendor. Fa, e solo qui:

- traduce la chiamata di dominio nell'SDK/HTTP del vendor specifico;
- applica la **policy di resilienza** (§2) e gestisce **autenticazione / secret** ([17-secrets.md](17-secrets.md));
- **mappa gli errori del vendor in eccezioni di dominio tipizzate** (`PaymentDeclinedException` con il
  suo `code`, non un'eccezione generica di Stripe che risalirebbe fino all'Application violando il
  confine — [pre/standard-risposta-api-dotnet.md §6](pre/standard-risposta-api-dotnet.md));
- è l'unico punto che conosce il **formato di webhook / firma** di quel vendor (verifica firma
  Stripe/Meta, idempotenza sull'`event.id`).

### 1.3 Perché conta, concretamente

- **Testabilità**: l'Application si testa con un fake dell'interfaccia ([02-strategia-di-test.md §3](02-strategia-di-test.md)),
  senza contattare Stripe/Fiskaly nemmeno negli integration test (§4 della stessa, WireMock.Net
  sull'`HttpClient`).
- **Sostituibilità**: cambiare provider tocca solo l'`Infrastructure` di quel modulo, mai il dominio o
  gli endpoint.
- **Confine imposto dagli `ArchitectureTests`**: la regola "Domain e Application non dipendono da
  Infrastructure" copre esattamente questo — un `using Stripe.net` che risalisse in Application fa
  fallire la **build**, non solo la code review.
- **Un solo punto da aggiornare** quando il vendor cambia versione di API — è già successo nel legacy,
  dove lo stesso pattern CRUD Stripe era copiato in più controller con bug divergenti
  ([01-criticita-e-indicazioni-backend.md §5](01-criticita-e-indicazioni-backend.md)): qui non può
  succedere perché esiste **un solo client per vendor**.

---

## 2. Resilienza — come, dove, quando

### 2.1 Come

`Microsoft.Extensions.Http.Resilience` (basato su **Polly v8**) su ogni `HttpClient` tipizzato verso un
servizio esterno: `.AddStandardResilienceHandler()` in fase di registrazione DI, che include già retry
con **backoff esponenziale + jitter**, **circuit breaker**, timeout per singolo tentativo e timeout
totale.

### 2.2 Dove

Esclusivamente nell'**`Infrastructure/ExternalApi`** del modulo che possiede l'integrazione — mai in
Application (che non deve sapere se sotto c'è HTTP, gRPC o altro). Stesso confine già stabilito per
`WeatherApiClient` in `Module.ZTest`. Registrazione nel `Module.cs`:

```csharp
services.AddHttpClient<IPaymentGateway, StripePaymentGateway>()
    .AddStandardResilienceHandler(options => { /* soglie specifiche per Stripe */ });
```

### 2.3 Quando (distinzione cruciale)

| Tipo di operazione | Trattamento |
|---|---|
| **Idempotente per natura** (GET, verifica stato pagamento) | Retry automatico sicuro |
| **Non idempotente** (creazione addebito, invio email) | **Mai** retry naïve: usare l'`Idempotency-Key` che Stripe (e i provider seri) espone, così un retry di rete non genera un doppio addebito — vedi §3 |
| **Non critica per il flusso principale** (es. previsioni meteo) | Fallback degradato: la richiesta utente prosegue comunque |
| **Critica** (charge Stripe, emissione scontrino Fiskaly) | **Nessun fallback silenzioso**: se fallisce dopo i retry, l'operazione fallisce visibilmente; l'effetto collaterale (notifica, email) va gestito con **outbox** (§3), non tentato in linea e perso |

### 2.4 Bulkhead

Limite di concorrenza **per dipendenza esterna** (`ConcurrencyLimiter` di Polly): un provider lento non
esaurisce il pool di connessioni/thread condiviso da tutta l'applicazione.

### 2.5 Osservabilità della resilienza

Ogni client esterno espone latenza, error rate e **stato del circuit breaker** come metriche; un
breaker che passa in `Open` su un provider critico è un alert `page`
([31-observability.md §6.2](31-observability.md)).

---

## 3. Idempotenza ed effetti collaterali

- **Chiamate in uscita non idempotenti**: passare sempre un `idempotencyKey` stabile (parte del
  contratto, vedi `IPaymentGateway.ChargeAsync`). La chiave è deterministica rispetto all'operazione di
  dominio (es. `payment:<bookingId>:<attemptNo>`), non un GUID casuale a ogni retry.
- **Effetti collaterali dopo un'operazione critica** (email di conferma, notifica, preparazione line
  item Fiskaly): **outbox pattern** — scritti nella **stessa transazione** del dominio, elaborati in
  modo affidabile da un job ([20-jobs.md](20-jobs.md)). Mai "chiama Stripe e poi manda l'email in
  linea": se la seconda fallisce, l'effetto è perso silenziosamente.
- **Webhook in ingresso**: idempotenza sull'`event.id` del provider (tabella di eventi già processati);
  un redelivery dello stesso evento non applica due volte l'effetto. È il fix diretto del bug legacy
  del webhook "payment link" che ritentava all'infinito
  ([01-criticita-e-indicazioni-backend.md §6](01-criticita-e-indicazioni-backend.md)).

---

## 4. Webhook

- **Autenticazione via firma del provider**, non JWT ([pre/autenticazione-autorizzazione-dotnet.md §8](pre/autenticazione-autorizzazione-dotnet.md)):
  verifica HMAC Stripe (`Stripe-Signature`), firma Meta/WhatsApp, ecc. — nell'implementazione concreta
  del modulo, con il secret da [17-secrets.md](17-secrets.md).
- **Rispondere subito 2xx** dopo aver verificato la firma e persistito l'evento grezzo (`jsonb`
  passthrough, [03-gestione-dati.md](03-gestione-dati.md)); l'elaborazione avviene **dopo**, dall'outbox
  / da un handler, non nel ciclo di request del webhook.
- **Idempotenza** sull'`event.id` (§3).
- **Replay tollerato**: il provider può reinviare; l'handler deve essere sicuro da rieseguire.

---

## 5. Provider in uso — mappatura

| Provider | Modulo proprietario | Contratto astratto (esempio) | Criticità | Note di resilienza |
|---|---|---|---|---|
| **Stripe** (pagamenti, Connect, payment link, refund) | Payment | `IPaymentGateway`, `IPaymentLinkService` | Critica | `Idempotency-Key` obbligatorio su charge/refund; webhook con verifica firma + dedup `event.id` |
| **Fiskaly** (scontrino elettronico, FiscOnline) | Fiskaly | `IReceiptIssuer` | Critica | Nessun fallback silenzioso; fallimento → alert `page`; credenziali cifrate at-rest ([11-modulo-privacy.md §4](11-modulo-privacy.md)) |
| **Twilio / Meta WhatsApp** (messaggistica, centralino) | Notification / Switchboard | `IMessageSender` | Media | Invii via outbox; webhook firma Meta; il runtime SIP/real-time è isolato ([../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md)) |
| **SendGrid / SMTP** (email transazionali e marketing) | Notification | `IEmailSender` | Media | Invii via outbox; retry su 5xx; bounce/deliverability come metrica |
| **Telnyx** (numeri, traffico centralino) | Switchboard | `ITelephonyProvider` | Media | Payload grezzo `jsonb`; retention del traffico da chiarire ([11-modulo-privacy.md §3](11-modulo-privacy.md)) |
| **AWS S3** (media, PDF fiscali) | modulo che possiede l'asset | `IObjectStorage` | Bassa/Media | SDK AWS con retry nativo; URL scomposto `origin`+`path` ([03-gestione-dati.md §1](03-gestione-dati.md)) |
| **API meteo** (eventuale) | Booking / ZTest | `IWeatherApi` | Bassa | Fallback degradato, la richiesta utente prosegue |
| **TeamSystem POS** (`ofsale`) | Cassa | `IPosGateway` | Media | Da modellare come gli altri: contratto in Application, SDK solo in Infrastructure |

---

## 6. Errori non gestiti

Un solo **`IExceptionHandler` globale** nell'host ([pre/standard-risposta-api-dotnet.md](pre/standard-risposta-api-dotnet.md))
traduce le eccezioni di dominio tipizzate in Problem Details. I moduli **lanciano eccezioni** con un
`code`, non costruiscono mai direttamente una risposta d'errore — un solo punto che produce il formato,
mai ripetuto per ogni controller come nel legacy.

---

## 7. Checklist per una nuova integrazione

- [ ] Contratto in `Application/Abstractions/` in termini di dominio, DTO propri (nessun tipo del
      vendor che trapela).
- [ ] Implementazione in `Infrastructure/ExternalApi/`; `using <sdk-vendor>` **solo** in questo
      progetto (verificato dagli `ArchitectureTests`).
- [ ] `AddHttpClient<IContract, Impl>().AddStandardResilienceHandler(...)` con soglie motivate.
- [ ] `ConcurrencyLimiter` (bulkhead) dimensionato.
- [ ] Mappatura degli errori del vendor → eccezioni di dominio con `code`.
- [ ] Operazioni non idempotenti: `idempotencyKey` deterministico nel contratto.
- [ ] Effetti collaterali post-operazione via **outbox**, non in linea.
- [ ] Webhook (se presente): verifica firma, persistenza evento grezzo, 2xx immediato, dedup
      `event.id`, elaborazione asincrona.
- [ ] Secret del provider in [17-secrets.md](17-secrets.md) (inventario aggiornato) e in Secrets
      Manager / `appsettings.Local.json`.
- [ ] Metriche + alert per la dipendenza ([31-observability.md](31-observability.md)).
- [ ] Test: fake del contratto negli unit test; WireMock.Net negli integration test; provider reale
      mai contattato.
- [ ] Riga aggiunta alla tabella §5.
