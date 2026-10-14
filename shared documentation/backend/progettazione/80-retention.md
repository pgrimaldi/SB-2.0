# Retention dei dati — categorie e tempi di conservazione

> ## ⚠️ Aggiornamento del 2026-10-01 — i **file** sono trattati in [83-retention-file.md](83-retention-file.md)
>
> Il titolare ha risposto alle sei domande aperte sulla conservazione dei file
> ([`retention-file-con-risposte.docx`](retention-file-con-risposte.docx)). Due risposte
> **cambiano quello che c'è scritto qui**, e vanno lette prima di implementare le righe
> corrispondenti:
>
> - ✅ **Cataloghi master (icone): regola aggiornata il 2026-10-01.** La vecchia regola «le voci
>   usate da un solo stabilimento si cancellano alla cessazione, per riga non per tabella» è
>   **superata e confermata tale dal titolare**: le icone sono del gestionale e non si cancellano
>   mai. Le due righe (§4 e §7) sono state corrette. Verificato che nessuna tabella di icone ha una
>   colonna di appartenenza, quindi «voci di un solo stabilimento» non esistevano nei dati.
> - ✅ **Aggiunta una riga nuova**: gli **sfondi di mappa** (`configure_maps`, l'unica tabella
>   grafica con `propertyId`) stanno nella stessa cartella S3 delle icone ma **non sono icone**:
>   seguono lo stabilimento e si cancellano alla cessazione. Sono 2,8 GB contro i pochi KB delle
>   icone.
> - **PDF dei report del bagnino** — non sono mai stati su S3: i 16.882 riferimenti sono percorsi
>   della cartella locale del cronjob. Non c'è niente da archiviare né da cancellare (83 §2.1).
>   Questo documento non li nomina (non erano emersi nell'analisi), quindi non c'è nulla da
>   correggere qui: va solo saputo prima di aggiungerli.
>
> Restano invariate due delle tre regole sui file già concordate e citate qui: **scontrino
> elettronico emesso** e **config centralino / audio**. La terza — **report periodici alle
> strutture convenzionate** — è stata rifatta, vedi sotto.
>
> ## 🔄 Aggiornamento del 2026-10-02 — tre punti chiusi guardando il codice legacy
>
> - **I PDF dei report alle strutture convenzionate: la regola resta, la decorrenza era sbagliata.** Il legacy rigenera il
>   PDF a ogni richiesta e non rilegge mai `hotel_reports.path`; in produzione non ne ha mai
>   prodotto nemmeno uno (2 righe, `path` NULL su entrambe, `AUTO_INCREMENT` = 3), e i 28 file sul
>   bucket sono dell'ambiente di **sviluppo**. ✅ **Il titolare ha deciso che la funzione si rifà e
>   che archivia**: la regola a **24 mesi resta**, ma decorre dalla **data del report** e non più da
>   quella ricavata dal nome del file, che era letta male (il numero è l'`id` del report, non
>   l'`hotelId`). Servono una **tabella nuova** — `catalog.agreement_reports`, oggi inesistente — e
>   l'archiviazione del PDF in `notification.media_assets` con `kind = 'rendered_pdf'`. Due difetti
>   del legacy da non riprodurre: `path` sovrascritto senza cancellare il file precedente, e
>   l'archiviazione anche della semplice anteprima. Righe corrette in §3.11, §4 e §6; dettaglio in
>   [83 §7-G1](83-retention-file.md), schema in
>   [`catalog_agreement_reports.md`](../analisi-post/db/catalog/catalog_agreement_reports.md).
> - **«Icone del gestionale» significa «utilizzabili da tutti i gestori».** Precisazione del
>   titolare. Ne derivano due esclusioni dalla policy sugli orfani che prima non c'erano: le voci
>   di catalogo **non ancora in tavolozza** (i 10 file `hourly`/`normal` di `images/servicetypes/`)
>   e quelle **nascoste con `status = false`**, che un flag riporta in tavolozza. Verificato che
>   nel legacy **non esiste nessun altro modo** di riesumare un file: zero `listObjects` in tutto
>   il backend, nessun endpoint di ripristino, e il catalogo si estende solo con un INSERT. Vedi
>   [83 §7-G2](83-retention-file.md).
> - **Le foto di stabilimenti senza vetrina si recuperano in parte**: si conservano le **272** di
>   property in attesa di approvazione o approvate, si scartano le **24** di property rifiutate.
>   ⚠️ Le 272 restano **senza riga di aggancio** e vanno escluse dalla policy sugli orfani,
>   altrimenti il job le cancella il giorno dopo la migrazione. ✅ Si distinguono con **`property_id`
>   su `catalog.media_assets`** (vedi
>   [`catalog_media_assets.md`](../analisi-post/db/catalog/catalog_media_assets.md)) — *non* con
>   un'etichetta `media_origin`, che descrive l'host dello storage e non la provenienza. Vedi
>   [83 §7-G3 e §5.5](83-retention-file.md).

> Politica di **conservazione e cancellazione** dei dati applicativi di SummerBooking, su due assi che
> vanno tenuti insieme:
>
> 1. **Legale / privacy** — quanto si **deve** (obbligo fiscale/contabile) o si **può** (GDPR: art. 5.1.e
>    limitazione della conservazione, art. 5.1.c minimizzazione) tenere un dato.
> 2. **Pratico / alleggerimento DB** — quanto **conviene** tenerlo online per non zavorrare il database
>    con dati che non servono più a nessun flusso operativo (prenotazioni di due stagioni fa, contatori
>    di inventario, snapshot JSON, tabelle di archivio).
>
> Complemento operativo alla voce *"Crea una lista di categorie di tipo di dato e tempi di retention"* e
> *"Batch per archiviazione dati vecchi"* di [note.md](note.md). Si aggancia a:
> [11-modulo-privacy.md §4–§5 — PII per tabella e API di cancellazione](11-modulo-privacy.md),
> [40-backup.md — backup e disaster recovery](40-backup.md),
> [15-logs.md §2 — dove salvare i log e per quanto](15-logs.md),
> [20-jobs.md — job schedulati](20-jobs.md) (i batch di retention sono job come gli altri),
> [03-gestione-dati.md §3.5 — soft-delete uniforme `deleted_at`](03-gestione-dati.md),
> [analisi-pre/db/_feature-tabelle.md](../analisi-pre/db/_feature-tabelle.md) (mappa tabella→dominio).
>
> **I tempi qui proposti sono una base di lavoro, non un parere legale**: la riga "retention minima"
> (obblighi fiscali, provvedimenti del Garante) va confermata con il commercialista e con un consulente
> privacy/DPO; la riga "retention operativa consigliata" è una scelta tecnica del team, modificabile.

> ### Stato: politica **validata dal titolare** (2026-09-17)
>
> Le colonne "Retention operativa" e "Azione a scadenza" del §3, la cascata del §5, i job del §6 e la
> cheat sheet del §7 riportano ora i valori **decisi**, non più solo proposti. Il verbale riga-per-riga
> è in [82-retention-data.md](82-retention-data.md) (colonna *"Considerazioni / scelta del
> proprietario"*, più i chiarimenti Q1–Q6 del 2026-09-17), che resta la **fonte autorevole** in caso di
> divergenza con questo documento.
>
> - Dove il titolare ha **scostato** dalla proposta iniziale, la cella porta il valore validato seguito
>   da *(proposta: …)* — la proposta resta visibile per non perdere la traccia del ragionamento.
> - Dove ha confermato, il valore è invariato.
> - Resta **non validata** (e da confermare con commercialista/DPO) la sola colonna **"Retention minima
>   (legale)"**: il titolare ha deciso i tempi *operativi*, non ha dato un parere legale.
> - I valori validati sono anche **dati**, non solo prosa: alimentano il seed di
>   [`privacy_retention_policies`](../analisi-post/db/privacy/privacy_retention_policies.md) (§ Seed) e
>   le finestre online esposte dalle API in
>   [`analisi-post/bl/02-errore-api-dati-archiviati.md`](../analisi-post/bl/02-errore-api-dati-archiviati.md).

---

## 0. Principi guida

1. **Due orologi, si tiene il più lungo.** Per ogni dato: `retention_effettiva = max(obbligo_legale,
   necessità_operativa)`. Non si cancella un dato ancora dovuto per legge; non si tiene online un dato
   che nessun obbligo impone e nessun flusso legge.
2. **Backup prima di cancellare, sempre.** Nessuna cancellazione massiva senza un export verificato su
   storage separato (S3 + lifecycle Glacier, account/bucket distinto dal primario — vedi
   [40-backup.md](40-backup.md)). Il dato "sparisce dal DB caldo", non "sparisce".
3. **Tre azioni a scadenza, non una sola:**
   - **Hard delete** — riga rimossa fisicamente. Per dati senza vincoli di integrità né obblighi
     (marketing, log diagnostici, blocchi mappa, lead).
   - **Anonimizzazione / pseudonimizzazione** — si sostituiscono nome/email/telefono/note con
     placeholder (`"Cliente #<id>"`), si tengono id e importi. Per righe legate a contabilità/fiscale
     dove serve ancora il *numero* ma non la *persona*.
   - **Archiviazione** — riga spostata su storage freddo (S3/tabella `*_archive`) e rimossa dal DB
     operativo. Per dati ancora dovuti per legge ma inutili online.
4. **Decorrenza esplicita.** Ogni termine parte da un evento preciso, non "dalla creazione della riga":
   fine prenotazione, chiusura della stagione, cessazione del contratto dello stabilimento, ultima
   attività del cliente, data del documento fiscale. La colonna "Decorrenza" del §3 la specifica.
5. **La testata sopravvive alle righe.** Nel modello target (testata `booking` + righe `booking_lines`,
   [_feature-tabelle.md](../analisi-pre/db/_feature-tabelle.md)) la **testata** con il riepilogo
   economico segue la retention fiscale (lunga); le **righe di dettaglio pesanti** (griglie prezzo
   giorno-per-giorno, snapshot mappa, contatori inventario, snapshot cliente JSON) hanno retention
   operativa **breve** e si potano prima (§4).
6. **Soft-delete non è retention.** `deleted_at IS NOT NULL` ([03-gestione-dati.md §3.5](03-gestione-dati.md))
   significa "riga logicamente cancellata", non "riga da tenere per sempre": dopo la finestra di
   ripristino/audit (default **90 giorni**, o il termine legale se più lungo) la riga soft-deleted va
   rimossa fisicamente dal batch di retention.
7. **Un dato personale che non serve non si raccoglie.** Prima ancora della retention: i campi PII morti
   (`users.birthDate`, `users.gender`, colonne carta in chiaro di `online_reservations`) non vanno
   migrati nel nuovo schema — vedi [11-modulo-privacy.md §4](11-modulo-privacy.md).
8. **Idempotenza e reversibilità della finestra.** I batch di retention girano più volte senza danni
   (`WHERE ... AND archived = false`) e la soglia è un parametro di configurazione per stabilimento/
   ambiente, non un valore cablato.

---

## 1. Riferimento temporale: la "stagione"

I tempi operativi sono espressi in **stagioni (S)** oltre che in mesi, perché il ciclo di vita reale
del dato è stagionale:

- **Ai fini della retention, 1 S = l'anno di calendario, e il confine è il 1 gennaio** ✅
  *(deciso dal titolare il 2026-10-01)*. Non si usano i confini per-stabilimento di
  [`seasons_periods`](../analisi-pre/db/seasons_periods.md) / `catalog.pricing_seasons`: servono al
  prezzo e all'operatività, non a decidere quando cancellare. Una soglia sola per tutti.
- La soglia si **ricalcola a ogni esecuzione**, non si congela: `N` è l'anno in cui il job gira.
  Esempio: eseguito nell'ottobre 2026, `N−2` vuol dire *tutto ciò che precede il **1 gennaio 2024***.
- Il **batch annuale** può continuare a girare **all'apertura della stagione** (indicativamente
  **1 aprile**), quando i dati della stagione conclusa non servono più all'operatività quotidiana:
  **quando gira non cambia la soglia**, cambia solo quando la cancellazione avviene.
- "Stagione N−1" = l'anno precedente a quello corrente (spesso **utile**: rinnovi, "stesso ombrellone
  dell'anno scorso", confronto ricavi). "Stagione N−2" = due anni fa.

> **Attenzione a non confondere soglia e finestra.** In questo documento "N−1"/"N−2" indicano *quanto
> resta online*, non *cosa si archivia*: **N−2 = stagione corrente + le due precedenti online**, si
> archivia dalla terza in giù. Il titolare ha **esteso da N−1 a N−2** la finestra di quasi tutto il
> blocco prenotazioni (chiarimento Q1/Q2 del 2026-09-17,
> [82-retention-data.md](82-retention-data.md)): dove la proposta diceva N−1, ora vale N−2.

| Sigla | Significato | Uso tipico |
|---|---|---|
| **fine S** | chiusura della stagione a cui il dato appartiene | far partire la retention operativa |
| **N−1** | tenere online la stagione corrente **+** la precedente | prenotazioni ombrellone, assenze, sharing, abbonamenti |
| **N−2 / +1 S** | tenere online la stagione corrente **+ le due precedenti** (come *finestra*); come *soglia*: archiviare tutto ciò che ha più di due stagioni piene | scelta del titolare per testata e dettaglio prenotazione, assenze/rivendita, riserve morbide, raggruppamenti |
| **10 anni** | termine fiscale/contabile italiano (art. 2220 c.c.) | scontrini, movimenti, riepiloghi economici, dati del titolare |

---

## 2. Categorie normative — retention **minima** (cosa impone la legge)

| Categoria | Riferimento (da confermare con DPO/commercialista) | Termine minimo | Note |
|---|---|---|---|
| **Documenti fiscali e commerciali** — scontrino elettronico Fiskaly, corrispettivi, ricevute | Art. 2220 c.c.; art. 39 DPR 633/1972; normativa corrispettivi telematici | **10 anni** dalla data del documento | Non cancellabili né anonimizzabili su richiesta dell'interessato prima della scadenza (art. 17.3.b GDPR — obbligo legale) |
| **Scritture e movimenti contabili** — ledger incassi, transazioni di pagamento, rimborsi, spese POS | Art. 2220 c.c.; art. 22 DPR 600/1973 | **10 anni** dalla registrazione | Idem: eccezione legittima al diritto all'oblio |
| **Dati identificativi/di contatto del titolare dello stabilimento** legati a fatturazione e contratto | Obblighi contrattuali + fiscali | **10 anni** dalla cessazione del contratto | Se il titolare è persona fisica/ditta individuale è anche dato personale |
| **Accettazioni contrattuali** (chi, quando, IP, hash del PDF) | Onere della prova (art. 2946 c.c., prescrizione ordinaria) | **durata del contratto + 10 anni** | L'IP prova l'accettazione: conservarlo solo finché utile a fini probatori, poi troncare l'ultimo ottetto — [11-modulo-privacy.md §4](11-modulo-privacy.md) |
| **Dati di prenotazione** (contratto tra cliente e stabilimento) collegati a un documento fiscale | Prescrizione ordinaria + collegamento fiscale | **10 anni** per il riepilogo economico; il resto secondo necessità | Vedi §3 C1: testata lunga, dettaglio breve |
| **Log accessi degli amministratori di sistema** | Provv. Garante 27/11/2008 "Amministratori di sistema" | **≥ 6 mesi** | Immodificabili; vedi [15-logs.md](15-logs.md) — audit separato dai log diagnostici |
| **Log di autenticazione / sicurezza applicativa** (login, lock, reset, 2FA) | Legittimo interesse (art. 6.1.f) — sicurezza | **6–12 mesi** | OTP/`two_factor_otp`: cancellazione **immediata** dopo consumo/scadenza |
| **Registro delle azioni degli operatori** (pagina «Tutti i log», modulo Audit) | Legittimo interesse organizzativo dello stabilimento; art. 4 L. 300/1970 (informativa ai lavoratori) | **12 mesi** ✅ (2026-10-13, D-66) | Dato personale dei dipendenti: durata proporzionata, informativa obbligatoria (D-67) — [86-modulo-audit.md §7](86-modulo-audit.md) |
| **Dati di traffico telefonico / messaggistica** (centralino, WhatsApp) | Legittimo interesse; principio di minimizzazione | **6–24 mesi** poi pseudonimizzazione del payload | Payload di terzi (Telnyx/Meta) con numeri e testo — [11-modulo-privacy.md §4](11-modulo-privacy.md) |
| **Dati per finalità di marketing** (liste, campagne, automazioni, iscrizioni newsletter) | Linee guida Garante marketing; art. 7 GDPR (revoca) | **24 mesi** dall'ultimo contatto/consenso, **salvo revoca prima** | Tenere **a tempo indeterminato solo** la *prova* della revoca / lista di soppressione (email in opt-out) |
| **Dati di pagamento con carta** (PAN, CVV, scadenza) | PCI-DSS; art. 5.1.c e 32 GDPR | **non conservare** | Tokenizzazione lato client (Stripe.js). Le colonne in chiaro esistenti (`online_reservations`) si **eliminano**, non si migrano |
| **Dati di categoria particolare** (salute) finiti in campi note liberi | Art. 9 GDPR | minimizzare / non raccogliere | Rischio già osservato ("cliente allergico al lattosio"): il campo va limitato, non "sanato" con un consenso a posteriori |
| **Registro delle richieste di cancellazione/export** (prova di conformità) | Accountability (art. 5.2) | **retention lunga** (es. 10 anni) | È la prova di aver ottemperato: non si minimizza a zero |
| **Backup** | Coerenza con la retention del dato contenuto | vedi [40-backup.md](40-backup.md) | Un backup non è un archivio a vita: PITR 7–35 gg, export logici 30–90 gg, copie fiscali per anni su S3 |

---

## 3. Tabellone — categoria di dato → retention legale + operativa

> ### ⚠️ «Cessazione» non è un dato: è derivata dall'abbonamento
>
> Deciso il 2026-09-30: **il concetto di "cessato" non esiste** e non va introdotto. Uno stabilimento
> può soltanto essere non attivo perché non ha rinnovato l'abbonamento — non esiste un momento in cui
> qualcuno dichiara "questo stabilimento ha chiuso". La retention va però applicata comunque, quindi
> la data di decorrenza si **ricava**:
>
> ```sql
> -- data di riferimento della cascata del §5, per stabilimento
> SELECT property_id, max(coalesce(ends_at, suspended_at)) AS cessazione_presunta
>   FROM identity.account_subscriptions
>  WHERE deleted_at IS NULL
>  GROUP BY property_id
> HAVING max(coalesce(ends_at, suspended_at)) < now() - interval '24 months';
> ```
>
> **La tolleranza di 24 mesi è il parametro che conta**, e va esposta come tale
> (`RETENTION_PROPERTY_TERMINATION_GRACE_MONTHS`), non cablata. Senza tolleranza un abbonamento
> scaduto da tre settimane — cioè un cliente che pagherà in ritardo — farebbe partire la
> cancellazione dei suoi dati; con una troppo lunga la retention non parte mai. I 24 mesi coincidono
> con i 2 anni che questo stesso documento concede allo snapshot della configurazione «utile per un
> eventuale rientro del cliente»: **la stessa soglia governa "il cliente potrebbe tornare" e
> "possiamo cancellare"**, che è il modo di non avere due numeri che si contraddicono.
>
> ### ⚠️ Sui dati migrati questa regola non è ancora calcolabile (2026-10-02)
>
> La query qui sopra legge `identity.account_subscriptions`, che **dopo la migrazione è vuota** — e non
> per un difetto: nel sistema legacy gli abbonamenti erano gestiti **fuori dal portale** e attivati o
> disattivati a mano, quindi nel dump non esiste alcuna tabella di abbonamenti per stabilimento
> (`subscribes` sono le iscrizioni alla newsletter, `licenses` sono due righe di anagrafica
> tipo/durata, e `properties` non ha colonne di scadenza). Verificato il 2026-10-02; decisione del
> committente: **si attende che i dati vengano inseriti da portale**, perché la gestione degli
> abbonamenti è una funzione del nuovo gestionale.
>
> Conseguenze da non perdere di vista, perché questo documento descrive una regola che per ora **non
> gira**:
>
> 1. **la cascata del §5 non parte**, perché `max(coalesce(ends_at, suspended_at))` non ha righe su cui
>    calcolarsi. È il comportamento prudente — i dati restano — ma va saputo: nessuno stabilimento
>    migrato verrà mai considerato cessato fino al primo inserimento da portale;
> 2. **quando il portale avrà la gestione, l'anzianità riparte da zero.** La data che si inserirà sarà
>    quella di inserimento, non quella reale di fine abbonamento: uno stabilimento fermo da tre anni
>    risulterà attivo da oggi. Se la data reale serve — e serve, se si vuole che la retention operi
>    sullo storico — va **importata da dove è stata tenuta finora** (fatturazione, foglio di calcolo,
>    altro). È l'unica informazione di questa regola che il nuovo sistema non può ricostruire da sé;
> 3. fino a quel momento il job `DeprovisionClosedProperty` gira a vuoto. Non va disattivato — va
>    saputo che non fa niente, altrimenti il primo che guarda i log conclude che è rotto.
>
> Vedi la voce **Q10** di [`_colonne-vuote.md`](../analisi-pre/db-no-copied/_colonne-vuote.md) (Parte 3, §3.2) per la verifica sui dati.
>
> Contesto e verifica in
> [`analisi-pre/db-no-copied/_mancanti-da-sistemare.md`](../analisi-pre/db-no-copied/_mancanti-da-sistemare.md),
> voci A1 e B8.



Legenda **Azione a scadenza**: 🗑️ hard delete · 🔒 anonimizza/pseudonimizza (tieni id + importi) ·
📦 archivia su storage freddo e rimuovi dal DB operativo.

### 3.1 C1 — Prenotazioni (ombrellone / servizi / riserve)

| Categoria di dato | Tabelle principali | PII / sensibile | Retention minima (legale) | Retention operativa consigliata | Decorrenza | Azione a scadenza |
|---|---|---|---|---|---|---|
| **Testata prenotazione ombrellone** (intestatario, canale, stato, riepilogo economico, `booking_number`) | `bookings` → target `bookings`/`reservations` (testata), `booking_payments`, `parent_bookings` | nome/email/telefono intestatario e ospiti | 10 anni (riepilogo economico collegato al fiscale) | **online: N−2** ✅ (stagione corrente + le due precedenti) *(proposta: N−1)*; poi 📦 archivio con **PII pseudonimizzata** e soli importi/date | fine S della prenotazione | 📦 + 🔒 oltre N−2; 🗑️ archivio dopo 10 anni |
| **Righe/dettaglio postazione** (griglie prezzo giorno-per-giorno, snapshot mappa, `reducedPriceObj`, articoli per data, snapshot cliente JSON) | campi JSON di `bookings`; target `booking_lines` | indirizzo/nome duplicati negli snapshot | nessuno (dato ricostruibile / non fiscale) | **N−2** ✅ *(proposta: fine S + 1 S)* — il titolare allinea il dettaglio alla testata; poi 🗑️ del dettaglio tenendo la testata | fine S | 🗑️ |
| **Prenotazioni di soli servizi** (bar / normali / a ore) | `service_reservation`, `service_reservation_payments` | nome/contatto cliente | 10 anni per il riepilogo economico se fiscalizzato | **N−1** ✅ (fine S **+ la stagione precedente**) *(proposta: fine S)*: backup poi 🗑️ del dettaglio; resta la testata economica se c'è scontrino | fine S | 📦 testata / 🗑️ dettaglio |
| **Contatori di consumo inventario** (ombrelloni, lettini, articoli aggiuntivi, servizi/giorno, slot orari) | `booking_items_counts_details`, `additional_items_counts_details`, `services_booking_counts`, `hourly_service_booking_management` → target `resource_consumptions` | no | nessuno | **fine S + 1 S** (N−1) poi 🗑️, con **aggregato mensile obbligatorio** prima della potatura ✅ (scelta esplicita del titolare: *"Sì, aggregato mensile"*) | fine S | 🗑️ (dopo aggregazione) |
| **Assenze pianificate / rivendita del posto** | `booking_absence_details` (attiva), `booking_absences` (legacy) | no | nessuno | **N−2** ✅ *(proposta: N−1)* — serve per confronti di occupazione su più stagioni; poi 🗑️ | fine S | 🗑️ |
| **Sharing / crediti da condivisione postazione** | `sharing_details` | riferimento utente titolare | il **credito maturato** segue la contabilità del wallet (C3, 10 anni); il *dettaglio giornaliero* no | dettaglio: **N−1** poi 🗑️ ✅ (confermato); saldo credito: → wallet C3 (10 anni) | fine S | 🗑️ dettaglio / 📦 movimento credito |
| **Riserve morbide di postazione** (manutenzione, VIP, blocco checkout) | `location_reservations`, `hold_locations` | no | nessuno | **N−2** ✅ *(proposta: fine S)* — il titolare allinea anche i blocchi alla finestra delle prenotazioni; `hold_locations` resta con TTL di **minuti** in checkout (il purge giornaliero del §4 non cambia: libera la postazione, non cancella la riga storica) | fine intervallo / fine S | 🗑️ |
| **Raggruppamenti e visibilità condivisa** | `booking_groups`, `bookings_requested_details`, `booking_areas` | id utente che ha aperto il codice | nessuno | segue la testata → quindi **N−2** ✅, poi 🗑️ | fine S | 🗑️ |
| **Contatore numero prenotazione per stabilimento** | `booking_logs` → target `booking_number_sequences` | no | — | **permanente** finché lo stabilimento è attivo (è una sequenza, non uno storico) | — | conserva |
| **Report periodici alle strutture convenzionate** (arrivi/partenze/ricavi) 🔄 **rivisto il 2026-10-02** | `hotel_reports` | nomi ospiti nel PDF | se contiene dati usati per la contabilità dello stabilimento: allineare al fiscale | **24 mesi** ✅ *(confermato, e la funzione **archivia** — deciso il 2026-10-02)* poi 🗑️ del PDF e della riga; il dato grezzo resta in `booking.*`. ⚠️ **Presupposto**: la tabella 🆕 `catalog.agreement_reports`, che nel nuovo schema **non esiste ancora** ([scheda](../analisi-post/db/catalog/catalog_agreement_reports.md)); il PDF va in `notification.media_assets` con `kind = 'rendered_pdf'`. La decorrenza era sbagliata e non la regola: la data si legge dalla riga, non dal nome del file. Vedi [83 §7-G1](83-retention-file.md) | data del report | 🗑️ |

### 3.2 C3 — Pagamenti (gateway, incassi, wallet, POS)

| Categoria di dato | Tabelle principali | PII / sensibile | Retention minima (legale) | Retention operativa consigliata | Decorrenza | Azione a scadenza |
|---|---|---|---|---|---|---|
| **Transazioni online e movimenti contabili** | `online_transaction_details`, `transactions_payments`, `card_transactions` (wallet), `booking_payments`/`service_reservation_payments` (movimenti dentro i JSON) → target `payment_transactions`, `customer_wallet_transactions` | id cliente | **10 anni** (scrittura contabile) | **online: 24 mesi** (copre riconciliazione + finestra chargeback con ampio margine), poi 📦 su S3 per il resto dei 10 anni | data movimento | 📦 a 24 mesi, 🗑️ archivio a 10 anni |
| **Rimborsi** | `refund_transaction_details` → target `payment_refunds` | id cliente | 10 anni | come sopra | data rimborso | 📦 / 🗑️ |
| **Link di pagamento Stripe ("riepilogo pagamento")** | `bookings_send_payment_summaries` | email/telefono destinatario | il *fatto* "pagamento richiesto, importo, esito" → 10 anni (C3); il *canale d'invio* → C6 | **fatto**: segue la transazione; **payload di invio** (`method_flag`, `email_sent`, testo): **12 mesi** poi 🗑️ | data invio | 🗑️ payload / 📦 fatto |
| **Spese/addebiti da POS esterni** (TeamSystem/Cassa in Cloud, OfSale) | `teamsystem_expenses`, righe in `booking_number_informations` | no | 10 anni (concorrono al conto/fiscale) | **24 mesi** online poi 📦, **🗑️ solo a 10 anni** ✅ *(proposta: archivio cancellato a 5 anni)* — il titolare conferma l'obbligo fiscale pieno (chiarimento Q4) | data spesa | 📦 a 24 mesi, 🗑️ a 10 anni |
| **Mappatura utente → Stripe Customer / carte salvate** | `user_stripe_card_details` | riferimento a customer Stripe (token, **mai** PAN) | — | **24 mesi di inattività** ✅ — e **la chiusura dell'account non anticipa la cancellazione**: la mappatura permane fino alla scadenza *(proposta: 🗑️ immediato alla chiusura account)*; decorrenza allineata a **N−2**. **Mai** PAN/CVV in chiaro | ultima attività (**non** la chiusura account) | 🗑️ |
| **Configurazione gateway per stabilimento** (id account Stripe Connect) | `stripe_configrations` | — | — | vive quanto lo stabilimento; 🗑️ **alla cessazione** (con revoca del collegamento Connect) | fine ultimo abbonamento + 24 mesi | 🗑️ |

### 3.3 C4 / C4bis — Scontrino fiscale elettronico e stampa RT

| Categoria di dato | Tabelle principali | PII / sensibile | Retention minima (legale) | Retention operativa consigliata | Decorrenza | Azione a scadenza |
|---|---|---|---|---|---|---|
| **Scontrino elettronico emesso** (documento commerciale, righe fiscali, PDF su S3) | `fiskaly_receipt`, `fiskaly_non_fiscal_receipt`, `booking_number_informations` (righe fiscali), `fiskaly_receipt_sequence` | dati acquirente se richiesti | **10 anni** dalla data del documento | **10 anni**: online **36 mesi** ✅ *(proposta: ~24 mesi)*, poi 📦 (metadati in DB freddo, PDF su S3 Glacier) — **mai** 🗑️ prima dei 10 anni | data emissione | 📦 a 36 mesi, 🗑️ a 10 anni |
| **Account fiscale dello stabilimento** (id/stati entità Fiskaly, credenziali) | `fiskaly_account` | credenziali API + Fisconline (segreti) | i **dati del titolare** legati ai documenti → 10 anni | credenziali → secret manager, non in tabella; il record → vive quanto il contratto + 10 anni per i riferimenti fiscali, poi **📦 archivia** ✅ (azione scelta esplicitamente dal titolare) | fine ultimo abbonamento + 24 mesi | 🔒 (rimuovi segreti) → 📦 |
| **Log del dialogo con l'API SIGN IT** (transizioni, errori) | `fiskaly_receipt_log` | payload con importi/righe | nessuno (è diagnostica, non il documento) | **12 mesi** poi 🗑️ ✅ (il titolare fissa l'estremo inferiore del range 12–24) | data evento | 🗑️ |
| **Cash register / device Fiskaly** | `fiskaly_cash_register` | — | riferimento nei documenti → tenere il minimo per 10 anni | soft-delete (`deleted_at`) → **🗑️** dopo 10 anni dall'ultimo documento che lo referenzia ✅ (il titolare sceglie *cancella*, non *archivia*: il device non è un documento fiscale) | ultimo documento collegato | 🗑️ |
| **Stampa fiscale su hardware RT** (config stampanti, log stampe, seriali) | `printer_configs`, `booking_receipts`, `receipt_serial_numbers` | intestazioni/footer | il *documento* fiscale è il journal della RT (lato dispositivo), non queste tabelle | `booking_receipts` (log stampe): **12 mesi** poi 🗑️; `printer_configs`: vive col dispositivo; feature mantenuta come componente separato (decisione di business) | data stampa | 🗑️ |

### 3.4 C5 — Identità, anagrafiche, contratti

| Categoria di dato | Tabelle principali | PII / sensibile | Retention minima (legale) | Retention operativa consigliata | Decorrenza | Azione a scadenza |
|---|---|---|---|---|---|---|
| **Account applicativi** (gestori, staff) | `users`, `user_roles` | email, telefono, nome, foto, id social | — (salvo azioni collegate a fiscale) | **disabilita** alla revoca accesso; **🗑️ cancellazione a 12 mesi dalla revoca** ✅ *(proposta: 🔒 a 24 mesi)* — per l'audit fiscale/contabile basta il riferimento allo **stabilimento**, non serve preservare l'identità dell'**utente** (chiarimento Q5); rimuovi subito i campi morti (`birthDate`, `gender`) | revoca/disabilitazione (o fine ultimo abbonamento + 24 mesi) | 🗑️ a 12 mesi |
| **Anagrafica clienti finali** | `customers` | nome, email, telefono, indirizzo, data nascita, codice fiscale, P.IVA, `note` | i dati collegati a un documento fiscale → 10 anni (solo il minimo necessario) | 🔒 **dopo 48 mesi senza prenotazioni** ✅ *(proposta: 24–36 mesi)*; se esistono documenti fiscali entro i 10 anni, si tiene la versione **pseudonimizzata** (id + CF/importi minimi), non i contatti | ultima prenotazione | 🔒 a 48 mesi, 🗑️ completo a 10 anni |
| **Campo `note` libero** su `customers`/`bookings`/`service_reservation` | come sopra | possibili dati salute (art. 9) | — | troncare/limitare in migrazione; retention **N−1** ✅ — più corta della prenotazione che la contiene (N−2): la nota si azzera una stagione prima del resto del dettaglio | fine S | 🗑️ a N−1 |
| **Flag `isBlackList`** | `customers` | dato valutativo su persona | — | **revisione annuale a carico del gestore dello stabilimento** ✅ (non del sistema): il flag non scade da solo, ma il gestore va sollecitato una volta l'anno a confermarlo o rimuoverlo — serve un promemoria/pannello, non un job che cancella | inserimento / ultima revisione | revisione annuale (umana) → 🗑️ se non confermato |
| **Accettazioni contrattuali** | `contract_acceptation`, `contract_documents` | IP, hash PDF | durata contratto **+ 10 anni** (prova) | come da minima; troncare l'ultimo ottetto dell'IP dopo **24 mesi** | accettazione / fine ultimo abbonamento + 24 mesi | 🔒 IP a 24 mesi, 📦 poi 🗑️ a +10 anni |
| **Preferiti / "mi piace" consumer** | `property_like_logs` | id utente consumer | — | 🗑️ alla **chiusura account** o dopo **24 mesi** di inattività dell'utente ✅ (confermato) | ultima attività utente | 🗑️ |
| **Anagrafica pubblica stabilimento** | `bathouses`, `properties` | contatti titolare (se persona fisica) | 10 anni dopo cessazione per i riferimenti fiscali | la scheda **resta pubblicata** dopo la cessazione ✅, **ridotta alle sole informazioni essenziali** (nome, foto, posizione, descrizione base) e **con la prenotazione online disattivata** — serve a popolare il portale *(proposta: rimozione dalla ricerca pubblica)*; il resto segue la cascata del §5 | fine ultimo abbonamento + 24 mesi | 🔒 riduzione a scheda essenziale (poi §5) |
| **Registro richieste GDPR** (cancellazione/export) | (nuova tabella `erasure_requests`) | id interessato + esito | prova di conformità → **10 anni** | come da minima | data richiesta | conserva |

### 3.5 C2 — Catalogo & configurazione

| Categoria di dato | Tabelle principali | Retention minima (legale) | Retention operativa consigliata | Decorrenza | Azione a scadenza |
|---|---|---|---|---|---|
| **Configurazione viva** (mappa spiaggia, settori, stagioni, listini, sconti, servizi, magazzino, revenue, convenzioni, template hotel) | `sectors_rows`, `configurations`, `configure_maps`, `seasons_periods`, `price_lists`, `discounts`, `services_types`, `warehouses`, `revenue_notifications`, `hotels`, `hotel_price_lists`, `hotel_message_templates`, … | — | **nessuna retention a tempo**: è stato corrente, si tiene finché lo stabilimento è attivo. Alla **cessazione**: 📦 snapshot completo poi 🗑️ dopo **2 anni** (utile per un eventuale rientro del cliente), salvo listini/sconti che hanno alimentato prenotazioni fiscalizzate (già "congelati" negli snapshot di prenotazione) | fine ultimo abbonamento + 24 mesi | 📦 → 🗑️ dopo 2 anni |
| **Cataloghi master globali** (icone, categorie, filtri, immagini, nazioni, licenze) | `configure_images`, `configure_categories`, `servicetypes_icons`, `service_type_filters`, `country`, `licenses` | — | **permanenti, senza eccezioni** ✅ *(confermato dal titolare il 2026-10-01: «le icone sono del gestionale e non vanno mai cancellate»)*. ~~le voci usate da un solo stabilimento si 🗑️ alla sospensione/cessazione del suo account — da applicare per riga, non per tabella~~ → **regola superata**: verificato che nessuna di quelle tabelle ha una colonna di appartenenza (né `propertyId` né altro), quindi «voci di un solo stabilimento» non esistono nei dati. Vedi [83-retention-file.md §2.2](83-retention-file.md) | — | conserva, sempre |
| **Sfondi di mappa dello stabilimento** (`templateImg`, `umbrellaImg`, ombrelloni speciali) — ⚠️ stanno in `images/configure/` insieme alle icone ma **non sono icone** | `configure_maps` — l'**unica** tabella grafica con `propertyId` | — | **seguono lo stabilimento** ✅ *(deciso il 2026-10-01)*: 🗑️ alla cessazione, come i listini. Sono il peso vero di quel prefisso: 2,8 GB di `.jpeg`, non icone da pochi KB | fine ultimo abbonamento + 24 mesi | 🗑️ alla cessazione |
| **Storico versioni listino/stagione** (se introdotto) | — | concorre alla ricostruzione dei prezzi applicati → allineare a prenotazioni (10 anni per lo snapshot dentro la prenotazione) | tenere solo lo **snapshot dentro la prenotazione**; non serve uno storico separato del listino oltre **N−1** | fine S | 🗑️ |

### 3.6 C6 — Comunicazione e marketing

| Categoria di dato | Tabelle principali | PII | Retention minima (legale) | Retention operativa consigliata | Decorrenza | Azione a scadenza |
|---|---|---|---|---|---|---|
| **Log invii email** (outbox + esito) | `email_logs` | `emailTo`, `body` (spesso nome/cognome nel testo) | — | **150 giorni** per il corpo ✅ *(proposta: 90 gg)* — poi troncare `body`; **12 mesi** per i metadati d'invio (esito, timestamp) poi 🗑️/📦 (confermato) | data invio | 🗑️ body a 150 gg, 🗑️ riga a 12 mesi |
| **Notifiche in-app "per stabilimento"** (campanella gestionale) | `notification_details` | testo con nome cliente/prenotazione | — | **6 mesi** poi 🗑️ ✅ (il titolare fissa l'estremo inferiore del range 6–12): è un feed operativo, non un archivio | data notifica | 🗑️ |
| **Liste contatti e appartenenze** | `contact_list`, `contact_list_member`, `subscribes` | email/telefono cliente | — | membro rimosso/ lista non usata da **24 mesi** → 🗑️; purge continuo delle righe orfane (`contact_list_member` ha ~231k id per 7,6k righe); tenere **a vita solo** la lista di **soppressione** (opt-out) come prova | ultimo contatto / disiscrizione | 🗑️ (tranne opt-out) |
| **Campagne e automazioni** (definizioni + snapshot destinatari) | `marketing_email_campaign`, `marketing_automation`, `marketing_event`, `marketing_event_notification` | array email in `recipientList`/`recipients` | — | definizione: **24 mesi dall'ultimo uso** poi 🗑️ ✅ (il titolare precisa: clock sull'**ultimo uso**, non sulla creazione); **snapshot destinatari**: 🗑️ a **6 mesi** dall'invio (verificare comunque il consenso *al momento dell'invio*, non dello snapshot) | **ultimo uso** / data invio | 🗑️ |
| **Lead "richiedi la tua pagina"** | `csv_manager_details` | nome, email, telefono, messaggio | — | **12 mesi** poi 🗑️ (è un lead, non un cliente); candidato a CRM esterno / invio email senza persistenza | data invio form | 🗑️ |
| **Template email/WhatsApp** | `custom_templates`, `bathhouse_email_configrations` | credenziali SMTP (segreti) | — | vivono con lo stabilimento; credenziali → secret manager; 🗑️ alla cessazione | fine ultimo abbonamento + 24 mesi | 🗑️ |

