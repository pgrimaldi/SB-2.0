# Cosa resta da sistemare

> **Stato al 2026-10-06.** Parti A e B: tutte le voci chiuse e applicate (corsa del 2026-10-01,
> poi corsa 9 del 2026-10-02: **6.163.412 righe, 0 errori**). Parte C: tutto applicato tranne le
> due eccezioni dichiarate (M15 punto 2, M18 corsa completa). Le **colonne** rimaste vuote dopo
> la migrazione, con il perché di ognuna e le domande Q1–Q14, stanno accanto a questo file in
> [`_colonne-vuote.md`](_colonne-vuote.md).

> Inventario del lavoro aperto sulla migrazione e sullo schema, al **2026-09-30**. Ogni voce dice
> **cos'è**, un **esempio concreto** preso dai dati reali, la **soluzione proposta** e **perché
> quella e non un'altra**.
>
> Il documento è diviso in due liste, perché sono due tipi di lavoro diversi:
>
> | | |
> |---|---|
> | **[Parte A](#parte-a--serve-una-decisione-o-uninformazione) — A1…A7** | serve una decisione o un'informazione che non sta nei dati: chi conosce gli stabilimenti, il business, o un legale. Senza quella risposta qualsiasi scelta sarebbe un'invenzione. |
> | **[Parte B](#parte-b--si-chiudono-senza-alcuna-informazione-in-più) — B1…B15** | si chiudono **senza chiedere niente a nessuno**: sono solo lavoro. La soluzione è già scritta e verificata sui dati; manca l'esecuzione. |
>
> **Stato della Parte A al 2026-09-30, dopo le risposte del committente.** Restano aperte **tre
> domande in tutto**, evidenziate qui sotto:
>
> | | Stato |
> |---|---|
> | **A1** | ✅ **chiusa**: niente "cessato", decorrenza = fine ultimo abbonamento **+ 24 mesi**; per la blacklist **basta `updated_at`** |
> | **A2** | ✅ **chiusa**: si resta in sola prova; legge verificata e **nessuna autorizzazione AGCOM** → l'art. 132 non ci riguarda, i 6 mesi del verbale restano |
> | **A3** | ✅ **chiusa**: prezzi, vince il giorno singolo; rivendita, **il vincolo era sulla colonna sbagliata** — con `booking_line_id` le sovrapposizioni sono **zero**, nessun posto venduto due volte |
> | **A4** | ✅ nessuna tracciatura dei permessi → si converte in array |
> | **A5** | ✅ **niente da installare**: su AWS il pooler è già nel template (RDS Proxy). Ma l'app non ci arriva: vedi **[B9](#b9-in-aws-lapp-non-troverebbe-la-propria-connection-string)** |
> | **A6** | ✅ **chiusa**: `consumer` agli 11, i 5 di prova fuori, e i 3 con l'indirizzo in `username` **si recuperano** |
> | **A7** | ✅ **chiusa e applicata**: i settori si creano dai nomi citati nei listini, visibili — **48 creati, 263 prezzi rientrati**. I due documenti di verifica non servono più. I 140 settori legacy non citati restano: reggono 4.040 ombrelloni |
>
> **Tutti i punti A sono chiusi**, ma due tornano brevemente al mittente perché nel verificarle è
> cambiata la premessa:
>
> 1. **I 3 account «senza email» hanno l'email nella colonna `username`** (Claudia è
>    `claudia197421@gmail.com`): il login legacy accetta entrambe le colonne. Ignorarli era stato
>    deciso sulla mia descrizione sbagliata — si recuperano con una riga di codice. Da riconfermare.
>    Vedi **[A6](#a6-i-20-account-applicativi-non-migrati--chiusa)**.
> 2. **I settori legacy non citati non si possono togliere**: sono 140 e reggono 4.040 ombrelloni.
>    Vedi **[A7](#a7-i-nomi-dei-settori-che-non-coincidono--chiusa-si-creano-dai-listini)**.
>
> Più una scelta di prodotto minore, emersa dalla prima: **l'accesso con username breve va
> conservato?** 384 utenti lo usano, tutti con un'email valida, quindi nessuno resta chiuso fuori.
>
> Nessuna delle tre blocca le modifiche di Parte B.
>
> ## ✅ Applicato il 2026-10-01 — zero errori
>
> Le modifiche di Parte B sono state **eseguite**, il database è stato ricreato dalle migration
> e la migrazione rifatta da zero. Risultato, a confronto con la corsa precedente:
>
> | | Prima | Dopo |
> |---|---:|---:|
> | Righe lette | 6.718.354 | 6.720.890 |
> | Righe migrate | 5.766.075 | **5.766.406** |
> | **Righe in errore** | **306** | **0** |
> | Verdetto «da correggere» nel report | 299 | **0** |
>
> Sono servite **tre corse**: la prima ha rivelato due difetti nelle modifiche stesse (la spec
> nuova saltata, la fusione delle finestre che non vedeva le righe non ancora committate), la
> seconda è scesa a 10 errori e ha scoperto una spec rimasta orfana, la terza è a zero. Le tre
> correzioni sono raccontate nelle rispettive voci.
>
> **Cosa è rientrato nel database:**
>
> | | |
> |---|---:|
> | Settori creati dai nomi citati nei listini | **+48** (635 → 683) |
> | Prezzi per settore recuperati | **+263** (3.158 → 3.421) |
> | Settori di sconto e limiti di permanenza | +1 e +1 |
> | Utenti recuperati dall'indirizzo in `username` | **+3** |
> | Periodi tariffari ridondanti collassati | −12 (580 → 568) |
> | Finestre di prenotabilità fuse | −7 (68 → 61) |
> | Vincoli `EXCLUDE` attivi | **6** (erano 3) |
>
> Restano due domande **minori e indipendenti**: l'accesso con username breve (deciso: non si
> conserva) e l'indirizzo Apple, che è diventato la nota **B15** nel codice del backend.
>
> Stato di partenza: l'ultima corsa completa ha letto **6.718.354 righe** su 129 tabelle, ne ha
> migrate **5.766.075** e scartate 4.629.414, con **306 errori**.
>
> 🔍 **Riverificato il 2026-09-30:** ogni numero di questo documento è stato ricalcolato sui dati
> reali — **105 affermazioni, tutte corrette** dopo aver sistemato cinque errori (fra cui due
> dovuti a query sbagliate mie, non a dati cambiati). La verifica è rieseguibile con
> `utility/01-migrazione-test/scripts_verifica_mancanti.py` e `…2.py`: se un numero si scosta,
> stampa atteso e reale. Il dettaglio riga per riga è in [`_indice.md`](_indice.md);
> qui c'è ciò che quel report **non** copre o non può risolvere da solo.

---

## Chiuse mentre scrivevo questo documento

Quattro voci erano nell'elenco come "da indagare". Andandole a guardare nei dati si sono chiuse, e
**tre delle quattro erano diagnosi sbagliate del report**, non problemi reali. Restano qui perché la
correzione è già applicata e perché il motivo per cui il report mentiva è la voce **B2**.

| Era scritto | Com'è andata davvero |
|---|---|
| *«5 nomi di settore percorsi carattere per carattere: difetto di lettura dell'ETL»* | **Dato sporco del legacy, l'ETL è corretto.** La riga `discounts` id 17 (stabilimento 98, voucher `tea`) contiene alla lettera `"selectedRow": ["F","i","l","a"," ","1","Fila 2"]`. Qualcuno, anni fa, ha salvato `Fila 1` lettera per lettera dall'interfaccia. L'esplosione annidata ha letto esattamente ciò che c'era. |
| *«39 codici sconto non risolti, effetto non tracciato»* | **Comportamento previsto.** Le righe ci sono tutte: `booking.discounts` ne ha **269 su 1.795** con `discount_id` NULL e il codice conservato in `code_snapshot` (`MARINELOVERS` 149 volte). Sono sconti applicati a mano, non a catalogo, e il transform lo dichiara nella propria docstring. Cercavo l'effetto nelle tabelle sbagliate (`catalog.pricing_discount_*` invece di `booking.discounts`). |
| *«la prenotazione 309575 non è stata migrata»* | **È stata migrata** (legacy 309575 → `booking.reservations` 233342, stabilimento *Bagno Romanza*, attivo, 906 prenotazioni). I due link di pagamento falliscono per un altro motivo: hanno `booking_id` **NULL** e `stripe_session_id` vuoto — non hanno mai avuto né prenotazione né sessione. |
| *«1.010 iscrizioni newsletter senza email»* | **Hanno tutte l'email.** Sono **email duplicate**: il legacy non aveva unicità su `subscribes.email` e la stessa arriva a ripetersi 15 volte. Il transform tiene l'`id` più alto. 1.010 doppioni collassati, nessun indirizzo perso: verdetto corretto **voluto**, non "da decidere". |

Il conteggio dei verdetti si è spostato di conseguenza: **voluto 14.862 · da decidere 37 · da
correggere 299** (prima 13.852 / 1.047 / 299).

---
---

# Parte A — serve una decisione o un'informazione

Sette voci, nate tutte dal fatto che i dati non bastavano a deciderle. **Dopo le risposte del
2026-09-30 sono tutte chiuse**, come riassume la tabella in testa al documento; l'unica scelta
minore rimasta — l'accesso con username breve — è stata decisa (non si conserva).

Le voci risolte restano scritte per intero, con la decisione in cima: servono a chi fra sei mesi si
chiederà perché lo schema è fatto così, e in due casi la risposta ha aperto un lavoro nuovo che
prima non c'era.

---

## A1. Tre colonne che mancano per *eseguire* la retention — D-65 — chiusa

> **Deciso il 2026-09-30:** **il concetto di "cessato" non esiste** — uno stabilimento può solo
> essere non attivo perché non ha rinnovato l'abbonamento — **ma la retention va applicata comunque**.
> Quindi `terminated`/`terminated_at` **non si aggiungono**, e la cascata deve ricavare la propria
> data di riferimento da un dato che già esiste. Vedi
> [la conseguenza](#la-conseguenza-la-cessazione-diventa-derivata-non-dichiarata) qui sotto.
>
> La seconda colonna cade con la risposta ad **[A4](#a4-due-conversioni-in-array-rimaste-condizionate--risolta)**
> («no» alla tracciatura di chi dà o toglie un permesso): se quella traccia non serve, non serve
> nemmeno una data di revoca dedicata, e `deleted_at` resta la sola data, com'è oggi.
>
> **Anche la terza è chiusa**: per la revisione annuale della blacklist **basta `updated_at`**
> (deciso il 2026-09-30). Il promemoria non distinguerà «ho riconfermato la blacklist» da «ho
> corretto il telefono», e va bene così: è un promemoria, non una prova.

**Cos'è.** La politica di retention è validata e seminata, ma tre regole non hanno una colonna su cui
poggiare.

**Esempio concreto del danno.** La cascata §5 deve far decorrere la cancellazione dalla **cessazione**
dello stabilimento. Oggi `catalog.properties` non distingue "cessato" da "sospeso per abbonamento
scaduto": entrambi si presentano come non attivi. Conseguenza pratica: uno stabilimento chiuso per
sempre e uno che non ha pagato il rinnovo sono indistinguibili, quindi **al primo pagamento si
riattiverebbe anche quello chiuso** — oppure, scegliendo la prudenza opposta, non si cancellerebbe
mai nulla.

| Colonna proposta | Su | Al posto di oggi |
|---|---|---|
| `terminated boolean` + `terminated_at timestamptz` | `catalog.properties` | interrogare Identity a ogni giro, senza poter distinguere cessato da sospeso |
| una data di revoca | `identity.access_users` | `deleted_at`, che regge solo se la revoca coincide col soft-delete: un account **sospeso e non cancellato** non scadrebbe mai |
| ~~`blacklist_reviewed_at`~~ **scartata** | `identity.customer_customers` | `updated_at`, accettato: cambia per qualsiasi modifica, ma per un promemoria basta |

**Soluzione che *era* stata proposta, e che non si fa più.** Erano tre migration piccole:

```sql
-- SCARTATA il 2026-09-30: il concetto di "cessato" non deve esistere
ALTER TABLE catalog.properties
  ADD COLUMN terminated    boolean     NOT NULL DEFAULT false,
  ADD COLUMN terminated_at timestamptz NULL;
```

**Perché serviva una conferma prima.** Non erano correzioni ma **decisioni di disegno**: avrebbero
introdotto un concetto nuovo — "cessato" come stato distinto da "non attivo" — che poi vive nel
gestionale, nei permessi e nella fatturazione. La risposta è stata che quel concetto **non deve
esistere**, e questo semplifica lo schema ma sposta il problema, come segue.

### La conseguenza: la cessazione diventa derivata, non dichiarata

In `docs/80-retention.md` la cessazione compare in **sedici righe** delle tabelle — come evento di
decorrenza, come azione a scadenza o dentro la durata: fra queste i 10 anni sui dati del titolare,
lo snapshot della configurazione poi cancellato dopo 2 anni, la revoca del collegamento Stripe
Connect, la riduzione della scheda pubblica a scheda essenziale. Se l'evento "cessazione" non
esiste come dato, nessuna di quelle righe ha una data da cui contare.

**Soluzione proposta.** Ricavarla dall'abbonamento, che esiste già:
`identity.account_subscriptions` porta `ends_at`, `suspended_at`, `status` e `reactivated_at`. Una
stabilimento si considera cessato **ai soli fini della retention** quando **non ha alcun abbonamento
attivo da più di un periodo di tolleranza**, e la data di decorrenza è l'`ends_at` (o
`suspended_at`) dell'ultimo abbonamento.

```sql
-- data di riferimento della cascata §5, per stabilimento
SELECT property_id, max(coalesce(ends_at, suspended_at)) AS cessazione_presunta
  FROM identity.account_subscriptions
 WHERE deleted_at IS NULL
 GROUP BY property_id
HAVING max(coalesce(ends_at, suspended_at)) < now() - interval '<tolleranza>'
```

**La tolleranza: ✅ 24 mesi, accettata il 2026-09-30.** Senza tolleranza un abbonamento scaduto da tre
settimane — cioè un cliente che pagherà in ritardo — farebbe partire la cancellazione dei suoi dati;
con una troppo lunga la retention non parte mai. I 24 mesi sono coerenti con i 2 anni che `docs/80`
§3 già concede allo snapshot della configurazione «utile per un eventuale rientro del cliente»:
**la stessa soglia governa "il cliente potrebbe tornare" e "possiamo cancellare"**, che è il modo di
non avere due numeri che si contraddicono.

Va esposta come parametro, non cablata, con lo stesso criterio delle date della finestra di
inattività (`analisi-post/bl/01`): `RETENTION_PROPERTY_TERMINATION_GRACE_MONTHS = 24`.

> **Lavoro che ne segue, e non richiede altre informazioni:** in `docs/80-retention.md` le **sedici
> righe** che citano la cessazione vanno riscritte su «fine dell'ultimo abbonamento + 24 mesi».
> Finché non è fatto, il documento promette un evento che non esiste. È
> **[B8](#b8-docs80-fa-decorrere-sedici-righe-da-un-evento-che-non-esiste)**.

Dettagli in [`_domande-committente.md`](../../analisi-post/db/_domande-committente.md), voce D-65.

---

## A2. Le 44 policy di retention sono tutte in prova — confermato, va bene così

> **Deciso il 2026-09-30:** si resta in sola prova. Lo spostamento su S3 e la cancellazione
> effettiva avverranno in un momento successivo, come atto separato. Questa voce non è più un
> lavoro aperto: resta scritta perché **nessuno deve dare per scontato che la retention stia già
> cancellando qualcosa**. Le due policy `disputed` restano invece davvero aperte, in fondo alla
> voce.

**Cos'è.** Le policy sono seminate con `is_dry_run_only = true`: il job produce solo il report di
ciò che *farebbe*, senza toccare nulla.

**Esempio concreto.** Nessuna retention è attiva oggi. Chi desse per scontato il contrario penserebbe
che i dati vecchi si stiano già cancellando, mentre stanno solo venendo contati. L'abilitazione è una
riga per policy:

```sql
UPDATE privacy.privacy_retention_policies
   SET is_dry_run_only = false
 WHERE schema_name = 'switchboard' AND table_name = 'voice_call_records';
```

**Perché è giusto che sia così.** È il comportamento corretto alla nascita: la prima esecuzione reale
di una regola di cancellazione è irreversibile, quindi deve essere un atto umano che segue la
lettura del report, **policy per policy**. Va solo detto esplicitamente, che è il motivo per cui
questa voce esiste.

### 🔍 Le due policy `disputed`: cosa dice la legge davvero

`switchboard.voice_call_records` e `switchboard.messaging_wa_event_logs` hanno
`basis_source = disputed`. **Prima correzione: la norma che avevo citato era sbagliata.** Non è il
D.L. 7/2015 (che era una misura temporanea, scaduta), ed è questa:

| Norma | Cosa impone | A chi |
|---|---|---|
| **Art. 132 c. 1 Codice Privacy** (D.Lgs. 196/2003) | traffico **telefonico: 24 mesi**; traffico **telematico: 12 mesi** | «il fornitore di servizi di comunicazione elettronica **accessibili al pubblico** oppure di una rete pubblica di comunicazione» |
| **Art. 132 c. 1-bis** | chiamate **senza risposta: 30 giorni** | idem |
| **Art. 24 L. 167/2017** (attua l'art. 20 Dir. UE 2017/541), richiamato dall'art. 132 c. 5-bis | **72 mesi**, in deroga ai commi 1 e 1-bis | idem, e **solo** per i reati di cui agli artt. 51 c. 3-quater e 407 c. 2 lett. a) c.p.p. — terrorismo, mafia, narcotraffico, pedopornografia |

Due conseguenze che cambiano la decisione.

**1. I 72 mesi non sono un obbligo generale.** Valgono in deroga e per quelle categorie di reato.
Conservare sei anni di traffico "per prudenza" non è la scelta cauta: se l'obbligo **non** ci
riguarda, sei anni di dati telefonici sono un eccesso, cioè una violazione del GDPR nella direzione
opposta. **Alzare a 72 mesi senza sapere se siamo soggetti all'obbligo sarebbe rischioso, non
prudente.**

**2. Tutto dipende da un solo fatto, e non è un'opinione: siamo "fornitore di servizi di
comunicazione elettronica accessibili al pubblico"?** I numeri e il traffico del centralino li
fornisce **Telnyx** (`ITelephonyProvider`, `docs/35-integrazioni-esterne.md` §5), e la messaggistica
Twilio/Meta: noi siamo clienti di un operatore, e offriamo agli stabilimenti una funzione a valore
aggiunto costruita sopra. Il modo concreto di verificarlo, che non richiede un'interpretazione:

> **SummerBooking ha un'autorizzazione generale AGCOM come operatore di comunicazioni
> elettroniche?** In Italia chi fornisce servizi di comunicazione elettronica al pubblico deve
> averla. **Se non ce l'ha, non è quel soggetto**, e l'art. 132 non impone nulla: la retention
> ricade sotto la sola minimizzazione GDPR, e 6 mesi va bene.

### ✅ Risposto il 2026-09-30: nessuna autorizzazione AGCOM, quindi niente da cambiare

**SummerBooking non ha un'autorizzazione generale AGCOM come operatore di comunicazioni
elettroniche.** Non è quindi il «fornitore di servizi di comunicazione elettronica accessibili al
pubblico» a cui l'art. 132 si rivolge: quell'obbligo è in capo a chi fornisce numeri e traffico —
**Telnyx** — non a chi ci costruisce sopra una funzione di centralino per i propri clienti.

Conseguenza: **l'art. 132 non impone nulla**, e la conservazione ricade sotto la sola minimizzazione
GDPR. La scelta già validata dal titolare in
[`docs/82-retention-data.md`](../../progettazione/82-retention-data.md) §3.7 — *«retention a 6 mesi, azione a
scadenza: cancella»* per il registro chiamate — **resta valida e non va toccata**. E i 72 mesi
sarebbero stati il contrario della prudenza: sei anni di dati telefonici senza un obbligo che li
giustifichi sono un eccesso, cioè una violazione nella direzione opposta.

**Resta solo un allineamento di dati, non una decisione:** le due policy portano ancora
`basis_source = disputed`, che ora è falso — il dubbio è stato risolto. Va messo il valore che
descrive la base reale (minimizzazione GDPR, scelta del titolare), altrimenti fra sei mesi qualcuno
riaprirà una questione chiusa leggendo quella parola. È l'unica cosa che resta di questa voce, e non
serve chiedere niente a nessuno.

> **Cautela dovuta, detta una volta:** non avere l'autorizzazione è il fatto che risolve la domanda
> in pratica, e coincide con la sostanza (compriamo il servizio da un operatore). Resta una
> qualificazione giuridica, e questo documento non è un parere legale: se un domani SummerBooking
> iniziasse a fornire numeri in proprio, la risposta cambierebbe.

---

## A3. Tre vincoli di non sovrapposizione non applicabili — chiusa

**Cos'è.** Dei sei vincoli `EXCLUDE USING gist` previsti, tre non si possono attivare perché i dati
migrati li violano già.

| Tabella | Coppie sovrapposte | Su |
|---|---:|---|
| `booking.resale_offers` | **294** | 12.900 righe |
| `catalog.pricing_tariff_periods` | **41** | 580 righe |
| `catalog.property_online_allowed_periods` | **9** | 68 righe |

**Esempi concreti, e sono di due tipi che vogliono risposte opposte.**

```
id=69  'Stagione unica'  2021-05-24 .. 2021-09-19
id=70  'Stagione unica'  2021-05-25 .. 2021-05-25     <- un giorno, dentro
```
```
id=250 'Bassa Stagione'  2025-04-30 .. 2026-07-18
id=252 'Bassa Stagione'  2025-04-30 .. 2026-09-29     <- stesso inizio, fine diversa
```

Il primo è un **periodo di un giorno dentro la stagione intera, con lo stesso nome**: somiglia a un
salvataggio accidentale, e si risolve buttando `id=70`. Il secondo ha **inizio identico e fine
diversa**: somiglia a una modifica salvata come riga nuova invece di aggiornare quella esistente, e
si risolve tenendo `id=252` e buttando `id=250`. Su `property_online_allowed_periods` lo schema si
ripete: `id=41` copre solo il 2026-08-30, dentro `id=44` che copre 2026-08-29 → 2026-09-03.

### Scomponendo le 41 coppie, il vincolo progettato è probabilmente sbagliato

Il vincolo previsto è `EXCLUDE (season_id WITH =, daterange(starts_on, ends_on) WITH &&)`: **nessuna
sovrapposizione fra due periodi qualsiasi della stessa stagione.** Guardando cosa violano davvero:

| Delle 41 coppie | |
|---:|---|
| **27** hanno **nome diverso**, e **22** hanno anche **fascia (`tier`) diversa** | `'Bassa stagione'` (low) 2021-06-11→06-29 contiene `'Alta stagione'` (high) **2021-06-12**, un giorno solo |
| **14** hanno lo **stesso nome** | `'Stagione unica'` 2021-05-24→09-19 contiene `'Stagione unica'` **2021-05-25** |
| 24 coinvolgono un periodo di **un solo giorno** | |

Le 27 con nome e fascia diversi **non sono dati sporchi: sono la funzione**. Un giorno di festa
dentro un periodo di bassa stagione, prezzato come alta, è esattamente ciò che uno stabilimento
vuole poter fare — e il vincolo così com'è **glielo vieterebbe**. Restringendolo a
`(season_id, period_group_key, tier)` restano **14 violazioni, tutte con lo stesso nome**, che sono
i duplicati veri.

Fra le 27 ci sono anche periodi chiamati `'Unit Testing'`, `'date check'`, `'Deployment'`,
`'Bug Fixing'`, `'Release'`: dati di collaudo di chi sviluppava, su stabilimenti dimostrativi.

### Su `resale_offers` la domanda ha dei soldi attaccati

Qui la sovrapposizione è fra **fasce della giornata sullo stesso posto e nello stesso giorno**, e le
294 coppie si dividono per stato:

| Coppie | Stati | Cosa significa |
|---:|---|---|
| **178** | `available` + `available` | lo stesso posto compare **due volte** nella lista di rivendita: fastidioso, non grave |
| **97** | `reassigned` + `available` | uno è stato rivenduto, **l'altro è ancora in lista**: può essere venduto di nuovo |
| **19** | `reassigned` + `reassigned` | **lo stesso posto risulta rivenduto due volte nello stesso giorno** |

I 19 sono su *Fiera Rimini* (2026-04-18, 2026-07-09, 2026-07-18), *Lido Bora Bora Playa*
(2025-08-15) e altri; nelle coppie le due offerte vengono da **prenotazioni diverse**.

**Soluzione proposta, in tre passi in quest'ordine:** (1) le due domande qui sotto; (2) applicare la
regola nell'ETL, così la bonifica si ripete a ogni rimigrazione senza interventi a mano; (3) solo
allora abilitare il vincolo — **corretto**, non quello progettato.

**Perché non una bonifica automatica.** Le coppie si somigliano nei numeri e vanno sistemate
all'opposto: `'Stagione unica'` dentro `'Stagione unica'` si risolve buttando il periodo di un
giorno, `'Alta stagione'` dentro `'Bassa stagione'` **non si risolve affatto**, perché è corretta. Un
`DELETE` scritto per la prima cancellerebbe la seconda, cioè cancellerebbe i prezzi dei giorni di
festa.

### ✅ Le risposte, e cosa ne segue (2026-09-30)

**Prezzi — «è voluta; prevale il prezzo del singolo giorno».** Due conseguenze, e la seconda è più
grossa della prima.

1. **Il vincolo va corretto, non i dati.** `EXCLUDE (season_id, daterange)` diventa
   `EXCLUDE (season_id, period_group_key, tier, daterange)`: così vieta due *"Alta stagione"*
   sovrapposte e permette un'*"Alta stagione"* di un giorno dentro una *"Bassa stagione"*. Restano
   **14 violazioni su 41**, tutte con lo stesso nome — quelle sì da ripulire, e 8 delle 14 sono
   periodi di un giorno solo.

   E le 14 si ripuliscono **senza chiedere niente**, perché sono redondanza a prezzo invariato: il
   prezzo non è agganciato al singolo periodo ma al **gruppo**
   (`pricing_price_list_period_prices.tariff_period_group` = `period_group_key`), quindi due periodi
   con la stessa stagione, lo stesso gruppo e lo stesso `tier` **hanno per costruzione lo stesso
   prezzo**: quale dei due si applichi a un dato giorno non cambia un centesimo. In 11 casi su 14 la
   riga con id maggiore contiene quella minore; duplicati esatti (stesse date) **zero**. Si tiene il
   periodo che contiene e si scarta quello contenuto.

   Le due righe di ogni coppia hanno lo **stesso `created_at`**, quindi non sono "una modifica salvata
   male": il legacy le conteneva già entrambe — coerente con il baco descritto in
   **[A7](#a7-i-nomi-dei-settori-che-non-coincidono--chiusa-si-creano-dai-listini)**, che il committente segnala valere anche «per
   stagioni e periodi».
2. **«Prevale il singolo giorno» è una regola di calcolo del prezzo che va scritta da qualche parte.**
   Non è un vincolo di database: è il motore di prezzo che, per un giorno coperto da due periodi,
   deve scegliere **il periodo più breve**. Oggi questa regola non è documentata in nessun file, e un
   motore che scegliesse «il primo trovato» o «l'ultimo inserito» darebbe prezzi diversi senza
   sbagliare nulla di evidente. Va aggiunta alla documentazione dei prezzi **e** tenuta presente per
   il motore di prezzo offline (`docs/19-frontend-offline.md`), che deve dare lo stesso risultato del
   server.

**Rivendita — «sì se mezza giornata o se c'è lo sharing».** La risposta spiega **199 delle 294
coppie**, non tutte, e i numeri dicono una cosa in più:

| | Coppie | |
|---|---:|---|
| Mezza giornata (mattina + pomeriggio) | **0** | il vincolo progettato **già** le permette: `morning` è `[1,2)` e `afternoon` `[2,3)`, non si sovrappongono. Tutte le 294 coppie sono `full_day` + `full_day` |
| Sharing (per origine dell'offerta o per modalità dello stabilimento) | **199** | *Blanca Cruz* e *Stabilimento balneare Demo* sono in modalità `sharing`; altrove l'origine è `sharing` o `absence_as_sharing` |
| **Residuo non spiegato** | **95** | stabilimenti in modalità `absence`, entrambe le offerte di origine `absence` |
| — di cui **entrambe riassegnate** | **17** | lo stesso posto per l'intera giornata risulta **rivenduto due volte** |

### ✅ I 17 "venduti due volte" non esistono: il vincolo era sulla colonna sbagliata

Cercando **a quando risalgono** — per valutare se non migrarli — è emersa prima una cosa che li
esclude come dati vecchi, e poi la spiegazione.

Le date dicono che **non si può rinunciare a migrarli**: delle 95 coppie, **88 sono del 2026**, cioè
della stagione in corso, e 13 dei 17 pure. E **12 dei 17 stanno su un solo stabilimento**, *Lido
Azzurro Aeronautica Militare*, su date ravvicinate (5, 14, 28 giugno; 4, 5, 12, 19, 21 luglio). Una
concentrazione così non è sporcizia storica: è il modo normale di lavorare di quello stabilimento.

E infatti il vincolo progettato guardava la cosa sbagliata. Chiave su `resource_id`, cioè **la
risorsa fisica**: ma una risorsa è l'ombrellone, e sotto un ombrellone ci sono più **linee di
prenotazione** (i lettini), ognuna rilasciabile per conto proprio da una prenotazione diversa.
Spostando la chiave sulla linea:

| Chiave del vincolo | Coppie che lo violano |
|---|---:|
| `(resource_id, offer_date, offered_period)` — come progettato | **294** |
| `(booking_line_id, offer_date, offered_period)` | **0** |

Tutte e 95 le coppie del residuo hanno `booking_line_id` **diverso**; nessuna ha la stessa linea. I
17 sono due lettini sotto lo stesso ombrellone, ciascuno restituito dalla propria prenotazione e
rivenduto: **corretti, non doppie vendite**. E lo sharing di cui parlavi è esattamente questo,
espresso in dati.

Quindi il vincolo **si può applicare subito**, con la chiave giusta, e non serve bonificare niente.
Un limite da dichiarare: **926 offerte su 12.900 (il 7%) non hanno `booking_line_id`**, e sui valori
nulli un `EXCLUDE` non confronta — quindi il vincolo protegge le 11.974 con la linea valorizzata, non
tutte. Meglio di zero protezione su tutte, ma non va raccontato come una copertura totale.

<details>
<summary>Le domande originali, per riferimento</summary>

> **1. Prezzi: un giorno può costare diversamente dal periodo che lo contiene?**
>
> Abbiamo trovato listini in cui un periodo lungo di *bassa stagione* contiene un singolo giorno
> marcato *alta stagione*. Esempio reale: dal 11 al 29 giugno è bassa stagione, ma il **12 giugno**
> è alta stagione.
>
> - È una cosa voluta — cioè gli stabilimenti usano questo modo per far pagare di più un giorno di
>   festa o un ponte — oppure sono errori di inserimento?
> - Se è voluta: quando un giorno è coperto da due periodi, **quale prezzo deve valere**? Il periodo
>   più breve (il giorno singolo) o quello più lungo?
>
> Ci serve per impedire al sistema nuovo le sovrapposizioni sbagliate senza vietare quelle giuste.
>
> **2. Rivendita: lo stesso posto può essere rivenduto due volte nello stesso giorno?**
>
> Quando un cliente avvisa che non verrà, il suo posto torna disponibile e può essere rivenduto.
> Abbiamo trovato **19 casi** in cui lo stesso posto, nello stesso giorno, risulta **rivenduto due
> volte** — le due rinunce arrivano da due prenotazioni diverse. Esempi: *Fiera Rimini* il 18 aprile,
> il 9 e il 18 luglio 2026; *Lido Bora Bora Playa* il 15 agosto 2025.
>
> - Può essere legittimo (per esempio il posto è un ombrellone con due lettini, rivendibili
>   separatamente), o è un errore che ha fatto pagare due volte lo stesso posto?
> - In altri **97 casi** un posto è stato rivenduto ma è **rimasto anche in lista** come ancora
>   disponibile. Succede ancora oggi? Se sì è un problema aperto, non solo di dati vecchi.

</details>

---

## A4. Due conversioni in array rimaste condizionate — risolta

> **Deciso il 2026-09-30:** **no**, non serve tracciare chi dà o toglie un permesso né quando.
> Quindi `identity.access_user_resource_scopes` **si converte in array** e la tabella sparisce. Come
> effetto collaterale cade anche la seconda colonna di
> **[A1](#a1-tre-colonne-che-mancano-per-eseguire-la-retention--d-65--chiusa)** (la
> data di revoca su `access_users`): se la storia dei permessi non serve, `deleted_at` basta.
>
> L'altra conversione, `booking_quick_action_dates`, resta come è: non aspettava una tua risposta ma
> una misura su carico reale, che oggi non esiste.
>
> **Lavoro che ne segue** (non richiede altre informazioni): modificare l'entità EF, generare la
> migration, aggiornare la spec dell'ETL. Passa in Parte B.

**Cos'è.** `docs/05-ottimizzazioni-db.md` §13.1 approva quattro collassi di tabelle figlie in array.
Due sono stati fatti; due portano una riserva che il documento stesso pone.

**Esempio concreto — la misura c'è, la decisione no.** L'obiezione a
`identity_access_user_resource_scopes` era che la tabella porta `created_at`/`deleted_at` **per
singola assegnazione**, e un array cancellerebbe la storia di quando una risorsa è stata concessa o
revocata. I dati migrati dicono:

| Righe | `created_at` distinti | Righe con `deleted_at` |
|---:|---:|---:|
| 19 | **2** | **0** |

Due soli istanti per diciannove assegnazioni, e nessuna revoca mai registrata: sono due scritture in
blocco. **Oggi quella storia non esiste**, quindi convertire non distrugge niente.

L'altra, `booking_quick_action_dates` → `date[]`, resta aperta per un motivo diverso: l'indice
`(property_id, action_date)` serve la query "azioni rapide del giorno X su tutte le prenotazioni", e
con un array servirebbe un GIN, meno adatto di un B-tree per una ricerca di uguaglianza su data.

**Soluzione proposta.** Convertire `access_user_resource_scopes`, citando la misura qui sopra;
**non** convertire `quick_action_dates` finché non si misura quella query su carico reale.

**Perché era una domanda e non una misura (storico).** La misura risponde a *«oggi si perde qualcosa?»* —
no. Non risponde a *«domani serve sapere quando un permesso è stato concesso o revocato?»*, che è una
domanda di prodotto: se la risposta è sì, quelle date vanno riempite per davvero, e l'array è la
strada opposta. `docs/05` §13.1 lo segnala esplicitamente come «da confermare col committente se è
un requisito reale», e 19 righe di risparmio non giustificano il rischio di chiudere una porta di
disegno da solo.

### 📋 Cosa mi serve sapere, esattamente

**Una risposta sola, e non tecnica:** *quando un permesso viene dato o togliato a una persona, deve
restare traccia di **chi** gliel'ha dato o togliato e **quando**?*

- **Se no** → converto in array e la tabella `access_user_resource_scopes` sparisce. È il caso
  coerente con quello che i dati mostrano oggi: 19 assegnazioni, due soli istanti di creazione, mai
  una revoca registrata.
- **Se sì** → non converto, **e va fatto di più**: le colonne esistono ma non vengono compilate, per
  cui oggi quella traccia non c'è comunque. Diventerebbe una modifica al gestionale (scrivere la
  data e l'autore a ogni cambio di permesso), non una scelta di schema.

Vale la pena rispondere anche se il beneficio è piccolo, perché la seconda risposta scopre un
requisito **non implementato** che oggi nessuno sta sorvegliando.

Per l'altra, `booking_quick_action_dates`, **non serve niente da te**: serve una misura su carico
reale che oggi non esiste. Resta com'è, e va bene così.

---

## A5. Il tetto dei pool divide in parti uguali carichi che uguali non sono — niente da fare ora

> **Cosa c'è da fare oggi: niente — e su AWS non c'è niente da installare, perché il pooler c'è già.**
> `infra/summerbooking-aws.yaml` dichiara un **`AWS::RDS::DBProxy`** (riga 300) e **tutti e tre** i
> container ci puntano già — `web`, `worker` e `job` — con
> `Database__Host: !GetAtt DbProxy.Endpoint`. RDS Proxy è
> PgBouncer gestito da AWS — stesso mestiere, nessun processo da installare o sorvegliare.
> **Dettagli, e il difetto che ho trovato guardando, [qui sotto](#su-aws-il-pooler-c%C3%A8-gi%C3%A0-ma-lapp-non-ci-arriva).**
>
> Le 88 connessioni attuali stanno comunque sotto il `max_connections = 100` del server, quindi non
> c'è un problema da risolvere: c'è uno spreco teorico che diventa un problema solo con traffico
> reale. Questa voce resta come promemoria, non come lavoro arretrato.
>
> **Il segnale che dice "adesso sì"**, uno solo: nei log compaiono attese sul pool
> (`The connection pool has been exhausted` o tempi di attesa non nulli su `Booking` o `Catalog`)
> mentre altri moduli sono fermi. Fino a quel momento alzare il tetto o installare PgBouncer
> aggiungerebbe un processo da sorvegliare per risolvere qualcosa che non sta accadendo.
>
> La procedura sotto è scritta perché quando servirà servirà in fretta, non perché vada eseguita.

**Cos'è.** Ogni modulo costruisce il proprio `NpgsqlDataSource` — necessario, perché le mappature
degli enum nativi sono per data source — quindi l'host ha **un pool per modulo**. Il tetto è fissato
a **8 connessioni per modulo**: 88 contro le 1.100 potenziali del default Npgsql, sotto il
`max_connections = 100` del server.

**Esempio concreto.** 8 nasce da una divisione in parti uguali fra undici moduli, ma `Booking` e
`Catalog` ricevono traffico a ogni prenotazione mentre `Chatbot` e `Infra` sono quasi fermi: si
stanno riservando a moduli inattivi otto connessioni ciascuno che servirebbero altrove. Il valore si
sovrascrive per ambiente senza toccare il codice:

```json
{ "Database": { "MaxPoolSizePerModule": 12 } }
```

e una `Maximum Pool Size` messa a mano nella connection string ha comunque la precedenza.

**Soluzione proposta, quando servirà.** Non alzare il numero, ma mettere un pooler (PgBouncer) davanti
al database.

**Perché non differenziare i numeri per modulo adesso.** Si può fare, ma significa scolpire in
configurazione un'ipotesi sul carico che nessuno ha misurato, e sbagliarla al ribasso su un modulo
caldo produce timeout difficili da attribuire. Un tetto uniforme e basso è *sicuramente* corretto sul
vincolo che conta — non saturare `max_connections` — e quando il carico reale si vedrà, la risposta
giusta non sarà ritoccare undici numeri, ma smettere di dividere una torta fissa.

### Su AWS il pooler c'è già, ma l'app non ci arriva

L'infrastruttura di destinazione è **ECS + RDS PostgreSQL** (`infra/summerbooking-aws.yaml`), con
tre task definition — `web`, `worker` e `job` — e il template dichiara già tutto il necessario:

```yaml
DbProxy:
  Type: AWS::RDS::DBProxy
  Properties:
    EngineFamily: POSTGRESQL
    RequireTLS: true
    IdleClientTimeout: 1800
    Auth: [{ AuthScheme: SECRETS, SecretArn: !GetAtt Database.MasterUserSecret.SecretArn }]
```

Quindi alla domanda «come si installa su AWS» la risposta è **non si installa**: si usa RDS Proxy,
che è già lì, e tutti e tre i container già lo indirizzano. Restano due cose da sapere e una da correggere.

**1. ⚠️ Un difetto che blocca l'avvio in AWS.** Il template inietta in ognuno dei tre container
`Database__Host`, `Database__Port`, `Database__Name` e — da Secrets Manager — `Database__Username` e
`Database__Password`. Ma il codice legge **`ConnectionStrings:SummerBooking`**
(`ModuleConnectionStrings.ConnectionStringName`), e `Boot/appsettings.json` **non la contiene**: c'è
solo in `appsettings.Development.json`, con `localhost`. In produzione nessuno legge quei cinque
valori, quindi **l'app non troverebbe alcuna connection string**.

Non si risolve dal lato template: una password che viene da Secrets Manager non si può concatenare
dentro una variabile d'ambiente in una task definition ECS. Si risolve nel codice, facendo comporre
la stringa da `Database:*` quando `ConnectionStrings:SummerBooking` manca — che è anche il punto
naturale per aggiungere `SSL Mode=Require`, obbligatorio perché il proxy ha `RequireTLS: true`. È
**[B9](#b9-in-aws-lapp-non-troverebbe-la-propria-connection-string)**.

**2. Il pinning è la cosa da verificare, non il tetto.** RDS Proxy smette di multiplexare e "appunta"
una connessione server a un client quando rileva stato di sessione: `SET` fuori transazione,
prepared statement con nome, tabelle temporanee, `LISTEN`. Se pinna, il beneficio svanisce senza
alcun errore. Nel nostro caso la strada è libera — non c'è `search_path` impostato a mano (gli schemi
sono qualificati via `HasDefaultSchema`), nessun `LISTEN/NOTIFY`, nessun advisory lock, e Npgsql con
`MaxAutoPrepare = 0` (il default) non crea prepared statement con nome — ma va **misurato** sulla
metrica CloudWatch `DatabaseConnectionsCurrentlySessionPinned`, non dato per buono.

**3. Le migration non passano dal proxy.** `dotnet ef database update` va puntato all'endpoint RDS
diretto: il DDL vuole una sessione stabile.

### 📋 Se invece si volesse PgBouncer vero

Non è una riga di codice: è un processo in più da installare e sorvegliare, e questo è il motivo per
cui la voce sta in Parte A. Cinque passi.

**1. PgBouncer in modalità `transaction`** — è la sola che aiuti davvero: restituisce la connessione
al termine di ogni transazione, non alla chiusura del client, quindi undici pool applicativi affamati
condividono poche connessioni reali.

```ini
[databases]
summerbooking = host=127.0.0.1 port=5432 dbname=summerbooking

[pgbouncer]
pool_mode = transaction
listen_port = 6432
max_client_conn = 500        ; quante connessioni l'app puo' aprire verso PgBouncer
default_pool_size = 25       ; quante connessioni reali verso PostgreSQL
```

**2. L'app punta a 6432 e il tetto per modulo si alza.** Il tetto basso esiste solo perché le
connessioni erano reali; dietro PgBouncer diventano economiche:

```json
{ "Database": { "MaxPoolSizePerModule": 40 } }
```

**3. Due impostazioni Npgsql, o si rompe in modo intermittente.** Nella connection string:
`No Reset On Close=true` (il reset lo fa già PgBouncer, farlo due volte è solo latenza) e nessun
`Prepare()` esplicito: con il pooling per transazione una *prepared statement* creata su una
connessione server può non esserci sulla successiva. EF Core non ne usa di persistenti, quindi oggi
va bene — ma è un vincolo da ricordare, perché il guasto si presenta come un errore raro e
inspiegabile sotto carico.

**4. Le migration NON passano da PgBouncer.** `dotnet ef database update` va puntato alla 5432
diretta: il DDL e i lock di migrazione vogliono una sessione stabile, e passare dal pooler è un modo
di cercarsi guai in un momento in cui non li si vuole.

**5. Cosa verificare subito dopo, perché è il punto fragile di questo progetto.** Ogni modulo mappa
i propri enum nativi leggendo gli OID dal database quando costruisce il `NpgsqlDataSource`. È una
lettura all'avvio, e con il pooling per transazione funziona — ma va **verificata all'avvio di tutti
gli undici moduli**, non di uno, perché se un modulo non riuscisse a risolvere i propri enum
fallirebbe alla prima query e non alla connessione.

Il codice non richiede alcuna modifica per questi passi: `ApplyModulePoolLimit` legge già
`Database:MaxPoolSizePerModule`, e una `Maximum Pool Size` messa a mano nella connection string ha la
precedenza su tutto.

---

## A6. I 20 account applicativi non migrati — chiusa

> **Deciso il 2026-09-30, voce chiusa:** ai **11** utenti dell'app senza stabilimento si **associa
> il ruolo `consumer`** (già implementato); i **5** account di prova con stabilimento restano fuori;
> i **4 senza email sono da ignorare nella migrazione**; le **4** stampanti con modello inventato
> sono confermate di prova.
>
> Sui 4 senza email non serve alcuna modifica: la regola `exclude_row_if: "not email"` della spec li
> scarta già. Cambia solo il verdetto nel report — da «da decidere» a **«voluto»** — perché ora è una
> scelta e non un dubbio. Elenco in [`_utenti-senza-ruolo.md`](_utenti-senza-ruolo.md), gruppo C.
>
> ⚠️ **La premessa dei 4 era sbagliata, e tre di loro sono persone reali:** l'email ce l'hanno, nella
> colonna `username`. Vedi subito qui sotto.
>
> La regola è ora in `custom_transforms/identity_access_users.py`: senza ruolo **e** senza
> stabilimento → `consumer` (id 3); senza ruolo **ma con** stabilimento → riga scartata, perché lì
> un ruolo inventato sarebbe un accesso al gestionale che nessuno ha concesso. I 11 rientrano alla
> prossima rimigrazione.

### I 3 account "senza email" hanno l'email nella colonna sbagliata

La domanda era: **con quale identificativo entrano**, se email e telefono sono vuoti? Non serviva
chiederlo a nessuno — la risposta è nel codice legacy, [`back-pre/app/controllers/auth.controller.js:310`](../../../../../../back-pre/app/controllers/auth.controller.js):

```js
where: { [Op.or]: [{ email: req.body.username }, { username: req.body.username }] }
```

Il login accetta **email oppure `username`**. E guardando la colonna `username` di quei quattro:

| id | `username` | `email` | Nome |
|---|---|---|---|
| 1629 | `claudia197421@gmail.com` | *(vuota)* | Claudia |
| 4488 | `elena.58@alice.it` | *(vuota)* | — |
| 5285 | `madreterra.roma@gmail.com` | *(vuota)* | — |
| 3788 | *(nulla)* | *(nulla)* | — |

**Tre dei quattro hanno un indirizzo email valido: è solo finito nella colonna `username`.**
Claudia non è un mistero, è `claudia197421@gmail.com`. Il quarto (3788, accesso Apple) non ha
davvero nulla.

**✅ Deciso il 2026-09-30: non si ignorano, si prende l'email da `username`.** La regola è
verificabile e non indovina niente: **se `email` è vuota e `username` ha la forma di un indirizzo, si
usa `username` come email**. Rientrano i tre (Claudia, Elena, `madreterra.roma`); il quarto (3788,
accesso Apple, entrambe le colonne nulle) resta fuori perché non c'è nulla da prendere.

Va applicata **prima** della regola `exclude_row_if: "not email"` della spec, che altrimenti scarta
la riga prima che il `username` venga guardato: è un `custom_row_transform` che valorizza `email`, non
una modifica a quella condizione. Diventa la voce **[B14](#b14-recuperare-lemail-dalla-colonna-username)**.

**E sul disegno dell'accesso, la buona notizia:** nessuno resta chiuso fuori. In tutto il dump gli
utenti con `username` diverso dall'email sono **453**, ma **384 hanno un username vero** (`summer`,
`vijay`, `kumar` — in gran parte account interni su `@summer.com`) **e tutti hanno anche un'email
valida**: con l'email come chiave di accesso entrano comunque. Gli altri 69 hanno uno username di
forma email. **Zero** utenti hanno un username vero senza email.

**✅ Deciso il 2026-09-30: l'accesso con username breve non si conserva.** Quei 384 utenti entreranno
con il proprio indirizzo invece che con `vijay`, e nessuno resta chiuso fuori perché tutti ne hanno
uno valido. Il nuovo schema resta com'è: **l'email è l'unica chiave di accesso**, senza un secondo
identificativo da mantenere unico e da spiegare.

> Una conseguenza da tenere presente al cutover, non un problema di migrazione: quei 384 sono in
> gran parte account interni su `@summer.com`, cioè proprio le persone che useranno il sistema
> nuovo per prime. Cambiando il modo in cui entrano, conviene dirlo prima e non scoprirlo al
> primo accesso fallito.

> **Una conferma incidentale, e vale la pena dirla.** Nello stesso file, riga 337:
> `var authorities = "consumer";` prima di leggere i ruoli. **Il legacy stesso trattava come
> `consumer` chiunque non avesse ruoli** — esattamente la decisione presa qui sopra per gli 11
> utenti senza ruolo. Non era un'inferenza: era già il comportamento del vecchio sistema.

**Cos'è.** `identity.access_users` pretende **un ruolo** (`role_id` obbligatorio) e **un'email**
(è l'identificativo di accesso, con indice unico). Nel legacy `users` non pretendeva né l'uno né
l'altra, e 20 righe ne sono senza: **16 scartate dal transform perché senza ruolo** e **4 dalla
regola `not email` della spec**.

> Nel dump gli utenti senza alcun ruolo sono **17**, non 16: il diciassettesimo (3788) è stato
> escluso prima, dalla regola sull'email, quindi non è mai arrivato al controllo sul ruolo. I due
> conteggi sono entrambi giusti e vanno letti così.

**L'elenco completo, con i dati per decidere, è in
[`_utenti-senza-ruolo.md`](_utenti-senza-ruolo.md)** (prodotto da
`utility/01-migrazione-test/scripts_utenti_senza_ruolo.py`). Guardandoli si dividono in tre gruppi
con risposte diverse, e **solo l'ultimo ha bisogno di te**:

| | Righe | Cosa sono | Proposta |
|---|---:|---|---|
| **A** | 11 | nessuno stabilimento, accesso Google/Facebook/Apple, email personali. Sei di loro hanno **schede cliente** con la stessa email | assegnare il ruolo `consumer` |
| **B** | 5 | hanno uno stabilimento ma sono **account di prova**: tutti `inactive`, due su stabilimenti **cancellati**, tre con email inventate create lo stesso giorno dallo stesso account | restano fuori |
| **C** | 4 | manca l'**email**; tre hanno il ruolo `consumer` e **tutti hanno fatto accesso nel 2026** | ← **serve una risposta** |

**Perché `consumer` non è il "fallback privilegiato" che la scheda vieta.** La nota di migrazione
proibisce di inventare un ruolo, e ha ragione: `manager` o `admin` creerebbero un accesso che nessuno
ha concesso. Ma `consumer` è il **meno privilegiato** dei tredici ruoli, è quello che per definizione
ha chi si registra dall'app, e non dà alcun accesso al gestionale. Per un account senza stabilimento
creato via Google o Apple è la sola lettura coerente, perché **gli altri dodici ruoli presuppongono
uno stabilimento che questi non hanno** — e ce l'hanno già **6.004 account identici a questi**.

**La domanda vera è nel gruppo C, e non riguarda quelle 4 righe.** Tre di loro hanno
`loginMethod = normal`, cioè accesso con credenziali, ma email vuota **e** telefono vuoto: con quale
identificativo entrano? Se esiste un modo di autenticarsi che non conosciamo, il nuovo schema —
che usa l'email come chiave — non lo prevede, e va saputo **prima del cutover**. Il quarto entra con
**Apple**, che consente di nascondere l'email: l'utente 3787, stesso giorno e stesso metodo, ha un
indirizzo `@privaterelay.appleid.com`, questo invece l'ha vuota. Se il vecchio sistema in qualche
caso non la salvava, il problema si ripeterà a ogni nuova registrazione con Apple.

> **Le 4 stampanti con modello inventato** (`dddd`, `Asdax`, `fi`, `tets`) erano qui come A6.2:
> **confermate di prova**, restano fuori. Le altre 11 sono stampanti vere e si recuperano senza
> chiedere niente — **e invece no**, vedi **[B5](#b5-11-stampanti-perse-per-una-porta-malformata--proposta-annullata)**: la proposta era sbagliata e le 11 vanno reinserite a mano.

---

## A7. I nomi dei settori che non coincidono — chiusa, si creano dai listini

**È la voce con più impatto pratico di tutto il documento**, ed è anche l'unica già pronta per essere
risolta da chi ha le informazioni.

**Cos'è.** Nel legacy il legame fra un prezzo e un settore è una **stringa**: `price_lists.mainPrices`
ripete il nome del settore, e `sectors_rows` contiene i settori veri. Non c'è alcuna chiave fra le
due. Rinominare un settore lascia i listini a citare il nome vecchio, e nulla lo segnala. Nel nuovo
schema `sector_id` è una chiave esterna obbligatoria, quindi ogni nome che non si risolve fa
**perdere quella riga di prezzo**.

> **La causa, dal committente (2026-09-30):** «la lista di settori è quella che sta nel listino. Il
> problema si è verificato perché fino a pochi mesi fa modificando il settore non modificava il
> listino, poi il codice è stato modificato e ora funziona, ma molti dati sono rimasti disallineati.
> Stessa cosa con stagioni e periodi.»
>
> Questo spiega **perché** i dati sono disallineati e chiude una domanda che restava aperta: non è un
> difetto della migrazione né un'anomalia da indagare, è un baco noto e già corretto che ha lasciato
> uno strascico. Spiega anche perché il disallineamento **non cresce più**: i listini toccati dopo la
> correzione sono allineati, quelli non più toccati restano indietro. E la stessa causa vale per le
> sovrapposizioni di periodi di **[A3](#a3-tre-vincoli-di-non-sovrapposizione-non-applicabili--chiusa)**.

### Cosa dicono i dati alla luce di quella causa

Se il meccanismo fosse una rinominazione semplice, per ogni nome orfano ci sarebbe un settore
esistente **non citato da nessun listino** — il nuovo nome — e l'accoppiamento sarebbe forzato senza
bisogno di chiedere niente. Ho verificato, e **non è così**:

| Delle 263 voci | |
|---:|---|
| **1** | accoppiamento forzato: un solo orfano e un solo settore libero (*Rosapineta Camping Village*: `Altre file` → `Fila 3 e 4`) |
| **66** | pochi orfani e pochi settori liberi, quindi una **rosa breve** fra cui scegliere (4 stabilimenti) |
| **196** | **nessun settore libero**: tutti i settori attuali di quello stabilimento sono già citati da altri listini |

I 196 sono il gruppo che conta, e dicono una cosa precisa: quei nomi **non sono i vecchi nomi di
settori esistenti**, perché i settori esistenti sono tutti già in uso. Sono nomi di una
**configurazione precedente diversa**, non solo rinominata. Il caso tipico si vede a occhio:

- *La Lampara – Passoscuro* cita `Fila 1-2`, `Altre file`, `RIVA 2 lettini`, `RIVA 2 lettini plus` —
  un assetto **per file**, mentre oggi i settori sono altri;
- *La Baia Blu* cita `Terrazza Santa Teresa` mentre ha `Terrazze Santa Teresa`: qui la rinominazione
  c'è stata, ma il nome nuovo è **già citato** dai listini corretti dopo il fix, quindi non risulta
  "libero" — ed è esattamente la fotografia di un disallineamento parziale.

### ✅ La soluzione: i settori si creano dai nomi citati nei listini

> **Deciso il 2026-09-30:** «prendi i nomi del settore nei listini». Il listino **è** la lista dei
> settori: se cita un nome che nell'anagrafica non c'è, quel settore va **creato**, non cercato.

Questo ribalta la voce e la chiude: da «263 nomi da far verificare a una persona» a «275 righe
recuperate senza chiedere niente a nessuno».

**È tecnicamente banale.** `catalog.map_sectors` non richiede una posizione sulla mappa né un
layout: le sole colonne obbligatorie sono `property_id`, `name` e `sort_order`, tutto il resto ha un
default o è annullabile. Un settore si crea dal solo nome.

```
per ogni stabilimento:
    settori = nomi in sectors_rows  ∪  nomi citati in price_lists.mainPrices
    i nomi mancanti si creano con sort_order dopo gli esistenti
```

**Cosa rientra**, e non sono solo i prezzi dei listini:

| Tabella | Righe recuperate |
|---|---:|
| `catalog.pricing_price_list_sector_prices` | **263** |
| `catalog.pricing_discount_sectors` | **11** |
| `catalog.property_online_stay_limits` | **1** |

**Due cose da sapere, che non cambiano la decisione ma vanno dette.**

1. **I settori creati compariranno nella configurazione del nuovo gestionale.** Per i 196 casi in cui
   tutti i settori attuali sono già citati, i nomi orfani appartengono a un assetto superato — *La
   Lampara* si ritroverà `Fila 1-2` e `Altre file` accanto ai settori di oggi. Se è un problema, la
   variante è crearli **già cancellati** (`deleted_at` valorizzato dopo la migrazione): la chiave
   esterna dei prezzi è soddisfatta, lo storico si conserva, e la mappa resta pulita. Dimmi quale
   delle due preferisci.
2. **Serve un vincolo di unicità che oggi non c'è.** `map_sectors` non ha un `UNIQUE (property_id,
   name)`: senza, una rimigrazione potrebbe creare due volte lo stesso settore. Va aggiunto assieme
   alla modifica, non dopo.

**Conseguenza: i due documenti di verifica non servono più.** `_settori-da-verificare.md` e
`Settori-da-verificare.docx` erano costruiti sulla domanda «che fine ha fatto questo settore», che
non si pone più. Restano come traccia di com'era la situazione, senza essere da compilare.

**Quanto pesa, misurato.**

| | Voci |
|---|---:|
| Voci prezzo-settore nei listini | 3.445 |
| Non risolvibili | **303** (8,8%) |
| — risolte in automatico dalla normalizzazione di formato | **40** |
| — **da decidere a mano** | **263** |
| di cui su stabilimenti **operativi** | **169** |

Gli stabilimenti coinvolti sono **24**, di cui **17 operativi** (approvati, non cancellati, con
prenotazioni dal 2025-09). Fra loro aziende molto attive: *La Lampara – Passoscuro* (9.856
prenotazioni totali, 4.155 nell'ultimo anno), *Marine Village* (7.090), *Lido Tuareg Beach* (2.486).
Le stesse rinominazioni colpiscono anche `catalog.pricing_discount_sectors` (**11 righe**: uno sconto
che si applica a meno settori del dovuto — silenzioso e sfavorevole al cliente) e
`catalog.property_online_stay_limits` (**1 riga**).

**Cosa è già stato fatto.** Il lookup dei settori ha un ripiego di **sola formattazione**: maiuscole,
accenti, spazi e separatori. `FILA-2` e `FILA - 2` sono ora lo stesso settore, e questo ha recuperato
40 prezzi di un solo stabilimento — il *Lido Raggio di Luna*, 820 prenotazioni tutte nell'ultimo
anno, che avrebbe perso l'intero listino per una differenza di spaziatura.

**Esempio concreto di come si compila.** Stabilimento con settori attuali `Spiaggia`,
`Terrazze Belvedere`, `Terrazze Santa Teresa`:

| Nome citato dai listini | Volte | Corrisponde a | Perché |
|---|---:|---|---|
| `Terrazza Santa Teresa` | 16 | `Terrazze Santa Teresa` | singolare/plurale: il nome è stato corretto nell'anagrafica e i listini sono rimasti indietro |
| `Piscina - Solarium Grecale - Terrazze Belvedere` | 16 | `Terrazze Belvedere` | nome lungo poi accorciato — **ma potrebbe anche essere che piscina e solarium fossero zone distinte, poi separate**, e allora la risposta è un'altra |
| `Fila 1-2` | 16 | `ELIMINATO` | nessun settore attuale è una fila: lo stabilimento è passato da un assetto per file a uno per zone |

**Perché le 263 non si possono chiudere in automatico.** La somiglianza fra stringhe qui inganna, e in
modo simmetrico:

| Nel listino | Settore esistente | Somiglianza | In realtà |
|---|---|---:|---|
| `Comitivo` | `Comitiva` | 88% | stesso settore, refuso |
| `Caraibi3` | `Caraibi2` | 88% | **settori diversi** |
| `RIVA 2 lettini` | `RIVA 1 lettino` | 86% | **diversi** |
| `Dalla 2° Fila in poi` | `Dalla 3° Fila in poi` | 95% | **diversi** |

Una mappa dedotta per somiglianza assegnerebbe a `Caraibi3` il prezzo di `Caraibi2`. **Un prezzo
mancante fallisce in modo rumoroso e si corregge; un prezzo sbagliato fattura male e non se ne
accorge nessuno.** Per questo il ripiego normalizza la sola forma e non tenta mai di indovinare il
contenuto — accettando di non risolvere nemmeno `Comitivo`/`Comitiva`, che è quasi certamente un
refuso.

**Soluzione proposta.** Le risposte del committente, già preparate in due formati:

- [`_settori-da-verificare.md`](_settori-da-verificare.md) — versione tecnica, per noi;
- [`Settori-da-verificare.docx`](Settori-da-verificare.docx) — versione per il committente, con
  l'esempio svolto e le schede per stabilimento da compilare.

Per ogni nome si indica il settore attuale corrispondente, oppure `ELIMINATO` o `NUOVO`. Ricevute le
risposte diventano una mappa di sinonimi applicata al lookup, e i prezzi rientrano alla prima
rimigrazione.

> **Nota sul conteggio.** Il report attribuisce a questa voce **287** righe su
> `pricing_price_list_sector_prices`, non 263: la differenza di **24** sono le righe orfane di
> **[B1](#b1-le-spec-figlie-non-ereditano-le-esclusioni-del-padre)** (figlie di listini esclusi),
> che con i settori non c'entrano. Sistemata quella, i due numeri coincidono.

---
---

# Parte B — si chiudono senza alcuna informazione in più

Quindici voci. Per nessuna serve una risposta: la regola da applicare è già determinata dai dati o
dallo schema, ed è verificabile subito. Sono in ordine di valore.

**Quella che recupera più dati è B11** (275 righe di prezzo, creando i settori dai listini). Diverse
altre non recuperano nemmeno una riga: rendono **onesto il conteggio degli errori**, che è la
condizione per potersi fidare del report la prossima volta — 306 errori che contengono righe che non
sono errori, e nessuno sa quali.

| | Voce | Effetto | Costo |
|---|---|---|---|
| **B1** | Esclusioni non ereditate dalle spec figlie | 24 righe passano da "errore" a "scarto previsto"; fino a 33 coppie da verificare | mezza giornata |
| **B2** | Copertura del registro sulle 57 spec con `aggregate_step` | toglie il punto cieco che ha già prodotto 2 diagnosi false | 1-2 giorni |
| **B3** | Dichiarare 7 scarti che oggi arrivano al database | 306 errori → 299 veri | mezz'ora |
| **B4** | Partizioni oltre il 2030 | elimina un degrado silenzioso e non recuperabile a costo basso | mezz'ora |
| ~~**B5**~~ | ❌ **annullata**: normalizzare la porta delle stampanti | la spec aveva già respinto la proposta, con ragione: 9100 sarebbe un dato inventato | — |
| **B6** | Convertire `access_user_resource_scopes` in array (deciso in **[A4](#a4-due-conversioni-in-array-rimaste-condizionate--risolta)**) | una tabella in meno | un'ora |
| **B7** | Correggere i tre vincoli `EXCLUDE` — chiave giusta su `resale_offers`, `tier`+gruppo sui periodi — e collassare le 14 sovrapposizioni a prezzo invariato (deciso in **[A3](#a3-tre-vincoli-di-non-sovrapposizione-non-applicabili--chiusa)**) | **tre** vincoli attivi invece di zero, senza bonifiche a mano | mezza giornata |
| **B8** | Riscrivere in `docs/80-retention.md` le 16 righe che decorrono da un evento inesistente (deciso in **[A1](#a1-tre-colonne-che-mancano-per-eseguire-la-retention--d-65--chiusa)**) | la policy diventa eseguibile | un'ora |
| **B9** | Comporre la connection string da `Database:*` + `SSL Mode=Require` | **oggi in AWS l'app non partirebbe** | un'ora |
| **B10** | Documentare «vince il periodo più breve» nel motore di prezzo (deciso in **[A3](#a3-tre-vincoli-di-non-sovrapposizione-non-applicabili--chiusa)**) | prezzi deterministici, server e offline uguali | un'ora |
| **B11** | Creare i settori dai nomi citati nei listini + `UNIQUE (property_id, name)` (deciso in **[A7](#a7-i-nomi-dei-settori-che-non-coincidono--chiusa-si-creano-dai-listini)**) | **recupera 275 righe** e cancella la voce più pesante del documento | mezza giornata |
| **B12** | Sostituire `basis_source = disputed` sulle 2 policy del centralino: il dubbio è risolto, i 6 mesi restano (chiuso in **[A2](#a2-le-44-policy-di-retention-sono-tutte-in-prova--confermato-va-bene-cos%C3%AC)**) | una parola che non mente | 10 minuti |
| **B13** | Togliere `HasDefaultValue(true)` da `is_staff`, correggere le 2 righe con SQL nella migration, aggiungere il test sul modello (3a) e la query di verifica (3c) | il flag mente su `consumer`: **5.987 utenti** verrebbero letti come personale | mezza giornata |
| **B14** | Prendere l'email da `username` quando `email` è vuota (deciso in **[A6](#a6-i-20-account-applicativi-non-migrati--chiusa)**) | **recupera 3 utenti reali** | 20 minuti |
| **B15** | Nota di progetto: sul percorso Apple l'email può mancare, e l'alias arriva una volta sola | evita di riprodurre l'account senza email | 0, è una nota |

---

## B1. Le spec figlie non ereditano le esclusioni del padre

**Cos'è.** Quando la spec di una tabella padre dichiara un `exclude_row_if`, le spec delle figlie
continuano a migrare le righe di quei padri esclusi. La figlia punta a un genitore che a
destinazione non esiste, e fallisce sulla chiave esterna o su un `NOT NULL`.

**Esempio concreto.** `catalog_pricing_price_lists.yaml` porta:

```yaml
exclude_row_if: "id == 0 or not propertyId"
```

I listini 271-278 non hanno stabilimento, quindi restano fuori — correttamente. Ma
`catalog_pricing_price_list_sector_prices.yaml` non ha nessun `exclude_row_if` e prova a inserire i
loro prezzi: **24 righe** che il database rifiuta. Risolvendo il settore fallirebbero comunque sulla
chiave `fk_catalog_pricing_price_list_sector_prices_price_list`.

Cercando lo stesso schema in tutte le spec: **33 coppie figlia/padre** dove il padre esclude e la
figlia no.

| Padre | La regola del padre | Figlie senza esclusione |
|---|---|---:|
| `catalog.map_sectors` | `not propertyId` | 8 |
| `catalog.pricing_price_lists` | `id == 0 or not propertyId` | 8 |
| `catalog.bathhouses` | `not bathouseName or not propertyId` | 6 |
| `catalog.property_online_booking_settings` | `not onlineBookingOptions and not onlineReservation` | 3 |
| `catalog.countries` | `country_english == 'test country'` | 2 |
| `fiscalprint.printer_configs` | modello fuori elenco | 2 |
| `identity.access_users` | `not email` | 2 |
| `catalog.map_layouts` | `not propertyId` | 1 |
| `catalog.service_amenity_categories` | `id == 7` | 1 |

**Soluzione, e perché non serve chiedere niente.** Per ogni coppia si interroga il dump: esistono
figli di padri esclusi? È una query. Dove esistono, si riporta la condizione del padre nella figlia
usando la colonna che la figlia ha davvero:

```yaml
# catalog_pricing_price_list_sector_prices.yaml
exclude_row_if: "price_list_id in (271,272,273,274,275,276,277,278)"   # NO: scade al primo dump nuovo
exclude_row_if: "not propertyId"                                       # sì, se la colonna c'è
```

Non c'è alcuna scelta di business: **quelle righe vanno scartate in ogni caso**, perché la chiave
esterna verso un genitore inesistente non è negoziabile. La sola domanda è se scartarle a monte
dichiarandolo o a valle lasciandole sbattere contro il database, e la prima è sempre meglio.

**Perché coppia per coppia e non a tappeto.** Non sono 33 difetti, sono 33 coppie da **guardare**:
difettosa con certezza è una sola, quella dei prezzi per settore, con 24 righe provate. Le altre
dipendono da quanti padri esclusi abbiano davvero dei figli, e spesso sono pochissimi —
`catalog.bathhouses`, per esempio, di righe senza nome o senza stabilimento ne esclude **una**.
Aggiungere 33 condizioni al buio significa scrivere regole che nessuno ha verificato, e ogni
condizione va letta nel contesto della propria spec, perché la colonna su cui filtrare non ha
necessariamente lo stesso nome del padre.

---

## B2. Il registro degli scarti non copre 57 spec su 129

**Cos'è.** Il registro (`app/discard_log.py`) intercetta i cinque punti in cui l'orchestrator scarta
una riga. Le **57 spec su 129** che usano `aggregate_step` eseguono `INSERT` propri dentro
`custom_transforms/` e non passano da lì: di quelle si conoscono solo i conteggi.

**Esempio concreto, e non è un'ipotesi: questo difetto ha già prodotto due diagnosi false**, rimaste
nel report fino a oggi.

- Il report dichiarava *«1.010 iscrizioni newsletter **senza indirizzo email**»* e concludeva
  «nessuna correzione tecnica possibile, il dato non c'è». Aprendo il registro, le righe hanno tutte
  l'email (`danieleduranti@ymail.com`, `salvoc-@hotmail.it`…): erano **duplicati deduplicati**.
- Dichiarava *«20 account applicativi **senza email**»*. Anche loro hanno l'email
  (`soumyamishra.mishra88@gmail.com`…): mancava il **ruolo**.

In entrambi i casi il registro riportava `(esclusa dal custom_row_transform, motivo non dichiarato)`,
perché il codice faceva `raise _RowExcluded()` **senza motivo**, e la diagnosi ha riempito il vuoto
con un'ipotesi plausibile e falsa. Un precedente più costoso: `booking.service_lines` ha perso
**10.611 righe** in una corsa intermedia senza comparire nel registro, perché inserisce per conto
proprio; si vedevano solo nel log applicativo.

**Soluzione, per metà già applicata.**

1. **Fatto**: tutti i `raise _RowExcluded()` senza motivo — **18 punti in 13 file** — ora dichiarano
   il proprio motivo, e le due diagnosi false sono state riscritte.
   ```python
   raise _RowExcluded("email duplicata: vince l'iscrizione con id piu' alto")
   raise _RowExcluded("nessun ruolo in user_roles: nessun fallback privilegiato")
   ```
2. **Da fare**: esporre il registro sul `TransformContext` (una riga) e cablarlo nelle 57 transform
   con `aggregate_step` che inseriscono davvero.

**Perché non serve chiedere niente.** È lavoro di strumentazione: il registro non cambia cosa viene
migrato, cambia solo cosa si sa. L'unica ragione per cui non è già fatto è che ogni transform ha il
proprio codice di inserimento e non c'è un punto unico dove agganciarsi — sono 55 modifiche piccole,
non una decisione.

**Perché prima i motivi e poi la copertura.** I motivi costavano un'ora e togliono al registro la
possibilità di mentire: un registro che dice "non lo so" è sgradevole ma innocuo, uno che indovina è
pericoloso, perché una diagnosi plausibile non viene verificata da nessuno. Fino alla copertura piena
vale la regola: **uno "0 scarti" su una tabella con `aggregate_step` non significa "nessun problema",
significa "non lo sappiamo"**.

> I file del registro su disco (`dati/*.jsonl`) portano ancora i `detail` vecchi: i motivi nuovi
> compariranno alla prossima corsa di migrazione. Le **diagnosi** sono già corrette, perché sono
> indicizzate su `(tabella, motivo)` e non sul testo del dettaglio.

---

## B3. Errori che sono scarti previsti non dichiarati

**Cos'è.** Sette righe finiscono fra i 306 "errori" pur essendo scarti perfettamente corretti:
arrivano fino al database e lì vengono rifiutate, invece di essere fermate prima.

**Esempi concreti, tutti verificati.**

| Righe | Cosa sono | Perché arrivano al DB |
|---:|---|---|
| **4** | `identity.customer_bathhouse_likes` id 95, 122, 128, 129 → casse 2, 8, 15, 41, che **non esistono** in `bathouses`. Portano anche `createdAt = '0000-00-00 00:00:00'`. | Il transform controlla che esista l'**utente**, non la **cassa**: 227 duplicati li ferma prima, questi 4 no. |
| **2** | `payment.request_links` 728 e 729: `booking_id` NULL, `stripe_session_id` vuoto, 35,00 € ciascuna, create il 2026-05-27 a due secondi di distanza. | Nessun `exclude_row_if` sulla mancanza di `booking_id`; la catena di ripiego per lo stabilimento non ha nulla su cui lavorare. |
| **1** | `notification.user_channel_preferences`: preferenza di uno degli 11 utenti del gruppo A di **[A6](#a6-i-20-account-applicativi-non-migrati--chiusa)**. | Cascata: rientra da sé assegnando il ruolo `consumer`, o resta fuori senza danno. |

**Soluzione, e perché non serve chiedere niente.**

```yaml
# payment_request_links.yaml
exclude_row_if: "not booking_id"
```

e, per i "mi piace", estendere al `bathHouseId` il controllo di esistenza che il transform già fa
sull'utente. Non c'è nulla da decidere perché **non c'è niente da recuperare**: un link di pagamento
senza prenotazione e senza sessione Stripe non ha nulla da incassare, e un "mi piace" verso una cassa
inesistente non ha un oggetto. Il valore è tutto nel **conteggio**: 306 errori di cui 7 non sono
errori rendono l'intero numero meno credibile, e chi lo guarda fra sei mesi non saprà quali 7
ignorare. Una riga di YAML compra un numero di cui ci si può fidare.

> Da guardare a parte, come segnale — e questo sì che è una domanda, se si vuole: **due richieste di
> pagamento generate a due secondi di distanza senza prenotazione** somigliano a un doppio invio
> dall'interfaccia. Vale la pena chiedersi se il meccanismo che le ha prodotte sia ancora attivo. Ma
> è indipendente dalla chiusura di questa voce.

---

## B4. Le partizioni finiscono a fine 2030

**Cos'è.** `booking.resource_consumptions` e `booking.resource_availability_daily` sono partizionate
per anno solare. Verificate: `_y2021` … `_y2030` più `_default`.

**Esempio concreto del guasto che arriverà.** Grazie alla partizione di default **non ci sarà nessun
errore**: nel 2031 le righe finiranno lì dentro in silenzio, perdendo il beneficio per cui il
partizionamento esiste. E qui scatta la parte cattiva: finché la default contiene righe di un anno,
**la partizione di quell'anno non è più creabile**.

```
ERROR:  updated partition constraint for default partition
        "resource_consumptions_default" would be violated by some row
```

Rimediare a quel punto richiede una finestra di manutenzione con lock su una tabella da 3,8 milioni
di righe.

**Soluzione, e perché non serve chiedere niente.** Due istruzioni per anno e per tabella, additive,
reversibili, senza riscrivere una riga di dati:

```sql
CREATE TABLE booking.resource_consumptions_y2031
  PARTITION OF booking.resource_consumptions
  FOR VALUES FROM ('2031-01-01') TO ('2032-01-01');
```

Una partizione vuota in più non costa niente: nessuno spazio significativo, nessun impatto sul piano
di esecuzione. Non c'è alcun compromesso da valutare, quindi non c'è niente da chiedere.

**Perché estendere le partizioni e non (ancora) scrivere il job.** Il promemoria oggi è
[`docs/06-partizioni-manutenzione.md`](../../progettazione/06-partizioni-manutenzione.md), con la query di
controllo del margine e le soglie. Con due tabelle, un job annuale è codice che gira per anni senza
fare nulla e che nessuno ricorderà di aver scritto quando servirà modificarlo; alla terza tabella il
conto si ribalta. Allungare la scadenza è l'intervento che compra il massimo di sicurezza col minimo
di codice — il rischio vero non è il 2031, è che la **partizione di default trasformi un errore
rumoroso in un degrado silenzioso**.

---

## B5. 11 stampanti perse per una porta malformata — proposta annullata

**Cos'è.** Delle 15 configurazioni di stampante fiscale scartate, **11 sono di un modello
supportato** e si perdono solo per un campo compilato male. La regola della spec è:

```python
model not in ('FP-81','FP-90','RCH','TM-T88','TM-T20','ONLINE')
  or (port is not None and port.count('.') == 3)
```

**Esempio concreto.**

| Righe | Modello | Perché scartate |
|---:|---|---|
| **11** | `TM-T20` — **un modello supportato** | la colonna `port` contiene un **indirizzo IP**. Esempio: id 36, `EPSON TM-T20II`, `ip = 192.168.0.188`, `port = 192.168.0.1` |
| 4 | `dddd`, `Asdax`, `fi`, `tets` | modelli inesistenti → **confermati di prova**, restano fuori (vedi **[A6](#a6-i-20-account-applicativi-non-migrati--chiusa)**) |

Nella `port` è finito un secondo indirizzo — `192.168.0.1` somiglia al gateway della rete — mentre
l'IP corretto è già nella sua colonna.

### ❌ Non si fa, e la ragione era già scritta nella spec

Andando ad applicarla, la proposta si è rivelata **sbagliata**, e la spec
`fiscalprint_printer_configs.yaml` aveva già valutato e respinto esattamente questa strada:

> *«`port` con un IP dentro non è recuperabile: nessun numero di porta reale nel dato legacy, e
> `connection_kind=tcp` con `port NULL` viola il vincolo `ck_fiscalprint_printer_configs_connection`
> a target. Indovinare una porta standard (es. 9100) scriverebbe un dato plausibile ma non
> verificato — stessa filosofia "nessun default silenzioso": si scarta, va reinserita manualmente da
> chi ha accesso fisico alla stampante per leggere la porta vera.»*

**Dove avevo sbagliato.** Avevo scritto che «il valore giusto non va indovinato, perché l'IP sta già
nella colonna `ip`». Vero per l'**host**, falso per la **porta**: quella non esiste da nessuna parte
nel dato legacy, e 9100 sarebbe un'invenzione. Ho applicato il criterio giusto — si corregge la
forma, non si inventa il contenuto — al campo sbagliato.

E il criterio, letto bene, sostiene la spec e non me: una stampante fiscale configurata su una porta
inventata **non stampa**, e lo si scopre alla cassa con un cliente davanti. Una configurazione che
manca la si vede al primo collaudo. Vale qui la stessa regola dei settori: un dato mancante fallisce
in modo rumoroso, un dato sbagliato in silenzio.

**Cosa resta da fare, quindi:** le 11 stampanti vanno **reinserite a mano** da chi può leggere la
porta vera sull'apparecchio. Non è lavoro da ETL, è una voce di collaudo del cutover — e il vero
recupero è che ora si sa quali sono (id 36 e le altre dieci con modello `TM-T20`), invece di
cercarle fra 58 configurazioni.

---

## B8. `docs/80` fa decorrere sedici righe da un evento che non esiste

**Cos'è.** Deciso in **[A1](#a1-tre-colonne-che-mancano-per-eseguire-la-retention--d-65--chiusa)**:
il concetto di "cessato" non esiste, e la decorrenza è **fine dell'ultimo abbonamento + 24 mesi**.
`docs/80-retention.md` cita ancora la cessazione in **sedici righe** delle proprie tabelle — fra
cui i 10 anni sui dati del titolare, lo snapshot della configurazione cancellato dopo 2 anni, la
revoca del collegamento Stripe Connect, la riduzione della scheda pubblica.

**Soluzione, e perché non serve chiedere niente.** La decisione e il numero ci sono entrambi: resta da
riscrivere quelle sedici righe, aggiungere il parametro
`RETENTION_PROPERTY_TERMINATION_GRACE_MONTHS = 24` e la query che ricava la data:

```sql
SELECT property_id, max(coalesce(ends_at, suspended_at)) AS cessazione_presunta
  FROM identity.account_subscriptions
 WHERE deleted_at IS NULL
 GROUP BY property_id
HAVING max(coalesce(ends_at, suspended_at)) < now() - interval '24 months'
```

**Perché conta più di un ritocco di documentazione.** `docs/80` non è un commento: è la specifica da
cui nascono i 17 job di retention e le 44 righe seminate in `privacy_retention_policies`. Finché
quelle regole si appoggiano a un evento che nessuno scriverà mai, i job che le eseguono **non
partiranno** — e non con un errore, ma restando fermi in silenzio: il modo peggiore, perché somiglia
a "non c'era niente da cancellare".

---

## B9. In AWS l'app non troverebbe la propria connection string

**Cos'è.** `infra/summerbooking-aws.yaml` inietta nel container `Database__Host` (l'endpoint di RDS
Proxy), `Database__Port`, `Database__Name` e, da Secrets Manager, `Database__Username` e
`Database__Password`. Il codice legge però **`ConnectionStrings:SummerBooking`**, e
`Boot/appsettings.json` non la contiene: esiste solo in `appsettings.Development.json`, puntata a
`localhost`. **Nessuno legge quei cinque valori.**

**Soluzione, e perché non serve chiedere niente.** Comporre la stringa dai pezzi quando la chiave
completa manca, nello stesso punto che già decide il tetto del pool:

```csharp
// BuildingBlocks.Infrastructure/Persistence/ModuleConnectionStrings.cs
var completa = configuration.GetConnectionString(ConnectionStringName);
if (!string.IsNullOrWhiteSpace(completa)) return completa;   // ha sempre la precedenza

var builder = new NpgsqlConnectionStringBuilder
{
    Host     = configuration["Database:Host"],
    Port     = int.TryParse(configuration["Database:Port"], out var p) ? p : 5432,
    Database = configuration["Database:Name"],
    Username = configuration["Database:Username"],
    Password = configuration["Database:Password"],
    SslMode  = SslMode.Require,   // il proxy ha RequireTLS: true
};
```

**✅ Strada scelta il 2026-09-30: (a), il codice compone la stringa — e in locale non cambia nulla.**
Era la condizione posta, ed è soddisfatta dalla prima riga: `GetConnectionString` viene letta
**prima**, quindi `ConnectionStrings:SummerBooking` in `appsettings.Development.json` continua a
vincere su tutto. In sviluppo non si tocca niente e non si aggiunge niente; i `Database:*` entrano in
gioco **solo** quando quella chiave manca, cioè in AWS. Vale anche per la variabile d'ambiente
`ConnectionStrings__SummerBooking`, che resta la via di sovrascrittura documentata in
`docs/42-migration.md` per le migration.

Perché (a) e non (b) — mettere la connection string intera in Secrets Manager: con (a) il segreto
resta **la sola password**, mentre con (b) diventerebbe segreto anche l'host, la porta e il nome del
database, che segreti non sono. Un segreto che contiene cose non segrete si finisce per stamparlo nei
log.

`SSL Mode=Require` non è un extra: il proxy è dichiarato `RequireTLS: true`, quindi senza TLS rifiuta
la connessione. Da verificare in fase di applicazione se serve `Trust Server Certificate=true` o la
CA di RDS montata nell'immagine: dipende da quale certificato presenta il proxy, e si vede solo
provando.

**Perché è un difetto e non una configurazione mancante.** L'intento è già dichiarato
nell'infrastruttura — quei cinque valori sono lì di proposito, con la password presa da Secrets
Manager come si deve. Manca solo il pezzo di codice che li raccoglie, e la mancanza si manifesta nel
modo più costoso possibile: **al primo avvio in AWS**, non in sviluppo, dove `appsettings.Development.json`
copre tutto. È il difetto più concreto di questa lista: non perde dati, ma ferma un deploy.

---

## B10. «Vince il periodo più breve» non è scritta da nessuna parte

**Cos'è.** Il committente ha confermato che un giorno può costare diversamente dal periodo che lo
contiene — un giorno di festa dentro la bassa stagione — e che **prevale il prezzo del singolo
giorno**. È una regola di calcolo, non un vincolo di database, e in nessun file del progetto è
scritta.

**Esempio concreto.** *Bagno La Vela*: `Bassa Stagione` dal 30 aprile 2025 al 29 settembre 2026
contiene `Bassa Stagione` del solo 30 aprile 2026, e in un altro caso una `Alta Stagione` di un solo
giorno cade dentro un periodo di bassa. Per il 30 aprile 2026 esistono due periodi validi: il motore
deve scegliere **quello più breve**.

**Soluzione, e perché non serve chiedere niente.** La regola c'è: va scritta nella documentazione dei
prezzi e implementata come ordinamento nella risoluzione del periodo
(`ORDER BY (ends_on - starts_on) ASC LIMIT 1` fra i periodi che coprono la data).

**Perché va scritta adesso e non quando si farà il motore.** Un motore che scegliesse «il primo
trovato» o «l'id più basso» **non sbaglierebbe in modo visibile**: darebbe un prezzo plausibile,
diverso da quello giusto, su un giorno di festa — cioè sul giorno che frutta di più. E vale doppio per
il motore di prezzo **offline** (`docs/19-frontend-offline.md`), che deve dare lo stesso risultato del
server: due implementazioni della stessa regola non documentata divergono con certezza.

---

## B11. Creare i settori dai nomi citati nei listini

**Cos'è.** Deciso in **[A7](#a7-i-nomi-dei-settori-che-non-coincidono--chiusa-si-creano-dai-listini)**: il listino è la lista dei
settori. Un nome citato che nell'anagrafica non esiste va **creato**, non cercato.

**Esempio concreto.** *La Baia Blu* cita `Terrazza Santa Teresa`, `Fila 1-2`, `Dalla 3^ fila in poi`
e `Piscina - Solarium Grecale - Terrazze Belvedere`: quattro nomi, 64 voci prezzo, oggi tutte perse.
Con la creazione rientrano tutte e quattro senza che nessuno debba ricordarsi cosa fosse la terza
fila nel 2022.

**Soluzione, e perché non serve chiedere niente.** `catalog.map_sectors` richiede solo
`property_id`, `name` e `sort_order`: il resto ha un default o è annullabile, quindi un settore si
crea dal solo nome. L'insieme dei settori di uno stabilimento diventa l'unione fra quelli di
`sectors_rows` e quelli citati da `price_lists.mainPrices`, e i mancanti si creano in coda.

Recupera **275 righe**: 263 su `pricing_price_list_sector_prices`, 11 su `pricing_discount_sectors`,
1 su `property_online_stay_limits`.

**Va fatto insieme a un vincolo che oggi manca.** `map_sectors` non ha un `UNIQUE (property_id,
name)`: senza quello, una seconda rimigrazione creerebbe i settori una seconda volta. Il vincolo va
aggiunto **nella stessa modifica**, non dopo: dopo significa prima ripulire i doppioni che si sono
creati nel frattempo.

**Perché creare e non far compilare.** La strada alternativa era il documento di verifica, e per un
po' è stata quella giusta: 263 nomi, 24 stabilimenti, una domanda per riga. Il committente ha però
stabilito che il listino **è** la fonte dei settori, e questo non è un ripiego per evitare il lavoro
manuale: è il modello reale del vecchio sistema, dove il nome nel listino era il settore. Creare
quei settori non inventa nulla — **rende esplicito un dato che c'era già**, e che l'anagrafica
aveva perso per un baco di allineamento oggi corretto.

### ✅ Visibili, deciso il 2026-09-30 — con un vincolo da conoscere

I settori creati dai listini sono **visibili**: saranno quelli i settori del nuovo sistema. Si creano
**48 settori nuovi su 24 stabilimenti**, che si aggiungono ai 638 legacy.

**Ma i settori legacy non si possono togliere**, e questo va detto perché la formulazione «non più i
settori legacy» lo lascerebbe pensare. Il motivo non è una preferenza, è la mappa:

| | |
|---:|---|
| **140** dei 635 settori migrati | non sono citati da **nessun** listino |
| **4.040** | i posti (ombrelloni) che quei 140 settori reggono, via `catalog.map_spots.sector_id` |

Esempi: *Bagno 5 Gino* → `Altre file`, **230 posti**; *Bagno Nettuno* → `Fila unica`, **219 posti**;
*Spiaggia 91* → `3 Settore`, **151 posti**. Cancellarli perché nessun listino li nomina staccherebbe
4.040 ombrelloni dalla spiaggia su cui stanno: un settore non è solo un'etichetta di prezzo, è
**dove sono fisicamente i posti**.

Quindi la lettura operativa è: **il listino smette di essere filtrato dall'anagrafica** — è questo
che causava la perdita delle 263 righe — e i suoi nomi diventano settori a pieno titolo. L'insieme
finale è l'unione dei due, tutta visibile. Chi vorrà ripulire lo farà nel nuovo gestionale, dove i
settori senza posti e senza prezzi si vedono a occhio; farlo in migrazione significherebbe decidere
al posto suo su 4.040 ombrelloni.

> Da notare, perché conferma che quei nomi sono veri: fra i 140 settori non citati ricorrono
> `Altre file`, `Fila unica`, `Dalla 3ª Fila in poi` — **gli stessi nomi** che altrove compaiono come
> orfani nei listini. Lo stesso vocabolario, le stesse file: non sono stringhe sporche, è il modo in
> cui gli stabilimenti chiamano le proprie zone.

---

## B13. `is_staff` è `true` per tutti i ruoli, e per due non dovrebbe

**Cos'è.** `identity.access_roles` ha `is_staff = true` su **tutti e tredici** i ruoli. La scheda
[`identity_access_roles.md`](../../analisi-post/db/identity/identity_access_roles.md) prescrive
`false` per due: **`systemadmin`** e **`consumer`**. Gli altri undici coincidono.

**Perché non è un dettaglio.** Il significato di quel flag, dalla scheda stessa: *«usato in
validazione (`is_staff = true` ⇒ `identity_access_users.property_id` obbligatorio)»*. Ma nel database
ci sono **6.012 utenti con ruolo `consumer` e nessuno stabilimento**, perché un utente dell'app non ha
uno stabilimento — è la normalità, non l'eccezione. Con `is_staff = true` su `consumer`, quella
regola di validazione, **appena verrà implementata**, dichiarerebbe invalidi 6.012 utenti legittimi.

Oggi non succede niente perché la regola non è ancora scritta in codice: non esiste alcun `CHECK` su
`identity.access_users` e `property_id` è annullabile. È un difetto **latente**, che si manifesterà
nel momento in cui qualcuno implementerà la validazione fidandosi del dato.

**La decisione di [A6](#a6-i-20-account-applicativi-non-migrati--chiusa) lo rende più urgente:** ai
11 utenti senza stabilimento viene ora assegnato `consumer`, quindi si aggiungono a quei 6.012
invece di restare fuori. La scelta è giusta, ma poggia su un flag che dice il contrario.

### A cosa serve quel flag, e perché proprio quei due devono essere `false`

`is_staff` separa chi **lavora in uno stabilimento** da chi non ci lavora. I due ruoli che devono
essere `false` lo sono per la stessa ragione letta da due direzioni opposte:

- **`systemadmin` sta sopra** gli stabilimenti: è un ruolo di piattaforma, non appartiene a nessuno
  stabilimento in particolare;
- **`consumer` sta sotto**: è il cliente finale che prenota dall'app, e non appartiene a uno
  stabilimento, li usa.

Gli undici in mezzo — `manager`, `cashoperator`, `lifeguard`, `waiter`… — sono personale, e per loro
`property_id` obbligatorio è giusto: un bagnino senza stabilimento non significa niente.

### I tre modi in cui questo si presenterà, in ordine di quanto costano

**1. Nuove registrazioni che falliscono in produzione — il più probabile e il più costoso.** Qualcuno
implementa la validazione fidandosi del dato:

```csharp
if (role.IsStaff && user.PropertyId is null)
    throw new ValidationException("Lo stabilimento è obbligatorio.");
```

Con `consumer` marcato `is_staff = true`, **ogni nuova registrazione dall'app fallisce**, perché un
cliente non ha uno stabilimento. E il messaggio d'errore punterà sull'utente — «lo stabilimento è
obbligatorio» — mentre la causa è una riga di seed: si cercherà il difetto nel modulo di
registrazione, che è corretto. Lo stesso vale per la creazione di un `systemadmin`.

**2. Un `CHECK` aggiunto per davvero — il caso fortunato.** Se invece di codice qualcuno scrive il
vincolo nel database, la migration **fallisce subito** sulle 6.012 righe esistenti. Rumoroso,
immediato, mezz'ora per capirlo. È l'esito migliore dei tre, e non è quello su cui contare.

**3. Il flag usato per decidere chi è "staff" — il caso silenzioso.** `is_staff` è un nome che invita
a usarlo per altro: mostrare o no l'accesso al gestionale, contare le utenze di personale, alimentare
un conteggio di licenze. In quel caso **6.012 clienti verrebbero trattati come personale** e non
comparirebbe nessun errore: un numero sbagliato in una schermata, o in una fattura, che nessuno
mette in dubbio perché viene dal database.

**E c'è un quarto esito, il peggiore: che venga "sistemato" al contrario.** Chi trovasse 6.012
consumer senza stabilimento potrebbe concludere che la regola è troppo stretta e **renderla
facoltativa per tutti**. Così si perde la garanzia utile — che un membro del personale sia sempre
legato a uno stabilimento — per aggiustare un dato sbagliato. Un flag errato non resta fermo: spinge
verso l'indebolimento di una regola giusta.

### La causa: `HasDefaultValue(true)` su una colonna che viene anche seminata

Il seed **non è sbagliato.** `RoleConfiguration.cs` dice la cosa giusta:

```csharp
new { Id = 1L, Code = "systemadmin", IsStaff = false, ... },
new { Id = 3L, Code = "consumer",    IsStaff = false, ... },
```

Sbagliata è la colonna, poche righe sopra nello stesso file:

```csharp
builder.Property(x => x.IsStaff)
    .HasDefaultValue(true);      // <- qui
```

Quando EF Core semina con `HasData`, **omette dall'`INSERT` le colonne il cui valore coincide con il
default CLR del tipo** — per un `bool` è `false` — perché non sa distinguere «impostato a false» da
«non impostato». Nella migration `InitialCreate` si vede a occhio: la riga `superadmin`, che ha
`IsStaff = true`, elenca la colonna; le righe `systemadmin` e `consumer`, che hanno `false`, **non la
elencano**.

```csharp
// superadmin: la colonna c'è
columns: new[] { "id", "code", "created_at", "deleted_at", "is_staff", "sort_order", "updated_at" },
// systemadmin: la colonna non c'è  ->  vince il default del database, che è true
columns: new[] { "id", "code", "created_at", "deleted_at", "updated_at" },
```

Omessa la colonna, si applica il default dichiarato — `true` — e il `false` del seed evapora.

**Il difetto colpisce esattamente e soltanto quella colonna**, e si può prevedere guardando le altre
due bandiere della stessa tabella:

| Colonna | Default del modello | Default CLR | Esito |
|---|---|---|---|
| `is_assignable_via_panel` | `false` | `false` | **corretto per caso**: i `false` omessi ricevono `false` |
| `sort_order` | `0` | `0` | **corretto per caso**, stessa ragione |
| **`is_staff`** | **`true`** | `false` | **sbagliato**: i `false` omessi ricevono `true` |

Non è sfortuna: è l'unica colonna il cui default dichiarato **differisce** dal default CLR. Gli
undici valori giusti sono giusti perché erano `true`, non perché qualcuno li avesse verificati.

> **È lo stesso gotcha già documentato in questo progetto** per le colonne enum native
> (`_dotnet-template.md` §4: `HasDefaultValueSql`, mai `HasDefaultValue`). Stessa famiglia, altra
> faccia: **non usare `HasDefaultValue(x)` su una colonna che viene anche seminata**, quando `x` non
> è il default CLR del tipo. Per una tabella di riferimento che semina tutte le proprie righe il
> default non serve a niente — e in cambio fa questo.

### La soluzione, in tre passi — ✅ approvata il 2026-09-30

**1. Togliere il default dal modello.** `access_roles` è una tabella di riferimento: semina tutte le
proprie tredici righe, quindi un default non copre nessun caso reale e apre questo. Si rimuove
`.HasDefaultValue(true)` da `IsStaff` — e per coerenza si valuta di farlo anche sulle altre due, che
oggi sono corrette solo per coincidenza.

**2. Generare la migration — e qui mi ero sbagliato.** Avevo scritto che EF non avrebbe prodotto
nulla per i dati, perché lo snapshot registra già `IsStaff = false`, e che serviva un `UPDATE` a
mano. **Alla prova non è così:** togliere il default cambia il modo in cui EF *materializza* il
seed — con un default dichiarato ometteva la colonna, senza default la include — quindi il motore di
diff vede una differenza e genera da sé le due righe giuste:

```csharp
migrationBuilder.UpdateData(schema: "identity", table: "access_roles",
    keyColumn: "id", keyValue: 1L, column: "is_staff", value: false);
migrationBuilder.UpdateData(schema: "identity", table: "access_roles",
    keyColumn: "id", keyValue: 3L, column: "is_staff", value: false);
```

Resta vero il punto che contava: il passo 1 da solo non basta. È il passo 1 a **provocare** questo
`UpdateData`, e l'ordine generato è quello giusto (prima l'`AlterColumn` che toglie il default, poi
l'aggiornamento delle righe).

> **I passi 1 e 2 non sono alternative: servono entrambi, in quest'ordine.** Il primo disarma la
> trappola, il secondo ripara il danno che ha già fatto. Solo il primo: la colonna smette di essere
> pericolosa, ma il database resta con `true` su due righe, perché — come si vede al passo 2 — EF non
> genera nulla. Solo il secondo: le due righe tornano giuste, e alla prossima riga di riferimento
> seminata con `false` il difetto si ripresenta identico.
>
> **Il passo 3 non fa parte della correzione**: è ciò che impedisce che succeda di nuovo. Senza, il
> difetto è comunque risolto; ma la trappola è una proprietà del modo in cui EF semina, non di questa
> tabella, e scatterà alla prossima occasione.

### Passo 3: il controllo, e perché nessuna delle forme basta da sola

La domanda era se basti un controllo nel codice. La risposta è che **un controllo sull'utente non
c'entra**: il codice applicativo è corretto, la divergenza è fra ciò che il modello dichiara e ciò
che il database contiene. Ci sono tre punti dove si può guardare, e **coprono buchi diversi**.

**3a — Un test sul modello, senza database. È quello che consiglio.**

La regola da verificare non riguarda i ruoli: riguarda il meccanismo. *Nessuna proprietà seminata con
`HasData` può dichiarare un default diverso dal default CLR del proprio tipo.* Si legge dal modello
EF senza connettersi a niente:

```csharp
foreach (var entita in context.Model.GetEntityTypes())
foreach (var riga in entita.GetSeedData())          // le righe di HasData
foreach (var prop in entita.GetProperties())
{
    var dichiarato = prop.GetDefaultValue();        // null se non c'è
    // fallisce se il default dichiarato non è il default CLR: seminare
    // quel valore lo farebbe evaporare, esattamente come per is_staff
}
```

Sta nel progetto che esiste già (`ArchitectureTests/`, che referenzia tutti gli 11 moduli, xunit,
nessun database), costa poco e **avrebbe fallito nel momento in cui `HasDefaultValue(true)` è stato
scritto accanto a un seed `false`** — non mesi dopo. E vale per gli 11 moduli, non per questa
tabella: è la stessa forma del gotcha sugli enum, quindi la stessa classe di difetti.

**3b — Confrontare riga per riga il seed col database. Copertura più ampia, ma oggi costa molto.**

Rileggere le righe seminate dal database e confrontarle con `GetSeedData()` cattura **qualunque**
scarto fra codice e dati, non solo quello causato dai default: una migration corretta a mano, un
`UPDATE` fatto in produzione, un seed applicato a metà. Il problema è che serve un database, e oggi
**non esiste alcun test di integrazione** in questo backend — niente Testcontainers, niente
`WebApplicationFactory`: `ArchitectureTests` è l'unico progetto di test. Introdurlo per questa regola
sola è sproporzionato; quando ci sarà per altri motivi, questa è una delle prime da aggiungerci.

**3c — La verifica semantica sui dati, come query di controllo dopo la migrazione.**

*Nessun ruolo con `is_staff = true` può avere utenti senza `property_id`.* Non guarda il codice:
guarda se il significato regge sui dati veri. Su `consumer` fallirebbe con 5.987 righe, impossibile
non vederlo.

E qui l'avvertimento dato prima: **oggi non passerebbe nemmeno dopo la correzione.** Restano fuori
regola `superadmin` (5 utenti su 227) e `websiteadmin` (1 su 1) — tutti account interni o di prova
(`adityapolo@yopmail.com`, `admin@admi.com` con nome «ASD ASD», `j@h.com`, `webadmin@summer.com`).
Va scritta **nominando quelle sei eccezioni una per una**, non con una soglia di tolleranza: un
controllo che accetta «qualche» violazione smette di essere un controllo, uno che le nomina si
accorge della settima.

**Perché non è una scelta fra i tre: hanno punti ciechi complementari.**

| Se sbaglia… | 3a | 3b | 3c |
|---|---|---|---|
| un default dichiarato diverso dal default CLR su una colonna seminata | **lo vede subito** | lo vede dopo | lo vede se produce dati incoerenti |
| una migration modificata a mano, o un `UPDATE` in produzione | non lo vede | **lo vede** | lo vede se incoerente |
| un valore sbagliato dichiarato **coerentemente** nel seed e nel database | non lo vede | non lo vede | **solo questo lo vede** |

L'ultima riga è la più interessante: se qualcuno decidesse per sbaglio che `consumer` è personale e
lo scrivesse sia nel seed sia nel database, 3a e 3b sarebbero soddisfatti — codice e dati
concordano — e solo 3c si accorgerebbe che 5.987 utenti contraddicono quella parola.

**In pratica:** 3a adesso, perché è mezza giornata e sta dove già stanno i test; 3c come voce della
verifica post-migrazione, che è una query e va eseguita comunque; 3b quando esisterà un progetto di
integrazione per altri motivi.

### Il problema è nella migrazione o anche dopo?

**Non è nella migrazione, è tutto dopo.** Vale la pena essere precisi, perché cambia l'urgenza.

| | |
|---|---|
| **Durante la migrazione** | nessun effetto. L'ETL Python **non legge** `is_staff` (verificato: la stringa non compare in tutta `utility/01-migrazione-test/`): risolve `role_id` da `user_roles` e scrive. Le 5.987 righe `consumer` senza stabilimento sono state scritte e sarebbero state scritte comunque |
| **Dopo la migrazione** | tutto l'effetto. Il flag conta solo quando qualcuno lo **legge**, e oggi nessuno lo legge: nel backend `IsStaff` esiste come proprietà dell'entità e nel seed, e basta |

Quindi: la migrazione **non è a rischio** e non c'è nulla da rifare per causa sua. Ma la
**migration** — il file `InitialCreate` — è il punto in cui il valore sbagliato è entrato, e ci
resterà finché non lo si corregge.

C'è una conseguenza che rende la correzione più semplice di quanto sembri: **non ci sono dati utente
da riparare**. Le 5.987 righe `consumer` senza stabilimento sono corrette così come sono; risultano
«invalide» solo se lette attraverso un flag sbagliato. Si correggono **due righe di una tabella di
riferimento** e 5.987 righe ridiventano valide retroattivamente, senza toccarle.

Il momento buono è adesso per una ragione sola: finché nessuno legge quel flag, il costo è due righe.
Appena qualcuno ci costruisce sopra — la validazione alla registrazione, la distinzione fra accesso
al gestionale e accesso all'app, un conteggio di utenze di personale — il costo diventa due righe più
tutto ciò che è stato costruito sull'assunzione sbagliata, e il sintomo si presenterà lontano dalla
causa.

---

## B14. Recuperare l'email dalla colonna `username`

**Cos'è.** Quattro utenti hanno `email` vuota, ma **tre di loro hanno un indirizzo valido nella
colonna `username`** — il login legacy accettava entrambe le colonne, quindi per loro era un dato
di accesso normale, non un campo di scorta.

| id | `username` | `email` | Nome |
|---|---|---|---|
| 1629 | `claudia197421@gmail.com` | *(vuota)* | Claudia |
| 4488 | `elena.58@alice.it` | *(vuota)* | — |
| 5285 | `madreterra.roma@gmail.com` | *(vuota)* | — |
| 3788 | *(nulla)* | *(nulla)* | — |

**Soluzione, e perché non serve chiedere niente.** Se `email` è vuota e `username` ha la forma di un
indirizzo, si usa `username` come email. La forma si verifica, non si indovina: non c'è alcun
margine di interpretazione fra `claudia197421@gmail.com` e una stringa qualsiasi.

**Il dettaglio che conta nell'applicarla.** La spec porta `exclude_row_if: "not email"`, che scatta
**prima** di qualsiasi trasformazione: se si lascia così, la riga viene scartata senza che nessuno
guardi lo `username`. Il recupero va quindi nel `custom_row_transform`, che valorizza `email` a
monte — non toccando quella condizione, che per il quarto utente resta giusta.

**Perché tre e non quattro.** Il quarto (3788) entra con **Apple** e ha entrambe le colonne nulle.
L'accesso Apple permette di nascondere il proprio indirizzo, e infatti l'utente 3787 — stesso
giorno, stesso metodo — ha un `@privaterelay.appleid.com`. Qui non c'è niente da prendere: non è
un dato nella colonna sbagliata, è un dato che non esiste.

**✅ Deciso il 2026-09-30: la riga Apple resta fuori**, e il dubbio sul meccanismo diventa una nota
per lo sviluppo del backend — vedi **[B15](#b15-nota-per-il-backend-lidentità-apple-può-arrivare-senza-email)**.

---

## B15. Nota per il backend: l'identità Apple può arrivare senza email

**Cos'è.** Non è un difetto della migrazione: è una cosa che il nuovo backend deve prevedere, e che
si è vista sui dati legacy. L'utente 3788 si è registrato con **Apple** e ha `email` e `username`
**entrambi nulli**; l'utente 3787, stesso giorno e stesso metodo, ha un regolare
`nwwrg6b747@privaterelay.appleid.com`.

**Perché succede.** «Accedi con Apple» permette di **nascondere il proprio indirizzo**: Apple genera
un alias `@privaterelay.appleid.com` e lo comunica all'applicazione **solo al primo accesso**. Se
quella prima risposta non viene salvata, agli accessi successivi non torna più — e l'account resta
senza email per sempre. Il legacy salvava l'intero JWT Apple in `appleId` (la migrazione ne estrae
il claim `sub`), quindi l'identificativo c'era; l'indirizzo, in quel caso, no.

**La riga resta fuori** (deciso il 2026-09-30): senza email non è migrabile, ed è una sola.

**Cosa deve fare il backend nuovo.** Tre cose, e la terza è quella che non si recupera più se si
sbaglia:

1. **Trattare l'email come non garantita** sul percorso Apple: l'utente si identifica con il claim
   `sub`, che è stabile e sempre presente, non con l'indirizzo. Se lo schema richiede l'email come
   chiave unica di accesso — e oggi la richiede — il percorso Apple va progettato sapendo che quel
   campo può mancare, non scoprendolo alla prima registrazione.
2. **Chiedere l'indirizzo all'utente** quando Apple non lo fornisce, invece di scrivere una riga
   incompleta: è l'unico momento in cui si può ancora ottenere.
3. **Salvare la prima risposta di Apple.** L'alias arriva una volta sola: se lo si perde, non esiste
   modo di richiederlo. Questo è esattamente ciò che è andato storto per la riga 3788, e il motivo
   per cui vale la pena scriverlo qui invece di considerarlo un caso isolato.

> **Da guardare anche sull'esistente**, e non è un caso teorico: se l'app attuale è ancora in uso
> mentre il nuovo sistema viene costruito, ogni nuova registrazione con Apple può produrre un altro
> account senza email. Una riga oggi, ma il meccanismo che l'ha prodotta è ancora lì.

# Parte C — Le modifiche applicate e le decisioni (era `docs/_temp4.md`, poi `_temp2.md` Parte 6)

> **Spostata qui il 2026-10-03**, quando `docs/_temp2.md` e' stato smontato e distribuito. Il
> contenuto nasce come `docs/_temp4.md` (elenco operativo del 2026-09-30), e' diventato la Parte 6 di
> `_temp2.md` il 2026-10-02 e vive adesso nell'inventario del lavoro, che e' il suo posto: Parte A le
> decisioni da prendere, Parte B il lavoro puro, **Parte C** le diciannove voci M e le nove decisioni
> D di quella seconda tornata. L'introduzione di `_temp4` diceva «non si sovrappone a
> _mancanti-da-sistemare.md: i due elenchi si sommano» — ed e' esattamente il motivo per cui ora sono
> nello stesso file.
>
> **Il censimento delle colonne vuote NON e' qui**: sta in
> [`_colonne-vuote.md`](_colonne-vuote.md) (era `docs/_temp3.md`), che dice *quali* colonne sono vuote, *perche'* lo sono e
> se sia un errore. Qui c'e' il lavoro che e' stato fatto su di esse.


> **Questa parte ha assorbito `docs/_temp4.md`, che è stato cancellato il 2026-10-02.** Quel
> documento era l'elenco operativo nato dalla Parte 2: diciannove voci di lavoro (**M1–M19**) e nove
> decisioni (**D1–D9**). I rimandi sparsi nel repository — «voce M13», «decisione D9» — puntano qui, e la numerazione è conservata per questo.
>
> **Quale dei due documenti era più aggiornato.** Sulle *diagnosi* e sulle *decisioni*, `_temp4`:
> erano il suo contenuto proprio e non esistevano altrove. Sullo *stato del database*, questo
> documento: l'intestazione di `_temp4` diceva «niente è stato eseguito» e «i numeri di righe vanno
> rimisurati», e a quel punto il DB dev era quello della corsa del 2026-09-23. Entrambe le cose sono
> superate — tutto è stato eseguito, e i numeri delle Parti 1 e 2 di
> [`_colonne-vuote.md`](_colonne-vuote.md) sono misurati sulla corsa a
> zero errori del 2026-10-02.

## C.1 Le diciannove voci di lavoro

Tutte applicate, con tre eccezioni segnalate nella colonna «esito». I numeri sono quelli finali,
rimisurati dopo la corsa del 2026-10-02 — non quelli del 2026-09-28, che `_temp4` stesso avvertiva di
dover rifare.

| | Cos'era | Esito |
|---|---|---|
| **M1** | Sei spec cercavano la namespace `id_map` `bookings`, che **nessuno popola**: `booking.reservations` ha `preserve_legacy_id: false` e registra le proprie corrispondenze con altri nomi. Il lookup non trovava mai niente, accodava un relink, e quel relink non si risolveva né allora né mai: **33.190 promemoria morti** | ✅ applicata |
| **M2** | `relink_pending()` non aveva **mai girato**: 36.880 promemoria tutti con `resolved_at IS NULL`, di cui 3.645 già risolvibili | ✅ applicata, e resa **passo obbligatorio** con l'esito nel report. Nella corsa finale: 3.645 risolti, 2.173 dichiarati irrisolti |
| **M3** | `base_price` di testata: entrambi i rami di calcolo leggevano chiavi che nei dati non esistono — 2.915 righe | ✅ diventata una **rimozione** (D1). ⚠️ applicata prima solo lato backend: la parte ETL è stata completata il 2026-10-02, vedi §C.4 |
| **M4** | 840 nomi di listino sostituiti dal segnaposto tecnico `price-list-<id>`: per le righe `type='additional'` il nome sta in `details.name`, chiave non letta | ✅ applicata |
| **M5** | `fiskaly_line_item_id`: era una pezza del legacy, non un dato del dominio → **rimossa** dalle 4 tabelle di booking | ✅ applicata. ⚠️ ha lasciato una tupla disallineata, vedi §C.4 |
| **M6** | `payment.transaction_charges.connected_account_id` vuota: la sorgente legacy è l'id Stripe **testuale** e il lookup standard risolve solo per id legacy numerico | ✅ applicata come `aggregate_step` durevole: **29.892 righe** |
| **M7** | `fiskaly.receipt_logs.receipt_id` vuota benché gli id della fetta fiscale siano preservati 1:1 | ✅ applicata: **7.840 righe su 8.125**. Le 285 restanti puntano a 78 ricevute scartate dall'ETL — scriverle violerebbe la chiave esterna |
| **M8** | I relink verso id legacy inesistenti sparivano in silenzio | ✅ applicata: `_registra_relink_irrisolvibili()` li dichiara. **È il motivo per cui oggi vediamo i 2.173 irrisolti**, che prima c'erano e nessuno nominava |
| **M9** | La nota di `notification.deliveries.property_id` chiedeva una cosa impossibile («dedurlo dall'entità di origine», che nel log non esiste) | ✅ riscritta |
| **M10** | `captured_at` e `canceled_at` a `constant: null` **senza spiegazione**: alla rilettura sembravano una dimenticanza | ✅ documentate (le scrive il webhook Stripe a runtime) |
| **M11** | `booking.discounts.booking_line_id` era l'unica delle tre sorelle senza la nota che spiega il NULL | ✅ allineata |
| **M12** | Il censimento delle colonne vuote era una **scoperta** fatta mesi dopo; la stessa query a ogni corsa lo renderebbe una **regressione** | ✅ applicata: `scripts_colonne_vuote.py`. Esteso il 2026-10-02 a **quattro** casi (regressione / eliminata dallo schema / recuperata / copertura nuova) |
| **M13** | Annotare nelle schede le colonne volutamente NULL, perché l'informazione descrive il dominio e non la migrazione | ✅ applicata: **126 note**. La formulazione è stata corretta: affermava «il dato non esiste nel legacy» anche dove esiste |
| **M14** | `identity.account_license_plan_translations.description` | ✅ **si chiude senza lavoro** (D6): resta NULL, basta annotarlo |
| **M15** | Sbloccare `switchboard_voice_call_records` con `outcome` placeholder, e agganciare i 7.355 `call_record_id` | 🔶 **solo in parte**: le 214 righe sono migrate, ma l'aggancio dei `call_record_id` **non è realizzabile** — vedi §C.4 |
| **M16** | Nota nel codice di `Module.Switchboard`: `outcome`/`forward_result` storici sono placeholder, il nuovo codice **deve** valorizzarli con l'esito reale | ✅ applicata, in `VoiceCallRecord.cs` e nella scheda |
| **M17** | `notification.deliveries.property_id` dedotta da `emailTo` → `customers.propertyId` | ✅ applicata come `aggregate_step`: **141.686 righe su 200.884** (70,5%); il resto resta NULL, limite dichiarato |
| **M18** | `utility/02-migrazione-file/`: scarico da S3 legacy, metadati, travaso su bucket nuovo privato **e** copia locale | 🔶 **implementata e collaudata in prova** il 2026-10-01 (bucket di produzione in sola lettura, 142/142 file, 0 errori). Mancano la chiave AWS di sola lettura dedicata e la corsa completa sui 780.220 oggetti (→ la voce Q2 di [`_colonne-vuote.md`](_colonne-vuote.md)) |
| **M19** | Lo stesso digest SHA-256 era dichiarato con due tipi diversi (`bytea` in `catalog`/`switchboard`, `char(64)` in `fiskaly`/`notification`) | ✅ uniformato a `bytea`. La migration è **scritta a mano**: `ALTER COLUMN ... TYPE bytea` richiede `USING`, che `AlterColumn` di EF non genera |

## C.2 Le nove decisioni

Sei hanno una risposta, tre sono cadute. **Nessuna è rimasta aperta**, ed è il motivo per cui tre
domande di [`_colonne-vuote.md`](_colonne-vuote.md) sono state corrette il 2026-10-02: le stavo riponendo.

| | Domanda | Risposta | Conseguenza |
|---|---|---|---|
| **D1** | Il `base_price` di testata serve? | **No.** Il prezzo resta dove è sempre stato: per settore (`mainPrices[].value`) e per durata (`detailPrices[].*`), già migrati nelle tabelle di dettaglio | `base_price` **rimossa** da `pricing_price_lists` e `agreement_price_lists`; M3 diventa una rimozione |
| **D2–D4** | `lineItemId`: a quale tabella appartiene? vale il 2%? le voci hotel? | **Cadute**: la verifica di M5 ha stabilito che il campo era una pezza del legacy | La colonna è rimossa, le tre domande non hanno più oggetto |
| **D5** | Lo storico email senza property: dedurlo da `emailFrom`? | **No** — `emailFrom` ha 25 valori distinti e copre 346 righe su 200.884 (0,17%). Ma cercando la prova è emersa una strada che funziona: **`emailTo`** | Nasce M17 |
| **D6** | Servono le descrizioni dei piani di licenza? | **No, resta NULL per scelta.** La tabella la popola un seed EF, non l'ETL; non c'è nessuna descrizione da recuperare | M14 si chiude senza lavoro |
| **D7** | `switchboard_voice_call_records`: placeholder o tabella `pending`? | **Placeholder dichiarato.** I 214 record del centralino sono dati di **test** (presenti solo in dev, zero in produzione), quindi un placeholder non falsifica niente di reale | Nascono M15 e M16 |
| **D8** | `created_by_user_id`/`updated_by_user_id` storici: NULL o utente tecnico? | **NULL. Nessun utente tecnico di migrazione.** Resta da verificare a valle che nessuna schermata del gestionale mostri «creato da» come campo obbligatorio | Non genera lavoro: NULL è già lo stato |
| **D9** | I metadati dei file: ETL, job, o NULL? | **Una utility a sé.** Calcolarli richiede di scaricare ogni file da S3, lavoro di natura diversa dall'ETL | Nasce M18 |

## C.3 Le tre voci nate da questo lavoro

`_temp4` registrava tre questioni emerse durante il lavoro e non previste all'inizio. **Tutte e tre
sono state ereditate da documenti dedicati**, e questa è la ragione per cui `_temp4` poteva essere
cancellato senza perdere niente.

| | Cos'era | Dove vive adesso |
|---|---|---|
| **N1** | **La retention non toccava i file.** Le 44 policy implementate riguardano solo righe di database, e l'enum delle azioni (`delete_row \| archive_cold \| null_fields \| anonymize`) **non sa esprimere «cancella il file»**: ogni volta che una policy cancella la riga che referenzia un file, il file resta su S3 per sempre. Cinque tipi di file non erano coperti da nessun documento | **Non più aperta**: le sei domande sono state poste al titolare e [`83-retention-file.md`](../../progettazione/83-retention-file.md) ne registra le risposte (report del bagnino, fotografie, allegati, listini e menù in PDF, icone, e il principio generale). Le modifiche a database e il seed che le rendono eseguibili sono state **applicate il 2026-10-03** (sette migration, elenco in [`83-retention-file.md` §5](../../progettazione/83-retention-file.md)); l'inventario file per file in `utility/02-migrazione-file/README.md` §7.1 |
| **N2** | Il tipo di `checksum` non era uniforme fra le quattro `*media_assets` | Diventata la voce **M19**, applicata |
| **N3** | **Credenziali AWS in chiaro** in `back-pre/` e ACL `public-read-write` su tutti gli oggetti tranne l'audio del centralino | `utility/02-migrazione-file/README.md` §7.3: **deciso** che si ricarica tutto su un bucket nuovo con ACL privata. La rotazione delle credenziali resta un lavoro a sé, fuori da questo elenco |

## C.4 Le cose che la verifica ha smentito, e i difetti scoperti applicando

Questa sezione è la più utile delle sei, perché ogni voce è un errore che era già stato scritto da
qualche parte come se fosse vero.

**Quattro diagnosi smentite verificandole.**

1. **M15 punto 2 non è realizzabile.** Chiedeva di agganciare i 7.355 `call_record_id` alle 214
   righe del registro. Non esiste una chiave: `switchboard_call_register` ha quattro colonne — `id`,
   `from`, `to`, `datetime` — e nessun identificativo Telnyx; `switchboard_call_log` ha gli
   identificativi ma nessun riferimento al registro. Le due tabelle descrivono inoltre **gambe
   diverse** della stessa chiamata. L'unica correlazione possibile sarebbe chiamante + vicinanza
   temporale, e non è una chiave: con una finestra di 60 secondi si ottengono **9.118 coppie
   candidate per 7.355 righe**.
2. **M5 lasciava riferimenti in giro**: oltre alle sezioni `### fiskaly_line_item_id`, nelle schede
   c'erano blocchi di esempio .NET, due note di chiusura e tre righe di «Mapping dal legacy». Le
   ultime sono state **riscritte e non cancellate**: il `lineItemId` nel legacy esiste, e la scheda
   deve dire che fine fa.
3. **La formulazione di M13 affermava troppo**: «per lo storico migrato il dato non esiste nel
   legacy» è falso per `slot_start`/`slot_end`, dove il valore sta nell'etichetta `"9-11"` e la
   conversione è rimasta indietro. Le 126 note ora rimandano alla riga «Legacy» della scheda.
   ⚠️ *Smentito a sua volta il 2026-10-02*: nei dati il formato `"9-11"` non c'è, le 11 etichette
   sono **durate** (`10 min` … `6 hrs`), non orari. `slot_start`/`slot_end` restano NULL per
   costruzione e la durata è in `duration_minutes` (Q4 e Q13 di [`_colonne-vuote.md`](_colonne-vuote.md)).
4. **Il censimento delle colonne vuote sbagliava un nome** (allora in `_temp2.md`, oggi in [`_colonne-vuote.md`](_colonne-vuote.md)): `notification.email_channel_settings.last_verified_at`
   non esiste, la colonna è **`last_verification_at`**.

**Tre difetti scoperti dalle corse di verifica, che la corsa «a zero errori» del mattino non poteva
vedere perché precedeva le migration di questo elenco.** In tutto **7.056 righe respinte**.

| Difetto | Righe | Causa |
|---|---|---|
| `base_price` ancora iniettato nell'INSERT | 6.368 | D1 elencava sette punti, compresi «cancellare le righe 30–39» nei due `custom_row_transform`: il lato backend era fatto, il lato ETL no. 2.915 testate rifiutate + 3.453 figlie respinte dalla chiave esterna. **Le YAML citavano `base_price` solo nei commenti** — l'INSERT lo costruivano i transform, quindi un `grep` sulle spec non bastava |
| `booking.line_additional_items`: tupla disallineata | 654 | Togliendo `fiskaly_line_item_id` (M5) erano stati aggiornati l'elenco colonne e il **primo** costruttore di tuple, non il secondo. È la famiglia di difetto per cui il giorno prima era stata rafforzata la guardia in `booking_service_lines.py` («la prima tupla non basta») — e questo file non ne aveva nessuna |
| `switchboard_voice_call_records`: tre difetti in un punto | 34 su 214 | Il riconoscimento dell'URI SIP guardava il solo prefisso `sip:`, assente su **tutti** i 34 valori di `from` e 13 di `to`; `caller_number` veniva riempito senza verificare la forma benché la spec dichiarasse «`from` sempre E.164» (falso: 21 righe sono il softphone che chiama fuori, 13 softphone-a-softphone); il troncamento era a 32 su colonne `varchar(30)` |

**Due trappole tecniche da ricordare**, perché costano un giro di migrazione ciascuna:

- **`enum::text` è vietato in un'espressione di indice** (il cast di un enum è `STABLE`, non
  `IMMUTABLE`: PostgreSQL rifiuta con 42P17), e **`ALTER COLUMN ... TYPE bytea` richiede `USING`**,
  che `AlterColumn` di EF non genera.
- **`migrations add --no-build` lascia la migration non compilata**: il successivo `database update
  --no-build` gira su un assembly che non la contiene e si blocca con `PendingModelChangesWarning`.
  La prima ricostruzione dello schema si è fermata a 90 tabelle su 195. **Compilare prima.**
