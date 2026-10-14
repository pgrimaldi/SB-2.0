# Multitenancy e isolamento dei tenant

> Come si modella e si **impone** l'isolamento tra stabilimenti nel nuovo backend **.NET 10**, e quali
> evoluzioni (schema per tenant, sharding) hanno senso solo in presenza di un driver concreto.
>
> Complemento a [01-criticita-e-indicazioni-backend.md §3.2, §13.2-13.3](01-criticita-e-indicazioni-backend.md)
> (IDOR sistemico: `tenant_id` preso dal body invece che dal token),
> [pre/autenticazione-autorizzazione-dotnet.md §2, §5](pre/autenticazione-autorizzazione-dotnet.md)
> (claim `property_id` nel JWT, autorizzazione per-risorsa),
> [pre/linee-guida-implementative-dotnet.md §1-2, §5](pre/linee-guida-implementative-dotnet.md) (uno
> schema per modulo, confini), [03-gestione-dati.md](03-gestione-dati.md) (colonna discriminatore sulle
> tabelle), [53-prestazioni.md](53-prestazioni.md) (RLS come `+OH` giustificato) e
> [../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) (dimensionamento RDS per ~500
> stabilimenti).
>
> **Documento vivo**: da aggiornare se emerge un requisito di data residency o un cliente enterprise con
> isolamento contrattuale.

---

## 0. Principio guida

SummerBooking **è già multi-tenant nella sostanza**: ogni `property`/stabilimento è un tenant, con
`property_id` come discriminatore su gran parte delle tabelle. Il problema non è l'assenza di un modello
multi-tenant — è che l'**isolamento non è imposto**. Prima si consolida il livello 0 (shared DB, shared
schema, discriminatore + autorizzazione, opzionalmente RLS); i livelli superiori si affrontano **solo**
con un driver concreto.

---

## 1. Stato attuale e priorità immediata

L'assenza di isolamento imposto è precisamente la causa dell'**IDOR sistemico** descritto in
[01-criticita-e-indicazioni-backend.md §3.2](01-criticita-e-indicazioni-backend.md): endpoint che
accettano `property_id` dal corpo della richiesta e restituiscono dati di un altro stabilimento.

Priorità immediata — **non è "il futuro", è la base**: applicare correttamente il modello **shared DB,
shared schema, colonna discriminatore**.

1. **`tenant_id` sempre dal claim del token** (`property_id`,
   [pre/autenticazione-autorizzazione-dotnet.md §2](pre/autenticazione-autorizzazione-dotnet.md)), **mai**
   dal body della richiesta ([01-criticita §13.2-13.3](01-criticita-e-indicazioni-backend.md)).
2. **Una policy di autorizzazione basata su risorsa**, condivisa da tutti gli endpoint scoped-per-tenant
   — non un controllo ripetuto (e dimenticabile) per handler.
3. **Difesa in profondità (opzionale ma consigliata): Row-Level Security (RLS) di PostgreSQL** sulle
   tabelle multi-tenant.

### 1.1 `tenant_id` dal contesto, non dal request

- Un `ITenantContext` risolto una volta per richiesta dal claim, iniettato dove serve.
- I `DbContext` dei moduli applicano un **Global Query Filter** su `tenant_id = _tenantContext.Id`
  (stesso meccanismo del soft-delete, [03-gestione-dati.md §3.5](03-gestione-dati.md)): una query che
  "dimentica" il `WHERE tenant_id` resta comunque filtrata.
- In `SaveChanges`, il `tenant_id` delle nuove entità è impostato dall'interceptor dal contesto, non
  dal payload.
- I job/worker e i consumer di eventi che non hanno un HTTP context impostano esplicitamente il tenant
  per ogni unità di lavoro ([20-jobs.md](20-jobs.md)).

### 1.2 Autorizzazione per-risorsa

- Una `AuthorizationPolicy` + `IAuthorizationHandler` che, dato l'id di risorsa nella route, verifica
  che la risorsa appartenga al tenant del token **prima** di eseguire l'handler.
- Applicata via requirement condiviso (`RequireTenantResource`), non ricodificata endpoint per
  endpoint.
- Test di autorizzazione in CI per ogni endpoint scoped-per-tenant
  ([01-criticita-e-indicazioni-backend.md](01-criticita-e-indicazioni-backend.md),
  [02-strategia-di-test.md](02-strategia-di-test.md)): un test che chiede la risorsa del tenant A con
  il token del tenant B deve ottenere 403/404.

### 1.3 Row-Level Security come secondo livello

Sulle tabelle multi-tenant, una policy RLS con la sessione DB che imposta il tenant corrente a ogni
richiesta:

