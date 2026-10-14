# Job schedulati (background jobs)

> Come trattare i job/cron nel nuovo backend **.NET 10** (monolite modulare → microservizi). Parte
> dall'inventario dei cron reali di `back-pre` e li riporta alle regole di
> [pre/linee-guida-implementative-dotnet.md](pre/linee-guida-implementative-dotnet.md) e
> [pre/architettura-evolutiva-microservizi.md](pre/architettura-evolutiva-microservizi.md).
> Complemento operativo alla voce "Jobs" di [note.md](note.md).
>
> Tutti i percorsi sono relativi alla root del repository. Dove non specificato, i file sono sotto `back-pre/`.

---

## 0. Principio guida

Un **job non è un bounded context**: è un *meccanismo* (la schedulazione è infrastruttura) che invoca
logica appartenente a un modulo. Da qui tutto il resto:

- La **logica** del job vive nell'**Application layer del modulo proprietario**, come normale
  application service — testabile, riusabile, richiamabile anche da un endpoint.
- Il **trigger** (quando parte, con che cron) è un adapter sottile che vive nell'**host**
  (regola §7 delle linee guida: *config/infrastruttura trasversale → host*).
- I job **non stanno in solution separate**, né una per job. Al più: un host API + un host Worker,
  entrambi nella stessa `SummerBooking.sln`.
- In ottica microservizi, un job **migra insieme al suo modulo**, non da solo.

---

## 1. Inventario dei job legacy (`back-pre`)

Wiring in [`server.js`](../../../../../back-pre/server.js) righe ~115-186. Fuso orario legacy: molti cron sono
schedulati su `Asia/Kolkata` con commento "orario italiano" — nel nuovo backend si usa `Europe/Rome`.

### 1.1 Job attivi

| Job legacy | File | Schedule | Cosa fa | Modulo target (nuovo) |
|---|---|---|---|---|
| `updateCancelBooking` | [`cronjob/cron-cancel-booking.js`](../../../../../back-pre/cronjob/cron-cancel-booking.js) | `*/5 * * * *` | Annulla prenotazioni non pagate oltre `end_date_time` (`payment_status='unpaid'`), poi disattiva il payment link Stripe | **Booking** (annullamento) + **Payment** (disattiva link) via evento |
| `updateBookingSummrey` | [`cronjob/cron-updatebooking-summary.js`](../../../../../back-pre/cronjob/cron-updatebooking-summary.js) | `* * * * *` | Prende il prossimo `bookings_send_payment_summaries` con `email_sent=0` e invia l'email di riepilogo pagamento | **Booking** emette evento → **Notification** invia |
| `updateBookingStatusCron` | [`cronjob/cron-job.js`](../../../../../back-pre/cronjob/cron-job.js) (`cronHelper.updateStatus`) | `*/6 * * * *` | Aggiorna lo stato delle prenotazioni in base alle date | **Booking** |
| `startSharingDetailsCron` | `cronjob/cron-job.js` (`cronHelper.getSharingDetails`) | `* * * * *` | Accredito crediti da "sharing"/referral | **Booking/Revenue** (crediti cliente) |
| `startLifeGuardCron` | `cronjob/cron-job.js` (`cronHelper.getLifegaurdEmail`) | `0 30 8 * * *` (IST) | Invio email bagnino (lifeguard) | **Notification** |
| `campaignEmail` | `cronjob/cron-job.js` (`cronHelper.campaignEmail`) | `*/1 * * * *` | Invio email di campagne marketing (`marketing_email_campaign`) | **Notification** (marketing) |
| `automationEmail` | `cronjob/cron-job.js` (`cronHelper.automationEmail`) | `*/5 * * * *` | Invio email di automation marketing (`marketing_automation`); guardia `isRunning` per non sovrapporre le run | **Notification** (marketing) |
| `updateRevenueNotificationCount` | `cronjob/cron-job.js` (`cronHelper.updateNotificationCount`) | `0 0 * * *` (Europe/Rome) | Ricalcolo contatori `revenue_notifications` | **Revenue/Reporting** |
| `updateDisebleProperty` | [`cronjob/cron-disableproperty.js`](../../../../../back-pre/cronjob/cron-disableproperty.js) | `0 0 0 * * *` | Disabilita gli abbonamenti scaduti (`endDate < now`, `subscriptionStatus='enable'`) e scrive su `propertylog.txt` | **Configuration/Identity** (subscription) |

### 1.2 Job disattivati / commentati (da valutare, non riportare a scatola chiusa)

| Job legacy | Nota |
|---|---|
| `stripeReconciliationCron` | Riconciliazione Stripe giornaliera — commentato in `server.js`. Utile: va riprogettato in **Payment** |
| `updateCurrentSeason` (`updateSeason`) | Aggiornamento date stagione — commentato. Nel nuovo modello la "stagione corrente" è una **query**, non uno stato da aggiornare via cron |
| `updateBookingNumberIn*` (payments, itemsCounts, hourlyService, reservationPayments, absenceDetails) | Backfill una-tantum di `bookingNumber` su tabelle collegate — **script di migrazione**, non job a regime. Non ricrearli |
| `news-letter.js` (`sentMailCronJob`) | Coda invio email da `logemail` — interamente commentato. La coda email è responsabilità di **Notification** (outbox), non un cron a parte |

### 1.3 Anti-pattern legacy da non riportare

- **Query SQL raw cross-tabella** dentro il cron (`LEFT JOIN bookings ...` in `cron-cancel-booking.js`):
  viola i confini di modulo. Il job deve chiamare le interfacce/eventi dei moduli.
- **Nessuna idempotenza / nessun lock**: schedule `* * * * *` che possono sovrapporsi; la guardia
  `isRunning` di `automationEmail` è in-process e salta con più istanze.
- **Log su file** (`propertylog.txt`, `cronjobpropertylog.txt`): niente file di log locali → logging
  strutturato (vedi voce log in [note.md](note.md)).
- **Effetti collaterali a soldi** (annulla booking, disattiva link Stripe) senza transazione né outbox.
- **Fuso orario** `Asia/Kolkata` con orari "italiani" scritti nei commenti.

### 1.4 Job nuovi introdotti dal nuovo schema (non hanno un cron legacy)

Il modello relazionale aggiunge stati/scadenze che il legacy non gestiva; servono job di manutenzione
corrispondenti.

| Job (nuovo) | Schedule indicativo | Cosa fa | Modulo |
|---|---|---|---|
| **`ReleaseExpiredHolds`** | `*/2 * * * *` (frequente) **+** passata di sicurezza nel job notturno | Libera i **blocchi temporanei di postazione** rimasti `active` oltre la scadenza: `booking_resource_consumptions` con `state = 'held' AND held_until < now()` → `state = 'released'` (e allinea `booking_spot_holds.hold_status` → `expired`). Sono gli hold creati "all'ingresso nel checkout/pagamento" ([`03-gestione-dati.md` §3.9](03-gestione-dati.md)) che restano appesi se il cliente abbandona il pagamento o la sessione cade. Idempotente (filtra su `held_until < now()`); nessun effetto a soldi (non annulla nulla — la prenotazione a quel punto non esiste ancora). Distinto da `updateCancelBooking`, che annulla prenotazioni **già create** e non pagate. | **Booking** (`Hold`) |
| `ExpireStaleCheckoutSessions` | idem, coordinato | (se il flusso lo richiede) chiude la sessione di pagamento C3 associata all'hold scaduto — via evento `HoldExpired` → Payment | **Booking** emette evento → **Payment** reagisce |
| `RebuildAvailabilityProjection` | `0 3 * * *` (notturno) | Ricostruisce/riconcilia `booking_resource_availability_daily` da `booking_resource_consumptions` e logga i drift trigger↔dettaglio ([`03-gestione-dati.md` §3.8](03-gestione-dati.md)). | **Booking** |
| `SettleResaleCredits` | `0 2 * * *` (notturno) | Ex `startSharingDetailsCron`, ridisegnato: accredito **incrementale** dei crediti "sharing" maturati (`booking_resale_offers` riprese, `credit_settled = false`) → riga in `booking_resale_credit_ledger` + evento `ResaleCreditEarned` verso Payment. | **Booking** (`Resale`) → **Payment** |

