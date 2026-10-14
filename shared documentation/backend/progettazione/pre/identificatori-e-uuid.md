# Identificatori e chiavi primarie — `bigint` PK + `public_id` UUIDv7

> Linea guida per la scelta degli identificatori nel backend **.NET 10 + EF Core + PostgreSQL**.
> Complemento a [infrastruttura-aws.md](../../../../../../infra/infrastruttura-aws.md) (§3.2.1),
> [linee-guida-implementative-dotnet.md](linee-guida-implementative-dotnet.md) e
> [../03-gestione-dati.md §3.1ter](../03-gestione-dati.md).
>
> **Revisione (decisione aggiornata).** La prima stesura di questo documento indicava *"UUIDv7 come PK
> ovunque"*. La decisione corrente — presa nel dominio **C5 (identity)** e adottata come default di
> progetto — è **`bigint identity` come PK + `public_id uuid` (UUIDv7) sulle sole entità referenziate
> fuori dal proprio schema di modulo**. Motivo del cambio: la migrazione dal MySQL legacy (id tutti
> interi) e la strategia di **dual-run / riconciliazione** col vecchio DB sono molto più semplici
> mantenendo l'`id` intero **1:1** col legacy, senza propagare una mappa `legacy_id → uuid` su ogni FK.
> Le sezioni §3–§5 (le PK non sono segreti, esposizione, criticità del progetto) **restano valide**:
> si applicano ora al `public_id`.

---

## 1. Decisione

- **Chiave primaria**: `id bigint GENERATED ALWAYS AS IDENTITY` su **ogni** tabella. Interna allo
  schema del modulo: usata per join, FK e indici; **non** esce dal modulo, **non** compare nelle API.
- **Id pubblico**: colonna `public_id uuid NOT NULL UNIQUE`, valore **UUIDv7** generato in factory
  (`Guid.CreateVersion7()`, §2), **solo** sulle entità:
  - esposte via **URL/REST** o negli endpoint **GDPR** ([../11-modulo-privacy.md §5](../11-modulo-privacy.md)); e/o
  - il cui id è **copiato in eventi/proiezioni da un altro modulo** (contratto cross-modulo — mai FK
    cross-schema, coerente con [autenticazione-autorizzazione-dotnet.md §9](autenticazione-autorizzazione-dotnet.md)).

  Le tabelle referenziate **solo internamente** (join, lookup, dettaglio, log, config) restano
  **solo `bigint`**.
- **Riferimenti cross-modulo (bidirezionale)**: si copia **sempre il `public_id` (uuid)** dell'entità
  referenziata, mai il suo `bigint`. Vale nei due sensi: quando un modulo punta a un'entità di **un
  altro** modulo (es. una tabella `identity` che referenzia `property`/`bathhouse` di `catalog` →
  colonna `property_id` / `bathhouse_id` di tipo **`uuid`**), e quando altri moduli puntano alle
  entità di questo. Così l'estrazione futura di un modulo in microservizio
  ([architettura-evolutiva-microservizi.md](architettura-evolutiva-microservizi.md)) non tocca lo
  schema: il riferimento è già globale, stabile, non enumerabile. Le colonne FK **interne** allo
  schema del modulo restano `bigint` (puntano alla PK `id`).
- **Al confine del modulo**: l'input esterno è `public_id` → risolto in `id` interno **una volta**,
  prima della persistenza. Entrambi valorizzati alla `INSERT`, entrambi immutabili.
- **Discriminatore tenant**: la colonna `property_id` sulle tabelle multi-tenant è **`uuid`** — è il
  `public_id` della `property` copiato da `catalog` (vedi [../34-multitenant.md](../34-multitenant.md)),
  non il suo `bigint` interno.

### Perché non le alternative

| Opzione | Problema | Verdetto |
|---|---|---|
| `int/bigint` sequenziale **esposto** grezzo | **Enumerabile**: `/customers/1001` → `/1002`. Favorisce IDOR e leak dei volumi. | ❌ non esporre il `bigint`: esporre `public_id` |
| **UUIDv7 come PK** ovunque | Migrazione dal legacy: mappa `legacy_id → uuid` da propagare su **ogni** FK; dual-run/riconciliazione col vecchio DB più fragili; PK/FK/indici a 16 byte anche su tabelle mai esposte. | ⚠️ superata da `bigint` PK + `public_id` |
| **UUIDv4** come `public_id` | Nessuna località: se mai usato come chiave d'ordinamento/indice, page split e bloat. | ⚠️ solo se l'istante di creazione è dato da proteggere (§4) |
| **UUIDv7 come `public_id`** | Rivela l'istante/ordine di creazione (ms) — sensibilità bassa per questo progetto (§5). | ✅ **standard** per l'id pubblico |

