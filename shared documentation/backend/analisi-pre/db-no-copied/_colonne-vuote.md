# Colonne interamente NULL dopo la migrazione

> Rilevazione del 2026-10-03 su PostgreSQL 17, `localhost:5432/summer_booking_dev`
> (la connessione usata da `backend/Boot/appsettings.Development.json`), **dopo la
> migrazione completa a zero errori** (6.163.412 righe migrate secondo l'ultimo report).
> Rigenerabile con `utility/01-migrazione-test/scripts_colonne_vuote_documento.py`.
>
> Il censimento e il suo **perché** stanno entrambi qui: la Parte 1 dice quali
> colonne sono vuote, la Parte 2 perché lo sono e se sia un errore — oggi su 166
> colonne e 26 tabelle vuote i difetti aperti sono **zero**.
>
> **Questo documento ha quattro parti** (era `docs/_temp3.md`, che il 2026-10-03 aveva
> assorbito `docs/_temp2.md`; spostato qui il 2026-10-06):
>
> | | Parte | Che domanda risponde | Chi la scrive |
> |---|---|---|---|
> | 1 | Rilevazione | **quali** colonne sono vuote, tabella per tabella | `scripts_colonne_vuote_documento.py` |
> | 2 | Perché ogni campo è vuoto | **perché** lo è, e **se è un errore** | `scripts_perche_vuote_documento.py` |
> | 3 | Domande e risposte | cosa serviva sapere, e cosa è stato deciso | a mano |
> | 4 | Come si è arrivati qui | il confronto col primo censimento e ciò che i fatti hanno smentito | a mano |
>
> Le Parti 1 e 2 si **rigenerano**: ognuno dei due script riscrive solo la propria e
> preserva il resto. Le Parti 3 e 4 sono scritte a mano e sopravvivono alle
> rigenerazioni.
>
> **Il registro del lavoro** che ne è derivato — le diciannove voci **M** e le nove
> decisioni **D** — non è qui: sta nella **Parte C** di
> [`_mancanti-da-sistemare.md`](_mancanti-da-sistemare.md),
> che è l'inventario del lavoro sulla migrazione.

# Parte 1 — Rilevazione: quali colonne sono vuote

## Metodo

Per ogni tabella base degli schemi applicativi è stato calcolato `count(<colonna>)`
su tutte le colonne nullable: se il conteggio dei non-NULL è `0` la colonna è
riportata qui sotto.

Criteri di esclusione:

- colonne `NOT NULL` (per definizione non possono essere tutte NULL);
- colonne tecniche di audit temporale: `created_at`, `updated_at`, `deleted_at`;
- tabelle **vuote** (0 righe): la condizione sarebbe vera per tutte le loro
  colonne senza portare informazione. Sono elencate a parte in appendice.

Le tabelle partizionate sono interrogate dalla **tabella padre**, quindi il
conteggio include tutte le partizioni: una colonna può essere tutta NULL in una
partizione e piena in un'altra, e contarla per foglia non direbbe niente sulla tabella.

> **Attenzione a confrontare questo numero con quello di `scripts_colonne_vuote.py`**,
> la guardia di regressione che gira a ogni migrazione: quella **non** esclude
> `deleted_at`, perché le serve rilevare qualunque variazione rispetto all'atteso, non
> interpretarla. Sullo stesso database dà **302** colonne contro le **166** di qui,
> e **118 di quella differenza sono proprio le `deleted_at`**; il resto si riduce
> contando le tabelle partizionate dal padre. Non è una discordanza: sono due domande
> diverse, e il numero senza il criterio non significa nulla.

Perché il padre e non le foglie, con l'esempio che lo rende evidente:
`booking.resource_consumptions` ha **due** colonne interamente NULL se interrogata dal
padre (`time_slot_id`, `held_until`); interrogando le sette partizioni una per una ne
comparirebbero altre — `booking_channel` e `booking_number` in `y2021` e `y2023`,
`slot_start`/`slot_end` in `y2027` — soltanto perché quelle partizioni contengono
esclusivamente righe che ne sono prive. Sarebbero lacune inesistenti a livello di tabella.

## Sintesi

| Metrica | Valore |
|---|---|
| Tabelle base totali (schemi applicativi) | 162 |
| di cui tecniche `__ef_migrations_history` | 11 |
| Tabelle popolate (>0 righe) | 136 |
| Tabelle vuote (escluse dall'analisi) | 26 |
| **Tabelle popolate con almeno una colonna tutta NULL** | **65** |
| **Colonne interamente NULL** | **166** |

Distribuzione per schema:

| Schema | Tabelle | Colonne |
|---|---|---|
| `notification` | 15 | 51 |
| `switchboard` | 9 | 34 |
| `booking` | 10 | 21 |
| `payment` | 7 | 18 |
| `catalog` | 10 | 15 |
| `fiskaly` | 6 | 11 |
| `chatbot` | 2 | 6 |
| `fiscalprint` | 2 | 5 |
| `identity` | 3 | 3 |
| `infra` | 1 | 2 |
| **Totale** | **65** | **166** |

## Dettaglio per tabella

## Schema `booking` — 10 tabelle, 21 colonne

### `booking.discounts` (1.795 righe)

| Colonna | Tipo |
|---|---|
| `booking_line_id` | `bigint` |

### `booking.groups` (69 righe)

| Colonna | Tipo |
|---|---|
| `created_by_user_id` | `uuid` |

### `booking.guests` (133 righe)

| Colonna | Tipo |
|---|---|
| `booking_line_id` | `bigint` |
| `checked_in_at` | `timestamp with time zone` |

### `booking.lines` (310.802 righe)

| Colonna | Tipo |
|---|---|
| `sector_id` | `uuid` |
| `sector_label_snapshot` | `character varying(100)` |
| `price_list_id` | `uuid` |
| `area_seat_count` | `smallint` |

### `booking.resale_credit_ledger` (1.547 righe)

| Colonna | Tipo |
|---|---|
| `note` | `character varying(255)` |

### `booking.resale_offers` (12.900 righe)

| Colonna | Tipo |
|---|---|
| `remaining_period` | `booking.day_period` |

### `booking.resource_consumptions` (3.809.485 righe)

| Colonna | Tipo |
|---|---|
| `time_slot_id` | `uuid` |
| `held_until` | `timestamp with time zone` |

### `booking.service_lines` (10.611 righe)

| Colonna | Tipo |
|---|---|
| `booking_line_id` | `bigint` |

### `booking.service_time_slots` (559 righe)

| Colonna | Tipo |
|---|---|
| `time_slot_id` | `uuid` |
| `slot_label_snapshot` | `character varying(40)` |
| `slot_price` | `numeric(12,2)` |

### `booking.spot_holds` (15.311 righe)

| Colonna | Tipo |
|---|---|
| `held_until` | `timestamp with time zone` |
| `reason_note` | `character varying(255)` |
| `released_at` | `timestamp with time zone` |
| `released_by_user_id` | `uuid` |
| `converted_reservation_id` | `bigint` |

## Schema `catalog` — 10 tabelle, 15 colonne

### `catalog.agreement_price_list_items` (3.550 righe)

| Colonna | Tipo |
|---|---|
| `included_service_id` | `integer` |

### `catalog.agreement_price_list_sector_prices` (32 righe)

| Colonna | Tipo |
|---|---|
| `seasonal_price` | `numeric(12,2)` |

### `catalog.map_spots` (26.027 righe)

| Colonna | Tipo |
|---|---|
| `created_by_user_id` | `uuid` |

### `catalog.media_assets` (11.040 righe)

| Colonna | Tipo |
|---|---|
| `checksum` | `bytea` |
| `byte_size` | `integer` |
| `content_type` | `character varying(100)` |

### `catalog.pricing_price_list_date_prices` (1.414 righe)

| Colonna | Tipo |
|---|---|
| `period_price_id` | `bigint` |
| `price` | `numeric(12,2)` |

### `catalog.pricing_price_list_items` (11.468 righe)

| Colonna | Tipo |
|---|---|
| `included_service_id` | `integer` |

### `catalog.pricing_suggested_prices` (49.366 righe)

| Colonna | Tipo |
|---|---|
| `applied_by_user_id` | `uuid` |

### `catalog.properties` (10.852 righe)

| Colonna | Tipo |
|---|---|
| `contract_document_id` | `uuid` |

### `catalog.service_sale_item_time_slots` (1.661 righe)

| Colonna | Tipo |
|---|---|
| `slot_start` | `time without time zone` |
| `slot_end` | `time without time zone` |

### `catalog.service_sale_items` (616 righe)

| Colonna | Tipo |
|---|---|
| `icon_id` | `integer` |
| `created_by_user_id` | `uuid` |

## Schema `chatbot` — 2 tabelle, 6 colonne

### `chatbot.conversation_turns` (26 righe)

| Colonna | Tipo |
|---|---|
| `actor_session_id` | `character varying(64)` |
| `llm_provider_id` | `bigint` |
| `llm_model_snapshot` | `character varying(64)` |
| `response_language` | `character varying(10)` |

### `chatbot.manager_configs` (1 righe)

| Colonna | Tipo |
|---|---|
| `llm_provider_id` | `bigint` |
| `created_by_user_id` | `uuid` |

## Schema `fiscalprint` — 2 tabelle, 5 colonne

### `fiscalprint.printer_configs` (43 righe)

| Colonna | Tipo |
|---|---|
| `updated_by_user_id` | `uuid` |

### `fiscalprint.receipts` (4.487 righe)

| Colonna | Tipo |
|---|---|
| `reservation_public_id` | `uuid` |
| `booking_number` | `character varying(32)` |
| `receipt_number` | `integer` |
| `created_by_user_id` | `uuid` |

## Schema `fiskaly` — 6 tabelle, 11 colonne

### `fiskaly.accounts` (7 righe)

| Colonna | Tipo |
|---|---|
| `updated_by_user_id` | `uuid` |

### `fiskaly.cash_registers` (5 righe)

| Colonna | Tipo |
|---|---|
| `updated_by_user_id` | `uuid` |

### `fiskaly.media_assets` (2.546 righe)

| Colonna | Tipo |
|---|---|
| `checksum` | `bytea` |
| `byte_size` | `integer` |

### `fiskaly.receipt_lines` (2.852 righe)

| Colonna | Tipo |
|---|---|
| `source_line_public_id` | `uuid` |

### `fiskaly.receipt_payments` (2.549 righe)

| Colonna | Tipo |
|---|---|
| `voucher_kind` | `fiskaly.voucher_kind` |
| `outstanding_concept` | `fiskaly.outstanding_concept` |
| `ledger_entry_public_id` | `uuid` |

### `fiskaly.receipts` (2.549 righe)

| Colonna | Tipo |
|---|---|
| `fiskaly_state_raw` | `character varying(40)` |
| `cancelled_by_user_id` | `uuid` |
| `cancellation_reason` | `character varying(255)` |

## Schema `identity` — 3 tabelle, 3 colonne

### `identity.access_users` (6.674 righe)

| Colonna | Tipo |
|---|---|
| `password_changed_at` | `timestamp with time zone` |

### `identity.account_license_plan_translations` (4 righe)

| Colonna | Tipo |
|---|---|
| `description` | `text` |

### `identity.customer_customers` (224.524 righe)

| Colonna | Tipo |
|---|---|
| `blacklist_reason` | `character varying(255)` |

## Schema `infra` — 1 tabella, 2 colonne

### `infra.integration_api_clients` (2 righe)

| Colonna | Tipo |
|---|---|
| `expires_at` | `timestamp with time zone` |
| `rotated_at` | `timestamp with time zone` |

## Schema `notification` — 15 tabelle, 51 colonne

### `notification.deliveries` (200.884 righe)

| Colonna | Tipo |
|---|---|
| `subject_public_id` | `uuid` |
| `recipient_customer_public_id` | `uuid` |
| `recipient_user_public_id` | `uuid` |
| `recipient_name_snapshot` | `character varying(160)` |
| `template_id` | `bigint` |
| `next_attempt_at` | `timestamp with time zone` |
| `provider_message_id` | `character varying(255)` |
| `idempotency_key` | `character varying(200)` |
| `failure_reason` | `text` |
| `skip_reason` | `notification.skip_reason` |
| `delivered_at` | `timestamp with time zone` |
| `failed_at` | `timestamp with time zone` |

### `notification.email_channel_settings` (20 righe)

| Colonna | Tipo |
|---|---|
| `last_verification_at` | `timestamp with time zone` |
| `last_verification_error` | `text` |

### `notification.establishment_claim_requests` (13 righe)

| Colonna | Tipo |
|---|---|
| `bathhouse_public_id` | `uuid` |
| `bathhouse_name_snapshot` | `character varying(200)` |
| `handled_by_user_id` | `uuid` |
| `handled_at` | `timestamp with time zone` |
| `resolution_note` | `character varying(500)` |
| `client_ip_snapshot` | `inet` |
| `user_agent_snapshot` | `character varying(255)` |

### `notification.inapp_messages` (317.503 righe)

| Colonna | Tipo |
|---|---|
| `subject_public_id` | `uuid` |
| `created_by_user_id` | `uuid` |

### `notification.marketing_automations` (221 righe)

| Colonna | Tipo |
|---|---|
| `last_run_at` | `timestamp with time zone` |
| `created_by_user_id` | `uuid` |

### `notification.marketing_campaign_recipients` (10.741 righe)

| Colonna | Tipo |
|---|---|
| `delivery_public_id` | `uuid` |
| `skip_reason` | `notification.recipient_skip_reason` |

### `notification.marketing_contact_lists` (125 righe)

| Colonna | Tipo |
|---|---|
| `created_by_user_id` | `uuid` |

### `notification.marketing_contacts` (7.181 righe)

| Colonna | Tipo |
|---|---|
| `consent_at` | `timestamp with time zone` |
| `consent_notice_version` | `character varying(40)` |
| `last_contacted_at` | `timestamp with time zone` |

### `notification.marketing_email_campaigns` (25 righe)

| Colonna | Tipo |
|---|---|
| `name` | `character varying(160)` |
| `created_by_user_id` | `uuid` |

### `notification.marketing_events` (26 righe)

| Colonna | Tipo |
|---|---|
| `repeat_unit` | `notification.event_repeat_unit` |
| `contact_list_id` | `bigint` |
| `created_by_user_id` | `uuid` |

### `notification.marketing_newsletter_subscriptions` (5.538 righe)

| Colonna | Tipo |
|---|---|
| `confirmation_token` | `character varying(64)` |
| `confirmed_at` | `timestamp with time zone` |
| `consent_notice_version` | `character varying(40)` |
| `customer_public_id` | `uuid` |

### `notification.marketing_suppressions` (434 righe)

| Colonna | Tipo |
|---|---|
| `property_id` | `uuid` |
| `source_delivery_public_id` | `uuid` |
| `contact_public_id` | `uuid` |
| `note` | `character varying(500)` |

### `notification.media_assets` (30 righe)

| Colonna | Tipo |
|---|---|
| `checksum` | `bytea` |
| `byte_size` | `integer` |
| `content_type` | `character varying(100)` |
| `uploaded_by_user_id` | `uuid` |

### `notification.templates` (246 righe)

| Colonna | Tipo |
|---|---|
| `created_by_user_id` | `uuid` |

### `notification.user_channel_preferences` (851 righe)

| Colonna | Tipo |
|---|---|
| `quiet_hours_start` | `time without time zone` |
| `quiet_hours_end` | `time without time zone` |

## Schema `payment` — 7 tabelle, 18 colonne

### `payment.gateway_connected_accounts` (44 righe)

| Colonna | Tipo |
|---|---|
| `revoked_at` | `timestamp with time zone` |

### `payment.ledger_entries` (2.540 righe)

| Colonna | Tipo |
|---|---|
| `agreement_line_public_id` | `uuid` |
| `payment_mode` | `payment.payment_mode` |
| `source_ref_id` | `bigint` |
| `customer_public_id` | `uuid` |
| `recorded_by_user_id` | `uuid` |

### `payment.ledger_entry_lines` (2.540 righe)

| Colonna | Tipo |
|---|---|
| `reservation_line_public_id` | `uuid` |

### `payment.request_links` (1.390 righe)

| Colonna | Tipo |
|---|---|
| `charge_id` | `bigint` |
| `created_by_user_id` | `uuid` |

### `payment.transaction_charges` (36.613 righe)

| Colonna | Tipo |
|---|---|
| `stripe_charge_id` | `character varying(255)` |
| `customer_public_id` | `uuid` |
| `captured_at` | `timestamp with time zone` |
| `canceled_at` | `timestamp with time zone` |

### `payment.wallet_transaction_items` (34 righe)

| Colonna | Tipo |
|---|---|
| `sale_item_public_id` | `uuid` |
| `variants_description` | `character varying(255)` |

### `payment.wallet_transactions` (610 righe)

| Colonna | Tipo |
|---|---|
| `resale_offer_ref` | `uuid` |
| `reverses_transaction_id` | `bigint` |
| `ledger_entry_ref` | `uuid` |

## Schema `switchboard` — 9 tabelle, 34 colonne

### `switchboard.switchboard_configs` (1 righe)

| Colonna | Tipo |
|---|---|
| `sip_client_notes` | `text` |
| `whatsapp_phone_number_id` | `character varying(64)` |
| `whatsapp_phone_e164` | `character varying(20)` |
| `forward_custom_caller_id_e164` | `character varying(20)` |
| `forward_caller_id_name` | `character varying(100)` |

### `switchboard.switchboard_media_assets` (5 righe)

| Colonna | Tipo |
|---|---|
| `duration_ms` | `integer` |
| `checksum` | `bytea` |

### `switchboard.switchboard_messaging_wa_event_logs` (11 righe)

| Colonna | Tipo |
|---|---|
| `config_id` | `bigint` |
| `property_id` | `uuid` |
| `contact_name` | `character varying(100)` |
| `conversation_id` | `character varying(100)` |
| `body_text` | `text` |
| `body_payload` | `jsonb` |

### `switchboard.switchboard_messaging_wa_templates` (3 righe)

| Colonna | Tipo |
|---|---|
| `meta_rejection_reason` | `text` |

### `switchboard.switchboard_voice_call_diagnostics` (7.355 righe)

| Colonna | Tipo |
|---|---|
| `call_record_id` | `bigint` |

### `switchboard.switchboard_voice_call_records` (214 righe)

| Colonna | Tipo |
|---|---|
| `forward_result` | `switchboard.forward_result` |
| `final_menu_item_public_id` | `uuid` |
| `telnyx_call_session_id` | `character varying(150)` |
| `telnyx_call_control_id` | `character varying(150)` |
| `telnyx_call_leg_id` | `character varying(150)` |
| `answered_at` | `timestamp with time zone` |
| `ended_at` | `timestamp with time zone` |
| `duration_seconds` | `integer` |

### `switchboard.switchboard_voice_menu_actions` (1 righe)

| Colonna | Tipo |
|---|---|
| `wa_template_id` | `bigint` |
| `wa_template_name_snapshot` | `character varying(512)` |
| `wa_recipient_phone_e164` | `character varying(20)` |

### `switchboard.switchboard_voice_number_registrations` (1 righe)

| Colonna | Tipo |
|---|---|
| `reg_identity_doc_number` | `bytea` |
| `reg_date_of_birth` | `bytea` |
| `reg_place_of_birth` | `bytea` |
| `reg_personal_tax_code` | `bytea` |
| `released_at` | `timestamp with time zone` |

### `switchboard.switchboard_voice_softphone_devices` (1 righe)

| Colonna | Tipo |
|---|---|
| `registered_by_user_id` | `uuid` |
| `device_label` | `character varying(120)` |
| `last_error` | `text` |

## Appendice — tabelle vuote (0 righe), escluse dall'analisi

| Schema | Tabella |
|---|---|
| `booking` | `resource_availability_daily` |
| `catalog` | `agreement_price_list_date_prices` |
| `catalog` | `config_snapshots` |
| `catalog` | `pricing_revenue_discounted_spots` |
| `catalog` | `property_online_allowed_locations` |
| `catalog` | `search_bathhouses` |
| `chatbot` | `llm_providers` |
| `fiscalprint` | `serial_numbers` |
| `identity` | `access_refresh_tokens` |
| `identity` | `access_user_auth_challenges` |
| `identity` | `account_contract_acceptances` |
| `identity` | `account_contract_documents` |
| `identity` | `account_subscription_periods` |
| `identity` | `account_subscriptions` |
| `notification` | `inapp_message_reads` |
| `notification` | `marketing_event_reminders` |
| `notification` | `push_device_tokens` |
| `payment` | `customer_gateway_profiles` |
| `payment` | `pos_charge_items` |
| `payment` | `pos_charges` |
| `payment` | `transaction_refunds` |
| `payment` | `webhook_events` |
| `privacy` | `privacy_compliance_audit_logs` |
| `privacy` | `privacy_data_subject_request_module_outcomes` |
| `privacy` | `privacy_data_subject_requests` |
| `privacy` | `privacy_legal_holds` |

# Parte 2 — Perché ogni campo è vuoto, e se è un errore

> Scritta il 2026-10-02 incrociando ogni colonna con la sua mapping spec via
> `utility/01-migrazione-test/scripts_perche_vuote.py`, e verificando sul legacy ogni caso in
> cui la spec dichiarava una fonte. Rigenerabile con `scripts_perche_vuote_documento.py`.

## Premessa: «zero errori» non significa «tutti i campi pieni»

Il contatore `in errore: 0` dell'ETL misura una cosa precisa: **nessuna riga è stata
rifiutata**. Non dice niente su quante colonne di quelle righe siano valorizzate, e non
potrebbe: una colonna nullable che resta vuota non produce alcun errore. Le due misure sono
indipendenti, ed è il motivo per cui questa parte esiste.

Una colonna può finire vuota per cinque ragioni, e distinguerle è tutto:

| | Meccanismo | È un errore? |
|---|---|---|
| 1 | **Il campo non esiste nel legacy**; la spec lo dichiara segnaposto | No: lo popolerà il nuovo backend |
| 2 | **Il campo esiste ma è vuoto su tutte le righe sorgente** | No — e l'ho misurato caso per caso |
| 3 | **La colonna è del modello nuovo** e nessuna spec la tocca | No |
| 4 | **Il dato c'è e non è arrivato** | **Sì** |
| 5 | **L'ETL potrebbe fare di più, ma serve una decisione** | Dipende da una risposta |

**Esito**: su **166 colonne** e **26 tabelle vuote**, sono difetti da correggere **0 colonne**
e **0 tabelle**; **nessuna dipende più da una decisione aperta** — tutte le domande di merito hanno ricevuto risposta (vedi la Parte 3, §3.0); tutto il resto è corretto.

### Nessun difetto aperto

Le due voci che lo erano — i **1.823 riferimenti all'utente creatore** buttati via sulle
tabelle esplose di `catalog` e `catalog.map_layout_special_icons` **vuota con 691 righe
disponibili** — sono state corrette e verificate nella corsa del 2026-10-02:

| | Prima | Dopo |
|---|---|---|
| `created_by_user_id` su `inventory_items`, `map_sectors`, `pricing_discounts` | 0 valorizzate | **1.044 + 635 + 144 = 1.823**, e i relink risolti passano da 3.645 a **5.468** |
| `catalog.map_layout_special_icons` | vuota | **690 righe** (570 `free` + 120 `occupied`); l'unica mancante è la mappa il cui layout non è migrato, scartata con motivo |

Nella stessa corsa sono state applicate anche le due voci nate dalle risposte del
committente: **`duration_minutes`** valorizzata su **1.661 righe su 1.661** (undici durate
× 151, zero etichette non riconosciute) e **`last_activity_at`** su **224.524 su 224.524**
(106.912 dalle prenotazioni, 117.612 dalla data di cutover).

## 2.1 — Le 26 tabelle vuote

| Tabella | Perché | Errore? | Domanda |
|---|---|---|---|
| `booking.resource_availability_daily` | **Read model** della disponibilita' giornaliera: lo calcola il backend, non esiste nel legacy e nessuna spec la tocca. | no |  |
| `catalog.agreement_price_list_date_prices` | Sorgente `hotel_price_lists` (388 righe) ma **nessun elemento in formato data singola**: verificato (D-63) che tutti i 307.517 elementi di `edit_price_data` usano la forma a griglia per numero di giorni, che va in `pricing_price_list_stay_prices`. `holidayDates` e' sempre vuoto. | no |  |
| `catalog.config_snapshots` | Spec dichiarata **`pending`**, e la migrazione non c'entra: **li costruisce il job** notturno di manutenzione (`20-jobs.md` §1.4, rete di sicurezza per gli snapshot di configurazione). Risposta del 2026-10-02. Resta da sciogliere se la **prima** generazione sia quella del job o la voce 1.12 di `utility/TODO.md`. | no | Q8 |
| `catalog.pricing_revenue_discounted_spots` | Sorgente `revenue_notifications` 11 righe: 10 hanno la lista sconti vuota, **1 ha 4 codici cella** (da `R12C1` a `R12C4`) che non esistono nella mappa della property 228 - quella mappa ha le righe 2-9, 11 e 13, la **12 no**. Riferimento pendente nel legacy, non difetto dell'ETL: le celle sono state cancellate dopo che le impostazioni erano state salvate. | no |  |
| `catalog.property_online_allowed_locations` | Verificato: **nessuna property** ha `onlineBookingOptions.locationOptionObj.locations` valorizzato. Spec scritta per completezza, 0 righe attese. | no |  |
| `catalog.search_bathhouses` | Read model della ricerca pubblica: la sua scheda prevede rebuild **sincrono su evento** + **job notturno** di ricostruzione totale, e la tabella e' scritta **solo** dal processo di rebuild, mai dal dominio. Vuota fino alla prima esecuzione, per costruzione. | no | Q8 |
| `chatbot.llm_providers` | Tabella di configurazione del nuovo sistema: il legacy aveva una sola variabile globale `CHATBOT_MANAGER_LLM`, nessun catalogo di provider. | no |  |
| `fiscalprint.serial_numbers` | Nuova: il legacy non teneva un registro dei numeri di serie delle stampanti. | no |  |
| `identity.access_refresh_tokens` | Token di sessione: si creano a runtime al primo login. Migrarli significherebbe migrare sessioni aperte. | no |  |
| `identity.access_user_auth_challenges` | Come sopra: sfide di autenticazione effimere, create a runtime. | no |  |
| `identity.account_contract_acceptances` | Nessuna spec: nel legacy `contract_documents` ha **0 righe**, quindi non esistono accettazioni da migrare. | no |  |
| `identity.account_contract_documents` | La tabella legacy omonima ha **0 righe**. | no |  |
| `identity.account_subscription_periods` | Come sopra: figlia di `account_subscriptions`, che non ha fonte (risposta del 2026-10-02). | no | Q10 |
| `identity.account_subscriptions` | **Nessuna spec e nessuna fonte legacy**: nel dump non esiste alcuna tabella di abbonamenti per stabilimento (`subscribes` sono le iscrizioni alla newsletter, `licenses` sono 2 righe di anagrafica tipo/durata) e `properties` non ha colonne di scadenza. **Conseguenza da non perdere di vista**: la retention concordata col titolare fa decorrere la cessazione da `account_subscriptions.ends_at` + 24 mesi - per i dati migrati quel dato non esiste, quindi la regola non e' calcolabile sullo storico. **Risposta del 2026-10-02**: nel legacy gli abbonamenti erano gestiti fuori dal portale e attivati a mano; nel nuovo sistema si gestiranno dal portale. Niente da migrare, il modello resta. | no | Q10 |
| `notification.inapp_message_reads` | Ricevute di lettura: nuove, il legacy non tracciava chi aveva letto un messaggio. | no |  |
| `notification.marketing_event_reminders` | Sorgente `marketing_event_notification`: **0 righe** nel legacy. | no |  |
| `notification.push_device_tokens` | Verificato: `users.deviceTokens` e' vuoto su **tutte le 6.680 righe** (nessun token mai registrato). | no |  |
| `payment.customer_gateway_profiles` | Nessun `users.stripeCustomerId` nella forma `cus_...`: **zero**. L'unico valore presente e' `acct_1J8QLw2RS3MLI6rZ`, cioe' l'id di un *connected account* finito per errore nella colonna del cliente, correttamente scartato dal filtro. | no |  |
| `payment.pos_charge_items` | Sorgente `teamsystem_expenses`: **0 righe** nel legacy. | no |  |
| `payment.pos_charges` | Sorgente `teamsystem_expenses`: **0 righe** nel legacy. | no |  |
| `payment.transaction_refunds` | Sorgente `refund_transaction_details`: **0 righe** nel legacy (nessun rimborso registrato). | no |  |
| `payment.webhook_events` | Tabella introdotta dalle ottimizzazioni DB (`docs/05`) per l'idempotenza dei webhook: si popola a runtime, non ha nulla da migrare. | no |  |
| `privacy.privacy_compliance_audit_logs` | Dominio **nuovo**: il legacy non aveva alcuna gestione privacy strutturata. | no |  |
| `privacy.privacy_data_subject_request_module_outcomes` | Dominio nuovo, come sopra. | no |  |
| `privacy.privacy_data_subject_requests` | Dominio nuovo, come sopra. | no |  |
| `privacy.privacy_legal_holds` | Dominio nuovo, come sopra. | no |  |

## 2.2 — Le 166 colonne interamente NULL

Il «perché» viene dal commento della mapping spec dove c'è: è la motivazione di chi ha scritto
la mappatura, non una ricostruzione a posteriori.

### `booking` — 21 colonne

| Tabella | Colonna | Righe | Perché | Errore? | Domanda |
|---|---|---|---|---|---|
| `discounts` | `booking_line_id` | 1.795 | Nel legacy lo sconto si applicava alla **prenotazione**, non alla singola postazione: l'informazione "quale riga ha ricevuto lo sconto" non esiste nei dati di partenza. | no |  |
| `groups` | `created_by_user_id` | 69 | non tracciato nel legacy (nessuna colonna "creato da"): sempre NULL per le righe migrate, come… | no | Q1 |
| `guests` | `booking_line_id` | 133 | Come sopra: l'ospite non era mai legato a una postazione. | no |  |
| `guests` | `checked_in_at` | 133 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `lines` | `sector_id` | 310.802 | Deriva da `selectedPackageDetails[].currentRow`, **verificato mai valorizzato** (0 su 5.000 campionati): dato assente nel legacy, non un limite dell'ETL. | no |  |
| `lines` | `sector_label_snapshot` | 310.802 | E' lo snapshot del settore che `sector_id` non ha: senza il settore non c'e' etichetta. | no |  |
| `lines` | `price_list_id` | 310.802 | Deriva da `selectedPackageDetails[].packageId`, stessa verifica: 0 su 5.000. | no |  |
| `lines` | `area_seat_count` | 310.802 | Numero di posti di un'area: il legacy non lo registrava per riga di prenotazione. | no |  |
| `resale_credit_ledger` | `note` | 1.547 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `resale_offers` | `remaining_period` | 12.900 | `remaining_period` lasciato SEMPRE NULL: la logica "FullDay ceduto, Morning ripreso -> resta… | no |  |
| `resource_consumptions` | `time_slot_id` | 3.809.485 | `time_slot_id` lasciato SEMPRE NULL: verificato altrove… | no |  |
| `resource_consumptions` | `held_until` | 3.809.485 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `service_lines` | `booking_line_id` | 10.611 | Come sopra: il servizio non era mai legato a una postazione. | no |  |
| `service_time_slots` | `time_slot_id` | 559 | `time_slot_id` resta sempre NULL per ora… | no |  |
| `service_time_slots` | `slot_label_snapshot` | 559 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `service_time_slots` | `slot_price` | 559 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `spot_holds` | `held_until` | 15.311 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `spot_holds` | `reason_note` | 15.311 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `spot_holds` | `released_at` | 15.311 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `spot_holds` | `released_by_user_id` | 15.311 | Il legacy non registrava l'autore dell'operazione: nessuna colonna «creato/modificato da» nella tabella sorgente. | no | Q1 |
| `spot_holds` | `converted_reservation_id` | 15.311 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |

### `catalog` — 15 colonne

| Tabella | Colonna | Righe | Perché | Errore? | Domanda |
|---|---|---|---|---|---|
| `agreement_price_list_items` | `included_service_id` | 3.550 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `agreement_price_list_sector_prices` | `seasonal_price` | 32 | La spec legge `seasonal` dagli **elementi** di `mainPrices`, che nei dati hanno solo `name`, `value`, `halfday`: il campo non esiste in nessuno dei 32 elementi. | no |  |
| `map_spots` | `created_by_user_id` | 26.027 | Il legacy non registrava l'autore dell'operazione: nessuna colonna «creato/modificato da» nella tabella sorgente. | no | Q1 |
| `media_assets` | `checksum` | 11.040 | Si ottiene solo **scaricando il file da S3**: lo fa `utility/02-migrazione-file`, non l'ETL (decisione D9). | no | Q2 |
| `media_assets` | `byte_size` | 11.040 | Come `checksum`: richiede il file, non il database. | no | Q2 |
| `media_assets` | `content_type` | 11.040 | Come `checksum`: richiede il file, non il database. | no | Q2 |
| `pricing_price_list_date_prices` | `period_price_id` | 1.414 | period_price_id=NULL, SOLO se `price_lists.allowPublicHolidays` e' vero (nota scheda). | no |  |
| `pricing_price_list_date_prices` | `price` | 1.414 | period_price_id=NULL, SOLO se `price_lists.allowPublicHolidays` e' vero (nota scheda). | no |  |
| `pricing_price_list_items` | `included_service_id` | 11.468 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `pricing_suggested_prices` | `applied_by_user_id` | 49.366 | Il legacy non registrava l'autore dell'operazione: nessuna colonna «creato/modificato da» nella tabella sorgente. | no | Q1 |
| `properties` | `contract_document_id` | 10.852 | `properties.contractDocumentsId` e' vuota su tutte le 10.852 righe, e `account_contract_documents` ha 0 righe. | no |  |
| `service_sale_item_time_slots` | `slot_start` | 1.661 | **Non esistono fasce orarie in questi dati**: le 11 etichette distinte sono **durate** (`10 min` … `6 hrs`), non intervalli, e la durata e' migrata in `label` (NOT NULL, valorizzata su tutte le 1.661 righe). Queste due colonne sono orari assoluti e il legacy non ne ha, quindi restano NULL per costruzione. **Q13 chiusa il 2026-10-02**: si aggiunge `duration_minutes`, popolata dalle 11 etichette con conversione esatta. | no | Q13 |
| `service_sale_item_time_slots` | `slot_end` | 1.661 | Come `slot_start`: il legacy registra una durata, non un orario. | no | Q13 |
| `service_sale_items` | `icon_id` | 616 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `service_sale_items` | `created_by_user_id` | 616 | Il legacy non registrava l'autore dell'operazione: nessuna colonna «creato/modificato da» nella tabella sorgente. | no | Q1 |

### `chatbot` — 6 colonne

| Tabella | Colonna | Righe | Perché | Errore? | Domanda |
|---|---|---|---|---|---|
| `conversation_turns` | `actor_session_id` | 26 | nuovo/preparatorio, nessun equivalente legacy: sempre NULL per lo storico migrato (canale… | no |  |
| `conversation_turns` | `llm_provider_id` | 26 | non ricostruibile retroattivamente (il legacy non persiste quale variante ha risposto). | no |  |
| `conversation_turns` | `llm_model_snapshot` | 26 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `conversation_turns` | `response_language` | 26 | il legacy calcola la lingua a runtime con un'euristica, non la persiste. | no |  |
| `manager_configs` | `llm_provider_id` | 1 | nuovo: nessun override per stabilimento nel legacy (solo CHATBOT_MANAGER_LLM globale). | no |  |
| `manager_configs` | `created_by_user_id` | 1 | `chatbot_manager_config.created_by` e' vuota: la riga la crea il servizio, non un operatore. | no | Q1 |

### `fiscalprint` — 5 colonne

| Tabella | Colonna | Righe | Perché | Errore? | Domanda |
|---|---|---|---|---|---|
| `printer_configs` | `updated_by_user_id` | 43 | Il legacy non registrava l'autore dell'operazione: nessuna colonna «creato/modificato da» nella tabella sorgente. | no | Q1 |
| `receipts` | `reservation_public_id` | 4.487 | `booking_receipts.bookingId` e' vuota su tutte le 4.487 righe. | no |  |
| `receipts` | `booking_number` | 4.487 | derivabile solo se reservation_public_id e' risolto (mai, nello storico noto) - lasciato… | no |  |
| `receipts` | `receipt_number` | 4.487 | "receipt_number/document_no sempre '001' (bug confermato) - NON riportati as-is ... | no |  |
| `receipts` | `created_by_user_id` | 4.487 | "il legacy non registrava l'operatore della stampa" (nota scheda). | no | Q1 |

### `fiskaly` — 11 colonne

| Tabella | Colonna | Righe | Perché | Errore? | Domanda |
|---|---|---|---|---|---|
| `accounts` | `updated_by_user_id` | 7 | Il legacy non registrava l'autore dell'operazione: nessuna colonna «creato/modificato da» nella tabella sorgente. | no | Q1 |
| `cash_registers` | `updated_by_user_id` | 5 | Il legacy non registrava l'autore dell'operazione: nessuna colonna «creato/modificato da» nella tabella sorgente. | no | Q1 |
| `media_assets` | `checksum` | 2.546 | Si ottiene solo **scaricando il file da S3**: lo fa `utility/02-migrazione-file`, non l'ETL (decisione D9). | no | Q2 |
| `media_assets` | `byte_size` | 2.546 | Come `checksum`: richiede il file, non il database. | no | Q2 |
| `receipt_lines` | `source_line_public_id` | 2.852 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `receipt_payments` | `voucher_kind` | 2.549 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `receipt_payments` | `outstanding_concept` | 2.549 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `receipt_payments` | `ledger_entry_public_id` | 2.549 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `receipts` | `fiskaly_state_raw` | 2.549 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `receipts` | `cancelled_by_user_id` | 2.549 | `cancelled_by_user_id`/`cancellation_reason` sempre NULL:… | no | Q1 |
| `receipts` | `cancellation_reason` | 2.549 | `cancellation_reason` sempre NULL:… | no |  |

### `identity` — 3 colonne

| Tabella | Colonna | Righe | Perché | Errore? | Domanda |
|---|---|---|---|---|---|
| `access_users` | `password_changed_at` | 6.674 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `account_license_plan_translations` | `description` | 4 | Tabella popolata da un **seed EF Core**, non dall'ETL: nessuna mapping spec la tocca, e nel legacy `licenses` ha due sole righe con `type` e `months`. **Decisione D6: resta NULL per scelta**, non servono le descrizioni. | no | Q6 |
| `customer_customers` | `blacklist_reason` | 224.524 | nuovo, nessun dato legacy (il legacy aveva solo il flag, nota scheda). | no |  |

### `infra` — 2 colonne

| Tabella | Colonna | Righe | Perché | Errore? | Domanda |
|---|---|---|---|---|---|
| `integration_api_clients` | `expires_at` | 2 | `keysecret.expiresAt` e' vuota su entrambe le righe: colonna morta nel legacy, mappata comunque per altri ambienti. | no |  |
| `integration_api_clients` | `rotated_at` | 2 | nuovo campo, nessuna fonte legacy: mai ruotato al momento della migrazione. | no |  |

### `notification` — 51 colonne

| Tabella | Colonna | Righe | Perché | Errore? | Domanda |
|---|---|---|---|---|---|
| `deliveries` | `subject_public_id` | 200.884 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `deliveries` | `recipient_customer_public_id` | 200.884 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `deliveries` | `recipient_user_public_id` | 200.884 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `deliveries` | `recipient_name_snapshot` | 200.884 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `deliveries` | `template_id` | 200.884 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `deliveries` | `next_attempt_at` | 200.884 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `deliveries` | `provider_message_id` | 200.884 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `deliveries` | `idempotency_key` | 200.884 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `deliveries` | `failure_reason` | 200.884 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `deliveries` | `skip_reason` | 200.884 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `deliveries` | `delivered_at` | 200.884 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `deliveries` | `failed_at` | 200.884 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `email_channel_settings` | `last_verification_at` | 20 | L'esito della verifica SMTP e' la coppia last_verification_at + last_verification_error:… | no |  |
| `email_channel_settings` | `last_verification_error` | 20 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `establishment_claim_requests` | `bathhouse_public_id` | 13 | bathhouseId era LETTO ma MAI persistito nel legacy (vedi scheda "criticita'"): non c'e' alcun… | no |  |
| `establishment_claim_requests` | `bathhouse_name_snapshot` | 13 | idem: usato solo nel corpo dell'email, mai salvato a DB. | no |  |
| `establishment_claim_requests` | `handled_by_user_id` | 13 | Il legacy non registrava l'autore dell'operazione: nessuna colonna «creato/modificato da» nella tabella sorgente. | no | Q1 |
| `establishment_claim_requests` | `handled_at` | 13 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `establishment_claim_requests` | `resolution_note` | 13 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `establishment_claim_requests` | `client_ip_snapshot` | 13 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `establishment_claim_requests` | `user_agent_snapshot` | 13 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `inapp_messages` | `subject_public_id` | 317.503 | risolto (best-effort) nel custom_row_transform: reference_id e' un bookings.id o un… | no |  |
| `inapp_messages` | `created_by_user_id` | 317.503 | user_id legacy contiene in realta' un customers.id (mai il vero "scrivente"): colonna morta,… | no | Q1 |
| `marketing_automations` | `last_run_at` | 221 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `marketing_automations` | `created_by_user_id` | 221 | Il legacy non registrava l'autore dell'operazione: nessuna colonna «creato/modificato da» nella tabella sorgente. | no | Q1 |
| `marketing_campaign_recipients` | `delivery_public_id` | 10.741 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `marketing_campaign_recipients` | `skip_reason` | 10.741 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `marketing_contact_lists` | `created_by_user_id` | 125 | Il legacy non registrava l'autore dell'operazione: nessuna colonna «creato/modificato da» nella tabella sorgente. | no | Q1 |
| `marketing_contacts` | `consent_at` | 7.181 | Il legacy non registrava quando e con quale informativa il contatto ha dato il consenso. Per un dato di consenso il vuoto non e' neutro: senza data il contatto non e' dimostrabilmente contattabile. **Decisione del 2026-10-02: se non c'era nel legacy, resta NULL.** | no | Q5 |
| `marketing_contacts` | `consent_notice_version` | 7.181 | Come `consent_at`: nessuna versione di informativa nel legacy. Decisione: resta NULL. | no | Q5 |
| `marketing_contacts` | `last_contacted_at` | 7.181 | Ricostruibile dallo storico invii (`deliveries`) ma non ricostruito. Decisione: resta NULL. | no | Q5 |
| `marketing_email_campaigns` | `name` | 25 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `marketing_email_campaigns` | `created_by_user_id` | 25 | Il legacy non registrava l'autore dell'operazione: nessuna colonna «creato/modificato da» nella tabella sorgente. | no | Q1 |
| `marketing_events` | `repeat_unit` | 26 | `repeat_unit` sempre NULL (mai valorizzato nel legacy, nota scheda confermata). | no |  |
| `marketing_events` | `contact_list_id` | 26 | `contact_list_id` sempre NULL: `notification.media_assets` ancora pending… | no |  |
| `marketing_events` | `created_by_user_id` | 26 | Il legacy non registrava l'autore dell'operazione: nessuna colonna «creato/modificato da» nella tabella sorgente. | no | Q1 |
| `marketing_newsletter_subscriptions` | `confirmation_token` | 5.538 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `marketing_newsletter_subscriptions` | `confirmed_at` | 5.538 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `marketing_newsletter_subscriptions` | `consent_notice_version` | 5.538 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `marketing_newsletter_subscriptions` | `customer_public_id` | 5.538 | subscribes non collegava a customers: nessun riferimento vivo ricostruibile in batch. | no |  |
| `marketing_suppressions` | `property_id` | 434 | scope globale: subscribes era una lista di piattaforma, non per stabilimento. | no |  |
| `marketing_suppressions` | `source_delivery_public_id` | 434 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `marketing_suppressions` | `contact_public_id` | 434 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `marketing_suppressions` | `note` | 434 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `media_assets` | `checksum` | 30 | Si ottiene solo **scaricando il file da S3**: lo fa `utility/02-migrazione-file`, non l'ETL (decisione D9). | no | Q2 |
| `media_assets` | `byte_size` | 30 | Come `checksum`: richiede il file, non il database. | no | Q2 |
| `media_assets` | `content_type` | 30 | Come `checksum`: richiede il file, non il database. | no | Q2 |
| `media_assets` | `uploaded_by_user_id` | 30 | Il legacy non registrava l'autore dell'operazione: nessuna colonna «creato/modificato da» nella tabella sorgente. | no | Q1 |
| `templates` | `created_by_user_id` | 246 | `custom_templates.createdBy` e' vuota su tutte le 103 righe sorgente (mai valorizzata dal frontend). | no | Q1 |
| `user_channel_preferences` | `quiet_hours_start` | 851 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `user_channel_preferences` | `quiet_hours_end` | 851 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |

### `payment` — 18 colonne

| Tabella | Colonna | Righe | Perché | Errore? | Domanda |
|---|---|---|---|---|---|
| `gateway_connected_accounts` | `revoked_at` | 44 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `ledger_entries` | `agreement_line_public_id` | 2.540 | Fonte (3) hotel_payments, fuori dalla copertura di questa spec (vedi nota in testa al file). | no |  |
| `ledger_entries` | `payment_mode` | 2.540 | transactions_payments non ha una colonna modalita' di pagamento (nessuna fra le colonne… | no |  |
| `ledger_entries` | `source_ref_id` | 2.540 | gross_amount NON si mappa: e' una colonna generata dal database… | no |  |
| `ledger_entries` | `customer_public_id` | 2.540 | Nessuna FK cliente su transactions_payments, solo snapshot testuali (colonne sotto). | no |  |
| `ledger_entries` | `recorded_by_user_id` | 2.540 | "movimento da JSON, nessun operatore tracciato" (nota scheda) - qui e' da transactions_payments… | no | Q1 |
| `ledger_entry_lines` | `reservation_line_public_id` | 2.540 | "correlazione assente" nel legacy (nota scheda). | no |  |
| `request_links` | `charge_id` | 1.390 | "il legacy non collegava il link alla transazione" (nota scheda): nessuna fonte legacy;… | no |  |
| `request_links` | `created_by_user_id` | 1.390 | Nessun operatore tracciato nel legacy. | no | Q1 |
| `transaction_charges` | `stripe_charge_id` | 36.613 | Nessuna colonna legacy dedicata al Charge id (ch_...): valorizzata a runtime dal webhook… | no |  |
| `transaction_charges` | `customer_public_id` | 36.613 | Il legacy non lega la transazione al cliente (nota scheda): nessuna fonte da cui derivarlo. | no |  |
| `transaction_charges` | `captured_at` | 36.613 | Runtime-only, come stripe_charge_id poco sopra: lo scrive il webhook Stripe quando l'addebito… | no |  |
| `transaction_charges` | `canceled_at` | 36.613 | Runtime-only, stessa ragione di captured_at: la scrive il webhook Stripe all'annullamento. | no |  |
| `wallet_transaction_items` | `sale_item_public_id` | 34 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `wallet_transaction_items` | `variants_description` | 34 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `wallet_transactions` | `resale_offer_ref` | 610 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `wallet_transactions` | `reverses_transaction_id` | 610 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `wallet_transactions` | `ledger_entry_ref` | 610 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |

### `switchboard` — 34 colonne

| Tabella | Colonna | Righe | Perché | Errore? | Domanda |
|---|---|---|---|---|---|
| `switchboard_configs` | `sip_client_notes` | 1 | La colonna omonima dell'unica riga di `switchboard_config` (RDS dev) e' vuota. | no |  |
| `switchboard_configs` | `whatsapp_phone_number_id` | 1 | La colonna omonima dell'unica riga di `switchboard_config` (RDS dev) e' vuota. | no |  |
| `switchboard_configs` | `whatsapp_phone_e164` | 1 | nuovo: il legacy non salvava il numero in chiaro, solo l'id Meta. | no |  |
| `switchboard_configs` | `forward_custom_caller_id_e164` | 1 | La colonna omonima dell'unica riga di `switchboard_config` (RDS dev) e' vuota. | no |  |
| `switchboard_configs` | `forward_caller_id_name` | 1 | La colonna omonima dell'unica riga di `switchboard_config` (RDS dev) e' vuota. | no |  |
| `switchboard_media_assets` | `duration_ms` | 5 | non salvato nel legacy. | no |  |
| `switchboard_media_assets` | `checksum` | 5 | Si ottiene solo **scaricando il file da S3**: lo fa `utility/02-migrazione-file`, non l'ETL (decisione D9). | no | Q2 |
| `switchboard_messaging_wa_event_logs` | `config_id` | 11 | vedi nota in cima al file (D-53): non risolvibile da phone_number_id da solo. | no |  |
| `switchboard_messaging_wa_event_logs` | `property_id` | 11 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `switchboard_messaging_wa_event_logs` | `contact_name` | 11 | La colonna omonima e' vuota su tutte le 11 righe di `switchboard_wa_msg_log`. | no |  |
| `switchboard_messaging_wa_event_logs` | `conversation_id` | 11 | La colonna omonima e' vuota su tutte le 11 righe di `switchboard_wa_msg_log`. | no |  |
| `switchboard_messaging_wa_event_logs` | `body_text` | 11 | placeholder: custom_row_transform separa message_body (testo puro / dump JSON media a seconda… | no |  |
| `switchboard_messaging_wa_event_logs` | `body_payload` | 11 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `switchboard_messaging_wa_templates` | `meta_rejection_reason` | 3 | Vuota su tutte e 3 le righe sorgente: nessun template e' stato rifiutato da Meta. | no |  |
| `switchboard_voice_call_diagnostics` | `call_record_id` | 7.355 | **Fra registro e log non esiste una chiave**: il registro ha 4 colonne e nessun id Telnyx, il log ha gli id e nessun riferimento al registro, e descrivono gambe diverse della stessa chiamata. Correlare per chiamante+tempo da 9.118 coppie per 7.355 righe. | no |  |
| `switchboard_voice_call_records` | `forward_result` | 214 | segue outcome: non conoscendo l'esito, non si conosce nemmeno il risultato dell'inoltro. | no |  |
| `switchboard_voice_call_records` | `final_menu_item_public_id` | 214 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `switchboard_voice_call_records` | `telnyx_call_session_id` | 214 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `switchboard_voice_call_records` | `telnyx_call_control_id` | 214 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `switchboard_voice_call_records` | `telnyx_call_leg_id` | 214 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `switchboard_voice_call_records` | `answered_at` | 214 | `answered_at`, `ended_at`, `duration_seconds`, `ivr_path`, i tre identificativi Telnyx → NULL:… | no |  |
| `switchboard_voice_call_records` | `ended_at` | 214 | `ended_at`, `duration_seconds`, `ivr_path`, i tre identificativi Telnyx → NULL:… | no |  |
| `switchboard_voice_call_records` | `duration_seconds` | 214 | `duration_seconds`, `ivr_path`, i tre identificativi Telnyx → NULL:… | no |  |
| `switchboard_voice_menu_actions` | `wa_template_id` | 1 | placeholder: custom_row_transform lo risolve (best-effort) da action_config.templateName SOLO per… | no |  |
| `switchboard_voice_menu_actions` | `wa_template_name_snapshot` | 1 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `switchboard_voice_menu_actions` | `wa_recipient_phone_e164` | 1 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |
| `switchboard_voice_number_registrations` | `reg_identity_doc_number` | 1 | Campo **cifrato** (`bytea`): l'utility non ha accesso a un KMS, quindi i 4 campi PII della registrazione numero non vengono mai popolati. **Decisione del 2026-10-02**: le chiavi stanno in AWS Secrets Manager online e in `appsettings` in locale, quindi la cifratura e' possibile — ma per **una sola riga** la strada sobria e' reinserirla dal pannello. | no | Q3 |
| `switchboard_voice_number_registrations` | `reg_date_of_birth` | 1 | Come sopra: PII cifrata. Chiave decisa (Q3), una sola riga. | no | Q3 |
| `switchboard_voice_number_registrations` | `reg_place_of_birth` | 1 | Come sopra: PII cifrata. Chiave decisa (Q3), una sola riga. | no | Q3 |
| `switchboard_voice_number_registrations` | `reg_personal_tax_code` | 1 | Come sopra: PII cifrata. Chiave decisa (Q3), una sola riga. | no | Q3 |
| `switchboard_voice_number_registrations` | `released_at` | 1 | Nessuna mapping spec tocca la colonna: campo del modello nuovo, che il backend popola a runtime. | no |  |
| `switchboard_voice_softphone_devices` | `registered_by_user_id` | 1 | nuovo: nel legacy la registrazione avveniva via credenziali SIP, senza utente autenticato. | no | Q1 |
| `switchboard_voice_softphone_devices` | `device_label` | 1 | nuovo: nessun metadato di dispositivo nel legacy (solo il token nudo). | no |  |
| `switchboard_voice_softphone_devices` | `last_error` | 1 | Segnaposto **dichiarato** nella mapping spec: nessuna fonte legacy. | no |  |

# Parte 3 — Domande e risposte

> **Spostate qui il 2026-10-03**, quando `docs/_temp2.md` e' stato smontato. Stanno con il censimento
> e con il «perche'» perche' sono le domande nate **da** quelle colonne vuote: una domanda separata
> dal dato che l'ha fatta nascere e' una domanda di cui nessuno sa piu' il motivo.
>
> Il registro del lavoro che ne e' derivato — le diciannove voci **M** e le nove decisioni **D** —
> sta nell'inventario:
> [`_mancanti-da-sistemare.md`](_mancanti-da-sistemare.md) **Parte C**.


> Riscritte il 2026-10-02. Le quattordici domande della vecchia §3.3 erano formulate quando il
> censimento era appena stato fatto: dieci hanno ricevuto risposta e quattro sono **decadute** perché
> riguardavano colonne poi rimosse dallo schema. Sono conservate in fondo con l'esito, perché una
> domanda chiusa è una decisione presa e serve poterla ritrovare.
>
> Ogni domanda qui sotto dice: **cosa chiedo**, **perché lo chiedo** (cosa resta vuoto senza una
> risposta, con i numeri), **cosa succede se non si risponde**, e **quali opzioni vedo**. Le opzioni
> non sono esaustive: se la risposta è un'altra, va bene.

## 3.0 Lo stato delle tredici domande, in una tabella

Le domande erano tredici e **sono tutte chiuse**: tre lo erano già in `docs/_temp4.md`, poi nella Parte C dell'inventario e le avevo
riproposte per errore, nove hanno ricevuto risposta dal committente il 2026-10-02, e una si è dissolta
verificandola — non esistevano le fasce orarie che credevo di dover parsare. La numerazione resta
perché la [Parte 2](#parte-2--perché-ogni-campo-è-vuoto-e-se-è-un-errore) le cita colonna per colonna.

| | Argomento | Stato |
|---|---|---|
| **Q1** | Autore delle righe storiche (`created_by_user_id`) | ✅ **chiusa (D8)**: resta NULL, nessun utente tecnico |
| **Q2** | Metadati dei file (`checksum`, `byte_size`, `content_type`) | ✅ decisione chiusa (**D9**) · ⏳ **servono le credenziali AWS di sola lettura** per collaudare la utility |
| **Q3** | I 4 campi di dati personali cifrati del centralino | ✅ **chiusa**: chiavi in AWS Secrets Manager online, `appsettings` in locale |
| **Q4** | Fasce orarie dei servizi | ✅ **chiusa**: non esistono fasce, sono **durate** — niente da parsare |
| **Q5** | Consenso dei 7.181 contatti marketing | ✅ **chiusa**: se non c'era nel legacy, resta NULL |
| **Q6** | Descrizioni dei piani di licenza | ✅ **chiusa (D6)**: resta NULL |
| **Q7** | Con quale strada applico le due correzioni | ✅ **chiusa: (a)**, rerun completo da zero |
| **Q8** | Le due tabelle `pending` | ✅ **chiusa**: le costruisce il job · ⚠️ resta da sciogliere la sovrapposizione con `utility/TODO.md` §1.12 (la *prima* generazione) |
| **Q9** | Le 62 icone speciali della mappa | ✅ **chiusa: sì**, vanno migrate → diventa lavoro |
| **Q10** | Gli abbonamenti | ✅ **chiusa**: si attende l'inserimento da portale · ✅ nota scritta in `80-retention.md`: la cascata sugli stabilimenti non ha una data da cui decorrere |
| **Q11** | 77 ricevute Fiskaly scartate | ✅ **chiusa**: transazioni fallite/annullate senza voci né importo, non scontrini. Nessun dato fiscale perduto |
| **Q12** | Password Fisconline cifrate con la chiave vecchia | ✅ chiave decisa (come Q3) · ⏳ **resta da eseguire** la ri-cifratura |
| **Q13** | La durata delle fasce dei servizi | ✅ **chiusa: si aggiunge** `duration_minutes`, conversione esatta dalle 11 etichette |

**Stato: tutte e tredici chiuse.** In questo documento non resta nessuna domanda aperta: le colonne
vuote e le tabelle vuote hanno tutte una spiegazione e un verdetto, e le decisioni che servivano sono
state prese. Quello che resta è **lavoro**, elencato qui sotto.

Le domande ancora aperte nel progetto stanno **altrove** e non riguardano la migrazione dei dati:
sono censite in [§3.5](#35-domande-aperte-fuori-da-questo-documento).

**Non sono domande, sono cose da fare.** ✅ **Applicate il 2026-10-02 con la corsa 9** (strada (a)
di Q7: ETL corretto e migrazione rifatta da zero), tranne le due che dipendono da un accesso.

| | Cosa | Da | Esito |
|---|---|---|---|
| 🔨 | Correggere il relink scartato sulle tabelle esplose | Q7 | ✅ **1.823 recuperati** (1.044 + 635 + 144); relink risolti da 3.645 a **5.468**. Il promemoria si accoda su `public_id`, che l'ETL genera prima dell'insert |
| 🔨 | Migrare le icone speciali della mappa | Q9 | ✅ **690 righe** (570 `free` + 120 `occupied`) su 691 attese: l'unica mancante è la mappa senza layout, scartata con motivo. Spec nuova con `explode.variants` |
| 🔨 | Aggiungere `duration_minutes` e popolarla | Q13 | ✅ **1.661/1.661**, undici durate × 151 righe, **zero etichette non riconosciute**. Migration EF `AddTimeSlotDurationMinutes` |
| 🔨 | Popolare `last_activity_at` | Q14 | ✅ **224.524/224.524**: 106.912 dalle prenotazioni, 117.612 dalla data di cutover. Nuovo `aggregate_step` idempotente |
| ✍️ | Nota in `80-retention.md` sulla cascata in attesa degli abbonamenti | Q10 | ✅ scritta, con le tre conseguenze (il job gira a vuoto, l'anzianità riparte da zero, la data reale va importata) |
| ⏳ | Credenziali AWS di sola lettura, per collaudare `utility/02-migrazione-file` | Q2 | **in attesa di un accesso** |
| ⏳ | Ri-cifrare le 5 password Fisconline con la chiave nuova | Q12 | **in attesa**, da fare prima che qualcuno provi a usarle |
| ❓ | Decidere se `utility/TODO.md` §1.12 è superata dal job | Q8 | **aperta** |

**Esito della corsa 9**: 7.066.191 righe lette, **6.163.412 migrate, 0 in errore**, 134 tabelle
(132 `ready`, 2 `skipped_pending`), `EXIT=0`. Verifiche rieseguibili: **107 affermazioni su 107
verdi**; `scripts_colonne_vuote.py` **nessuna regressione** — e la classificazione a quattro casi ha
riconosciuto da sola le 4 colonne recuperate e la tabella a copertura nuova.

## 3.1 Le domande che riguardano difetti, non scelte

### Q7 — Con quale strada applico le due correzioni? — **chiusa: (a), rerun completo**

**Risposta del committente: (a).** Si correggono le spec e si **rifà la migrazione da zero**, perché è
l'unica strada che dimostra che l'ETL, eseguito su un ambiente pulito, produce il risultato giusto.

Il lavoro è elencato in §3.0 ed è stato ✅ **eseguito il 2026-10-02 con la corsa 9**, con la
procedura di sempre: drop degli schemi, `dotnet ef database update` sugli 11 moduli, poi
`run_headless.py --clean --relink` lanciato con `python -u` (~6 ore).

#### Che cosa va corretto, in concreto

| | Difetto | Righe | Cosa cambia nei file |
|---|---|---|---|
| **A** | I riferimenti all'utente creatore vengono buttati via su tre tabelle di `catalog` | **1.823** | `app/orchestrator.py`: quando una riga esplosa produce un riferimento da risolvere dopo, oggi il promemoria viene scartato perché la tabella non preserva l'id legacy e non c'è un id su cui fare l'`UPDATE`. Va recuperato l'id generato dal database (`INSERT ... RETURNING id`) e usato quello |
| **B** | `catalog.map_layout_special_icons` è vuota | **62** | una mapping spec nuova (`mappings/catalog/catalog_map_layout_special_icons.yaml`) che esplode le dieci colonne `specialUmbrella1..5` e `_free` con `explode.variants`, più la rimozione della nota ormai falsa in `catalog_map_layouts.yaml` |

La correzione **A** vale anche per il futuro: qualunque tabella esplosa che in avanti referenzi un
modulo migrato dopo di lei oggi perde il riferimento in silenzio. Non è una pezza per queste tre.

#### Le tre strade

| | Cosa faccio | Tempo | Com'è il DB dopo | Cosa succede alla prossima corsa pulita |
|---|---|---|---|---|
| **(a)** | Correggo l'ETL e **rifaccio la migrazione da zero** | **~6 ore** | identico a quello che l'ETL produce da un ambiente pulito | niente di nuovo: il risultato è già quello |
| **(b)** | Correggo l'ETL **e** scrivo uno script che applica solo la differenza al DB attuale | **~20 minuti** | corretto nei valori, ma **non è** quello che l'ETL produrrebbe | se la correzione dell'ETL fosse incompleta, il problema **ricompare** — e nessuno lo saprebbe, perché il DB attuale sarebbe già a posto |
| **(c)** | Correggo l'ETL e **non eseguo niente** ora | minuti | invariato: quelle 1.885 righe restano vuote | la correzione entra in vigore e il DB si allinea |

#### Perché raccomando (a), e qual è il vero argomento

Non è il tempo, è **la verifica**. Lo scopo di queste corse non è popolare il database di sviluppo —
quello si può rifare sempre — ma dimostrare che **l'ETL, eseguito da zero su un ambiente pulito,
produce il risultato giusto**. La strada (b) ottiene il dato senza dimostrare niente sull'ETL: se la
correzione fosse parziale, il database sarebbe corretto e il programma sbagliato, cioè la situazione
peggiore delle due, perché il sintomo sparisce e la causa resta.

È già accaduto esattamente così in questo lavoro: `base_price` e `fiskaly_line_item_id` erano stati
corretti nel backend e non nell'ETL, e il difetto è emerso **solo** rifacendo la migrazione da zero —
7.056 righe respinte che nessuna verifica sul database esistente avrebbe mostrato.

**La (c) è una scelta legittima** se preferisci non tenere occupata la macchina adesso: la correzione
resta nel codice e si materializza alla prima corsa utile. L'unico effetto è che fino a quel momento
il DB di sviluppo ha 1.885 righe con quelle colonne vuote — non un problema, se chi lo usa lo sa.

**Se non mi rispondi**: faccio (c), cioè correggo e non eseguo. Per lanciare sei ore di corsa
preferisco un tuo via esplicito.

### Q9 — Le icone speciali della mappa — **chiusa: sì, vanno migrate**

**Risposta del committente: sì.** Le dieci colonne `specialUmbrella1..5` più le varianti `_free` di
`configure_maps` vanno esplose in `catalog.map_layout_special_icons`, oggi vuota. Nel legacy **62
righe su 175** hanno almeno un'icona speciale valorizzata.

Nessun ostacolo tecnico: il motivo scritto nella spec («il motore non supporta l'esplosione di N
colonne in N righe») è decaduto il 2026-09-16 con `explode.variants`, introdotto esattamente per
questo caso. Diventa quindi lavoro da fare, e rientra in [Q7](#q7--con-quale-strada-applico-le-due-correzioni--chiusa-a-rerun-completo).

## 3.2 Le domande (e le decisioni già prese) sulle colonne vuote

### Q1 — L'autore delle righe storiche — **già deciso (D8): resta NULL**

**Non è una domanda aperta**, e averla riproposta è stato un mio errore: la decisione **D8** (la Parte C dell'inventario) ha
già risposto. Per le righe storiche in cui il legacy non ha l'autore, `created_by_user_id` e
`updated_by_user_id` restano **NULL**, senza alcun utente tecnico di migrazione. Sono oltre 30
colonne, su tabelle come `booking.groups`, `catalog.map_spots`, `fiscalprint.receipts`,
`notification.templates`.

**Resta una cosa, che però è un controllo e non una scelta**: nessuna schermata del nuovo gestionale
deve mostrare «creato da» come campo obbligatorio o filtrare per autore, altrimenti le righe storiche
si presenteranno come righe rotte. È una verifica da fare quando quelle schermate esisteranno.

**Da non confondere con il difetto Q7**: D8 dice NULL per le righe che nel legacy **non hanno**
l'autore. Le 1.823 righe di `inventory_items`, `map_sectors` e `pricing_discounts` ce l'hanno — ed è
per questo che sono un difetto e non una scelta.

### Q2 — I metadati dei file: la decisione c'è, mancano le credenziali

**La decisione è presa** (**D9**, la Parte C dell'inventario): `checksum`, `byte_size` e `content_type` non si calcolano
nell'ETL, perché richiedono di scaricare ogni file da S3. Li calcola `utility/02-migrazione-file`,
che **è scritta** (voce M18) e ~~non è mai stata collaudata su AWS reale~~ 🔄 **è stata collaudata su
AWS reale il 2026-10-01**: corsa in modalità prova con **sorgente il bucket di produzione in sola
lettura**, 20 file per tipo, **142 su 142** trasferiti su entrambe le destinazioni (archivio su disco
e LocalStack), **0 errori**, metadati calcolati. Vedi `utility/02-migrazione-file/reports/` e
[`TODO.md` §1ter/§1quater](../../../../../../utility/02-migrazione-file/TODO.md).

**Quello che resta non è una decisione, è un accesso**: una chiave AWS di **sola lettura** dedicata.
La prova del 2026-10-01 è stata fatta passando **dall'ambiente** la chiave in chiaro del legacy — che
è onnipotente e **va ruotata** (`README` §3.1 e §7.4 della utility). Quello che manca è quindi la
chiave dedicata e la **corsa completa** sui 780.220 oggetti, non il collaudo. Oggi quei metadati sono vuoti su tutte le righe delle quattro tabelle di
asset (`catalog.media_assets` 11.040, `fiskaly.media_assets` 2.546, `notification.media_assets` 30,
`switchboard.switchboard_media_assets` 5).

**Se non arrivano**: la utility resta non collaudata e i metadati vuoti. Senza `content_type` il
browser può non visualizzare correttamente un allegato, e senza `checksum` non si può verificare che
un file non sia corrotto — ma nessuna delle due cose blocca il funzionamento.

### Q3 e Q12 — I campi cifrati — **chiuse: AWS Secrets Manager online, `appsettings` in locale**

**Risposta del committente**: le chiavi stanno in **AWS Secrets Manager quando il sistema è online** e
in **`appsettings`** quando si lavora in locale. Vale per entrambi i casi aperti:

- **Q3 — i 4 dati personali del centralino** (`reg_identity_doc_number`, `reg_date_of_birth`,
  `reg_place_of_birth`, `reg_personal_tax_code`, `bytea`, 1 riga): l'utility di migrazione può
  cifrarli leggendo la chiave dalla stessa fonte del backend. Resta una scelta di merito, piccola:
  dare a un'utility di migrazione la chiave di cifratura per **una riga** è sproporzionato — la
  strada più sobria è che quella registrazione venga reinserita dal pannello. Lo segnalo, ma la
  decisione sulla chiave è presa e vale comunque per il backend.
- **Q12 — le 5 password Fisconline**: sono migrate **ancora cifrate con la vecchia chiave legacy**
  (`FISKALY_FISCONLINE_ENCRYPTION_KEY`), e vanno ri-cifrate in un passo manuale. Ora si sa con quale
  chiave: quella del secret manager in AWS, dell'`appsettings` in locale. **Va fatto prima che
  qualcuno provi a usare quelle credenziali**, perché oggi il campo è pieno e inutilizzabile.

### Q4 — Le fasce orarie dei servizi — **chiusa: non esistono fasce orarie**

**La domanda era mal posta, e l'ho posta male io.** Avevo scritto che il dato esiste nell'etichetta
legacy «in forme come `"9-11"`», citando il commento del `custom_row_transform`. Quel formato **nei
dati non c'è**. Le undici etichette distinte sono **durate**, ognuna ripetuta 151 volte
(11 × 151 = 1.661, cioè tutte le righe):

`10 min` · `15 min` · `30 min` · `45 min` · `1 hr` · `1 hr 30 min` · `2 hrs` · `3 hrs` · `4 hrs` ·
`5 hrs` · `6 hrs`

La precisazione del committente — «tendenzialmente non ci sono fasce notturne» — non serve quindi a
nulla: non c'è nessun orario da interpretare, e nessun `"2-4"` ambiguo. **Non c'è niente da parsare**,
e il «sì» con cui la domanda è stata chiusa va letto come conferma di questa conclusione, non come
via libera a un parsing.

**Il dato non è perduto**: `label` è `NOT NULL` ed è valorizzata su tutte le 1.661 righe, con la
durata esatta. Quello che manca è che `slot_start`/`slot_end` sono colonne di **orario assoluto**
mentre il legacy registra una **durata**: due concetti diversi, e la tabella non ha una colonna di
durata. Quelle due colonne restano quindi NULL per costruzione, non per rinuncia.

**La conseguenza operativa è in [Q13](#q13--la-durata-delle-fasce--chiusa-si-aggiunge-la-colonna).**

### Q5 — Il consenso dei contatti marketing — **chiusa: resta NULL**

**Risposta del committente: se nel legacy non era presente, si lascia NULL.** `consent_at` e
`consent_notice_version` restano vuoti su tutti i 7.181 contatti, e `last_contacted_at` con loro.

Resta però la conseguenza, che va saputa e non è una colonna vuota fra le altre: senza data e versione
dell'informativa quei contatti **non sono dimostrabilmente contattabili**. La decisione è coerente con
il principio applicato in tutto il progetto — non si inventa un dato di consenso — ma chi userà le
funzioni di invio marketing sullo storico deve sapere che quel consenso non è documentato.

### Q6 — Le descrizioni dei piani di licenza — **già deciso (D6): resta NULL**

**Non è una domanda aperta**, e anche questa l'avevo riproposta per errore: la decisione **D6** (la Parte C dell'inventario)
stabilisce che `identity.account_license_plan_translations.description` resta **NULL per scelta**. La
tabella la popola un seed EF Core, non la migrazione, e nel legacy `licenses` ha due sole righe con
`type` e `months`: non esiste nessuna descrizione da recuperare.

Se in futuro quei testi servissero sono quattro righe da scrivere nel seed, ma è lavoro di prodotto,
non di migrazione.

### Q8 — Le due tabelle `pending` — **chiusa: le costruisce il job**

**Risposta del committente**: la costruzione degli snapshot è lavoro del **job**, non della migrazione.
Vale per entrambe le tabelle della domanda, e coincide con quello che la documentazione già prevede:

- **`catalog.config_snapshots`** — la passata notturna di manutenzione ricostruisce gli snapshot di
  configurazione come rete di sicurezza ([`20-jobs.md` §1.4](../../progettazione/20-jobs.md), rimando a
  [`03-gestione-dati.md` §7.5](../../progettazione/03-gestione-dati.md));
- **`catalog.search_bathhouses`** — la sua scheda descrive già il modello di aggiornamento: rebuild
  **sincrono su evento**, più un **job notturno** di ricostruzione totale, più una utility manuale.
  La tabella è scritta **solo** dal processo di rebuild, mai dal dominio applicativo.

Quindi restano vuote per costruzione fino alla prima esecuzione del job, e non è un difetto.

**Sì, c'è una cosa in più, e riguarda la prima volta.** `utility/TODO.md` **§1.12 è ancora aperta**
(☐ «Ricostruzione iniziale degli snapshot di configurazione — dopo la migrazione, generazione da zero
di tutti gli snapshot read-model C2», con la proposta in
`analisi-post/db/_config-snapshot-proposta.md` §8). C'è quindi una sovrapposizione da sciogliere:

- se il **primo giro del job notturno** genera tutto da zero, la voce 1.12 non serve e va **chiusa
  come superata**;
- se invece la generazione iniziale ha bisogno di qualcosa che il job non fa — per esempio decidere
  la data a cui attribuire lo snapshot dello stato migrato, che non è «stanotte» ma «com'era alla
  migrazione» — allora 1.12 resta, e si riduce a una corsa una tantum.

È una differenza che si vede solo alla prima esecuzione, e vale la pena deciderla prima: uno snapshot
datato stanotte su una configurazione importata ieri racconta una storia sbagliata delle
configurazioni passate.

### Q10 — Gli abbonamenti — **chiusa: si attende l'inserimento da portale**

Nel legacy gli abbonamenti erano gestiti **fuori dal portale** e attivati o disattivati a mano; nel
nuovo sistema si gestiranno dal portale. **Decisione: si attende che i dati vengano inseriti da
portale.** `identity.account_subscriptions` e `account_subscription_periods` restano quindi vuote per
assenza di fonte, e il modello che già esiste va mantenuto perché è lì che il gestionale scriverà.

**La conseguenza non è una domanda, è un vincolo che va scritto in `80-retention.md`.** La retention
degli stabilimenti fa decorrere la cessazione da `account_subscriptions.ends_at` + 24 mesi
(`RETENTION_PROPERTY_TERMINATION_GRACE_MONTHS`). Finché quelle righe non esistono:

- la cascata di cancellazione sugli stabilimenti **non ha una data da cui decorrere e non parte**. È
  il comportamento prudente — i dati restano — ma non è ciò che il documento di retention descrive, e
  un documento che descrive una regola che non gira è peggio di uno che dichiara l'attesa;
- quando il portale avrà la gestione, l'anzianità degli stabilimenti migrati partirà dalla data di
  **inserimento**, non da quella reale. Se la data reale serve, è l'unica informazione che il nuovo
  sistema non può ricostruire da sé: va importata da dove è stata tenuta finora.

## 3.3 Due cose emerse rispondendo a «cosa resta da chiarire» (2026-10-02)

### Q11 — Le 77 ricevute Fiskaly — **chiusa: sono transazioni fallite, non scontrini**

**Verificato come chiesto, e la strada indicata non serviva: il problema non era il `property_id`.**
Ecco cos'erano davvero quelle 77, misurate sul legacy:

| `record_state` | Righe |
|---|---|
| `TRANSACTION_FAILED` | 55 |
| `INTENTION_CREATED` | 20 |
| `CANCELLED` | 2 |

**Tutte e 77 hanno `entries` e `payments` vuoti e `amount_total` nullo**: nessuna voce, nessun
pagamento, nessun importo. Sono transazioni fiscali **fallite o mai completate**, non documenti
emessi. Lo scarto avviene sul ramo «nessuna riga voce», non su quello del `property_id`.

**Il `booking_number` ce l'hanno tutte e risolve**: ho verificato che nessuna delle 77 ha un numero
non risolvibile. La derivazione che suggerivi — via `booking_number_informations` o via
`bookings`/`service_reservation` — non era quindi necessaria. (Per completezza: `booking_number_informations`
esiste, 143.556 righe, ma **non ha una colonna `propertyId`**, quindi passerebbe comunque per
`bookings`/`service_reservation`, che è già ciò che il transform fa.)

**Nessun dato fiscale è perduto**, e i 285 log diagnostici che puntano a quelle ricevute puntano
legittimamente a documenti che non esistono. Resta vero il rilievo di metodo: quello scarto era
**silenzioso** — nessuna riga di log — e lo si è potuto ricostruire solo interrogando il legacy.
Cablare il registro scarti su `fiskaly_receipts` resta utile perché la prossima volta la risposta sia
nel report invece che in un'indagine.

### Q12 — Le password Fisconline cifrate con la chiave vecchia

→ **Chiusa insieme a Q3**: vedi [Q3 e Q12](#q3-e-q12--i-campi-cifrati--chiuse-aws-secrets-manager-online-appsettings-in-locale).
Le chiavi stanno in AWS Secrets Manager quando il sistema è online, in `appsettings` in locale. La
ri-cifratura delle 5 password resta un passo manuale da fare **prima** che qualcuno provi a usarle.

### Q13 — La durata delle fasce — **chiusa: si aggiunge la colonna**

**Risposta del committente: aggiungere anche la durata, se presente nei dati legacy.** È presente, su
tutte le 1.661 righe, e la conversione è **esatta e chiusa**: undici valori noti, nessuna ambiguità,
nessun parsing che indovina.

| Etichetta legacy | `duration_minutes` | Righe |
|---|---|---|
| `10 min` | 10 | 151 |
| `15 min` | 15 | 151 |
| `30 min` | 30 | 151 |
| `45 min` | 45 | 151 |
| `1 hr` | 60 | 151 |
| `1 hr 30 min` | 90 | 151 |
| `2 hrs` | 120 | 151 |
| `3 hrs` | 180 | 151 |
| `4 hrs` | 240 | 151 |
| `5 hrs` | 300 | 151 |
| `6 hrs` | 360 | 151 |

**Il lavoro che ne deriva** — ✅ eseguito il 2026-10-02 (corsa 9: 1.661/1.661, zero etichette non riconosciute):

1. **backend** — `duration_minutes` (`smallint`, **nullable**) su
   `catalog.service_sale_item_time_slots`, con la migration EF. Nullable e non obbligatoria, perché
   una fascia definita come orario assoluto (`09:00–11:00`) non ha una durata da dichiarare: la si
   calcola. Il vincolo giusto, se si vuole, è «almeno uno dei due»: o la durata, o la coppia di orari;
2. **ETL** — `custom_transforms/catalog_service_sale_item_time_slots.py` converte l'etichetta con una
   tabella di corrispondenza **chiusa alle undici voci sopra**. Un'etichetta non prevista deve
   lasciare NULL **e scrivere una riga di log**, non tentare un'interpretazione: se un giorno
   comparisse `"1 hr 15 min"` va vista, non indovinata;
3. **schede** — `analisi-post/db/catalog/catalog_service_sale_item_time_slots.md` va aggiornata con la
   colonna nuova e con il fatto che il legacy registra durate, non fasce.

**Nota sulla convivenza**: `slot_start`/`slot_end` restano. Le righe storiche avranno la durata e non
gli orari; le fasce che un gestore definirà in futuro potranno avere gli orari. Le due cose non si
escludono, ed è il motivo per cui la colonna nuova si aggiunge invece di sostituirle.

## 3.4 Q14 — `last_activity_at`: la colonna vuota che bloccava tre policy — **chiusa il 2026-10-02**

Emersa incrociando questo documento con la specifica della retention sui file (allora
`_temp5.md`, poi confluita nelle schede di `analisi-post/db/`), su richiesta. Non era nell'elenco
delle tredici perché, presa da sola, `last_activity_at` sembrava una colonna legittimamente vuota —
categoria B, «la mantengono gli eventi di dominio». Vista **dal lato della retention** è un'altra cosa:
è la **decorrenza di tre policy già seminate** in `privacy.privacy_retention_policies`.

| Policy | `field_names` | Durata | Azione |
|---|---|---|---|
| anonimizzazione cliente inattivo | 9 campi di contatto | **48 mesi** | `anonymize` |
| azzeramento delle note | `note`, `discount_note` | **1 stagione** | `null_fields` |
| cancellazione completa | *(riga intera)* | **10 anni** | `delete_row` |

Con la colonna NULL su tutte le 224.524 righe, **nessuna delle tre si applica a un solo cliente
legacy** — cioè proprio ai dati vecchi che la retention dovrebbe ripulire, comprese le `note`, che
possono contenere dati sanitari (art. 9 GDPR).

**✅ Risposta del committente: derivarla dalle prenotazioni; per chi non ne ha, la data della
migrazione.**

| Clienti | Quanti | `last_activity_at` |
|---|---:|---|
| con almeno una prenotazione | **106.910** | `MAX` sulle date delle sue prenotazioni |
| senza nessuna prenotazione | **117.614** | **la data della migrazione** |

La seconda riga è una scelta dichiarata, non un dato sul cliente: *di attività prima di quel momento
non c'è prova, quindi si comincia a contare da lì*. Due conseguenze da conoscere:

1. quei 117.614 maturano **tutti lo stesso giorno**, 48 mesi dopo la migrazione: sarà
   un'anonimizzazione di massa, non un flusso continuo, e il job deve reggerla;
2. ⚠️ **la data va passata come costante della corsa, non `now()`.** Con il rerun completo deciso in
   **Q7**, `now()` farebbe **azzerare l'orologio** a 117.614 schede a ogni riesecuzione, e la retention
   non scatterebbe mai. Va presa dalla data di cutover e registrata nel report della corsa.

**Lavoro che ne deriva** — ✅ eseguito il 2026-10-02 (corsa 9: 224.524/224.524, `aggregate_step`
`identity_customer_last_activity`): `mappings/identity/identity_customer_customers.yaml` e il
relativo `custom_transforms`; annotazioni già scritte nella spec e in
[`identity_customer_customers.md`](../../analisi-post/db/identity/identity_customer_customers.md).

## 3.5 Domande aperte **fuori** da questo documento

Censite il 2026-10-02 rispondendo a «ci sono altre domande aperte?». **Nessuna riguarda le colonne
vuote o la migrazione dei dati** — quelle sono tutte chiuse (§3.0) — ma esistono, e se non sono scritte
qui tornano a essere invisibili.

| Dove | Cosa resta aperto | Natura |
|---|---|---|
| [`_mancanti-da-sistemare.md`](_mancanti-da-sistemare.md) | **L'accesso con username breve va conservato?** 384 utenti lo usano, tutti con un'email valida, quindi nessuno resta chiuso fuori in entrambi i casi | scelta di prodotto, non blocca niente |
| ~~[`83-retention-file.md`](../../progettazione/83-retention-file.md) §3.4~~ ✅ | **Chiusa** (B2, 2026-10-01): «come contare i riferimenti a un file» si risolve con la **query di risalita sulle 19 FK** al momento della valutazione, senza vista materializzata né trigger. È un job giornaliero, non un percorso caldo; si rivaluta se i tempi lo richiedono | chiusa |
| ~~[`83-retention-file.md`](../../progettazione/83-retention-file.md) §3.2~~ ✅ | **Non più una domanda: è lavoro in attesa del via.** Le colonne sono confluite nelle **7 migration** applicate il 2026-10-03 (elenco in [`83-retention-file.md` §5](../../progettazione/83-retention-file.md)) (le 3 di privacy + `property_id` su `catalog.media_assets` + la tabella `catalog.agreement_reports`) | lavoro bloccato, non una scelta |
| [`19-frontend-offline.md`](../../progettazione/19-frontend-offline.md) §9 | **Sette decisioni sull'hub offline (A–G)**, da chiudere prima di implementare. La principale è **A, il motore di prezzo**: oggi il prezzo è calcolato interamente nel client e il server non ricalcola; se il refactoring lo sposta lato server, per prenotare offline serve una copia del motore nel client e il server la rivalida al replay. È un bivio architetturale, non un dettaglio | **la più pesante delle aperte** |
| [`11-modulo-privacy.md`](../../progettazione/11-modulo-privacy.md) §7 | **Dubbi legali** da chiudere con uno specialista: campo note e dati sanitari (art. 9), necessità di un DPO (art. 37), DPA/SCC con i fornitori extra-UE | serve un parere legale, non una scelta nostra |

**Due avvertenze su quell'elenco, perché due documenti sono rimasti indietro:**

1. **`11-modulo-privacy.md` §7 punto 1 è superato.** Pone come aperta la domanda se SummerBooking sia
   soggetta ai 72 mesi di retention telefonica, e cita il **D.L. 7/2015**. Entrambe le cose sono state
   chiuse: la norma giusta è l'**art. 24 della L. 167/2017** (il D.L. 7/2015 era una mia citazione
   sbagliata), e l'obbligo vincola il «fornitore di servizi di comunicazione elettronica accessibili al
   pubblico» — **SummerBooking non ha un'autorizzazione generale AGCOM**, quindi non la riguarda, e i 6
   mesi decisi dal titolare restano. Vedi A2 di `_mancanti-da-sistemare.md`. Quella sezione va corretta,
   altrimenti continua a presentare come dubbio una cosa verificata.
2. ✅ **`_temp5.md` §8 aveva un titolo stantio — corretto il 2026-10-02** in «due chiusi, uno è lavoro»; quel documento è poi stato smontato nelle schede di `analisi-post/db/`. Era «due chiusi, uno è
   lavoro». Dei tre nodi due sono ✅ e il terzo (F8, la regola stagionale) ha la regola decisa — «1
   gennaio dell'anno N−2, ricalcolata a ogni esecuzione». Quello che resta è **lavoro**, non una
   decisione: scrivere quella funzione **una volta** e condividerla fra tutti i job stagionali, perché
   se ogni job se la ricalcola prima o poi uno userà il 1 aprile.

## 3.6 Le quattordici domande precedenti, con l'esito

| # vecchio | Domanda | Esito |
|---|---|---|
| 1 | Backfill o rerun? | **Risposta: (b) rerun completo.** I due backfill sono diventati `aggregate_step` durevoli, così un ambiente pulito dà lo stesso risultato |
| 2 | `relink_pending()` va nella procedura standard? | **Fatto** (voce M8): è un passo di `run_headless.py --relink` e il report dichiara i riferimenti irrisolti |
| 3 | I relink verso id legacy inesistenti vanno nel registro scarti? | **Fatto**: li registra `_registra_relink_irrisolvibili()`; nella corsa dell'2026-10-02 sono 2.173, ripartiti per namespace |
| 4 | `packageSelectedData[].PackageItem[].lineItemId`: a quale tabella appartiene? | **Decaduta**: la voce M5 ha ribaltato la conclusione — il campo era una pezza del legacy e la colonna è stata **rimossa** |
| 5 | `agreement_lines.fiskaly_line_item_id`: l'ipotesi è corretta? | **Decaduta**: colonna rimossa |
| 6 | `lines.fiskaly_line_item_id`: vale aprire la lettura per il 2%? | **Decaduta**: colonna rimossa |
| 7 | `base_price` di testata serve? | **Risposta del titolare: non serve.** Colonna rimossa da entrambe le testate (decisione D1) |
| 8 | `voice_call_records`: placeholder per `outcome` o tabella `pending`? | **Risposta: placeholder dichiarato.** Le 214 righe sono migrate; i 7.355 `call_record_id` restano NULL per un motivo diverso e accertato (nessuna chiave fra le due tabelle) |
| 9 | `created_by_user_id`: NULL o utente tecnico? | **Ancora aperta** → riscritta come **Q1** |
| 10 | `deliveries.property_id`: tentare la deduzione da `emailFrom`? | **Fatto** (voce M17): la colonna è popolata su 141.686 righe su 200.884 |
| 11 | Metadati dei file: ETL, job, o NULL? | **Risposta: utility dedicata** (`02-migrazione-file`, decisione D9) — resta il collaudo su AWS → **Q2** |
| 12 | `account_license_plan_translations.description`: resta NULL? | **Ancora aperta** → riscritta come **Q6** |
| 13 | Il controllo va reso ricorrente? | **Fatto** (voce M12): `scripts_colonne_vuote.py` confronta con un elenco atteso e distingue regressione, colonna eliminata, recupero e copertura nuova |
| 14 | Le colonne volutamente NULL vanno annotate nelle schede? | **Fatto** (voce M13): 126 note aggiunte nelle schede di `analisi-post/db/` |

# Parte 4 — Come si è arrivati qui (storia del censimento)

> **Composta il 2026-10-03** smontando il primo censimento (allora anch'esso in un `docs/_temp3.md`). Conserva i due pezzi di quel documento che
> servono ancora: il **confronto** col primo censimento e l'elenco di ciò che i fatti hanno
> **smentito**. Sono quelli che evitano di riaprire una questione chiusa.
>
> **Non conserva** la classificazione per categorie delle 182 colonne del 2026-09-28 né l'elenco dei
> rimedi proposti allora: la prima è superata dalla [Parte 2](#parte-2--perché-ogni-campo-è-vuoto-e-se-è-un-errore),
> che ha *ogni* colonna con il suo perché e un verdetto; i secondi sono stati tutti applicati, e
> l'esito di ognuno è nella **Parte C** di
> [`_mancanti-da-sistemare.md`](_mancanti-da-sistemare.md). Tenere due
> versioni della stessa analisi significa che qualcuno leggerà la vecchia credendola attuale.

## 4.1 La fotografia del 2026-09-28

> **Questa classificazione è quella del censimento del 2026-09-28** (182 colonne) e resta valida come
> analisi del *perché* una colonna sia vuota: le ragioni non cambiano al cambiare del conteggio. Per
> lo stato attuale (170 colonne al 2026-10-02, **166** dopo la corsa 9) vale la Parte 1, e il raccordo qui sopra elenca le 21 uscite e le 9
> entrate. Le 6 colonne eliminate dallo schema restano citate qui sotto: la riga spiega perché erano
> vuote, ed è l'argomento per cui sono state poi rimosse.

| Cat. | Significato | Colonne |
|---|---|---|
| **A** | Placeholder NULL esplicito nella spec (`constant` / `value: null`) | 80 |
| **B** | Colonna assente da yaml e da custom transform — non migrata | 63 |
| **C** | Riferimento FK non risolto (relink in sospeso) | 19 |
| **D** | Mappata/calcolata, ma la sorgente legacy è sempre vuota | 19 |
| **E** | Nessuna mapping spec | 1 |
| | **Totale** | **182** |

## 4.2 Che cosa è cambiato da allora

La rilevazione precedente contava **182** colonne, quella del 2026-10-02 ne contava **170** (oggi
**166**: le 4 recuperate dalla corsa 9, §3.0). Lo scostamento è
piccolo e interamente spiegato: 21 colonne sono uscite dall'elenco, 9 sono entrate.

> Confrontando i due elenchi serve una cautela: nel censimento del 28 settembre le tabelle di
> `notification` e `switchboard` portavano il **prefisso dello schema** (`notification_deliveries`),
> oggi no (`deliveries`). Senza normalizzare i nomi, la stessa colonna compare come uscita *e* come
> entrata, e il confronto dice il falso — una trentina di voci su entrambi i lati.

**Le 21 uscite, in tre gruppi diversi che non vanno confusi:**

| Gruppo | Colonne | Cosa è successo |
|---|---|---|
| **Eliminate dallo schema** (6) | `fiskaly_line_item_id` su `booking.agreement_lines`, `.line_additional_items`, `.lines`, `.service_lines`; `base_price` su `catalog.pricing_price_lists` e `.agreement_price_lists` | **non sono state riempite: non esistono più.** Rimosse dalle voci M5 e D1 della Parte C di [`_mancanti-da-sistemare.md`](_mancanti-da-sistemare.md). Un elenco di colonne vuote che si accorcia non è di per sé una buona notizia |
| **Rinominata** (1) | `notification.email_channel_settings.last_verified_at` | il nome reale è **`last_verification_at`**: era un errore del censimento, non un cambiamento. Ricompare fra le "entrate" col nome giusto, quindi è vuota come prima |
| **Effettivamente popolate** (14) | `created_by_user_id` su 4 tabelle di `catalog`, `catalog.bathhouses.updated_by_user_id`, `catalog.properties.owner_user_public_id`, `fiskaly.receipt_logs.receipt_id`, `notification.deliveries.property_id`, `reservation_public_id` su 3 tabelle di `payment`, `payment.transaction_charges.connected_account_id`, `welcome_audio_asset_id` e `fallback_audio_asset_id` su `switchboard.configs` | recuperate dal lavoro sulle mapping spec (voci M6, M7, M17 e precedenti) |

**Le 9 entrate non sono una regressione.** Otto sono su
`switchboard.voice_call_records`, che il 28 settembre era **vuota** e quindi esclusa
dall'analisi: con la voce M15 ha 214 righe, e le sue colonne-segnaposto diventano visibili per la
prima volta. Nessuna ha perso valori — è copertura nuova, non un peggioramento. La nona è la
colonna rinominata di cui sopra.

> Questa distinzione è ora automatica: `scripts_colonne_vuote.py` classifica gli scostamenti in
> quattro casi (regressione / eliminata dallo schema / recuperata / **copertura nuova**) invece di
> due, e ricorda quali tabelle erano popolate. Prima chiamava "recuperate" le colonne cancellate, cioè
> dava una buona notizia al posto di una modifica di schema.

## 4.3 La prima lettura, e che cosa i fatti hanno smentito

> Lettura a caldo dei soli dati. Ognuno di questi punti e' poi stato verificato contro
> le mapping spec e il legacy nella **Parte 2**, che ne conferma alcuni, ne smentisce
> altri e individua 3 problemi non visibili da qui.

> **Aggiornamento del 2026-10-02** (verificato sul database dopo la migrazione a zero errori).
> Quattro dei punti qui sotto sono superati, e vanno letti con questa premessa:
>
> - **punto 1** — la causa dichiarata è falsa: `switchboard_voice_call_records` **non è più vuota**
>   (214 righe, voce M15). `call_record_id` resta NULL su tutte le 7.355 righe, ma per un motivo
>   diverso e accertato: **fra le due tabelle legacy non esiste una chiave**. Il registro ha quattro
>   colonne e nessun identificativo Telnyx, il log ha gli identificativi e nessun riferimento al
>   registro, e descrivono **gambe diverse** della stessa chiamata. Correlare per chiamante e
>   vicinanza temporale dà 9.118 coppie candidate per 7.355 righe: non è una chiave;
> - **punto 2** — risolto: `fiskaly.receipt_logs.receipt_id` è valorizzata su **7.840 righe su
>   8.125** (voce M7). Le 285 che restano NULL puntano a 78 ricevute scartate dall'ETL, e scriverle
>   violerebbe la chiave esterna;
> - **punto 3** — ancora valido, ma le colonne tutte NULL di `booking.lines` sono **quattro**, non
>   cinque: `fiskaly_line_item_id` è stata **eliminata**;
> - **punto 5** — decaduto: la colonna `fiskaly_line_item_id` non esiste più su nessuna delle quattro
>   tabelle. La voce M5 (Parte C di [`_mancanti-da-sistemare.md`](_mancanti-da-sistemare.md)) ha ribaltato la conclusione della Parte 3 §E —
>   quel campo era una pezza del legacy, e il modello nuovo non lo ospita.

1. **`switchboard.switchboard_voice_call_diagnostics.call_record_id`** (7.355 righe):
   la FK è sempre NULL e la tabella padre `switchboard_voice_call_records` è
   **vuota**. I log diagnostici sono stati migrati senza la chiamata di
   riferimento: verificare la mappatura ETL fra `switchboard_call_log` e
   `switchboard_call_register` del legacy.

2. **`fiskaly.receipt_logs.receipt_id`** (8.125 righe): stessa forma — i log
   esistono, il collegamento alla ricevuta no, pur essendoci 2.549 righe in
   `fiskaly.receipts`.

3. **`booking.lines`** (310.802 righe) ha 5 colonne tutte NULL, fra cui
   `sector_id`, `sector_label_snapshot` e `price_list_id`: la riga di
   prenotazione non è collegata né al settore né al listino. È il nucleo del
   dominio, va chiarito se è un limite dell'ETL o del modello legacy.

4. **`*_line_id` verso `booking.lines`** (`discounts`, `guests`,
   `service_lines`) sono sempre NULL: le righe figlie non risalgono alla riga
   di prenotazione.

5. **`fiskaly_line_item_id`** è NULL su tutte e 4 le tabelle che la espongono
   (`agreement_lines`, `line_additional_items`, `lines`, `service_lines`):
   la riconciliazione con le righe di ricevuta Fiskaly non è mai stata scritta.
   → **confermato e quantificato nella Parte 3 §E**: il `lineItemId` esiste nel
   legacy (fino a 158.151 valori) e l'ETL non lo legge.

6. **`notification.notification_deliveries`** (200.884 righe) ha 13 colonne
   tutte NULL su 33: di fatto la tabella conserva solo lo scheletro della consegna, senza destinatario, template, esito né
   timestamp di consegna.

7. **`payment.transaction_charges`** (29.387 righe): `stripe_charge_id`,
   `connected_account_id`, `captured_at` e `canceled_at` sempre NULL —
   nessun addebito migrato è riconciliato con Stripe.

8. **`checksum` / `byte_size` / `content_type`** sui quattro `*media_assets`
   (catalog, fiskaly, notification, switchboard): i metadati dei file non sono
   mai stati calcolati in migrazione.

9. **`created_by_user_id` / `updated_by_user_id`** (30 occorrenze) sono NULL in
   modo sistematico: l'ETL non risolve l'utente autore. Se il requisito di
   tracciabilità è reale serve una decisione (utente tecnico di migrazione vs
   NULL accettato).

10. **Prezzi mai valorizzati**: `catalog.pricing_price_lists.base_price` (2.527
    righe), `catalog.agreement_price_lists.base_price` (388),
    `catalog.pricing_price_list_date_prices.price` (1.414),
    `booking.service_time_slots.slot_price` (559). Da verificare se il prezzo
    vive solo nelle tabelle di dettaglio.

11. **Colonne cifrate `bytea` in `switchboard_voice_number_registrations`**
    (`reg_identity_doc_number`, `reg_date_of_birth`, `reg_place_of_birth`,
    `reg_personal_tax_code`): l'unica riga presente non ha i dati di
    registrazione del numero.

---
