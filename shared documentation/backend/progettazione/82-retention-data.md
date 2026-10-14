# Validazione dei periodi di retention — tabelle e scelte del proprietario

> Trascrizione di `docs/retention (1).docx` — la versione del documento di validazione **compilata dal
> proprietario/titolare** (colonna "Considerazioni / scelta del proprietario" valorizzata riga per riga).
> È il verbale delle decisioni; l'elaborazione tecnica derivata è in [80-retention.md](80-retention.md).
>
> Questo documento raccoglie le sole tabelle di sintesi della politica di conservazione e cancellazione
> dei dati, per validare i periodi proposti. L'ultima colonna di ogni tabella riporta la scelta del
> proprietario: conferma, deroga, oppure un periodo/modalità alternativa con la relativa motivazione — nei
> casi in cui erano offerte più opzioni (es. 6-12 mesi) o una scelta di tipo di archiviazione (in genere
> S3 [più sicuro e basso costo] o locale [costo per download, maggior "controllo"]).
>
> I tempi indicati sono una base di lavoro, non un parere legale: la colonna **"Retention minima
> (legale)"** (obblighi fiscali/contabili, provvedimenti del Garante) va confermata con il commercialista
> e con un consulente privacy; la colonna **"Retention operativa consigliata"** è una scelta tecnica del
> team, modificabile.

> **Aggiornamento 2026-09-17** — chiarimenti richiesti al titolare in
> [`retention-chiarimenti.docx`](retention-chiarimenti.docx) su alcune contraddizioni emerse fra la
> tabella per categoria (§3) e il cheat sheet (§6), più una tensione con l'obbligo legale sulle spese POS.
> Risposte ricevute, riportate nelle celle "Considerazioni / scelta del proprietario" interessate (con
> nota "chiarito 2026-09-17"):
> - **Q1/Q2** — dettaglio prenotazione e assenze/rivendita del posto/riserve morbide: confermato **N-2**
>   (non N-1 come inizialmente accettato con "ok" nella tabella per categoria).
> - **Q3** — anagrafica pubblica stabilimento dopo cessazione: la scheda **resta pubblicata** ma
>   **ridotta alle sole informazioni essenziali**, e **non resta attiva la possibilità di prenotare
>   online**.
> - **Q4** — spese/addebiti POS: confermato l'obbligo legale, **10 anni** (corregge la precedente
>   indicazione "cancella archivio a 5 anni").
> - **Q5** — cancellazione account staff: per l'audit fiscale/contabile è sufficiente il riferimento allo
>   **stabilimento**, non serve preservare l'identità dell'**utente**.
> - **Q6** — prenotazioni di soli servizi: l'annotazione "S-1" era un refuso, confermato **N-1**.
> - **Risposta evidenziata in giallo nel docx originale** (mappatura utente → Stripe Customer, §3.2):
>   tempistica confermata **N-2**.

---

## Legenda

| Termine | Significato |
|---|---|
| **Stagione (S)** | **Ai fini della retention, la stagione è l'anno di calendario: il confine è il 1 gennaio** ✅ *(deciso dal titolare il 2026-10-01)*. Non si usano i confini per-stabilimento di `seasons_periods` / `catalog.pricing_seasons`: quelli servono al prezzo e all'operatività, non a decidere quando cancellare. Il batch può continuare a girare all'apertura della stagione (~1 aprile): **quando gira non cambia la soglia**, che è sempre il 1 gennaio dell'anno N−*k*. |
| **fine S** | Chiusura della stagione a cui il dato appartiene: da qui parte la retention operativa. |
| **N-1** | Tenere online la stagione corrente E quella precedente (utile per rinnovi, "stesso ombrellone dell'anno scorso", confronto ricavi). |
| **N-2 / +1 S** | Archiviare tutto ciò che ha più di una stagione piena. |
| **10 anni** | Termine di conservazione fiscale/contabile italiano (art. 2220 c.c.); i dati fiscali non si cancellano prima di questa scadenza, al più si archiviano fuori dal DB operativo. |
| **Cancella** | Rimozione fisica della riga dal database (hard-delete). |
| **Anonimizza / pseudonimizza** | Sostituzione di nome, email, telefono e note con un segnaposto ("Cliente #id"), mantenendo id e importi per la contabilità. |
| **Archivia** | Spostamento su storage freddo (S3/Glacier o tabella di archivio o salvataggio in locale) e rimozione dal database operativo. Non è una cancellazione: il dato resta recuperabile. |
| **Backup prima di cancellare** | Nessuna cancellazione massiva senza un export verificato su storage separato. |
| **Considerazioni / scelta del proprietario** | Colonna da compilare in fase di validazione. |