`public_id` UUIDv7 dà i vantaggi che servono all'id esposto e copiato fuori dal modulo:

1. **Monotòno nel tempo** → se entra in un indice (unique su `public_id`, o come chiave copiata
   altrove) l'insert è quasi-append: località simile a un intero, niente bloat.
2. **74 bit di casualità** → **non enumerabile / non indovinabile**.
3. **Univoco fra tutte le tabelle** → una colonna `uuid` può fare da **riferimento polimorfo** a più
   tabelle target scegliendole con un enum discriminante, senza il rischio di collisione degli spazi id
   `bigint` (es. `booking_resource_consumptions.resource_id` → postazione / articolo di magazzino /
   servizio secondo `resource_type`). Vedi §1bis.

---

## 1bis. Pro e contro: `id bigint` + `public_id uuid` vs solo `id bigint`

Confronto fra lo schema adottato (**`id bigint`** PK ovunque **+ `public_id uuid`** sulle sole entità
esposte/referenziate da fuori) e l'alternativa **solo `id bigint`**.

| Aspetto | `id bigint` + `public_id uuid` | Solo `id bigint` |
|---|---|---|
| **Migrazione dal legacy** | `id` preservato 1:1 (nessun remap sulle FK interne); va però generato `public_id` per ogni riga + una mappa `legacy_id → uuid` per le colonne cross-modulo | Massima semplicità: tutto 1:1, nessuna mappa, nessuna colonna in più |
| **Dual-run / riconciliazione col vecchio DB** | Si confronta per `id` intero come nel legacy | Identico |
| **Storage / dimensione indici** | +16 byte per riga sulle entità con `public_id` + 1 indice `UNIQUE` in più; le FK interne restano a 8 byte | Solo 8 byte ovunque, un indice in meno |
| **Performance query interne (join, filtri)** | I join/indici *dentro* il modulo usano `id bigint` → nessuna penalità | Identico |
| **Enumerazione / IDOR se l'id finisce in URL o payload** | Si espone solo `public_id` (74 bit random): non indovinabile, non enumerabile, non rivela i volumi | `/customers/224468` → `/224469`; l'id sequenziale rivela quanti record esistono e il ritmo di crescita → va offuscato altrove (gateway, hashid, …) |
| **Endpoint GDPR** (`/customers/{id}/personal-data`) | Path parametro = `public_id`, stabile e opaco | Espone l'id interno o serve un livello di offuscamento dedicato |
| **Riferimenti cross-modulo (eventi/proiezioni)** | Si copia il `public_id` (uuid): globale, stabile, non dipende da una sequenza condivisa | Si copia il `bigint`: funziona finché il modulo proprietario tiene quella PK stabile e "un id di *X* è un bigint" diventa un contratto da non rompere mai |
| **Riferimento polimorfo** (una colonna che punta a entità di **tabelle diverse** secondo un discriminante — es. `booking_resource_consumptions.resource_id` → una **postazione**, un **articolo di magazzino** *o* un **servizio** a seconda di `resource_type`; `booking_resource_consumptions.source_id` → riga prenotazione / riga servizio / hold / offerta di rivendita) | Il `public_id` uuid è **univoco fra tutte le tabelle**: bastano **una** colonna `uuid` + l'enum discriminante; nessuna FK (già impossibile cross-schema) e nessun rischio che l'`id 42` di tre tabelle diverse si confonda | Con `bigint` gli spazi id delle tabelle **si sovrappongono**: servono N colonne FK nullable, oppure `(target_type, target_id)` dove `target_id` **non** è una vera FK e può collidere fra i target |
| **Estrazione futura di un modulo in microservizio** | Zero modifiche di schema/contratto: il riferimento è già un uuid | Il bigint dipende da una sequenza del DB monolitico; su servizi separati servono id globali → o si aggiunge il `public_id` allora (migrazione), o si adotta un generatore di id distribuito |
| **Merge / replica / import fra ambienti** (dev↔staging↔prod) | Gli uuid non collidono mai | Le sequenze bigint di ambienti diversi collidono: import complicati |
| **Complessità del modello** | Due chiavi per riga sulle entità esposte: entrambe valorizzate all'`INSERT`, immutabili; ogni input esterno va tradotto `public_id → id` al confine del modulo | Una sola chiave, nessuna traduzione |
| **Debug / query manuali / supporto** | In una query diretta vedi due colonne; per correlare col legacy usi `id`, per correlare con le API usi `public_id` | Un solo id, più immediato "a occhio" |
| **Coerenza con le regole di progetto** | Allineato a [`../03-gestione-dati.md` §3.1ter](../03-gestione-dati.md) e a questo documento | Introduce un'eccezione ("il tenant è bigint") da ricordare e giustificare |
| **Rischio "chiave sbagliata nel percorso sbagliato"** | Serve disciplina: `id` mai in DTO/eventi, `public_id` mai come FK interna | Non esiste il problema (una chiave sola) |

