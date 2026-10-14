# Prestazioni — overhead vs velocizzazione delle decisioni di refactoring

> Quadro d'insieme di **tutte** le decisioni proposte nei documenti di progetto, classificate per
> impatto sulle prestazioni / sull'overhead operativo. Serve a rendere esplicito, in un unico posto,
> quali scelte aggiungono costo (latenza, infrastruttura, complessità), quali lo riducono, e quali
> pagano un piccolo costo per prevenirne uno maggiore.
>
> Ogni riga rimanda al documento che la definisce e motiva. Questo documento **non introduce**
> decisioni nuove: le riepiloga.
>
> **Documento vivo**: aggiungere una riga quando una nuova decisione con impatto prestazionale viene
> presa altrove.

> **Nota di coerenza da chiudere (DB engine)**: [02-strategia-di-test.md](02-strategia-di-test.md)
> indica il DB come **MySQL**; [03-gestione-dati.md](03-gestione-dati.md),
> [12-lingue-e-date.md](12-lingue-e-date.md), [pre/identificatori-e-uuid.md](pre/identificatori-e-uuid.md)
> e [../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) assumono **PostgreSQL** (`jsonb`,
> `timestamptz`, `ENUM` nativo, RDS PostgreSQL Multi-AZ). Le tabelle qui sotto e i documenti derivati
> da questa sessione seguono la maggioranza (**PostgreSQL**), ma la decisione va resa **esplicita e
> uniforme in un unico posto** (proposta: [03-gestione-dati.md](03-gestione-dati.md)).

---

## 0. Legenda

- **+OH** — aggiunge overhead (latenza, infrastruttura, complessità)
- **-OH** — riduce overhead (elimina query, ridondanze, bug)
- **±OH** — entrambi: un piccolo costo che ne previene uno maggiore

---

## 1. Dati e schema DB

| Decisione | Documento | Impatto | Perché |
|---|---|---|---|
| URL file scomposto in `origin`+`path` invece di URL completo | [03-gestione-dati.md](03-gestione-dati.md) | ±OH | Elimina migrazioni per cambio bucket, ma aggiunge un join di ricostruzione |
| Tabella `*_media_assets` invece di array JSON di foto | [03-gestione-dati.md](03-gestione-dati.md) | -OH | INSERT/DELETE normali al posto della riscrittura dell'intero blob |
| `enum` C# per vocabolari chiusi | [03-gestione-dati.md](03-gestione-dati.md) | -OH | Elimina il drift di stringhe equivalenti, zero costo runtime |
| Tabella di lookup per vocabolari growable da pannello | [03-gestione-dati.md](03-gestione-dati.md) | +OH | Necessaria solo se serve gestione runtime senza deploy |
| Normalizzazione dati prima della migrazione a enum/lookup | [03-gestione-dati.md](03-gestione-dati.md) | +OH | Lavoro one-off per non riprodurre il drift esistente |
| Niente tabella EAV generica (`entity_type,field,value`) | [03-gestione-dati.md](03-gestione-dati.md) | -OH | Mantiene FK reali e indicizzazione mirata |
| Un solo motore di placeholder (`{{ }}`) | [03-gestione-dati.md](03-gestione-dati.md) | -OH | Elimina 3 motori duplicati di sostituzione |
| Naming uniforme (`snake_case`, plurale, ecc.) | [03-gestione-dati.md](03-gestione-dati.md) | -OH | Elimina ambiguità, nessun costo runtime |
| `created_at`/`updated_at` via trigger/interceptor | [03-gestione-dati.md](03-gestione-dati.md) | -OH | Elimina codice duplicato/dimenticato nei controller |
| Soft-delete uniforme + Global Query Filter EF | [03-gestione-dati.md](03-gestione-dati.md) | ±OH | Filtro automatico richiesto, ma sostituisce flag incoerenti sparsi |
| Snapshot tipizzati invece di blob JSON per fatti storici | [03-gestione-dati.md](03-gestione-dati.md) | ±OH | Duplica il dato alla "congelazione", ma lo rende stabile e indicizzabile |
| `jsonb` passthrough per documenti di sistemi esterni | [03-gestione-dati.md](03-gestione-dati.md) | -OH | Evita di trasformare ogni evoluzione di un'API terza in una migrazione |
| Importi `numeric(12,2)` invece di float/string | [03-gestione-dati.md](03-gestione-dati.md) | -OH | Elimina bug di arrotondamento |
| Ricalcolo server-side obbligatorio (`POST /bookings/quote`) | [03-gestione-dati.md](03-gestione-dati.md); [01-criticita](01-criticita-e-indicazioni-backend.md) | +OH | Round-trip in più, ma elimina la frode/discrepanza sugli importi |
| Tabella unica `resource_consumptions` invece di 6 sovrapposte | [03-gestione-dati.md](03-gestione-dati.md) | -OH | Un solo posto per la logica di disponibilità |
| `resource_availability_daily` aggregato + trigger di sync | [03-gestione-dati.md](03-gestione-dati.md) | ±OH | Scrittura extra a ogni consumo, ma lettura da GROUP BY a SELECT puntuale |
| Cache Redis per la disponibilità (mai fonte di verità) | [03-gestione-dati.md](03-gestione-dati.md); [16-redis.md](16-redis.md) | +OH | Componente in più per scaricare le letture |
| Hold temporaneo (`state=held`) durante il checkout | [03-gestione-dati.md](03-gestione-dati.md) | +OH | Nuovo stato/job di scadenza, necessario contro la doppia prenotazione oggi assente |
| Lock/transazione o constraint di esclusione su disponibilità/stock | [03-gestione-dati.md](03-gestione-dati.md); [01-criticita](01-criticita-e-indicazioni-backend.md) | +OH | Contesa in scrittura, ma chiude overbooking e race condition sistemiche |
| `ENUM` nativo Postgres per vocabolari stabili | [03-gestione-dati.md](03-gestione-dati.md) | -OH | Una sola fonte di verità del tipo |
| Uno schema/`DbContext` per modulo (no DB monolitico) | [03-gestione-dati.md](03-gestione-dati.md); [pre/architettura-evolutiva-microservizi.md](pre/architettura-evolutiva-microservizi.md) | +OH | Niente più JOIN comode cross-modulo, riferimenti per ID più verbosi |
| PK monotone (UUIDv7, non UUIDv4) | [pre/identificatori-e-uuid.md](pre/identificatori-e-uuid.md); [../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) | -OH | Evita frammentazione dell'indice da inserimento casuale |