---

## 3. Retention per categoria di dato

Legenda azione a scadenza: **Cancella** = rimozione fisica; **Anonimizza** = pseudonimizzazione (id +
importi restano); **Archivia** = storage freddo, riga rimossa dal DB operativo.

### 3.1 C1 - Prenotazioni (ombrellone / servizi / riserve)

| Categoria di dato | Tabelle principali | PII / sensibile | Retention minima (legale) | Retention operativa consigliata | Decorrenza | Azione a scadenza | Considerazioni / scelta del proprietario |
|---|---|---|---|---|---|---|---|
| Testata prenotazione ombrellone (intestatario, canale, stato, riepilogo economico, `booking_number`) | `bookings` → target `bookings`/`reservations` (testata), `booking_payments`, `parent_bookings` | nome/email/telefono intestatario e ospiti | 10 anni (riepilogo economico collegato al fiscale) | online: N-1 (stagione corrente + precedente); poi archivia con PII pseudonimizzata e soli importi/date | fine S della prenotazione | Archivia + anonimizza a fine S; cancella l'archivio dopo 10 anni | **Retention operativa online N-2** |
| Righe/dettaglio postazione (griglie prezzo giorno-per-giorno, snapshot mappa, `reducedPriceObj`, articoli per data, snapshot cliente JSON) | campi JSON di `bookings`; target `booking_lines` | indirizzo/nome duplicati negli snapshot | nessuno (dato ricostruibile / non fiscale) | fine S + 1 S (tieni la stagione in corso e quella appena chiusa per contestazioni/riemissione), poi cancella il dettaglio tenendo la testata | fine S | Cancella | **N-2** (chiarito 2026-09-17 — sostituisce il precedente "ok" su N-1, allineato al cheat sheet) |
| Prenotazioni di soli servizi (bar / normali / a ore) | `service_reservation`, `service_reservation_payments` | nome/contatto cliente | 10 anni per il riepilogo economico se fiscalizzato | fine S: backup poi cancella il dettaglio all'apertura della stagione successiva; resta la testata economica se c'è scontrino | fine S | Archivia testata / cancella dettaglio | **N-1** (chiarito 2026-09-17 — l'annotazione "S-1" era un refuso) |
| Contatori di consumo inventario (ombrelloni, lettini, articoli aggiuntivi, servizi/giorno, slot orari) | `booking_items_counts_details`, `additional_items_counts_details`, `services_booking_counts`, `hourly_service_booking_management` → target `resource_consumptions` | no | nessuno | fine S + 1 S poi cancella (eventuale aggregato mensile/stagione per la reportistica storica) | fine S | Cancella (dopo aggregazione) | **ok** |
| Assenze pianificate / rivendita del posto | `booking_absence_details` (attiva), `booking_absences` (legacy) | no | nessuno | N-1 (serve per confronti di occupazione), poi cancella | fine S | Cancella | **N-2** (chiarito 2026-09-17 — sostituisce il precedente "ok" su N-1, allineato al cheat sheet) |
| Sharing / crediti da condivisione postazione | `sharing_details` | riferimento utente titolare | il credito maturato segue la contabilità del wallet (C3, 10 anni); il dettaglio giornaliero no | dettaglio: N-1 poi cancella; saldo credito: verso wallet C3 | fine S | Cancella dettaglio / archivia movimento credito | **ok** |
| Riserve morbide di postazione (manutenzione, VIP, blocco checkout) | `location_reservations`, `hold_locations` | no | nessuno | fine S → cancella (i blocchi scaduti non hanno valore); `hold_locations` con TTL di minuti in checkout | fine intervallo / fine S | Cancella | **N-2** (chiarito 2026-09-17 — sostituisce il precedente "Ok" su "fine S", allineato al cheat sheet) |
| Raggruppamenti e visibilità condivisa | `booking_groups`, `bookings_requested_details`, `booking_areas` | id utente che ha aperto il codice | nessuno | segue la testata (N-1) poi cancella | fine S | Cancella | **Ok** — segue la testata, quindi ora **N-2** (allineato 2026-09-17) |
| Contatore numero prenotazione per stabilimento | `booking_logs` → target `booking_number_sequences` | no | - | permanente finché lo stabilimento è attivo (è una sequenza, non uno storico) | - | Conserva | **ok** |
| Report periodici alle strutture convenzionate (arrivi/partenze/ricavi) | `hotel_reports` | nomi ospiti nel PDF | se contiene dati usati per la contabilità dello stabilimento: allineare al fiscale | 24 mesi poi cancella il PDF/record (il dato grezzo resta in `hotel_payments`/`bookings`) | data del report | Cancella | **ok** |

### 3.2 C3 - Pagamenti (gateway, incassi, wallet, POS)

| Categoria di dato | Tabelle principali | PII / sensibile | Retention minima (legale) | Retention operativa consigliata | Decorrenza | Azione a scadenza | Considerazioni / scelta del proprietario |
|---|---|---|---|---|---|---|---|
| Transazioni online e movimenti contabili | `online_transaction_details`, `transactions_payments`, `card_transactions` (wallet), `booking_payments`/`service_reservation_payments` (movimenti nei JSON) → target `payment_transactions`, `customer_wallet_transactions` | id cliente | 10 anni (scrittura contabile) | online: 24 mesi (copre riconciliazione + finestra chargeback con ampio margine), poi archivia a freddo per il resto dei 10 anni | data movimento | Archivia a 24 mesi, cancella archivio a 10 anni | **ok** |
| Rimborsi | `refund_transaction_details` → target `payment_refunds` | id cliente | 10 anni | come sopra | data rimborso | Archivia / cancella | **ok** |
| Link di pagamento Stripe ("riepilogo pagamento") | `bookings_send_payment_summaries` | email/telefono destinatario | il fatto "pagamento richiesto, importo, esito" → 10 anni (C3); il canale d'invio → C6 | fatto: segue la transazione; payload di invio (`method_flag`, `email_sent`, testo): 12 mesi poi cancella | data invio | Cancella payload / archivia fatto | **ok** |
| Spese/addebiti da POS esterni (TeamSystem / Cassa in Cloud, OfSale) | `teamsystem_expenses`, righe in `booking_number_informations` | no | 10 anni (concorrono al conto/fiscale) | 24 mesi online poi archivia | data spesa | Archivia / cancella | **10 anni** (chiarito 2026-09-17 — corregge la precedente indicazione "cancella archivio a 5 anni": confermato l'obbligo legale) |
| Mappatura utente → Stripe Customer / carte salvate | `user_stripe_card_details` | riferimento a customer Stripe (token, mai PAN) | - | eliminare alla chiusura account o dopo 24 mesi di inattività; mai PAN/CVV in chiaro | ultima attività / chiusura account | Cancella | **Solo dopo 24 mesi, anche se chiude l'account permane** — tempistica: **N-2** (chiarito 2026-09-17) |
| Configurazione gateway per stabilimento (id account Stripe Connect) | `stripe_configrations` | - | - | vive quanto lo stabilimento; cancella alla cessazione (con revoca del collegamento Connect) | cessazione contratto | Cancella | **ok** |

### 3.3 C4 / C4bis - Scontrino fiscale elettronico e stampa RT

| Categoria di dato | Tabelle principali | PII / sensibile | Retention minima (legale) | Retention operativa consigliata | Decorrenza | Azione a scadenza | Considerazioni / scelta del proprietario |
|---|---|---|---|---|---|---|---|
| Scontrino elettronico emesso (documento commerciale, righe fiscali, PDF su S3) | `fiskaly_receipt`, `fiskaly_non_fiscal_receipt`, `booking_number_informations` (righe fiscali), `fiskaly_receipt_sequence` | dati acquirente se richiesti | 10 anni dalla data del documento | 10 anni: online i più recenti (es. 24 mesi), poi archivia (metadati in DB freddo, PDF su S3 Glacier o locale). Mai cancellare prima dei 10 anni | data emissione | Archivia a 24 mesi, cancella a 10 anni | **Online a 36 mesi** |
| Account fiscale dello stabilimento (id/stati entità Fiskaly, credenziali) | `fiskaly_account` | credenziali API + Fisconline (segreti) | i dati del titolare legati ai documenti → 10 anni | credenziali → secret manager, non in tabella; il record vive quanto il contratto + 10 anni per i riferimenti fiscali | cessazione contratto | Anonimizza (rimuovi segreti) / archivia | **Ok - archivia** |
| Log del dialogo con l'API SIGN IT (transizioni, errori) | `fiskaly_receipt_log` | payload con importi/righe | nessuno (è diagnostica, non il documento) | 12-24 mesi poi cancella | data evento | Cancella | **Retention 12 mesi** |
| Cash register / device Fiskaly | `fiskaly_cash_register` | - | riferimento nei documenti → tenere il minimo per 10 anni | soft-delete (`deleted_at`) → cancella dopo 10 anni dall'ultimo documento che lo referenzia | ultimo documento collegato | Archivia / cancella | **Ok, azione a scadenza: cancella** |
| Stampa fiscale su hardware RT (config stampanti, log stampe, seriali) | `printer_configs`, `booking_receipts`, `receipt_serial_numbers` | intestazioni/footer | il documento fiscale è il journal della RT (lato dispositivo), non queste tabelle | `booking_receipts` (log stampe): 12 mesi poi cancella; `printer_configs`: vive col dispositivo | data stampa | Cancella | **ok** |

### 3.4 C5 - Identità, anagrafiche, contratti

| Categoria di dato | Tabelle principali | PII / sensibile | Retention minima (legale) | Retention operativa consigliata | Decorrenza | Azione a scadenza | Considerazioni / scelta del proprietario |
|---|---|---|---|---|---|---|---|
| Account applicativi (gestori, staff) | `users`, `user_roles` | email, telefono, nome, foto, id social | - (salvo azioni collegate a fiscale) | disabilita alla revoca accesso; anonimizza dopo 24 mesi da disabilitazione (o dalla cessazione dello stabilimento); rimuovi subito i campi morti (`birthDate`, `gender`) | disabilitazione / cessazione | Anonimizza poi cancella | **Dopo 12 mesi dalla revoca cancellazione** — ai fini dell'audit fiscale/contabile basta il riferimento allo **stabilimento**, non serve preservare l'identità dell'**utente** (chiarito 2026-09-17) |
| Anagrafica clienti finali | `customers` | nome, email, telefono, indirizzo, data nascita, codice fiscale, P.IVA, note | i dati collegati a un documento fiscale → 10 anni (solo il minimo necessario) | anonimizza dopo 24-36 mesi senza prenotazioni; se esistono documenti fiscali entro i 10 anni si tiene la versione pseudonimizzata (id + CF/importi minimi), non i contatti | ultima prenotazione | Anonimizza a 24-36 mesi, cancella completo a 10 anni | **Anonimizza dopo 48 mesi e cancella a 10 anni** |
| Campo note libero su `customers` / `bookings` / `service_reservation` | come sopra | possibili dati salute (art. 9 GDPR) | - | troncare/limitare in migrazione; retention breve (segue la prenotazione, poi cancella col dettaglio) | fine S | Cancella | **N-1** |
| Flag `isBlackList` | `customers` | dato valutativo su persona | - | rivedere periodicamente (es. annuale); rimuovere se non più giustificato; documentare base giuridica | inserimento | Revisione / cancella | **Revisione annuale da parte del gestore**  🔄 *(2026-10-02, D-65.3: la revisione resta a carico del gestore, ma **senza traccia a database** — sul flag si applica la logica del legacy, nessun `blacklist_reviewed_at`. Vedi [`80-retention.md` §6](80-retention.md))* |
| Accettazioni contrattuali | `contract_acceptation`, `contract_documents` | IP, hash PDF | durata contratto + 10 anni (prova) | come da minima; troncare l'ultimo ottetto dell'IP dopo 24 mesi | accettazione / cessazione | Anonimizza IP a 24 mesi, archivia poi cancella a +10 anni | **ok** |
| Preferiti / "mi piace" consumer | `property_like_logs` | id utente consumer | - | cancella alla chiusura account o dopo 24 mesi di inattività dell'utente | ultima attività utente | Cancella | **ok** |
| Anagrafica pubblica stabilimento | `bathouses`, `properties` | contatti titolare (se persona fisica) | 10 anni dopo cessazione per i riferimenti fiscali | vedi cascata "stabilimenti non più attivi" (sezione 5) | cessazione contratto | Archivia / anonimizza / cancella | **Salva solo le informazioni essenziali della scheda** (nome, foto, posizione, descrizione base) **— non resta attiva la possibilità di prenotare online**; il resto della configurazione segue comunque la cascata ordinaria (sezione 5) (chiarito 2026-09-17) |
| Registro richieste GDPR (cancellazione/export) | nuova tabella `erasure_requests` | id interessato + esito | prova di conformità → 10 anni | come da minima | data richiesta | Conserva | **ok** |

### 3.5 C2 - Catalogo & configurazione

| Categoria di dato | Tabelle principali | Retention minima (legale) | Retention operativa consigliata | Decorrenza | Azione a scadenza | Considerazioni / scelta del proprietario |
|---|---|---|---|---|---|---|
| Configurazione viva (mappa spiaggia, settori, stagioni, listini, sconti, servizi, magazzino, revenue, convenzioni, template hotel) | `sectors_rows`, `configurations`, `configure_maps`, `seasons_periods`, `price_lists`, `discounts`, `services_types`, `warehouses`, `revenue_notifications`, `hotels`, `hotel_price_lists`, `hotel_message_templates`, … | - | nessuna retention a tempo: è stato corrente, si tiene finché lo stabilimento è attivo. Alla cessazione: archivia snapshot completo poi cancella dopo 2 anni (utile per un eventuale rientro del cliente), salvo listini/sconti che hanno alimentato prenotazioni fiscalizzate (già congelati negli snapshot di prenotazione) | cessazione contratto | Archivia → cancella dopo 2 anni | **ok** |
| Cataloghi master globali (icone, categorie, filtri, immagini, nazioni, licenze) | `configure_images`, `configure_categories`, `servicetypes_icons`, `service_type_filters`, `country`, `licenses` | - | permanenti (dati di riferimento); si potano solo le voci deprecate | - | Conserva | **Se è solo dello stabilimento, per le prenotazioni cancellare alla sospensione dell'account; se serve per il portale web, da conservare** |
| Storico versioni listino/stagione (se introdotto) | - | concorre alla ricostruzione dei prezzi applicati → allineare a prenotazioni (10 anni per lo snapshot dentro la prenotazione) | tenere solo lo snapshot dentro la prenotazione; non serve uno storico separato del listino oltre N-1 | fine S | Cancella | **ok** |

### 3.6 C6 - Comunicazione e marketing

| Categoria di dato | Tabelle principali | PII / sensibile | Retention minima (legale) | Retention operativa consigliata | Decorrenza | Azione a scadenza | Considerazioni / scelta del proprietario |
|---|---|---|---|---|---|---|---|
| Log invii email (outbox + esito) | `email_logs` | `emailTo`, `body` (spesso nome/cognome nel testo) | - | 90 giorni per il corpo (poi troncare `body`); 12 mesi per i metadati d'invio (esito, timestamp) poi cancella/archivia | data invio | Cancella body a 90 gg, cancella riga a 12 mesi | **Cancella body a 150 gg** |
| Notifiche in-app "per stabilimento" (campanella gestionale) | `notification_details` | testo con nome cliente/prenotazione | - | 6-12 mesi poi cancella (è un feed operativo, non un archivio) | data notifica | Cancella | **6 mesi** |
| Liste contatti e appartenenze | `contact_list`, `contact_list_member`, `subscribes` | email/telefono cliente | - | Bonifica una-tantum: `contact_list_member` non è più scritta da lug 2025 mentre `contact_list` è attiva ad ago 2026. A regime: elimina l'appartenenza quando il cliente viene anonimizzato, o quando la lista non è usata da 24 mesi — clock = `contact_list.updated_at` / ultima campagna che l'ha usata, non il singolo membro. `subscribes` (lista globale write-only, ultima scrittura mar 2026, nessun lettore) → confluisce in `contact_list`; opt-in = base marketing, opt-out = lista di soppressione a vita | ultima attività della lista (`contact_list.updated_at`) / anonimizzazione del cliente / disiscrizione | Cancella (tranne opt-out) | **ok** |
| Campagne e automazioni (definizioni + snapshot destinatari) | `marketing_email_campaign`, `marketing_automation`, `marketing_event`, `marketing_event_notification` | array email in `recipientList` / `recipients` | - | definizione: 24 mesi dopo l'ultimo invio poi cancella; snapshot destinatari: cancella a 6 mesi dall'invio (verificare comunque il consenso al momento dell'invio, non dello snapshot) | data invio / ultimo uso | Cancella | **Ok, da ultimo uso** |
| Lead "richiedi la tua pagina" | `csv_manager_details` | nome, email, telefono, messaggio | - | 12 mesi poi cancella (è un lead, non un cliente); candidato a CRM esterno / invio email senza persistenza | data invio form | Cancella | **ok** |
| Template email/WhatsApp | `custom_templates`, `bathhouse_email_configrations` | credenziali SMTP (segreti) | - | vivono con lo stabilimento; credenziali → secret manager; cancella alla cessazione | cessazione | Cancella | **ok** |

### 3.7 C7 - Switchboard / centralino (oggi 0 righe)

| Categoria di dato | Tabelle principali | PII / sensibile | Retention operativa consigliata | Decorrenza | Azione a scadenza | Considerazioni / scelta del proprietario |
|---|---|---|---|---|---|---|
| Registro chiamate ricevute | `switchboard_call_register` | numeri chiamante/chiamato | 12 mesi poi cancella (o anonimizza il numero) | data chiamata | Cancella / anonimizza | **Retention a 6 mesi – azione a scadenza: cancella** |
| Log diagnostico flusso chiamata (eventi Telnyx, payload) | `switchboard_call_log` | payload completo con numeri | 30-90 giorni poi cancella (diagnostica; oggi già disabilitato di default) | data evento | Cancella | **30 gg** |
| Log webhook WhatsApp (stati consegna, messaggi in arrivo) | `switchboard_wa_msg_log` | numeri, `message_body`, `raw_payload` | 6-12 mesi poi anonimizza il payload / cancella | data evento | Anonimizza / cancella | **6 mesi e poi cancella** |
| Config centralino, IVR, audio, template | `switchboard_config`, `switchboard_menu_item`, `switchboard_menu_action`, `switchboard_audio_file`, `switchboard_wa_template` | `sip_password`, documenti identità (`reg_*`) | vivono con lo stabilimento; segreti → secret manager; cancella alla cessazione (+ cancellazione audio da S3) | cessazione | Cancella | **ok** |

### 3.8 C8 - Chatbot (branch separato)

| Categoria di dato | Tabelle | PII / sensibile | Retention operativa consigliata | Decorrenza | Azione a scadenza | Considerazioni / scelta del proprietario |
|---|---|---|---|---|---|---|
| Storico conversazioni Q&A | `chatbot_history` | `input_text` / `output_text` (può contenere dati liberi), `created_by` | 12 mesi poi cancella (o anonimizza il testo mantenendo metriche aggregate d'uso) | data conversazione | Cancella | **ok** |
| Config chatbot per stabilimento | `chatbot_manager_config` | - | vive con lo stabilimento | cessazione | Cancella | **ok** |

### 3.9 Log applicativi e diagnostica trasversale

| Categoria | Dove | Retention | Riferimento | Considerazioni / scelta del proprietario |
|---|---|---|---|---|
| Log diagnostici applicativi (profili P1-P5) | CloudWatch Logs | 30-90 giorni in CloudWatch, poi S3 con lifecycle IA/Glacier o locale | - | **90 gg e poi S3** |
| Audit fiscale/contabile ("chi ha fatto cosa" su scontrini, movimenti) | tabella / stream dedicato | anni (allineato al fiscale), separato dai log diagnostici | - | **Ok in tabella** |
| Log accessi amministratori di sistema | stream immutabile dedicato | ≥ 6 mesi (Provv. Garante "Amministratori di sistema" 27/11/2008) | sezione normativa | **ok** |
| Log di autenticazione / sicurezza applicativa (login, lock, reset, 2FA) | DB / log strutturato | 6-12 mesi; OTP cancellato subito dopo consumo/scadenza | sezione normativa | **6 mesi in DB** |

---

## 4. Alleggerimento del DB a parità di obblighi legali

Interventi che si possono attuare presto senza toccare nulla di ciò che la legge impone di conservare,
perché l'obbligo fiscale è soddisfatto dallo snapshot dentro la prenotazione / dallo scontrino, non dal
dettaglio operativo.

| Intervento | Cosa si toglie | Quando | Beneficio | Considerazioni / scelta del proprietario |
|---|---|---|---|---|
| Purge blocchi mappa scaduti | `location_reservations`, `hold_locations` con `end_date < oggi` | giornaliera (di notte) | righe puramente operative, zero valore storico | **ok** |
| Compattazione soft-delete | righe con `deleted_at` più vecchio di 90 giorni (o del termine legale, se maggiore) | mensile | il soft-delete si accumula indefinitamente | **Ok** |
| Troncamento corpo email | `email_logs.body` → NULL dopo 90 giorni; riga intera dopo 12 mesi | mensile | `email_logs` ha già circa 61k righe di solo append | **Ok** |
| Non migrare le colonne morte | campi PII/JSON mai letti (`users.birthDate`/`gender`, colonne carta `online_reservations`, `revenue_notifications.discounts`, ecc.) | in migrazione | vedi task "campi vuoti o nulli" e `_Zcampi-deprecati.md` | **Ok** |
| Snapshot destinatari marketing | `recipientList` / `recipients` JSON | 6 mesi dall'invio | array di email congelati, nessun uso dopo l'invio | **ok** |

---

## 5. Stabilimenti non più attivi - cascata di retention

Alla cessazione del contratto di uno stabilimento (`properties` / `bathouses`) il batch applica una
cascata scaglionata, perché i dati collegati hanno obblighi diversi.

| Tempo dalla cessazione | Azione | Considerazioni / scelta del proprietario |
|---|---|---|
| Subito | disabilita gli account `users` dello stabilimento; revoca il collegamento Stripe Connect; rimuovi la scheda dalla ricerca pubblica; cancella config centralino + audio S3 | **Non rimuovere la scheda dalla ricerca** — resta pubblicata ma **ridotta alle sole informazioni essenziali** (nome, foto, posizione, descrizione base) e **con la prenotazione online disattivata**, perché serve per la popolazione del portale (chiarito 2026-09-17) |
| + 3 mesi | archivia snapshot completo della configurazione (mappa, listini, servizi, magazzino, sconti, template) su S3 o locale; cancella le tabelle di configurazione dal DB operativo | **S3** |
| + 12 mesi | cancella notifiche in-app, log email, liste marketing dello stabilimento; anonimizza le prenotazioni (pseudonimizza i clienti che non hanno documenti fiscali) | **ok** |
| + 2 anni | cancella lo snapshot di configurazione archiviato (se il cliente non è rientrato) | **ok** |
| + 10 anni | cancellazione definitiva di scontrini, movimenti, riepiloghi economici, dati del titolare (fino a qui solo su storage freddo, mai prima) | **ok** |

> Nota: prima di 10 anni si può sempre archiviare (storage freddo / S3 Glacier) ma non eliminare i dati
> fiscali e contabili dello stabilimento cessato.

---

## 6. Cheat sheet - tempi consigliati

| Dato | Online (DB caldo) | Poi | Cancellazione definitiva | Considerazioni / scelta del proprietario |
|---|---|---|---|---|
| Scontrini elettronici, movimenti, rimborsi, riepiloghi economici | ~24 mesi | Archivia (storage freddo) | 10 anni | **ok** |
| Dati del titolare stabilimento (fatturazione/contratto) | durata contratto | Archivia | 10 anni dalla cessazione | **Ok** |
| Accettazioni contrattuali | durata contratto | Anonimizza IP a 24 mesi | contratto + 10 anni | **Ok** |
| Testata prenotazione ombrellone | stagione corrente + N-1 | Archivia + anonimizza PII | 10 anni (se fiscalizzata) | **N-2** |
| Dettaglio prenotazione (JSON pesanti, snapshot mappa/prezzi) | stagione corrente + N-1 | - | Cancella a fine S + 1 S | **N-2** |
| Prenotazioni di soli servizi | fino a fine stagione | Archivia testata economica | Cancella dettaglio a inizio stagione successiva | **Anche stagione precedente** |
| Contatori di consumo inventario | stagione corrente + N-1 | aggregato mensile/stagione | Cancella dettaglio a fine S + 1 S | **Sì, aggregato mensile** |
| Assenze / sharing / blocchi mappa | N-1 / fine S | - | Cancella | **N-2** |
| Anagrafica cliente finale | fino a 24-36 mesi dall'ultima prenotazione | Anonimizza (pseudonimizza) | Cancella a 10 anni (se legata a fiscale) | **48 mesi poi archivia – cancella a 10 anni** |
| Account staff (`users`) | fino a disabilitazione | Anonimizza a +24 mesi | Cancella | **Già risposto** |
| Log invii email - corpo | 90 giorni | troncamento | - | **Già risposto** |
| Log invii email - metadati | 12 mesi | Archivia (opzionale) | Cancella | **Già risposto** |
| Notifiche in-app | 6-12 mesi | - | Cancella | **Già risposto** |
| Dati marketing (liste, campagne) | 24 mesi dall'ultimo contatto | - | Cancella (tranne lista opt-out, a vita) | **Già risposto** |
| Snapshot destinatari campagna | 6 mesi | - | Cancella | **Già risposto** |
| Lead "richiedi la tua pagina" | 12 mesi | - | Cancella | **Già risposto** |
| Log diagnostici applicativi | 30-90 giorni (CloudWatch) | S3 IA/Glacier | secondo policy | **Già risposto** |
| Log dialogo Fiskaly / webhook Switchboard | 12-24 mesi / 6-12 mesi | Anonimizza payload | Cancella | **Già risposto** |
| Log diagnostico chiamata (`switchboard_call_log`) | 30-90 giorni | - | Cancella | **Già risposto** |
| Registro chiamate ricevute | 12 mesi | Anonimizza numero | Cancella | **Già risposto** |
| Storico chatbot | 12 mesi | Anonimizza testo | Cancella | **Già risposto** |
| Log accessi amministratori di sistema | ≥ 6 mesi | stream immutabile | secondo policy interna | **Già risposto** |
| Dati carta (PAN / CVV) | mai | - | tokenizzare, eliminare le colonne | **Già risposto** |
| Configurazione stabilimento attivo | finché attivo | Archivia alla cessazione | Cancella a +2 anni dalla cessazione | **Già risposto** |
| Cataloghi master globali | permanenti | - | solo potatura voci deprecate | **Già risposto** |
| Registro richieste GDPR | permanente (prova) | - | ~10 anni | **Già risposto** |
| Backup PITR (Point In Time Recovery) / export logici | 7-35 gg / 30-90 gg | - | rotazione automatica | **Già risposto** |