**Regola operativa.** *Solo `id`* basta per le tabelle **puramente interne, mai esposte e mai
referenziate da altri moduli** (lookup, dettaglio, log, giunzioni) — la maggioranza. Si aggiunge
*`+ public_id`* **solo** dove l'entità (a) compare in URL/API/GDPR, **oppure** (b) il suo id viene
copiato da un altro modulo; e, in ingresso, le colonne che referenziano un altro modulo
(`property_id`, `bathhouse_id`, …) sono `uuid` = il `public_id` di quell'entità.

---

## 2. Come si genera (.NET 10 / EF Core / PostgreSQL)

### Generazione lato applicazione (consigliata)

.NET 9+ include la generazione nativa:

```csharp
var id = Guid.CreateVersion7();                 // timestamp = adesso
var id = Guid.CreateVersion7(DateTimeOffset.UtcNow);
```

Assegnare l'ID **nella factory dell'aggregato**, non lasciarlo al DB, così il timestamp del v7 coincide
con la creazione logica dell'entità:

```csharp
public static Reservation Create(/* ... */)
{
    var reservation = new Reservation(Guid.CreateVersion7(), /* ... */);
    // ...
}
```

### Mappatura EF Core / Npgsql

- **PK**: `id` `bigint`, colonna `GENERATED ALWAYS AS IDENTITY`, generata dal DB:
  ```csharp
  builder.HasKey(x => x.Id);
  builder.Property(x => x.Id).UseIdentityAlwaysColumn();   // Npgsql
  ```
- **`public_id`**: colonna PostgreSQL di tipo **`uuid`** (Npgsql mappa `Guid` ↔ `uuid` nativamente),
  **assegnata dal codice** nella factory dell'aggregato, non dal database, con indice `UNIQUE`:
  ```csharp
  builder.Property(x => x.PublicId).ValueGeneratedNever();
  builder.HasAlternateKey(x => x.PublicId);               // oppure HasIndex(...).IsUnique()
  ```
- Npgsql serializza il `Guid` nell'ordine di byte standard dell'UUID, quindi **l'ordinamento del v7 è
  preservato nell'indice B-tree** di Postgres (da confermare nel primo tuning, ma è il comportamento atteso).
- In alternativa alla generazione app-side, PostgreSQL 18 offre la funzione `uuidv7()` lato DB; qui si
  preferisce l'app per restare indipendenti dalla versione del motore e controllare il timestamp.

---

## 3. Regola di sicurezza: le PK **non sono** token segreti

Punto chiave, indipendente dalla versione di UUID: **un identificatore di risorsa non è un segreto**.
La protezione delle risorse è data dall'**autorizzazione** (verifica che l'utente possa accedere a
*quella* prenotazione), non dall'imprevedibilità dell'ID.

Tutto ciò che è **capacità/segreto** deve essere un **token casuale dedicato**, mai una PK:

