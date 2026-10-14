# Righe non copiate nel nuovo database

> Prodotto da `utility/01-migrazione-test/scripts_report_scarti.py` dal registro degli
> scarti dell'ETL (`app/discard_log.py`), con la diagnosi di `app/diagnosi_scarti.py`.
> **Una riga qui e' una riga che nel nuovo database non c'e'.**
>
> Accanto a questo report, nella stessa cartella:
> **[`_mancanti-da-sistemare.md`](_mancanti-da-sistemare.md)** — l'inventario del lavoro
> sulla migrazione: la **Parte A** e' cio' che serviva sapere da chi conosce gli
> stabilimenti o il business, la **Parte B** cio' che si chiude senza chiedere niente a
> nessuno (comprese le cose che questo report **non copre**: le spec con `aggregate_step`,
> di cui si conoscono solo i conteggi), la **Parte C** le voci M e le decisioni D;
> **[`_colonne-vuote.md`](_colonne-vuote.md)** — l'altra faccia di questo report: qui le
> righe che mancano, li' le **colonne** rimaste interamente NULL nelle righe migrate, con il
> perche' di ognuna e le domande Q1–Q14;
> **[`_utenti-senza-ruolo.md`](_utenti-senza-ruolo.md)** — i 20 account applicativi esclusi,
> con la decisione per gruppo;
> **[`_settori-da-verificare.md`](_settori-da-verificare.md)** e
> **[`Settori-da-verificare.docx`](Settori-da-verificare.docx)** — **superati** il 2026-09-30
> (i settori si creano dai nomi citati nei listini): restano come fotografia del
> disallineamento.

## In breve

**18153 righe** non migrate su **29 tabelle**. Ma il numero da solo non dice niente: quasi
tutte sono scartate **apposta**, e la parte che richiede attenzione e' una frazione piccola.

| Verdetto | Righe | Cosa vuol dire |
|---|---:|---|
| ✅ **Voluto** | 16744 | il comportamento e' quello previsto dal disegno: nessuna correzione |
| ❓ **Da decidere** | 23 | il meccanismo e' corretto, ma serve una scelta di chi conosce il business |
| ⚠️ **Da correggere** | 0 | c'e' un difetto o una perdita di dati da sanare |

## Cosa richiede un intervento

In ordine: prima cio' che va corretto, poi cio' che va deciso. Il resto del report
e' materiale di verifica, questa tabella e' l'unica da cui partire.

| | Tabella | Righe | Problema | Cosa fare |
|---|---|---:|---|---|
| ❓ | [`fiscalprint.printer_configs`](fiscalprint_printer_configs.md) | **15** | Stampanti fiscali con **modello non riconosciuto**, oppure con la porta valorizzata con un indirizzo IP anziche' con una porta. | **Da guardare**: 15 configurazioni su 58, il 26% — una quota troppo alta per essere solo sporcizia. Due possibilita': (a) sono stampanti dismesse, e allora va bene; (b) esistono modelli in uso che l'enum non prevede, e allora va esteso l'enum, non scartata la configurazione. La differenza la sa solo chi conosce il parco stampanti. |
| ❓ | [`identity.access_users`](identity_access_users.md) | **6** | Account applicativi **senza ruolo** (16) o **senza email** (4). | **Elenco completo con i dati per decidere in `analisi-pre/db-no-copied/_utenti-senza-ruolo.md`.** I 20 si dividono in tre gruppi: **11** senza stabilimento, con accesso Google/Facebook/Apple ed email personali (sei hanno anche una scheda cliente con la stessa email) - sono utenti dell'app che hanno perso la riga in `user_roles`, e il ruolo coerente e' `consumer`, il MENO privilegiato dei tredici, che ce l'hanno altri 6.004 account identici; **5** con stabilimento ma tutti `inactive`, due su stabilimenti cancellati e tre con email inventate create lo stesso giorno dallo stesso account - restano fuori; **4** senza email, di cui **tre hanno l'indirizzo nella colonna `username`** (il login legacy accettava email OR username) e vanno recuperati per decisione del committente del 2026-09-30, mentre il quarto entra con Apple e non ha nulla in nessuna delle due. |
| ❓ | [`payment.request_links`](payment_request_links.md) | **2** | Link di pagamento che non si riesce ad attribuire a uno stabilimento. | 2 righe, e vanno lasciate fuori: senza prenotazione e senza sessione Stripe non esiste alcuna via per attribuirle a uno stabilimento, e un link di pagamento non attribuibile non ha nulla da incassare. La correzione utile non e' recuperarle ma **dichiararle**: la spec dovrebbe portare `exclude_row_if: "not booking_id"`, cosi' diventano uno scarto previsto invece di un errore di inserimento, e smettono di sporcare il conteggio degli errori veri. Da guardare a parte, come segnale: due richieste di pagamento generate a due secondi di distanza senza prenotazione suggeriscono un doppio invio dall'interfaccia, quindi vale la pena chiedersi se il meccanismo che le ha prodotte sia ancora attivo. |