> **`RefreshCommonPasswords` (Identity, 2026-10-08).** Il primo del mese alle 3:00 (Europe/Rome) scarica
> l'elenco delle password diffuse (SecLists: elenco NCSC delle 100.000 più usate + elenco italiano), tiene
> quelle di 8–128 caratteri e, se ne trova almeno 10.000, lo salva in `Identity:CommonPasswords:FilePath` e
> lo mette in uso senza riavvio. Un download fallito lascia l'elenco in uso com'è. Gira come `BackgroundService`
> nel processo web finché non esiste l'host dei job; con più istanze ognuna aggiorna la propria copia.
> ⚠️ Su un container con disco effimero (Fargate) il file si perde al riavvio: fino all'aggiornamento
> successivo vale la copia incorporata nel modulo, che va rigenerata ogni tanto o il percorso va su un volume persistente.
>
> **Pulizia delle tabelle di sessione di Identity (2026-10-07).** Non servono job dedicati: sono due
> policy del motore di retention già esistente ([81-retention-motore.md](81-retention-motore.md)), da
> aggiungere al seed di `privacy_retention_policies` insieme alle API: `identity.access_refresh_tokens`
> cancellati **30 giorni** dopo `expires_at`, `identity.access_user_auth_challenges` cancellate **1 giorno**
> dopo `expires_at` (l'OTP non va conservato). Mancano anche l'avviso di pre-scadenza e quello di disattivazione
> dell'abbonamento (`da-chiarire.md` #27).
>
> **Da aggiungere con il modulo Audit (2026-10-07).** `audit_trail_entries` è partizionata per mese: le
> partizioni future vanno create in anticipo dallo stesso job di manutenzione delle partizioni di Booking
> ([06-partizioni-manutenzione.md](06-partizioni-manutenzione.md)). La retention per categoria passa dal
> motore di retention con tre policy nel seed di `privacy_retention_policies` — [86-modulo-audit.md §7](86-modulo-audit.md).

**Nota job notturno** — la passata notturna di manutenzione, oltre alla ricostruzione degli snapshot
di configurazione (rete di sicurezza, [`03-gestione-dati.md` §7.5](03-gestione-dati.md)) e alle
retention (§1.5), esegue anche una passata di **sblocco delle operazioni rimaste appese**: hold di
checkout `held` da troppo tempo, sessioni di pagamento orfane, proiezioni di disponibilità
disallineate. È la rete di sicurezza per i casi che il job frequente `ReleaseExpiredHolds` non ha
intercettato (host fermo, coda bloccata, bug).

### 1.5 Job di retention e archiviazione (nuovi, politica validata dal titolare)

Il legacy **non ha mai cancellato né archiviato nulla**: tutto resta a tempo indeterminato nel DB caldo.
L'intero blocco è quindi nuovo. La politica è **validata dal titolare** (2026-09-17): elaborazione
tecnica in [`80-retention.md` §6](80-retention.md), verbale delle scelte in
[`82-retention-data.md`](82-retention-data.md). Il resoconto del motore che li esegue — codice,
difetti incontrati, decisioni, test — è in [`81-retention-motore.md`](81-retention-motore.md).

**I tempi non si scrivono qui e non si cablano nel codice.** Ogni job legge la propria fetta di regole
dalla tabella [`privacy_retention_policies`](../analisi-post/db/privacy/privacy_retention_policies.md)
(filtro `schema_name` = il proprio schema, `is_active = true`) e applica l'azione `delete_row |
null_fields | anonymize | archive_cold` sulla **propria** tabella, col **proprio** `DbContext` — mai una
query cross-schema da un servizio centrale ([`11-modulo-privacy.md` §6](11-modulo-privacy.md)).
Cambiare una durata è un `UPDATE` su una riga di policy, non una release.

> **✅ La tabella è già popolata** (2026-09-21): 44 righe seminate dalla migration
> `20260921074959_SeedRetentionPolicies` di `Module.Privacy`, **tutte con `is_dry_run_only = true`**.
> Chi scriverà uno di questi job trova quindi le regole già pronte da leggere — e un primo giro che
> produce solo il report, per costruzione. Due righe (`switchboard.voice_call_records`,
> `switchboard.messaging_wa_event_logs`) hanno `basis_source = disputed` per il dubbio D.L. 7/2015 e non
> vanno attivate senza parere legale.
>
> Una durata espressa in **stagioni** (`duration_unit = seasons`, tutto il blocco C1) è applicabile
> **solo** dal job annuale `SeasonalArchive`: un job mensile che la leggesse non saprebbe tradurla in una
> data di taglio. Se ne trova una, deve saltarla e loggarlo, non improvvisare una conversione in mesi.

| Job | Schedule indicativo | Modulo | Nota |
|---|---|---|---|
| `PurgeExpiredMapHolds` | `0 3 * * *` | Booking | hold di checkout scaduti (TTL minuti). **Non** è la retention storica delle riserve morbide (N−2) |
| `SeasonalArchive` | `0 4 1 4 *` (**annuale**, apertura stagione ~1 apr) | Booking | il job più pesante: archivia/pota il dettaglio prenotazioni oltre **N−2**, servizi e contatori oltre **N−1**, previo **aggregato mensile** dei contatori. ⚠️ **La soglia è il 1 gennaio dell'anno N−*k*, non la data di esecuzione**: eseguito il 1 aprile 2027, `N−2` significa «tutto ciò che precede il 1 gennaio 2025». Vedi [80-retention.md §3](80-retention.md) |
| `ArchiveOnlineTransactions` | `0 4 2 * *` (mensile) | Payment | 📦 su S3 oltre 24 mesi; 🗑️ dell'archivio **solo a 10 anni** (spese POS incluse) |
| `PurgeStaleGatewayProfiles` | `0 4 2 * *` | Payment | profili gateway dopo 24 mesi di inattività (la chiusura account **non** anticipa) |
| `ArchiveFiscalReceipts` | `0 4 3 * *` | Fiskaly | 📦 oltre **36 mesi**; **nessuna** cancellazione prima di 10 anni |
| `PurgeDiagnosticLogs` | `0 4 3 * *` | Fiskaly / Switchboard | `fiskaly_receipt_logs` 12 mesi · `switchboard_voice_call_diagnostics` 30 gg · `switchboard_messaging_wa_event_logs` 6 mesi |
| `PurgeCallRecords` | `0 4 4 * *` | Switchboard | registro chiamate oltre 6 mesi |
| `TruncateEmailLogBodies` | `0 4 4 * *` | Notification | `rendered_body → NULL` oltre **150 gg**; riga oltre 12 mesi |
| `PurgeInAppNotifications` | `0 4 5 * *` | Notification | feed campanella oltre 6 mesi |
| `PurgeMarketingSnapshots` | `0 4 5 * *` | Notification | snapshot destinatari 6 mesi; campagne/automazioni 24 mesi **dall'ultimo uso**; **mai** la lista di soppressione |
| `PurgeExpiredLeads` | `0 4 5 * *` | Notification | lead "richiedi la tua pagina" oltre 12 mesi |
| `AnonymizeInactiveCustomers` | `0 4 6 * *` | Identity | clienti senza prenotazioni da **48 mesi** → pseudonimizzazione |
| `PurgeRevokedStaffAccounts` | `0 4 6 * *` | Identity | account staff revocati da **12 mesi** → cancellazione |
| `PurgeChatbotHistory` | `0 4 7 * *` | Chatbot | conversazioni oltre 12 mesi |
| `PurgeUnreferencedFiles` 🆕 | `0 5 * * *` (giornaliero) | Privacy | 🗑️ dalla **copia AWS** i file che nessuna riga usa più, **48 ore** dopo che l'ultimo riferimento è stato rimosso: allegati di campagne, foto la cui riga `bathhouse_photos` è stata compattata, listini sostituiti. **Non** tocca scontrini, icone e foto della scheda pubblica. 🔄 Vanno escluse anche le **272 foto** recuperate da `properties.photos` di property senza vetrina: sono senza riga di aggancio per costruzione, non orfane ([83 §7-G3](83-retention-file.md)) |
| `PurgeStorageByPrefix` 🆕 | `0 5 8 * *` (mensile) | Privacy | 🗑️ gli oggetti **senza nessuna riga a database**, per prefisso e `LastModified`: i 743.576 QR oltre `N−2`, i ~12.000 orfani. ⚠️ **forma pericolosa** — una policy su prefisso non ha nessuna riga che la trattenga: solo prefissi da elenco chiuso. 🔄 **Tre esclusioni obbligatorie** (2026-10-02): ① mai sotto `images/services/` e `images/servicetypes/`, che sono cataloghi anche per le voci non ancora in tavolozza; ② mai un file nominato da una riga con `status = false`; ③ sotto `images/configure/` solo ciò che nessuna riga nomina **e** che non è nel catalogo `configure_images`. Vedi [83 §7-G2](83-retention-file.md) |
| `ReconcileStorageDeletions` 🆕 | `0 6 * * *` (giornaliero) | Privacy | ritenta le cancellazioni in coda fallite; segnala quelle ferme da troppo tempo. Cancellare su S3 **non è transazionale** col DB: senza questo, il database dice una cosa e l'archivio un'altra |
| `PurgeHotelReportPdfs` 🆕 | `0 5 8 * *` (mensile) | Notification | 🗑️ i PDF dei report alle strutture convenzionate oltre **24 mesi**, archiviati in `notification.media_assets` con `kind = 'rendered_pdf'`. 🔄 **La data si legge dalla riga dell'asset**, non dal nome del file: nel legacy il numero nel nome è l'`id` del report e non l'`hotelId`, e quella decorrenza era sbagliata ([83 §7-G1](83-retention-file.md)). ⚠️ **Nasce a vuoto**: non ha niente da cancellare finché la funzione dei report non è rifatta — presuppone la tabella 🆕 `catalog.agreement_reports` ([scheda](../analisi-post/db/catalog/catalog_agreement_reports.md)) |

> 🆕 **I quattro job sui file** nascono dalla politica validata il 2026-10-01
> ([`80-retention.md` §3.11](80-retention.md), analisi in
> [`83-retention-file.md`](83-retention-file.md)). Due note che li distinguono dagli altri:
>
> - **sono gli unici job che cancellano oggetti su storage, non righe.** Richiedono due etichette
>   nuove sull'enum `privacy.retention_action` (`delete_object`, `archive_object`) e una coda di
>   cancellazione, perché una `DELETE` su S3 non partecipa alla transazione del database;
> - **`PurgeUnreferencedFiles` dipende da `CompactSoftDeleted`**: una foto rimossa da un gestore
>   resta referenziata da una riga con `deleted_at` finché `CompactSoftDeleted` non la rimuove a 90
>   giorni. Quei 90 giorni sono di fatto la finestra di recupero di una rimozione per errore, e non
>   vanno accorciati senza saperlo.

> ### ✅ IMPLEMENTATO il 2026-10-03 — un motore, non diciannove job
>
> I job di retention esistono nel codice. **Non** come diciannove classi scritte a mano: come **un
> motore generico** che legge le regole dalla tabella dichiarativa e le applica. È la forma che questa
> stessa sezione presuppone — «i tempi non si scrivono qui e non si cablano nel codice» — e l'unica che
> non richieda una release per cambiare una durata.
>
> | Pezzo | Dove | Cosa fa |
> |---|---|---|
> | `SeasonBoundary` | `BuildingBlocks.Infrastructure/Retention/` | **l'unica** funzione che traduce «N stagioni» in una data di taglio: 1 gennaio dell'anno N−k, ricalcolato a ogni esecuzione. 18 test unitari in `BuildingBlocks.Infrastructure.Tests`, perché è il pezzo che la documentazione ha sbagliato per settimane |
> | `RetentionEngine` | `BuildingBlocks.Infrastructure/Retention/` | legge le policy del proprio schema, risolve il nome fisico della tabella, calcola il taglio, applica l'azione. **Non si ferma al primo errore** |
> | `IArchiveStore` / `S3ArchiveStore` | `BuildingBlocks.Infrastructure/Retention/` | l'archivio delle policy `archive_cold`: JSON Lines compresso su storage a oggetti, un oggetto per lotto |
> | `IModuleRetentionJob` | `BuildingBlocks.Infrastructure/Retention/` | il job di **un** modulo. Ogni modulo lo registra sul proprio `DbContext` con una riga in `ModulePersistenceRegistration` |
> | `privacy_retention_runs` | `Module.Privacy` | il verbale di una corsa: righe trovate, righe toccate, durata, errori. Scade a **24 mesi**. [Scheda](../analisi-post/db/privacy/privacy_retention_runs.md) |
> | `privacy_archive_manifests` | `Module.Privacy` | l'**inventario** degli archivi: una riga per oggetto, con quante righe contiene. Vive **quanto l'oggetto** (dieci anni), ed è la fonte della policy che lo cancella. [Scheda](../analisi-post/db/privacy/privacy_archive_manifests.md) |
> | `StorageDeletionReconciler` | `BuildingBlocks.Infrastructure/Retention/` | svuota la coda `privacy_file_deletions`: rimuove gli oggetti e segna `purged_at` sulle righe di asset che hanno quella colonna. ⚠️ un'origine non configurata **non viene toccata** |
> | `TargetFilter` | `BuildingBlocks.Infrastructure/Retention/` | traduce il perimetro dichiarato (`target_filter`) in un frammento di `WHERE`. È l'unico pezzo che trasforma un dato di tabella in SQL: 36 test, metà dei quali verificano che un filtro **venga rifiutato** |
> | `PropertyCeasedPredicate` | `BuildingBlocks.Infrastructure/Retention/` | la cessazione di uno stabilimento, derivata in SQL da `identity.account_subscriptions`. ⚠️ Non basta `closed_at`: serve anche «nessun abbonamento attivo o sospeso» |
> | `RetentionRunLog` | `BuildingBlocks.Infrastructure/Retention/` | l'`INSERT` del verbale, condiviso fra il motore e i job con il perimetro nel codice: due copie divergerebbero alla prima colonna aggiunta |
> | `IBespokeRetentionJob` | `BuildingBlocks.Infrastructure/Retention/` | un job che applica **una** policy il cui perimetro non è dichiarabile. Oggi uno: le foto degli stabilimenti cessati |
> | `RetentionCommand` | `Boot` | esecuzione da riga di comando: `dotnet run --project Boot -- retention [--apply] [--schema NOME] [--no-seasonal]` |
> | `RetentionEnableCommand` | `Boot` | **accende** una policy (`is_dry_run_only`): `retention-enable [--list] [--all] [<public_id>] [--dry-run-again] [--yes]`. Verbo separato di proposito — non esegue niente, modifica configurazione |
>
> **Perché il motore sta in `BuildingBlocks.Infrastructure` e non in un modulo**: deve servirli tutti
> senza dipendere da nessuno (regola verificata da `BuildingBlocksArchitectureTests`). Fino al 2026-10-07
> stava in `Shared`; è stato spostato perché usa EF, Npgsql e AWS, che Shared non deve imporre ai moduli
> ([linee-guida-implementative-dotnet.md §7](pre/linee-guida-implementative-dotnet.md)). Per la stessa ragione non conosce nessuna entità: lavora su
> `DbConnection` e nomi di tabella, e le colonne enum le legge con `::text` perché gli enum nativi di
> `privacy` non sono mappati sulle connessioni degli altri moduli.
>
> ### ✅ Come si accende una policy (2026-10-05)
>
> ```bash
> dotnet run --project Boot -- retention-enable --list        # chi e' accesa e chi e' in prova
> dotnet run --project Boot -- retention-enable <public_id>   # accende una policy
> dotnet run --project Boot -- retention-enable --all         # tutte, con conferma da tastiera
> dotnet run --project Boot -- retention-enable --all --dry-run-again   # torna indietro
> ```
>
> ⚠️ In sviluppo serve `DOTNET_ENVIRONMENT=Development`, altrimenti la stringa di connessione non si
> risolve (`Host.CreateApplicationBuilder` legge quella variabile, non `ASPNETCORE_ENVIRONMENT`).
>
> **Perché un comando e non una migration.** Una policy nasce `is_dry_run_only = true` e non tocca
> niente finché una persona non la accende dopo aver letto il report: è la protezione che regge tutto
> l'impianto. Metterla a `false` in una migration la farebbe nascere **accesa** in ogni ambiente,
> produzione compresa.
>
> **Perché un comando e non un `UPDATE` a mano.** Perché un `UPDATE` a mano non lascia traccia, non si
> rivede e si perde al primo ricaricamento del database — **è già accaduto il 2026-10-05**: un drop e
> rimigrazione ha riportato tutte le 61 policy in prova, e le corse successive non archiviavano più
> niente senza che niente lo dicesse.
>
> `--all` chiede di scrivere `confermo` da tastiera; `--yes` salta la domanda per gli usi non
> interattivi.
>
> **Che cosa resta da fare**: la **pianificazione**. I cron di questa tabella non sono cablati da
> nessuna parte — non c'è ancora un host dei job (nessun `BackgroundService`, nessuno scheduler) e
> `Boot` è un'applicazione web. Oggi la retention si esegue a mano dalla riga di comando, che è quello
> che serve per leggere il report e decidere. I nomi dei job di questa tabella restano la mappa di come
> andranno raggruppati quando lo scheduler ci sarà: il motore li esegue già tutti, ma in un colpo solo.
>
> #### Il primo giro, misurato il 2026-10-03
>
> Prova a vuoto su un database con 6,1 milioni di righe (la corsa 9 dell'ETL): **59 policy valutate,
> 0 fallite, 44 eseguibili e 15 saltate** con ragione dichiarata, **427.072 righe** intercettate. Le
> più grosse: `notification.inapp_messages` 159.676, `notification.deliveries` 87.366 + 83.796,
> `booking.reservations` 41.944 (azzeramento note), `identity.customer_customers` 17.257.
>
> ⚠️ **Un rischio strutturale misurato e oggi non attivo.** Quattro tabelle bersaglio hanno chiavi
> esterne **senza** `CASCADE` che potrebbero trattenere una cancellazione:
> `fiskaly.cash_registers` (da `receipts` e `receipt_sequences`), `identity.access_users` (da
> `account_contract_acceptances`), `payment.transaction_charges` (da `request_links` e
> `transaction_refunds`), `payment.wallet_transactions` (auto-riferimento). Verificato riga per riga:
> **nessuna delle righe in scadenza è davvero trattenuta** su questi dati, quindi oggi passerebbero.
> Ma `payment.request_links` ha 1.390 righe e 4.308 addebiti sono in scadenza: il giorno in cui un
> link punta a un addebito scaduto, quella policy fallisce. Il motore la segna `failed` e continua —
> non si perde la corsa — ma va risolto decidendo l'ordine di cancellazione fra genitore e figlie.

> #### Le azioni su storage: si accodano, non si eseguono
>
> Il motore gestisce anche le policy `delete_object`/`archive_object` **riferite a una tabella** — non
> cancella niente su S3: **accoda** in
> [`privacy_file_deletions`](../analisi-post/db/privacy/privacy_file_deletions.md). La separazione è il
> punto: una `DELETE` su S3 non partecipa alla transazione del database, ed è l'errore che il legacy ha
> fatto lasciando ~12.000 file orfani sul bucket.
>
> **`only_if_unreferenced` si risolve leggendo `pg_constraint`**, non da un elenco cablato di chiavi
> esterne: il motore genera una `NOT EXISTS` per ogni FK che punta alla tabella di asset. Il progetto ne
> ha diciannove e cambiano con lo schema; un elenco scritto a mano si disallineerebbe alla prima tabella
> nuova, e si disallineerebbe **in silenzio** — una FK dimenticata non dà errore, fa cancellare un file
> ancora in uso. Il predicato esclude inoltre gli asset con `property_id` valorizzato: hanno un
> proprietario dichiarato e non sono orfani (sono le 272 foto di stabilimenti senza vetrina).
>
> Verificato su dati sintetici: di quattro asset scaduti vengono accodati solo quelli davvero orfani —
> non il referenziato, non quello con `property_id`, non quello troppo recente — e un secondo
> accodamento non duplica nulla (`ON CONFLICT` sull'indice unico parziale della coda).
>
> ⚠️ **Che cosa resta fuori, e perché.**
>
> | Policy | Perché non si accoda |
> |---|---|
> | le **8 su prefisso** (QR, orfani) | guardano il bucket, non il database: serve un job che lo elenchi. È la «forma pericolosa» — cancellare per prefisso senza una riga che trattenga i file — e non va improvvisata |
> | le **azioni su storage senza perimetro dichiarato** | ~~il perimetro non è esprimibile nella policy~~ 🔄 **risolto in parte il 2026-10-05**: vedi il riquadro sotto |
> | le **2 con `property_ceased`** (F6, F7) | la decorrenza va derivata da `identity.account_subscriptions`, fuori dallo schema del modulo |
>
> #### ✅ Il perimetro si dichiara (2026-10-05) — colonna `target_filter`
>
> Il problema era che una riga di policy diceva *quando* un file scade ma non *quali* file riguarda:
> `notification.media_assets` ha sei `kind` e la regola riguarda **solo** i PDF dei report, ma la
> policy, letta alla lettera, diceva «cancella tutti i file di notification oltre 24 mesi». Il motore
> la saltava — giustamente.
>
> Ora c'è una colonna `target_filter` (`jsonb`) sulla policy. Due forme:
>
> ```jsonc
> {"scope": "whole_table"}                                 // tutte le righe, DICHIARATO
> {"column": "kind", "op": "=", "value": "rendered_pdf"}   // un filtro
> ```
>
> Il cancello del motore è diventato: **un'azione su storage si esegue se la policy dichiara
> `only_if_unreferenced` oppure un `target_filter`.** `{"scope": "whole_table"}` serve proprio a
> questo: trasforma «nessuno ha detto quali» in «qualcuno ha detto tutte».
>
> | Policy | Esito |
> |---|---|
> | `notification` · PDF dei report a 24 mesi | ✅ **gira** |
> | `fiskaly` · scontrini a 10 anni | ✅ gira |
> | `fiskaly` · scontrini a 36 mesi (`archive_object`) | ⚠️ salta: l'azione non è implementata, vedi sotto |
> | `switchboard` · audio del centralino | ⚠️ salta: decorrenza da `property_ceased` |
> | `catalog` · foto di uno stabilimento cessato | ⚠️ salta, e **deve** saltare: vedi sotto |
>
> **Il caso `catalog` non si dichiara e non si dichiarerà.** La regola è «le prime 3 foto per
> `sort_order` più la copertina restano per sempre». Non è un confronto su una colonna: è «escludi le
> prime N secondo un **ordinamento**» più «escludi quella puntata da `main_photo_id` su **un'altra
> tabella**». `target_filter` non la esprime, e un perimetro approssimato qui cancellerebbe la scheda
> pubblica che il titolare ha chiesto di conservare. **Serve un job scritto a mano**, ed è l'unico
> caso rimasto.
>
> ⚠️ **I valori di un filtro sono limitati a token** (`^[A-Za-z0-9_.:-]{1,64}$`). Il motivo è in
> `BuildingBlocks.Infrastructure/Retention/TargetFilter.cs`: il predicato si compone in **sette punti** del motore, e
> passarlo come parametro avrebbe richiesto di propagarlo in tutti e sette — dimenticandosene in uno,
> quel punto avrebbe eseguito la policy **senza** perimetro, in silenzio. Con i valori ridotti a
> token l'iniezione è impossibile per costruzione e il perimetro non può sparire per distrazione.
>
> #### ✅ `archive_object`: implementata il 2026-10-05, come azione sulla coda
>
> Due policy su `fiskaly.media_assets` chiedono di **raffreddare** i PDF degli scontrini (classe di
> storage più economica) a 36 mesi, non di cancellarli. La coda sapeva fare una cosa sola —
> cancellare — e non c'era nessuna colonna che distinguesse «rimuovi» da «cambia classe»: eseguirle
> come le altre avrebbe accodato **alla cancellazione** PDF fiscali che devono vivere dieci anni. Il
> motore le saltava per questo.
>
> Ora `privacy_file_deletions` ha una colonna **`action`** (`delete_object` | `archive_object`,
> migration `20261005113821_AddFileDeletionAction`) e il riconciliatore ha due rami:
>
> | Azione | Che cosa fa |
> |---|---|
> | `delete_object` | `DeleteObjects`, e valorizza `purged_at` dove quella colonna esiste |
> | `archive_object` | `CopyObject` **dell'oggetto su se stesso** con la `StorageClass` nuova e `MetadataDirective = COPY`. ⚠️ Su S3 la classe non si modifica: si ricopia. Senza `COPY` il content type si perde e un PDF torna indietro come `application/octet-stream` |
>
> **Le due azioni si eseguono separatamente anche dentro lo stesso lotto**, e il raggruppamento è per
> azione e non per chiave: non esiste nessun percorso in cui una riga `archive_object` finisca in una
> `DeleteObjectsRequest`. Una richiesta di cancellazione di massa sbagliata non si annulla.
>
> La **classe** sta nella configurazione (`Retention:Deletion:Targets[].ArchiveStorageClass`, default
> `GLACIER`) e non sulla policy: «quale classe» è una decisione di costo che dipende dall'ambiente,
> mentre la policy decide *che cosa* succede al dato. ⚠️ `GLACIER` ha un minimo di **90 giorni** di
> fatturazione; per gli scontrini (36 mesi → 10 anni) non è un problema.
>
> #### ✅ Il file segue le righe (2026-10-06) — colonna `linked_asset`
>
> Due policy su `fiskaly.media_assets` **fallivano a ogni corsa**: dichiaravano `signed_at` come
> decorrenza, ma quella colonna sta su `fiskaly.receipts`. Il difetto era **mascherato** — venivano
> saltate dal cancello sul perimetro *prima* del controllo sulle colonne — ed è emerso proprio
> dichiarando il perimetro.
>
> Il problema vero però non era la colonna. `fiskaly.receipts` si archivia a 36 mesi: a quel punto la
> riga lascia il database caldo, e il PDF resta sullo storage **senza nessuna riga calda che lo nomini**
> e con la sua data di firma uscita dal database. Non resta niente che possa decidere quando
> cancellarlo.
>
> ⚠️⚠️ **E non si risolve con «cancella i file che nessuno nomina più»**: dopo l'archiviazione il PDF
> di uno scontrino **è** per definizione non nominato da nessuno, quindi quella regola lo
> cancellerebbe al **mese 36** invece che all'**anno 10**. È il motivo per cui `fiskaly.media_assets`
> non ha, e non deve avere, una policy `only_if_unreferenced`.
>
> La policy dichiara ora il file che la segue:
>
> ```jsonc
> {"column": "pdf_asset_id", "table": "media_assets", "action": "archive_object"}
> ```
>
> Il motore, dopo aver scritto l'archivio e **prima** di cancellare le righe, accoda l'azione sul file
> **e** lo inventaria in `privacy_archive_manifests` con `object_kind = 'linked_asset'`. La policy
> decennale su quell'inventario cancella archivio e PDF **insieme**: una sola regola per i due pezzi
> della coppia, che è il punto — scadere in momenti diversi era il difetto.
>
> Le due policy su `fiskaly.media_assets` sono **ritirate**, non cancellate: la riga resta come traccia
> di una regola che è esistita, con il motivo nella base giuridica.
>
> #### ✅ `property_ceased` si deriva (2026-10-05) — e sui dati migrati trova zero
>
> Le due policy con decorrenza dall'evento «cessazione dello stabilimento» venivano saltate perché
> l'evento va derivato da un altro schema. Ora `BuildingBlocks.Infrastructure/Retention/PropertyCeasedPredicate.cs` lo
> costruisce in SQL, in **due metà che servono entrambe**:
>
> 1. **ha chiuso** — esiste un abbonamento `cancelled` o `expired`, e `max(closed_at)` dei suoi periodi
>    è anteriore al taglio;
> 2. **e non è ripartito** — *nessun* abbonamento è `active` o `suspended`.
>
> ⚠️ **La seconda metà è quella che conta.** Senza, uno stabilimento che ha disdetto il piano vecchio e
> ne ha sottoscritto uno nuovo risulterebbe cessato, e la retention gli cancellerebbe le foto mentre
> sta lavorando. Un abbonamento **sospeso** non è una cessazione: era l'obiezione che aveva bloccato
> D-65.
>
> ⚠️⚠️ **Sui dati migrati non trova niente, e non è un difetto.** `identity.account_subscriptions` è
> **vuota**: nel legacy gli abbonamenti erano gestiti fuori dal portale e attivati a mano, quindi non
> c'è niente da migrare (Q10, 2026-10-02). Le due policy troveranno zero righe finché gli abbonamenti
> non saranno nel nuovo sistema. Il meccanismo è verificato con **dati sintetici** in
> `Module.Catalog.Tests` — altrimenti sarebbe codice mai eseguito pronto a sbagliare cancellando foto.
>
> #### ✅ I job con il perimetro nel codice (2026-10-05)
>
> Resta **un** caso che nessuna colonna di configurazione esprime: «alla cessazione, cancella le foto
> **oltre le prime tre per `sort_order`**, e **mai** la copertina». È «escludi le prime N secondo un
> ordinamento» più «escludi quella puntata da `bathhouses.main_photo_id`, su un'altra tabella».
>
> Per questo c'è `IBespokeRetentionJob`: un job che applica **una** policy il cui perimetro sta nel
> codice. Oggi uno solo, `Module.Catalog/.../CeasedPropertyPhotoRetentionJob.cs`, eseguito da
> `Boot -- retention` come **secondo passo**, fra il motore e la riconciliazione della coda.
>
> | Regola | Perché |
> |---|---|
> | il job legge la **sua policy** per `public_id` e ne prende durata, unità e `is_dry_run_only` | nel codice sta solo il perimetro. Cablare anche i 24 mesi significherebbe tornare a fare una release per cambiare un numero |
> | scrive nel verbale con `RetentionRunLog`, lo stesso del motore | il report di una corsa è uno |
> | il motore salta la sua policy **senza scrivere nel verbale** (`PoliciesHandledElsewhere`) | altrimenti una corsa produrrebbe **due righe per la stessa policy**, e la più visibile sarebbe quella sbagliata |
>
> #### ✅ `ReconcileStorageDeletions` — scritto, con due protezioni deliberate
>
> `BuildingBlocks.Infrastructure/Retention/StorageDeletionReconciler.cs` prende le righe con `executed_at IS NULL`, rimuove gli
> oggetti e segna l'esito. È registrato **solo** da `Module.Privacy`: la coda è una, non una per modulo.
> Gira come terzo passo di `Boot -- retention`.
>
> | Protezione | Cosa impedisce |
> |---|---|
> | un `storage_origin` **senza destinazione configurata non viene toccato** | che un ambiente di sviluppo cancelli dal bucket di produzione perché nessuno ha letto la configurazione. Il job riporta «*n* oggetti non toccati» invece di indovinare un bucket |
> | in **prova** non parte nessuna richiesta di cancellazione | che una verifica diventi un'esecuzione |
>
> Le destinazioni stanno in `Retention:Deletion:Targets`, una per origine. In sviluppo l'unica dichiarata
> è LocalStack. Il tentativo si conta sulla riga e oltre `MaxAttempts` (5) la riga smette di ritentarsi
> da sola: il report la segnala come ferma, perché a quel punto serve una persona.
>
> `DeleteObjects` è idempotente — una chiave già assente non è un errore — ed è il caso normale quando
> una corsa precedente è stata interrotta fra la rimozione e l'aggiornamento della coda.
>
> **Provato contro storage e database veri** (`BuildingBlocks.Infrastructure.Tests`, 3 test): rimuove e chiude la riga; non
> tocca un'origine non configurata e la riporta; su errore conta il tentativo e scrive `last_error`
> lasciando `executed_at` vuoto.

> #### ✅ L'esito, misurato il 2026-10-03 su un database ricostruito da zero
>
> Ciclo completo come richiesto: **198 tabelle rimosse**, 11 moduli ri-migrati, ETL da zero
> (**6.163.412 righe, 0 errori**), poi le 59 policy accese e applicate.
>
> | Corsa | Policy | ok | saltate | fallite | Righe toccate |
> |---|---:|---:|---:|---:|---:|
> | 1ª | 59 | 45 | 13 | **1** | 426.442 |
> | 2ª *(dopo la correzione del timeout)* | 59 | 46 | 13 | **0** | 63.518 |
> | 3ª *(dopo le guardie di idempotenza)* | 59 | 46 | 13 | **0** | **0** |
>
> La terza corsa a zero è il risultato che conta: la retention **converge** e si può rieseguire ogni
> notte senza rifare lavoro già fatto.
>
> **Le righe davvero rimosse o modificate**, in ordine di grandezza: `notification.inapp_messages`
> 159.692 · `notification.deliveries` 87.371 azzerate + 83.796 cancellate · `booking.reservations`
> 41.944 note azzerate · `identity.customer_customers` 17.257 note azzerate e **93 anagrafiche
> anonimizzate** · `booking.resource_consumptions` 15.367 · `switchboard.voice_call_diagnostics` 7.355
> · `payment.transaction_charges` 4.308 archiviate · `marketing_campaign_recipients` 5.245 ·
> `booking.service_lines` 1.487 · `fiscalprint.receipts` 1.387 · `booking.spot_holds` 927 ·
> `booking.reservations` 650 archiviate · `identity.customer_bathhouse_likes` 111 ·
> `payment.wallet_transactions` 82 archiviate.
>
> **Gli archivi su LocalStack** (bucket dedicato `summerbooking-archivio-retention`, creato dal job):
> 3 oggetti, **334.370 byte compressi**. Riletti per verifica: l'archivio di `booking.reservations`
> contiene 650 righe da 37 colonne, e **la colonna `note` è già NULL** — la prova che l'ordine
> «minimizza → archivia → cancella» funziona: l'archivio non porta con sé i campi liberi che una
> policy più breve deve azzerare, e l'archivio non si riscrive.
>
> **L'anonimizzazione**, verificata riga per riga: nome, telefono e città diventano
> `anonimizzato-<id>` (il valore porta la chiave primaria, non una costante, altrimenti la seconda
> riga collide sull'indice unico dell'email); la data di nascita diventa NULL; **codice fiscale e
> importi restano**, perché servono agli obblighi fiscali e non sono nei `field_names` della policy.
>
> #### Altri due difetti trovati dalla corsa reale
>
> **5. Il timeout predefinito di Npgsql è sbagliato per un job batch.** La prima corsa ha fallito su
> `booking.reservations` con «Exception while reading from stream | Timeout during reading attempt»
> **dopo** aver scritto l'archivio. Misurato con `EXPLAIN ANALYZE`: cancellare **650** righe richiede
> **42,7 secondi** — non per il volume (quelle righe non hanno figlie) ma per i **trigger di verifica
> delle 14 chiavi esterne** che puntano a `booking.reservations`, uno per riga per vincolo. Il default
> di Npgsql è 30 secondi. Ora il tetto è `RetentionRunOptions.StatementTimeoutSeconds` (1800) e passa
> da un'**unica fabbrica di comandi**, perché dimenticarlo su uno solo — quello della cancellazione —
> è esattamente l'errore che è costato questa corsa.
>
> > ⚠️ **Il numero da ricordare**: 650 righe in 43 secondi significa che su volumi veri il collo di
> > bottiglia non è la cancellazione ma la verifica delle chiavi esterne. Una tabella con molte FK in
> > entrata va cancellata a lotti, o la finestra notturna non basta.
>
> **6. `null_fields` e `anonymize` non erano idempotenti.** La seconda corsa ha riscritto 41.943 righe
> di `booking.reservations`, 17.257 di `customer_customers` e 3.575 di `deliveries` **già azzerate**,
> perché la condizione guardava solo la data. Su un job giornaliero sono ~63.000 versioni di riga al
> giorno da far ripulire al vacuum, per niente. Ora la condizione include `IS NOT NULL` (azzeramento) e
> `IS DISTINCT FROM <espressione>` (anonimizzazione), e **lo stesso filtro vale per il conteggio**:
> così il verbale dice quanto *resta* da fare, che è il numero su cui si decide.
>
> #### I quattro difetti trovati eseguendo, non ragionando
>
> Vale la pena elencarli perché nessuno dei quattro si vedeva leggendo il codice, e tre su quattro
> avrebbero rotto l'archiviazione in silenzio.
>
> **1. `DisablePayloadSigning` funziona solo su HTTPS** (trovato da `BuildingBlocks.Infrastructure.Tests`). L'avevo impostato
> per «agevolare» un endpoint non-AWS; l'SDK v4 lo rifiuta su HTTP, e LocalStack parla HTTP. **Ogni**
> policy `archive_cold` sarebbe fallita alla prima corsa.
>
> **2. Assegnare `RegionEndpoint` dopo `ServiceURL` azzera l'endpoint** (trovato da `BuildingBlocks.Infrastructure.Tests`).
> Le richieste partono verso AWS vera: un archivio di righe di produzione finirebbe sul bucket
> sbagliato. L'errore che si vede è «The AWS Access Key Id you provided does not exist», che sembra un
> problema di credenziali e invece è di indirizzo. L'ordine di quelle due assegnazioni ora ha un
> commento nel codice.
>
> **3. Lo stream compresso veniva chiuso due volte** (trovato dalla prima esecuzione reale). Tutte e
> tre le policy `archive_cold` con righe da archiviare sono fallite con «Cannot access a disposed
> object: GZipStream» — **dopo** aver caricato l'oggetto su S3.
>
> > ⚠️ **Qui si è visto che l'ordine a prova di perdita funziona.** La sequenza è «prima l'archivio,
> > poi la cancellazione»: l'errore è arrivato in mezzo, quindi i tre archivi erano scritti e le
> > **651 + 4.308 + 82 righe erano ancora tutte nel database**. Il verso innocuo dell'errore sono tre
> > oggetti in più sul bucket, non righe perdute. Se l'ordine fosse stato l'inverso, quella corsa
> > avrebbe cancellato 5.041 righe e poi fallito l'archiviazione.
>
> **4. L'ordine fra policy della stessa tabella non è indifferente** (visto guardando i numeri della
> prima esecuzione). `booking.reservations` ha tre policy: azzera le note a 1 stagione, archivia a 2
> stagioni, cancella a 10 anni. Se l'archiviazione girasse **prima** dell'azzeramento, l'archivio
> conterrebbe le note dei clienti — che possono contenere dati sulla salute (art. 9 GDPR) — e
> **l'archivio non si riscrive**. Il motore ora ordina deliberatamente: prima si **minimizza**
> (`null_fields`, `anonymize`), poi si **archivia**, poi si **cancella**. Prima funzionava per caso,
> perché `duration_value` cresceva nell'ordine giusto.

> #### ⚠️ Un nodo aperto che il motore rende visibile: le policy a due fasi
>
> Cinque tabelle hanno **due** policy: archivia a N, cancella a 10 anni — `fiskaly.receipts`,
> `payment.ledger_entries`, `payment.pos_charges`, `booking.reservations`, `booking.resale_credit_ledger`.
> Con `archive_cold` implementata come «esporta su storage **e poi cancella dal database caldo**», la
> seconda fase non trova più niente: le righe hanno lasciato il database quando è scattata la prima.
>
> Il risultato è corretto per il database caldo ma **l'archivio non viene mai ripulito**, e il report
> della seconda policy dice «0 righe» con l'aria di non aver niente da fare, quando in realtà il dato
> è altrove. Le due letture possibili:
>
> - `archive_cold` **copia** e lascia la riga, e il `delete_row` a 10 anni la rimuove. Ma allora
>   «resta consultabile per la stagione corrente e le due precedenti» (§3) non è vero: la riga resta
>   calda per dieci anni;
> - `archive_cold` **sposta** (l'implementazione attuale), e la cancellazione a 10 anni va riscritta
>   come una policy **sull'archivio** — cioè un `delete_object` su un prefisso di archivio, che oggi
>   non esiste.
>
> La seconda è più coerente con la politica.
>
> #### ✅ RISOLTO il 2026-10-05 — e non con un prefisso
>
> La seconda lettura è stata implementata, ma **senza** la policy su prefisso che questa sezione
> immaginava. Un prefisso di archivio sarebbe stato il modo peggiore: per sapere quali oggetti sotto
> `archivio/` sono scaduti bisogna elencare il bucket e **fidarsi della data scritta nel nome della
> chiave**, cioè dedurre un termine di legge da una convenzione di naming.
>
> Al suo posto c'è un **inventario nel database**: [`privacy_archive_manifests`](../analisi-post/db/privacy/privacy_archive_manifests.md),
> una riga per oggetto scritta dal motore prima di cancellare le righe, e sopra di essa una policy
> ordinaria:
>
> ```
> privacy · archive_manifests · archived_at · 10 anni · delete_object · only_if_unreferenced = true
> ```
>
> Il vantaggio non è solo di eleganza: su una tabella di inventario il perimetro della policy è
> **l'intera tabella**, perché ogni riga nomina un oggetto e sono tutte della stessa natura. Nessuna
> condizione da dichiarare, che è esattamente il problema che blocca le policy sulle tabelle di asset.
>
> **Quello che resta vero di questa sezione**: il report della seconda policy di una coppia continua a
> dire «0 righe». Non è un errore — le righe hanno lasciato il database alla prima fase — ma va letto
> sapendolo, perché ha l'aria di una policy che non ha niente da fare.
>
> ⚠️ **Quello che resta approssimato**: un oggetto di archivio contiene righe di **una** tabella, ma
> tabelle diverse hanno termini diversi, e la policy li appiattisce tutti su dieci anni — il più lungo.
> È prudente, non preciso. Policy più specifiche per `source_table` si possono aggiungere senza
> toccare il codice.

> ### ✅ Quello che questi job trovano già pronto (2026-10-03)
>
> Le etichette e la coda citate sopra **esistono**: `privacy.retention_action` ha `delete_object` e
> `archive_object`, `privacy.privacy_file_deletions` è stata creata, e **15 policy sui file sono
> seminate**, tutte con `is_dry_run_only = true`. Elenco delle migration in
> [`83-retention-file.md` §5](83-retention-file.md); forma delle righe nella scheda di
> [`privacy_retention_policies`](../analisi-post/db/privacy/privacy_retention_policies.md), sezione
> **FILE** del seed. 🔄 **I job sono stati scritti il 2026-10-03** (riquadri sopra): resta da fare la
> **pianificazione**, perche' non c'e' ancora un host dei job e i cron di questa tabella non sono cablati.
>
> Quattro cose che un job deve rispettare, e che a schema non sono esprimibili:
>
> 1. ⚠️⚠️ **Su una policy con `storage_prefix`, non è il prefisso a decidere: è l'assenza di
>    riferimento.** Il prefisso limita solo *dove* guardare. Un job che implementasse quelle righe
>    come «cancella tutto sotto questo prefisso oltre N» su `images/bathouse/` cancellerebbe **tutte**
>    le foto degli stabilimenti. Difesa obbligatoria, la stessa che già applica
>    `uploader.clean_test_prefix` di `utility/02-migrazione-file`: elenco chiuso dei prefissi
>    ammessi, rifiuto di tutto il resto, e rifiuto di qualunque prefisso che sia **antenato** di un
>    altro prefisso noto.
> 2. **`delete_object` non si esegue in linea**: si accoda in `privacy_file_deletions`. Una `DELETE`
>    su S3 non partecipa alla transazione del database, ed è esattamente l'errore che il legacy ha
>    fatto — ~12.000 file orfani sul bucket sono cancellazioni decise e non portate a termine.
> 3. **`duration_value = 0` è legittimo** e va ammesso dalla validazione: otto delle 15 righe nuove lo
>    usano per dire «senza attesa». Il commento della colonna dice «validata positiva in
>    applicazione», e preso alla lettera le scarterebbe tutte.
> 4. **La soglia stagionale va calcolata da una funzione sola, condivisa.** «2 stagioni» significa *1
>    gennaio dell'anno N−2, ricalcolato a ogni esecuzione*. Se ogni job stagionale se la ricalcola,
>    prima o poi uno userà il **1 aprile** — che è l'errore che la documentazione conteneva fino al
>    2026-10-01. Riguarda 10 policy: le 9 già validate più F8 (i QR). Vedi
>    [`83-retention-file.md` §9](83-retention-file.md).
>
> ⚠️ **L'attivazione si fa una policy per volta**, leggendo il report di dry-run: `is_dry_run_only =
> false` non va messo a tutte e 59 insieme. Per una regola che tocca 743.576 oggetti non è prudenza
> eccessiva, è il minimo.
| `CompactSoftDeleted` | `0 4 8 * *` | trasversale (per modulo) | righe con `deleted_at` oltre 90 gg, o il termine legale se maggiore |
| `DeprovisionClosedProperty` | `0 5 * * *` (giornaliero) | Catalog / Identity | cascata §5 di `80-retention.md` in base a `mesi_da_cessazione` |
| `EnforceGdprErasure` | **su evento** (non schedulato) | trasversale | richieste art. 17, rispettando i `privacy_legal_holds` |

> **Nota sul partizionamento**: `SeasonalArchive` pota i contatori di consumo con una `DELETE`, ma
> `booking.resource_consumptions` è ora partizionata per anno — dove l'intervallo da potare coincide
> con una partizione, `DETACH` + `DROP` costa quanto cancellare un file invece di riscrivere ogni riga.
> Le due granularità (stagioni vs anni solari) non coincidono sempre, quindi il job deve saper fare
> entrambe: vedi [`06-partizioni-manutenzione.md` §5](06-partizioni-manutenzione.md).

Regole che valgono **solo** per questa famiglia, in aggiunta a quelle del §4:

1. **Dry-run è lo stato di nascita, non un'opzione di debug.** Una policy seminata ha
   `is_dry_run_only = true`: il job produce il report di cosa *farebbe* e non tocca niente. Il passaggio
   all'esecuzione reale è un atto umano (SystemAdmin/DPO) dopo aver letto il report.
2. **Export verificato prima di ogni cancellazione massiva** ([`40-backup.md`](40-backup.md)). Il dato
   "sparisce dal DB caldo", non "sparisce".
3. **Legal hold batte la policy.** Un record sotto `privacy_legal_holds` si salta, non si cancella — il
   job lo conta e lo logga, non fallisce.
4. **Log strutturato con conteggi** (`valutati=…, archiviati=…, cancellati=…, saltati_per_hold=…,
   durata=…`) e scrittura in `privacy_compliance_audit_logs`: è la prova di conformità (art. 5.2 GDPR),
   non un log diagnostico — non finisce su CloudWatch e basta.
5. **Mensili scaglionati su giorni diversi** (colonna Schedule): girano di notte e scrivono molto; farli
   partire tutti il primo del mese alle 4 concentrerebbe I/O e lock su una sola finestra.
6. **`SeasonalArchive` gira una volta l'anno**: è il job con meno occasioni di essere collaudato in
   produzione. Va reso ri-eseguibile a mano su un singolo stabilimento/stagione da un endpoint interno,
   altrimenti il primo vero collaudo è anche la prima cancellazione.
7. **Due decorrenze diverse dentro lo stesso job**: in `SeasonalArchive` la testata scade a N−2 ma
   `note` e le prenotazioni di soli servizi a N−1. Due passate distinte, non una soglia unica — con una
   soglia sola le note (dato potenzialmente art. 9 GDPR) resterebbero una stagione di troppo.

---

## 2. Dove vive un job nel nuovo backend

Separa **sempre** due cose:

| Parte | Dove | Forma |
|---|---|---|
| **Logica** (cosa fa) | Application layer del modulo proprietario | Application service normale: `CancelExpiredBookingsService.RunAsync(CancellationToken)` |
| **Trigger** (quando parte) | Host (`Boot`) | Adapter sottile: `IHostedService`/`BackgroundService`, o registrazione in un job runner (Hangfire/Quartz/Coravel) |

Vantaggi: la logica è **unit-testabile** senza scheduler, è **richiamabile da un endpoint** (utile per
run manuali / debug), e al momento dello split microservizi **si sposta col modulo** senza riscritture.

```
Boot (host)
 └─ scheduling (cron expression, timezone Europe/Rome, retry policy)
      └─ chiama ──► Booking.Application.CancelExpiredBookingsService   (logica)
                     └─ repo del modulo + IEventBus.Publish(BookingExpired)
```

Regola non negoziabile: il job orchestratore parla con gli altri moduli **solo tramite interfaccia
pubblica o evento** — mai query/JOIN sulle tabelle di un altro modulo (l'errore di `back-pre`).

---

## 3. Un processo o due? (mai N solution)

| Scenario | Scelta | Quando |
|---|---|---|
| **In-process con l'API** | I job girano come `BackgroundService` dentro `Boot`, che richiama gli application service dei moduli | **Default adesso.** Un solo deployable, coerente col monolite modulare |
| **Progetto `Worker` separato** (SDK `Microsoft.NET.Sdk.Worker`) **nella stessa `SummerBooking.sln`** | Referenzia gli stessi progetti-modulo, riusa gli stessi application service | Solo con un driver reale: job pesanti, scaling indipendente, non devono competere con l'API, cadenza di deploy diversa |
| **Una solution per job** | ❌ Mai | Frammenta il codice, duplica il composition root, rompe il modello "un monolite modulare" |

Il massimo consentito nel monolite: **`Boot` (API) + `Worker` (job)**, stessa solution, stessi moduli.

---

## 4. Motore di scheduling

Per pochi cron: `PeriodicTimer` + parser cron (NCrontab) o **Coravel** bastano.

Passa a **Hangfire** o **Quartz.NET** quando servono:

- **persistenza** dei job e **retry** automatici;
- **single-execution** quando l'host scala a più istanze (Hangfire con storage / lock distribuito) —
  necessario per i job che toccano soldi (`CancelExpiredBookings`, disattivazione link Stripe);
- dashboard/monitoraggio.

In ogni caso:

- **Idempotenza**: ogni job deve poter girare due volte senza danni (marcatura "processato",
  `WHERE ... AND processed = 0`, chiavi naturali).
- **Overlap guard** reale (non in-process): lock distribuito o job store, non un `bool isRunning`.
- **Timezone**: `Europe/Rome` esplicito, non ereditato dal server.
- **Osservabilità**: log strutturato con esito, durata, numero record processati; niente file locali.
- **Cross-modulo → eventi + outbox**: il job fa la sua parte in una transazione del **suo** `DbContext`
  e pubblica un integration event; gli altri moduli reagiscono (vedi §6 delle linee guida).

---

## 5. Evoluzione verso i microservizi

**I job non "escono" per diventare servizi indipendenti.** Un job segue il modulo di cui esegue la
logica.

- Quando `Booking` viene estratto in servizio, il job "annulla prenotazioni scadute" va **dentro il
  servizio Booking** (suo `BackgroundService` o progetto `Booking.Worker` nel repo/solution di
  Booking), perché richiama i suoi application service e tocca il suo schema.
- Ogni microservizio estratto ha tipicamente **API host + (opzionale) Worker host**, nel proprio
  repo/solution, che condividono gli stessi application service.
- I **job cross-modulo** vanno spezzati **già ora** via eventi: `updateCancelBooking` diventa
  Booking che emette `BookingExpired` + Payment che reagisce disattivando il link. Al momento
  dell'estrazione ogni pezzo è già nel posto giusto — nessun "job orchestratore" da smistare.
- Se un flusso multi-step diventa una **saga** vera (prenota → paga → scontrino → notifica),
  l'orchestrazione vive nel servizio che possiede quel processo (di norma Booking) o in un servizio
  di orchestrazione dedicato: è una scelta di dominio, non "i job vanno tutti insieme da qualche parte".
- Ciò che **può** diventare infrastruttura condivisa è solo il **motore di scheduling** (es. Hangfire
  col suo storage, o un trigger esterno che manda messaggi in coda / chiama endpoint). La logica di
  business resta nei servizi.

| Fase | Dove sta il job |
|---|---|
| Monolite modulare (oggi) | Logica nell'Application del modulo; trigger in `Boot`. Eventuale `Worker` unico nella stessa solution |
| Modulo estratto in microservizio | Logica e trigger migrano **col modulo**, nel suo repo/solution (API host + Worker host di quel servizio) |
| Job "indipendente" | Non esiste come concetto: se sembra indipendente, o è mal ritagliato o è una saga con un proprietario preciso |

---

## 6. Il progetto `Shared` serve? Va su NuGet?

**Serve**: è già previsto dalle linee guida (`SharedKernel` puro + `BuildingBlocks.Infrastructure`
opzionale) e la cartella [`backend/summerbooking-backend/Shared/`](../../../backend/summerbooking-backend/Shared/)
esiste già.

**Non su NuGet, non ora.** È consumato da una sola solution: va tenuto come `ProjectReference` dentro
`SummerBooking.sln`. Un pacchetto NuGet aggiunge versioning, pipeline di publish e allunga l'inner
loop — costo senza beneficio per 2-3 dev su una solution. Il `Worker`, stando nella stessa solution,
fa semplicemente `ProjectReference` a `Shared`.

**Quando** estrarre un **NuGet privato** (AWS CodeArtifact / GitHub Packages): solo quando un
deployable separato in un *altro repo* avrà bisogno degli stessi contratti — cioè quando si estrae
davvero un modulo come microservizio. La forma prevista (2026-10-07) è in
[linee-guida-implementative-dotnet.md §7bis](pre/linee-guida-implementative-dotnet.md): **due pacchetti**,
Shared (contratti, per tutti) e BuildingBlocks.Infrastructure (implementazioni, solo per chi le usa),
quest'ultimo eventualmente diviso per area; i contratti di ogni servizio separati in un proprio pacchetto.
Dal 2026-10-07 Shared non ha più dipendenze tecniche, quindi è già impacchettabile così com'è.

---

## 7. Rapporto con i cron di `back-pre`

- I cron Node di `back-pre` restano attivi sul sistema legacy **fino al cutover**: repo e deploy
  separati, **nessuna condivisione di codice** con il .NET.
- Nel nuovo backend i job si **reimplementano** come application service + trigger nel modulo giusto.
  "Stessi job con modifiche" = stesso intento, implementazione nuova — non un porting 1:1 dei cron.
- I `updateBookingNumberIn*` e simili backfill **non** sono job: sono migrazioni una-tantum.

---

## 8. Riepilogo delle regole d'oro

1. **Job = meccanismo, non dominio.** Logica nell'Application del modulo, trigger nell'host.
2. **Mai una solution per job.** Al più `Boot` + `Worker` nella stessa `SummerBooking.sln`.
3. **In-process di default**; `Worker` separato solo con un driver reale.
4. **Cross-modulo solo via evento/interfaccia + outbox**, mai JOIN sulle tabelle altrui.
5. **Idempotenza + single-execution + `Europe/Rome`** su ogni job; logging strutturato, niente file.
6. **In microservizi il job migra col suo modulo**, non diventa un servizio a sé.
7. **`Shared` resta `ProjectReference`**; NuGet privato solo quando un altro repo lo consuma davvero.
8. **I job di retention non conoscono le durate**: le leggono da `privacy_retention_policies`, nascono in
   dry-run, rispettano i legal hold e scrivono l'esito in `privacy_compliance_audit_logs` (§1.5).
