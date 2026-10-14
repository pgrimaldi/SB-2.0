# Domande per il committente / architetto

> Raccolta unica delle decisioni ancora aperte sul **nuovo schema** (`analisi-post/db/`). Ogni voce:
> contesto → perché conta → opzioni → **raccomandazione** (proposta di default se non arriva risposta).
> Indice tabelle: [`_feature-tabelle.md`](_feature-tabelle.md). Read-model JSON:
> [`_config-snapshot-proposta.md`](_config-snapshot-proposta.md).
>
> Legenda impatto: 🟥 cross-cutting / struttura · 🟧 una feature · 🟨 rifinitura / dato.
>
> **Nota (2026-09-19/21)**: le domande già risolte (✅ deciso/verificato, 🅿️ parcheggiato, o risolte da
> una decisione trovata altrove nella documentazione) sono state rimosse da questo documento per
> tenerlo focalizzato su ciò che resta aperto; le decisioni prese restano comunque tracciate nelle
> singole schede `analisi-post/db/**.md` aggiornate di conseguenza. Rimosse in questo giro: **D-01**
> (risolta in [`docs/03-gestione-dati.md` §3.1ter](../../progettazione/03-gestione-dati.md) e
> [`docs/pre/identificatori-e-uuid.md`](../../progettazione/pre/identificatori-e-uuid.md) — `property_id` =
> `uuid`), **D-34** (retention confermata dal titolare in
> [`docs/05-ottimizzazioni-db.md`](../../progettazione/05-ottimizzazioni-db.md) presuppone che la tabella si
> mantenga), **D-50** (valori di retention fissati dal titolare negli stessi documenti — vedi le
> singole schede `switchboard_voice_call_records.md`/`_diagnostics.md`/`_wa_event_logs.md`), **D-63** e
> **D-64** (gap di disegno, entrambe chiuse con l'opzione (a): vedi
> [`catalog_agreement_price_list_stay_prices.md`](catalog/catalog_agreement_price_list_stay_prices.md)
> — tabella nuova aggiunta — e [`catalog_pricing_suggested_prices.md`](catalog/catalog_pricing_suggested_prices.md)
> — colonna `period_label` aggiunta, migration EF Core
> `20260921064813_AddAgreementPriceListStayPricesAndSuggestedPricePeriodLabel` applicata).
>
> **Rimosse il 2026-09-21** applicando la retention validata: **D-62** (`privacy_retention_policies`
> globali o per-stabilimento) — **risolta (a): globali**, nessun `property_id`. Non è più una
> raccomandazione ma un dato: la finestra di retention è *"uguale per tutti gli stabilimenti: nessuna
> variazione per piano di licenza o accordo commerciale"*
> ([`analisi-post/bl/02-errore-api-dati-archiviati.md` §2](../bl/02-errore-api-dati-archiviati.md)), e
> il titolare ha deciso i tempi come policy di piattaforma, non come opzione contrattuale
> ([`docs/82-retention-data.md`](../../progettazione/82-retention-data.md)). Se un domani servisse una policy
> **più severa** per un singolo stabilimento, si aggiunge un `property_id` nullable (`NULL` = regola di
> piattaforma) senza rompere lo schema. **Aggiunta**: D-65 (tre colonne mancanti per eseguire la
> retention validata).
>
> **Chiuse il 2026-10-02**: **D-65** su tutti e tre i punti — **nessuna delle tre colonne si aggiunge**.
> La voce resta qui per intero, perché su due punti porta con sé una **condizione di riapertura** che va
> letta prima di toccare quelle aree: (2) se nascerà uno stato «sospeso» distinto dal soft-delete degli
> account, e (3) se si volesse rendere dimostrabile la revisione annuale della blacklist. Le decisioni
> sono riportate nelle schede: [`catalog_properties`](catalog/catalog_properties.md),
> [`identity_access_users`](identity/identity_access_users.md),
> [`identity_customer_customers`](identity/identity_customer_customers.md).

---

## Regola generale di risoluzione (committente, 2026-09-09)

> **Priorità alle nuove specifiche** nei documenti di indirizzo (`docs/`, `analisi-pre/business-logic/`,
> le decisioni recepite in [`_feature-tabelle.md`](_feature-tabelle.md)). **Dove non specificato,
> rimane il comportamento legacy** (si migra la colonna/tabella così com'è, con la sola pulizia di tipo
> e naming; nessuna logica nuova).
>
> Applicando questa regola, la maggior parte delle domande originarie è stata **risolta**; restano qui
> solo quelle che richiedono ancora un input umano (contraddizioni fra documenti, validazioni
> legali/DPO, disponibilità dati di migrazione, o default proposti ancora da confermare).
>
> **Decisioni puntuali del committente (2026-09-09):**
> - **D-10** → **campo strutturato** (`cancellation_notice_hours` + `is_free_cancellation`), *per ora*;
>   annotato che potrà evolvere (enum/lookup) in futuro.
> - **D-08** → verificato nel codice back/front (vedi sotto); si migra come `updated_by_user_id`.
> - **D-12** → spiegazione ampliata sotto; default = **resta il legacy** (`type` NOT NULL su tutte le
>   righe, `template` incluso).
> - **D-28..D-33** (read-model JSON) → confermati i punti proposti (Opzione A; snapshot §4; aggiornamento
>   uniforme; `assetRef` + `ETag` + `payload_schema`; disponibilità rimandata a C1).

---

## A. C2 — sottoarea `pricing`

### D-13 🟥 `price_calculation` / `monthly_setting` / `weekend_setting`: insieme canonico + mappa varianti
**Contesto.** In produzione: `priceCalculation` **8 varianti** sporche
(`proportional`/`proportional price`/`Proportional price`/`daily`/`Daily price`/`Daily price with weekend`/
`logarithmic`/`""`); `monthlySetting` **6 varianti**; `weekendSetting` **6 varianti** per 3 concetti.
Le schede propongono: lookup `catalog_pricing_calculation_methods` (`proportional`, `daily`,
`daily_with_weekend`, `logarithmic`) + enum `monthly_setting` (`effective_month_duration`,
`actual_month_duration`, `fixed_30_days`) + enum `weekend_setting` (`saturday_sunday`, `sunday_only`).
**Perché conta.** È l'input del **motore prezzo**; una mappatura errata cambia i prezzi calcolati.
**Domanda.** (1) L'insieme canonico proposto è corretto e completo? `logarithmic` è ancora raggiungibile
da UI o è un residuo? (2) Confermare la **tabella di mappatura** varianti storiche → canonico
(es. `"Daily price"` → `daily`? o → `daily_with_weekend`? `"monthly"` → quale `monthly_setting`?).
**Raccomandazione.** Serve una revisione con chi conosce il modulo prezzo del frontend legacy
(`PriceCalcService.js`) prima di congelare la mappa di migrazione.

---

## B. C5 — domande ancora aperte (dalla precedente iterazione)

### D-25 🟨 `identity_customer_customers.note` — dati art. 9 GDPR (allergie)?
Base giuridica e disegno del campo da validare con DPO/legale
([`11-modulo-privacy.md` §7](../../progettazione/11-modulo-privacy.md)).