---

## 2. Multilingua, date, log

| Decisione | Documento | Impatto | Perché |
|---|---|---|---|
| UTC ovunque + `IDateTimeProvider` | [12-lingue-e-date.md](12-lingue-e-date.md) | -OH | Elimina compensazioni di fuso e rende testabili le scadenze |
| Tabella di traduzione per entità (non generica) | [12-lingue-e-date.md](12-lingue-e-date.md) | +OH | N tabelle in più, ma FK tipizzate, niente refusi silenziosi |
| Cache granulare (entità×cultura) per contenuti multilingua | [12-lingue-e-date.md](12-lingue-e-date.md) | ±OH | Evita join di traduzione ripetuti, richiede invalidazione esplicita |
| Fallback per campo, non per record | [12-lingue-e-date.md](12-lingue-e-date.md) | +OH | Il frontend non può assumere liste omogenee, ma niente campi vuoti |
| Avvisi come `code + parameters_json` | [12-lingue-e-date.md](12-lingue-e-date.md) | -OH | Evita di ricalcolare la validazione per ristampare in un'altra lingua |
| Nessuna aritmetica importi lato client | [12-lingue-e-date.md](12-lingue-e-date.md) | +OH | Riconciliazione in più, ma elimina discrepanze double/decimal |
| `ILogger<T>` + Serilog JSON + OpenTelemetry | [15-logs.md](15-logs.md); [31-observability.md](31-observability.md) | +OH | Nuove dipendenze, ma logging strutturato e correlabile |
| Log solo su stdout → CloudWatch → S3 | [15-logs.md](15-logs.md) | -OH | Elimina i file di log locali del legacy |
| 5 profili di volume log configurabili | [15-logs.md](15-logs.md) | ±OH | Meccanismo in più, ma consente di tagliare costi o alzare diagnostica a richiesta |
| Floor minimo P3 permanente sui moduli critici | [15-logs.md](15-logs.md) | +OH | Più righe di log, ma un evento perso su Payment/Fiskaly costa di più |
| `LoggingLevelSwitch` runtime con TTL | [15-logs.md](15-logs.md) | +OH | Endpoint dedicato, ma evita un log level alto dimenticato acceso |
| EF Core query log a `Warning` in prod | [15-logs.md](15-logs.md) | -OH | Elimina il logging di query con parametri a ogni chiamata |
| Redazione PII/segreti nei log | [15-logs.md](15-logs.md); [17-secrets.md](17-secrets.md) | +OH | Lavoro di policy dedicato, chiude il logging di credenziali/PAN visto nel legacy |
| `IExceptionHandler` globale unico | [15-logs.md](15-logs.md); [01-criticita](01-criticita-e-indicazioni-backend.md) | -OH | Sostituisce decine di gestioni ad-hoc/inesistenti |

