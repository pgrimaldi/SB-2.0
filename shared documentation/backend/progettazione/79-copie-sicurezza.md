# Copie di sicurezza esterne — altra AZ, altro server in Europa, server locale

> **Sviluppo futuro.** Progetto di **copie dei dati fuori dal perimetro primario**: in un'altra
> Availability Zone / un altro account AWS, su un server in un'altra regione europea (AWS o fornitore
> terzo), o su un **server locale creato ad hoc**. Copre **dati in database** e **file**, e risponde
> alle due domande lasciate aperte dalla richiesta: *si copiano anche i dati archiviati?* (§6) e
> *si applica prima la retention?* (§7).
>
> **Non sostituisce [40-backup.md](40-backup.md)**, lo estende: quello descrive il backup gestito
> *dentro* l'account AWS (snapshot RDS, PITR, versioning S3, DR cross-region); questo descrive le copie
> che devono restare leggibili **anche se l'account AWS non c'è più o non è più affidabile** (§0).
>
> Si aggancia a: [80-retention.md](80-retention.md) e [82-retention-data.md](82-retention-data.md)
> (cosa si tiene, per quanto, e cosa significa 📦 *archiviazione* — fonte autorevole dei tempi),
> [11-modulo-privacy.md](11-modulo-privacy.md) (PII per tabella, art. 17, legal hold),
> [17-secrets.md](17-secrets.md) (dove stanno le chiavi, e perché non stanno nella copia),
> [03-gestione-dati.md §1](03-gestione-dati.md) (come sono indirizzati i file: `origin` + chiave
> relativa, non URL cablati), [20-jobs.md](20-jobs.md) (i job di copia sono job come gli altri),
> [31-observability.md](31-observability.md) (metrica di freschezza e alert),
> [54-database-di-test.md](54-database-di-test.md) (la pipeline di sanificazione serve anche qui, §7.4),
> [34-multitenant.md](34-multitenant.md) (perché "copia di sistema" ≠ "export dei dati di un tenant", §5),
> [../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) (regione `eu-south-1`, Multi-AZ, S3).
>
> **Stato: proposta tecnica, nessuna decisione presa.** I valori numerici (cadenze, finestre, costi)
> sono punti di partenza per la discussione, non parametri validati. Le scelte da chiudere prima di
> implementare sono in §12.
>
> **Documento vivo**: da aggiornare quando si sceglie la destinazione L2 (§12 A) e quando si misurano i
> volumi reali (§10).

---

## 0. Perché un documento separato da `40-backup.md`

Backup e copia esterna rispondono a due domande diverse, e confonderle è il modo classico di scoprire
troppo tardi che si aveva un solo asse di difesa:

| | **Backup** ([40-backup.md](40-backup.md)) | **Copia di sicurezza esterna** (questo documento) |
|---|---|---|
| **Domanda a cui risponde** | "Come torno indietro di un'ora / di un giorno?" | "Che cosa mi resta se perdo l'accesso all'intero ambiente?" |
| **Dove vive** | Nello stesso account AWS, gestita dal provider | In un altro account / un'altra regione / fuori AWS / in sede |
| **Da cosa protegge** | Guasto hardware, errore su una tabella, migration sbagliata | Account compromesso o chiuso, errore di configurazione globale, contenzioso o uscita dal fornitore, disastro regionale |
| **Formato** | Snapshot proprietario del motore (ripristinabile solo su RDS) | Formato aperto, ripristinabile su un Postgres qualsiasi |
| **Chi la ripristina** | Chi ha accesso alla console AWS | Chiunque abbia la copia e la chiave, anche senza AWS |
| **Tipico fallimento** | Nessuno la testa (→ drill §3 di 40-backup) | Nessuno la riporta a terra per anni, e la chiave si perde (→ §8) |

### 0.1 Principi guida

1. **Una copia che nessuno sa ripristinare non è una copia.** Ogni destinazione ha il proprio drill
   (§11), con tempi misurati. Vale a maggior ragione per la copia locale, che tipicamente nessuno
   guarda per mesi.
2. **Una copia è un trattamento di dati, non un file.** Ogni nuova destinazione va nel registro dei
   trattamenti con base giuridica, ruolo (titolare/responsabile), luogo fisico e misure di sicurezza.
   Moltiplicare le copie moltiplica la superficie di attacco e gli obblighi: si aggiungono destinazioni
   perché risolvono un rischio dichiarato, non per abbondanza.
3. **La copia non prolunga la retention e non la aggira.** Vale lo stesso principio di
   [80-retention.md §0](80-retention.md): un dato conservato in una copia non soddisfa un obbligo di
   conservazione applicativo, e non sfugge a un obbligo di cancellazione. Da qui tutto il §7.
4. **Formato aperto, non snapshot proprietario.** Una copia utile si legge senza il software che l'ha
   prodotta: `pg_dump` (formato `custom`/`directory`) + i file **così come sono** (stessa chiave
   relativa che usa il DB, §3.2) + un **manifest** verificabile (§3.4). Uno snapshot RDS in un altro
   account è comodo ma resta dipendente da AWS.
5. **Il target tira, il primario non spinge.** Il server della copia ha credenziali di **sola lettura**
   verso il primario; il primario **non** ha credenziali di scrittura o cancellazione sul target. Chi
   compromette la produzione non deve poter cancellare anche le copie.
6. **Cifratura con chiave nostra, custodita altrove.** Una copia cifrata con una chiave che vive
   accanto alla copia è una copia in chiaro (§8).