### D-26 🟨 `identity_account_subscription_periods.amount` — importi storici recuperabili?
Gli importi dei rinnovi **passati** sono recuperabili per la migrazione, o i periodi ante-go-live
nascono con `amount = NULL`?

---

## C. Read-model JSON di configurazione (doppio approccio tabelle + `jsonb`)

Proposta completa: **[`_config-snapshot-proposta.md`](_config-snapshot-proposta.md)**. Schede tabella:
[`catalog_config_snapshots`](catalog/catalog_config_snapshots.md),
[`catalog_search_bathhouses`](catalog/catalog_search_bathhouses.md).

### D-33 🟨 Serve un read-model di configurazione anche per **C5**?
Es. `account_config` (piano / stato abbonamento / giorni residui) — oppure C5 lo espone già bene via API
mirate e non serve uno snapshot. *(minore; da valutare quando si tocca C5.)*

---

## D. C3 — `payment`

### D-35 🟨 `payment_ledger_entry_lines`: ripartizione proporzionale come stima o solo movimento aggregato?
**Contesto.** [`transactions_payments`](../../analisi-pre/db/transactions_payments.md) spalma ogni
deposito/pagamento su **tutte** le righe-articolo in proporzione al lordo → gli importi per riga sono
stime, non incassi distinti. Serve all'export contabile esterno (`transactionApi`), attivo per **una
sola** property in allow-list.
**Opzioni.** (a) mantenere la ripartizione con flag `is_allocated = true` (compatibile con l'export
attuale); (b) registrare solo la testata `payment_ledger_entries` e popolare `Lines` **solo** quando
esiste un incasso realmente imputato a un articolo.
**Raccomandazione.** **(a)** `is_allocated` esplicito: l'export resta invariato, il consumatore sa che è
una stima. Rivedere se l'export verrà esteso a più stabilimenti.

### D-36 ❓ Crediti "sharing" storici su `users`: ricostruibili come wallet `resale_credit` per `(property, customer)`?
**Contesto.** Il cron legacy scrive `users.total_number_of_credit` / `total_credit_value` /
`totalCreditsEarned` **per assegnazione** e **ancorati all'email**, senza `property_id`. Il nuovo modello
([`payment_wallet_accounts`](payment/payment_wallet_accounts.md) `wallet_kind = resale_credit`) è per
`(property, customer)`.
**Perché conta.** Dipende dalla disponibilità dei dati in migrazione: se il legame credito↔stabilimento
non è ricostruibile (un utente ha condiviso posti in più stabilimenti), il saldo storico va assegnato a
una property "prevalente" o tenuto come saldo unico non-scoped.
**Raccomandazione.** Ricostruire da [`sharing_details`](../../analisi-pre/db/sharing_details.md)
(`property_id` + `booking_id` → cliente) dove possibile; per i residui non attribuibili, **saldo di
apertura** su un wallet per la property con più righe sharing dell'utente, annotato. Stessa natura di
**D-26** (importi storici che dipendono dai dati di migrazione).

