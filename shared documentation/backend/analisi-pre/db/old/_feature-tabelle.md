# Indice tabelle per feature

> File **da tenere aggiornato** ad ogni nuova scheda tabella aggiunta in questa cartella: elenco sintetico
> (nome tabella + una riga di descrizione) organizzato per feature/dominio di business, per orientarsi
> rapidamente su quali tabelle riguardano quale parte dell'applicazione. Per il dettaglio completo
> (campi, criticità, riferimenti a codice) vedere la scheda `<nome_tabella>.md` corrispondente in questa
> stessa cartella. I domini fra parentesi (C1, C2, ...) fanno riferimento alla classificazione già usata
> in [`docs/01-criticita-e-indicazioni-backend.md`](../../docs/01-criticita-e-indicazioni-backend.md),
> con due precisazioni introdotte in questa analisi: il dominio "C3/C4 — Pagamenti & Fiscale" di quel
> documento è qui **separato in C3** (`Payment` — Stripe/incassi) e **C4** (`Fiskaly` — scontrino
> **elettronico** SIGN IT); ed è aggiunto **C4bis — Stampa fiscale su hardware RT** (percorso legacy
> di stampa su registratore di cassa RT fisico, `printer_configs` + `booking_receipts` +
> `receipt_serial_numbers`; distinto sia da C3 sia da C4, candidato alla dismissione). È inoltre
> introdotto **C8 — Chatbot** (feature su un **branch separato**; nessun codice in `back-pre`).
>
> **Aggiornamento** — sono state aggiunte le schede di 9 tabelle prima **non censite** in questo indice:
> [`sharing_details`](sharing_details.md) (C1), [`contract_documents`](contract_documents.md) +
> [`contract_acceptation`](contract_acceptation.md) + [`property_like_logs`](property_like_logs.md) (C5),
> [`revenue_notifications`](revenue_notifications.md) (C2),
> [`subscribes`](subscribes.md) + [`csv_manager_details`](csv_manager_details.md) (C6),
> [`chatbot_history`](chatbot_history.md) + [`chatbot_manager_config`](chatbot_manager_config.md) (C8).

---

## Autenticazione, utenti, ruoli (C5)

- **[users](users.md)** — account che accedono al sistema (gestori, staff, superadmin, utenti consumer).
- **[roles](roles.md)** — catalogo dei 13 ruoli applicativi e dei relativi permessi.
- **[user_roles](user_roles.md)** — associazione utente↔ruolo (in pratica 1:1, mai più ruoli per utente).
- **[customers](customers.md)** — anagrafica dei clienti finali che prenotano (distinta da `users`).

### Account / contratti (sottoarea `account`)

- **[contract_documents](contract_documents.md)** — catalogo dei documenti contrattuali (addendum, es.
  "Service Fee") che lo stabilimento deve accettare; referenziato da
  [`properties`](properties.md)`.contractDocumentsId` (campo che resta in **C2**). Tabella **vuota**:
  feature completa (backend [`contractConditionService.js`](../../back-pre/app/Service/contractConditionService.js)
  + UI in **`front-modern-pre`** — modale bloccante "Condizioni Contrattuali", `GET/POST
  sb/contract-condition`) ma **mai attivata** (nessuna property ha `contractDocumentsId` valorizzato →
  `{ enabled: false }` per tutti). `front-pre`/`front-customer-pre`/`front-app-pre` non la usano.
- **[contract_acceptation](contract_acceptation.md)** — registro dell'accettazione dell'addendum **per
  stabilimento** (chi, quando, IP, hash SHA-256 del PDF accettato). 1 riga per `propertyId` (nessuno
  storico). Tabella **vuota** — scritta solo dal "Accetto e continuo" della modale di `front-modern-pre`,
  che non compare finché `contractDocumentsId` è `NULL`.

### Preferiti consumer (sottoarea `customer`)

- **[property_like_logs](property_like_logs.md)** — "mi piace" / preferiti degli stabilimenti da parte
  degli utenti consumer (app cliente `front-customer-pre` / `front-app-pre`); mantiene il contatore
  denormalizzato [`bathouses`](bathouses.md)`.likeCount`. Il "non mi piace" cancella la riga
  (`likedStatus` è di fatto sempre `1`). In alternativa collocabile in un dominio "Vetrina / Ricerca
  pubblica" insieme a [`property_search_regions`](property_search_regions.md).

## Anagrafica stabilimento

- **[properties](properties.md)** — dati amministrativi/gestionali dello stabilimento (proprietario,
  fatturazione, opzioni di prenotazione online, integrazione Fiskaly).
- **[bathouses](bathouses.md)** — dati pubblici/di presentazione dello stabilimento (nome, indirizzo,
  contatti, foto, slug, copia dei servizi selezionati).

## Catalogo & configurazione — Mappa spiaggia (C2)

- **[sectors_rows](sectors_rows.md)** — elenco dei settori/file in cui è divisa la spiaggia (1 riga JSON
  per stabilimento).
- **[configurations](configurations.md)** — stato cella-per-cella della griglia mappa spiaggia + opzioni
  della funzionalità "sharing".
- **[configure_maps](configure_maps.md)** — layout/template grafico della mappa spiaggia (dimensioni
  griglia, icone scelte), companion 1:1 di `configurations`.
- **[configure_images](configure_images.md)** — catalogo master delle icone/immagini per la mappa spiaggia
  (template, ombrellone, cabina, ecc.), con pannello di amministrazione SuperAdmin.
- **[configure_categories](configure_categories.md)** — le 5 categorie fisse in cui sono raggruppate le
  icone di `configure_images`.

## Catalogo & configurazione — Stagioni, listini, sconti (C2)

- **[seasons_periods](seasons_periods.md)** — confini stagione corrente/successiva e periodi tariffari
  (alta/bassa/media stagione) per stabilimento.
- **[price_lists](price_lists.md)** — pacchetti e articoli aggiuntivi vendibili, con prezzi per periodo;
  alimenta anche la feature "Revenue Management" (prezzo suggerito dinamico).