7. **Nessun segreto nella copia.** Credenziali, chiavi API, segreti SIP/Stripe/SMTP stanno in Secrets
   Manager ([17-secrets.md](17-secrets.md)) e **non** vengono copiati: una copia rubata non deve
   diventare un accesso ai sistemi vivi (§2, riga segreti).
8. **Una copia senza manifest verificato è un'ipotesi.** Conteggi per tabella, checksum per file,
   versione dello schema: senza, non si sa se si è copiato tutto o metà (§3.4).

---

## 1. I tre livelli di destinazione

| Livello | Dove | Cosa protegge **in più** rispetto al livello precedente | Cosa **non** protegge | RPO / RTO realistici |
|---|---|---|---|---|
| **L0 — già previsto** | `eu-south-1`, RDS Multi-AZ + PITR, S3 versionato | guasto di istanza, guasto di AZ, overwrite accidentale di un oggetto | perdita/compromissione dell'**account**, errore globale di configurazione, disastro regionale | RPO ≈ 5 min / RTO 2-4 h |
| **L1 — altra AZ / altro account, stessa regione** | seconda AZ (già implicita in Multi-AZ) e, soprattutto, **secondo account AWS** della stessa organizzazione | errore umano e compromissione dell'account di produzione (il secondo account ha credenziali e permessi distinti) | disastro di regione, blocco dell'intera organizzazione AWS, lock-in | RPO 24 h / RTO 4-8 h |
| **L2 — altro server in Europa** | altra regione AWS (`eu-west-1`, `eu-central-1`) **oppure** fornitore terzo UE (VPS/dedicato) | indisponibilità regionale; con fornitore terzo anche l'indipendenza dal provider (exit strategy, contenzioso, fatturazione sospesa) | distruzione fisica contemporanea (improbabile); non protegge da un dato già sbagliato alla fonte | RPO 24 h-7 gg / RTO 1-2 gg |
| **L3 — server locale ad hoc** | macchina in sede, sotto il controllo diretto del titolare | perdita totale di ogni fornitore cloud; dà la copia "in mano", ispezionabile senza intermediari | furto/incendio/guasto locale, obsolescenza, nessuno che la controlla; introduce **nuovi** rischi (§4) | RPO 7 gg / RTO giorni |

> ### ⚠️ "Altra Availability Zone" da sola aggiunge poco
>
> Va detto apertamente, perché è il livello che sembra più ovvio ed è il meno utile: in `eu-south-1`
> **RDS Multi-AZ tiene già uno standby sincrono in un'altra AZ** e **S3 replica già ogni oggetto su più
> AZ per progetto** ([../infra/infrastruttura-aws.md §3.2](../../../../../infra/infrastruttura-aws.md),
> [40-backup.md §1](40-backup.md)). Una copia "in un'altra AZ" **non** aggiunge quindi protezione dal
> guasto: aggiunge qualcosa solo se cambia **chi può cancellarla**, cioè se finisce in un **account AWS
> separato** con permessi propri. L'asse utile a questo livello è l'**isolamento di accesso**, non la
> distanza geografica.
>
> **Conseguenza**: L1 si implementa come *copia in un secondo account* (bucket dedicato, bucket policy
> che consente la sola scrittura in append dal primario e la lettura dal target), non come "istanza in
> AZ-b". Il valore geografico vero comincia da **L2**.

### 1.1 Combinazione consigliata

Non sono alternative: è una scala che si costruisce in ordine di valore per costo.

| Fase | Destinazioni attive | Razionale |
|---|---|---|
| Oggi | L0 | copre la gran parte degli incidenti reali |
| Passo 1 | L0 + **L1** | il salto più grande per il costo più basso: niente banda in uscita, solo un secondo account |
| Passo 2 | L0 + L1 + **L2** | prima destinazione che sopravvive alla regione e (con fornitore terzo) al fornitore |
| Passo 3 | + **L3** | solo se il titolare vuole la copia materialmente in sede; va trattato come il livello **più costoso in termini di responsabilità**, non come il più economico |

---

## 2. Perimetro — che cosa si copia

| Categoria | Dove vive oggi | In copia? | Note |
|---|---|---|---|
| **DB — schemi applicativi** (`identity`, `catalog`, `booking`, `payment`, `fiskaly`, `notification`, `switchboard`, `privacy`, …) | RDS PostgreSQL | ✅ sempre | È il nucleo. Un dump per schema o unico, vedi §3.1 |
| **DB — storia delle migration** (`__ef_migrations_history`) | RDS | ✅ sempre | Senza, non si sa a quale versione di codice corrisponde la copia (§3.4) |
| **DB — partizioni fredde / tabelle di archivio** | RDS | ✅ se presenti | Vanno incluse esplicitamente: un dump per schema le prende solo se sono ancora nello schema |
| **File — media stabilimenti** (foto, immagini mappa) | S3 | ✅ | È il volume dominante e cresce; è anche ciò che **non** si rigenera |
| **File — PDF fiscali / scontrini** | S3 | ✅ priorità massima | Obbligo decennale ([80-retention.md §2](80-retention.md)). Valgono più di tutto il resto |
| **File — documenti identità (`reg_*`), allegati** | S3 | ✅ con cautela | PII sensibile: copia **sempre cifrata**, destinazione L3 solo se §4 è soddisfatto |
| **File — audio centralino, template** | S3 | ✅ | Volume modesto |
| **File — export/report generati** | S3 | ❌ | Rigenerabili dai dati; copiare output derivati è peso inutile |
| **Dati archiviati** (📦 di [80-retention.md §3/§5](80-retention.md): snapshot di configurazione degli stabilimenti cessati, transazioni e scontrini oltre la finestra online) | S3 / Glacier | ⚠️ **sì, ma con regime diverso** | Vedi **§6** |
| **Audit fiscale/contabile** (tabella DB dedicata, [80-retention.md §3.9](80-retention.md)) | RDS | ✅ | È dentro il dump; va verificato che nessuna policy lo poti prima del termine fiscale (§7) |
| **Log diagnostici applicativi** | CloudWatch → S3 | ❌ | Non sono dati di business: hanno già la loro archiviazione ([15-logs.md](15-logs.md)) |
| **Segreti** (Secrets Manager / SSM) | Secrets Manager | ❌ **mai nella copia dati** | Serve una procedura separata di custodia (§8.3): una busta sigillata, non un file dentro il dump |
| **Redis / cache** | ElastiCache | ❌ | Ricostruibile ([16-redis.md](16-redis.md)) |
| **IaC** (`infra/summerbooking-aws.yaml`) | Git | ✅ già fatto | Git **è** già una copia esterna e distribuita; va solo verificato che esista un remote fuori dal provider principale |
| **Working set offline del gestionale** (IndexedDB) | Browser | ❌ | Copia volatile lato client ([19-frontend-offline.md](19-frontend-offline.md)) |