### D-37 🟨 Serve il "pagamento multi-prenotazione" (ex `parent_bookings`)?
**Contesto.** [`parent_bookings`](../../analisi-pre/db/parent_bookings.md) è **dead code** (0 righe,
import errato). L'idea era pagare in un colpo solo più prenotazioni distinte.
**Perché conta.** Se è un requisito, va progettato ex novo in C3 (un `payment_transaction_charges` /
`payment_request_links` che referenzia **N** `reservation_public_id`, o un'entità "carrello di
pagamento").
**Raccomandazione.** **Non** modellato in questa iterazione (nessun dato, nessuna spec). Da riprendere
se il committente lo chiede.

---

## E. C4 — `fiskaly` / C4bis — `fiscalprint`

> Decisioni strutturali **già prese dal committente** (2026-09-10) → recepite in
> [`_feature-tabelle.md` → "Decisioni del committente" §C4/§C4bis](_feature-tabelle.md) come
> **C4-1..C4-5** e **C4bis-1..2**. Sotto solo i punti **residui** (non bloccanti, di fase
> implementativa / migrazione).

### D-43 🟨 `fiskaly_vat_codes` — mappa `code → Natura N1–N6`
La corrispondenza `NOT_SUBJECT`/`NOT_TAXABLE`/`CAUSE_1..4` → Natura (N1–N6) proposta nella scheda va
**validata col commercialista** prima del seed definitivo. I `code` sono quelli attesi da SIGN IT.

### D-44 🟨 `fiskaly_receipt_sequences` — numerazione con o senza buchi + reset su ricreazione cassa
(1) Consumo **atomico** (semplice, sempre corretto, ammette un "buco" se la firma fallisce) vs
**peek + commit** espliciti in transazione (nessun buco, più complesso). Il legacy fa peek+commit.
(2) Se una cassa Fiskaly viene **ricreata** (decommission → nuovo `system_id`) a metà giornata, la
sequenza — ora chiavata su `cash_register_id`, non su `system_id` — **prosegue**; `document_number`
si azzera comunque ogni giorno, quindi in pratica l'effetto è nullo, ma va confermato.
**Raccomandazione**: consumo atomico + prosecuzione, salvo che il commercialista richieda numerazione
senza salti o reset esplicito per cambio dispositivo.

### D-45 🟨 Migrazione — risoluzione `booking_number → reservation_public_id`
Gli scontrini legacy si legano alla prenotazione solo per stringa `booking_number`; 23 righe
`intention_created` appese + 53 `transaction_failed` + eventuali `booking_number` non più risolvibili
→ `reservation_public_id` resta `NULL` per quelle righe (annotato nella scheda).

---

## F. C6 — `notification`

> Perimetro e decisioni strutturali recepite in
> [`_feature-tabelle.md` → "Decisioni del committente" §C6](_feature-tabelle.md) come **C6-1..C6-9**
> (a seguito delle 4 risposte del committente del 2026-09-10: redesign in-app completo; outbox unica
> `notification_deliveries`; `notification_marketing_contacts` con consenso; creare anche le feature
> marginali marcate). Sotto solo i punti **residui**.

### D-46 🟥 Consenso marketing — base giuridica e collocazione
**Contesto.** Il legacy **non traccia alcun consenso** ([`docs/11-modulo-privacy.md`](../../progettazione/11-modulo-privacy.md):
"oggi non c'è prova che il consenso sia mai stato dato"); `contact_list_member` punta a `customers.id`,
`subscribes` è una lista di email, le campagne usano snapshot di indirizzi grezzi.
**Perché conta.** Determina se `notification_marketing_contacts` (collocazione **C6**, come modellato)
è la sede giusta del consenso o se il consenso è un attributo del **cliente C5**; e se lo stato
`consent_status = unknown` (tutto lo storico migrato) **blocca** l'invio o lo consente con warning.
**Opzioni.** (a) consenso in C6 su `notification_marketing_contacts` (`consent_status`/`_source`/`_at`/
`_notice_version`) + verifica **al momento dell'invio** — come modellato; (b) consenso in C5 su
`identity_customer_customers` (un attributo dell'anagrafica), C6 lo legge per valore; (c) ibrido: la
*prova* in C6, il *flag corrente* proiettato su C5.
**Raccomandazione.** **(a)** — il marketing è C6, il consenso ne governa il ciclo di vita
([`docs/03 §6`](../../progettazione/03-gestione-dati.md), criterio "chi possiede il dato"); `unknown` → invio
**bloccato** in prod (default prudente), sbloccabile per singola lista solo con re-opt-in.
**Richiede validazione DPO/legale** — non risolvibile con la regola generale.

### D-47 🟨 Feature marginali / incomplete di C6 — completare o dismettere
Tre feature poco usate, create su richiesta ma marcate "candidata a dismissione":
1. **`notification_marketing_events`** (39 righe, metà di prova) — tenere la feature "eventi
   promozionali"? Se sì, ricorrenza come **regola** (modellato) + proiezione verso la vetrina C2
   (`is_public`).
2. **`notification_marketing_event_reminders`** (0 righe, **nessun mittente nel legacy**) — completare
   con uno scheduler (EventBridge → worker) o **dismettere** insieme a
   `notification_marketing_events.reminders_enabled` / `contact_list_id`.
3. **`notification_establishment_claim_requests`** (ex `csv_manager_details`, 13 righe in 3 anni) —
   tabella + workflow di lavorazione, oppure semplice invio email senza persistenza / modulo CRM.
**Raccomandazione.** (1) tenere; (2) **dismettere** salvo richiesta esplicita (feature mai stata
viva); (3) tenere la tabella (costo nullo) ma senza workflow finché non serve.

### D-48 🟨 Vocabolario canonico dei segnaposto template
[`docs/03 §2.7`](../../progettazione/03-gestione-dati.md) chiede un vocabolario **chiuso** di segnaposto
(`establishment_name`, `customer_full_name`, `booking_date_from`, …) validato alla `create`/`update`
del template. **Opzioni.** (a) **enum/costante di codice** + `.resx` per l'etichetta editor + mappa
`legacy → canonico` nello script di migrazione (proposto); (b) **tabella di lookup**
`notification_template_placeholders` (code, label, `applies_to` template_kind) gestibile da pannello.
**Raccomandazione.** **(a)** — il vocabolario cambia solo con nuovo codice di rendering (ogni
segnaposto ha una fonte dati nel renderer), quindi è code-bound come i ruoli/`.resx`; una tabella
sarebbe un EAV mascherato ([`docs/03 §2.5`](../../progettazione/03-gestione-dati.md)).

### D-49 🟨 `notification_inapp_messages.payload jsonb`
Le variabili di rendering della notifica in-app (nome cliente, date, importo, sintesi errore) sono un
`jsonb` **snapshot** con forma chiusa per `kind`. Coerente con l'eccezione "`jsonb` derivato/read-model
ammesso" di [D-28] (approccio A). **Raccomandazione.** **Confermato** l'uso di `jsonb`: mai
interrogato per sotto-campo, ricostruzione non necessaria, alternativa (10+ colonne nullable per
`kind`) peggiore. Da confermare che rientra nell'eccezione già concessa per i read-model.

---

## G. C7 — `switchboard`

> Perimetro e decisioni strutturali recepite in
> [`_feature-tabelle.md` → "Decisioni del committente" §C7](_feature-tabelle.md) come **C7-1..C7-7**
> (a seguito delle risposte del committente del 2026-09-10: **redesign pulito** come C4bis; **creare
> anche** le 2 tabelle di log forensico). Sotto solo i punti **residui**.
>
> **⚠️ Aggiornamento 2026-09-11.** Le 8 tabelle `switchboard_*` legacy hanno **0 righe** su `local_sb` e
> sul dump di produzione ⇒ nessun backfill da quelle due copie. **Ma** esiste un **terzo ambiente**,
> `summer_booking_dev` (RDS, credenziali in
> [`back-pre/app/config/db.config.js`](../../../../../../back-pre/app/config/db.config.js)), dove la feature **è
> attiva e collaudata** con dati reali (1 config, 4 voci menù, 1 azione, 5 audio, 3 template WhatsApp
> approvati, 214 righe di registro chiamate, 7 355 di log diagnostico, 11 di log WhatsApp — dettaglio
> completo nelle note "Aggiornamento 2026-09-11" delle schede `analisi-pre/db/switchboard_*.md`). Questo
> **non cambia** le decisioni C7-1..C7-7 (il redesign pulito è anzi rinforzato dall'evidenza reale), ma
> aggiorna sostanzialmente D-51 sotto e introduce D-53.
>
> Le finestre di retention delle 3 tabelle di log (ex D-50) sono state **fissate dal titolare**
> (2026-09-17): dettaglio e fonti nelle singole schede
> [`switchboard_voice_call_records.md`](switchboard/switchboard_voice_call_records.md),
> [`switchboard_voice_call_diagnostics.md`](switchboard/switchboard_voice_call_diagnostics.md),
> [`switchboard_messaging_wa_event_logs.md`](switchboard/switchboard_messaging_wa_event_logs.md).

### D-51 🟧 C7 — mantenere, dismettere, o reperire l'interfaccia *(aggiornata 2026-09-11)*
**Contesto.** La feature "Centralino/Switchboard" ha **~159 rotte** nel backend legacy e 8 tabelle.
**Non è "mai attivata" in assoluto**: è **0 righe** su `local_sb` e sul dump di produzione, ma **attiva
e collaudata** su un terzo ambiente (`summer_booking_dev`, RDS) con un centralino reale configurato,
un IVR di test funzionante, 3 template WhatsApp approvati su Meta e ~2,5 mesi di traffico chiamate
(214 righe di registro, 7 355 di log diagnostico). Resta però vero che **nessuna schermata** esiste in
nessuno dei 5 frontend del repo (`front-admin/` compresa) —
[`analisi-pre/business-logic/stabilimento/13`](../../analisi-pre/business-logic/stabilimento/13-assistente-notifiche-account.md),
[`da-chiarire.md` #36](../../analisi-pre/business-logic/da-chiarire.md): la configurazione osservata è
stata fatta o via chiamate dirette all'API, o da un pannello esterno al repository (i commenti nel
controller citano un *"BACKOFFICE API (FOR PYTHON APP)"*). In più
[`docs/11-modulo-privacy.md` §3 / §"Domande aperte" 1](../../progettazione/11-modulo-privacy.md) segnala un
**dubbio legale non risolto**: se SummerBooking, come cliente-API di Telnyx, sia soggetta all'obbligo
di conservazione di 72 mesi sui dati di traffico (D.L. 7/2015) o se gravi solo su Telnyx — **ora meno
ipotetico**, visto che esiste già un volume reale di dati di traffico telefonico.
**Perché conta.** Determina se le 10 schede C7 vanno implementate ora, rimandate, o dismesse; e se la
retention del traffico centralino è derogata da un obbligo di legge. La scoperta di un uso reale cambia
il rischio di "dismettere senza guardare": qualcuno (dev interno o cliente pilota) sta già usando
questa integrazione.
**Opzioni.** (a) implementare C7 come modellato (redesign pulito), marcandolo "candidata a
dismissione" solo per l'assenza di UI nel repo, non per mancanza d'uso; (b) rimandare C7 (schema non
creato) finché non si reperisce il pannello esterno o si definiscono i requisiti da zero; (c) dismettere
il modulo (ora meno difendibile, avendo un uso reale documentato).
**Raccomandazione.** **(a)**, con priorità più alta di quanto assegnato inizialmente — coerente con
l'approccio "target pulito ridisegnato" già seguito per C4bis, ma qui l'evidenza reale suggerisce di
**chiedere esplicitamente al committente** se esiste già un cliente/uso a cui la migrazione deve
garantire continuità (es. l'account "Qbitsoft s.r.l." osservato potrebbe essere solo un test interno,
ma andrebbe confermato). Il dubbio D.L. 7/2015 **richiede validazione legale/DPO** — non risolvibile
con la regola generale.

### D-52 🟨 Eccezioni `jsonb` in C7
**Contesto.** Nel redesign C7 restano `jsonb`: `switchboard_messaging_wa_templates.components`
(definizione template nel formato Meta Cloud API), `switchboard_voice_menu_actions.wa_body_params` /
`wa_header_params` / `wa_button_params` (liste di argomenti dell'API Meta),
`switchboard_voice_call_records.ivr_path` (traccia sintetica del percorso IVR di **una** chiamata), e i
`raw_payload` / `handler_result` dei log forensici (webhook Telnyx/Meta).
**Perché conta.** Il modulo vieta il JSON come modello di dominio ([`docs/03 §2.5`](../../progettazione/03-gestione-dati.md)).
**Opzioni.** (a) confermare le eccezioni: sono **documenti/argomenti di sistemi esterni** (schema non
nostro, evolutivo, passthrough) o **snapshot chiusi scoped a una riga**, coperti da
[`docs/03 §3.6 punto 5`](../../progettazione/03-gestione-dati.md) e dall'analogia con [D-49]; `action_config`
polimorfo è invece **de-jsonizzato** in colonne. (b) spezzare anche questi in tabelle figlie.
**Raccomandazione.** **(a)** — `components` normalizzato = ogni modifica dell'API Meta diventa una
migrazione; `wa_*_params` e `ivr_path` sono 0–3 valori scoped a una riga. Colonne di riepilogo
**derivate** su `components` per la lista del pannello. Da confermare che rientrano nell'eccezione già
concessa ai read-model / documenti di terzi.

### D-53 🟨 *(nuova, 2026-09-11)* Attribuzione `config_id` sul numero WhatsApp generico
**Contesto.** `switchboard_messaging_wa_event_logs.config_id` è pensato per essere risolto da
`phone_number_id` (mapping numero Meta → centralino). Verificato su `summer_booking_dev`: le 11 righe
reali sono **tutte** sul numero WhatsApp **generico** di piattaforma (`switchboard_configs.whatsapp_phone_number_id`
è `NULL` per quello stabilimento), non su un numero custom. Se più stabilimenti condividono il numero
generico (scenario tipico finché non ordinano un numero custom), `phone_number_id` **da solo non
distingue** a quale di essi appartiene un evento.
**Perché conta.** Senza attribuzione, `config_id`/`property_id` restano sistematicamente `NULL` per la
maggioranza probabile dei messaggi (chi non ha un numero WhatsApp custom), riducendo l'utilità della
tabella per audit per-stabilimento.
**Opzioni.** (a) accettare l'attribuzione parziale (`config_id NULL` quando il numero è generico,
popolato solo per i numeri custom) — è comunque un miglioramento sul legacy (che non aveva **nessuna**
colonna di collegamento); (b) correlare via `wamid` con la riga `notification_deliveries` (C6)
dell'invio originale, che porta `property_id` nella propria transazione; (c) instradare un identificatore
di stabilimento nel `client_state`/parametro custom della chiamata a Meta, da ripropagare nel webhook.
**Raccomandazione.** **(a)** come default minimo (nessun costo aggiuntivo); **(b)** se serve un audit
per-stabilimento affidabile anche sul numero condiviso — richiede che l'invio `send_whatsapp` passi
sempre dall'outbox C6 (coerente con l'architettura generale). Non bloccante per la creazione delle
tabelle C7.

---

## H. C8 — `chatbot`

> Perimetro e decisioni strutturali recepite in
> [`_feature-tabelle.md` → "Decisioni del committente" §C8](_feature-tabelle.md) come **C8-1..C8-4**.
> Dominio piccolo (2 tabelle legacy → 3 nel target, con l'aggiunta di un catalogo provider LLM), **0
> righe** su `local_sb`/dump di produzione ma con dati **reali** su `summer_booking_dev` (10 righe di
> conversazione + 1 riga di configurazione, verificate 2026-09-11 — vedi
> [`chatbot_conversation_turns.md`](chatbot/chatbot_conversation_turns.md) e
> [`chatbot_manager_configs.md`](chatbot/chatbot_manager_configs.md)).

### D-54 🟧 Dati personali di clienti nel testo libero di `chatbot_conversation_turns`
**Contesto.** `input_text`/`output_text` sono testo libero, senza alcuna struttura o validazione di
contenuto. Verificato realmente su `summer_booking_dev`: una risposta contiene **nome e cognome di due
clienti finali** ("L'ombrellone 122 ... risulta essere stato prenotato da [nome] e da [nome]"),
restituiti dal provider LLM a partire da una fonte dati interrogata al momento (una funzionalità di
interrogazione dati non più presente nel modulo ridisegnato — vedi
[`docs/26-chatbot.md` §8](../../progettazione/26-chatbot.md)). Il legacy non ha **nessuna** retention: le righe
restano a tempo indefinito.
**Perché conta.** È un dato personale di un **terzo** (il cliente finale), non dell'operatore che ha
scritto il messaggio — un export/cancellazione GDPR relativo a quel cliente non troverebbe questa
occorrenza (il testo libero non è indicizzato per soggetto), un problema strutturale già noto per
qualunque campo "note libere" ([`docs/11-modulo-privacy.md`](../../progettazione/11-modulo-privacy.md)) ma qui
osservato con un caso reale, non solo ipotetico.
**Opzioni.** (a) retention breve/moderata (es. 12 mesi) + accesso ristretto (solo lo stabilimento
proprietario + `systemadmin`), senza tentare la redazione automatica dei nomi (rischio di falsi
positivi/negativi con un NLP dedicato, complessità sproporzionata al rischio); (b) redazione automatica
best-effort dei nomi propri prima della persistenza (stesso principio di pseudonimizzazione già adottato
in C7 sui numeri di telefono); (c) nessuna retention dedicata, si applica la policy generale del
modulo Privacy con un flag "contiene testo libero non indicizzabile per soggetto" sulla tabella.
**Raccomandazione.** **(a)** come primo passo (costo contenuto, riduce la finestra di esposizione);
valutare **(b)** solo se il volume cresce sostanzialmente. In ogni caso, annotare la tabella nel
registro dei trattamenti del modulo Privacy come "può contenere dati personali di terzi non
indicizzabili per soggetto" — da validare con DPO/legale.

### D-55 🟨 Canale `customer` (`chatbot_conversation_turns.channel`): seminare già l'enum o solo `manager`?
**Contesto.** Il legacy prevede già, nel commento del modello (`app`: *"'custo' = customer app, future
use"`), un canale lato cliente finale mai implementato — confermato: **0** righe reali con quel valore.
**Perché conta.** Decide se l'enum `chatbot.channel_kind` nasce con 1 solo valore selezionabile
(`manager`) o già con 2 (`manager`/`customer`, quest'ultimo non ancora raggiungibile da nessun
endpoint).
**Opzioni.** (a) creare l'enum con entrambi i valori fin da ora, coerente con l'intento già dichiarato
nel legacy, costo nullo; (b) creare solo `manager` ed estendere l'enum (migrazione) quando/se il canale
cliente finale verrà davvero costruito.
**Raccomandazione.** **(a)** — un enum PG si estende comunque con una migrazione a basso rischio (`ALTER
TYPE ... ADD VALUE`), e avere già il valore evita di dover distinguere "canale previsto ma senza
storico" da "canale che non esiste ancora" quando la domanda si ripresenterà. Non bloccante.

---

## I. INFRA — `infra`

> Perimetro e decisioni strutturali recepite in
> [`_feature-tabelle.md` → "Decisioni del committente" §INFRA](_feature-tabelle.md) come **INFRA-1..INFRA-3**.
> Dominio piccolo, **1 tabella** ([`infra_integration_api_clients`](infra/infra_integration_api_clients.md)),
> sostituisce `keysecret` (2 righe reali in produzione). Tutti gli altri riferimenti "INFRA" incontrati
> negli altri domini (segreti Fiskaly, SMTP, SIP, provider LLM) **non** producono tabelle: sono
> riferimenti al secret manager annotati sulla tabella del dominio proprietario (`*_secret_ref`) —
> [`docs/17-secrets.md`](../../progettazione/17-secrets.md), [`docs/80-retention.md` §3.10](../../progettazione/80-retention.md).

### D-56 🟨 Scope granulari per client API oltre `app_name`/`property_id`?
**Contesto.** [`keysecret.md`](../../analisi-pre/db/keysecret.md) propone, tra le colonne della futura
`api_clients`, anche `scopes`. Nel codice attuale però **non esiste alcun controllo di permesso più
fine** di "che applicazione sei" (`app_name`) + "a quale stabilimento sei vincolato" (`property_id`):
`checkApiKey` non legge né verifica alcuna lista di permessi.
**Perché conta.** Decide se aggiungere già ora una colonna/tabella di scope (complessità e superficie
di test in più, senza un consumatore reale) o rimandare a quando servirà davvero.
**Opzioni.** (a) non modellare `scopes` ora — `app_name` + `property_id` bastano per i 2 client reali;
aggiungere una tabella figlia `infra_integration_api_client_scopes` **solo** quando un partner futuro
richiederà permessi più fini della sola coppia app/property; (b) aggiungere fin da ora una colonna
`scopes` (enum array o testo) non ancora verificata da alcun codice, seguendo alla lettera la proposta
di `keysecret.md`.
**Raccomandazione.** **(a)** — coerente con "non progettare per requisiti ipotetici": nessun consumo
reale oggi, e un enum array aggiunto in un secondo momento non è una migrazione rischiosa. Non
bloccante.

### D-57 🟨 Seminare l'enum `integration_app` con `teamsystem` mai attivato?
**Contesto.** Come [D-55] per il chatbot: il codice prevede già `APP_NAMES.TEAMSYSTEM` (Cassa in
Cloud) ma **0 righe** `keysecret` con quel valore in produzione, coerente con
[`payment_pos_charges`](payment/payment_pos_charges.md) (`teamsystem_expenses` a 0 righe):
l'integrazione non è mai stata attivata.
**Perché conta.** Decide se l'enum `infra.integration_app` nasce con i soli valori realmente in uso
(`ofsale`, `transactions`) o già con `teamsystem`.
**Opzioni.** (a) includerlo fin da ora (il codice lo prevede già, costo nullo); (b) aggiungerlo solo
quando/se l'integrazione TeamSystem verrà davvero attivata.
**Raccomandazione.** **(a)**, per lo stesso motivo di [D-55]: un `ALTER TYPE ... ADD VALUE` è a basso
rischio comunque, meglio farlo insieme al resto dell'enum. Non bloccante.

### D-58 🟨 `expires_at` finalmente reale: scadenza obbligatoria sui nuovi client o resta opzionale?
**Contesto.** Nel legacy `expiresAt` è una colonna morta (mai letta né scritta): la scadenza reale era
solo quella del JWT emesso (24h / 7gg per TeamSystem). Nel target la colonna va **davvero** verificata
in autenticazione, ma resta nullable (nessuna scadenza per le 2 righe esistenti, migrate con
`expires_at = NULL`).
**Perché conta.** Decide se in futuro il pannello systemadmin **impone** una scadenza a ogni nuovo
client API partner (rotazione forzata) o la lascia a discrezione di chi lo crea.
**Opzioni.** (a) `expires_at` resta opzionale (`NULL` = nessuna scadenza), con un **alert operativo**
(non un vincolo a schema) sui client senza scadenza né rotazione (`rotated_at`) da oltre N mesi; (b)
`expires_at` obbligatoria per ogni nuovo client (`CHECK` applicativo in creazione), i 2 client esistenti
restano un'eccezione migrata.
**Raccomandazione.** **(a)** — coerente col volume (2-3 partner, gestiti a mano dal team, non un
self-service pubblico): un vincolo bloccante aggiunge frizione senza un rischio proporzionato; l'alert
operativo copre comunque il caso "credenziale dimenticata attiva per anni" che il legacy non tracciava
affatto. Non bloccante.

---

## J. Privacy — `privacy`

> Perimetro e decisioni strutturali recepite in
> [`_feature-tabelle.md` → "Decisioni del committente" §Privacy](_feature-tabelle.md) come
> **Privacy-1..Privacy-3**. Dominio piccolo, **5 tabelle** (schema `privacy`), nessuna legacy diretta —
> vedi [`docs/11-modulo-privacy.md`](../../progettazione/11-modulo-privacy.md). Il modulo **non possiede mai** un
> dato personale di cliente/utente: tutte le domande sotto sono di **modellazione interna**
> (workflow/configurazione), non di dato — nessuna richiede un parere DPO/legale (quelle, verificate sul
> testo di legge, sono già in `docs/11 §3/§7`).

### D-59 🟨 Testata unica invece di due aggregati `ErasureRequest`/`ExportRequest`?
**Contesto.** [`docs/11-modulo-privacy.md` §2](../../progettazione/11-modulo-privacy.md) elenca `ErasureRequest` ed
`ExportRequest` come due aggregati **distinti**, ma nota esplicitamente che condividono lo stesso
meccanismo ("Lo stesso meccanismo copre l'export", §5). La scheda
[`privacy_data_subject_requests`](privacy/privacy_data_subject_requests.md) li unifica in una tabella con
`request_kind` (`erasure`/`export`), seguendo il precedente già adottato nel progetto per casi identici
(`booking_reservations.reservation_kind`, `fiskaly_receipts.document_kind` — vedi §E).
**Perché conta.** Decide se lo schema `privacy` ha 1 testata + 1 figlia di esiti (5 tabelle totali, come
modellato) o 2 testate quasi-gemelle + 2 figlie di esiti (7 tabelle, più fedele alla lettera del
documento).
**Opzioni.** (a) testata unica con discriminatore (**già applicato**); (b) due tabelle separate come nel
documento, con più duplicazione strutturale ma aderenza letterale.
**Raccomandazione.** **(a)** — stesso criterio già validato nel progetto (pattern C1/C4): il
meccanismo (evento → risposta per modulo → consolidamento) è identico, solo l'azione finale cambia.
Correzione da riportare in `docs/11-modulo-privacy.md` §2. Non bloccante.

### D-60 🟨 Seminare `subject_kind` con `user` oltre `customer`?
**Contesto.** Tutti gli esempi del documento di indirizzo (endpoint, flusso §5) riguardano solo il cliente
finale (`customer`, rubrica C5 per-stabilimento). Un account di login C5 (`user`: titolare/staff) è
comunque una persona fisica con propri diritti GDPR, ma **nessun requisito verificato** lo richiede oggi.
**Perché conta.** Decide se l'enum `privacy.subject_kind` nasce con un solo valore realmente usato
(`customer`) o già con `user` per il caso futuro di un self-service staff.
**Opzioni.** (a) seminarlo fin da ora (costo nullo, stesso criterio di [D-55]/[D-57]); (b) aggiungerlo solo
quando/se richiesto.
**Raccomandazione.** **(a)**, stesso motivo di [D-55]/[D-57]: un `ALTER TYPE ... ADD VALUE` è a basso
rischio comunque. Non bloccante.

### D-61 🟨 Bundle di export: file su object storage o `payload jsonb` inline?
**Contesto.** L'aggregazione dei dati personali per una richiesta di export (Art. 15/20) può includere
anni di prenotazioni/pagamenti/notifiche per un cliente — dimensione non prevedibile a priori. C2 ammette
un `jsonb` come eccezione per un read-model **derivato** e a dimensione contenuta
(`catalog_config_snapshots`); qui il volume è meno controllabile.
**Perché conta.** Decide se `privacy_data_subject_requests` porta il contenuto del bundle come colonna a
DB o solo un riferimento a un file esterno.
**Opzioni.** (a) file su bucket privato (`export_artifact_origin`/`_path`, presigned URL a runtime — **già
modellato**); (b) `export_payload jsonb` inline, per analogia con l'eccezione C2.
**Raccomandazione.** **(a)** — coerente col pattern "mai URL/blob completo a colonna" del progetto
([`docs/03-gestione-dati.md` §1](../../progettazione/03-gestione-dati.md)) e più sicuro per dimensioni imprevedibili
(niente limite pratico di colonna, niente bloat della tabella di workflow). Non bloccante.

### D-65 ✅ **RISOLTA il 2026-10-02** — tre colonne che mancavano per **eseguire** la retention validata

> **Esito: nessuna delle tre colonne si aggiunge.** Punto per punto qui sotto. Le tre migration
> previste non servono, e i job nascono con quello che lo schema ha già.
**Contesto.** Applicando i valori validati dal titolare
([`docs/82-retention-data.md`](../../progettazione/82-retention-data.md)) al seed di
[`privacy_retention_policies`](privacy/privacy_retention_policies.md) sono emersi tre punti in cui la
decisione **c'è** ma lo schema non ha la colonna per applicarla. Non sono dubbi sulla politica: sono
buchi di disegno che la politica ha reso visibili.

**(1) `catalog.properties` — stato `terminated` + `terminated_at`.** Il titolare ha deciso che lo
stabilimento cessato resta pubblicato in forma ridotta (chiarimento Q3), e la cascata del
[`docs/80-retention.md` §5](../../progettazione/80-retention.md) decorre dalla cessazione. Ma l'enum
`catalog.property_status` descrive solo l'**ingresso** in piattaforma (`pending`/`approved`/`rejected`/
`imported`) e la data di cessazione vive solo in
[`identity_account_subscriptions.ends_at`](identity/identity_account_subscriptions.md), in un altro
schema. *Conseguenza se non si aggiunge*: `DeprovisionClosedProperty` interroga Identity a ogni giro per
ogni property, e "cessato" resta indistinguibile da "sospeso per abbonamento scaduto"
(`is_online_sales_suspended`) — con il rischio di **riattivare uno stabilimento chiuso** al primo
rinnovo. ~~*Raccomandazione*: aggiungere entrambi.~~

> ## ✅ **Risolta il 2026-10-02: la cessazione si DERIVA, le due colonne non si aggiungono.**
>
> Coincide con la definizione scritta in [`docs/80-retention.md` §5](../../progettazione/80-retention.md), da cui dipendono tre
> policy di retention sui file. La definizione va scritta **una volta** e condivisa, non ricalcolata in
> ogni job.
>
> **Due precisazioni misurate che mancavano al testo sopra, e che rendono la derivazione praticabile:**
>
> - la data di cessazione **non** vive solo in `account_subscriptions.ends_at`: c'è
>   `identity.account_subscription_periods` con `closed_at`, `ends_at` **e** `close_reason`
>   (`renewed | expired | cancelled | superseded`);
> - «cessato» **è già distinguibile** da «sospeso»: `account_subscriptions.status` è
>   `active | suspended | expired | cancelled`. `cancelled` è la cessazione esplicita, `expired` senza
>   rinnovo è quella di fatto, `suspended` non è né l'una né l'altra. L'obiezione «rischio di
>   riattivare uno stabilimento chiuso al primo rinnovo» quindi **non chiede una colonna qui**.
>
> Di quell'obiezione resta un problema vero, ma di natura diversa: l'**irreversibilità** del
> deprovisioning. Se si cancellano i file e poi il cliente rinnova, non si torna indietro. Si risolve
> con la coda delle cancellazioni e un periodo di grazia
> ([`privacy_file_deletions`](privacy/privacy_file_deletions.md)), non con uno stato in più.
>
> **Costo accettato**: `DeprovisionClosedProperty` interroga Identity invece di leggere una colonna
> locale. Si è preferita **una sola verità** a una copia che qualcuno deve ricordarsi di aggiornare.
> Nota aggiornata in [`catalog_properties`](catalog/catalog_properties.md).

**(2) `identity.access_users` — una data di revoca.** La policy validata è "cancella 12 mesi dopo la
revoca dell'accesso" (chiarimento Q5). Oggi l'unica data disponibile è `deleted_at`, che è il
soft-delete. *Conseguenza se non si aggiunge*: va bene **finché** la revoca si materializza sempre come
soft-delete; se nascerà uno stato "sospeso" distinto, un account sospeso ma non cancellato **non
scadrebbe mai**. > ## ✅ **Risolta il 2026-10-02: revoca ≡ cancellazione.** Nessuna colonna da aggiungere.
>
> La policy resta quella già seminata: `identity.access_users`, decorrenza **`deleted_at`**, 12 mesi,
> `delete_row` — verificata a database, è già così. Non serve `access_revoked_at`.
>
> ⚠️ **Condizione sotto cui va riaperta**: se nel nuovo gestionale nascerà uno stato «sospeso»
> distinto dalla cancellazione, un account sospeso ma non cancellato **non scadrebbe mai**. Chi
> introdurrà quello stato deve tornare qui: la decisione di oggi vale perché revocare un accesso
> *significa* cancellarlo.

**(3) `identity.customer_customers` — `blacklist_reviewed_at`.** Il titolare ha assegnato al **gestore
dello stabilimento** una revisione annuale del flag `is_blacklisted`. *Conseguenza se non si aggiunge*:
`updated_at` cambia per qualsiasi modifica del cliente, quindi il promemoria non sa distinguere "ho
riconfermato la blacklist" da "ho corretto il telefono", e ripeterebbe la domanda a chi ha appena
risposto. > ## ✅ **Deciso il 2026-10-02: si applica la logica del legacy.** Nessuna colonna da aggiungere.
>
> Nel legacy `customers.isBlackList` è un `tinyint` scritto alla creazione e all'aggiornamento del
> cliente (`customers.controller.js`), letto da `isUserBlacklisted`, e **niente altro**: nessuna data
> di revisione, nessun promemoria, nessuna scadenza. 40 righe su 224.526 ce l'hanno a 1. Il nuovo
> gestionale fa lo stesso: `is_blacklisted` resta un flag che il gestore mette e toglie a mano.
>
> **Cosa comporta, da sapere:** la **revisione annuale** che il titolare ha assegnato al gestore
> ([`docs/80-retention.md` §3.7](../../progettazione/80-retention.md),
> [`docs/82-retention-data.md`](../../progettazione/82-retention-data.md)) resta un compito umano **senza traccia
> a database**. Un eventuale promemoria potrà basarsi solo su `updated_at`, che cambia per qualsiasi
> modifica del cliente: non distinguerà «ho riconfermato la blacklist» da «ho corretto il telefono», e
> ripeterà la domanda a chi ha appena risposto. ⚠️ Per i **dati legacy** non è documentata nemmeno la
> base giuridica: `blacklist_reason` è **interamente NULL su tutte le 224.524 righe**, comprese le 40 col
> flag attivo. Nessuna policy di retention è interessata — a database non c'era e non c'è nessuna riga di
> `privacy_retention_policies` per questo flag, perché la revisione è umana e non un job.

**Perché conta.** Sono tre migration piccole, ma se non si fanno ora i job di retention nascono con
approssimazioni cablate nel codice — esattamente ciò che la tabella dichiarativa doveva evitare.

---

## K. Audit — `audit`

> Modulo nuovo deciso il 2026-10-07 ([`docs/86-modulo-audit.md`](../../progettazione/86-modulo-audit.md)): una sola
> tabella, [`audit_trail_entries`](audit/audit_trail_entries.md), registro trasversale di chi ha fatto
> cosa (operatori, eventi di sicurezza, systemadmin). Le domande sotto riguardano il trattamento dei
> dati del **personale** degli stabilimenti.

### D-66 ✅ **RISOLTA il 2026-10-13** — Per quanto tempo si conservano le azioni degli operatori?
> **Decisione: 12 mesi** (opzione a). Policy seminata con la migration `AddAuditRetentionPolicies` di Privacy,
> insieme a `security` 6 mesi e `platform` 12 mesi.
**Contesto.** La pagina «Tutti i log» mostra le azioni di ogni operatore (prenotazioni, incassi,
scontrini, modifiche). Per le altre due categorie del registro la durata è già fissata: eventi di
sicurezza **6 mesi** (validato), azioni del systemadmin **≥ 6 mesi** (Provv. Garante 27/11/2008).
**Perché conta.** È un dato personale dei lavoratori: la durata deve essere proporzionata allo scopo
(ricostruire un'operazione contestata, verificare un incasso). Senza un valore il motore di retention
non può avere la sua policy.
**Opzioni.** (a) **12 mesi**: copre una stagione intera più la successiva, utile per le contestazioni
di fine stagione; (b) 6 mesi, allineato agli eventi di sicurezza; (c) 24 mesi.
**Raccomandazione.** **(a) 12 mesi** — la stagione balneare è il ciclo naturale delle contestazioni; oltre
non serve a ricostruire un'operazione e aumenta l'esposizione. Bloccante per il seed della policy, non
per lo sviluppo.

### D-67 🟧 Informativa al personale sul registro delle attività (art. 4 Statuto dei Lavoratori)
**Contesto.** Un registro delle azioni dei dipendenti, consultabile dal titolare, è uno strumento da cui
può derivare un controllo a distanza dell'attività lavorativa (art. 4 L. 300/1970, come modificato dal
D.Lgs. 151/2015). Per gli strumenti usati per rendere la prestazione non serve l'accordo sindacale, ma
serve comunque un'**informativa adeguata** ai lavoratori sulle modalità d'uso e sui controlli.
**Perché conta.** Il titolare del trattamento è lo **stabilimento** (datore di lavoro), non la
piattaforma: SummerBooking deve fornire allo stabilimento un testo di informativa e presentare la
funzione come strumento organizzativo e di sicurezza, non di valutazione delle persone.
**Opzioni.** (a) testo di informativa standard fornito dalla piattaforma, mostrato al titolare
all'attivazione della pagina «Tutti i log»; (b) lasciare l'informativa interamente allo stabilimento.
**Raccomandazione.** **(a)**, con verifica del testo da parte di un legale. Non bloccante per lo sviluppo,
bloccante per il go-live della pagina.

---

## Risoluzione di default (regola generale applicata)

> Esito di ogni domanda rimasta aperta applicando "**priorità alle nuove specifiche; dove non
> specificato, resta il legacy**". **❓** = richiede ancora un input umano genuino (contraddizione,
> DPO/legale, dato di migrazione) · **🟨/🟧** = un default è già proposto e **non bloccante**, ma resta
> da confermare.

| # | Esito |
|---|---|
| **D-13** ❓ | Insieme canonico proposto (`proportional` / `daily` / `daily_with_weekend` / `logarithmic` + enum monthly/weekend) **si adotta**; la **tabella di mappatura** delle 6–8 varianti storiche → canonico va **validata** con chi conosce `PriceCalcService.js` prima di congelare la migrazione. `logarithmic`: verificare se ancora raggiungibile da UI. |
| **D-25** ❓ | `identity_customer_customers.note` con dati art. 9 GDPR: **richiede validazione DPO/legale** — non risolvibile con la regola generale. |
| **D-26** ❓ | Importi storici dei rinnovi abbonamento: **dipende dalla disponibilità dei dati** in migrazione — serve verifica. Default provvisorio: periodi ante-go-live con `amount = NULL`. |
| **D-33** 🟨 | Minore; da valutare quando si tocca C5. |
| **D-35** 🟨 | Non specificato → **resta il legacy**: ripartizione proporzionale mantenuta con `is_allocated = true` (l'export contabile esterno non cambia). |
| **D-36** ❓ | **Dipende dalla disponibilità dei dati** in migrazione (crediti sharing su `users` per email, senza `property_id`). Default provvisorio: ricostruzione da `sharing_details` dove possibile, saldo di apertura sulla property prevalente per i residui. |
| **D-37** 🟨 | Non specificato + dead code → **non modellato** ("pagamento multi-prenotazione"); da progettare ex novo solo se richiesto. |
| **D-43** 🟨 | `fiskaly_vat_codes.code → Natura N1–N6`: mappa proposta, **da validare col commercialista**. |
| **D-44** 🟨 | `fiskaly_receipt_sequences`: default proposto **consumo atomico** (ammette buchi); peek+commit se serve numerazione senza salti. |
| **D-45** 🟨 | Migrazione: `booking_number → reservation_public_id` non sempre risolvibile → `NULL` per gli scontrini legacy appesi/falliti. |
| **D-46** ❓ | Consenso marketing: **default proposto (a)** — consenso in C6 su `notification_marketing_contacts`, verifica all'invio, `unknown` blocca in prod. **Richiede validazione DPO/legale** — non risolvibile con la regola generale. |
| **D-47** 🟨 | Feature marginali: (1) `notification_marketing_events` **tenere**; (2) `notification_marketing_event_reminders` **dismettere** salvo richiesta (mai stata viva nel legacy); (3) `notification_establishment_claim_requests` tabella sì, workflow no. Tutte create con annotazione "candidata a dismissione". |
| **D-48** 🟨 | Non specificato → **default proposto (a)**: segnaposto template = enum/costante di codice + `.resx` + mappa di migrazione; **nessuna** tabella lookup (sarebbe EAV, `docs/03 §2.5`). |
| **D-49** 🟨 | Non specificato → **default proposto**: `notification_inapp_messages.payload jsonb` confermato (snapshot chiuso per `kind`), per analogia con l'eccezione read-model [D-28]. |
| **D-51** 🟧 | **Aggiornata 2026-09-11**: la feature **non** è "mai attivata" (è attiva/collaudata su `summer_booking_dev`, 0 righe solo su `local_sb`/dump prod). Scelta committente (2026-09-10, confermata dai fatti): **redesign pulito** come C4bis (opzione a) — 10 schede `switchboard_*` create, dominio marcato "candidata a dismissione" **solo per l'assenza di UI**; interfaccia da reperire (probabile pannello esterno) / requisiti da definire ([`da-chiarire.md` #36](../../analisi-pre/business-logic/da-chiarire.md)). Il **dubbio D.L. 7/2015** (72 mesi traffico telefonico), ora meno ipotetico, **richiede validazione legale/DPO** — non risolvibile con la regola generale. **Da chiedere al committente**: c'è un cliente reale dietro l'uso osservato, o è solo test interno? |
| **D-52** 🟨 | Non specificato → **default proposto (a)**: `components` (documento Meta), `wa_*_params` (argomenti API Meta), `ivr_path` (traccia flusso), `raw_payload`/`handler_result` (webhook di terzi) restano `jsonb` (`docs/03 §3.6 punto 5`, analogia [D-49]); `action_config` polimorfo **de-jsonizzato** in colonne. Colonne di riepilogo derivate su `components`. |
| **D-53** 🟨 | *(nuova)* Non specificato → **default proposto (a)**: `config_id`/`property_id` restano `NULL` sugli eventi del numero WhatsApp generico condiviso (attribuzione parziale, comunque un miglioramento sul legacy senza alcun collegamento); opzione (b) correlazione via `wamid` con `notification_deliveries` (C6) se serve audit affidabile per-stabilimento anche sul numero condiviso. |
| **D-54** 🟧 | Dati personali di terzi (clienti) osservati nel testo libero di `chatbot_conversation_turns`: **default proposto (a)** — retention breve/moderata (12 mesi) + accesso ristretto (property proprietaria + `systemadmin`), nessuna redazione automatica NLP per ora. **Da annotare nel registro dei trattamenti del modulo Privacy**, validazione DPO/legale consigliata. |
| **D-55** 🟨 | Non specificato → **default proposto (a)**: enum `chatbot.channel_kind` seminato fin da ora con `manager`/`customer` (quest'ultimo non ancora raggiungibile da alcun endpoint), coerente con l'intento già dichiarato nel commento del model legacy. |
| **D-56** 🟨 | Non specificato → **default proposto (a)**: `scopes` granulari **non** modellati in `infra_integration_api_clients` (nessun consumo reale oggi, `app_name`+`property_id` bastano); tabella figlia solo se un partner futuro lo richiede. |
| **D-57** 🟨 | Come [D-55]: **default proposto (a)** — enum `infra.integration_app` seminato fin da ora anche con `teamsystem` (mai attivato, 0 righe, ma già previsto dal codice `APP_NAMES`). |
| **D-58** 🟨 | Non specificato → **default proposto (a)**: `expires_at` resta opzionale sui nuovi client API partner (`NULL` = nessuna scadenza) + alert operativo sui client senza scadenza/rotazione da tempo, nessun vincolo bloccante a schema. |
| **D-59** 🟨 | Scostamento dal documento → **default proposto (a)**: testata unica `privacy_data_subject_requests.request_kind` invece di due aggregati `ErasureRequest`/`ExportRequest` distinti, per lo stesso precedente già adottato altrove nel progetto. Correzione da riportare in `docs/11-modulo-privacy.md` §2. |
| **D-60** 🟨 | Non specificato → **default proposto (a)**: enum `privacy.subject_kind` seminato fin da ora anche con `user` (self-service staff, non ancora un requisito verificato), come [D-55]/[D-57]. |
| **D-61** 🟨 | Non specificato → **default proposto (a)**: bundle di export come file su object storage privato (`export_artifact_origin`/`_path`, presigned URL a runtime) invece di `payload jsonb` inline, per dimensione imprevedibile. |
| **D-65** ✅ | **Risolta il 2026-10-02: nessuna delle tre colonne si aggiunge.** (1) la **cessazione si deriva** da `identity.account_subscription_periods.closed_at`, con `account_subscriptions.status`/`close_reason` che distinguono già cessato da sospeso — niente `terminated`/`terminated_at`; (2) **revoca ≡ cancellazione**, la decorrenza resta `deleted_at` (già seminata così) — niente `access_revoked_at`, da riaprire **solo** se nascerà uno stato «sospeso» distinto; (3) sul flag blacklist si **applica la logica del legacy** — niente `blacklist_reviewed_at`: la revisione annuale resta un compito umano senza traccia a database. |
| **D-62** ✅ | **Risolta (a)**: `privacy_retention_policies` è configurazione **globale di piattaforma** (nessun `property_id`). Non più una proposta: la finestra di retention è "uguale per tutti gli stabilimenti, nessuna variazione per piano di licenza o accordo commerciale" ([`bl/02`](../bl/02-errore-api-dati-archiviati.md)) e il titolare ha deciso i tempi come policy di piattaforma ([`docs/82-retention-data.md`](../../progettazione/82-retention-data.md)). Voce conservata qui come traccia; un eventuale `property_id` nullable per policy **più severe** resta aggiungibile senza rompere lo schema. |