- **[discounts](discounts.md)** — voucher/sconti configurabili per stabilimento, con codice, validità e
  limite di utilizzi.
- **[revenue_notifications](revenue_notifications.md)** — configurazione **per stabilimento** della
  feature **Revenue Management**: interruttori del prezzo suggerito/banner sconto, numero di
  visualizzazioni giornaliere del banner sulla pagina pubblica, elenco postazioni scontate. 1 riga per
  `property_id`. "notification" nel nome è fuorviante — è configurazione (`pricing`), **non C6**; la
  logica di visibilità del banner oggi è nel frontend, da portare nel backend. Il campo `discounts`
  (JSON) è **colonna morta** (mai scritta/letta da back, cron o front; `NULL` ovunque) → non migrare.

## Catalogo & configurazione — Servizi (C2)

- **[services](services.md)** — catalogo master dei servizi selezionabili dagli stabilimenti (es. Wi-Fi,
  bar, doccia).
- **[services_types](services_types.md)** — servizi bar/normali/a ore effettivamente venduti da ciascuno
  stabilimento, con prezzi e fasce orarie.
- **[servicetypes_icons](servicetypes_icons.md)** — catalogo master delle icone per i tre tipi di servizio
  di `services_types`.
- **[service_type_filters](service_type_filters.md)** — le 6 categorie di filtro (es. "Accessibility") che
  raggruppano i servizi nella ricerca pubblica.

## Catalogo & configurazione — Magazzino (C2)

- **[warehouses](warehouses.md)** — quantità disponibili degli articoli fisici a corredo (ombrelloni,
  lettini, ecc.) per stabilimento.

## Convenzioni / Hotel — Configurazione (C2)

- **[hotels](hotels.md)** — anagrafica delle strutture partner ("convenzioni") registrate da uno
  stabilimento; non è un modulo di prenotazione camere, è figlia di `properties`.
- **[hotel_price_lists](hotel_price_lists.md)** — listini e voucher venduti da una struttura partner,
  stessa struttura di `price_lists` con l'aggiunta dei campi specifici voucher.
- **[hotel_message_templates](hotel_message_templates.md)** — template email/WhatsApp riutilizzabili per
  l'invio dei report periodici alle strutture partner (condivisi per property, non per singola struttura).

*(la parte transazionale — **[hotel_payments](hotel_payments.md)** e **[hotel_reports](hotel_reports.md)** —
è documentata nella sezione "Prenotazioni (C1)" qui sotto: sono un dettaglio dell'aggregato Booking,
non della configurazione hotel.)*

> **Collocazione tra i domini**: diverse tabelle "di pagamento/incasso" elencate qui sotto sono per
> comodità raggruppate in Prenotazioni ma vanno collocate in **C3 (Pagamenti — Stripe/incassi)**,
> **C4 (Fiskaly — scontrino elettronico)**, **C4bis (Stampa fiscale su hardware RT)** *(es.
> `booking_receipts`)* o **C6 (Comunicazione)** nella riscrittura. L'analisi tabella per tabella è in
> [`docs/03-gestione-dati.md` §4](../../docs/03-gestione-dati.md) (scritto con l'etichetta unica
> "C3 Pagamenti & Fiscale" → oggi separati).

### Struttura target C1: **testata** (`booking_number`) + **righe** (`booking_id`)

Nella riscrittura l'attuale tabella `bookings` (1 riga per postazione/intervallo/periodo, con lo
stesso `bookingNumber` ripetuto su ogni riga e decine di campi JSON duplicati) viene spezzata in due:

| | **Testata** — `bookings` (o `reservations`) | **Righe** — `booking_lines` |
|---|---|---|
| **Granularità** | 1 riga per **prenotazione logica** | 1 riga per **postazione × intervallo contiguo × `booking_period`** (= la singola riga `bookings` odierna) |
| **Chiave** | `id` PK; `booking_number` `UNIQUE` | `id` PK; `booking_line_number` `UNIQUE` (subordinato) |
| **FK** | `property_id`, `bathhouse_id`, `customer_id` (intestatario), `hotel_id?` | `booking_id` → testata; `resource_id` (postazione), `price_list_package_id` |
| **Cosa contiene** | dati **comuni a tutta la prenotazione**: intestatario, ospiti, canale (`booking_channel`), stato (`booking_status`), conferma online, note, `reservation_code`, riepilogo economico (subtotale/sconto/supplemento/totale/pagato/residuo), flag (`is_seasonal_pass`, `is_marketing_campaign`, …), `created_at`/`updated_at`/`deleted_at`, `user_operator_id` | dati **specifici della singola postazione/tratta**: `resource_id`, `start_date`/`end_date`, `booking_period`, pacchetto scelto, `line_price` (prezzo bloccato di *quella* riga), articoli aggiuntivi di riga, `fiskaly_line_item_id`, `checked_in_at`/`checked_out_at` (il check-in può essere per singola postazione), flag `is_split`/`is_area` |
| **Cosa NON contiene più** | griglie prezzo giorno-per-giorno, snapshot mappa, snapshot cliente JSON | il `booking_number`/consumer/agent/totali ripetuti su ogni riga |

**Esempio.** Il cliente Mario Rossi prenota **in un'unica operazione**: ombrellone **A12 dal 20 al 24
ago** (pacchetto "ombrellone + 2 lettini") e ombrellone **A13 dal 22 al 24 ago** (stesso pacchetto),
mezza giornata mattino, con voucher 10% e acconto 30 €.

*Testata* — **1 riga**:
```
bookings
  id                 = 90012
  booking_number     = "10795-0000012345"
  property_id        = 10795
  bathhouse_id       = 10461
  customer_id        = 209301            -- Mario Rossi (intestatario)
  booking_channel    = manager
  booking_status     = confirmed
  online_confirmation= not_required
  reservation_code   = "w9ap9gsqck"
  subtotal           = 268.00
  discount_amount    = -26.80
  surcharge_amount   =   0.00
  total              = 241.20
  paid_amount        =  30.00
  balance_due        = 211.20
  is_seasonal_pass   = false
  user_operator_id   = 3128
  created_at / updated_at / deleted_at
```