---

## 3. Cache, job, architettura

| Decisione | Documento | Impatto | Perché |
|---|---|---|---|
| `ICache` predisposto subito, Redis rimandato | [16-redis.md](16-redis.md) | -OH | Zero infrastruttura finché non serve davvero |
| Attivazione Redis solo su trigger concreti misurati | [16-redis.md](16-redis.md) | +OH (quando attivato) | Nuovo nodo/punto di fallimento, ma evitato finché non necessario |
| Logica del job in Application, trigger sottile nell'host | [20-jobs.md](20-jobs.md) | -OH | Job unit-testabile, nessun costo aggiuntivo |
| Job in-process (`BackgroundService`) di default | [20-jobs.md](20-jobs.md) | -OH | Evita un secondo deployable prima che serva davvero |
| Hangfire/Quartz con storage e lock quando serve | [20-jobs.md](20-jobs.md) | +OH | Nuova dipendenza, garantisce single-execution sui job che toccano soldi |
| Idempotenza + overlap guard reale (lock distribuito) | [20-jobs.md](20-jobs.md); [01-criticita](01-criticita-e-indicazioni-backend.md) | +OH | Elimina invii duplicati e race condition |
| Job cross-modulo via eventi + outbox | [20-jobs.md](20-jobs.md) | +OH | Richiede event bus/outbox, ma pronto per estrazione a microservizio |
| Timezone esplicito `Europe/Rome` sui job | [20-jobs.md](20-jobs.md) | -OH | Chiude il bug del cron su fuso sbagliato |
| Monolite modulare invece di microservizi da subito | [pre/architettura-evolutiva-microservizi.md](pre/architettura-evolutiva-microservizi.md) | -OH | Evita costi di deploy distribuiti senza benefici al contesto attuale |
| Regole di confine tra moduli (dati posseduti, comunicazione via interfacce) | [pre/architettura-evolutiva-microservizi.md](pre/architettura-evolutiva-microservizi.md) | ±OH | Più disciplina quotidiana, ma rende l'estrazione futura meccanica |
| Event bus in-memory (sostituibile in futuro) | [pre/architettura-evolutiva-microservizi.md](pre/architettura-evolutiva-microservizi.md) | -OH | Nessuna infrastruttura di messaging oggi |
| Architecture test (NetArchTest) in CI | [pre/architettura-evolutiva-microservizi.md](pre/architettura-evolutiva-microservizi.md); [01-criticita](01-criticita-e-indicazioni-backend.md) | +OH | Build/manutenzione in più, ma impedisce il degrado dei confini (visto nel legacy con i middleware disattivati) |
| Non estrarre microservizi senza un driver concreto | [pre/architettura-evolutiva-microservizi.md](pre/architettura-evolutiva-microservizi.md) | -OH | Evita di pagare oggi costi operativi non necessari |
| Riscrittura totale in .NET/EF Core | [pre/refactoring-analisi-A-vs-B.md](pre/refactoring-analisi-A-vs-B.md) | ±OH | Costo iniziale enorme, ma unica strada che risolve la causa radice (dati in JSON) |
| Parallel run manuale durante la riscrittura | [pre/refactoring-analisi-A-vs-B.md](pre/refactoring-analisi-A-vs-B.md) | +OH | Carico sui tester, compensa l'assenza di test automatici legacy |
| Migrazione dati in un'unica finestra (script idempotente) | [pre/refactoring-analisi-A-vs-B.md](pre/refactoring-analisi-A-vs-B.md) | +OH | Settimane di lavoro dedicato, ma reversibile e a rischio ridotto |
| EF Core con query parametrizzate | [pre/refactoring-analisi-A-vs-B.md](pre/refactoring-analisi-A-vs-B.md); [01-criticita](01-criticita-e-indicazioni-backend.md) | -OH | Elimina SQL injection sistemica senza costo prestazionale |

