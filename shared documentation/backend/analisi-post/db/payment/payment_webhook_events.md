# Tabella `payment_webhook_events`

> **Dominio**: C3 — `payment` · **sottoarea**: — · **schema**: `payment` · **nome flat**:
> `payment_webhook_events` · **forma canonica**: `payment.webhook_events`.
>
> Tabella **nuova** (nessuna legacy). Nasce da
> [`docs/05-ottimizzazioni-db.md` §6](../../../progettazione/05-ottimizzazioni-db.md): l'idempotenza dei webhook
> di pagamento, che nel legacy era un `SELECT` seguito da un `INSERT` applicativo, diventa un vincolo
> di schema. **Applicata nel backend il 2026-09-21**, migration `AddPaymentWebhookEvents`.

---

## Descrizione

Registro di **tutti** gli eventi webhook ricevuti dai gateway di pagamento, con la chiave di idempotenza
`(provider, external_event_id)` come `UNIQUE`.

**Il punto non è archiviare i webhook, è chiudere una finestra di corsa.** Nel legacy
(`back-pre/app/controllers/stripeWebhookHandler.js`, verificato in doc §6.1) l'unicità dell'evento era
affidata a una sequenza applicativa: leggo se l'ho già visto, se no lo elaboro. Fra la lettura e la
scrittura ci sta comodamente un secondo webhook — e Stripe **ritenta** gli eventi, quindi non è un caso
di scuola. Con il vincolo a schema l'inserimento diventa una sola istruzione:

```sql
INSERT INTO payment.webhook_events (provider, external_event_id, event_type, payload, processing_status)
VALUES ('stripe', 'evt_...', 'payment_intent.succeeded', '{...}', 'received')
ON CONFLICT (provider, external_event_id) DO NOTHING;
```

Chi ottiene la riga elabora, chi trova zero righe modificate sa che è un duplicato e si ferma. La corsa
la arbitra PostgreSQL, non l'applicazione.

**Registra anche ciò che non gestisce.** Il legacy riconosceva due soli tipi di evento
(`payment_intent.succeeded`, `checkout.session.completed`); ogni altro evento riceveva un `200` e
spariva. Qui viene comunque scritto con `processing_status = 'ignored'`: *"non lo gestiamo"* e *"lo
abbiamo perso"* diventano due stati distinguibili, e il giorno in cui servirà gestire rimborsi o dispute
lo storico è già lì.

**Tabella generica, non una per tipo di evento.** `provider` + `event_type` testuale + `payload jsonb`
coprono qualunque evento futuro senza cambiare schema.

## Verifica di esclusività