*Righe* — **2 righe** (una per postazione/tratta):
```
booking_lines
  id  booking_id  resource_id  start_date   end_date     booking_period  package                     line_price  fiskaly_line_item_id
  501 90012       A12          2026-08-20   2026-08-24    morning         "Ombrellone e 2 lettini"    145.00      "1749d3ba-..."
  502 90012       A13          2026-08-22   2026-08-24    morning         "Ombrellone e 2 lettini"     96.20      "8f3b6516-..."
```
(Se A12 avesse due tratte con periodo diverso — es. 20–21 full day + 22–24 morning — sarebbero
**3 righe**: 501a, 501b, 502.)

**Chi punta a cosa** (le tabelle satellite si agganciano al livello giusto, senza duplicare):

| Tabella | Punta a | Perché |
|---|---|---|
| `resource_consumptions` (ex `*_items_counts_details`, vedi [`docs/03-gestione-dati.md` §3.8](../../docs/03-gestione-dati.md)) | **riga** (`booking_line_id`) + `booking_number` copiato | il consumo inventario è per postazione/giorno |
| `booking_absence_details` (assenze/rivendita) | **riga** (`booking_line_id`) | l'assenza è di una specifica postazione |
| `booking_line_services` / servizi bar-normali-orari | **riga** o **testata** secondo dove sono stati aggiunti | un caffè "sulla prenotazione" → testata; un lettino extra su A13 → riga |
| `hotel_payments` (→ righe C1) | **riga** (`booking_line_id`) | la voce di listino/voucher hotel è per postazione |
| `booking_group_members` | **testata** (`booking_id`) | il gruppo raccoglie prenotazioni intere |
| `bookings_requested_details` (accesso via `reservation_code`) | **testata** | la visibilità è della prenotazione intera |
| `payment_transactions` / link Stripe / ricevute (**C3**) | **testata** (`booking_number`) | l'incasso è della prenotazione, non della singola postazione |

Regola: **il `booking_number` è la chiave di correlazione verso C3/C6 e verso tutto ciò che è "di
prenotazione"; il `booking_line_id` è la chiave verso tutto ciò che è "di postazione/giorno"
(inventario, assenze, check-in per postazione, voce hotel).** Nessun dato comune (consumer, agente,
totali, canale) va ripetuto sulle righe.

## Prenotazioni — Ombrellone / postazione (C1)

- **[bookings](bookings.md)** — aggregato-radice della prenotazione di una o più postazioni per un
  intervallo di date e una fascia oraria; 1 riga per (intervallo contiguo × `booking_period`), più
  righe con lo stesso `bookingNumber` per multi-data / split / hotel. Quasi tutto in campi JSON.
- **[booking_payments](booking_payments.md)** — riepilogo economico "1 per prenotazione" derivato dal
  JSON `bookings.booking_payments`; alimenta le viste crediti/incassi/non pagati. Resta **C1**
  (testata `booking_number`); i singoli **movimenti** dentro `paidAmountData`/`depositList` →
  **C3** (`payment_transactions`).
- **[booking_number_informations](booking_number_informations.md)** — 1 riga per `bookingNumber`
  (prenotazione **o** prenotazione servizi) che raccoglie pre-conto, consolidato, addebiti OfSale,
  spese TeamSystem e le righe fiscali; input dello scontrino elettronico Fiskaly.
- **[booking_items_counts_details](booking_items_counts_details.md)** — registro di consumo
  dell'inventario fisico (ombrelloni, lettini, …): 1 riga per (prenotazione × articolo × giorno).
- **[additional_items_counts_details](additional_items_counts_details.md)** — come sopra ma per gli
  articoli aggiuntivi scelti solo per certe date (`bookings.certainAddItemData`).
- **[services_booking_counts](services_booking_counts.md)** — contatore dei servizi bar/normali
  venduti per giorno (bar/normali; gli orari stanno altrove).
- **[hourly_service_booking_management](hourly_service_booking_management.md)** — prenotazione degli
  slot dei servizi a fascia oraria (SUP, campo, …): 1 riga per (prenotazione × servizio × slot).
- **[booking_absences](booking_absences.md)** *(legacy)* / **[booking_absence_details](booking_absence_details.md)** —
  "assenza pianificata / rivendita del posto": giorni in cui il titolare libera la postazione, che
  torna prenotabile. `booking_absence_details` è la versione attiva.
- **[sharing_details](sharing_details.md)** — registro della feature **"sharing"**: quando il titolare
  di un abbonamento/prenotazione lunga condivide la postazione nei giorni di non utilizzo, quel giorno
  torna prenotabile e il titolare matura un **credito** (`number_of_credit`/`credit_value`, accreditato
  su `users` da un cron). Strettamente legata a `booking_absence_details` (campo
  `is_sharing_coming_from_absence`). Gemella morta **`sharings`** (0 righe, nessun model), archivio
  `sharing_details_archive_03072026`. → **C1** (sottoarea rivendita/`hold`); campo `email_sent` → **C6**;
  confluisce in `resource_consumptions` ([`docs/03-gestione-dati.md` §3.8](../../docs/03-gestione-dati.md)).
- **[booking_groups](booking_groups.md)** — raggruppamento manuale di prenotazioni distinte sotto
  un'etichetta.
- **[booking_logs](booking_logs.md)** — malgrado il nome, è il **contatore progressivo del numero
  prenotazione per stabilimento** (dovrebbe essere 1 riga per property; bug → ~3).
- **[bookings_requested_details](bookings_requested_details.md)** — quali utenti hanno aperto una
  prenotazione tramite `reservation_code` (visibilità cliente delle prenotazioni condivise).
