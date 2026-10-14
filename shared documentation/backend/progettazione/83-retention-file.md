# 83 — Conservazione dei file: risposte del titolare e cosa serve per metterle a database

> **Stato: analisi e proposta. Niente è stato applicato.**
>
> Su richiesta esplicita: **la struttura del database non è stata toccata** (c'è una corsa
> dell'ETL in corso) e i job non erano stati scritti. Questo documento dice cosa servirebbe, perché,
> e cosa va chiarito prima.
>
> 🔄 **Aggiornamento del 2026-10-03: è stato applicato tutto.** Le sette migration e il seed delle 15
> policy sui file (riquadro di §5), e i **job**, che ora esistono come motore generico in
> `BuildingBlocks.Infrastructure/Retention/` — vedi [`20-jobs.md` §1.5](20-jobs.md). Resta fuori la **pianificazione**: non
> c'è ancora un host dei job e i cron non sono cablati.
>
> Fonte delle risposte: [`retention-file-con-risposte.docx`](retention-file-con-risposte.docx)
> (1 ottobre 2026), che risponde alle sei domande di
> [`retention-file.docx`](retention-file.docx). Tutto il resto è misurato: sul PostgreSQL
> `summer_booking_dev`, sul MySQL `u811491364_summer_booking` e sul bucket S3 di produzione.

---

## 1. Le sei risposte

Riportate **alla lettera**, perché due sono ambigue e una ribalta una decisione precedente: la
parafrasi nasconderebbe entrambe le cose.

| N. | Domanda | Risposta del titolare |
|---|---|---|
| 1 | Report del bagnino: servono? obbligo? dati personali? durata? | «Non servono, dopo l'invio si cancellano» |
| 2 | Fotografie: quelle della scheda pubblica restano per sempre? E le altre? | «Si rimangono, potremmo pensare di ridurle a max 3» — **integrata il 2026-10-01**: «le altre archiviate, seguire le regole di archiviazione e cancellazione degli stabilimenti» |
| 3 | Allegati di email e campagne: si cancellano con la campagna? | «Si cancellano dopo 48 ore» — **chiarita il 2026-10-01**: «48 ore da quando nessuno lo usa più» |
| 4 | Listini e menù in PDF: alla cessazione? e quando sostituiti? | «Si cancellano alla cessazione e quando vengono sostituiti» |
| 5 | Icone: distinzione per singola icona, o si conserva tutto il catalogo? | «Attenzione! Le icone sono del gestionale e non vanno mai cancellate.» |
| 6 | Principio generale: cancellare la scheda cancella anche i suoi file? | «Sì» |

Il documento conferma inoltre, come già concordato e **senza bisogno di altre risposte**:
scontrini elettronici (archivio a 36 mesi, mai cancellati prima di 10 anni), audio del centralino
(cancellati alla cessazione), PDF dei report alle strutture convenzionate (24 mesi).

---

## 2. Tre risposte applicabili subito

### 2.1 Report del bagnino — la risposta coincide con la realtà misurata

«Non servono, dopo l'invio si cancellano.» Non è una regola di conservazione: è il
comportamento del generatore. Il PDF si produce, si allega all'email, si butta.

**E il legacy già fa così, senza che nessuno lo sapesse.** Verificato sul bucket di produzione il
1 ottobre 2026: né `Pdf/` né `lifeguardPdf/` **esistono su S3**. I 16.882 riferimenti in
`email_logs.attachments` hanno la forma

```json
[{"filename":"lifeguardPdf/lifeguard-1722481200657.pdf","path":"lifeguardPdf/lifeguard-1722481200657.pdf"}]
```

cioè il percorso della cartella **locale** del cronjob (`cron.helper.js`:
`pdfDirectory = "lifeguardPdf"`). Il file veniva allegato e mai caricato.

Conseguenze:

- **Niente da migrare.** I 16.882 file che il README di `utility/02-migrazione-file` §1 conta
  come il 42% dell'inventario non esistono. I file da migrare sono circa 23.700, non 40.597.
- **L'ETL è già a posto**: `notification_media_assets.yaml` legge da `custom_templates`,
  `marketing_email_campaign`, `marketing_automation`, `marketing_event` e `hotel_reports` —
  **non** da `email_logs.attachments`. Non crea quelle 16.882 righe. Non serve toccarlo.
- **Niente policy e niente job.** `media_kind` ha già `rendered_pdf`, che resta utile ai report
  per le strutture convenzionate (§2.3).
- **Un requisito per il nuovo gestionale**, non per la retention: il generatore dei report
  bagnino deve produrre il PDF e cancellarlo dopo l'invio. Se lo caricasse su S3
  ricreerebbe il problema da zero.

### 2.2 Icone — questa risposta **ribalta** una decisione precedente

> ⚠️ **Va fatta confermare prima di cambiare qualcosa.**
> [`80-retention.md`](80-retention.md), riga «Cataloghi master globali», dice: *«le voci usate da
> un solo stabilimento si 🗑️ alla sospensione/cessazione del suo account — distinzione chiesta
> esplicitamente dal titolare (da applicare per riga, non per tabella)»* — e lo dice in **due
> punti**, nella tabella per categoria e nel riepilogo finale. La risposta 5 dice l'opposto: le icone
> sono del gestionale e non si cancellano mai. Essendo la più recente prevale, ma i due documenti
> ora si contraddicono e la richiesta precedente era stata registrata come esplicita: conviene
> che il titolare confermi di volerla sostituire, non che lo deduciamo noi.

Se confermata, semplifica tre cose in modo sostanziale:

- **cade il lavoro di tracciamento per singola icona**, che era la parte onerosa: non serve più
  sapere quali stabilimenti usano quale icona;
- **cade l'ostacolo strutturale** descritto in `utility/02-migrazione-file/README.md` §7.1
  («`catalog.media_assets` non ha `property_id`, quindi la regola *per riga, non per tabella* non
  è applicabile»): le icone erano il catalogo condiviso, cioè il caso che rendeva la risalita
  necessaria. Misurato sulle 11.040 righe di `catalog.media_assets`: **10.954 hanno già uno
  stabilimento risolvibile**, **22 sono catalogo master** e **64 non hanno nessun utilizzatore**.
  Con le icone permanenti, i 22 non si toccano mai e il problema si riduce a foto e allegati;
- le icone **non entrano in nessun job di cancellazione**: nessuna policy da scrivere.

Resta un punto che la risposta chiude in modo più ampio della proposta: il documento suggeriva di
trattare come materiale dello stabilimento le icone personalizzate riconoscibili (per esempio con
un logo). La risposta dice «non vanno **mai** cancellate», quindi anche quelle. È una scelta
legittima, ma va saputa: se un gestore chiede la rimozione del proprio logo caricato come icona,
la regola attuale dice no.

### 2.3 Quello che era già deciso e resta tale

| Tipo | Regola | Dove è scritta |
|---|---|---|
| PDF scontrini elettronici | archivio a 36 mesi, **mai** cancellati prima di 10 anni | `80-retention.md`, riga «Scontrino elettronico emesso» |
| Audio del centralino | cancellati da S3 alla cessazione, con la configurazione | `80-retention.md`, riga «Config centralino, IVR, audio» |
| PDF report alle strutture convenzionate | cancellazione del PDF a 24 mesi | `80-retention.md`, riga «Report periodici alle strutture convenzionate» |

Su quest'ultimo c'è un dato nuovo, misurato: **i 28 PDF esistono su S3** in `reports/hotel/`, ma
`hotel_reports.path` è **NULL su entrambe le 2 righe** della tabella legacy. Il file c'è, la riga
che dovrebbe nominarlo è vuota. Quindi oggi sono orfani: una migrazione guidata dal database non
li vedrebbe, e una regola «24 mesi dalla data del report» non ha una riga su cui agganciarsi. Va
deciso se recuperarli per chiave (il nome è `report_<hotelId>_<epoch_ms>.pdf`, quindi la data
c'è nel nome) o considerarli persi.

---

## 3. Le due risposte chiarite il 2026-10-01

### 3.1 Allegati — «48 ore da quando nessuno lo usa più»

La regola è ora precisa e **calcolabile**: `only_if_unreferenced = true`,
`reference_event = asset_unreferenced`, durata 2 giorni.

Verificato quali righe contano come «utilizzatori» di un
`notification.media_assets` — sono **due sole**, e sono proprio quelle giuste:

| Chi lo usa | Cosa copre |
|---|---|
| `notification.template_attachments.media_asset_id` | allegati dei modelli di email **e** di campagne e automazioni (entrambe puntano a un `template_id`, e l'allegato è appeso al modello) |
| `notification.marketing_events.cover_image_asset_id` | immagine di copertina di un evento |

**Le email già inviate non contano**, e va bene così: `notification.deliveries` ha solo un
booleano `has_attachments`, nessun riferimento all'asset. Quindi un allegato non resta in vita
per dodici mesi solo perché una email spedita lo nominava.

> ⚠️ **Però, così com'è, la regola non scatterebbe mai per gli allegati delle campagne.**
> L'allegato è appeso al **modello**, non alla campagna: cancellare una campagna a 24 mesi non
> libera l'allegato, perché il modello continua a referenziarlo. E `notification.templates`
> **non ha nessuna regola di conservazione** fra le 44 attive. Risultato: l'allegato di una
> campagna diventa «non usato da nessuno» solo se qualcuno cancella a mano la riga di
> `template_attachments`.
>
> Non è un difetto della risposta: è una conseguenza di come è modellato lo schema, e va sciolta
> prima di scrivere il job. Tre strade, in §7 punto 1.

### 3.2 Fotografie — «le altre archiviate, seguendo le regole degli stabilimenti»

Due regole distinte, entrambe applicabili:

- **foto della scheda pubblica** → permanenti, anche dopo la cessazione;
- **tutte le altre** → archiviate e poi cancellate **con la configurazione dello stabilimento**,
  cioè la cascata di [`80-retention.md`](80-retention.md) §5: 📦 snapshot su S3 a **+3 mesi**
  dalla cessazione, 🗑️ a **+2 anni**.

> ⚠️ **Manca il criterio per dire quali foto sono «della scheda pubblica».**
> `catalog.bathhouse_photos` ha `bathhouse_id`, `media_asset_id` e `sort_order`: **nessun flag**
> che distingua le foto pubblicate. Quindi la distinzione va definita, e ci sono due modi:
> solo la foto di copertina (`catalog.bathhouses.main_photo_id`, una per stabilimento) oppure le
> prime N per `sort_order` — con N probabilmente 3, il «max 3» della prima risposta.
>
**Quanto pesa la scelta di N.** Misurato su `catalog.bathhouse_photos` (10.600 righe su 10.431
stabilimenti):

| Foto per stabilimento | Stabilimenti |
|---:|---:|
| 1 | **9.706** (93%) |
| 2–3 | 20 |
| 4–9 | 113 |
| 10–16 | 11 |
| 63 | 1 |

Quindi il gruppo «altre» è **piccolo in entrambi i casi**: 465 foto con N = 3, 749 con N = 1
(solo la copertina). Non è la decisione costosa che sembrava: 93% degli stabilimenti ha una foto
sola e non è toccato da nessuna delle due varianti.

> 📌 Il «max 3» come *tetto* — quante foto un gestore può pubblicare — è una regola di prodotto,
> non di conservazione, e il modello delle policy non sa esprimerla. Se serve va aperta a sé.

### 3.3 ⚠️ Ma 10.090 foto non hanno nessuna scheda a cui agganciarsi

Questo non viene dalle risposte: l'ho trovato controllando i numeri di §3.2, e **cambia il
perimetro** di R2 e R6.

Le foto su S3 referenziate dal legacy sono **20.389** (`images/bathouse/`) + 422
(`images/property/`). Di queste, in `catalog.media_assets` ce ne sono **10.702**. Le altre
**~10.090 non hanno nessuna riga nel nuovo schema**, e la ragione è una scelta deliberata:

- `bathouses.photos` + `mainPhoto` → 10.702 chiavi, **tutte presenti** in `catalog.media_assets`;
- `properties.photos` → 9.687 chiavi sotto `images/bathouse/` più ~403 sotto `images/property/`,
  **insieme completamente disgiunto** dal precedente (zero in comune), e **nessuna** presente in
  `catalog.media_assets`.

L'ETL le salta per questa ragione, scritta in
[`analisi-post/db/catalog/catalog_properties.md`](../analisi-post/db/catalog/catalog_properties.md):

> *«`properties.photos` | array URL a livello property, **quasi sempre `[]`**, duplicato
> concettuale di `bathouses.photos` → le foto vivono sulla vetrina»*

**Entrambe le premesse sono smentite dai dati:**

| Premessa | Misura |
|---|---|
| «quasi sempre `[]`» | valorizzata su **10.510 righe su 10.852**, cioè il **96,8%** |
| «duplicato concettuale di `bathouses.photos`» | **zero chiavi in comune** fra i due insiemi |

Conseguenze, tutte e tre da sciogliere prima di applicare le regole sulle foto:

1. **R2 non le copre**: non essendo né «della scheda pubblica» né foto di uno stabilimento
   registrato, non cadono in nessuna delle due metà della risposta.
2. **R6 non le copre**: «cancellare la scheda cancella i suoi file» non si applica a un file che
   non ha nessuna scheda.
3. **Il travaso non le porta via**: la lista di lavoro costruita dal PostgreSQL non le vede, e le
   classifica come orfani. Sono circa il 40% delle foto.

Non è una questione di conservazione, è una questione di migrazione e di schema — ma decide su
quanti file le regole sulle foto hanno effetto, quindi va chiusa prima.

**A chi appartengono.** Misurato il 2026-10-01 sulle 10.097 chiavi distinte di
`properties.photos`, incrociate con `properties.status`:

| `status` | Significato (`analisi-pre/db/properties.md`) | Foto | Ha una vetrina in `bathouses`? |
|---:|---|---:|---|
| 3 | import da CSV — **compare nella ricerca pubblica** (il filtro di `serchProperty` è `status: [1,3]`) | **8.999** | sì, 8.998 su 8.999 |
| 5 | **non documentato** in nessun punto dell'analisi né del codice | 679 | sì |
| 1 | approvato dal system admin — pubblico | 214 | 117 sì, 97 no |
| 0 | registrato, in attesa di approvazione | 179 | 175 no |
| 2 | rifiutato dal system admin | 32 | 24 no |
| 30 | **non documentato** | 1 | sì |

**Sono foto di stabilimenti attivi, non di stabilimenti cessati**: 9.213 su 10.097 (il 91%)
appartengono a stabilimenti che compaiono nella ricerca pubblica (`status` 1 o 3), e per 9.115 di
questi esiste già la vetrina corrispondente in `catalog.bathhouses`. Solo 211 sono di stabilimenti
rifiutati o mai approvati.

> 📌 **Due valori di `status` non sono documentati da nessuna parte**: `5` (731 stabilimenti, 679
> foto) e `30` (1). `analisi-pre/db/properties.md` elenca solo 0–4, e nel dump attuale `status=4`
> **non esiste più**. Da chiarire a parte: non blocca la retention, ma `status=5` è il secondo
> valore più frequente della tabella.

### 3.4 I 743.576 QR: a cosa servono, dal codice

Non viene dalle risposte: è la domanda C di §7, e la risposta sta nel codice legacy.

`AWS_QR_FILE_PATH = "images/qrcodescan"` (`globalConstants.js`) è usato in **19 punti**, tutti in
`email.helper.js` e `booking.controller.js`. Lo schema è sempre lo stesso:

```js
let managerqRIt = `${basePath}/onlinebooking/consumer/it/${reservationCode}`;
let managerQRResultIt = await generateQrCode(managerqRIt);
let qRCodeIt = await awsUpload.UploadBase64OnS3(managerQRResultIt, AWSCredentials.AWS_QR_FILE_PATH);
```

Quindi ogni file è **l'immagine PNG di un QR code che codifica un URL**:
`https://summerbooking.it/onlinebooking/consumer/{it|en}/{reservation_code}`. Due per
prenotazione (italiano e inglese), a volte quattro (cliente e gestore).

Tre fatti che ne determinano il valore:

1. **Non contengono informazione.** Il QR codifica un URL costruito dal solo `reservation_code`,
   che è già a database. Sono **rigenerabili al 100%**, in millisecondi.
2. **Esistono solo perché un client di posta non può generare un'immagine al volo.** L'URL S3
   finisce nell'`<img src>` dell'email di conferma (`finalQRurlForCust` passato al template). È
   l'unico motivo per cui sono su S3 invece di essere generati su richiesta.
3. **Puntano a una pagina che per le prenotazioni vecchie non serve più.** Il corpo dell'email
   viene già azzerato a **150 giorni** (`notification.deliveries.rendered_body → NULL`) e la riga
   cancellata a **12 mesi**; il dettaglio della prenotazione è archiviato a **N−2 stagioni**.

**Valore relativo alle altre decisioni già prese sui file:**

| Tipo | Rigenerabile? | Serve a | Valore |
|---|---|---|---|
| PDF scontrini | no | obbligo di legge, 10 anni | **massimo** |
| Foto scheda pubblica | no | il portale pubblico | alto |
| Icone | no (sono del gestionale) | tutto il gestionale | alto |
| Audio centralino | no | la configurazione dello stabilimento | medio, legato alla vita dello stabilimento |
| Allegati campagne | no | nulla, una volta che nessuno li usa | basso → 48 ore |
| **QR** | **sì, dal `reservation_code`** | **mostrare un'immagine dentro un'email già spedita** | **il più basso di tutti — e sono il 95% degli oggetti del bucket** |

**Suggerimento: cancellarli a 12 mesi, allineandoli a `notification.deliveries`.** È la regola più
coerente con il principio della risposta 6: la conservazione di un file segue la conservazione
della cosa che lo usa, e la cosa che usa il QR è la registrazione dell'email. Quando quella
sparisce, il motivo per cui l'immagine esiste sparisce con lei.

L'unico effetto collaterale è che una email più vecchia di un anno, riaperta nella casella del
destinatario, mostrerebbe un'immagine rotta — ma quel QR rimanderebbe a una prenotazione già
archiviata, quindi non funzionerebbe comunque.

> 🔧 **La correzione strutturale, per il nuovo gestionale**: non salvarli affatto. Un endpoint che
> genera il QR al volo da `reservation_code` (`GET /qr/{codice}.png`) rende l'`<img src>` stabile
> *e* non lascia niente su S3. È lo stesso ragionamento della risposta 1 sui report del bagnino.
> Così il problema non si ripresenta.

✅ **Deciso il 2026-10-01: i QR seguono la retention delle prenotazioni.** Non quella delle email,
come avevo suggerito. È la scelta più coerente: il QR *è* un accessorio della prenotazione, non
dell'email che lo ha consegnato, e l'email era solo il mezzo.

In pratica significa agganciarli a `SeasonalArchive`, il job che archivia il dettaglio delle
prenotazioni oltre **N−2 stagioni** ([`80-retention.md`](80-retention.md)): quando il dettaglio di
una prenotazione viene archiviato, i suoi QR si cancellano.

Due conseguenze da tenere a mente quando si scriverà il job:

- **è una finestra più lunga di 12 mesi**, quindi i QR restano in media due stagioni invece di una.
  Su 743.576 file e 1,2 GB la differenza di costo è modesta, ma c'è;
- **oggi non c'è modo di sapere a quale prenotazione appartiene un QR.** Il nome del file è
  generato da `uniqid()` e il `reservation_code` non compare da nessuna parte: né nel nome, né in
  una riga di database (nessuna tabella referenzia `images/qrcodescan/`). Quindi la regola **non è
  applicabile ai QR esistenti** senza prima ricostruire il legame — e ricostruirlo richiede di
  leggere l'HTML delle email in `email_logs.body`, che a sua volta viene azzerato a 150 giorni.
  Per i QR **nuovi** basta registrarli con la prenotazione (o non salvarli affatto, vedi sopra).

> ❓ **Ne deriva una domanda**: per i 743.576 QR **già esistenti**, che non si possono collegare a
> una prenotazione, cosa si fa? Le opzioni sono in §7 punto C2.

### 3.5 I 494 scontrini orfani — chiusa senza indagare

✅ **Deciso il 2026-10-01: non si indaga**, sono considerati scontrini emessi dopo il dump.

Registro qui i numeri che avevo misurato, perché se un giorno la domanda tornasse si ripartirebbe
da questi invece che da zero. Incrociando il `LastModified` degli oggetti con la copertura del
dump (`fiskaly_receipt` va dal **2026-06-01** al **2026-09-08 14:36**, 2.609 righe):

| Quando è stato caricato il PDF | Quanti | Spiegazione |
|---|---:|---|
| dopo il 2026-09-08 (data del dump) | **190** | la produzione ha continuato a lavorare: sul dump definitivo avranno la loro riga |
| prima del 2026-06-01 | **57** | il dump **non contiene nessuna ricevuta** anteriore a giugno 2026: non c'è riga perché non c'è il periodo |
| fra il 2026-06-01 e il 2026-09-08 | **247** | periodo coperto dal dump, nessuna riga corrispondente — **non spiegati** |

Verificato anche che non si tratta di righe con il path mancante: **0 righe** hanno un
`e_receipt_id` senza `e_receipt_path`.

Quindi 247 PDF su 494 restano senza spiegazione. Sono 247 documenti su ~2.600, cioè il 9%, e il
peso è irrilevante (1,5 MB in tutto). La decisione è di non indagare: va bene, ma è una decisione
su **documenti fiscali**, quindi resta scritta qui con il numero, non sottintesa.

---

## 4. Cosa il database sa esprimere oggi, e cosa no

### 4.1 Com'è fatto il modello attuale

`privacy.privacy_retention_policies` — **44 righe attive, nessuna sui file**:

| Colonna | Tipo | |
|---|---|---|
| `schema_name`, `table_name` | varchar | la tabella su cui agire |
| `field_names` | varchar[] | le colonne (per `null_fields`/`anonymize`) |
| `reference_date_column` | varchar | **la colonna data da cui conta la durata** |
| `duration_value` + `duration_unit` | int2 + enum | `days \| months \| years \| seasons` |
| `action` | enum | `delete_row \| null_fields \| anonymize \| archive_cold` |
| `legal_basis` + `basis_source` | text + enum | `law \| recommended_policy \| disputed` |
| `is_dry_run_only`, `is_active` | bool | |

Unica per `(schema_name, table_name, field_names, action)`. Azioni usate: 29 `delete_row`,
9 `archive_cold`, 4 `null_fields`, 2 `anonymize`.

Il modello è **per riga di database, con una durata contata da una colonna data della riga
stessa**. Dentro questo stampo si esprime bene tutto ciò che è «cancella questa riga N mesi dopo
questa data».

### 4.2 I sette punti in cui non arriva

| # | Cosa serve | Perché il modello attuale non lo esprime |
|---|---|---|
| A | **Agire su un oggetto di storage** | `retention_action` non ha nessuna azione che tocchi un file: `archive_cold` significa «sposta la riga su storage freddo», non «sposta l'oggetto su Glacier». Serve distinguere le due cose o il job non sa cosa fare |
| B | **«solo se nessun altro lo usa»** (risposta 6) | non c'è nessuna nozione di dipendenza o di conteggio dei riferimenti. «I file che *solo* quella scheda usava» non è decidibile da `(tabella, colonna data, durata)` |
| C | **Decorrere da un evento, non da una data in colonna** | «alla cessazione dello stabilimento», «alla sostituzione del listino», «dall'ultimo uso» non sono colonne data sulla riga del file |
| D | **Dire «non si cancella mai»** | risposte 2 e 5 sono regole di conservazione permanente. Oggi si ottengono con l'**assenza** di una policy, che è esattamente la fragilità segnalata in `02-migrazione-file/README.md` §7.1 punto 4: «è una scelta, ma va scritta come policy esplicita, altrimenti è solo un'assenza di regola» |
| E | **Sapere che la cancellazione è andata a buon fine** | cancellare un oggetto su S3 non è transazionale con il DB. Senza un posto dove registrare «deciso / eseguito / fallito», un errore di rete lascia il database che dice una cosa e l'archivio che ne dice un'altra |
| F 🆕 *(2026-10-02)* | **Distinguere un file senza aggancio da un file orfano** | `catalog.media_assets` non aveva `property_id`: una foto di uno stabilimento che non ha aperto la vetrina e un file che nessuno userà mai hanno la stessa firma. Vedi §5.5 |
| G 🆕 *(2026-10-02)* | **Archiviare i report alle strutture convenzionate** | nel nuovo schema non esisteva nessuna tabella per i report: la policy a 24 mesi non aveva niente su cui agire. Vedi §7-G1 |

Due vincoli al contorno, verificati:

- **lo stato «cessato» non esiste.** `catalog.property_status` è
  `pending | approved | rejected | imported`. In `80-retention.md` la cessazione è sempre
  espressa come «fine ultimo abbonamento + N mesi», e c'è
  `identity.account_subscription_periods.closed_at` da cui derivarla. Va scritta **una** volta
  come definizione, perché oggi tre documenti la nominano in tre modi;
- **non esiste nessuna tabella dei job.** `infra` contiene solo `__ef_migrations_history` e
  `integration_api_clients`. Il catalogo dei job vive nella documentazione
  ([`80-retention.md`](80-retention.md), [`20-jobs.md`](20-jobs.md)), non a database.

---

## 5. La struttura — ✅ **applicata il 2026-10-03**

> ## ✅ Cosa è stato applicato, e dove sta il dettaglio
>
> Questa sezione resta il **perché** di ogni scelta. Il **come** — SQL, colonne, vincoli, indici,
> righe di seed — è nelle schede di design, dove vive insieme al resto della tabella:
>
> | Dove | Che cosa ci trovi |
> |---|---|
> | [`privacy_retention_policies.md`](../analisi-post/db/privacy/privacy_retention_policies.md) | le quattro colonne nuove, i quattro `CHECK`, l'indice unico rifatto, le due etichette su `retention_action`/`audit_action`, e le 15 righe di seed nella sezione **FILE** |
> | [`privacy_file_deletions.md`](../analisi-post/db/privacy/privacy_file_deletions.md) | la coda delle cancellazioni su storage (§5.3) |
> | [`catalog_media_assets.md`](../analisi-post/db/catalog/catalog_media_assets.md) | `property_id` (§5.5) |
> | [`catalog_agreement_reports.md`](../analisi-post/db/catalog/catalog_agreement_reports.md) | la tabella dei report (§7-G1) |
> | [`80-retention.md` §5](80-retention.md) | come si deriva «la cessazione dello stabilimento» |
>
> **Sette migration**, applicate in quest'ordine:
>
> | Migration | Modulo | Cosa |
> |---|---|---|
> | `20261003074912_AddRetentionObjectActionsAndFileEventEnums` | Privacy | `delete_object`/`archive_object` su **entrambi** gli enum, più `retention_reference_event` e `retention_storage_date_source` |
> | `20261003075313_AddRetentionFileTargetingColumns` | Privacy | 4 colonne, 3 `CHECK`, indice unico rifatto |
> | `20261003075631_AddPrivacyFileDeletionsQueue` | Privacy | la coda delle cancellazioni |
> | `20261003075855_AddMediaAssetPropertyId` | Catalog | `property_id` su `media_assets` |
> | `20261003080145_AddAgreementReports` | Catalog | la tabella dei report + 2 enum |
> | `20261003080319_AddRetentionDecorrenzaCheck` | Privacy | 🆕 vincolo «ogni policy ha una decorrenza» — **non era in specifica**: è nato applicandola |
> | `20261003080717_SeedFileRetentionPolicies` | Privacy | **15 righe** (F11 elenca 5 prefissi → F11a…e) |
>
> Stato dopo: `retention_action` ha **6** etichette, `privacy_retention_policies` **20 colonne** e
> **59 righe** (44 + 15), **tutte** con `is_dry_run_only = true`. `dotnet build` 0 warning / 0 errori,
> `ArchitectureTests` 68/68, e il seed è stato verificato **idempotente** rieseguendolo a mano (59
> righe prima e dopo, `updated_at` invariato): la prima applicazione non esercita il percorso
> `ON CONFLICT`, quindi senza quella prova non si sarebbe saputo se funziona.
>
> 🔄 **I job sono stati scritti il 2026-10-03**: un motore generico in `BuildingBlocks.Infrastructure/Retention/` che
> legge le regole dalla tabella dichiarativa, piu' il riconciliatore della coda delle cancellazioni.
> §6 resta la loro specifica, e l'elenco per nome in [`20-jobs.md` §1.5](20-jobs.md) resta la mappa
> di come andranno raggruppati quando ci sara' uno scheduler.
>
> ### Tre scostamenti dalla proposta, con la ragione
>
> 1. **L'indice unico usa `NULLS NOT DISTINCT`**, non le espressioni `COALESCE` che erano state
>    proposte. PostgreSQL è alla 17.6 e lo supporta dalla 15. Non è un dettaglio di stile: su colonne
>    nullable semplici due righe identiche con `reference_event` e `storage_prefix` entrambi NULL — il
>    caso di **tutte e 44** le policy preesistenti — non collidere­bbero, perché in PostgreSQL i NULL
>    sono distinti in un indice unico, e la protezione sarebbe sparita proprio sulle righe che
>    l'indice proteggeva prima. Nessuna delle due forme è esprimibile con la Fluent API, quindi
>    l'indice **non è dichiarato in EF** ed è creato in SQL dentro la migration.
> 2. **`privacy_file_deletions.id` è `GENERATED ALWAYS`** e non `BY DEFAULT`: tutte le altre tabelle
>    del modulo usano `ALWAYS`, e un'eccezione senza motivo è una trappola per chi legge.
> 3. **Il riferimento al PDF del report è un `bigint` logico, non una FK**, e si chiama
>    `pdf_media_asset_id` invece di `media_asset_id`: vedi la scheda della tabella per il perché.

Tre modifiche, in ordine di necessità. Tutte sono migration EF Core su `Module.Privacy` (più una
su `Module.Catalog`).

### 5.1 Due etichette nuove su `privacy.retention_action` — indispensabile

```
retention_action: delete_row | null_fields | anonymize | archive_cold
                  + delete_object      ← cancella l'oggetto dallo storage
                  + archive_object     ← sposta l'oggetto su classe fredda (Glacier)
```

Senza queste, **nessuna** delle risposte 3, 4 e 6 è rappresentabile: si potrebbe registrare la
policy ma non dire al job cosa fare. Va aggiunta la coppia corrispondente anche a
`privacy.audit_action` (`delete_row | null_fields | anonymize | archive_cold | exported`), o la
cancellazione di un file non è tracciabile nell'audit.

Ricordare il `HasColumnType` obbligatorio sulle colonne enum native e `HasDefaultValueSql` mai
`HasDefaultValue` — vedi `docs/43-migration-procedure.md` §4.

### 5.2 Due colonne nuove su `privacy_retention_policies` — raccomandato

```
only_if_unreferenced   bool NOT NULL DEFAULT false
reference_event        privacy.retention_reference_event NULL
```

- `only_if_unreferenced` è il principio della risposta 6 reso eseguibile: cancella l'oggetto
  **solo se nessuna altra riga lo usa**. È un booleano e copre il caso che il documento stesso
  aveva previsto come eccezione («se lo stesso file è usato da più campagne, si cancella quando
  l'ultima viene cancellata»).
- `reference_event` sostituisce `reference_date_column` quando la durata non parte da una colonna
  ma da un evento. Enum nuovo, con le sole voci che servono davvero:
  `property_ceased | asset_unreferenced | asset_replaced`.

**Perché su questa tabella e non su una tabella dedicata.** Una
`privacy_file_retention_policies` separata sarebbe più espressiva, ma spezzerebbe in due il posto
dove si leggono le regole: il pannello systemadmin, l'audit e la *dry run* dovrebbero interrogare
due modelli. Con due colonne su quella esistente il modello resta uno, e le 44 righe attive non
cambiano comportamento (`only_if_unreferenced = false`, `reference_event = NULL` è esattamente il
comportamento di oggi).

> ### 🔄 Una terza colonna, aggiunta il 2026-10-05: `target_filter`
>
> `only_if_unreferenced` copre **un** perimetro — «solo i file che nessuna riga nomina più» — e
> all'uso si è visto che non copre l'altro: **quale sottoinsieme della tabella** riguarda la policy.
>
> `notification.media_assets` ha sei `kind` e la regola decisa dal titolare riguarda **solo** i PDF
> dei report alle strutture convenzionate. La policy, letta alla lettera, diceva «cancella tutti i
> file di notification oltre 24 mesi»: gli allegati delle campagne compresi. Il motore la saltava, e
> aveva ragione.
>
> ```
> target_filter   jsonb NULL     -- {"scope":"whole_table"} oppure {"column","op","value"/"values"}
> ```
>
> Il cancello del motore è ora: **un'azione su storage si esegue se la policy dichiara
> `only_if_unreferenced` oppure un `target_filter`.** `{"scope": "whole_table"}` serve a dire «tutte
> le righe, **deliberatamente**» — indistinguibile nell'SQL da «nessun filtro», e tutt'altra cosa nel
> significato.
>
> Dettaglio completo (forme, operatori, il limite sui valori e il perché, i cinque perimetri
> seminati) nella [scheda di `privacy_retention_policies`](../analisi-post/db/privacy/privacy_retention_policies.md),
> sezione `target_filter`. Sblocca **F5** e la policy decennale di fiskaly; `catalog` resta fuori e
> richiede un job scritto a mano.

### 5.3 Una coda delle cancellazioni sullo storage — indispensabile prima di cancellare qualcosa

```
privacy.privacy_file_deletions
    id, public_id
    asset_schema, asset_table, asset_id      -- quale delle 4 *media_assets, quale riga
    storage_origin, storage_path             -- cosa cancellare, com'era al momento della decisione
    policy_id                                -- quale regola l'ha deciso (FK, nullable per le manuali)
    decided_at
    executed_at        NULL                  -- quando l'oggetto è stato davvero rimosso
    local_copy_removed_at NULL               -- la seconda destinazione (vedi sotto)
    attempts, last_error, last_error_at
```

Serve perché la cancellazione di un oggetto non è transazionale con il DB. È la stessa forma che
`utility/02-migrazione-file` usa già nel suo checkpoint per il travaso — **tre stati distinti per
file, con tentativi e ultimo errore** — e per la stessa ragione: le due destinazioni possono
fallire indipendentemente.

> ### ⚠️ Due cose sulla coda imparate il 2026-10-05, scrivendo la retention degli archivi
>
> **① `asset_schema`/`asset_table`/`asset_id` sono annullabili, e lo devono restare.** Una riga in
> coda può venire da una policy su **prefisso**, che indirizza oggetti senza nessuna riga di asset
> dietro: per quelle tre colonne non c'è niente da scrivere. Un job che le legga come obbligatorie
> fallisce sulle righe da prefisso — ed è successo, preso da due test del riconciliatore.
>
> **② L'indice unico protegge solo le cancellazioni PENDENTI.** Il suo filtro è `executed_at IS NULL`,
> quindi appena il job esegue la cancellazione la protezione sparisce, e la riga di asset — che esiste
> ancora — torna a cadere nel perimetro della policy a ogni corsa successiva. Senza una guardia
> esplicita la coda si riempie delle stesse righe per sempre. Il motore ora esclude gli asset già
> ripuliti con un `NOT EXISTS` **sull'asset** e non sul percorso: un percorso riusato è un oggetto
> nuovo, un asset già ripulito resta ripulito.
>
> **E una conseguenza da sapere**: la coda **non ha una retention**. È la prova documentale di aver
> rimosso gli oggetti nei termini, e cresce di una riga per oggetto cancellato, per sempre. Finché i
> volumi sono quelli attuali va bene; se un giorno si decide di potarla, va deciso **insieme** al
> termine di `privacy_compliance_audit_logs`, perché provano la stessa cosa.

> ⚠️ **Le destinazioni sono due anche in cancellazione.** La decisione §7.3 di
> `02-migrazione-file/README.md` è che ogni file viva sul bucket nuovo **e** in una copia locale
> completa. Una regola che cancella un file deve cancellarlo in entrambi i posti, o la copia
> locale diventa l'archivio di tutto ciò che è stato cancellato per policy — cioè l'opposto di
> quello che la retention vuole. Questo non è ancora scritto in nessun documento.

### 5.4 Il conteggio dei riferimenti — da decidere con i numeri

`only_if_unreferenced` ha bisogno di sapere quante righe usano un `media_asset`. Oggi si ricava
percorrendo le **19 chiavi esterne in entrata** verso le quattro `*media_assets`
(`02-migrazione-file/README.md` §4.1), cosa che `app/links.py` di quella utility già fa.

Tre strade, in ordine di costo:

1. **query di risalita al volo**, a ogni valutazione di policy — nessun cambiamento di struttura;
2. **vista materializzata** con il conteggio per asset, rinfrescata dal job;
3. **colonna denormalizzata** `used_by_count` mantenuta da trigger.

La risposta 5 (icone permanenti) riduce molto la pressione, perché le icone erano il catalogo
condiviso, cioè il caso in cui il conteggio contava davvero. Restano foto e allegati. Misurato
sul `catalog.media_assets` attuale: 10.954 asset con uno stabilimento risolvibile, 22 di catalogo
master, 64 senza nessun utilizzatore.

**Raccomandazione: la 1**, e si rivaluta se i tempi del job lo richiedono. Non introdurre un
trigger per un job mensile.

### 5.5 bis Policy su un **prefisso di storage**, senza nessuna riga — richiesto da C2

Serve una seconda forma di policy, che la §5.2 non copre.

La decisione su C2 è «cancellare i QR più vecchi di N−2 stagioni usando il `LastModified`
dell'oggetto, **espresso come policy di retention**». Ma quei 743.576 file **non hanno nessuna riga
a database**: il nome viene da `uniqid()` e nessuna tabella li referenzia. Una policy agganciata a
`(schema_name, table_name, field_names)` non ha niente a cui agganciarsi.

Lo stesso vale per i ~12.000 orfani di C4: anche loro senza riga.

Quindi `privacy_retention_policies` ha bisogno di poter esprimere anche: *«sul bucket, sotto questo
prefisso, cancella gli oggetti più vecchi di N, contando dal `LastModified` dell'oggetto stesso»*.

Due colonne in più, entrambe nullable:

```
storage_prefix        varchar NULL   -- es. 'images/qrcodescan/'
storage_date_source   privacy.retention_storage_date_source NULL   -- 'object_last_modified'
```

Regole di coerenza da imporre con un `CHECK`:

- una policy ha **o** `table_name` **o** `storage_prefix`, mai entrambi e mai nessuno dei due;
- con `storage_prefix` valorizzato, `action` può essere solo `delete_object` o `archive_object`:
  non c'è nessuna riga su cui fare `delete_row`.

**Perché non un job con la regola scritta dentro.** Perché così la regola è visibile nel pannello
systemadmin insieme alle altre 44, si può mettere in prova con `is_dry_run_only`, e lascia traccia
nell'audit. Una regola che cancella 743mila oggetti non deve stare nascosta nel codice di un job.

> ⚠️ **È una forma pericolosa e va trattata come tale**: una policy che dice «cancella tutto sotto
> questo prefisso oltre N» non ha nessuna riga di database che la trattenga se il prefisso è
> sbagliato. `images/` invece di `images/qrcodescan/` cancellerebbe le foto degli stabilimenti.
> Va protetta come il prefisso di prova della utility 02 (`uploader.clean_test_prefix`, che rifiuta
> i prefissi che si sovrappongono a contenuto reale): un elenco chiuso di prefissi ammessi, e il
> rifiuto di tutto il resto.

### 5.5 `catalog.media_assets.property_id` — 🔄 **da «non urgente» a indispensabile** (2026-10-02)

`02-migrazione-file/README.md` §7.1 lo indicava come forse necessario perché la regola «per riga, non
per tabella» non era applicabile senza. Con la risposta 5 quella regola non esiste più, e questa
colonna era stata archiviata come «da non fare adesso».

**La decisione G3 (b) la rende necessaria.** Le 272 foto di property senza vetrina si conservano e
restano **senza riga di aggancio**, cioè con la stessa firma di un file orfano: senza un modo di
distinguerle, la policy F2 le cancella 48 ore dopo la migrazione e F9 il mese successivo.
`property_id` dice esattamente la cosa vera — **la property esiste, è la vetrina che manca** — e
trasforma la condizione delle policy in «nessun riferimento **e** nessuna property», che è la
definizione corretta di orfano.

| | |
|---|---|
| Colonna | `property_id uuid NULL`, FK logica verso `catalog.properties.public_id` |
| Precedente nello schema | `notification.media_assets` ha già la stessa colonna, con lo stesso scopo |
| Perché **non** un'etichetta `media_origin` | ⚠️ `catalog.media_origin` descrive **l'host dello storage** (`s3us_east2`, `s3us_east1`, `cloudfront`, `external`), non la provenienza logica. Metterci un'etichetta semantica mescolerebbe due significati nello stesso enum e romperebbe chi usa `origin` per costruire un URL |
| Quando | **prima** del recupero di `properties.photos`: dopo, i file sono già migrati e il primo giro di policy li ha già cancellati |

---

## 6. Proposta di job — **da non scrivere fino a indicazione**

Quattro job nuovi e due estensioni di job già in catalogo. Nessuno è stato scritto.

| Job | Cadenza | Cosa farebbe | Dipende da |
|---|---|---|---|
| `PurgeUnreferencedMediaAssets` | mensile | il principio della risposta 6: cancella da entrambe le destinazioni i file che nessuna riga usa più, con le eccezioni dichiarate (scontrini, foto della scheda pubblica, icone) | §5.1 + §5.2 + §5.3 |
| `PurgeExpiredCampaignAttachments` | giornaliera | la risposta 3 — **bloccato** finché «48 ore da cosa» non è chiarito (§3.1) | §3.1 |
| `PurgeHotelReportPdfs` | mensile | i PDF in `reports/hotel/` a 24 mesi. **Bloccato**: `hotel_reports.path` è NULL, non c'è una riga da cui partire (§2.3) | §2.3 |
| `ReconcileStorageDeletions` | giornaliera | ritenta le cancellazioni fallite in coda, e segnala quelle che falliscono da troppo tempo. Segna anche `purged_at` sulle righe di asset che hanno quella colonna — oggi solo [`privacy_archive_manifests`](../analisi-post/db/privacy/privacy_archive_manifests.md) | §5.3 |
| `DeprovisionClosedProperty` **(estensione)** | giornaliera | già in catalogo (`80-retention.md`) e già previsto per l'audio del centralino. Va esteso ai listini e menù in PDF — risposta 4, prima metà | §5.1 |
| `ArchiveFiscalReceipts` **(estensione)** | mensile | già in catalogo con la finestra giusta (36 mesi → Glacier, mai prima di 10 anni). Va implementato **l'effetto sul PDF**, che oggi non c'è | §5.1 |

Non servono job per:

- **report del bagnino** — risposta 1: non si archiviano affatto, non c'è niente da cancellare;
- **icone** — risposta 5: permanenti;
- **fotografie** — finché la §3.2 non è chiusa. La prima metà della risposta 2 («restano») si
  attua *non facendo niente*, ma va scritta come policy esplicita (§4.2 punto D), non lasciata
  all'assenza di regole.

La risposta 4, seconda metà — «e quando vengono sostituiti» — **non è un job**: è logica
applicativa. Quando il gestore carica un nuovo listino, il vecchio va messo in coda di
cancellazione nello stesso momento. Un job mensile che cerca i listini sostituiti sarebbe un giro
inutile.

---

## 7. Cosa resta da sapere

Aggiornato al 2026-10-01, dopo i chiarimenti su R2 e R3. Per ognuna c'è un **default proposto**:
se va bene, basta confermare.

### A — Decisioni prese il 2026-10-01

| # | Domanda | ✅ Decisione |
|---|---|---|
| **A1** | Come fa un allegato di campagna a diventare «non usato da nessuno»? (§3.1) | quando una campagna o automazione viene cancellata, si cancellano anche le righe `template_attachments` del suo modello **se nessun'altra campagna usa quel modello**. Le 48 ore decorrono da lì |
| **A2** | Quali foto sono «della scheda pubblica»? (§3.2) | le prime **3** per `sort_order` più la copertina (`main_photo_id`). Le altre seguono la cessazione dello stabilimento. Impatto misurato: **465 foto** nel gruppo «altre» |
| **A3** | Le 10.097 foto di `properties.photos` (§3.3) | **recuperarle.** Modifica annotata in [`analisi-post/db/catalog/catalog_properties.md`](../analisi-post/db/catalog/catalog_properties.md) e nella spec [`catalog_media_assets.yaml`](../../../../../utility/01-migrazione-test/mappings/catalog/catalog_media_assets.yaml): 9.801 si agganciano alla vetrina via `bathouses.propertyId`, 296 restano scarto motivato |
| **A5** | I 28 PDF in `reports/hotel/` (§2.3) | **recuperarli**. La data si ricava dal nome (`report_<hotelId>_<epoch_ms>.pdf`), da cui partono i 24 mesi |
| **A6** | La cancellazione per policy tocca anche la copia locale? (§5.3) | **no**: cancella **solo la copia su AWS**. La copia locale resta per i test |
| **C** | Retention dei QR (§3.4) | **seguono la retention delle prenotazioni**, non quella delle email |
| **C-bis** | I 494 scontrini orfani | **non indagare**: accettati come spiegati dalla data del dump (dettaglio e numeri in §3.5) |
| **B1–B4** | le quattro decisioni tecniche | confermate come proposte |

> ⚠️ **Conseguenza di A6, da tenere presente.** Se la policy cancella solo su AWS, la copia locale
> diventa col tempo **l'archivio completo di tutto ciò che è stato cancellato per policy**: foto di
> stabilimenti cessati, allegati scaduti, QR. Va bene come copia di sicurezza e per i test, ma
> **non è più la fotografia di «cosa deve esistere»**, e non va usata per ricostruire il bucket:
> ripristinandola si rimetterebbe in circolo tutto quello che la retention aveva cancellato.
> Se un giorno serve che la copia locale rispecchi lo stato corretto, serve un secondo passo che
> le applichi le stesse cancellazioni — da decidere allora, non adesso.

### B — Decisioni tecniche confermate

| # | Cosa | Scelta |
|---|---|---|
| **B1** | Cos'è «la cessazione»: non esiste uno stato (`property_status` = `pending \| approved \| rejected \| imported`) | fine dell'ultimo abbonamento, da `identity.account_subscription_periods.closed_at`, come già fa `80-retention.md` §5. Da scrivere **una volta** come definizione: oggi tre documenti la nominano in tre modi |
| **B2** | Come si conta «quante righe usano questo file» | query di risalita sulle 19 FK al momento della valutazione, senza vista materializzata né trigger (§5.4). Si rivaluta se i tempi del job lo richiedono |
| **B3** | Le nuove policy sui file partono attive o in prova? | **in prova** (`is_dry_run_only = true`): la prima corsa riporta cosa cancellerebbe senza cancellare |
| **B4** | Le 64 righe `catalog.media_assets` senza nessun utilizzatore | si guardano prima di cancellarle: R6 le renderebbe candidate, ma nessuna scheda le ha mai usate, quindi vanno capite non eliminate |

### C — Gli orfani: non sono una categoria sola

R6 dice «cancellare la scheda cancella i suoi file», ma non dice niente dei file che non hanno
**mai** avuto una scheda. Misurato il 2026-10-01, e vanno separati perché richiedono risposte
diverse:

| Gruppo | Oggetti | Peso | Cosa sono | Suggerimento |
|---|---:|---:|---|---|
| `images/qrcodescan/` | **743.576** | 1,2 GB | QR rigenerabili dal `reservation_code` (§3.4) | 🗑️ **a 12 mesi**, con `notification.deliveries`. Nel nuovo gestionale: non salvarli affatto |
| `images/bathouse/` | 9.117 | 804 MB | foto rimosse o sostituite dai gestori nel tempo, mai ripulite. Date dal 2021-03 al 2026-08 | 🗑️, ma dopo A3: parte potrebbe essere recuperabile |
| `images/configure/` | **2.216** 🔄 | **2,6 GB** | sfondi e template di mappa sostituiti. **Il peso più grosso dopo i QR** | 🗑️: sono versioni superate di un'immagine che è stata rimpiazzata, **con le tre esclusioni di §7-G2** |
| `file/` + `files[]/` + `files/newsletter/` | 490 | 165 MB | allegati di campagne che non esistono più | 🗑️, coerente con A1 |
| `images/property/` | 127 | 102 MB | come sopra per le foto delle strutture | 🗑️, dopo A3 |
| `reports/hotel/` | 28 | 67 KB | i PDF di A5 | 🔄 **rivisto in §7-G1**: sono file dell'ambiente di **sviluppo**, e 24 su 28 sono versioni superate. In produzione non esiste nessun report con un PDF: **non si migrano** |
| `pdf/` | 14 | 7 MB | listini sostituiti | 🗑️, coerente con R4 |
| `images/servicetypes/` | 10 | 21 KB | 🔄 **non icone superate**: le stesse 7 icone nelle categorie `hourly`/`normal`, mai aperte del tutto (§7-G2) | ✅ **conservare**: sono catalogo, utilizzabili da tutti i gestori |
| **`receipts/fiskaly/`** | **494** | 1,5 MB | **PDF di scontrini fiscali senza riga a database** | ⚠️ **non cancellare: indagare** |

**Tutti i gruppi tranne gli scontrini hanno 0 oggetti successivi al dump**: i loro orfani sono
genuinamente non referenziati, non un effetto della data del dump. Gli scontrini sono trattati in
§3.5 e chiusi.

#### Perché questi file sono rimasti, gruppo per gruppo

Non sono «file misteriosi»: hanno tutti la stessa causa, e **non è che il legacy non cancelli**.
Il legacy *prova* a cancellare, e fallisce in silenzio.

> ### Perché le cancellazioni del legacy non arrivano a destinazione
>
> `_multipleDeleteFileFromS3(ImageObject, path)` in
> [`globalConstants.js`](../../../../../back-pre/app/global/globalConstants.js) è chiamata da
> `bathouse.controller.js:406` ogni volta che un gestore rimuove delle foto dalla galleria. Ha tre
> difetti che si sommano:
>
> 1. **Non attende.** Il ciclo lancia `s3Bucket.deleteObject(data, callback)` per ogni file e fa
>    `return true` senza aspettare nessuna callback. Il chiamante aggiorna il database dando per
>    riuscita un'operazione che non è ancora cominciata.
> 2. **Ingoia gli errori.** La callback fa solo `console.log("Error: " + err)`. Un fallimento non
>    risale a nessuno e non impedisce al `return true` di dire che è andata bene.
> 3. **Ricostruisce la chiave dal solo nome del file**, concatenandolo alla cartella che il
>    chiamante passa: `Key: path + "/" + fileName`. E `bathouse.controller.js` passa **sempre**
>    `AWS_BATH_IMG_PATH` (`images/bathouse`). Quindi una foto archiviata in un'altra cartella —
>    per esempio `images/property/` — viene cancellata all'indirizzo `images/bathouse/<nome>`, che
>    non esiste: l'oggetto vero sopravvive, e la cancellazione "riesce".
>
> Poi, in tutti e tre i casi, `deleteNormalResponse == true` e l'array `photos` viene riscritto
> senza quella voce. **Il riferimento sparisce, il file resta.**
>
> Questo cambia la natura degli orfani: non sono file che nessuno ha mai deciso di cancellare, sono
> **cancellazioni che il legacy ha deciso e non ha portato a termine**. Rimuoverli adesso non è una
> decisione nuova — è finire un lavoro già deciso allora.

| Gruppo | Cosa è successo | Prova |
|---|---|---|
| `images/bathouse/` (9.117) | il gestore ha rimosso o sostituito foto dalla galleria; la cancellazione su S3 è stata tentata e non è arrivata (riquadro sopra), l'array è stato riscritto comunque | le date vanno dal **2021-03** al **2026-08**: coprono tutta la vita del gestionale, è un accumulo continuo, non un evento |
| `images/configure/` (**2.216**, **2,6 GB**) | ⚠️ **non sono icone**: sono gli **sfondi di mappa** caricati dai gestori (`configure_maps.templateImg`, una per stabilimento, `.jpeg` da oltre un MB). Ogni volta che un gestore ridisegna la spiaggia carica un nuovo sfondo e il vecchio resta | 175 righe `configure_maps` referenziano 175 sfondi; misurato il 2026-10-02: **2.448 oggetti**, **236 referenziati**, **2.216 orfani**, **4 riferimenti rotti**. Tutti gli orfani hanno nome `uniqid()`, quindi sono irriesumabili (§7-G2) |
| `file/` + `files[]/` + `files/newsletter/` (490) | allegati di campagne e newsletter cancellate negli anni | le righe che li nominavano non esistono più in `marketing_*` |
| `images/property/` (127) | come le foto degli stabilimenti, lato property | idem |
| `pdf/` (14) | listini e menù sostituiti con una versione nuova | `bathouses.priceListPDF`/`menuPDF` puntano a 6 file; nel prefisso ce ne sono 20 |
| `images/servicetypes/` (10) | 🔄 **non sostituite**: 7 icone (`burger, cocktail, coffee, fruit, mug, pool, tea`) replicate in 3 categorie, e per `hourly`/`normal` sono stati scelti solo `burger` e `cocktail`. Sono voci di catalogo mai messe in tavolozza, non versioni morte (§7-G2) | 11 referenziate, 21 presenti, **0 riferimenti rotti** |
| `reports/hotel/` (28) | 🔄 la chiave è `report_<id del report>_<epoch>`, **non** `<hotelId>`, e ogni rigenerazione del PDF sovrascrive `path` senza cancellare il file precedente: **24 su 28 sono versioni superate**, e i 4 correnti sono referenziati solo dal MySQL di **sviluppo** (§7-G1) | produzione: 2 righe, `AUTO_INCREMENT` = 3, `path` NULL su entrambe → gli `id` 4, 5, 6, 8 non sono **mai** esistiti in produzione |

> ⚠️ **Attenzione su `images/configure/`: in quel prefisso convivono due cose diverse.**
>
> - le **icone** del catalogo (`configure_images`, 89 righe) — sono «del gestionale», permanenti
>   per la risposta 5;
> - gli **sfondi di mappa** (`configure_maps.templateImg`, 175 righe) — `configure_maps` è
>   l'**unica** tabella di quelle «grafiche» che ha un `propertyId`: quegli sfondi sono materiale
>   **dello stabilimento**, non del gestionale, e pesano quasi tutto il 2,8 GB del prefisso.
>
> La risposta 5 («le icone non si cancellano mai») non li copre: non sono icone. Seguono lo
> stabilimento, come i listini. Vedi §7 punto C3.

### A4, C2, C3, C4 — decise il 2026-10-01

| # | Domanda | ✅ Decisione |
|---|---|---|
| **A4** | Le icone: la risposta 5 sostituisce la regola selettiva? | **sì.** Confermato anche dai dati: **tutte** le tabelle delle icone (`configure_images`, `servicetypes_icons`, `services`, `configure_categories`, `service_type_filters`) **non hanno nessuna colonna di appartenenza** — sono globali per costruzione, e «icone personalizzate di un singolo stabilimento» non esistono. L'ipotesi del questionario era infondata |
| **C2** | I 743.576 QR esistenti, non collegabili a nessuna prenotazione | **opzione (b), espressa come policy di retention**: cancellare gli oggetti con `LastModified` anteriore al **1 gennaio dell'anno N−2** (eseguita oggi: prima del **1 gennaio 2024**), usando il `LastModified` come data approssimata della prenotazione. Richiede la forma di policy di §5.5 bis e la regola di §9 |
| **C3** | Gli sfondi di mappa in `images/configure/` | **seguono lo stabilimento**, come i listini. Non sono icone: `configure_maps` è l'unica tabella grafica con `propertyId` |
| **C4** | Gli altri orfani si cancellano? Il codice prevede il recupero? | **no, non esiste nessun recupero** (sotto). Si cancellano con una policy basata sul flag della tabella che li referenzia — che nel nuovo schema **esiste già**: `deleted_at` |

#### C4 — il recupero non esiste, e il flag invece sì

**Nel legacy non c'è recupero, in nessuna forma.** Verificato:

- `bathouses.photos` è un array JSON **riscritto in blocco** a ogni modifica: la voce rimossa non
  lascia traccia;
- `bathouses` **non ha** nessuna colonna di cancellazione logica (nessun `isDeleted`, nessun
  `deletedAt`);
- nessuna tabella di storico o di archivio contiene foto: le sole colonne `*photo*` di tutto il
  database sono `bathouses.photos`, `bathouses.mainPhoto`, `properties.photos` e `mainPhoto` sulle
  tabelle delle prenotazioni;
- e la cancellazione su S3 era **già stata tentata** dal legacy (riquadro in §7-C): quei file sono
  cancellazioni fallite, non materiale che qualcuno ha scelto di conservare.

Quindi per gli orfani esistenti non c'è niente da recuperare e nessun flag da cui partire: si
cancellano con la policy su prefisso di §5.5 bis, la stessa forma dei QR.

**Per il futuro il flag esiste già, ed è quello che suggerivi.** `catalog.bathhouse_photos` ha
`deleted_at`, e così `catalog.media_assets` e le altre tabelle utilizzatrici. Quindi quando nel
nuovo gestionale un gestore rimuove una foto:

1. la riga di `catalog.bathhouse_photos` prende `deleted_at` — **la foto resta referenziata**, da una
   riga cancellata logicamente;
2. `CompactSoftDeleted`, già in catalogo in [`80-retention.md`](80-retention.md), rimuove le righe
   con `deleted_at` oltre **90 giorni**;
3. da quel momento il file **non è più referenziato da nessuno**, e lo raccoglie la stessa policy
   `only_if_unreferenced` che serve agli allegati (A1).

Non serve nessun meccanismo nuovo: i 90 giorni di `CompactSoftDeleted` diventano di fatto la
finestra di recupero di una foto rimossa per errore, e la cancellazione del file segue
automaticamente. **Questo è il modo in cui il problema non si ripresenta**: l'unica cosa da
garantire è che il nuovo gestionale faccia soft-delete e non rimuova la riga di colpo.

> 📌 **Ordine che conta**: A3 (recupero di `properties.photos`) va **prima** di C4. Se si cancellano
> gli orfani di `images/bathouse/` e poi si implementa il recupero, si sono cancellati file che il
> recupero avrebbe rimesso in gioco.

### E — decise il 2026-10-01

| # | Domanda | ✅ Decisione |
|---|---|---|
| **E1** | «N−2 stagioni» contato da quando? | **il confine è il 1 gennaio, non il 1 aprile**, e la soglia si **ricalcola a ogni esecuzione**. Eseguita nell'ottobre 2026, la policy dei QR cancella tutto ciò che precede il **1 gennaio 2024**. ⚠️ Vale per **tutte** le policy stagionali, non solo i file: vedi §9 |
| **E2** | Gli sfondi di mappa di uno stabilimento cessato | **+2 anni** dalla cessazione, con la configurazione (non subito come l'audio) |
| **E3** | Il nuovo gestionale farà soft-delete sulle foto? | **sì.** È il presupposto di tutto il meccanismo di C4 per il futuro |

### G — Le tre domande: **chiuse il 2026-10-02**

Due si sono chiuse guardando il codice legacy e i tre database, come chiesto; le altre decisioni
sono del titolare. **Non resta aperta nessuna domanda di questa sezione.** Qui sotto: cosa è
risultato e cosa ne consegue.

#### G1 — La funzione «report alle strutture convenzionate»: che cosa fa il codice legacy

🔍 **Indagato il 2026-10-02 su richiesta.** Il verdetto cambia la domanda: la funzione **esiste ed è
completa nel legacy**, ma **in produzione non ha mai prodotto un solo PDF**, e i 28 file sul bucket
**non sono di produzione**.

**1. La funzione c'è, ed è finita.** Non è un abbozzo: 10 endpoint in
`back-pre/app/controllers/hotel/hotelReport.controller.js` (crea, elenca, dettaglio, annulla,
annulla in blocco, note, PDF, invia, invia messaggio, anteprima), il modello `hotel_reports` con 14
colonne e 5 indici, il generatore di PDF (`mailtemplates/hotelReportPdfTemplate.js`), i template di
messaggio bilingui (`hotelReportTemplates.js`) e l'invio via **email** (PDF allegato) o **WhatsApp**
(link `wa.me` con l'URL del PDF nel testo) in `Service/hotelReportNotificationService.js`.

**2. ⚠️ Correzione: il numero nel nome del file è l'`id` del report, non l'`hotelId`.** La chiave la
costruisce `storeReportPdf`:

```js
const key = `${AWS_HOTEL_REPORT_PATH}/report_${report.id}_${Date.now()}.pdf`;
```

Quindi l'attribuzione precedente dei 28 PDF alla property **10761** (ricavata leggendo quel numero
come `hotelId`) **non vale**. Vale quella che segue.

**3. I 28 PDF vengono dall'ambiente di *sviluppo*, non dalla produzione.** La prova è in tre numeri:

| Database | Righe in `hotel_reports` | `id` presenti | `AUTO_INCREMENT` | `path` valorizzati |
|---|---:|---|---:|---:|
| Produzione (`u811491364_summer_booking`) | **2** | 1, 2 | **3** | **0** |
| Sviluppo (`summer_booking_dev` su RDS) | **8** | 1…8 | 9 | **4** (id 4, 5, 6, 8) |

In produzione l'`AUTO_INCREMENT` è **3**: le righe con `id` 4, 5, 6 e 8 **non sono mai esistite**.
Sul MySQL di sviluppo ci sono, e i loro `path` contengono **esattamente quattro** dei 28 URL del
bucket. I due ambienti condividono lo stesso bucket, quindi i file di sviluppo sono finiti fra
quelli di produzione.

In più, in produzione l'invio era **tecnicamente impossibile**: `hotel_message_templates` ha **0
righe**, e il servizio pretende un template per comporre il messaggio. Le 2 righe di produzione sono
una `cancelled` (30 agosto) e una `created` (2 settembre), entrambe con `path` NULL: qualcuno ha
provato la pagina due volte e non ha mai generato un PDF.

**4. Perché 28 file per 4 report: il legacy sovrascrive il riferimento e non cancella niente.** Ogni
chiamata a `storeReportPdf` — e la fa **sia** l'endpoint «scarica il PDF» **sia** quello di invio —
rigenera il PDF, lo carica con un `Date.now()` nuovo e fa `report.update({ path: pdfUrl })`. Il file
precedente resta sul bucket senza nessuna riga che lo nomini. Misurato: i 4 `path` di sviluppo
puntano al file con l'epoch **più alto** di ciascun gruppo, quindi **24 dei 28 sono versioni
superate** e 4 sono i correnti. Sono tutti da **2.387 a 2.520 byte** (PDF di prova quasi vuoti),
**~68 KB in tutto**.

**5. Il file archiviato non serve a niente, nemmeno nel legacy.** Nessun punto del codice rilegge
`hotel_reports.path`: il PDF viene **rigenerato** da `reportData` a ogni richiesta, l'email lo allega
dal buffer in memoria, e l'unico uso dell'URL è il link dentro il messaggio WhatsApp. La copia su S3
è un effetto collaterale dell'avere bisogno di un link pubblico per WhatsApp.

**Conseguenze sulle decisioni già prese:**

- **A5 «recupera»** non si può applicare alla lettera: in produzione **non c'è niente da
  recuperare** — nessuna riga e nessun PDF. I 28 file sono materiale di collaudo di luglio 2026.
- La regola a **24 mesi** di [`80-retention.md`](80-retention.md) poggiava su `report_<hotelId>_<epoch>`
  e su un archivio che non esiste: va rifatta.

**✅ Deciso dal titolare il 2026-10-02: la funzione si rifà, e archivia** — opzione (a). Quindi:

- la regola a **24 mesi** resta valida, ma decorre dalla **data del report**, non da quella ricavata
  dal nome del file;
- serve una **tabella nuova** nel nuovo schema, che oggi non c'è;
- i 28 PDF di collaudo **non si migrano**: sono di un altro ambiente e non hanno nessuna riga di
  produzione che li nomini.

##### Dove vanno le due cose, nel nuovo schema

La legacy `hotels` è già stata rifatta come **`catalog.agreement_partners`** (112 righe, sottoarea
`agreement` del dominio C2, con `catalog.agreement_price_lists` e `booking.agreement_lines`). Manca
solo il report. Proposta, coerente con quella nomenclatura e con la divisione già in uso fra i quattro
`*media_assets`:

| Cosa | Dove | Perché |
|---|---|---|
| la **riga** del report (periodo, `search_type`, totali, `report_data`, note, autore, stato) | 🆕 **`catalog.agreement_reports`** — figlia di `catalog.agreement_partners` | sta con l'anagrafica e i listini della convenzione, nella stessa sottoarea `agreement`. Il dato grezzo resta in `booking.*`: `report_data` è una **cache**, non la fonte |
| il **PDF archiviato** | **`notification.media_assets`** | già esiste, già ha `property_id` **e** `uploaded_by_user_id`, e `notification.media_kind` ha già l'etichetta giusta: **`rendered_pdf`**. Nessuna etichetta enum nuova, nessuna colonna nuova |

Questo è anche il motivo per cui **F5 torna nel seed esattamente com'era disegnata**
(sezione **FILE** del seed in
[`privacy_retention_policies`](../analisi-post/db/privacy/privacy_retention_policies.md)):
`notification.media_assets`, `created_at`, 24 mesi,
`delete_object`. Non cambia la policy — cambia che ora la data è **vera**. Il PDF viene archiviato
nello stesso momento in cui il report è generato, quindi `media_assets.created_at` **è** la data del
report, a meno di secondi: non serve una `JOIN` sulla riga del report per leggerla.

> ⚠️ **Due difetti del legacy da non riprodurre nella tabella nuova.**
>
> 1. **Ogni rigenerazione del PDF sovrascriveva `path` senza cancellare il file precedente.** È da qui
>    che vengono 24 dei 28 oggetti. Nel nuovo gestionale: o la rigenerazione **sostituisce** l'asset
>    (cancellando l'oggetto vecchio, cioè passando dalla coda di §5.3), o ogni versione è un asset suo
>    con la sua data — ma non si può avere una colonna sola che punta all'ultima e nessuno che pulisce
>    le altre.
> 2. **L'endpoint «scarica il PDF» archiviava.** Nel legacy *anche* la semplice anteprima/scaricamento
>    chiamava `storeReportPdf`, cioè caricava su S3. Si archivia **quando il report viene inviato**,
>    non quando qualcuno lo guarda: altrimenti l'archivio si riempie di versioni che nessuno ha mai
>    mandato a nessuno. È il difetto che ha prodotto 11 oggetti per un solo report.

Per il link WhatsApp, in ogni caso: **URL firmato a scadenza breve** sul bucket privato nuovo. Il
bucket nuovo ha ACL `private`, quindi l'URL pubblico permanente che usava il legacy non è più
possibile — e non è una perdita, è la ragione per cui il bucket nuovo è privato.

#### G2 — Icone: i file orfani sono riesumabili dal codice legacy? **Misurato, e la risposta cambia per prefisso**

🔍 **Indagato il 2026-10-02 su richiesta.** Le due risposte R5 e C4 **non si contraddicono**: una
volta misurati, gli insiemi non si sovrappongono. Ma la ragione non è quella che avevo ipotizzato —
non sono «versioni sostituite» — e su un prefisso la mia lettura era sbagliata.

**Precisazione del titolare (2026-10-02):** «le icone del gestionale» di R5 sono le icone
**utilizzabili da tutti i gestori**, cioè il catalogo offerto dal gestionale — non un file qualsiasi
che sta in una cartella di icone.

**Che cosa può riesumare un file, nel legacy:**

| Meccanismo | C'è? | Verifica |
|---|---|---|
| Elencare il bucket e mostrare i file trovati | **no** | in tutto `back-pre/app` ci sono **zero** `listObjects`/`listObjectsV2`, e solo 2 `getObject`, entrambi per l'audio del centralino (`utils/audioUpload.js`). Gli URL nascono **solo** dalle colonne a database |
| Un endpoint di ripristino / annulla-cancellazione | **no** | nessun `restore`/`undelete` in tutto il backend |
| Riattivare una voce nascosta tramite un **flag** | **sì** | `servicetypes_icons.status` e `configure_images.status` |
| Aggiungere una voce al catalogo dall'applicazione | **no** | `/api/public/servicetypeicons` è in **sola lettura** e filtra `status: true`; non esiste nessun endpoint di scrittura. Il catalogo si estende solo con un INSERT a mano |

**⚠️ Il flag `status` è il vero caso «riesumabile», e c'è un esempio vivo.**
`servicetypes_icons` ha **60 righe `status=1` e 1 riga `status=0`**: la riga 6,
`images/servicetypes/bar/pool.png`. È **nascosta dalla tavolozza** ma il file è ancora **usato da una
riga** di `services_types.barServices`. Basta rimettere `status=1` e torna offerta a tutti.
**Regola che ne deriva: un file dietro una riga `status=0` non si cancella mai** — non è un orfano, è
una voce di catalogo in panchina. Oggi riguarda 1 file; domani, dopo qualunque pulizia della
tavolozza, ne riguarderebbe molti.

**I tre prefissi, misurati sul bucket di produzione il 2026-10-02:**

| Prefisso | Oggetti su S3 | Referenziati | **Orfani** | Riferimenti rotti | Che cosa sono |
|---|---:|---:|---:|---:|---|
| `images/services/` | **49** | **49** | **0** | 0 | il catalogo delle icone di servizio: **perfettamente pulito** |
| `images/servicetypes/` | **21** | **11** | **10** | 0 | 7 icone (`burger, cocktail, coffee, fruit, mug, pool, tea`) × 3 categorie (`bar, hourly, normal`) |
| `images/configure/` | **2.448** | **236** | **2.216** | **4** | due scrittori nella stessa cartella: il catalogo e gli sfondi di mappa dei gestori |

**`images/servicetypes/` — qui avevo torto: i 10 non sono versioni superate, sono catalogo.** Sono
lo *stesso* insieme di 7 icone replicato in 3 categorie. La tavolozza `bar` è completa (7 righe,
`id` 1–7); per `hourly` e `normal` sono stati scelti solo `burger` e `cocktail`, e il resto di quelle
due tavolozze punta a `images/services/*`. I 10 non referenziati sono dunque **icone del catalogo di
una tavolozza mai aperta del tutto**: sono esattamente «icone utilizzabili da tutti i gestori».
→ **R5 vince: non si cancellano.** Costo: 10 file di pochi KB. E dato che il catalogo si estende solo
con un INSERT, nel nuovo gestionale sono riesumabili in un modo preciso: aggiungere la riga dalla
pagina di systemadmin.

**`images/configure/` — qui i 2.216 orfani non sono riesumabili da nessuna strada.** Il prefisso è
condiviso da due scrittori che `AWS_CONF_IMG_PATH` mette nella stessa cartella:

- `confimages.controller.js` → il **catalogo** (`configure_images`): 89 righe, **89 immagini
  distinte**, **tutte `status=1`**. Zero voci superate, zero voci nascoste: del catalogo non avanza
  **nulla**;
- `confimaps.controller.js` → gli **sfondi di mappa e gli ombrelloni** del singolo stabilimento
  (`configure_maps`, 175 righe, 14 colonne di immagine).

Quindi **tutti** i 2.216 orfani sono **caricamenti di gestori sostituiti**: ogni volta che un gestore
cambia lo sfondo della mappa, `UploadBase64OnS3` scrive un file nuovo con nome `uniqid()` e il
vecchio resta. Non sono riesumabili perché il nome è **casuale e senza significato**, il database
tiene solo il corrente, non c'è storico delle versioni, non c'è elenco e non c'è ripristino.
→ **C4 vale: si cancellano.** Sono anche il peso vero del prefisso (2,8 GB di `.jpeg`).

**La regola che ne esce, da scrivere nella policy sugli orfani:**

1. **mai** cancellare sotto `images/services/` e `images/servicetypes/` — sono cataloghi;
2. **mai** cancellare un file nominato da una riga con `status = false` (oggi: 1);
3. sotto `images/configure/` cancellare **solo** gli oggetti che nessuna riga nomina **e** che non
   sono nel catalogo `configure_images` — in pratica i caricamenti `uniqid()` sostituiti;
4. 🆕 **4 riferimenti rotti** da segnalare, non da cancellare: `configure_maps` nomina 4 file che su
   S3 **non ci sono più** (`2yjaexoknp06w01.jpeg`, `2yjah9bknqlkcea.jpeg`, `2yjah9bknqll8az.jpeg`,
   `2yjah9bknqlm0zd.jpeg`). Sono mappe di stabilimenti che oggi mostrano un'immagine mancante.

> ⚠️ **Il conteggio di `images/configure/` va corretto nei documenti**: non 2.195 orfani, ma **2.216
> orfani su 2.448 oggetti**. La cifra precedente veniva da una ricognizione parziale.

#### G3 — Le 296 foto di stabilimenti senza vetrina: ✅ **(b)**, deciso dal titolare

| `status` | Significato | Foto | Esito |
|---:|---|---:|---|
| 0 | registrato, in attesa di approvazione | 175 | ✅ **si conservano** |
| 1 | approvato, senza vetrina | 97 | ✅ **si conservano** |
| 2 | **rifiutato** dal system admin | 24 | 🗑️ **si scartano** |

**272 foto si migrano come asset senza aggancio**, in attesa che la vetrina esista; **24 si
scartano** con motivo esplicito nel registro.

Conseguenze operative, da riportare nella spec dell'ETL:

- il recupero di `properties.photos` crea la riga in `catalog.media_assets` anche quando non esiste
  nessuna `bathhouse_photos` da collegare: le 272 restano **senza riga di aggancio**, non vengono
  scartate;
- quelle 272 **non devono finire sotto la policy degli orfani**, altrimenti il job le cancella il
  giorno dopo averle migrate. ✅ **Deciso il 2026-10-02 come si distinguono: con `property_id`
  sull'asset**, cioè la §5.5 di questo documento, che era stata archiviata come «non più urgente» e
  con G3 (b) torna necessaria.

  > ⚠️ **Correzione.** La prima proposta era un'etichetta `media_origin` dedicata, e **non regge**:
  > `catalog.media_origin` non descrive la provenienza legacy ma **l'host dello storage** — le sue
  > etichette sono `s3us_east2`, `s3us_east1`, `cloudfront`, `external`. Aggiungerci
  > `legacy_property_photo_unattached` metterebbe due significati diversi nello stesso enum, e
  > romperebbe la lettura di `origin` per chiunque la usi per costruire un URL.
  >
  > `property_id` invece dice la cosa vera: **la property esiste, è la vetrina che manca.** La query
  > delle policy diventa «nessun riferimento **e** nessuna property», che è la definizione corretta di
  > orfano. C'è anche il precedente: `notification.media_assets` ha già `property_id` (`uuid`,
  > nullable) ed è la stessa esigenza. Dettaglio nella scheda di
  > [`catalog_media_assets`](../analisi-post/db/catalog/catalog_media_assets.md);
- le 24 dei rifiutati vanno nel registro degli scarti con motivo `property rifiutata (status=2),
  nessuna vetrina`.

### F — Lavoro, in ordine

1. ✅ **`80-retention.md` allineato.** Le due righe sui cataloghi master sono corrette (la vecchia
   regola resta visibile barrata, è un verbale), ed è stata aggiunta la riga sugli sfondi di mappa.
2. ✅ **Le tre domande di §7-G sono chiuse** (2026-10-02). Riportate nelle regole: la nuova forma
   della policy sugli orfani (G2, quattro punti), l'esito dei PDF dei report (G1) e il recupero
   parziale delle foto (G3 → (b)).
3. 🔧 **Il recupero di `properties.photos`** (A3) — annotato in
   [`analisi-post/db/catalog/catalog_properties.md`](../analisi-post/db/catalog/catalog_properties.md)
   e nella spec dell'ETL, **con l'esito (b) di G3**. **Va prima** della cancellazione degli orfani.
4. ✅ **Le migration e il seed sono applicati** (2026-10-03): sette migration e 15 righe di policy,
   elenco e scostamenti nel riquadro di §5.
5. ✅ **La utility `02-migrazione-file` è allineata** (2026-10-03): `verify_links()` ha un terzo
   esito, `owned_without_users` — nessuna riga di aggancio **ma** `property_id` valorizzato, quindi si
   travasa invece di finire fra gli orfani. Il gruppo `report_hotel` è passato agli orfani attesi.
   ⚠️ La corsa di prova ha fatto emergere che il caso esiste già su
   `switchboard.switchboard_media_assets`, dove un audio caricato e non ancora assegnato a una voce
   di menù era classificato «candidato allo scarto».
6. 🔧 **Il recupero di `properties.photos`** — ora ha la colonna che gli serviva. Resta da
   implementare nell'ETL.
7. 🔧 **I job (§6)**, da non scrivere fino a indicazione. Quattro, più la **funzione del confine
   stagionale**: va scritta **una volta** e condivisa da tutti i job stagionali, perché se ognuno se
   la ricalcola prima o poi uno userà il 1 aprile — l'errore che la documentazione conteneva fino al
   2026-10-01 (vedi §9 e [`20-jobs.md`](20-jobs.md)).

✅ **Non resta aperta nessuna decisione.** Le ultime due si sono chiuse il 2026-10-02: la funzione
dei report **si rifà e archivia** (G1 → (a)), e le 272 foto senza vetrina si distinguono con
**`property_id`** sull'asset (G3, §5.5). Anche le **due scelte tecniche** sono state adottate e
applicate: la cessazione si **deriva** da `closed_at` (definizione in
[`80-retention.md` §5](80-retention.md), chiude D-65 punto 1) e l'indice unico si **estende** con
`reference_event` (vedi la scheda di
[`privacy_retention_policies`](../analisi-post/db/privacy/privacy_retention_policies.md)).

### L'ordine in cui va fatto il resto, e perché non è una sequenza su un database vivo

Il committente ha deciso che le correzioni dell'ETL si applicano con un **rerun completo**: drop
degli schemi, `dotnet ef database update` sugli 11 moduli, poi `run_headless.py --clean --relink`,
~6 ore ([`_colonne-vuote.md` Q7](../analisi-pre/db-no-copied/_colonne-vuote.md)). Questo cambia il senso dell'elenco:

- le sette migration **non** sono un intervento da ripetere a mano: entrano nella catena e vengono
  applicate dal `database update` della corsa. Il seed è a sua volta una migration, quindi si
  riapplica da sé — ed è il motivo per cui doveva essere idempotente;
- il **recupero di `properties.photos`** (10.097 file) va **dentro** quella corsa, non dopo: se si
  rifà la migrazione da zero senza averlo pronto, le foto vanno recuperate con un backfill a parte,
  cioè esattamente il lavoro in più che il rerun dovrebbe evitare;
- la cancellazione degli orfani (policy F9 e F11a) va **dopo** quel recupero, o cancella file che il
  recupero avrebbe rimesso in gioco;
- i job vanno per ultimi, e si accendono **una policy per volta** leggendo il report di dry-run:
  `is_dry_run_only = false` non va messo a tutte e 59 insieme.

> 📌 **Una cosa che *non* è una domanda — e non è un suggerimento di tornare al 1 aprile.** Il
> confine è il **1 gennaio**: deciso in E1, già applicato in tutti i documenti, e non si rimette in
> discussione. Il numero qui sotto serve a una domanda diversa — *«le 9 finestre stagionali già
> validate vanno rivalidate col significato nuovo?»* — e la risposta è **no**: il cambio di confine è
> praticamente irrilevante sui dati reali. Misurato su 261.011 prenotazioni legacy, solo **1.514
> finiscono fra gennaio e marzo** — lo **0,58%**, ed è l'unico intervallo in cui i due confini danno
> un esito diverso. Valeva la pena misurarlo perché una di quelle policy azzera le `note` dei
> clienti, che è irreversibile.

---

## 9. Il confine della stagione è il 1 gennaio — e riguarda tutto, non solo i file

✅ **Deciso il 2026-10-01.** È nato dalla domanda E1 sui QR, ma la portata è più ampia: **tocca le
9 policy che usano `duration_unit = 'seasons'`**, otto in `booking` e una in `identity`.

### La regola

> Ai fini della retention **la stagione è l'anno di calendario**. La soglia di `N−k` è il
> **1 gennaio dell'anno (anno di esecuzione − k)**, e si **ricalcola a ogni esecuzione**.

Eseguita nell'ottobre 2026: `N−1` → prima del 1 gennaio 2025; `N−2` → prima del 1 gennaio 2024.

**Non si usano i confini per-stabilimento** di `seasons_periods` / `catalog.pricing_seasons`.
È una semplificazione sostanziale, e vale la pena dirla: quelle tabelle hanno un intervallo di
stagione **diverso per ogni stabilimento** (`uq_catalog_pricing_seasons_property`), quindi la
retention avrebbe avuto una soglia diversa per ognuno dei 10.431 stabilimenti. Con l'anno di
calendario la soglia è **una sola per tutti**, e la retention non dipende più da `pricing_seasons`.

**La data di esecuzione del job resta separata dalla soglia.** `SeasonalArchive` può continuare a
girare il 1 aprile (`0 4 1 4 *`): eseguito il 1 aprile 2027, `N−2` resta «prima del 1 gennaio
2025». Quando gira cambia *quando* la cancellazione avviene, non *cosa* viene cancellato.

### Le 9 policy interessate

| Schema | Tabella | Colonna data | Finestra | Azione |
|---|---|---|---|---|
| booking | `reservations` | `stay_to` | 1 stagione | `null_fields` (`note`, `customer_tags`) |
| booking | `reservations` | `stay_to` | 2 stagioni | `archive_cold` |
| booking | `lines` | `end_date` | 2 stagioni | `delete_row` |
| booking | `service_lines` | `service_date` | 1 stagione | `delete_row` |
| booking | `resource_consumptions` | `consumption_date` | 1 stagione | `delete_row` |
| booking | `resale_offers` | `offer_date` | 2 stagioni | `delete_row` |
| booking | `spot_holds` | `end_date` | 2 stagioni | `delete_row` |
| booking | `groups` | `created_at` | 2 stagioni | `delete_row` |
| identity | `customer_customers` | `last_activity_at` | 1 stagione | `null_fields` (`note`, `discount_note`) |

### Il seed **non** va modificato

Verificato in `20260921074959_SeedRetentionPolicies.cs` (dal 2026-10-06 consolidata nella
`InitialCreate` di `Module.Privacy`, vedi [`42-migration.md` §10](42-migration.md)):
il seed scrive `duration_value` + `duration_unit = 'seasons'` e **non contiene nessuna data di
confine**. La soglia non è un dato: è l'interpretazione di «stagione», e vive nel job che la
applica — che non è ancora scritto. Quindi le 44 righe a database restano valide così come sono, e
non serve nessuna migration.

**Dove va scritta la regola**: nel job, una volta sola, in una funzione condivisa da tutte le policy
stagionali. Se ogni job se la ricalcola per conto proprio, prima o poi uno userà aprile.

### Documenti aggiornati

| Documento | Cosa diceva | Cosa dice ora |
|---|---|---|
| [`82-retention-data.md`](82-retention-data.md) | «Stagione (S): circa 12 mesi, ancorata a `seasons_periods` … il batch gira all'apertura della stagione (indicativamente 1 aprile)» — confondeva **quando gira** con **dove taglia** | anno di calendario, confine 1 gennaio, e la distinzione fra soglia e data di esecuzione |
| [`80-retention.md`](80-retention.md) §3 | stessa confusione, più «ancorata ai confini in `seasons_periods`» | regola esplicita, esempio numerico, e il ricalcolo a ogni esecuzione |
| [`80-retention.md`](80-retention.md), riga `SeasonalArchive` | solo «N−2 / N−1» | aggiunta la soglia al 1 gennaio |
| [`20-jobs.md`](20-jobs.md), riga `SeasonalArchive` | cron `0 4 1 4 *` senza dire dove taglia | avviso esplicito: la soglia non è la data di esecuzione, con l'esempio del 2027 |

---

## 8. Dove sono i numeri citati

Tutti misurati, non stimati:

| Dato | Fonte |
|---|---|
| 44 policy, nessuna sui file; enum e colonne | PostgreSQL `summer_booking_dev`, 1 ottobre 2026 |
| `property_status` senza «cessato»; `infra` senza tabella dei job | idem |
| 11.040 asset: 10.954 con stabilimento, 22 master, 64 senza utilizzatori | `utility/02-migrazione-file`, passo `collegamenti` |
| `Pdf/` e `lifeguardPdf/` inesistenti su S3; 16.882 riferimenti locali | ricognizione del bucket di produzione, 1 ottobre 2026 |
| 20.377 foto per 1,5 GB; 23.702 file totali per 1,8 GB | idem, più la passata `HEAD` |
| 28 PDF in `reports/hotel/`; `hotel_reports.path` NULL su 2 righe | bucket + MySQL `u811491364_summer_booking` |
| L'ETL non migra i PDF bagnino | `utility/01-migrazione-test/mappings/notification/notification_media_assets.yaml` |

Il dettaglio della ricognizione del bucket è in
[`utility/02-migrazione-file/TODO.md`](../../../../../utility/02-migrazione-file/TODO.md) §1bis e §1quater.