| Uso | Cosa NON fare | Cosa fare |
|---|---|---|
| Reset password, conferma email, magic link, inviti | Usare la PK dell'utente/record | **Token random ≥128 bit** (`RandomNumberGenerator`), base64url, **salvato hashed**, con scadenza |
| Webhook secret / API key partner | Derivarli da ID | Segreto random dedicato, confronto **a tempo costante** |
| Sessione / refresh token | UUID come sessione | Token opachi random, revocabili |

Riassunto: **UUIDv7 per identificare, segreti random dedicati per autorizzare/capacitare.**

---

## 4. Esposizione di UUIDv7: cosa svela e quando preoccuparsene

UUIDv7 contiene un **prefisso temporale (millisecondi)**. Un ID esposto quindi rivela:

- ✅ **Non rivela** contenuti, ed è **non enumerabile** (i 74 bit random impediscono di indovinare altri ID).
- ⚠️ **Rivela l'istante di creazione** del record (al ms).
- ⚠️ **Rivela l'ordine relativo** tra record: raccogliendo più ID si può stimare il **ritmo/volume**
  di creazione (es. quante prenotazioni in un intervallo).

### Vale la pena usare UUIDv4 per il `public_id`?

Solo per la **rara** entità in cui *l'istante/ordine di creazione* è esso stesso un'informazione da
proteggere verso terzi. In quel caso il `public_id` di **quella** entità è **UUIDv4** invece di v7
(la PK resta comunque `bigint`):

```
id        (bigint identity, interno)   ─┐  performance, join, FK, migrazione 1:1 dal legacy
public_id (uuid v4, unico, esposto)    ┘  nessun leak temporale
```

Da introdurre **solo se e quando** emerge un requisito concreto, non di default (default: UUIDv7).

---

## 5. Questo progetto ha dati così critici da richiederlo?

**No.** SummerBooking (1 tenant, ~500 clienti) espone identificatori di prenotazioni, clienti,
strutture, pagamenti. Per questi:

- Il rischio reale degli ID è l'**enumeration** → già neutralizzato da UUIDv7 (parte random).
- Il leak di **timestamp/ordine** di una prenotazione o di un cliente ha sensibilità **bassa** e non
  giustifica il costo (bloat/perf) di UUIDv4 come PK.
- I dati davvero sensibili — **credenziali, dati di pagamento (Stripe), scontrino (Fiskaly)** — non si
  proteggono con la versione dell'UUID ma con: **tokenizzazione lato provider** (i dati carta stanno da
  Stripe, non da noi), **segreti dedicati** (§3), **autorizzazione** per-risorsa e **cifratura**.

**Conclusione:** `bigint identity` come PK ovunque (migrazione 1:1 dal legacy, join/indici compatti);
`public_id` UUIDv7 sulle entità esposte/referenziate cross-modulo; i controlli di sicurezza stanno
altrove (autorizzazione per-risorsa + segreti random dedicati + delega ai provider PCI/fiscali).
UUIDv4 per il `public_id` resta un'opzione da tenere in tasca per la singola entità in cui, in
futuro, l'istante di creazione diventasse sensibile.

---

## 6. Checklist implementativa

- [ ] PK `id bigint GENERATED ALWAYS AS IDENTITY` su **tutte** le tabelle.
- [ ] `public_id uuid UNIQUE` (UUIDv7) **solo** su: entità esposte via URL/REST o endpoint GDPR, ed
      entità il cui id è copiato in eventi/proiezioni da un altro modulo. Tabelle solo interne (join,
      lookup, dettaglio, log): niente `public_id`.
- [ ] `public_id` generato in factory con `Guid.CreateVersion7()` (migrare gli attuali `Guid.NewGuid()`).
- [ ] EF Core: PK `UseIdentityAlwaysColumn()`; `Property(PublicId).ValueGeneratedNever()`, colonna `uuid`, indice unico.
- [ ] Riferimenti **cross-modulo**: copiare sempre il `public_id` (uuid), mai il `bigint`. Input esterno
      risolto `public_id → id` una volta, al confine del modulo.
- [ ] Nessun id usato come token: reset/conferma/inviti/webhook = **segreti random hashed** con scadenza.
- [ ] Autorizzazione per-risorsa su ogni endpoint (no reliance sull'imprevedibilità dell'id).
- [ ] `public_id` UUIDv4 **solo** dove un requisito esplicito impone di nascondere l'istante di creazione.