- **[hotel_payments](hotel_payments.md)** — parte transazionale delle Convenzioni/Hotel: 1 riga per
  (prenotazione × postazione × voce di listino/voucher hotel) con snapshot del prezzo; scritta dal
  flusso `booking` quando `bookings.hotelId` è valorizzato. **Di confine**: le righe
  (voce hotel × postazione) → **C1** (`booking_id`) + lo snapshot prezzo dell'aggregato; l'importo /
  stato pagamento / metodo → **C3**.
- **[hotel_reports](hotel_reports.md)** — report periodico arrivi/partenze/ricavi per una struttura
  partner, generato dal gestionale e inviato all'hotel (aggrega `hotel_payments` + `bookings`).
  → **C2** (artefatto della gestione convenzioni) + **C6** (invio PDF via email/WhatsApp).

> **Tabelle di pagamento/incasso spostate in altri domini** (erano storicamente elencate qui, ma
> **non** appartengono a C1 — vedi [`docs/03-gestione-dati.md` §4](../../docs/03-gestione-dati.md)):
> **→ C3** (sezione "Pagamenti — gateway & incassi"):
> [`online_transaction_details`](online_transaction_details.md),
> [`refund_transaction_details`](refund_transaction_details.md),
> [`transactions_payments`](transactions_payments.md),
> [`card_transactions`](card_transactions.md),
> [`bookings_send_payment_summaries`](bookings_send_payment_summaries.md) *(+ C6 per il canale di
> invio del link)*.
> **→ C4bis (Stampa fiscale su hardware RT)**: [`booking_receipts`](booking_receipts.md).

## Prenotazioni — Servizi (C1)

- **[service_reservation](service_reservation.md)** — aggregato della prenotazione di **soli servizi**
  (bar / normali / a ore) senza postazione; struttura simile a `bookings`, condivide `bookingNumber`,
  `booking_number_informations`, i contatori e i pagamenti.
- **[service_reservation_payments](service_reservation_payments.md)** — riepilogo economico "1 per
  prenotazione servizi", gemella di `booking_payments`. Come quella: testata → **C1**, movimenti →
  **C3**.

## Prenotazioni — Riserva postazione (C1)

- **[location_reservations](location_reservations.md)** — segna una postazione come "riservata"
  (blocco morbido) per un intervallo, senza creare una prenotazione (manutenzione, VIP, mappa);
  1 riga per (postazione × giorno).
- **[online_reservations](online_reservations.md)** — *(in realtà C2)* configurazione del canale
  "prenotazione online" per stabilimento (on/off, restrizioni mappa/periodo/location); sovrapposta a
  `properties.onlineBookingOptions` / `properties.paymentOptions`. Contiene colonne carta in chiaro
  da eliminare.
- **[hold_locations](hold_locations.md)** — *(feature incompleta)* blocco temporaneo di una
  postazione durante il checkout: modello + endpoint CRUD ma **nessun consumatore** e nessuna
  enforcement; 0 righe.
- **[parent_bookings](parent_bookings.md)** — *(dead code)* scheletro per un "pagamento genitore"
  multi-prenotazione mai implementato; 0 righe, unico riferimento è una variabile mal scritta.
- **[booking_areas](booking_areas.md)** — *(non utilizzata)* doveva contenere il conteggio posti per
  "area"; il writer non è mai invocato e i reader usano `bookings.areaDetails`; 0 righe.

### Prenotazioni — tabelle **legacy / archivio** (non documentate singolarmente, da NON migrare)

- **`booking`** — vecchia tabella (il model `"booking"` per un errore di `sequelize.define` mappa
  invece `bookings`); 0 righe.
- **`bookings_old`** — 6 righe, ultimi dati 2021.
- **`bookings_archive_03072026`** — snapshot/archivio del 03-07-2026.
- **`booking_items_counts`** — vecchio contatore inventario a colonne fisse
  (`umbrella_counts`, `sunbed_counts`, …); ultimi dati 2021.
- **`booking_items_counts_details_news`** — tabella scratch di migrazione (model
  `"booking_items_counts_details_new"` → pluralizzato); popolata solo dall'endpoint
  `databaseChanges`; 0 righe.
- **`booking_items_counts_details_archive_03072026`**, **`booking_absence_details_archive_03072026`**,
  **`sharing_details_archive_03072026`** — archivi datati.
- **`bookin-absences`** — tabella orfana (nome con trattino, nessun model); 91 righe, ultimi dati
  2022.
- **`sharings`** — precursore orfano di [`sharing_details`](sharing_details.md) (0 righe, nessun model;
  `booking_id`/`number_of_credit`/`credit_value` come `varchar`); da non migrare.
- **`location_reservations_old`** — 0 righe.
- **`beacharound_reservation`** — 100 righe ma **nessun riferimento nel codice** (back o front):
  tabella di import da integrazione esterna ("BeachAround"), estranea al codice `back-pre`.

## Pagamenti — gateway & incassi (C3)

> Dominio **C3 (Payment)**: gateway di pagamento (Stripe Connect), incassi/transazioni online,
> rimborsi, borsellino prepagato, movimenti contabili, webhook, revenue/reporting. Corrisponde al
> modulo **Payment** di
> [`docs/pre/architettura-evolutiva-microservizi.md` §2.2](../../docs/pre/architettura-evolutiva-microservizi.md)
> / [`docs/pre/linee-guida-implementative-dotnet.md`](../../docs/pre/linee-guida-implementative-dotnet.md)
> (`Modules/Payment/`). Analisi tabella per tabella in
> [`docs/03-gestione-dati.md` §4](../../docs/03-gestione-dati.md).
>
> **Di confine con C1** (restano in "Prenotazioni" come **testata**, i soli *movimenti* sono C3):
> [`booking_payments`](booking_payments.md), [`service_reservation_payments`](service_reservation_payments.md)
> — riepilogo economico "1 per prenotazione"; [`hotel_payments`](hotel_payments.md) — le righe
> (voce hotel × postazione) sono C1, l'importo/stato pagamento è C3.

### Configurazione gateway — Stripe Connect

