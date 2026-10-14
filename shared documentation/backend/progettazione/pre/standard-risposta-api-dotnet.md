# Standard di risposta delle API (.NET 10)

> Formato uniforme delle risposte HTTP per la riscrittura in **.NET 10 (Web API)**.
> Principio guida: **il backend restituisce codici, il frontend li traduce** → backend agnostico rispetto
> alla lingua. Complemento a [linee-guida-implementative-dotnet.md](linee-guida-implementative-dotnet.md)
> e [autenticazione-autorizzazione-dotnet.md](autenticazione-autorizzazione-dotnet.md).

---

## 1. Principio

- Sul **successo** si restituiscono i **dati** con lo **status HTTP 2xx** appropriato.
- Sull'**errore** si usa il formato standard **Problem Details (RFC 9457)**, esteso con un **`code`
  machine-readable** che il **frontend** converte nella stringa localizzata.
- Gli status HTTP sono **semantici** (non "tutto 200"): il frontend può ramificare anche sulla classe di status.
- I **messaggi testuali** (`title`/`detail`) sono per **sviluppatori e log**, **non** per l'utente finale.

---

## 2. Formato successo (2xx)

Dati diretti:
```json
{ "id": "bk_123", "total": 280.00, "currency": "EUR", "status": "confirmed" }
```
Se il frontend preferisce una forma unica anche sul successo, è ammesso un envelope minimale — ma **non**
"tutto 200":
```json
{ "data": { "id": "bk_123", "total": 280.00 }, "meta": { "page": 1, "total": 42 } }
```

---

## 3. Formato errore (4xx/5xx) — Problem Details esteso

```jsonc
{
  "type": "https://errors.summerbooking/booking/umbrella-overlap",
  "status": 409,                              // status HTTP semantico
  "code": "booking.umbrella_overlap",          // ← CHIAVE che il frontend traduce
  "args": { "umbrellaId": "42", "date": "2026-08-01" },  // per l'interpolazione nel messaggio
  "traceId": "00-abc123...",                   // correlazione con i log
  "title": "Umbrella already booked",          // solo dev/log, NON mostrato all'utente
  "errors": [                                  // validazione multi-campo
    { "field": "endDate", "code": "validation.end_before_start", "args": {} }
  ]
}
```

Campi:
| Campo | Ruolo |
|---|---|
| `status` | Status HTTP semantico |
| `code` | **Chiave stabile** che il frontend localizza (il campo più importante) |
| `args` | Valori per interpolare il messaggio (es. `{umbrellaId}`) |
| `errors[]` | Errori di validazione per campo (`field` + `code` + `args`) |
| `traceId` | Correlazione con i log per il debug |
| `type`/`title` | Riferimento/etichetta per dev e log — **non** per l'utente |

---

## 4. Convenzioni sui codici (`code`)

- **Namespacati e stabili**: `booking.umbrella_overlap`, `validation.required`, `auth.invalid_credentials`,
  `payment.card_declined`.
- Una volta rilasciato, **un `code` non si cambia**: il frontend ci dipende.
- Prevedere un fallback lato frontend ("errore sconosciuto") per `code` non ancora tradotti.

---

## 5. Mappatura degli status HTTP

| Status | Quando |
|---|---|
| 200 / 201 / 204 | Successo (ok / creato / nessun contenuto) |
| 400 | Input non valido (validazione) |
| 401 | Non autenticato (token mancante/invalido) |
| 403 | Autenticato ma non autorizzato |
| 404 | Risorsa non trovata |
| 409 | Conflitto (es. overbooking) |
| 422 | Regola di business violata |
| 500 | Errore server non gestito |

---

## 6. Implementazione .NET (essenziale)

```csharp
builder.Services.AddProblemDetails();   // supporto nativo

// eccezioni di dominio tipizzate che portano il codice
public class DomainException : Exception {
    public string Code { get; }
    public IReadOnlyDictionary<string, object>? Args { get; }
    public int HttpStatus { get; }
    public DomainException(string code, int httpStatus, IReadOnlyDictionary<string, object>? args = null)
        : base(code) { Code = code; HttpStatus = httpStatus; Args = args; }
}
```
Un **`IExceptionHandler` globale** mappa in Problem Details:
- `DomainException` → `HttpStatus` + `code` + `args`;
- `ValidationException` (FluentValidation) → **400** con `errors[]`;
- eccezione non gestita → **500** `code = "server.unexpected"` (senza esporre dettagli interni).

Così c'è **un solo punto** che produce il formato; i moduli si limitano a **lanciare eccezioni tipizzate
con un codice**.

---

## 7. Dove stanno le stringhe (e Redis)

- I **messaggi** li possiede il **frontend** (i suoi bundle i18n): il backend manda solo `code` + `args`.
- Se in *alcuni* casi è il **backend** a dover produrre testo (email, PDF, consumer non-SPA), le stringhe
  vengono dai **`.resx`** — già **in memoria** nell'assembly, veloci.
- **Redis NON serve per i messaggi d'errore.** La cache Redis è riservata al **contenuto di dominio su DB**
  multilingua (nomi servizi, descrizioni del catalogo: read-heavy), non a un insieme finito di errori.

---

## 8. Lingua: dove prenderla (non nel token come autoritativa)

La lingua è una **preferenza volatile**, non un dato di identità: metterla nel token è possibile ma non
ideale (resta lo *snapshot* al login, fino al refresh). E nel modello "il frontend traduce" al backend
serve pochissimo.

| Caso | Fonte della lingua |
|---|---|
| Risposte API con errori | non serve: **traduce il frontend** dal `code` |
| Richieste sincrone (se mai servisse lato backend) | header **`Accept-Language`** (cambia liberamente) |
| Contenuti asincroni senza richiesta (email, PDF) | **preferenza persistita** nel profilo utente/cliente (DB) |

Al più, un `locale` nel token come **default di comodo non autoritativo**, che `Accept-Language`/profilo
possono sovrascrivere.

---

## 9. Catalogo condiviso dei codici

Mantenere un **registro unico dei `code`** (lista documentata o JSON di contratto) come *single source of
truth* tra backend e frontend: evita il drift (un `code` nuovo che il frontend non sa tradurre) e permette
un fallback ordinato.

---

## 10. Riepilogo regole

1. **Successo** = dati + status 2xx; **errore** = **Problem Details (RFC 9457)** esteso con `code`.
2. Il **`code`** (stabile, namespacato) è la chiave che il **frontend** traduce; `args` per l'interpolazione.
3. **Status HTTP semantici**, non "tutto 200".
4. `title`/`detail` → **dev/log**, mai per l'utente; sempre un **`traceId`**.
5. **Un `IExceptionHandler` globale** produce il formato; i moduli lanciano **eccezioni tipizzate con codice**.
6. **Redis per gli errori: no** — messaggi nel frontend (o `.resx` in memoria lato backend).
7. **Lingua**: `Accept-Language` per le richieste, **profilo DB** per email/PDF; **non** autoritativa nel token.
8. **Catalogo condiviso dei codici** per evitare il drift backend↔frontend.
