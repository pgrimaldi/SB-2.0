# Analisi decisionale: Riscrittura in .NET (A) vs Migliorie parziali su Node (B)

> Documento di valutazione strategica per il backend `summer-booking-backend`.
> Ipotesi di lavoro: l'Opzione A prevede la **riscrittura completa in .NET 10 (Web API)** + Entity
> Framework Core, C#. È un *replatform* cross-linguaggio: il codice Node/Express/Sequelize viene
> sostituito, non riusato.

NOTA: è stata scelta l'opzione A!

---

## 1. Executive summary

Il backend è una codebase di **~98.000 LOC** (line of code) cresciuta in ~4,5 anni (2.785 commit) con problemi
strutturali profondi: logica di business nei controller, modello dati destrutturato in colonne JSON,
dipendenze con CVE (Common Vulnerabilities and Exposures, vulnerabilità di sicurezza note e catalogate).

Vengono confrontate due strategie:

- **Opzione A — Riscrittura totale in .NET**: riscrittura completa del backend in **.NET 10 (Web API) + EF
  Core (C#)**, con nuovo modello dati relazionale, migrazione dei dati e adeguamento del frontend.
  *Replatform* cross-linguaggio: il codice Node attuale viene sostituito, non riusato.
- **Opzione B — Migliorie parziali su Node**: interventi incrementali mirati sul codice Node esistente,
  senza cambiare stack né modello dati.

**Verdetto sintetico:** dato il contesto specifico (**1 solo tenant**, **frontend interamente
controllato**, **7 mesi di finestra stagionale morta** — inizio agosto → fine febbraio, **2-3 dev + 2 tester**), l'Opzione A —
normalmente sconsigliata — diventa **fattibile e a rischio contenuto**, ed è l'unica che risolve la
causa radice (modello dati in JSON). Si raccomanda **A in versione "phased big-bang"** (riscrittura a
fette verticali con rete di sicurezza tramite parallel run manuale), **non** un rewrite
"from scratch" alla cieca. L'Opzione B (restare su Node con migliorie incrementali) non risolve la
causa radice: va considerata un ripiego, non un'alternativa strategica.

---

## 2. Contesto e vincoli

| Fattore | Valore | Impatto sulla decisione |
|---|---|---|
| Sviluppatori | 2-3 dev | ~14-21 dev-mesi disponibili nella finestra |
| Tester | 2 dedicati | Abilitano il parallel run manuale (regressione a mano su vecchio vs nuovo): compensano gli 0 test |
| Frontend | Interamente controllato | Si possono cambiare contratti API e modello dati liberamente |
| Produzione | **1 installazione condivisa (tenant)**, ~200-300 **stabilimenti** (clienti del software) | Migrazione dati = evento singolo su un DB, controllato, reversibile con backup |
| Finestra | **~7 mesi** (inizio agosto → fine febbraio, stagione morta) | Runway reale per lavoro strutturale; tutto il perimetro rientra nella finestra |

I tre vincoli che di norma rendono letale un rewrite (**molte installazioni separate** da migrare,
frontend di terzi, nessun tempo) **qui non sussistono**: i ~200-300 stabilimenti vivono in un'unica
installazione/DB, quindi la migrazione resta un evento singolo (vedi però la scalabilità in §6.5).

---

## 3. Fotografia oggettiva del codice (baseline misurata)

| Dato | Valore | Nota |
|---|---|---|
| LOC totali (`app/`) | ~97.900 su 240 file | Codebase medio-grande |
| Controller | 54 | Logica di dominio prevalentemente qui |
| Model Sequelize | 92 | Schema frammentato, con refusi (`booking.modle.js`) e duplicati (`bookingItemsCountNew`) |
| Endpoint HTTP | 458 | Di cui **375 in un solo `user.routes.js`** |
| `booking.controller.js` | **12.775 LOC** | God object |
| `bookingTemplates.js` | **10.005 LOC** | Template email mescolati al codice |
| `stripe.controller.js` | 4.461 LOC | Pagamenti |
| Colonne JSON/TEXT nei model | ~120 | Model `booking`: **19 colonne JSON** in una tabella |
| `JSON.parse` nel codice | **633** | Dati relazionali salvati come JSON |
| Query SQL raw (`sequelize.query`/`.query`) | **281** | Logica DB fuori dai model, superficie SQLinjection |
| Service layer | 18 service | Presente ma **usato in modo incoerente** |
| Helper | 13 | Include telefonia (SIP/Telnyx/WhatsApp), fiskaly, payment |
| Integrazioni esterne | Stripe, Fiskaly, Twilio/Telnyx/SIP, WhatsApp, Firebase, SendGrid, AWS, Redis, Socket.io | Molti punti di rottura |
| Storia | 2.785 commit dal 2021-02 | ~4,5 anni di stratificazione |

### Problemi strutturali principali

1. **Logica nei controller** — nessuna separazione dominio/trasporto; service layer solo parziale.
2. **Dati destrutturati** — 633 `JSON.parse` e ~120 colonne JSON/TEXT: dati che dovrebbero essere
   relazioni sono blob non indicizzabili, non validabili, non interrogabili in modo efficiente.
3. **Template mescolati al codice** — 14 file `mailtemplates/`, di cui uno da 10k LOC.
4. **Dati restituiti su più chiamate** — il frontend deve orchestrare più round-trip per popolare
   una singola schermata.
5. **Duplicazione dati e modelli** — es. `bookingItemsCount` vs `bookingItemsCountNew`,
   `bookingItemsForScript`; dati duplicati tra tabelle.
6. **Assenza di test** — ogni modifica è un rischio di regressione non rilevabile.
7. **Sicurezza** — dipendenze con CVE note (importi stripe in front, ecc..); 281 query raw come superficie SQL injection.

---

## 4. Mappa delle componenti funzionali

I 54 controller si raggruppano in **7 domini**. Questa mappa è la base per stimare A e B per componente.

| # | Dominio | Componenti principali | Peso / criticità | Complessità intrinseca |
|---|---|---|---|---|
| C1 | **Prenotazioni (core)** | `booking` (12.775), `booking.helper` (3.839), `servicereservation`, `locationreservation`, `onlinereservation`, absences/groups | Massima | Molto alta |
| C2 | **Catalogo & configurazione** | `bathouse`, `property`, `sectors`, `seasons`, `pricelist`, `services`, `servicetypes`, `serviceFilters`, `confimages`, `confimaps`, `discounts`, `searchRegion`, `warehouse` | Alta | Media |
| C3 | **Pagamenti & fiscale** | `stripe` (4.461), `stripeWebhookHandler`, `payment`, `bookingPayments`, `cardTransaction`, `onlineTransaction`, `transactionApi`, `revenue` | Massima | Molto alta |
| C4 | **Fiskaly (scontrino fiscale IT)** | `fiskalyAccount`, `fiskalyReceipt`, `fiskalyHelper` + 5 service `fiskaly*` + `fiskaly.helper` | Alta (obbligo di legge) | Molto alta |
| C5 | **Auth & utenti** | `auth` (1.044), `user` (2.494), `password`, `checkEmail`, `subscribe`, `license`, `sysadmin` | Alta | Media |
| C6 | **Comunicazione & marketing** | `email.helper` (2.800), 14 `mailtemplates`, `customTemplate`, `emailConfig`, `newsletter`, `notification`, `marketing*` (4 controller) | Media | Media |
| C7 | **Centralino (switchboard/PBX)** | `switchboard` (3.030) + `switchboardService` + `sip.helper`, `telnyx.helper`, `whatsapp.helper` | Bassa/marginale | Alta (telefonia real-time) |
| INFRA | **Supporto** | `uploadFile`, `printerConfig`, `country`, `configuration`, `ofsale`, `teamsystem`, cron, middleware | Trasversale | Bassa/Media |

---

## 5. Opzione A — Riscrittura totale in .NET (dettaglio per componente)

Modalità raccomandata: **phased big-bang** — riscrittura in **.NET 10 (Web API) + EF Core** a fette
verticali dietro nuovi contratti API, con nuovo schema relazionale e migrazione dati unica nella
finestra morta. Non "from scratch alla cieca".

Il passaggio a .NET è coerente con gli obiettivi: la **tipizzazione statica di C#** intercetta a
compile-time interi classi di errori oggi silenziosi in JavaScript, ed **EF Core** rende naturale il
modello relazionale che sostituisce le colonne JSON. La mappatura tra i due stack è diretta:
Express → .NET 10 (Web API), Sequelize → EF Core, middleware → middleware, `jsonwebtoken` → auth JWT nativa.
Costo del cambio linguaggio: **nessun codice riusabile** (ogni riga è net-new).

### 5.1 Interventi trasversali (prerequisiti, valgono per tutte le componenti)

| Intervento | Descrizione | Vantaggio | Rischio | Stima |
|---|---|---|---|---|
| Rete di sicurezza (parallel run manuale) | I 2 tester eseguono lo stesso caso su vecchio e nuovo sistema in parallelo e ne confrontano l'output | Parte subito senza setup tecnico; compensa l'assenza di test unitari | Non ripetibile automaticamente: copre il momento della migrazione ma non protegge dalle regressioni dopo lo switch; qualità legata alla copertura dei tester | **0 sul percorso di sviluppo** — corre in parallelo, a carico dei 2 tester |
| Nuovo schema dati relazionale (EF Core) | Modellare le ~120 colonne JSON come entità/relazioni EF Core, rimodellare alcune tabelle con vincoli e indici | Query efficienti, integrità, validazione a livello di tipo | Errori di modellazione difficili da correggere dopo | 3-4 settimane |
| Script di migrazione JSON→relazionale | Migrazione idempotente, testata in dual-run sul DB reale | 1 solo tenant: gestibile e reversibile | Perdita/corruzione dati | 3-4 settimane |
| Architettura target .NET | .NET 10 (Web API): controller sottili + service layer + repository (EF Core) + DTO; tipizzazione statica C# | Manutenibilità, testabilità, errori a compile-time | Over-engineering | 1-2 settimane (setup) + continuo |
| Nuovi contratti API | Consolidare le chiamate multiple in endpoint aggregati | –N round-trip per schermata (misurabile) | Coordinamento col frontend | Continuo, per fetta |

### 5.2 Dettaglio per dominio

**C1 — Prenotazioni (core)**
- *Vantaggi*: elimina il god object da 12.775 LOC; regole di prenotazione esplicite e testabili;
  dati prenotazione relazionali (oggi 19 colonne JSON nella tabella `booking`).
- *Rischi*: **massimi** — qui si concentrano le regole non documentate; ogni edge case perso (casi limiti persi) è un
  errore di prenotazione reale.
- *Mitigazione*: prima fetta a essere coperta dal parallel run manuale; riscrittura per ultima tra le "core",
  quando l'infrastruttura è rodata.
- *Stima*: **6-8 settimane-dev**.

**C2 — Catalogo & configurazione**
- *Vantaggi*: normalizza listini/settori/servizi (oggi duplicati e in JSON); base per prestazioni ricerca.
- *Rischi*: medi — molte entità, ma logica meno insidiosa del core.
- *Stima*: **4-5 settimane-dev**.

**C3 — Pagamenti & fiscale**
- *Vantaggi*: isola Stripe dietro un'interfaccia C# (SDK Stripe.net ufficiale); tracciamento transazioni relazionale (EF Core); riconciliazione affidabile.
- *Rischi*: **alti** — soldi reali; webhook idempotenti; conciliazione con Fiskaly.
- *Mitigazione*: isolare per primo, ambiente di test Stripe, parallel run sugli importi (confronto vecchio/nuovo).
- *Stima*: **4-6 settimane-dev**.

**C4 — Fiskaly (scontrino fiscale)**
- *Vantaggi*: già parzialmente in service; consolidare in un modulo pulito e testato.
- *Rischi*: **alti** — obbligo di legge italiano; errori = sanzioni. Contratto esterno complesso.
- *Mitigazione*: non reinventare, portare la logica esistente in modulo isolato con test di contratto.
- *Stima*: **3-4 settimane-dev**.

**C5 — Auth & utenti**
- *Vantaggi*: autenticazione JWT nativa di .NET 10; RBAC (Role-Based Access Control, controllo
  degli accessi basato sui ruoli) centralizzato con policy/ruoli; smontare il
  monolite di rotte (oggi 375 endpoint in `user.routes.js`).
- *Rischi*: medi — sicurezza-critico ma pattern standard e ben supportati in .NET.
- *Stima*: **3-4 settimane-dev**.

**C6 — Comunicazione & marketing**
- *Vantaggi*: estrarre i template (10k LOC) in un motore di templating; disaccoppiare invio da logica.
- *Rischi*: medi — tante varianti di email, ma non transazionali.
- *Stima*: **3-4 settimane-dev**.

**C7 — Centralino (switchboard)**
- *Vantaggi*: sottosistema telefonia/IVR isolabile.
- *Rischi*: Bassi tecnicamente.
- *Decisione consigliata*: riscrivere alla fine (in perimetro, rientra nella finestra dei 7 mesi).
- *Stima*: **4-6 settimane-dev**.

**INFRA — Supporto**
- *Vantaggi*: upload/print/config/cron standardizzati; middleware di caching coerente.
- *Rischi*: bassi.
- *Stima*: **2-3 settimane-dev**.

### 5.3 Riepilogo tempi Opzione A

| Blocco | Stima (settimane-dev) |
|---|---|
| Prerequisiti trasversali (schema + migrazione + setup) — *rete di sicurezza esclusa: parallela* | 7-10 |
| C1 Prenotazioni | 6-8 |
| C2 Catalogo | 4-5 |
| C3 Pagamenti | 4-6 |
| C4 Fiskaly | 3-4 |
| C5 Auth/utenti | 3-4 |
| C6 Comunicazione | 3-4 |
| C7 Switchboard | 4-6 |
| INFRA | 2-3 |
| **Totale (tutto il perimetro incluso)** | **36-50 settimane-dev** |

Con 2-3 dev su ~7 mesi (≈ 30 settimane di calendario → **60-90 settimane-dev disponibili**, al netto del
coordinamento tester), le **36-50 settimane-dev necessarie rientrano nella finestra con tutto
il perimetro incluso** (C7 e coda lunga compresi). La rete di sicurezza non pesa sul percorso di
sviluppo perché corre in parallelo a carico dei tester.

> **Margine non conteggiato nelle stime.** Le settimane-dev sopra sono espresse **senza il supporto
> dell'IA**: con l'IA (traduzione Node→C#, generazione di codice ripetitivo standard, ecc..) i
> tempi reali sono attesi più bassi — l'IA è un margine di sicurezza, non il caso base. Contro-effetto da
> tenere in conto: il porting cross-linguaggio non riusa codice (ogni riga è net-new).

### 5.4 Rischi complessivi Opzione A

| Rischio | Probabilità | Impatto | Mitigazione |
|---|---|---|---|
| Regole di business perse | Alta | Alto | Parallel run manuale durante la riscrittura |
| Migrazione dati errata | Media | Alto ma **reversibile** (1 tenant + backup) | Script idempotente + dual-run + rollback |
| Sforamento tempi (nessun riuso codice) | Alta | Medio | Fette verticali rilasciabili; AI per accelerare il porting |
| Sottostima integrazioni esterne | Media | Alto | Isolare Stripe/Fiskaly per primi |
| Coordinamento frontend | Media | Medio | Versioning contratti, rilascio a fette |

---

## 6. Opzione B — Migliorie parziali su Node (dettaglio per componente)

Interventi incrementali sul codice Node esistente, sempre rilasciabili, **senza cambiare stack né
modello dati**.

### 6.1 Manutenibilità mirata

| Intervento | Vantaggio | Rischio | Stima |
|---|---|---|---|
| Estrarre i template email dal codice (10k LOC) | –10k LOC di rumore; template modificabili senza deploy | Basso | 1-2 settimane |
| Spezzare `booking.controller.js` in moduli | File <1.000 LOC; navigabilità | Medio (nessun test) | 2-3 settimane |
| Estendere il service layer ai 5-6 controller peggiori | Coerenza, testabilità parziale | Basso | 2-3 settimane |

### 6.2 Prestazioni mirate

| Intervento | Vantaggio | Rischio | Stima |
|---|---|---|---|
| Indici sulle query più lente (dove possibile senza cambiare schema) | –X ms su lista/ricerca | JSON resta non indicizzabile: tetto basso | 1 settimana |
| Caching (Redis già presente) sulle viste calde | Meno carico DB | Invalidazione cache | 1-2 settimane |
| Consolidare 2-3 chiamate multiple in endpoint aggregati | –round-trip su viste critiche | Coordinamento frontend | 1-2 settimane |

### 6.3 Riepilogo tempi Opzione B

Risultati **in settimane, non mesi**: sicurezza + performance critiche + primi refactor in **~6-8
settimane**. Tetto strutturale basso: **non** risolve dati-in-JSON, duplicazioni, round-trip multipli
sistematici.

### 6.4 Rischi complessivi Opzione B

| Rischio | Probabilità | Impatto |
|---|---|---|
| Causa radice (dati JSON) resta → limiti prestazionali strutturali intatti | Certa | Alto a lungo termine |
| Migliorie erose dal debito sottostante | Alta | Medio |
| "Ci torniamo dopo" mai realizzato → si paga due volte | Alta | Medio |
| Crescita a **500 stabilimenti** (da 200-300): carico multi-tenant e volume dati crescono; le viste che filtrano/aggregano su JSON degradano (vedi §6.5) | Media | **Alto** |

### 6.5 Scalabilità (scenario: crescita a 500 stabilimenti)

> **Nota:** "500 clienti" = **500 stabilimenti**, non 500 clienti finali. È quindi uno scenario di
> **scaling multi-tenant**, molto più impegnativo del semplice aumento di utenti finali.

Passare da ~200-300 a ~500 stabilimenti aumenta di circa **+67-150% tutto il carico**: operatori alla
cassa in alta stagione, prenotazioni online dei clienti di *ogni* stabilimento e, soprattutto, il
**volume dati** (ogni stabilimento ha proprie configurazioni, ombrelloni, prenotazioni, clienti nello
stesso DB condiviso).

Tre pressioni simultanee sul modello attuale (Node + dati JSON):

| Pressione | Effetto su B |
|---|---|
| **Concorrenza** in alta stagione su centinaia di stabilimenti | I CRUD reggono, ma i picchi si concentrano tutti nello stesso periodo stagionale |
| **Volume dati** in un DB multi-tenant condiviso | Liste / ricerche / report che filtrano **dentro** i JSON → full scan + `JSON.parse`; costo **più che lineare** col numero di stabilimenti |
| **Isolamento per stabilimento** | Il modello attuale non è pensato per partizionare/indicizzare per stabilimento in modo efficiente |

Le migliorie di B sono **palliativi con tetto basso**:
- **Indici** → solo dove il campo è una colonna vera, non dentro un JSON.
- **Caching Redis** → aiuta le letture ripetute, **non** le query che filtrano/ordinano su JSON né il write-path.
- **Aggregazione endpoint** → riduce i round-trip, non il costo unitario della query.

**Verdetto:** a 500 stabilimenti **B non è una risposta strutturale**. È proprio lo scenario in cui il
modello relazionale di **A** (indici, query per-tenant efficienti) diventa necessario, e che si collega
all'evoluzione verso un'architettura **multi-tenant / modulare**
(vedi [architettura-evolutiva-microservizi.md](architettura-evolutiva-microservizi.md)). La crescita nel
**numero di stabilimenti** è un driver molto più forte della crescita di clienti finali: più è
probabile, più B è un investimento a perdere.

### 6.6 Scaling verticale e orizzontale (Opzione B)

Domanda pratica: si può reggere la crescita **aggiungendo hardware** invece di riscrivere? In parte sì,
ma con tetto basso, perché il collo di bottiglia è il **DB unico + modello JSON**, non la potenza bruta.

**Scaling verticale (server più potente: CPU/RAM/disco)**
- *App (Node)*: un processo Node usa **1 core**; per sfruttare più core servono più worker
  (`cluster`/PM2) → è di fatto già uno scale-out interno. Beneficio del singolo processo limitato.
- *DB (MySQL)*: la leva più efficace nel breve — più RAM (buffer pool), CPU, NVMe. Facile, **senza
  modifiche al codice**.
- *Limite*: **tetto hardware** (istanza massima) e costo non lineare; e le query che filtrano **dentro**
  i JSON restano full scan → paghi di più per eseguire query inefficienti. **Rimanda il muro, non lo toglie.**

**Scaling orizzontale (più istanze)**
- *App tier*: si possono mettere più istanze Node dietro load balancer, ma servono interventi:
  **Socket.io** richiede adapter Redis (o sticky session) per il multi-istanza; i **cron** (`node-cron`)
  devono girare una sola volta (leader election), non su ogni istanza; nessuno stato in memoria. Fattibile, non gratis.
- *DB — letture*: **read replica** MySQL per scaricare report/liste, ma serve **read/write splitting nel
  codice** (oggi tutto va sul primary) e tollerare la **replication lag**. Aiuta, ma le query JSON restano costose su ogni replica.
- *DB — scritture*: qui il muro. Scalare le scritture richiede **sharding** (partizionare i dati su più
  DB); con **un tenant unico, schema denormalizzato, JSON e join incrociati non è shardabile** senza la
  riscrittura relazionale. E la crescita è proprio +stabilimenti nello **stesso** DB → più scritture concorrenti sullo stesso primary.

| Asse | Fattibile su B? | Limite |
|---|---|---|
| Verticale — app | Parziale (worker/PM2) | 1 processo = 1 core; oltre è scale-out |
| Verticale — DB | ✅ Sì, subito | Tetto hardware; query JSON restano full scan |
| Orizzontale — app | ✅ Con lavoro | Socket.io→adapter Redis, cron single-run, no stato in-memory |
| Orizzontale — DB (letture) | ⚠️ Con modifiche | Read replica + read/write split nel codice + lag |
| Orizzontale — DB (scritture) | ❌ Bloccato | Schema JSON/join non shardabile senza la riscrittura (A) |

**Conclusione:** su B lo scaling **verticale del DB** è la leva reale a breve (compra tempo) e lo scaling
**orizzontale dell'app** è fattibile con qualche intervento; ma la crescita a 500 stabilimenti è
soprattutto **scritture + query cross-stabilimento su un DB unico**, ed è esattamente ciò che B **non
può** scalare (niente sharding sul modello JSON). Lì serve **A**: il modello relazionale abilita indici,
read replica efficaci e, in prospettiva, **partizionamento/sharding per stabilimento**.

---

## 7. Confronto sintetico

| Criterio | A — Totale | B — Parziale |
|---|---|---|
| Risolve il modello dati (causa radice) | ✅ Sì | ❌ No |
| Manutenibilità (target file <500 LOC, service layer) | ✅ Piena | ⚠️ Parziale |
| Prestazioni (dati indicizzabili, meno round-trip) | ✅ Alto tetto | ⚠️ Tetto basso |
| Sicurezza | ✅ EF Core (query parametrizzate), stack .NET LTS, tipizzazione statica | ⚠️ Stack Node invariato: superficie resta |
| Stack / linguaggio | .NET 10 (Web API) + EF Core, C# | Node/Express/Sequelize (invariato) |
| Rischio di regressione | Alto (mitigabile con parallel run manuale) | Basso |
| Time-to-value | Mesi | Settimane |
| Fattibilità nel contesto (1 tenant, front proprio, 7 mesi) | **Alta** | Alta |
| Costo se si sbaglia | Reversibile (1 tenant + backup) | Basso, ma si spende due volte |

---

## 8. Raccomandazione e piano a fasi

**Raccomandazione: Opzione A in versione "phased big-bang".** Le condizioni al contorno (tenant
singolo, frontend proprio, 7 mesi, 2 tester) rendono questa la finestra giusta, che potrebbe non
ripresentarsi. B da sola lascia intatta la causa radice.

### Timeline proposta (finestra ~7 mesi, inizio agosto → fine febbraio)

| Fase | Contenuto | Durata | Chi |
|---|---|---|---|
| **0 — Setup .NET** | Skeleton **.NET 10 (Web API)**, scelte architetturali, EF Core, CI/CD | Settimana 0-2 | 1-2 dev |
| **1 — Rete di sicurezza** | Parallel run manuale su C1/C3/C4 (prenotazioni, pagamenti, fiskaly): definizione dei casi di test - creare un TestBook - e confronto vecchio vs nuovo (in parallelo alle altre fasi) | Settimana 1-fine | 2 tester |
| **2 — Dati** | Nuovo schema relazionale EF Core + script migrazione, dual-run sul DB reale | Settimana 3-9 (parziale overlap) | 1-2 dev |
| **3 — Riscrittura a fette (.NET)** | Ordine: Auth (C5) → Catalogo (C2) → Pagamenti (C3) → Fiskaly (C4) → Prenotazioni (C1) → Comunicazione (C6) → Switchboard (C7) → Infra | Settimana 8-28 | 2-3 dev |
| **4 — Hardening & buffer** | Rifiniture, migrazione finale, collaudo end-to-end, margine per imprevisti | Settimana 28-30 | 2-3 dev |

Ogni fetta della Fase 3 è **rilasciabile** e concordata col frontend tramite nuovi contratti API,
così da evitare un unico salto big-bang finale. **Tutto il perimetro (C7 incluso) rientra nella
finestra**; le stime non conteggiano il supporto dell'IA né il porting già iniziato a mano, che restano
margine.

### Criteri di successo (KPI misurabili)

| Metrica | Oggi | Target |
|---|---|---|
| Max LOC per file | 12.775 | < 500 |
| `JSON.parse` su dati di dominio | 633 | ≈ 0 (dati tipizzati con EF Core) |
| Query SQL raw non parametrizzate | 281 | ≈ 0 (query EF Core parametrizzate) |
| Colonne JSON di dominio | ~120 | solo dove semanticamente corretto |
| Copertura flussi critici | 0% | parallel run manuale su C1/C3/C4 + unit test sui nuovi moduli C# |
| Round-trip API per schermata critica | (da misurare) | ridotto e documentato |
| Stack / runtime | Node legacy, deps con CVE | .NET LTS supportato |

---

## 9. Condizioni di fattibilità (Opzione A)

La fattibilità stimata nei capitoli precedenti **non è incondizionata**: regge solo se valgono *tutti* i
punti seguenti. Sono condizioni **cumulative** — se ne viene a mancare anche una sola, le stime di §5.3 e
la timeline di §8 vanno riviste.

| # | Condizione | Perché è necessaria | Se disattesa |
|---|---|---|---|
| 1 | **Competenza .NET già in casa** — entrambi gli sviluppatori backend sono già produttivi in C#/.NET/EF Core (preferibile poterne avere un terzo) | Le stime non includono tempo di apprendimento | Ramp-up non previsto → slittamento diretto della timeline |
| 2 | **Dedizione quasi esclusiva** — i due sviluppatori backend lavorano quasi solo su questo refactoring | Le 36-50 settimane-dev presuppongono capacità piena e continuativa | Il lavoro si diluisce: la finestra stagionale non basta più |
| 3 | **TestBook preparato in anticipo** — i tester scrivono i casi *prima* che il dominio corrispondente sia completato, per poter testare appena la fetta è pronta | Il parallel run pesa 0 sul percorso di sviluppo **solo** se corre davvero in parallelo | I dev restano in attesa di validazione: il parallelismo si perde |
| 4 | **Nessuna nuova feature, nessun cambio di regole al volo** — si riproduce il comportamento esistente; ciò che non è documentato non si cambia. Niente *"servirebbe che queste percentuali fossero impostabili dal pannello admin"*, niente *"dovrebbe essere possibile dividere l'ombrellone e affittarlo a un amico"* | Il perimetro è "stesso comportamento, architettura nuova": ogni aggiunta è scope creep su un perimetro già pieno | Scope creep → la riscrittura non chiude nella finestra |
| 5 | **Uso dell'IA come strumento di lavoro** — porting Node→C#, generazione di codice ripetitivo e di casi di test | Le stime di §5.3 sono conservative (espresse *senza* IA): l'IA è il margine che assorbe gli imprevisti | Si lavora senza margine: ogni imprevisto va dritto a slittamento |
| 6 | **Referente di dominio raggiungibile** — in caso di dubbio sulla logica di business ci dev'essere qualcuno disponibile a rispondere | Le regole non sono documentate: senza un referente si procede a intuito | Regole ricostruite male → bug funzionali, di incasso o fiscali |
| 7 | **Frontend fattibile negli stessi tempi** — l'adeguamento ai nuovi contratti API deve procedere in parallelo | Un backend nuovo senza il frontend corrispondente non è rilasciabile | Le fette non sono realmente rilasciabili: il rilascio slitta comunque |

> In sintesi: le condizioni 1-2 garantiscono la **capacità**, la 3 il **parallelismo del collaudo**, la 4
> la **stabilità del perimetro**, la 5 il **margine**, la 6 la **correttezza funzionale**, la 7 la
> **rilasciabilità**. Sono i presupposti su cui poggia il verdetto di fattibilità di §1.

---

## 10. Glossario

| Termine | Significato |
|---|---|
| **LOC** (*Lines Of Code*) | Righe di codice. Metrica grezza di dimensione. |
| **CVE** (*Common Vulnerabilities and Exposures*) | Identificativo pubblico di una vulnerabilità di sicurezza nota (es. `CVE-2023-45857`). "Dipendenza con CVE" = libreria con falle già pubblicate. |
| **RBAC** (*Role-Based Access Control*) | Controllo degli accessi basato sui ruoli: cosa può fare un utente dipende dal suo ruolo (admin, operatore, cliente…), non da permessi sparsi. |
| **.NET / C#** | Piattaforma e linguaggio Microsoft (open source, multipiattaforma) scelti come target della riscrittura. Linguaggio a **tipizzazione statica**: molti errori emergono a compile-time invece che a runtime. |
| **.NET 10 (Web API)** | Versione LTS della piattaforma .NET usata per esporre le API REST del backend, equivalente di Express in ambito Node. Gestisce routing, middleware, autenticazione, DI. |
| **EF Core** (*Entity Framework Core*) | ORM di .NET, equivalente di Sequelize. Mappa classi C# a tabelle relazionali; genera query parametrizzate; gestisce migrazioni di schema. |
| **Replatform / porting cross-linguaggio** | Riscrivere un sistema su uno stack/linguaggio diverso (qui Node→.NET). Comporta che **nessun codice esistente viene riusato**: ogni riga è net-new. |
| **Big-bang** | Sostituzione del vecchio sistema col nuovo in un unico passaggio (opposto dello *strangler-fig*, dove i due convivono a lungo). |
| **Phased / fette verticali** | Riscrivere un dominio completo alla volta (dal DB all'API, funzionante e rilasciabile), invece di tutto insieme. Spezza il salto unico in tanti piccoli. |
| **Strangler-fig** | Pattern di migrazione graduale: il nuovo sistema "avvolge" e sostituisce il vecchio pezzo per pezzo. Qui **non** applicabile perché il modello dati cambia sotto. |
| **Parallel run / shadow testing** | Rete di sicurezza anti-regressione: lo stesso input viene dato a vecchio e nuovo sistema in parallelo e i tester ne confrontano l'output a mano. Parte subito senza setup, ma non è ripetibile automaticamente. |
| **Migrazione dual-run** | Lo script di conversione dati (JSON→relazionale) viene eseguito in parallelo mentre il vecchio sistema gira ancora, per confrontare i risultati e validare la conversione *prima* dello switch. |
| **Runway** | "Pista di decollo" (gergo startup): quanto tempo continuativo hai davanti prima di doverti fermare. Qui = i ~7 mesi di stagione morta (inizio agosto → fine febbraio) disponibili per lavoro strutturale. |
| **Edge case** (*caso limite*) | Situazione rara o estrema che il codice deve gestire ma è facile dimenticare (es. prenotazione a cavallo di due stagioni, rimborso parziale su scontrino fiscale già emesso, cliente senza email). È dove si nascondono i bug. |
| **God object** | Un singolo file/classe che fa troppo e concentra troppa logica (qui `booking.controller.js`, 12.775 LOC). |
| **Service layer / repository / DTO** | Livelli architetturali: *service* = logica di dominio; *repository* = accesso ai dati; *DTO* (Data Transfer Object) = struttura dati scambiata con l'esterno. Separarli rende il codice testabile e manutenibile. |
| **Round-trip (API)** | Un giro completo richiesta→risposta tra frontend e backend. Meno round-trip per schermata = meno latenza. |
| **Idempotente** | Operazione che, eseguita più volte con lo stesso input, produce lo stesso risultato senza effetti collaterali (fondamentale per script di migrazione e webhook di pagamento). |
| **Fiskaly** | Servizio esterno per la generazione degli scontrini fiscali elettronici (obbligo di legge in Italia). |