---

## 4. Sicurezza, auth, infrastruttura

| Decisione | Documento | Impatto | Perché |
|---|---|---|---|
| Secure-by-default (`FallbackPolicy` autenticazione) | [pre/autenticazione-autorizzazione-dotnet.md](pre/autenticazione-autorizzazione-dotnet.md); [01-criticita](01-criticita-e-indicazioni-backend.md) | -OH | Un endpoint dimenticato resta protetto, non esposto |
| Ruolo/scoping nel JWT (no round-trip a Identity) | [pre/autenticazione-autorizzazione-dotnet.md](pre/autenticazione-autorizzazione-dotnet.md) | -OH | Autorizzazione decentralizzata senza chiamata extra |
| Due schemi JWT (`UserBearer`/`ApiBearer`) | [pre/autenticazione-autorizzazione-dotnet.md](pre/autenticazione-autorizzazione-dotnet.md) | +OH | Config doppia, ma impedisce che un token utente valga per M2M |
| Access token breve + refresh token | [pre/autenticazione-autorizzazione-dotnet.md](pre/autenticazione-autorizzazione-dotnet.md) | +OH | Flow di refresh da gestire, chiude il token valido 365gg senza revoca |
| Client esterni via client_credentials | [pre/autenticazione-autorizzazione-dotnet.md](pre/autenticazione-autorizzazione-dotnet.md) | +OH | Endpoint in più, ma autenticazione M2M oggi assente |
| Webhook autenticati via firma, non JWT | [pre/autenticazione-autorizzazione-dotnet.md](pre/autenticazione-autorizzazione-dotnet.md); [35-integrazioni-esterne.md](35-integrazioni-esterne.md) | -OH | Chiude webhook oggi privi di verifica |
| Identity minimale, anagrafiche in modulo separato | [pre/autenticazione-autorizzazione-dotnet.md](pre/autenticazione-autorizzazione-dotnet.md) | -OH | Riduce accoppiamento cross-modulo |
| Autorizzazione per-risorsa centralizzata (no id dal body) | [01-criticita](01-criticita-e-indicazioni-backend.md); [34-multitenant.md](34-multitenant.md) | +OH | Handler/policy in più, ma chiude l'IDOR sistemico |
| `Task.WhenAll` esplicito invece di `.map(async)` non atteso | [01-criticita](01-criticita-e-indicazioni-backend.md) | -OH | Nessun costo, chiude race condition e risposte premature |
| Rate limiting attivo di default | [01-criticita](01-criticita-e-indicazioni-backend.md) | +OH | Overhead di throttling per richiesta, ma chiude una protezione oggi sempre disattivata |
| Test di autorizzazione per ogni endpoint sensibile in CI | [01-criticita](01-criticita-e-indicazioni-backend.md) | +OH | Suite in più, evita regressioni silenziose come nel legacy |
| ECS Fargate con auto-scaling stagionale | [../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) | -OH | Paga la capacità a domanda invece del picco sempre acceso |
| ARM64/Graviton (Fargate, RDS, ElastiCache) | [../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) | -OH | ~20% più economico a parità di prestazioni |
| RDS Multi-AZ + Reserved Instance | [../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) | ±OH | Costo di impegno e del secondo nodo, ma failover automatico e sconto |
| RDS Proxy (solo quando serve) | [../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) | +OH | Introdotto solo quando il pool nativo non basta più |
| Job/worker scorporati dal web (EventBridge/SQS) | [../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md); [01-criticita](01-criticita-e-indicazioni-backend.md) | +OH | Più componenti, ma isola i guasti dei job dal traffico utenti |
| CloudFront + WAF davanti ad ALB | [../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) | +OH | Componente e costo in più, riduce latenza e rischio DDoS |
| Riduzione NAT Gateway via VPC Endpoints | [../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) | -OH | Elimina un costo ricorrente |
| Switchboard isolato su runtime dedicato | [../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md); [pre/architettura-evolutiva-microservizi.md](pre/architettura-evolutiva-microservizi.md) | +OH | Componente separato, ma un runtime SIP/real-time non si adatta a HTTP stateless |