- **[stripe_configrations](stripe_configrations.md)** — id dell'account Stripe Connect del venditore
  per stabilimento (1 riga per `propertyId`, solo per convenzione). Abilita i pagamenti online delle
  prenotazioni; la sua cancellazione spegne `properties.serviceOnlineBooking`.
- **[user_stripe_card_details](user_stripe_card_details.md)** — mappatura utente → Stripe Customer id
  per le carte salvate del cliente. **Feature di fatto non operativa** (model disallineato dallo
  schema: manca la colonna `propertyId`; tabella vuota).

### Transazioni & rimborsi online — Stripe

- **[online_transaction_details](online_transaction_details.md)** — dettaglio delle transazioni
  Stripe (Connect) per i pagamenti online di prenotazioni/servizi (pagato/hold/pending, commissione,
  stato Stripe). Verso C1 solo la FK. *(oggi sotto "Prenotazioni (C1)" per continuità storica.)*
- **[refund_transaction_details](refund_transaction_details.md)** — rimborsi Stripe eseguiti sulle
  transazioni online (0 righe nel campione).
- **[bookings_send_payment_summaries](bookings_send_payment_summaries.md)** — link di pagamento Stripe
  ("riepilogo pagamento") inviati al cliente per saldare/confermare una prenotazione. **Da spezzare**:
  il *fatto* "richiesto pagamento con link, importo X, esito Z" → **C3**; il *canale* di invio
  (`method_flag`, `email_sent` = email/WhatsApp) → **C6**.

### Movimenti / ledger contabile

- **[transactions_payments](transactions_payments.md)** — ledger normalizzato per riga-articolo e
  movimento (`deposit`/`payment`), con net/IVA separati; scritto **solo** per le property in
  allow-list (`API_TRANSACTIONS_PROPERTYID_CSV`), per un'integrazione esterna. Prototipo della
  tabella `payment_transactions` di C3; verso C1 solo `booking_id`/`service_reservation_id`.
- **[card_transactions](card_transactions.md)** — movimenti della carta/tessera prepagata del cliente
  (ricariche/consumi); mezzo di pagamento alternativo, opzionalmente legato a una prenotazione.
  → `customer_wallet_transactions` (C3); in C1 resta solo, se serve, il riferimento "questa
  consumazione è stata pagata con carta X".

### Integrazioni POS esterne (spese/addebiti sul conto prenotazione)

- **[teamsystem_expenses](teamsystem_expenses.md)** — spese (scontrini del POS **TeamSystem / Cassa
  in Cloud**) associate a una prenotazione, poi specchiate in
  [`booking_number_informations`](booking_number_informations.md) (`teamsystem_payment`). Idempotenza
  su `(beachId, idRef)`. È una voce **di pagamento/addebito** (come `ofsale_payments` per il POS
  OfSale), non un documento fiscale: la fiscalizzazione è solo **consultata** in lettura su
  [`fiskaly_receipt`](fiskaly_receipt.md) (C4). **Tabella vuota** (disallineamento di tipo su `idRef`).
  Autenticazione API via [`keysecret`](keysecret.md).

## Scontrino fiscale elettronico — Fiskaly SIGN IT (C4)

> Dominio **C4 (Fiskaly)**: lo **scontrino fiscale elettronico** italiano (documento commerciale
> online via API Fiskaly SIGN IT). Corrisponde al modulo **Fiskaly** di
> [`docs/pre/architettura-evolutiva-microservizi.md` §2.2](../../docs/pre/architettura-evolutiva-microservizi.md)
> / [`docs/pre/linee-guida-implementative-dotnet.md`](../../docs/pre/linee-guida-implementative-dotnet.md)
> (`Modules/Fiskaly/`). Il legame verso la prenotazione (C1) e verso l'incasso (C3) passa solo per
> `booking_number` / gli importi snapshot; nessuna FK cross-modulo.
> **La stampa su registratore di cassa RT hardware è un dominio *diverso*** — sezione a parte qui
> sotto. *(La §4 di [`docs/03-gestione-dati.md`](../../docs/03-gestione-dati.md), scritta con la
> vecchia etichetta unica "C3 Pagamenti & Fiscale", cita `fiskaly_*` sotto "C3".)*

- **[fiskaly_account](fiskaly_account.md)** — account dello stabilimento presso Fiskaly: accettazione
  termini, id/stati delle 4 entità Fiskaly (Organization/Subject/Taxpayer/Location), credenziali API
  e credenziali Fisconline (AdE). 1 riga per `property_id`. Contiene segreti.
- **[fiskaly_cash_register](fiskaly_cash_register.md)** — registratori di cassa (*System*
  `FISCAL_DEVICE`) registrati su Fiskaly; almeno uno "software" auto-creato. N righe per
  `fiskaly_account`. Ha `deleted_at`.
- **[fiskaly_receipt](fiskaly_receipt.md)** — 1 riga per documento commerciale elettronico emesso
  (flusso INTENTION→TRANSACTION→PDF→S3 in 10 step, con *resume*); annullamento = stato `CANCELLED` +
  CANCELLATION. Legata alla prenotazione via `booking_number`. Fatto fiscale storico (snapshot).
- **[fiskaly_non_fiscal_receipt](fiskaly_non_fiscal_receipt.md)** — pre‑conto / ricevuta NON fiscale
  (solo PDF, nessuna firma Fiskaly); numero mostrato `"NF-<id>"`.