```sql
ALTER TABLE bookings ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON bookings
  USING (property_id = current_setting('app.current_tenant')::uuid);
```

> **Nota sul tipo.** La colonna del discriminatore è **`property_id uuid`** e contiene il **`public_id`**
> (UUIDv7) della `property` — il modulo proprietario dello stabilimento (C2 `catalog`) tiene `id bigint`
> come PK **interna** e `public_id uuid` come riferimento esposto; ogni altro modulo copia **il
> `public_id`, mai il `bigint`** ([03-gestione-dati.md §3.1ter](03-gestione-dati.md)). Così l'estrazione
> futura di C2 in microservizio non tocca né lo schema né il contratto del tenant. `tenant_id` è usato
> in questo documento come **nome concettuale** della stessa colonna. Il claim `property_id` del JWT
> porta questo uuid.

```csharp
// interceptor EF: all'apertura della connessione per la richiesta
await connection.ExecuteAsync("SET app.current_tenant = @tenant", new { tenant = tenantContext.Id });
```

- **Costo**: configurazione iniziale per tabella + leggero overhead per query.
- **Beneficio**: se l'autorizzazione applicativa avesse un bug, l'RLS a livello di database **blocca
  comunque** la query cross-tenant — un secondo livello indipendente dal codice, proprio sul tipo di
  bug che ha causato più danni nel legacy.
- Attenzione: l'utente DB dell'applicazione **non** deve avere `BYPASSRLS`; le migrazioni/job
  amministrativi usano un ruolo distinto.

---

## 2. Evoluzione futura — solo con un driver concreto

Nessuna di queste va costruita preventivamente.

| Livello | Modello | Quando ha senso | Costo operativo |
|---|---|---|---|
| **0** (attuale, da consolidare) | Shared DB, shared schema, colonna `tenant_id` + autorizzazione (+ RLS) | Sempre, è la base | Basso |
| **1** | Schema Postgres per tenant, stessa istanza DB | Un cliente enterprise richiede contrattualmente l'isolamento dei dati, o serve export/restore per singolo tenant senza toccare gli altri | Medio: migrazioni EF da eseguire per-schema, connection string / schema dinamico per tenant |
| **2** | Database per tenant (sharding), router tenant→istanza | Un singolo tenant genera un carico che penalizza gli altri (noisy neighbor), o un requisito di **data residency** impone che i dati di un cliente stiano in una regione/istanza specifica | Alto: servizio di routing, migrazioni su ogni shard, reportistica cross-tenant che non può più fare JOIN live (serve data warehouse / aggregazione asincrona) |

### 2.1 Perché non ora

Con ~500 stabilimenti l'istanza PostgreSQL dimensionata in
[../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) è ampiamente sufficiente. Introdurre
sharding senza un driver reale aggiunge solo complessità operativa
([53-prestazioni.md](53-prestazioni.md), riga "sharding").

### 2.2 Cosa fare fin da subito perché il salto resti meccanico

- Mantenere rigorosamente i **confini di modulo/schema** già decisi
  ([pre/linee-guida-implementative-dotnet.md](pre/linee-guida-implementative-dotnet.md)).
- Il discriminatore su **ogni** tabella multi-tenant fin dal disegno iniziale: colonna `property_id`
  di tipo **`uuid`** (il `public_id` della `property`, copiato da C2 — nessuna FK cross-modulo),
  indicizzata, spesso come prima colonna di indici compositi.
- Nessun accesso ai dati che non passi da `ITenantContext` (niente query "amministrative" sparse che
  scavalcano il filtro).
- Reportistica cross-tenant (per il SystemAdmin) già isolata dietro un servizio dedicato, così quando
  al livello 2 non si potrà più fare JOIN live si sostituisce solo quell'implementazione.

Questo è esattamente ciò che rende possibile, in futuro, spostare un singolo tenant su un proprio schema
o una propria istanza **senza toccare il codice applicativo**.

---

## 3. Testing dell'isolamento

- **Unit/integration**: per ogni aggregato multi-tenant, un test "tenant B non vede/tocca la risorsa di
  tenant A" (query filtrata, comando rifiutato).
- **RLS**: test che esegue una query con `app.current_tenant` impostato su B e verifica zero righe di A,
  anche bypassando il Global Query Filter EF (query SQL raw).
- **Autorizzazione**: suite in CI su tutti gli endpoint scoped-per-tenant (§1.2).
- **Job**: test che un job processi solo i dati del tenant per cui è stato schedulato / di tutti i
  tenant in modo esplicito e isolato, senza fuga.