## Tutte le tabelle

| Tabella target | Modulo | Sorgente legacy | Righe | Motivi | Dettaglio |
|---|---|---|---:|---|---|
| `notification.marketing_suppressions` | notification | `subscribes` | **6114** | Esclusa da una regola×6114 | [scheda](notification_marketing_suppressions.md) |
| `notification.user_channel_preferences` | notification | `users` | **5829** | Esclusa da una regola×5829 | [scheda](notification_user_channel_preferences.md) |
| `payment.transaction_charges` | payment | `online_transaction_details` | **3487** | Esclusa da una regola×3487 | [scheda](payment_transaction_charges.md) |
| `catalog.map_layout_special_icons` | catalog | `configure_maps` | **1060** | Esclusa da una regola×1060 | [scheda](catalog_map_layout_special_icons.md) |
| `notification.marketing_newsletter_subscriptions` | notification | `subscribes` | **1010** | Esclusa da una regola×1010 | [scheda](notification_marketing_newsletter_subscriptions.md) |
| `fiskaly.receipt_logs` | fiskaly | `receipt_bigint_by_fiskaly_receipt_id` | **285** | Esclusa da una regola×285 | [scheda](fiskaly_receipt_logs.md) |
| `identity.customer_bathhouse_likes` | identity | `property_like_logs` | **227** | Esclusa da una regola×227 | [scheda](identity_customer_bathhouse_likes.md) |
| `catalog.properties` | identity | `users` | **40** | Esclusa da una regola×40 | [scheda](catalog_properties.md) |
| `fiscalprint.printer_configs` | fiscalprint | `printer_configs` | **15** | Esclusa da una regola×15 | [scheda](fiscalprint_printer_configs.md) |
| `catalog.property_online_allowed_periods` | catalog | `properties` | **13** | Esclusa da una regola×13 | [scheda](catalog_property_online_allowed_periods.md) |
| `catalog.pricing_discount_sectors` | catalog | `discounts` | **10** | Esclusa da una regola×10 | [scheda](catalog_pricing_discount_sectors.md) |
| `catalog.pricing_price_list_sector_prices` | catalog | `price_lists` | **9** | Esclusa da una regola×9 | [scheda](catalog_pricing_price_list_sector_prices.md) |
| `catalog.pricing_price_lists` | catalog | `price_lists` | **9** | Esclusa da una regola×9 | [scheda](catalog_pricing_price_lists.md) |
| `catalog.map_sectors_from_price_lists` | catalog | `price_lists` | **8** | Esclusa da una regola×8 | [scheda](catalog_map_sectors_from_price_lists.md) |
| `catalog.property_online_booking_settings` | catalog | `properties` | **8** | Esclusa da una regola×8 | [scheda](catalog_property_online_booking_settings.md) |
| `identity.access_users` | identity | `users` | **6** | Esclusa da una regola×6 | [scheda](identity_access_users.md) |
| `booking.service_lines` | booking | `bookings` | **3** | Esclusa da una regola×3 | [scheda](booking_service_lines.md) |
| `catalog.pricing_discounts` | catalog | `discounts` | **3** | Esclusa da una regola×3 | [scheda](catalog_pricing_discounts.md) |
| `catalog.pricing_tariff_periods` | catalog | `seasons_periods` | **3** | Esclusa da una regola×3 | [scheda](catalog_pricing_tariff_periods.md) |
| `catalog.inventory_items` | catalog | `warehouses` | **2** | Esclusa da una regola×2 | [scheda](catalog_inventory_items.md) |
| `identity.customer_customers` | identity | `customers` | **2** | Esclusa da una regola×2 | [scheda](identity_customer_customers.md) |
| `notification.email_channel_settings` | notification | `bathhouse_email_configrations` | **2** | Esclusa da una regola×2 | [scheda](notification_email_channel_settings.md) |
| `payment.request_links` | payment | `bookings_send_payment_summaries` | **2** | Esclusa da una regola×2 | [scheda](payment_request_links.md) |
| `booking.resale_settings` | booking | `configurations` | **1** | Esclusa da una regola×1 | [scheda](booking_resale_settings.md) |
| `catalog.bathhouses` | catalog | `bathouses` | **1** | Esclusa da una regola×1 | [scheda](catalog_bathhouses.md) |
| `catalog.map_layouts` | catalog | `configure_maps` | **1** | Esclusa da una regola×1 | [scheda](catalog_map_layouts.md) |
| `catalog.map_sectors` | catalog | `sectors_rows` | **1** | Esclusa da una regola×1 | [scheda](catalog_map_sectors.md) |
| `payment.ledger_entries` | booking | `reservation_public_id_by_bookings_id` | **1** | Esclusa da una regola×1 | [scheda](payment_ledger_entries.md) |
| `switchboard.switchboard_voice_softphone_devices` | switchboard | `switchboard_config` | **1** | Esclusa da una regola×1 | [scheda](switchboard_switchboard_voice_softphone_devices.md) |