### 3.7 C7 — Switchboard / centralino (oggi 0 righe)

| Categoria di dato | Tabelle principali | PII | Retention operativa consigliata | Decorrenza | Azione a scadenza |
|---|---|---|---|---|---|
| **Registro chiamate ricevute** | `switchboard_call_register` | numeri chiamante/chiamato | **6 mesi** poi 🗑️ ✅ *(proposta: 12 mesi)* — azione scelta: **cancella** | data chiamata | 🗑️ |
| **Log diagnostico flusso chiamata** (eventi Telnyx, payload) | `switchboard_call_log` | payload completo con numeri | **30 giorni** poi 🗑️ ✅ *(proposta: 30–90 gg)* — diagnostica; oggi già disabilitato di default | data evento | 🗑️ |
| **Log webhook WhatsApp** (stati consegna, messaggi in arrivo) | `switchboard_wa_msg_log` | numeri, `message_body`, `raw_payload` | **6 mesi** poi 🗑️ ✅ *(proposta: 6–12 mesi con 🔒 del payload)* — il titolare sceglie la cancellazione, non la sola pseudonimizzazione | data evento | 🗑️ |
| **Config centralino, IVR, audio, template** | `switchboard_config`, `switchboard_menu_item`, `switchboard_menu_action`, `switchboard_audio_file`, `switchboard_wa_template` | `sip_password`, documenti identità (`reg_*`) | vivono con lo stabilimento; segreti → secret manager; 🗑️ alla cessazione (+ cancellazione audio da S3) | fine ultimo abbonamento + 24 mesi | 🗑️ |