> **Nota sui file legacy**: oggi i media stanno su bucket in **`us-east-1` / `us-east-2`**
> ([03-gestione-dati.md §1](03-gestione-dati.md)). Qualunque piano di copie deve partire dal
> presupposto che i file siano già stati consolidati in UE: **una copia non deve reintrodurre una
> destinazione extra-UE**, e replicare oggi quei bucket significherebbe moltiplicare un problema di
> residenza dati invece di risolverlo. L'ordine corretto è: prima il consolidamento in `eu-south-1`,
> poi le copie.

---

## 3. Meccanismi

### 3.1 Database

| Opzione | Come | Pro | Contro | Dove conviene |
|---|---|---|---|---|
| **A — copia dello snapshot RDS** | `CopySnapshot` cross-account / cross-region (già citata in [40-backup.md §1](40-backup.md)) | zero codice, nessuna banda a nostro carico, cifratura KMS nativa | **ripristinabile solo su RDS**: non soddisfa il principio 0.1.4; inutile per L2-terzi e per L3 | L1; L2 se resta dentro AWS |
| **B — dump logico cifrato** | `pg_dump -Fd -j N` (o `-Fc`) da una replica di lettura, compressione, cifratura, deposito nel bucket di copia; il target **tira** | formato aperto, ripristinabile ovunque, selettivo per schema, riusa il `pg_dump` settimanale già previsto | RPO = cadenza (ore/giorni); su DB grandi il dump costa tempo e I/O | **la base per L1, L2 e L3** |
| **C — replica logica Postgres** | `rds.logical_replication = 1`, `PUBLICATION` sul primario, `SUBSCRIPTION` su un Postgres esterno | RPO di minuti, nessuna finestra di dump | richiede PK su tutte le tabelle replicate; **il DDL non si replica** (ogni migration va riapplicata a mano sul target); le **tabelle partizionate** richiedono `publish_via_partition_root` e attenzione a ogni nuova partizione; il target va tenuto vivo e aggiornato; carico aggiuntivo sul primario | solo se nasce un requisito di RPO < 1 h, **non** come scelta iniziale |

**Raccomandazione**: **B** per tutte e tre le destinazioni. È l'unica che soddisfa "ripristinabile senza
AWS" e l'unica che si può realisticamente verificare. **A** come comodità aggiuntiva su L1 (costa quasi
nulla e abbrevia il ripristino nel caso più probabile). **C** è un sistema distribuito da mantenere: con
11 moduli, partizionamento attivo e migration frequenti, il costo operativo è alto e il beneficio (RPO)
non è il rischio che questo documento affronta.

**Dettagli operativi di B**:

- Dump da **read replica**, non dal primario, per non competere con il traffico applicativo.
- `pg_dump` lavora su uno **snapshot transazionale**: il dump è coerente a un istante preciso anche se
  dura a lungo. Quell'istante va nel manifest (§3.4).
- Un file per schema (`-n identity`, `-n booking`, …) invece di un dump unico: la copia diventa
  ispezionabile e il ripristino selettivo, coerentemente con "uno schema per modulo"
  ([../infra/infrastruttura-aws.md §3.2](../../../../../infra/infrastruttura-aws.md)). Attenzione agli oggetti
  globali (ruoli, estensioni, tipi `ENUM` nativi — questi ultimi pesano, vedi il template dei moduli):
  servono anche `pg_dumpall --globals-only` e la verifica che ogni enum sia nel dump dello schema che
  lo possiede.
- Le **partizioni fredde** eventualmente staccate dalla tabella padre vanno elencate esplicitamente.

### 3.2 File

| Destinazione | Meccanismo | Note |
|---|---|---|
| L1 (altro account, stessa regione) | **S3 Replication (SRR)** cross-account + **Batch Replication** per il pregresso | Richiede il versioning (già previsto). Tenere **disattivata** la replica dei delete marker: una cancellazione in produzione **non** deve propagarsi alla copia |
| L2 AWS (altra regione) | **S3 Replication (CRR)** | Stesso impianto; si paga il trasferimento inter-regione |
| L2 terzi / L3 locale | `aws s3 sync` o `rclone` **in pull dal target**, senza `--delete` | Il costo dominante è l'**egress** (§10). Le cancellazioni legittime si gestiscono con la potatura programmata del target (§7), mai con un `--delete` cieco |