## Sull'errore del database: serviva rieseguire l'inserimento?

No, e vale la pena dire perche'. Il registro cattura il messaggio di PostgreSQL **nel
momento in cui la riga viene rifiutata**, completo della clausola `DETAIL: La riga in errore
contiene (...)` con i valori effettivi. Un secondo tentativo a posteriori darebbe meno:
andrebbe ricostruita la riga di destinazione a partire dalla sola riga sorgente, rifacendo
tutte le trasformazioni e le risoluzioni di riferimento — e un riferimento che oggi si
risolve potrebbe non essersi risolto allora, o viceversa. Si otterrebbe *un* errore, non
*quell'* errore.

Dove il messaggio descrive il sintomo e non la causa (un `NOT NULL` violato e' quasi sempre
un riferimento che non si e' risolto a monte), la diagnosi risale la catena e riporta i
valori che il lookup non ha trovato, presi dal log dello stesso run.

## Copertura di questo report — leggere prima dei numeri

Il registro intercetta i cinque punti in cui **l'orchestrator** scarta una riga. Non copre
le spec con `aggregate_step`, che eseguono `INSERT` propri dentro `custom_transforms/` e non
passano da li'.

| | Spec | Righe scartate |
|---|---:|---|
| Percorso standard dell'orchestrator | ~74 | **registrate una per una**, con la riga sorgente completa |
| Spec con `aggregate_step` | 55 | **solo conteggi**, nessun dettaglio per riga |

La prova che la differenza e' reale: `booking.service_lines` ha perso **10.611 righe** in un
run intermedio senza comparire in questo registro, perche' inserisce per conto proprio.
Quelle righe si vedevano solo nel log applicativo.

**Il contatore globale "scartate" dell'ETL non e' il numero di righe perse.** Nell'ultimo run
dichiara 4.636.640, ma righe lette (6.718.354) meno migrate (5.758.809) fa 959.545: quel
contatore somma anche gli *elementi* valutati dentro gli array esplosi, e piu' spec leggono la
stessa tabella sorgente. Va letto come "valutazioni che non hanno prodotto una riga", mai come
"dati persi".

---

I dati grezzi completi di ogni tabella, con tutte le colonne sorgente e mai troncati, sono in
[`dati/`](dati/) — un file JSONL per tabella, una riga JSON per scarto.