### 3.8 C8 — Chatbot (branch separato)

| Categoria di dato | Tabelle | PII | Retention operativa consigliata | Decorrenza | Azione a scadenza |
|---|---|---|---|---|---|
| **Storico conversazioni Q&A** | `chatbot_history` | `input_text`/`output_text` (può contenere dati liberi), `created_by` | **12 mesi** poi 🗑️ ✅ (confermato; in alternativa 🔒 del testo mantenendo metriche aggregate d'uso) | data conversazione | 🗑️ |
| **Config chatbot per stabilimento** | `chatbot_manager_config` | — | vive con lo stabilimento | fine ultimo abbonamento + 24 mesi | 🗑️ |

### 3.9 Log applicativi e diagnostica trasversale

| Categoria | Dove | Retention | Riferimento |
|---|---|---|---|
| Log diagnostici applicativi (P1–P5) | CloudWatch Logs | **90 giorni** in CloudWatch ✅ *(proposta: 30–90)*, **poi S3** con lifecycle IA/Glacier | [15-logs.md §2](15-logs.md) |
| Audit fiscale/contabile ("chi ha fatto cosa" su scontrini, movimenti) | **tabella DB dedicata** ✅ (scelta esplicita: *"Ok in tabella"*, non uno stream esterno) | **anni** (allineato al fiscale), separato dai log diagnostici | [15-logs.md §2](15-logs.md) |
| Log accessi amministratori di sistema | registro immutabile in DB, modulo **Audit** (`audit_trail_entries`, categoria `platform`) | **12 mesi** ✅ (2026-10-07; Provv. Garante AdS chiede ≥ 6) | §2, [86-modulo-audit.md](86-modulo-audit.md) |
| Log di autenticazione/sicurezza applicativa | **DB** ✅ (non solo log strutturato), modulo **Audit** (categoria `security`) | **6 mesi** ✅ *(proposta: 6–12)*; OTP 🗑️ subito dopo consumo/scadenza | §2 |
| Azioni degli operatori (pagina «Tutti i log») | DB, modulo **Audit** (categoria `operator`) | **12 mesi** ✅ (2026-10-13, D-66) | [86-modulo-audit.md](86-modulo-audit.md) |
| `booking_logs` diagnostici *(nome fuorviante — è un contatore, vedi §3.1)* | — | — | — |

### 3.10 INFRA — segreti e credenziali

| Categoria | Tabelle | Retention | Azione |
|---|---|---|---|
| Credenziali integrazioni partner (`apiKey`/`secret` in chiaro), SMTP, SIP, Stripe secret | `keysecret`, campi segreti in `fiskaly_account`, `bathhouse_email_configrations`, `switchboard_config`, `stripe_configrations` | **nessuno storico**: spostare in **AWS Secrets Manager / SSM** (versioning nativo), rotazione periodica; in DB non resta il segreto | migrazione a secret manager; 🗑️ dalle tabelle |

### 3.11 FILE — oggetti su storage (foto, PDF, allegati, audio, QR)

> ✅ **Politica validata dal titolare il 2026-10-01** — verbale in
> [`retention-file-con-risposte.docx`](retention-file-con-risposte.docx), analisi e numeri misurati
> in [`83-retention-file.md`](83-retention-file.md).
>
> **Il principio generale**: la cancellazione di una scheda porta con sé la cancellazione dei file
> che **solo quella scheda** usava, con le eccezioni elencate qui sotto. Un file usato da più
> schede si cancella quando l'ultima viene cancellata.
>
> ⚠️ **La cancellazione per policy agisce sulla copia su AWS.** La copia locale di sicurezza non
> viene toccata ✅ *(scelta esplicita del titolare)*: resta per i test. Conseguenza da conoscere:
> col tempo la copia locale diventa l'archivio di tutto ciò che la retention ha cancellato, quindi
> **non va usata per ripristinare il bucket**.

| Tipo di file | Dove | Quanti (misurati) | Retention | Decorrenza | Azione |
|---|---|---|---|---|---|
| **PDF scontrini elettronici** | `receipts/fiskaly/` | 2.546 | 📦 a **36 mesi** su Glacier, **mai** 🗑️ prima di **10 anni** | data del documento | 📦 → 🗑️ |
| **Foto della scheda pubblica** — le prime **3** per `sort_order` più la copertina (`main_photo_id`) ✅ | `images/bathouse/`, `images/property/` | ~10.100 | **permanenti**, anche dopo la cessazione: la scheda ridotta resta sul portale | — | conserva, sempre |
| **Altre foto dello stabilimento** (oltre le prime 3) ✅ | idem | 465 | seguono la **cessazione dello stabilimento**: 📦 con la configurazione, 🗑️ a **+2 anni** | fine ultimo abbonamento + 24 mesi | 📦 → 🗑️ |
| **Icone del catalogo** (mappa, servizi, tipi di servizio) ✅ | `images/configure/` (solo le 89 di `configure_images`), `images/services/`, `images/servicetypes/` | 320 | **mai cancellate, senza eccezioni**: «sono del gestionale», cioè **utilizzabili da tutti i gestori** 🔄 *(precisato dal titolare il 2026-10-02)*. ⚠️ Comprende anche le voci **non ancora in tavolozza** (i 10 file `hourly`/`normal` di `images/servicetypes/`: sono catalogo, non versioni morte) e quelle **nascoste con `status = false`**, che un flag può riportare in tavolozza — oggi `images/servicetypes/bar/pool.png`, ancora in uso da una riga. Vedi [83 §7-G2](83-retention-file.md) | — | conserva, sempre |
| **Sfondi di mappa dello stabilimento** ⚠️ non sono icone ✅ | `images/configure/` | 175 in uso | 🗑️ a **+2 anni** dalla cessazione, con la configurazione | fine ultimo abbonamento + 24 mesi | 🗑️ |
| **Allegati di email, campagne e automazioni** ✅ | `file/`, `files[]/`, `files/newsletter/` | 49 | 🗑️ **48 ore da quando nessuno li usa più** — non da quando l'email è stata spedita. Le email già inviate non contano come utilizzatori | ultimo riferimento rimosso | 🗑️ |
| **Listini e menù in PDF** ✅ | `pdf/`, `file/` | 6 | 🗑️ alla **cessazione** e 🗑️ **subito alla sostituzione** (il prezzo applicato è già congelato dentro la prenotazione) | cessazione / sostituzione | 🗑️ |
| **Audio del centralino** | `audio/switchboard/` | 5 | 🗑️ **alla cessazione**, con la configurazione del centralino | cessazione | 🗑️ |
| **PDF report alle strutture convenzionate** 🔄 **decorrenza corretta il 2026-10-02** | `notification.media_assets`, `kind = 'rendered_pdf'` | **0** oggi: i 28 su `reports/hotel/` sono dell'ambiente di **sviluppo** e non si migrano | 🗑️ a **24 mesi** ✅ *(il titolare ha confermato che la funzione si rifà **e archivia**)*. ⚠️ La decorrenza era sbagliata: ~~data ricavata dal nome `report_<hotelId>_<epoch_ms>.pdf`~~ — il numero nel nome è l'**`id` del report**, non l'`hotelId`. Si usa la data dell'asset, che coincide con la data del report. Presupposti: la tabella 🆕 `catalog.agreement_reports` ([scheda](../analisi-post/db/catalog/catalog_agreement_reports.md)) e l'archiviazione **all'invio**, non all'anteprima. Vedi [83 §7-G1](83-retention-file.md) | data del report (= `media_assets.created_at`) | 🗑️ |
| **QR delle prenotazioni** ✅ | `images/qrcodescan/` | **743.576** (1,2 GB) | seguono la **retention delle prenotazioni** (`N−2`). Sono rigenerabili dal `reservation_code`: nessuna perdita di informazione | `LastModified` dell'oggetto (i QR esistenti non sono collegabili a una prenotazione) | 🗑️ |
| **PDF dei report del bagnino** ✅ | — | **0** | **non esistono su S3**: erano percorsi locali del cronjob, allegati all'email e mai caricati. Nel nuovo gestionale: generare, inviare, cancellare — **non archiviare** | — | nessuna azione |
| **File orfani** (nessuna riga li nomina) ✅ | vari | ~12.000 (3,6 GB) | 🗑️. Sono **cancellazioni che il legacy ha tentato e non ha completato** (vedi 83 §7-C), non materiale che qualcuno ha scelto di conservare. 🔄 **Tre esclusioni obbligatorie** (2026-10-02, [83 §7-G2](83-retention-file.md)): ① mai sotto `images/services/` e `images/servicetypes/`, che sono cataloghi; ② mai un file nominato da una riga con `status = false`; ③ sotto `images/configure/` solo gli oggetti che nessuna riga nomina **e** che non sono nel catalogo `configure_images` — in pratica i caricamenti `uniqid()` sostituiti (2.216 su 2.448). Sono inoltre da **segnalare, non cancellare**, i 4 file che `configure_maps` nomina e che su S3 non esistono più | `LastModified` dell'oggetto | 🗑️ |

> ⚠️ **Ordine di esecuzione**: il recupero delle foto di `properties.photos` (10.097 file, oggi non
> migrate — vedi [`catalog_properties.md`](../analisi-post/db/catalog/catalog_properties.md)) va
> fatto **prima** della cancellazione degli orfani, altrimenti si cancellano file che il recupero
> avrebbe rimesso in gioco.

> 📌 **Come il problema non si ripresenta.** Per le foto rimosse da un gestore nel nuovo gestionale:
> la riga di `catalog.bathhouse_photos` prende `deleted_at` ✅ (soft-delete confermato dal titolare),
> `CompactSoftDeleted` la rimuove a **90 giorni**, e da quel momento il file non è più referenziato e
> lo raccoglie la stessa regola degli allegati. I 90 giorni diventano la finestra di recupero di una
> rimozione per errore.

---

## 4. Alleggerimento del DB **a parità di obblighi legali**

Cose che si possono potare presto **senza toccare nulla di ciò che la legge impone di tenere**, perché
l'obbligo fiscale è soddisfatto dallo **snapshot dentro la prenotazione / dallo scontrino**, non dal
dettaglio operativo:

| Intervento | Cosa si toglia | Quando | Beneficio |
|---|---|---|---|
| **Potatura degli snapshot JSON di prenotazione** | griglie prezzo giorno-per-giorno, `reducedPriceObj`, snapshot mappa, snapshot cliente duplicato, array articoli-per-data dentro `bookings` | fine S + 1 S | è il grosso del peso di `bookings` (decine di campi JSON ripetuti su ogni riga) |
| **Compattazione contatori inventario** | `booking_items_counts_details` & C. → un aggregato per (stabilimento × articolo × mese/stagione) | fine S + 1 S | tabelle ad altissimo numero di righe, lette solo per la stagione corrente |
| **Purge blocchi mappa scaduti** | `location_reservations`, `hold_locations` con `end_date < oggi` | giornaliera (di notte) | righe puramente operative, zero valore storico |
| **Compattazione soft-delete** | righe con `deleted_at` più vecchio di **90 giorni** (o del termine legale, se maggiore) | mensile | il soft-delete si accumula indefinitamente ([03-gestione-dati.md §3.5](03-gestione-dati.md)) |
| **Troncamento corpo email** | `email_logs.body` → `NULL` dopo **150 giorni** ✅ *(proposta: 90 gg)*; riga intera dopo 12 mesi | mensile | `email_logs` ha già ~61k righe di solo append |
| **Drop delle tabelle legacy / di archivio datato** (dopo export verificato) | `bookings_old` (dati 2021), `booking`, `booking_items_counts` (2021), `booking_items_counts_details_news`, `bookin-absences` (2022), `location_reservations_old`, `sharings`, `email_configrations`, `*_archive_03072026`, `beacharound_reservation` | una tantum, in migrazione | eliminano tabelle che non entrano nemmeno nel nuovo modello ([_feature-tabelle.md](../analisi-pre/db/_feature-tabelle.md)) |
| **Non migrare le colonne morte** | campi PII/JSON mai letti (`users.birthDate/gender`, colonne carta `online_reservations`, `revenue_notifications.discounts`, ecc.) | in migrazione | vedi task *"campi vuoti o nulli"* di [note.md](note.md) e `_Zcampi-deprecati.md` |
| **Snapshot destinatari marketing** | `recipientList`/`recipients` JSON | 6 mesi dall'invio | array di email congelati, nessun uso dopo l'invio |
| **Log diagnostici fuori dal DB** | `fiskaly_receipt_log`, `switchboard_*_log`, eventuale logging in tabella | ✅ 30 gg (`switchboard_call_log`) / 6 mesi (`switchboard_wa_msg_log`) / 12 mesi (`fiskaly_receipt_log`) | i log vanno su CloudWatch/S3, non nel DB operativo ([15-logs.md](15-logs.md)) |

---

## 5. Stabilimenti non più attivi — cascata di retention

Alla **cessazione del contratto** di uno stabilimento (`properties`/`bathouses`), il batch applica una
cascata scaglionata, perché i dati collegati hanno obblighi diversi:

> ### Che cos'è «la cessazione», a database — ✅ deciso il 2026-10-02
>
> **A database quello stato non esiste**: `catalog.property_status` è
> `pending | approved | rejected | imported`, cioè descrive l'**ingresso** in piattaforma, non
> l'uscita. E non lo significano nemmeno `deleted_at` o `is_online_sales_suspended`, che è un
> kill-switch temporaneo da abbonamento scaduto.
>
> **Il titolare ha deciso di derivarla**, non di aggiungere uno stato. Con questo si chiude anche
> [D-65 punto 1](../analisi-post/db/_domande-committente.md), che raccomandava l'opposto
> (`terminated` + `terminated_at` su `catalog.properties`). La definizione va scritta **una volta** e
> condivisa, non ricalcolata in ogni job: oggi tre documenti la nominano in tre modi.
>
> | Serve | Dove sta |
> |---|---|
> | distinguere **cessato** da **sospeso** | `identity.account_subscriptions.status` → `active \| suspended \| expired \| cancelled` |
> | perché il periodo si è chiuso | `identity.account_subscription_periods.close_reason` → `renewed \| expired \| cancelled \| superseded` |
> | **la data da cui parte la cascata** | `closed_at` (oppure `ends_at`) dell'ultimo periodo |
>
> `cancelled` è la cessazione **esplicita**; `expired` senza un rinnovo successivo è la cessazione
> **di fatto** (il cliente ha smesso di pagare); `suspended` non è né l'una né l'altra. È questo che
> risolve l'unica obiezione che rendeva la derivazione dubbia — «cessato resta indistinguibile da
> sospeso, col rischio di riattivare per sbaglio uno stabilimento chiuso al primo rinnovo».
>
> **Costo accettato**: `DeprovisionClosedProperty` interroga Identity invece di leggere una colonna
> locale al modulo Catalog. Si è preferita **una sola verità** a una copia che qualcuno deve
> ricordarsi di aggiornare, e uno stato che si può dimenticare di scrivere è peggio di un calcolo.
>
> ⚠️ **Due avvertenze misurate.**
>
> 1. **Vale solo in avanti.** Nel legacy la nozione di abbonamento non esiste: `subscribes` è la lista
>    newsletter (6.548 righe di sole email) e `licenses` ha 2 righe di *tipi* di licenza. Per gli
>    stabilimenti cessati prima del cutover non c'è nessuna data da cui partire.
> 2. **Di quell'obiezione resta un problema vero, ma di natura diversa**: l'**irreversibilità**. Se il
>    deprovisioning cancella i file e poi il cliente rinnova, non si torna indietro. Si risolve
>    facendo passare le cancellazioni dalla coda
>    [`privacy_file_deletions`](../analisi-post/db/privacy/privacy_file_deletions.md) con un periodo
>    di grazia, non con uno stato in più.
>
> A database questa decorrenza si dichiara con `reference_event = 'property_ceased'` su
> `privacy_retention_policies` (due policy sui file la usano, F6 e F7): la cascata non è esprimibile
> come «N mesi dopo una colonna data di questa riga», ed è il motivo per cui quella colonna esiste.

| Tempo dalla fine dell'ultimo abbonamento (+ 24 mesi) | Azione | Stato |
|---|---|---|
| **Subito** | disabilita gli account `users` dello stabilimento; revoca il collegamento Stripe Connect; sposta i segreti fuori dal DB; **riduci la scheda pubblica alle sole informazioni essenziali** (nome, foto, posizione, descrizione base) e **disattiva la prenotazione online** — la scheda **resta nella ricerca pubblica**; 🗑️ config centralino + audio S3 | ✅ *(proposta: rimozione dalla ricerca pubblica — **corretta** dal titolare: la scheda serve a popolare il portale)* |
| **+ 3 mesi** | 📦 snapshot completo della configurazione (mappa, listini, servizi, magazzino, sconti, template) **su S3** (non copia locale); 🗑️ le tabelle di configurazione dal DB operativo | ✅ archiviazione **S3** scelta esplicitamente |
| **+ 3 mesi** *(file)* | 🗑️ i **PDF di listino e menù** dello stabilimento; 📦 le **foto oltre le prime 3** e gli **sfondi di mappa** insieme allo snapshot di configurazione. Restano pubblicate: la copertina e le prime 3 foto per `sort_order` | ✅ *(deciso il 2026-10-01, vedi §3.11)* |
| **+ 12 mesi** | 🗑️ notifiche in-app, log email, liste marketing dello stabilimento; 🔒 le prenotazioni (pseudonimizza i clienti che non hanno documenti fiscali) | ✅ confermato |
| **+ 2 anni** | 🗑️ lo snapshot di configurazione archiviato (se il cliente non è rientrato), **e con esso le foto oltre le prime 3 e gli sfondi di mappa** | ✅ confermato · la parte sui file decisa il 2026-10-01 |
| **+ 10 anni** | 🗑️ definitivo di scontrini, movimenti, riepiloghi economici, dati del titolare (fino a qui **solo** su storage freddo, mai prima) | ✅ confermato |

> Prima di 10 anni si può sempre **archiviare** (📦, DB freddo / S3 Glacier) ma non **eliminare** i dati
> fiscali e contabili dello stabilimento cessato.

> **Conseguenza di disegno della prima riga** — "scheda ridotta ma pubblicata" non è una cancellazione
> parziale improvvisata: serve uno **stato esplicito** dello stabilimento (es. `is_online_booking_enabled
> = false` + una vista "scheda essenziale") invece di una `DELETE` sui campi non essenziali, altrimenti
> il portale mostrerebbe una scheda sfregiata a metà. Vedi
> [`catalog_properties`](../analisi-post/db/catalog/catalog_properties.md) /
> [`catalog_bathhouses`](../analisi-post/db/catalog/catalog_bathhouses.md) e la scheda pubblica
> [`catalog_search_bathhouses`](../analisi-post/db/catalog/catalog_search_bathhouses.md).

---

## 6. Job di retention (batch)

Sono job come gli altri ([20-jobs.md](20-jobs.md)): logica nell'Application del modulo proprietario,
trigger nell'host, `Europe/Rome`, **idempotenti**, con **export/backup verificato prima della
cancellazione**, log strutturato con conteggi (`archiviati=…, cancellati=…, durata=…`), e **dry-run**
attivabile (scrive solo cosa *farebbe*).

Tutti i job leggono la propria fetta di regole da
[`privacy_retention_policies`](../analisi-post/db/privacy/privacy_retention_policies.md) (filtro
`schema_name` = il proprio schema, `is_active`): **le durate qui sotto non sono cablate nel codice**, la
colonna "Finestra" documenta il valore seminato. Un job che non trova policy attive per il proprio schema
**non fa nulla** e lo logga.

> **✅ Le policy esistono già in database** (2026-09-21). Le prime **44 righe** sono seminate dalla
> migration `20260921074959_SeedRetentionPolicies` di `Module.Privacy`, **tutte in dry-run**: nessun
> job cancella alcunché finché un umano non toglie `is_dry_run_only` policy per policy dopo aver letto
> il report. Elenco riga per riga e motivazioni nel
> [§ Seed della scheda](../analisi-post/db/privacy/privacy_retention_policies.md).
>
> **Oggi sono 61.** Alle 44 iniziali si sono aggiunte: **15 policy sui file** il 2026-10-03
> ([83-retention-file.md §5](83-retention-file.md)), la retention del **verbale delle corse** a 24 mesi
> il 2026-10-05, e la retention degli **archivi** a 10 anni lo stesso giorno (sotto).
>
> **✅ E i job esistono**, dal 2026-10-03: non diciannove classi scritte a mano, ma **un motore
> generico** che legge questa tabella e la applica. Vedi [20-jobs.md](20-jobs.md). I nomi di job nella
> tabella qui sotto restano la mappa di come andranno raggruppati quando ci sarà uno scheduler: il
> motore li esegue già tutti, in un colpo solo.

| Job | Cadenza | Finestra (validata) | Cosa fa | Modulo |
|---|---|---|---|---|
| `PurgeExpiredMapHolds` | giornaliera (di notte) | TTL di minuti | 🗑️ `location_reservations`/`hold_locations` **scaduti in checkout** — libera la postazione; distinto dalla retention storica N−2 delle riserve morbide (§3.1) | Booking |
| `CompactSoftDeleted` | mensile | 90 gg | 🗑️ righe con `deleted_at` oltre 90 gg (o termine legale, se maggiore) su tutte le tabelle di dominio | trasversale (per modulo) |
| `TruncateEmailLogBodies` | mensile | **150 gg** / 12 mesi | `notification_deliveries.rendered_body → NULL` oltre **150 gg** ✅; 🗑️ riga oltre 12 mesi | Notification |
| `PurgeInAppNotifications` | mensile | **6 mesi** | 🗑️ `notification_inapp_messages` (+ `_reads`) oltre **6 mesi** ✅ | Notification |
| `PurgeDiagnosticLogs` | mensile | **12 mesi** / **30 gg** / **6 mesi** | 🗑️ `fiskaly_receipt_logs` oltre 12 mesi; `switchboard_voice_call_diagnostics` oltre 30 gg; `switchboard_messaging_wa_event_logs` oltre 6 mesi ✅ | Fiskaly / Switchboard |
| `PurgeCallRecords` | mensile | **6 mesi** | 🗑️ `switchboard_voice_call_records` oltre **6 mesi** ✅ *(azione: cancella, non pseudonimizza)* | Switchboard |
| `PurgeChatbotHistory` | mensile | 12 mesi | 🗑️ `chatbot_conversation_turns` oltre 12 mesi ✅ | Chatbot |
| `PurgeMarketingSnapshots` | mensile | 6 mesi / **24 mesi dall'ultimo uso** | 🗑️ snapshot destinatari oltre 6 mesi; 🗑️ campagne/automazioni oltre 24 mesi **dall'ultimo uso** ✅ (non dalla creazione); purge righe orfane `contact_list_member`; **mai** la lista di soppressione | Notification/Marketing |
| `PurgeExpiredLeads` | mensile | 12 mesi | 🗑️ `notification_establishment_claim_requests` oltre 12 mesi ✅ | Notification |
| `AnonymizeInactiveCustomers` | mensile | **48 mesi** | 🔒 `identity_customer_customers` senza prenotazioni da **48 mesi** ✅ *(proposta: 24–36)*, tenendo il minimo fiscale entro i 10 anni; 🗑️ `note` già a **N−1** | Identity/Customers |
| `PurgeRevokedStaffAccounts` | mensile | **12 mesi dalla revoca** | 🗑️ `identity_access_users` revocati/disabilitati da 12 mesi ✅ — l'audit fiscale tiene il riferimento allo **stabilimento**, non all'utente (chiarimento Q5) | Identity/Access |
| `PurgeStaleGatewayProfiles` | mensile | 24 mesi di **inattività** | 🗑️ `payment_customer_gateway_profiles` dopo 24 mesi senza attività — **la chiusura account non anticipa** ✅ | Payment |
| `SeasonalArchive` | **annuale, apertura stagione (~1 apr)** | **N−2** (dettaglio prenotazioni) · **N−1** (servizi, contatori, sharing, `note`) — **soglia al 1 gennaio dell'anno N−*k***, ricalcolata a ogni esecuzione ✅ | 📦 + 🗑️ dettaglio prenotazioni/assenze/riserve/raggruppamenti oltre **N−2** ✅; servizi, contatori e `note` oltre **N−1**; potatura snapshot JSON; **aggregato mensile obbligatorio** dei contatori prima della potatura ✅ | Booking |
| `ArchiveOnlineTransactions` | mensile | 24 mesi → **10 anni** | 📦 su S3 transazioni/rimborsi/**spese POS** oltre 24 mesi; 🗑️ dell'archivio **solo a 10 anni** ✅ *(la precedente ipotesi "5 anni" per le spese POS è stata corretta dal titolare)* | Payment |
| `ArchiveFiscalReceipts` | mensile | **36 mesi** → 10 anni | 📦 metadati scontrino + PDF oltre **36 mesi** ✅ *(proposta: 24)* su Glacier; **nessuna cancellazione prima di 10 anni** | Fiskaly |
| `PurgeCeasedPropertyPhotos` 🆕 | mensile | **24 mesi** dalla cessazione | 🗑️ accoda le foto degli stabilimenti cessati **oltre le prime tre per `sort_order`**, mai la copertina. ✅ Implementato il 2026-10-05 come **job con il perimetro nel codice** (`Module.Catalog/.../CeasedPropertyPhotoRetentionJob.cs`): è l'unico perimetro che una riga di policy non esprime. Legge comunque durata e dry-run dalla policy. ⚠️ **Trova zero righe sui dati migrati**: la cessazione si deriva dagli abbonamenti, che nel legacy erano gestiti fuori dal portale (Q10) | Catalog |
| `DeprovisionClosedProperty` | giornaliera | cascata §5 | applica la cascata del §5 in base a `mesi_da_cessazione`; **subito**: riduce la scheda pubblica a "essenziale" + disattiva la prenotazione online (**non** la rimuove dalla ricerca) ✅; **+3 mesi**: snapshot su **S3** ✅ | Catalog/Identity |
| `EnforceGdprErasure` | su evento | — | applica la regola per-modulo alle richieste art. 17 ([11-modulo-privacy.md §5](11-modulo-privacy.md)), rispettando i `privacy_legal_holds` | trasversale |
| `PurgeUnreferencedFiles` 🆕 | giornaliera | **48 ore** dall'ultimo riferimento rimosso | 🗑️ dalla **copia AWS** i file che nessuna riga usa più: allegati di campagne, foto con la riga `bathhouse_photos` già compattata, listini sostituiti. Non tocca scontrini, icone e foto della scheda pubblica. 🔄 Vanno escluse anche le **272 foto** recuperate da `properties.photos` di property senza vetrina: sono senza riga di aggancio per costruzione, non orfane ([83 §7-G3](83-retention-file.md)) | trasversale (Privacy) |
| `PurgeStorageByPrefix` 🆕 | mensile | `N−2` per i QR · immediata per gli orfani | 🗑️ gli oggetti **senza nessuna riga a database**, per prefisso e per `LastModified`: i 743.576 QR oltre `N−2` e i ~12.000 orfani. ⚠️ forma pericolosa: solo prefissi da elenco chiuso (vedi [83 §5.5 bis](83-retention-file.md)). 🔄 **Tre esclusioni obbligatorie** (2026-10-02): ① mai sotto `images/services/` e `images/servicetypes/`, che sono cataloghi anche per le voci non ancora in tavolozza; ② mai un file nominato da una riga con `status = false`; ③ sotto `images/configure/` solo ciò che nessuna riga nomina **e** che non è nel catalogo `configure_images`. Vedi [83 §7-G2](83-retention-file.md) | Privacy |
| `ReconcileStorageDeletions` 🆕 | giornaliera | — | ritenta le cancellazioni in coda che sono fallite e segnala quelle ferme da troppo tempo: cancellare su S3 non è transazionale col DB. Segna anche `purged_at` sulle righe di asset che hanno quella colonna | Privacy |
| `PurgeExpiredArchives` 🆕 | mensile | **10 anni** da `archived_at` | 🗑️ gli **oggetti di archivio** scaduti, dall'inventario [`privacy_archive_manifests`](../analisi-post/db/privacy/privacy_archive_manifests.md). ✅ Implementato il 2026-10-05: è una policy ordinaria `delete_object` sulla tabella di inventario, non un job a parte. **Chiude il buco più grosso dell'impianto**: archiviare non è cancellare, e fino a quel giorno nessuna policy toccava gli archivi. ⚠️ Il termine è il più lungo fra quelli che motivano un'archiviazione (10 anni fiscali) applicato a tutti: prudente, non preciso | Privacy |
| `PurgeHotelReportPdfs` 🆕 | mensile | **24 mesi** | 🗑️ i PDF dei report alle strutture convenzionate archiviati (`notification.media_assets`, `kind = 'rendered_pdf'`), **con la data presa dalla riga** 🔄 — non dal nome del file, che la conteneva in una forma letta male (vedi [83 §7-G1](83-retention-file.md)). ⚠️ Nasce **a vuoto**: non ha niente da cancellare finché la funzione dei report non è rifatta | Notification |

**Non è un job**: la **revisione annuale del flag `isBlackList`** ✅ è un compito *del gestore dello
stabilimento*, non del sistema — va esposta come promemoria/pannello nel gestionale, non implementata come
batch che cancella flag (§3.4).

> 🔄 **Deciso il 2026-10-02 (D-65 punto 3): sul flag si applica la logica del legacy**, cioè un flag che
> il gestore mette e toglie, **senza data di revisione**. Non si aggiungono `blacklist_reviewed_at` né
> `blacklist_reviewed_by_user_id`.
>
> **Conseguenza da conoscere**: la revisione annuale resta un compito umano **senza traccia a
> database**. Un promemoria potrà basarsi solo su `updated_at`, che cambia per qualsiasi modifica del
> cliente, quindi non distinguerà «ho riconfermato la blacklist» da «ho corretto il telefono» e
> ripeterà la domanda a chi ha appena risposto. ⚠️ E per i **dati legacy** non è documentata nemmeno
> la base giuridica: `blacklist_reason` è **interamente NULL su tutte le 224.524 righe**, comprese le 40
> con il flag attivo (il legacy aveva solo il flag; vedi [`_colonne-vuote.md`](../analisi-pre/db-no-copied/_colonne-vuote.md)). Si riempirà solo per
> le blacklist messe dal nuovo gestionale.

**Attenzione alle due decorrenze diverse in `SeasonalArchive`**: la testata e il grosso del dettaglio
scadono a N−2, ma `customers.note` e le prenotazioni di soli servizi a N−1. Un job che usasse una soglia
unica cancellerebbe troppo tardi le note (dato potenzialmente art. 9 GDPR) — due passate, non una.

---

## 7. Cheat sheet — tempi consigliati

Valori **validati**; ✅ marca le righe dove il titolare ha scostato dalla proposta iniziale
(riportata fra parentesi). In caso di divergenza fra questa cheat sheet e la riga specifica del §3,
**vince il §3** — la cheat sheet accorpa categorie che il §3 distingue.

| Dato | Online (DB caldo) | Poi | Cancellazione definitiva |
|---|---|---|---|
| Scontrini elettronici | ✅ **36 mesi** *(24)* | 📦 storage freddo | **10 anni** |
| Movimenti, rimborsi, riepiloghi economici | ~24 mesi | 📦 storage freddo | **10 anni** |
| Spese/addebiti POS esterni | 24 mesi | 📦 storage freddo | ✅ **10 anni** *(archivio a 5 anni)* |
| Dati del titolare stabilimento (fatturazione/contratto) | durata contratto | 📦 | **10 anni** dalla cessazione |
| Accettazioni contrattuali | durata contratto | 🔒 IP a 24 mesi | contratto **+ 10 anni** |
| Testata prenotazione ombrellone | ✅ **stagione corrente + N−2** *(N−1)* | 📦 + 🔒 PII | 10 anni (se fiscalizzata) |
| Dettaglio prenotazione (JSON pesanti, snapshot mappa/prezzi) | ✅ **stagione corrente + N−2** *(fine S + 1 S)* | — | **🗑️ oltre N−2** |
| Prenotazioni di soli servizi | ✅ **N−1** *(fino a fine stagione)* | 📦 testata economica | 🗑️ dettaglio oltre N−1 |
| Contatori di consumo inventario | stagione corrente + N−1 | ✅ **aggregato mensile obbligatorio** | 🗑️ dettaglio a fine S + 1 S |
| Assenze / rivendita / riserve morbide / raggruppamenti | ✅ **N−2** *(N−1 / fine S)* | — | 🗑️ |
| Sharing (dettaglio giornaliero) | N−1 | saldo → wallet C3 | 🗑️ dettaglio |
| Anagrafica cliente finale | ✅ **48 mesi** dall'ultima prenotazione *(24–36)* | 🔒 pseudonimizza | 🗑️ a 10 anni (se legata a fiscale) |
| Campo `note` su cliente/prenotazione | ✅ **N−1** (più corto della prenotazione) | — | 🗑️ |
| Account staff `users` | fino a revoca/disabilitazione | — | ✅ **🗑️ a +12 mesi dalla revoca** *(🔒 a +24 mesi)* |
| Mappatura utente → Stripe Customer | 24 mesi di inattività | — | ✅ 🗑️ — **la chiusura account non anticipa** *(🗑️ immediato)* |
| Log invii email — corpo | ✅ **150 giorni** *(90)* | troncamento | — |
| Log invii email — metadati | 12 mesi | 📦 opzionale | 🗑️ |
| Notifiche in-app | ✅ **6 mesi** *(6–12)* | — | 🗑️ |
| Dati marketing (liste, campagne) | ✅ **24 mesi dall'ultimo uso** | — | 🗑️ (tranne lista opt-out, **a vita**) |
| Snapshot destinatari campagna | 6 mesi | — | 🗑️ |
| Lead "richiedi la tua pagina" | 12 mesi | — | 🗑️ |
| Log diagnostici applicativi | ✅ **90 giorni** (CloudWatch) *(30–90)* | **S3** IA/Glacier | secondo policy |
| Log dialogo Fiskaly (`fiskaly_receipt_logs`) | ✅ **12 mesi** *(12–24)* | — | 🗑️ |
| Log webhook WhatsApp | ✅ **6 mesi** *(6–12, 🔒 payload)* | — | 🗑️ |
| Log diagnostico chiamata (`switchboard_call_log`) | ✅ **30 giorni** *(30–90)* | — | 🗑️ |
| Registro chiamate ricevute | ✅ **6 mesi** *(12, 🔒 numero)* | — | 🗑️ |
| Storico chatbot | 12 mesi | 🔒 testo | 🗑️ |
| Log accessi amministratori di sistema | **12 mesi** *(≥ 6 per legge)* | registro immutabile (modulo Audit) | secondo policy interna |
| Azioni degli operatori (modulo Audit) | **12 mesi** ✅ (2026-10-13, D-66) | — | 🗑️ |
| Log di autenticazione/sicurezza | ✅ **6 mesi in DB** *(6–12)* | — | 🗑️ |
| Audit fiscale/contabile | ✅ **in tabella DB** (non stream esterno) | — | allineato al fiscale |
| Dati carta (PAN/CVV) | **mai** | — | tokenizzare, eliminare le colonne |
| Configurazione stabilimento attivo | finché attivo | 📦 **su S3** alla cessazione | 🗑️ a +2 anni dalla cessazione |
| Scheda pubblica stabilimento cessato | ✅ **resta pubblicata, ridotta all'essenziale, senza prenotazione online** | — | segue il §5 |
| Cataloghi master globali (icone) | ✅ **permanenti, senza eccezioni** *(confermato 2026-10-01)*. 🔄 «del gestionale» = **utilizzabili da tutti i gestori**: comprende le voci non ancora in tavolozza e quelle con `status = false` *(precisato 2026-10-02)* | — | nessuna cancellazione |
| Sfondi di mappa dello stabilimento (`configure_maps`) | ✅ 🗑️ **alla cessazione**, come i listini *(deciso 2026-10-01)* | — | non sono icone: vedi [83-retention-file.md](83-retention-file.md) |
| Registro richieste GDPR | permanente (prova) | — | ~10 anni |
| Backup PITR / export logici | 7–35 gg / 30–90 gg | — | rotazione automatica |

---

## 8. Cosa **non** cancellare (o cancellare solo con cautela)

- **Documenti e scritture fiscali/contabili** prima dei 10 anni: nemmeno su richiesta di cancellazione
  dell'interessato (art. 17.3.b GDPR — obbligo legale). La richiesta risulta "accolta parzialmente",
  con motivazione e scadenza esplicite ([11-modulo-privacy.md §5](11-modulo-privacy.md)).
- **Sequenze e contatori** (`booking_logs`, `fiskaly_receipt_sequence`, seriali): non sono storici, sono
  stato corrente — si conservano finché lo stabilimento è attivo.
- **Lista di soppressione marketing** (email in opt-out): si tiene **a tempo indeterminato**, è la prova
  che quel contatto ha chiesto di non essere più contattato.
- **Registro delle richieste GDPR**: è la prova di conformità (accountability), non un dato da
  minimizzare.
- **Snapshot dentro la prenotazione** (prezzo applicato, pacchetto, articoli) finché la prenotazione è
  entro i 10 anni fiscali: è ciò che permette di ricostruire *cosa è stato venduto a che prezzo* senza
  dipendere dai listini correnti.
- **Righe legate da integrità referenziale a dati ancora dovuti**: si **anonimizza**, non si elimina
  (id + importi restano per la contabilità).
- Qualsiasi cancellazione massiva **senza export verificato** su storage separato.