- **[fiskaly_receipt_log](fiskaly_receipt_log.md)** — log append-only di ogni transizione/errore del
  flusso scontrino (diagnostica del dialogo con l'API SIGN IT).
- **[fiskaly_receipt_sequence](fiskaly_receipt_sequence.md)** — contatore progressivo giornaliero del
  numero documento per cassa: 1 riga per (`system_id` × `date`), `UNIQUE`.

## C4bis — Stampa fiscale su hardware RT *(legacy)*

> **Feature `C4bis`** (etichetta introdotta in questa analisi): **non** è C3 (Payment/Stripe) né
> C4 (Fiskaly/SIGN IT). È il **meccanismo alternativo** — più vecchio — per emettere lo scontrino
> fiscale: la **stampa su registratore di cassa RT fisico**, gestita quasi interamente **lato
> client** (adapter Epson TM/FP, RCH in `front-pre/src/services/printers/`) con **esattamente 3
> tabelle** di supporto lato backend, tutte alimentate da
> [`printerConfig.controller.js`](../../back-pre/app/controllers/printerConfig.controller.js)
> (endpoint `add-printer`, `update-printer`, `update-printer-status`, `delete-printer`,
> `get-config-printer`, `save-receipt-data`, `get-receipt-data`). Nessun'altra tabella coinvolta.
> Nel target: componente separato (es. `FiscalPrinting`) **oppure dismesso** se lo scontrino
> elettronico Fiskaly (C4) copre tutti gli stabilimenti.

- **[printer_configs](printer_configs.md)** — configurazione delle stampanti (termiche/fiscali RT)
  di uno stabilimento: rete (IP/porta), `model` (FP-81/FP-90/RCH/TM-T88/TM-T20/ONLINE → adapter
  dedicato lato frontend), `mode` (`fiscal`/`nonfiscal`/`orders`), reparti IVA, intestazioni/footer.
- **[booking_receipts](booking_receipts.md)** — log delle stampe di scontrino/comanda su stampante
  termica RT (spesso non legato a una prenotazione: `bookingId` per lo più `NULL`).
  *(oggi elencata sotto "Prenotazioni (C1)" per continuità storica.)*
- **[receipt_serial_numbers](receipt_serial_numbers.md)** — contatore del numero progressivo dello
  scontrino stampato su stampante RT, per stabilimento/giorno. **Non incrementa mai** per due bug
  (sempre `"001"`).

## Comunicazione (C6)

> Dominio **C6 (Comunicazione)** — modulo `Notification` di
> [`docs/01-criticita-e-indicazioni-backend.md` §8](../../docs/01-criticita-e-indicazioni-backend.md):
> notifiche in-app, invio email/WhatsApp/push, template, liste contatti e marketing (eventi, campagne
> email, automazioni). **Non** è un dominio da una tabella. Analisi degli enumeratori in
> [`docs/03-gestione-dati.md` §2.4quater](../../docs/03-gestione-dati.md).
>
> A livello di **soli campi** (non tabelle intere) confluiscono qui i flag "canale + inviato" oggi
> sparsi in altri aggregati: [`bookings_send_payment_summaries`](bookings_send_payment_summaries.md)
> (`method_flag`, `email_sent`), [`fiskaly_receipt`](fiskaly_receipt.md) /
> [`fiskaly_non_fiscal_receipt`](fiskaly_non_fiscal_receipt.md) (`emailSent`, `whatsappSent`), la
> parte "invio" di [`hotel_reports`](hotel_reports.md), `sharing.email_sent`,
> [`users`](users.md)`.disableNotification`.

### Notifiche in-app

- **[notification_details](notification_details.md)** — centro notifiche in-app "per stabilimento"
  (campanella del pannello gestionale): 1 riga per evento operativo (prenotazione creata, pagamento
  ricevuto, errore scontrino elettronico). Feed **per `property_id`**, non per destinatario; `type`
  1 = prenotazione ([bookings](bookings.md)) / 2 = servizi ([service_reservation](service_reservation.md)),
  `status` = flag di visibilità (2 = nascosta finché non pagata). Testo concatenato a mano in 6+ punti.

### Canale email

- **[email_logs](email_logs.md)** — outbox + log invii email (61 k righe). In pratica **di solo
  append**: l'unico consumatore (cron newsletter in `cronjob/news-letter.js`) è **disattivato** in
  `server.js`; `status` di fatto sempre `1`, `cronStatus` colonna morta. Nessun log dei fallimenti.
- **[bathhouse_email_configrations](bathhouse_email_configrations.md)** — config SMTP in uscita per
  stabilimento (`fromEmail`, `smtpServer/Port/Username/Password` **in chiaro**, `smtpSecurity`). 1 sola
  riga nei dati, per giunta `status = 0` (disattivata) → oggi tutti spediscono dal mittente globale.
  Sorella legacy `email_configrations` (0 righe, nessun model) da eliminare.
- **[custom_templates](custom_templates.md)** — template email (`type 1`) / WhatsApp (`type 2`)
  riutilizzabili per il "riepilogo prenotazione" al cliente; placeholder `@token` sostituiti **lato
  client**. Per WhatsApp l'invio è solo un link `wa.me` (nessuna persistenza).

### Marketing — contatti

- **[contact_list](contact_list.md)** — liste/segmenti di clientela per campagne e automazioni;
  1 lista `isDefaultList` per property ("Marketing", auto-iscrizione da prenotazione se
  `bookings.isMarketingCampaign`). Route **senza auth**; delete fisico.
- **[contact_list_member](contact_list_member.md)** — ponte lista↔[`customers`](customers.md)
  (`contactId` = `customers.id`, nome fuorviante). **73 % di righe orfane**, `AUTO_INCREMENT` ~231 k
  per 7,6 k righe (destroy+recreate a ogni modifica lista). `status` colonna morta.
- **[subscribes](subscribes.md)** — lista iscrizioni **newsletter** dal sito pubblico (solo `email` +
  `status`). **Scrittura viva** (`POST /api/sb/createSubscribe`) da **front-pre**, **front-customer-pre**
  e **front-app-pre** (input newsletter in home) + auto-insert ad ogni signup utente e ad ogni creazione
  stabilimento → **popolata anche in produzione**. **Ma write-only**: `getSubscribe`/`deleteSubscribe`
  non hanno alcun chiamante frontend, **nessun job** la legge (cron newsletter tutto commentato e
  comunque non la referenzia). Lista **globale** senza `property_id` → da unificare con
  `contact_list`/`contact_list_member` nella riscrittura.
- **[csv_manager_details](csv_manager_details.md)** — lead **"Sei il gestore? Richiedi la tua pagina"**:
  form sulla pagina pubblica di dettaglio di un bagno (`front-customer-pre` `PropertyContact.js`,
  `front-app-pre` `PropertyListContract.js`) con cui un titolare chiede di rivendicare/ottenere la
  propria scheda gestionale. `POST /api/sb/createCsvOwnerDetails` → salva nome/telefono/email/messaggio
  (`bathhouseId` **non persistito**) e invia email a `info@summerbooking.it` (oggetto "Richiesta
  struttura"), log in `email_logs`. Nome legacy fuorviante (`db.csv_owner`, ex import CSV gestori). 12
  righe (2023→2026). Candidata a CRM esterno o a invio email senza persistenza.

### Marketing — eventi & campagne

- **[marketing_event](marketing_event.md)** — eventi/iniziative dello stabilimento (nome, periodo,
  copertina); `repeatType` espanso in **una riga per occorrenza**. `showOnEstablishmentPage` scritto
  ma **mai usato per filtrare** il carosello pubblico.
- **[marketing_event_notification](marketing_event_notification.md)** — notifiche pianificate di un
  evento (`1_day_before` / `custom` / …). **Tabella vuota, feature incompleta**: nessun mittente,
  `status` mai diverso da `pending`.
- **[marketing_email_campaign](marketing_email_campaign.md)** — campagna email one-shot pianificata;
  cron `campaignEmail` ogni minuto. Destinatari da snapshot JSON `recipientList` (la `contactListId`
  con FK reale è decorativa per l'invio). Ignora l'SMTP della property.
- **[marketing_automation](marketing_automation.md)** — messaggi automatici a trigger
  (`before`/`after`/`custom` su prenotazione; `pendingpayments`, …) + push FCM opzionale. Cron
  `automationEmail` ogni 5 min; **Welcome/Goodbye auto-generate escluse per nome** (inerti); `state`
  mai portato a `sent`.

*(`revenue_notifications` ha "notification" nel nome ma è **configurazione di Revenue Management**
— banner sconto sulla pagina pubblica — non messaggistica: **non è C6**, va in **C2** sottoarea
`pricing`. Scheda dedicata: [`revenue_notifications`](revenue_notifications.md), elencata sopra in
"Catalogo & configurazione — Stagioni, listini, sconti (C2)".)*

## Switchboard / Centralino (C7)

> Dominio **C7 (Switchboard)** — modulo `Switchboard` di
> [`docs/01-criticita-e-indicazioni-backend.md` §9](../../docs/01-criticita-e-indicazioni-backend.md):
> centralino telefonico virtuale per stabilimento su **Telnyx** (numero PSTN geografico, IVR ad
> albero con audio prompt, inoltro a un endpoint SIP softphone) + canale **WhatsApp Business** via
> Meta Cloud API (template + messaggi + webhook). Pilotato da un **pannello di back-office esterno**
> (i commenti nel controller: *"BACKOFFICE API (FOR PYTHON APP)"*) e dai webhook Telnyx/Meta: **nessuna
> UI** in nessuno dei frontend presenti (`front-pre`/`front-modern-pre`/`front-customer-pre`/
> `front-app-pre`); "centralino virtuale" compare solo come voce marketing nella pagina pubblica delle
> funzionalità. **Tutte le 8 tabelle hanno 0 righe** in `local_sb` (`AUTO_INCREMENT = 1`, create il
> 2026-07-27): feature cablata nel codice (`switchboard.controller.js` ~3 030 righe,
> `switchboardService.js`, helper `telnyx`/`whatsapp`/`sip`) ma mai esercitata su questa base dati.
> A differenza del resto del legacy, il dominio usa **FK InnoDB reali**. Analisi enumeratori in
> [`docs/03-gestione-dati.md` §2.4quinquies](../../docs/03-gestione-dati.md).

### Voce (chiamate / IVR)

- **[switchboard_config](switchboard_config.md)** — configurazione centralino, **1 riga per property**
  (`property_id` UNIQUE, FK). Numero Telnyx + id oggetti Telnyx (phone number, connection, call
  control app, messaging profile), credenziali SIP (`sip_username` = `sb0<propertyId>`, `sip_password`
  **in chiaro**), numero WhatsApp custom, audio di benvenuto/fallback (FK), modalità caller ID di
  inoltro (`forward_caller_id_mode`), token push mobile per il "wake" SIP (`deviceFcmToken`/
  `deviceVoipToken`, in camelCase), ~20 campi `reg_*` per i requisiti regolatori dell'ordine numero
  IT. `is_active` è l'interruttore del centralino.
- **[switchboard_menu_item](switchboard_menu_item.md)** — voci dell'IVR ad albero (auto-referenza
  `parent_id`, FK verso config e audio). `dtmf_key` = tasto premuto (`[0-9*#]`), `label`,
  `audio_file_id`, `position` (solo UI), `is_active` (filtrato solo a runtime). L'IVR naviga con
  `findOne(config_id + dtmf_key + parent_id + is_active=1)`.
- **[switchboard_menu_action](switchboard_menu_action.md)** — azioni eseguite quando si seleziona una
  voce, ordinate per `execution_order`. `action_type` **`ENUM('hangup','forward_sip','send_whatsapp')`**;
  `action_config` JSON (per `send_whatsapp`: `templateName`/`recipientPhone`/`language`/
  `bodyParameters`; per `forward_sip` il contratto documentato è **morto**, il runtime prende tutto da
  `switchboard_config`).
- **[switchboard_audio_file](switchboard_audio_file.md)** — prompt audio (MP3) su S3 **privato**,
  chiave `audio/switchboard/<propertyId>/<uuidv7>.mp3`, ≤ 3 MB. Proxy pubblico
  `/api/public/switchboard/audio/:id` per Telnyx. `s3_bucket` e `mime_type` (`audio/mpeg` costante)
  scritti ma mai riletti.
- **[switchboard_call_register](switchboard_call_register.md)** — registro minimale "chi mi ha
  chiamato" (`from`/`to`/`datetime`): 1 riga per B-leg SIP inoltrato. Unico lettore: endpoint
  pubblico `sip/call-history` (auth con `sip_username`+`sip_password` in chiaro, no rate limiting).
- **[switchboard_call_log](switchboard_call_log.md)** — log diagnostico del flusso chiamata (eventi
  Telnyx `call.*` + passi interni `step.*`, `raw_payload`/`handler_result` JSON). **Disabilitato di
  default** (`SB_CALL_LOGGING_ENABLED`), **nessun lettore** → non un dato di dominio.

### Messaging (WhatsApp)

- **[switchboard_wa_template](switchboard_wa_template.md)** — template WhatsApp Business per property
  (FK), creati **prima su Meta** poi salvati; `category` (`UTILITY`/`MARKETING`/`AUTHENTICATION`),
  `components` JSON formato Meta, `meta_status` **`ENUM('PENDING','APPROVED','REJECTED')`** (troppo
  stretto: Meta ne restituisce altri). **Non governa l'invio** (che passa un `templateName` libero).
- **[switchboard_wa_msg_log](switchboard_wa_msg_log.md)** — log forensico dei webhook Meta
  (`event_type` `status`/`message_in`, stato consegna, pricing/conversation, messaggi in arrivo,
  `raw_payload`). Webhook POST **senza verifica firma** `X-Hub-Signature-256`; **nessun lettore** →
  non un dato di dominio (semmai outbox del modulo `Notification`).

## Chatbot (C8) — *feature su branch separato*

> Dominio **C8 (Chatbot)** — assistente/chatbot FAQ per l'app gestionale, sviluppato in un **branch a
> parte**: **nessun codice in `back-pre`** né nella solution [`backend/`](../../backend/) fa riferimento
> a queste tabelle (le schede derivano da schema + dati). I componenti `ChatBot/` di `front-pre` /
> `front-customer-pre` sono un widget assistente-prenotazione **a regole** (con date picker), cosa
> diversa da questa feature.

- **[chatbot_history](chatbot_history.md)** — log delle conversazioni domanda/risposta col chatbot
  (`input_text` / `output_text`), per `property_id` e `created_by` (utente); campo `app` (`"manag"`).
  URL di immagini FAQ incapsulati `[[[ ... ]]]` nel testo di risposta. 26 righe (demo, `property_id` 96).
- **[chatbot_manager_config](chatbot_manager_config.md)** — configurazione per stabilimento del chatbot
  gestionale: `max_call_per_day`, `max_returned_history`. 1 riga (demo).

## Credenziali integrazioni partner (INFRA)

- **[keysecret](keysecret.md)** — catalogo credenziali API delle integrazioni partner:
  `apiKey`/`secret` (in chiaro) + `params` JSON di scoping (`property_id`). Alimenta i flussi
  OAuth/HMAC server-to-server per `OfSale` e `TeamSystem` (POS → spese/incassi sul conto
  prenotazione, **C3**) e `Transactions` (export transazioni giornaliere, **C3**). Da spostare in un
  **secret manager** / IdP — vedi scheda.

## Ricerca pubblica

- **[property_search_regions](property_search_regions.md)** — tabella per una funzionalità di
  "destinazioni in evidenza" risultata di fatto non collegata alla home page pubblica (che usa un elenco
  hardcoded) — verificare se dismettere o riattivare nella riscrittura.

## Dati di riferimento globali

- **[country](country.md)** — elenco (non esaustivo) di 11 nazioni selezionabili in alcuni form.
- **[licenses](licenses.md)** — i 2 piani di abbonamento disponibili (basic/advanced); non collegata da
  alcuna relazione reale alla tabella `users`.

---

## Domini non ancora coperti da una scheda in questa cartella

- **Comunicazione (C6)** — coperta: notifiche in-app, canale email
  ([`email_logs`](email_logs.md), [`bathhouse_email_configrations`](bathhouse_email_configrations.md),
  [`custom_templates`](custom_templates.md)) e marketing ([`contact_list`](contact_list.md),
  [`contact_list_member`](contact_list_member.md), [`marketing_event`](marketing_event.md),
  [`marketing_event_notification`](marketing_event_notification.md),
  [`marketing_email_campaign`](marketing_email_campaign.md),
  [`marketing_automation`](marketing_automation.md)), più
  [`subscribes`](subscribes.md) (newsletter, write-only) e
  [`csv_manager_details`](csv_manager_details.md) (lead "richiedi la tua pagina"). Manca il canale
  **WhatsApp/Twilio** lato `email.helper.js`/`sendWhatsappTo` (non c'è tabella dedicata).
- **Switchboard/Centralino (C7)** — **coperta**: [`switchboard_config`](switchboard_config.md),
  [`switchboard_menu_item`](switchboard_menu_item.md),
  [`switchboard_menu_action`](switchboard_menu_action.md),
  [`switchboard_audio_file`](switchboard_audio_file.md),
  [`switchboard_wa_template`](switchboard_wa_template.md),
  [`switchboard_call_register`](switchboard_call_register.md),
  [`switchboard_call_log`](switchboard_call_log.md),
  [`switchboard_wa_msg_log`](switchboard_wa_msg_log.md) — tutte a 0 righe.
- **INFRA** — solo [`keysecret`](keysecret.md) coperta (credenziali integrazioni partner); il resto no.
- **Chatbot (C8)** — coperta da [`chatbot_history`](chatbot_history.md) e
  [`chatbot_manager_config`](chatbot_manager_config.md), ma è una feature di un **branch separato**:
  nessun codice `back-pre`/`backend` da tracciare, schede basate su schema + dati.
- **Revenue Management** — [`revenue_notifications`](revenue_notifications.md) ora coperta (collocata in
  C2, sottoarea `pricing`); [`price_lists`](price_lists.md) alimenta la stessa feature.
- **Account / contratti (C5)** — [`contract_documents`](contract_documents.md) +
  [`contract_acceptation`](contract_acceptation.md) coperte (tabelle vuote; feature completa lato
  backend **e** lato `front-modern-pre`, ma non attivata per alcuna property → 0 righe anche in prod).