`processing_status` è un enum a valore singolo, nessun gruppo di booleani paralleli. `processed_at` è
valorizzato quando lo stato esce da `received`; `failure_reason` solo su `failed` — coppie stato/dettaglio
coerenti per convenzione applicativa, non vincolate a schema (un `CHECK` bloccherebbe la scrittura del
motivo prima del cambio di stato, che è l'ordine naturale in un blocco `catch`).

## Snapshot

`payload` **è** uno snapshot: il corpo dell'evento come ricevuto dal gateway, congelato. Serve a poter
rielaborare un evento fallito senza richiederlo di nuovo a Stripe (che lo conserva per un tempo limitato)
e a ricostruire cosa è successo in caso di contestazione.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_payment_webhook_events` | `id` | PK | — |
| `uq_..._external` | `(provider, external_event_id)` | UNIQUE | **La ragione d'essere della tabella**: senza, l'idempotenza torna a essere un `SELECT` + `INSERT` applicativo |
| `ix_..._pending` | `(received_at) WHERE processing_status IN ('received','failed')` | parziale | coda di lavoro: da elaborare o da riprovare, in ordine d'arrivo. Parziale perché gli eventi già chiusi sono la quasi totalità delle righe |
| `ix_..._property` | `(property_id, received_at) WHERE property_id IS NOT NULL` | parziale | storico eventi per stabilimento, per il supporto |
| `brin_..._created_at` | `(created_at)` | BRIN | tabella in solo append: scansione del job di retention a costo quasi nullo |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Nullabile**: No.

### provider
- **Legacy**: — (nuovo)
- **Descrizione**: gateway che ha inviato l'evento.
- **Tipo**: `enum payment.webhook_provider` — valori: `stripe`.
- **Nullabile**: No.
- **Note**: un solo valore oggi, ma è comunque un enum perché fa parte della **chiave di idempotenza**:
  un secondo gateway non deve poter collidere con gli id evento del primo.

### external_event_id
- **Legacy**: — (nuovo)
- **Descrizione**: id dell'evento presso il gateway (per Stripe: `evt_...`).
- **Tipo**: `varchar(255)`.
- **Nullabile**: No.
- **Relazioni**: seconda metà della chiave di idempotenza.

### event_type
- **Legacy**: — (nuovo)
- **Descrizione**: tipo di evento come dichiarato dal gateway.
- **Tipo**: `varchar(120)`.
- **Nullabile**: No.
- **Note**: testo libero e **non** enum, deliberatamente: il vocabolario lo decide Stripe e si allunga
  senza un nostro deploy. Un enum costringerebbe a una migration per registrare un evento che non
  gestiamo nemmeno.

### property_id
- **Legacy**: — (nuovo)
- **Descrizione**: `public_id` di `catalog.properties`, se l'evento è attribuibile a uno stabilimento.
- **Tipo**: `uuid`.
- **Nullabile**: Sì — l'attribuzione si ricava leggendo il payload e può non riuscire (eventi di
  piattaforma, account Connect non ancora associato).
- **Relazioni**: riferimento testuale, nessuna FK cross-schema.

### payload
- **Legacy**: — (nuovo)
- **Descrizione**: corpo dell'evento come ricevuto.
- **Tipo**: `jsonb`.
- **Nullabile**: No.

### processing_status
- **Legacy**: — (nuovo)
- **Descrizione**: esito dell'elaborazione.
- **Tipo**: `enum payment.webhook_processing_status` — valori: `received`, `processed`, `ignored`,
  `failed`.
- **Nullabile**: No.

### attempt_count
- **Legacy**: — (nuovo)
- **Descrizione**: numero di tentativi di elaborazione.
- **Tipo**: `smallint`. **Default**: `0`. **Nullabile**: No.
- **Note**: distingue un fallimento isolato da uno ricorrente senza dover leggere i log.

### failure_reason
- **Legacy**: — (nuovo)
- **Descrizione**: motivo dell'ultimo fallimento.
- **Tipo**: `text`. **Nullabile**: Sì (`NULL` quando lo stato non è `failed`).

### received_at / processed_at
- **Legacy**: — (nuovo)
- **Descrizione**: istante di ricezione del webhook / istante di elaborazione.
- **Tipo**: `timestamptz`. **Default**: `now()` / nessuno.
- **Nullabile**: No / Sì (`NULL` finché è `received`).
- **Relazioni**: `received_at` è la decorrenza della retention.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Tipo**: `timestamptz` ciascuno. **Default**: `now()`. **Nullabile**: No.

## Valutazione JSON

`payload` è `jsonb` a ragione: è un documento **di terzi**, di forma non controllata da noi e variabile
per tipo di evento. De-jsonizzarlo significherebbe inseguire lo schema di Stripe a ogni loro rilascio.
Le poche informazioni su cui si interroga (`provider`, `event_type`, `property_id`) sono estratte in
colonne vere e indicizzate; il resto resta documento.

## Suggerimenti per le prestazioni

- Volume ridotto: il legacy gestisce due tipi di evento, e anche registrando tutto il traffico Stripe si
  resta nell'ordine delle migliaia di righe l'anno.
- `ix_..._pending` è parziale: la coda di lavoro resta piccola anche quando la tabella cresce.
- Nessun partizionamento (stesso ragionamento delle altre tabelle `payment`, doc §2.1): il BRIN basta.

## Implementazione .NET

Entità `WebhookEvent`, modulo `Module.Payment`, schema `payment`. Non è un aggregate root di dominio ma
un **registro di infrastruttura**: nessun `ISoftDeletable` (una riga di webhook non si "cancella
logicamente", scade e sparisce con la retention), nessun evento di dominio sollevato da qui — è chi
elabora l'evento a produrre effetti sul dominio.

### Enum propri

```csharp
public enum WebhookProvider { Stripe = 0 }
public enum WebhookProcessingStatus { Received = 0, Processed = 1, Ignored = 2, Failed = 3 }
```

```csharp
// DbContext.OnModelCreating
modelBuilder.HasPostgresEnum<WebhookProvider>(Schema, "webhook_provider");
modelBuilder.HasPostgresEnum<WebhookProcessingStatus>(Schema, "webhook_processing_status");

// ModulePersistenceRegistration.cs *e* DesignTimeDbContextFactory.cs — righe identiche
dataSourceBuilder.MapEnum<WebhookProvider>("payment.webhook_provider");
dataSourceBuilder.MapEnum<WebhookProcessingStatus>("payment.webhook_processing_status");
```

### Nota di implementazione

L'inserimento **non** passa da `SaveChanges` su un'entità tracciata: EF Core non genera `ON CONFLICT`.
Va eseguito come SQL esplicito (`ExecuteSqlAsync` con `ON CONFLICT DO NOTHING`), leggendo il numero di
righe interessate per decidere se procedere o fermarsi. Usare il tracciamento EF qui reintrodurrebbe
esattamente la finestra di corsa che la tabella esiste per chiudere.

### Note di migrazione

Tabella nuova, nessun dato legacy da portare: gli eventi storici di Stripe non sono ricostruibili né
servono. Parte vuota e si popola dal primo webhook ricevuto dal nuovo backend.