I file si copiano **con la stessa chiave relativa** che il DB conserva ([03-gestione-dati.md
§1](03-gestione-dati.md): colonna `origin` + percorso relativo, non URL completi). È ciò che rende la
copia ricomponibile: si ripristina il DB, si punta `Storage:Origins` al percorso locale e
l'applicazione trova i file senza riscrivere una riga di dati. È un beneficio concreto e già pagato di
quella scelta di disegno.

### 3.3 Coerenza fra DB e file

Il DB referenzia chiavi di file: una copia "a metà" produce record che puntano a file assenti.

- **Ordine: prima i file, poi il DB.** Copiando prima il DB si ottengono di sicuro riferimenti pendenti
  (i file caricati fra i due momenti); copiando prima i file si ottengono al più file non ancora
  referenziati, che sono innocui.
- La copia è quindi coerente così: **DB all'istante T**, **file ≥ T**. Il manifest registra entrambi.
- Nessun tentativo di "congelare" il sistema durante la copia: non serve e sarebbe un disservizio.
- I riferimenti pendenti residui (file cancellati fra una fase e l'altra) si **rilevano** nel drill con
  un controllo a campione, non si prevengono.

### 3.4 Manifest e verifica

Ogni esecuzione produce un **manifest** accanto ai dati. È ciò che distingue una copia da una cartella
di file:

```jsonc
{
  "copyId": "2026-10-01T02:15:00Z",
  "destination": "L2-eu-west-1",
  "database": {
    "snapshotAt": "2026-10-01T02:15:04Z",            // istante coerente del pg_dump
    "schemaVersions": { "booking": "20260921074959_SeedRetentionPolicies", "…": "…" },
    "rowCounts": { "booking.reservations": 412233, "fiskaly.receipts": 88104 },
    "files": [ { "name": "booking.dump", "bytes": 0, "sha256": "…" } ]
  },
  "objects": { "prefix": "media/", "count": 0, "bytes": 0, "throughTime": "2026-10-01T02:40:00Z" },
  "retention": { "applied": false, "policySetHash": "…" },   // vedi §7
  "verify": { "checksums": "ok", "rowCountDelta": "entro soglia", "at": "2026-10-01T03:05:00Z" }
}
```

- **Conteggi riga per tabella**: l'unico modo di accorgersi che un dump è "riuscito" esportando metà
  delle righe. Un delta anomalo rispetto alla copia precedente è un alert (§9).
- **Versione dello schema**: senza `__ef_migrations_history`, fra un anno non si saprà quale versione di
  codice sa leggere quella copia.
- **Checksum ricalcolati sul target**, non sulla sorgente: è l'unica verifica che dica qualcosa sul
  trasferimento.
- **`policySetHash`**: l'impronta dell'insieme di policy di retention in vigore al momento della copia
  (§7.5). Serve a sapere, anni dopo, *che cosa* c'è dentro quella copia.

---

## 4. Il server locale ad hoc (L3)

È il livello che il titolare percepisce come il più sicuro ("i dati ce li ho qui") ed è tecnicamente il
più delicato: sposta dati personali fuori da un ambiente gestito e li mette in un luogo fisico.

| Requisito | Perché |
|---|---|
| **Cifratura dell'intero disco** (LUKS / BitLocker) **più** cifratura della copia a livello di file (§8) | Un server rubato o un disco dismesso con dati in chiaro è una **violazione notificabile** (art. 33 GDPR). Con copia cifrata e chiave altrove, resta un furto di hardware |
| **Nessuna porta in ingresso**: la macchina esce verso AWS, non viceversa | Modello pull (principio 0.1.5). Un server locale esposto in ingresso diventa il punto più debole dell'intera infrastruttura |
| **Credenziali dedicate di sola lettura** (utente IAM limitato ai prefissi di copia, utente Postgres read-only), ruotate e revocabili | Le credenziali di produzione non devono mai risiedere su una macchina fisicamente accessibile a terzi |
| **Aggiornamenti e patch**, con un responsabile nominato | Una macchina dimenticata per tre anni è un rischio, non una copia |
| **Ruolo privacy chiarito**: di chi è la macchina, chi è titolare e chi responsabile | Se è del titolare dello stabilimento, chi ci scrive sopra è responsabile del trattamento e serve l'accordo relativo (§12 B) |
| **Nessuna applicazione punta a questo DB** | Non è un ambiente di failover: un secondo ambiente "vivo" e non sincronizzato produrrebbe dati divergenti |
| **Drill annuale documentato** (ripristino + apertura dell'applicazione contro la copia) | È l'unica prova che quella macchina stia facendo qualcosa |
| **Piano di fine vita del disco** (sostituzione, cancellazione sicura, dismissione) | Una copia senza piano di dismissione sopravvive agli obblighi di retention (§7.6) |

> **Che cosa L3 non è**: non è l'**hub offline** di [19-frontend-offline.md](19-frontend-offline.md).
> Quello è un working set operativo di una giornata, pensato per **lavorare** senza rete, con replay dei
> comandi verso il server; questo è un deposito **freddo, di sola lettura, mai usato in esercizio**. I
> due progetti non condividono né dati né meccanismi, e accorparli creerebbe una seconda fonte di
> verità — esattamente ciò che il §0 di quel documento esclude.

---

## 5. Copia di sistema ≠ export dei dati di un tenant

Due richieste che si assomigliano e vanno tenute separate:

- **Copia di sicurezza** (questo documento): tutto il sistema, destinata al ripristino, mai consegnata a
  terzi, formato tecnico.
- **Export dei dati di uno stabilimento** ("voglio i miei dati"): è **portabilità** (art. 20 GDPR) o
  uscita contrattuale. Perimetro di un solo tenant ([34-multitenant.md](34-multitenant.md)), formato
  leggibile dal cliente, verifica di identità, tracciata nel registro delle richieste
  ([11-modulo-privacy.md §5](11-modulo-privacy.md)).

Usare la copia di sicurezza per soddisfare una richiesta di portabilità significa consegnare i dati di
tutti gli stabilimenti: è un errore da evitare per costruzione, non da gestire con attenzione.

---

## 6. Domanda 1 — si copiano anche i dati **archiviati**?

**Di che dati parliamo**: ciò che [80-retention.md](80-retention.md) marca 📦 — spostato su storage
freddo e rimosso dal DB operativo. Oggi: snapshot di configurazione degli stabilimenti non più attivi
(§5 di quel documento), transazioni e spese POS oltre 24 mesi, metadati e PDF degli scontrini oltre 36
mesi, dettaglio prenotazioni oltre N−2 — cioè esattamente ciò che le API segnalano come fuori finestra
([`analisi-post/bl/02`](../analisi-post/bl/02-errore-api-dati-archiviati.md)).

| A favore della copia | Contro |
|---|---|
| Sono i dati con l'**obbligo più lungo** (10 anni, art. 2220 c.c.): perderli è il danno più grave | Volume maggiore e in crescita monotòna; su Glacier il **recupero costa e richiede ore**, quindi anche solo leggerli per copiarli ha un prezzo |
| Sono **freddi**: dopo il primo pieno, l'incrementale è quasi gratis | L'archivio è già su S3, con durabilità e multi-AZ native: la copia extra protegge da **cancellazione e compromissione**, non da guasto |
| Una volta potato il DB caldo, l'archivio è **l'unica copia** di quel dato — e il DB caldo ha PITR, l'archivio no | Ogni destinazione in più va potata a 10 anni: più copie, più punti dove dimostrare di aver cancellato (§7.6) |
| Un archivio corrotto o cancellato **non dà segnale**: ci si accorge solo quando serve, cioè durante una verifica fiscale | Un **Object Lock** sulla copia la rende incancellabile anche per noi: protegge e al tempo stesso impedisce la potatura (vedi §6.1) |

### 6.1 Raccomandazione

**Sì, ma con un regime proprio.** Caldo e freddo non si copiano con la stessa cadenza né verso le stesse
destinazioni:

| | **Dati caldi** (DB operativo + file correnti) | **Dati archiviati** (storage freddo) |
|---|---|---|
| Cadenza | giornaliera (L1) / settimanale (L2, L3) | **mensile o trimestrale** — cambiano solo quando passa un job di archiviazione |
| Destinazione | L1 + L2 (+ L3 se adottato) | **L2**; su L3 solo l'**indice**, non il contenuto |
| Formato | dump + file | copia oggetto per oggetto, invariata |
| Vita della copia | breve, a rotazione (§7.2) | **lunga**, allineata all'obbligo residuo |
| Immutabilità | no (ruota) | **sì** (Object Lock / WORM), con durata = **obbligo residuo**, mai "per sempre" |

**Due accorgimenti che fanno la differenza:**

1. **L'indice si copia più spesso del contenuto.** Un catalogo degli oggetti archiviati (che cosa, quale
   periodo, quale stabilimento, dove si trova, checksum, quando scade) è piccolo, va replicato ovunque
   compreso L3, e permette di rispondere a "quel dato esiste ancora?" senza toccare Glacier. Senza
   indice, un archivio di anni diventa illeggibile anche quando è perfettamente integro.
2. **Object Lock con durata pari all'obbligo residuo.** In modalità compliance l'oggetto non si cancella
   prima della scadenza **nemmeno da amministratore**: è esattamente ciò che si vuole contro ransomware e
   cancellazioni accidentali, ed è esattamente ciò che impedisce di ottemperare se quel dato un giorno va
   rimosso. La durata va calcolata **per oggetto** come "10 anni meno l'età del documento", non impostata
   su un valore fisso al momento della copia.

---

## 7. Domanda 2 — si applica la retention **prima** di copiare?

È la domanda più importante del documento, e la risposta non è "sì" o "no": **dipende da quanto vive la
copia.**

### 7.1 Non è un ordine di operazioni: sono due copie con finalità diverse

| | **Copia fedele** (immagine) | **Copia di conservazione** |
|---|---|---|
| A cosa serve | ripristinare il sistema com'era | tenere il dato per anni |
| Deve contenere | **anche** ciò che la retention sta per cancellare — altrimenti un ripristino "perde" dati che al momento dell'incidente c'erano | **solo** ciò che va effettivamente conservato |
| Retention applicata prima? | **No** | **Sì** |
| Vita | breve, a rotazione (7-30 giorni) | anni |
| Destinazioni tipiche | L1, L2 quotidiana/settimanale | archivio su L2, copia L3 |

### 7.2 Il criterio operativo

> **La retention va applicata alla copia in proporzione a quanto la copia vive.**
>
> `vita_della_copia ≤ finestra_di_propagazione` → si può copiare il dato **grezzo**: la cancellazione
> avvenuta in produzione si propaga da sé, perché alla rotazione successiva la copia che conteneva quel
> dato semplicemente non esiste più.
>
> `vita_della_copia > finestra_di_propagazione` → la copia **deve** essere già minimizzata, oppure deve
> essere **potabile** (le cancellazioni si riapplicano anche sul target).

La *finestra di propagazione* è il ritardo massimo accettabile fra "cancellato in produzione" e "sparito
da ogni copia". **Proposta: 30 giorni** — coerente con la retention degli export logici di
[40-backup.md §1](40-backup.md) (30-90 giorni) e difendibile come tempo tecnico. Va dichiarata
nell'informativa e nel registro dei trattamenti: è un impegno verso gli interessati, non un dettaglio
implementativo (§12 D).

### 7.3 Tabella decisionale

| Copia | Vita | Retention applicata prima? | Perché |
|---|---|---|---|
| L1 giornaliera (altro account) | 30 gg a rotazione | **No** | È un'immagine di ripristino; la rotazione propaga le cancellazioni |
| L2 settimanale (altra regione / terzi) | 30-90 gg a rotazione | **No**, ma rotazione **tassativa e verificata** | Idem; se la rotazione si inceppa, la copia diventa silenziosamente un archivio non dichiarato → alert dedicato (§9) |
| Copia dell'**archivio** (§6) | anni | **Già applicata a monte** | Ciò che è stato archiviato ha *già* attraversato la retention: la copia non deve rifiltrare nulla, deve copiare l'archivio com'è |
| **Copia L3 locale** | mesi/anni, nei fatti | **Sì** | È il caso in cui "copio tutto e poi si vedrà" diventa un deposito di PII fuori controllo. Si copia un dataset **già minimizzato** |
| Copia consegnata a terzi (consulente, perizia) | fuori dal nostro controllo | **Sì, sempre**, più pseudonimizzazione | Esce dal perimetro: vale la pipeline di [54-database-di-test.md](54-database-di-test.md) |

### 7.4 Come si produce una copia "già minimizzata" (e perché non si improvvisa)

Applicare la retention prima di copiare **non** significa filtrare il `pg_dump`: le regole sono **44
policy** su schemi diversi, con azioni diverse (🗑️ hard delete, 🔒 anonimizzazione, 📦 archiviazione) e
decorrenze diverse ([80-retention.md §6](80-retention.md),
[`privacy_retention_policies`](../analisi-post/db/privacy/privacy_retention_policies.md)). Riscriverle
come clausole di export significa avere **due implementazioni della stessa politica**, che divergeranno.

La via corretta riusa ciò che è già previsto:

1. Ripristino del dump in un **ambiente di staging isolato** — stessa meccanica del restore drill
   ([40-backup.md §3](40-backup.md)).
2. Esecuzione dei **job di retention reali** (non dry-run) su quell'ambiente: l'implementazione delle
   regole resta una sola, quella di produzione.
3. Opzionale, per copie destinate a uscire dal perimetro: passaggio di **sanificazione/pseudonimizzazione**
   già descritto in [54-database-di-test.md](54-database-di-test.md).
4. **Secondo dump** dell'ambiente potato → è questa la copia di conservazione.
5. Il manifest registra `retention.applied = true` e il `policySetHash` usato (§3.4).

Costo: un ciclo di restore in più. Beneficio: nessuna seconda implementazione delle regole, e una copia
di cui si sa dire con esattezza che cosa contiene.

### 7.5 Il cortocircuito da evitare

[80-retention.md §0.2](80-retention.md) impone: **export verificato prima di ogni cancellazione**. Quindi
retention e copie sono **già** incatenate: l'archivio prodotto dalla retention **è** il contenuto della
copia fredda. La copia di sicurezza non deve ri-archiviare per conto proprio ciò che la retention ha già
archiviato — deve **copiare l'archivio** (§6). Due impianti di archiviazione paralleli sarebbero due
verità sullo stesso dato, con due scadenze diverse.

### 7.6 Cancellazioni su richiesta e legal hold

Il punto più facile da dimenticare: una richiesta art. 17 accolta cancella dal DB caldo, non dalle copie.

- Le copie a rotazione breve (§7.3) si sistemano da sole entro la finestra di propagazione: va
  **dichiarato** all'interessato, non taciuto.
- Le copie di lunga vita richiedono un **registro delle cancellazioni** (chi, quando, quali
  identificativi, quale base) da **rigiocare sul target** alla rotazione successiva, con esito
  verificato. Il registro esiste già come perimetro in
  [11-modulo-privacy.md §5](11-modulo-privacy.md) e
  [`privacy_compliance_audit_logs`](../analisi-post/db/privacy/privacy_compliance_audit_logs.md): va
  esteso alle destinazioni di copia.
- I **legal hold** (`privacy_legal_holds`) hanno effetto opposto e prevalgono: un dato sotto hold **non**
  si cancella, nemmeno dalle copie, finché l'hold è attivo.
- Gli oggetti sotto **Object Lock** non sono cancellabili prima della scadenza: la richiesta risulta
  "accolta parzialmente", con motivazione e data, esattamente come per i documenti fiscali
  ([80-retention.md §8](80-retention.md)). È una conseguenza da accettare consapevolmente **prima** di
  attivare il lock, non da scoprire alla prima richiesta.

---

## 8. Cifratura, chiavi, accessi

### 8.1 Cifratura

- Cifratura **a livello di copia**, non solo del volume di destinazione: la copia resta illeggibile anche
  per chi amministra il server che la ospita.
- Chiave **nostra**, non del provider che ospita la copia: diversamente, cambiare fornitore o perdere
  l'accesso all'account rende la copia inutile — cioè realizza il rischio da cui ci si voleva difendere.
- Su L1/L2-AWS: chiave KMS multi-region dedicata alle copie, distinta da quelle di produzione.
- Su L2-terzi/L3: cifratura a chiave asimmetrica, così il processo di copia può **scrivere senza poter
  rileggere** (la chiave privata non vive sulla macchina che copia).

### 8.2 Accessi

- Utente Postgres **read-only** dedicato al dump; ruolo IAM dedicato limitato ai prefissi di copia;
  credenziali **distinte per destinazione** (una compromessa non apre le altre).
- Nessuna credenziale di produzione sul target; nessuna credenziale di scrittura sul target dal lato
  produzione (principio 0.1.5).

### 8.3 Custodia della chiave

La perdita della chiave è il modo più comune di perdere tutte le copie insieme.

- Chiave principale in **Secrets Manager** ([17-secrets.md](17-secrets.md)), versionata.
- **Copia della chiave fuori da AWS**, sigillata, con procedura di apertura scritta e almeno due persone
  che sanno dov'è.
- La **prova di decifratura** fa parte del drill: non si verifica solo che il file si scarichi, ma che si
  apra (§11).
- La rotazione della chiave è compatibile con le copie vecchie solo se le chiavi precedenti restano
  custodite: non si butta una chiave finché esiste una copia cifrata con essa.
- **I pepper delle password** (`Auth:PasswordHash:Pepper`/`Peppers`) seguono la stessa regola, ed è
  l'eccezione al principio 7: non stanno nella copia, ma senza di loro un DB ripristinato rifiuta il
  login di tutti gli utenti già passati ad Argon2 (successo su sb-dev il 2026-10-19). Copia fuori da AWS
  insieme alla chiave delle copie, e nessun pepper si elimina finché un hash o una copia lo usano.

---

## 9. Monitoraggio

Stesso impianto dei job esistenti ([20-jobs.md](20-jobs.md),
[31-observability.md §6.2](31-observability.md)): ogni esecuzione emette metriche di esito, e la mancata
esecuzione è essa stessa un allarme.

| Metrica | Allarme quando | Perché conta |
|---|---|---|
| `copy_age_hours` per destinazione | supera il doppio della cadenza attesa | È **la** metrica: una copia vecchia è il fallimento silenzioso tipico |
| `copy_bytes`, `copy_rows` per tabella | delta anomalo rispetto all'esecuzione precedente | Intercetta il dump parziale andato "a buon fine" |
| `verify_result` (checksum + conteggi, ricalcolati sul target) | diverso da ok | Un trasferimento riuscito non è ancora una copia valida |
| `rotation_overdue` | una copia a rotazione breve supera la finestra di propagazione (§7.2) | Una copia che non ruota diventa un archivio non dichiarato: è un problema di conformità, non solo tecnico |
| `drill_last_success_days` | supera la cadenza del drill | Misura l'unica cosa che conta davvero: la ripristinabilità |

Una pagina a semaforo con le destinazioni attive, l'età dell'ultima copia e l'esito dell'ultimo drill vale
più di qualunque report: chi guarda deve capire in tre secondi se la rete di sicurezza esiste.

---

## 10. Costi — le voci che dominano

> Ordini di grandezza, **non** un preventivo. Stime da rifare sui volumi reali, con la stessa riserva di
> [../infra/infrastruttura-aws.md §4](../../../../../infra/infrastruttura-aws.md) (±20%).

| Voce | Dove pesa | Nota |
|---|---|---|
| **Egress AWS** (uscita verso fornitore terzo o verso la sede) | **L2-terzi, L3** | Voce dominante, nell'ordine di ~0,09 USD/GB oltre la quota gratuita (da verificare per `eu-south-1`). È il motivo per cui una copia completa settimanale "verso casa" costa più di una incrementale |
| Trasferimento inter-regione | L2-AWS | Più economico dell'egress verso Internet |
| Trasferimento nello stesso luogo | L1 | Quasi nullo: è il vantaggio costo/beneficio di L1 |
| Storage al target | tutte | Su fornitore terzo e in sede è il capitolo meno caro; su Glacier pesa il **recupero**, non il deposito |
| **Recupero da Glacier** per copiare l'archivio | §6 | Si paga per GB recuperato e si aspetta: è la ragione della cadenza mensile/trimestrale |
| Hardware e gestione L3 | L3 | Macchina, disco, aggiornamenti e soprattutto il **tempo di qualcuno** |
| Drill | tutte | L'unica voce che non si può tagliare senza annullare il valore di tutto il resto |

**Dimensionamento di partenza, da misurare**: il dump completo del legacy occupa ~12 GB di SQL testuale
non compresso (`dump-prod-legacy/`, prod 2026-09-08); compresso, l'ordine di grandezza del DB resta di
pochi GB — **il DB non è il problema**. Il volume vero sono i **media**, oggi ancora su bucket US (§2) e
**mai misurati**: finché non si misurano, qualunque stima di costo per L2/L3 è campata in aria. **Prima
la misura, poi la scelta della destinazione** (§12 A).

---

## 11. Roadmap e checklist

| Fase | Contenuto | Esito atteso |
|---|---|---|
| **F0 — misura** | volume di DB e file per classe, tasso di crescita, inventario dei bucket (compresi quelli US) | i numeri che oggi mancano per decidere (§10) |
| **F1 — manifest e verifica sull'esistente** | manifest + conteggi + checksum sugli export `pg_dump` già previsti da [40-backup.md](40-backup.md) | si sa, per la prima volta, se le copie attuali sono complete. Costo quasi nullo |
| **F2 — L1 (secondo account)** | bucket nel secondo account, replica S3 cross-account, deposito dei dump, ruoli minimi, pull dal target | difesa da errore umano e compromissione dell'account |
| **F3 — L2 (Europa)** | scelta della destinazione (§12 A), copia settimanale cifrata, rotazione verificata, drill | sopravvivenza alla regione e, se fornitore terzo, al fornitore |
| **F4 — archivio freddo** | copia mensile/trimestrale dell'archivio + indice replicato, Object Lock a durata residua (§6) | protezione del dato decennale |
| **F5 — L3 (locale)** | solo dopo F3: pipeline di minimizzazione (§7.4), macchina conforme al §4, drill annuale | copia in sede, senza creare un deposito di PII fuori controllo |

**Checklist per ogni nuova destinazione** (nessuna si considera attiva finché non sono tutte spuntate):

- [ ] Scopo dichiarato: da quale rischio difende, che gli altri livelli non coprono
- [ ] Registro dei trattamenti aggiornato; se fornitore terzo, accordo di responsabile firmato; destinazione **UE**
- [ ] Perimetro dei dati deciso (§2) e, se la copia è di lunga vita, pipeline di minimizzazione (§7.4)
- [ ] Modello pull, credenziali dedicate di sola lettura, nessuna porta in ingresso
- [ ] Cifratura attiva e **chiave custodita altrove**, con procedura di apertura scritta
- [ ] Manifest prodotto e **verificato sul target**
- [ ] Rotazione configurata e monitorata (`rotation_overdue`)
- [ ] Metriche e alert attivi (§9)
- [ ] **Drill eseguito almeno una volta**, con tempi misurati e verbale
- [ ] Piano di dismissione: come si cancella questa destinazione, e chi lo fa

---

## 12. Decisioni aperte (chiudere prima di implementare)

- **A — Destinazione L2: altra regione AWS o fornitore terzo UE?** AWS costa meno in trasferimento e
  riusa gli strumenti esistenti, ma non protegge dalla perdita dell'organizzazione AWS né dal lock-in;
  un fornitore terzo copre entrambi, al prezzo dell'egress e di un secondo impianto da gestire. **È il
  bivio principale** e dipende da quale rischio si vuole davvero coprire. Prerequisito: F0 (§10).
- **B — Il server locale: di chi è e chi lo gestisce?** Macchina del titolare dello stabilimento (chi ci
  scrive sopra diventa responsabile del trattamento, serve l'accordo) o del fornitore presso la sede del
  cliente? Da chiudere **prima** di comprare hardware: determina responsabilità, obblighi di notifica in
  caso di furto e chi fa gli aggiornamenti (§4).
- **C — RPO target della copia esterna.** Giornaliero o settimanale? Determina cadenza, costo di egress e
  se l'opzione C del §3.1 debba mai essere riconsiderata.
- **D — Finestra di propagazione delle cancellazioni.** Proposta **30 giorni** (§7.2). È un impegno da
  dichiarare nell'informativa: da confermare con il consulente privacy, non da decidere in sede tecnica.
- **E — Copia dei dati archiviati.** La raccomandazione del §6.1 (sì, cadenza bassa, su L2, con indice
  replicato ovunque) è da confermare sul **budget**, una volta noti i volumi e il costo di recupero.
- **F — Pipeline di minimizzazione.** Confermare il riuso di
  [54-database-di-test.md](54-database-di-test.md) (restore → job di retention reali → secondo dump,
  §7.4) invece di regole di export dedicate.
- **G — Object Lock sui fiscali copiati.** Durata calcolata per oggetto come obbligo residuo (§6.1) e
  accettazione esplicita che quegli oggetti non saranno cancellabili prima della scadenza.
- **H — Responsabile e cadenza dei drill**, per destinazione. Senza un nome, il drill non si fa.

---

## 13. Cosa **non** si copia, e perché

| Elemento | Perché no |
|---|---|
| **Segreti e credenziali** | Non devono esistere dentro una copia dati: custodia separata e sigillata (§8.3). Una copia rubata non deve diventare un accesso ai sistemi vivi |
| **Dati di carta (PAN/CVV)** | Non esistono nel nuovo schema e non vanno migrati ([80-retention.md §2](80-retention.md)): non c'è nulla da copiare, e non va reintrodotto nulla |
| Redis / cache | Ricostruibile ([16-redis.md](16-redis.md)) |
| Log diagnostici applicativi | Hanno già archiviazione propria ([15-logs.md](15-logs.md)); non sono dati di business |
| Export e report generati | Derivati rigenerabili dai dati |
| Immagini container e artefatti di build | Rigenerabili dalla pipeline CI dal sorgente versionato |
| Working set offline (IndexedDB) | Copia volatile lato client ([19-frontend-offline.md](19-frontend-offline.md)) |
| Snapshot RDS come **unica** copia esterna | Ripristinabile solo su RDS: non soddisfa lo scopo del documento (§0.1.4). Ammesso come copia *aggiuntiva* su L1 |
| Una copia **senza manifest** | Non è una copia: è una cartella di file di cui nessuno sa dire se è completa (§3.4) |