---

## 5. Osservabilità, secret, privacy, multitenancy, resilienza, offline

| Decisione | Documento | Impatto | Perché |
|---|---|---|---|
| Secret via `IConfiguration`/`appsettings.Local.json`/Secrets Manager (mai hardcoded) | [17-secrets.md](17-secrets.md) | -OH | Nessun costo runtime, elimina la classe di incidenti "chiave live nel repo" |
| Test/gitleaks anti-secret in CI | [17-secrets.md §5](17-secrets.md) | +OH | Gate in più in pipeline, previene un incidente ben più costoso |
| Cifratura at-rest dei campi PII/credenziali elencati | [11-modulo-privacy.md §4](11-modulo-privacy.md) | +OH | Costo di cifratura/decifratura, necessario per i dati più sensibili individuati |
| API di cancellazione/export via eventi cross-modulo | [11-modulo-privacy.md §5](11-modulo-privacy.md) | +OH | Coordinamento asincrono multi-modulo, ma è l'unico modo coerente con "uno schema per modulo" |
| Retention: policy dichiarativa + job decentrati per modulo | [11-modulo-privacy.md §6](11-modulo-privacy.md); [80-retention.md](80-retention.md) | ±OH | Job in più per modulo, ma elimina la crescita illimitata dei dati e il rischio GDPR |
| OpenTelemetry + ADOT + dashboard/alert | [31-observability.md](31-observability.md) | +OH | Nuova infrastruttura di osservabilità, ma riduce drasticamente il tempo di diagnosi in produzione |
| Campionamento tracce (100% errori, ~10% successi) | [31-observability.md §5](31-observability.md) | -OH | Tiene sotto controllo il costo X-Ray senza perdere visibilità sugli incidenti |
| Backup: PITR + export logico + copia cross-region + restore drill | [40-backup.md](40-backup.md) | +OH | Storage e job in più, ma è la rete di sicurezza su dati non rigenerabili |
| Row-Level Security Postgres come difesa in profondità multi-tenant | [34-multitenant.md §1.3](34-multitenant.md) | +OH | Overhead per query, ma secondo livello di protezione indipendente dal codice contro l'IDOR |
| `tenant_id` da claim + Global Query Filter | [34-multitenant.md §1](34-multitenant.md) | -OH | Nessun round-trip, chiude l'IDOR alla radice |
| Resilienza (Polly) su ogni client esterno + idempotency key | [35-integrazioni-esterne.md §2-3](35-integrazioni-esterne.md) | +OH | Configurazione per client, ma elimina doppi addebiti/effetti duplicati su fallimento di rete |
| Integrazioni esterne come layer dedicato (contratto + impl) | [35-integrazioni-esterne.md §1](35-integrazioni-esterne.md) | -OH | Un solo client per vendor, testabile senza contattarlo, sostituibile senza toccare il dominio |
| Uso offline del gestionale (working set + coda comandi) | [19-frontend-offline.md](19-frontend-offline.md) | +OH | Endpoint di sync, colonne `version`/`occurred_at`, store di idempotenza — ma è un requisito del committente |
| Sanificazione obbligatoria del DB di test (mai dump grezzo) | [54-database-di-test.md](54-database-di-test.md) | +OH | Pipeline dedicata, ma evita PII reali e credenziali reali in ambienti a controlli deboli |

---

## 6. Lettura d'insieme

- La maggior parte dei **-OH** viene dall'eliminazione di duplicazione e di bug strutturali del legacy
  (motori di placeholder multipli, CRUD copiato, gestione errori ad-hoc, `sync({alter:true})`).
- La maggior parte dei **+OH** è **difesa** (idempotenza, lock/constraint, RLS, rate limiting, outbox,
  anti-secret): costo per richiesta o per job in cambio della chiusura di intere classi di incidenti
  già osservati.
- I **±OH** sono quasi tutti trade-off di *forma dei dati* (snapshot tipizzati, URL scomposto,
  soft-delete): un piccolo costo di lettura/join in cambio di stabilità e assenza di migrazioni.
- Nessuna delle voci **+OH** è infrastruttura "preventiva": Redis, sharding, RDS Proxy, worker
  scorporati e microservizi sono tutti condizionati a un **driver concreto misurato**.
