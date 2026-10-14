# Tabella `booking_reservations`

> **Dominio**: C1 — `booking` · **sottoarea**: — (nucleo) · **schema**: `booking` · **nome flat**:
> `booking_reservations` · **forma canonica**: `booking.reservations`.
>
> **Testata unica** dell'aggregato Prenotazione. Sostituisce la parte "comune a tutta la prenotazione"
> delle legacy [`bookings`](../../../analisi-pre/db/bookings.md) (prenotazione di postazioni) **e**
> [`service_reservation`](../../../analisi-pre/db/service_reservation.md) (prenotazione di soli
> servizi), discriminate da `reservation_kind`. Assorbe il **riepilogo economico** oggi in
> [`booking_payments`](../../../analisi-pre/db/booking_payments.md) /
> [`service_reservation_payments`](../../../analisi-pre/db/service_reservation_payments.md) (colonne su
> questa testata — [`docs/03-gestione-dati.md` §4](../../../progettazione/03-gestione-dati.md)).
>
> Fonti: [`analisi-pre/db/bookings.md`](../../../analisi-pre/db/bookings.md),
> [`analisi-pre/db/service_reservation.md`](../../../analisi-pre/db/service_reservation.md),
> [`analisi-pre/db/booking_payments.md`](../../../analisi-pre/db/booking_payments.md),
> [`analisi-pre/business-logic/stabilimento/01-mappa.md`](../../../analisi-pre/business-logic/stabilimento/01-mappa.md),
> [`analisi-pre/business-logic/stabilimento/_motore-di-prezzo-e-sconti.md`](../../../analisi-pre/business-logic/stabilimento/_motore-di-prezzo-e-sconti.md),
> [`analisi-pre/business-logic/cliente-finale/04-prenotazione-postazioni.md`](../../../analisi-pre/business-logic/cliente-finale/04-prenotazione-postazioni.md),
> [`docs/03-gestione-dati.md` §2.4bis, §3.6, §3.7, §4, §6](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga per prenotazione logica** (un solo `booking_number`), qualunque sia il numero di postazioni,
tratte di date o servizi che contiene. Nel legacy la stessa prenotazione produceva **N righe** in
`bookings` (una per postazione × intervallo contiguo × fascia oraria) con `bookingNumber`, dati cliente,
agente e totali **ripetuti**; qui quei dati stanno una volta sola qui, e il dettaglio per
postazione/tratta è in [`booking_lines`](booking_lines.md), i servizi in
[`booking_service_lines`](booking_service_lines.md).

`reservation_kind` distingue i due canali storici:

- `spot` — prenotazione che include almeno una postazione (ex `bookings`); ha righe in `booking_lines`.
- `service` — prenotazione di **soli servizi**, senza postazione (ex `service_reservation`); nessuna
  riga in `booking_lines`, solo `booking_service_lines`.

Le colonne "spot-only" (`has_seasonal_pass`, `is_split`, `agreement_partner_id`, `listing_type`,
`price_flags`) sono `NULL`/vuote per le righe `service`; `pay_now` è usato soprattutto dal canale
`service`/online. È la scelta di testata unica confermata dal committente (vedi
[`_feature-tabelle.md`](../_feature-tabelle.md) §"Decisioni del committente — C1").

Tabelle figlie / satellite (tutte con FK singola `reservation_id` → questa):

| Aspetto | Tabella |
|---|---|
| postazione × tratta contigua × fascia | [`booking_lines`](booking_lines.md) |
| articoli aggiuntivi di riga / per certe date | [`booking_line_additional_items`](booking_line_additional_items.md) |
| servizi bar/normali/orari venduti | [`booking_service_lines`](booking_service_lines.md) (+ [`_time_slots`](booking_service_time_slots.md)) |
| voucher/sconti applicati (snapshot) | [`booking_discounts`](booking_discounts.md) |
| ospiti (non intestatari) | [`booking_guests`](booking_guests.md) |
| account consumer che hanno aperto la prenotazione col codice | [`booking_participants`](booking_participants.md) |
| date "azioni rapide" del planner | [`booking_quick_action_dates`](booking_quick_action_dates.md) |
| consumo inventario / occupazione per data | [`booking_resource_consumptions`](booking_resource_consumptions.md) |
| voci listino/voucher di una convenzione (partner) | [`booking_agreement_lines`](booking_agreement_lines.md) |
| appartenenza a un gruppo | [`booking_group_members`](booking_group_members.md) |
| giorni ceduti alla rivendita/sharing | [`booking_resale_offers`](booking_resale_offers.md) |

Correzioni rispetto al legacy:

- **`bookingByAdmin` `"0"`/`"1"` (varchar) → `enum booking_channel`** (`customer` / `manager`).
- **`status` `tinyint` sovraccarico (0/1/2/3) → `enum reservation_status`**, separato dal soft-delete
  (`deleted_at`) e dalla "guardia anti-doppia prenotazione" (che diventa un lock reale, vedi
  [`booking_resource_consumptions`](booking_resource_consumptions.md) §3.9).
- **`is_booking_confirmed` (semantica invertita, mai avanzato) → `enum online_confirmation_state`**
  (`not_required` / `pending` / `confirmed`) che **avanza davvero** alla conferma via codice.
- **`booking_agent` (stringa id, dal body, immutabile) → `operator_user_id uuid`** dal token JWT
  ([`docs/03-gestione-dati.md` §3.2](../../../progettazione/03-gestione-dati.md)).
- **`booking_consumer` / `guest_data` (JSON snapshot cliente) → FK viva `customer_id`** (C5) +
  [`booking_guests`](booking_guests.md); snapshot tipizzato solo sui fatti fiscali (C4).
- **`bookingNumber` fragile (concatenazione + `parseInt+1`, non unico) → `booking_number` generato
  server-side** con [`booking_number_sequences`](booking_number_sequences.md), zero-padding ampio,
  `UNIQUE`.
- **`reservation_code` (`Math.random().toString(36)`, lunghezza variabile, non unico) → generato
  server-side con RNG sicuro, lunghezza fissa, `UNIQUE`**.
- **`booking_payments` (JSON riepilogo) → colonne tipizzate** su questa testata; i **singoli movimenti**
  (`paidAmountData`/`depositList`/`getCustExpenseData`) → **C3** (`payment` — fuori perimetro C1).
- **`isBookingDoneBySuggestedPrice` + `isRecalculate` + `isEditedFinalPrice`** (3 flag booleani, non
  esclusivi, sulla *provenienza del prezzo finale*) → **`price_flags` `enum[]`** (regola di esclusività
  [`docs/03-gestione-dati.md` §3.10](../../../progettazione/03-gestione-dati.md): più flag dello stesso argomento
  → `enum[]`).
- **doppie/quadruple colonne data** (`created_at`+`createdAt`, `0000-00-00`) → una sola coppia
  `timestamptz`.
- **campi importo/prezzo senza suffisso `_amount`** (`subtotal`, `discount`, `surcharge`, `total`,
  `deposit`, `paid`, `balance_due`, `price_override`) — allineati ai nomi del contratto API di
  [`docs/03-gestione-dati.md` §3.7](../../../progettazione/03-gestione-dati.md).
- **colonne morte non migrate**: `booking_map`, `sharing_dates`, `mainPhoto`, `ofsale_payments` (su
  `bookings`), `booking_location_zeroth`, `consumer_name`/`consumer_mobile`/`consumer_email` (backfill
  esterno), `isRepeat` (scritto mai letto).
- **`isMarketingCampaign` → C6** (attribuzione campagna + auto-iscrizione contact list) — non su questa
  testata ([`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md)).
- **`summaryServicesData` (consumi pagati col borsellino) → C3** (`payment.wallet`); in C1 resta il
  riferimento sulla riga servizio (`is_paid_with_wallet`).
- **`AttachedFiles` (URL S3 completi) → `booking_media_assets`** (`origin`+`path`,
  [`docs/03-gestione-dati.md` §1](../../../progettazione/03-gestione-dati.md)) — figlia non modellata in questa
  iterazione (allegati poco usati), annotata.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_reservations` | `id` | PK | — |
| `uq_booking_reservations_public_id` | `(public_id)` | UNIQUE | referenziato cross-modulo (C3/C4/C6) |
| `uq_booking_reservations_number` | `(booking_number) WHERE deleted_at IS NULL` | UNIQUE parziale | chiave di business; correlazione verso C3/C4/C6 |
| `uq_booking_reservations_reservation_code` | `(reservation_code) WHERE deleted_at IS NULL` | UNIQUE parziale | apertura pubblica via link/QR |
| `ix_..._property_stay` | `(property_id, stay_from) WHERE deleted_at IS NULL` | parziale | agenda/elenco per stabilimento e data (prima colonna tenant, poi range) ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): a DB mancava perché la causa era EF: due `HasIndex` sulle stesse colonne senza nome sono lo stesso indice, e il secondo sovrascrive il primo. |
| `ix_..._property_status` | `(property_id, reservation_status) WHERE deleted_at IS NULL` | parziale | filtri "attive / annullate / da confermare" |
| `ix_..._customer` | `(customer_id) WHERE deleted_at IS NULL` | parziale | "le prenotazioni di questo cliente" |
| `ix_..._property_channel` | `(property_id, booking_channel) WHERE deleted_at IS NULL` | parziale | statistiche online/gestionale |
| `ix_..._agreement` | `(agreement_partner_id) WHERE agreement_partner_id IS NOT NULL AND deleted_at IS NULL` | parziale | prenotazioni di una convenzione |
| `ix_..._unpaid` | `(property_id, stay_from) WHERE balance_due > 0 AND deleted_at IS NULL` | parziale | vista "non pagate / con residuo" |
| `ix_..._online_pending` | `(property_id) WHERE online_confirmation_state = 'pending' AND deleted_at IS NULL` | parziale | prenotazioni online da confermare / da pagare |

## Campi

### id
- **Legacy**: `bookings.id` / `service_reservation.id` (una prenotazione = N `bookings.id`, ora 1)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da tutte le figlie `booking_*` via `reservation_id`.
- **Note**: in migrazione **non** è 1:1 col legacy (N righe `bookings` collassano in 1); serve una mappa
  `legacy_bookings.id → (reservation_id, booking_line_id)`.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile, **copiato cross-modulo** (C3 transazioni/link, C4 scontrino, C6
  notifiche). Alternativa "pulita" al `booking_number` come chiave di correlazione.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: UUIDv7.
- **Nullabile**: No.
- **Relazioni**: copiato da C3/C4/C6.

### property_id
- **Legacy**: `bookings.property_id` / `service_reservation.propertyId` (`int`)
- **Descrizione**: stabilimento proprietario. Discriminatore tenant, prima colonna di quasi ogni indice.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md) — nessuna FK cross-schema.
- **Note**: **sempre dal claim del token**, mai dal body ([`docs/34-multitenant.md` §1](../../../progettazione/34-multitenant.md)); il legacy operava su `req.body.propertyId` (IDOR sistemico).

### bathhouse_id
- **Legacy**: `bookings.bathHouseId` / `service_reservation.bathHouseId` (`int`, `0` sul 98% delle righe)
- **Descrizione**: vetrina pubblica associata (per email/QR/dettaglio cliente: nome, slug, foto,
  regole di cancellazione). Ridondante con `property_id` finché il rapporto è N:1.
- **Tipo**: `uuid` (= `public_id` di `catalog_bathhouses`).
- **Default**: nessuno.
- **Nullabile**: Sì (il legacy lo lasciava a `0`; qui `NULL` = "usa la vetrina di default della property").
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_bathhouses`](../catalog/catalog_bathhouses.md).

### booking_number
- **Legacy**: `bookings.bookingNumber` / `service_reservation.bookingNumber` (`varchar`, default `'0'`, **non unico**)
- **Descrizione**: numero prenotazione mostrato all'operatore e usato come chiave di correlazione verso
  C3 (link/transazioni), C4 ([`fiskaly_receipt.booking_number`](../../../analisi-pre/db/fiskaly_receipt.md)),
  C6, e le figlie che copiano il numero per query dirette.
- **Tipo**: `varchar(40)`.
- **Default**: generato server-side.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE` parziale; generato da
  [`booking_number_sequences`](booking_number_sequences.md).
- **Note**: forma target `"{propertyLegacyId}-{progressivo zero-padded 10}"` (es. `10795-0000012345`),
  vincolo di unicità reale, generazione in transazione con lock sulla riga di sequenza. Lo schema legacy
  (`propertyId` concatenato a `"10000"` + `parseInt+1`) è **rotto per costruzione** (collisione garantita
  alla 100.000ª prenotazione).

### reservation_code
- **Legacy**: `bookings.reservation_code` (`Math.random().toString(36).slice(2)` lato client, lunghezza variabile, **non unico**, enumerabile)
- **Descrizione**: codice usato negli URL pubblici cliente
  (`/onlinebooking/consumer/{it|en}/{code}`) e nel QR; con esso un consumer "aggancia" la prenotazione
  (→ [`booking_participants`](booking_participants.md)) e la conferma.
- **Tipo**: `varchar(16)`.
- **Default**: generato server-side (RNG crittografico, alfabeto senza ambigui, lunghezza fissa).
- **Nullabile**: No.
- **Relazioni**: `UNIQUE` parziale; retry su collisione.

### reservation_kind
- **Legacy**: — (implicito: tabella `bookings` vs `service_reservation`)
- **Descrizione**: canale/natura della prenotazione.
- **Tipo**: `enum booking.reservation_kind` — valori: `spot` (con postazioni), `service` (soli servizi).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `spot` ⇒ ≥1 riga in `booking_lines`; `service` ⇒ 0 righe in `booking_lines`,
  ≥1 in `booking_service_lines`.

### customer_id
- **Legacy**: `bookings.customerId` / `service_reservation.customerId` (`int`, `0` = nessuno) + snapshot JSON `booking_consumer` / `customerData`
- **Descrizione**: cliente **intestatario** (a cui sono imputati pagamenti e comunicazioni). Il record
  cliente è creato/aggiornato in C5 prima della prenotazione (match per email/telefono + property).
- **Tipo**: `uuid` (= `public_id` di `identity_customer_customers`).
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` = prenotazione senza cliente collegato — il legacy usava `0`).
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_customer_customers`](../identity/identity_customer_customers.md) — riferimento **vivo** (nome/contatti seguono l'anagrafica, [`docs/03-gestione-dati.md` §3.6](../../../progettazione/03-gestione-dati.md)).

### booking_channel
- **Legacy**: `bookings.bookingByAdmin` / `service_reservation.bookingByAdmin` (`varchar` `"0"`/`"1"`, default `'1'`)
- **Descrizione**: origine della prenotazione. Guida testi notifica, invio email al cliente (solo se
  `customer`), varie statistiche.
- **Tipo**: `enum booking.booking_channel` — valori: `customer` (`"0"` legacy, cliente online),
  `manager` (`"1"` legacy, operatore da gestionale).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **immutabile** dopo la creazione (il legacy lo faceva con `delete data.bookingByAdmin`).
- **Note**: vocabolario condiviso con `booking_service_lines`/`booking_resource_consumptions` (ex
  `services_booking_counts.booking_type`, nome legacy fuorviante) —
  [`docs/03-gestione-dati.md` §2.4bis](../../../progettazione/03-gestione-dati.md).

### reservation_status
- **Legacy**: `bookings.status` (`tinyint` 0/1/2/3) + `isDeleted` + `payment_status==2→3` / `service_reservation.status` (1/2)
- **Descrizione**: stato del ciclo di vita della prenotazione, **separato** da soft-delete e da
  pagamento.
- **Tipo**: `enum booking.reservation_status` — valori: `pending` (online, in attesa di
  conferma/pagamento — ex `status 2`), `confirmed` (attiva — ex `status 1`), `cancelled` (annullata per
  regole di cancellazione — ex `status 0`+`isDeleted 1`), `no_show` (nuovo, cliente non presentato).
- **Default**: nessuno a DB: il valore lo sceglie sempre l'applicazione — `confirmed` per il canale
  `manager`, `pending` per `customer` con conferma richiesta. Un default unico sarebbe sbagliato per uno
  dei due canali.
- **Nullabile**: No.
- **Relazioni**: la "guardia anti-doppia prenotazione" del legacy (`status 3`) **non** è uno stato: è
  sostituita da un lock/exclusion constraint su
  [`booking_resource_consumptions`](booking_resource_consumptions.md) (§3.9) — ⚠️ vincolo progettato ma
  **non presente a DB** al 2026-10-06 (riquadro in quella scheda).
- **Note**: semantica da riconfermare col team prodotto in fase di migrazione
  ([`docs/03-gestione-dati.md` §2.4bis](../../../progettazione/03-gestione-dati.md)); `status 2` legacy è raro e
  transitorio.

### online_confirmation_state
- **Legacy**: `bookings.is_booking_confirmed` (`int`, semantica invertita: `1` = "da confermare", mai avanzato — bug)
- **Descrizione**: stato del flusso "prenotazione online che richiede conferma esplicita del cliente"
  (`catalog_property_online_booking_settings` — `requestBookingConfirmation`).
- **Tipo**: `enum booking.online_confirmation_state` — valori: `not_required` (conferma non richiesta —
  ex `is_booking_confirmed 0`), `pending` (in attesa della conferma via codice — ex `1`), `confirmed`
  (confermata dal cliente).
- **Default**: `not_required`.
- **Nullabile**: No.
- **Relazioni**: `confirmBookingByReservationCode` porta `pending → confirmed` (nel legacy lo stato
  **non** avanzava: si limitava a inviare l'email).

### operator_user_id
- **Legacy**: `bookings.booking_agent` (stringa id, dal body, immutabile) / `service_reservation.agent`
- **Descrizione**: operatore che ha **creato** la prenotazione da gestionale. `NULL` per le
  prenotazioni online (nessun operatore).
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_access_users`](../identity/identity_access_users.md).
- **Note**: **dal token JWT**, mai dal body ([`docs/03-gestione-dati.md` §3.2](../../../progettazione/03-gestione-dati.md)); il legacy permetteva di attribuire la prenotazione a un altro operatore.

### updated_by_user_id
- **Legacy**: `service_reservation.updatedById` (`int`, default `0`)
- **Descrizione**: ultimo operatore che ha modificato la prenotazione.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_access_users`](../identity/identity_access_users.md).

### agreement_partner_id
- **Legacy**: `bookings.hotelId` (`int`, commento "FK to hotels")
- **Descrizione**: se valorizzato, la prenotazione è associata a una **struttura partner** (convenzione /
  "hotel"); attiva la lavorazione di [`booking_agreement_lines`](booking_agreement_lines.md).
- **Tipo**: `uuid` (= `public_id` di `catalog_agreement_partners`).
- **Default**: nessuno.
- **Nullabile**: Sì (solo `reservation_kind = spot`).
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_agreement_partners`](../catalog/catalog_agreement_partners.md).

### listing_type
- **Legacy**: `bookings.bookingType` (`tinyint` 0/1/2, commento model)
- **Descrizione**: tipo di listino applicato alla prenotazione.
- **Tipo**: `enum booking.listing_type` — valori: `standard` (listino stabilimento — ex `0`),
  `agreement_price_list` (listino convenzione senza voucher — ex `1`), `agreement_voucher` (voucher
  convenzione — ex `2`).
- **Default**: `standard`.
- **Nullabile**: No.
- **Relazioni**: coerente con `agreement_partner_id` (il legacy forzava `1` se `hotelId` presente e
  valore fuori `[0,1,2]`, `0` se assente).

### has_seasonal_pass
- **Legacy**: `bookings.seasonalPass` (`tinyint`, ~5% righe)
- **Descrizione**: prenotazione stagionale / abbonamento.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No (`false` per `reservation_kind = service`).
- **Relazioni**: guida `getSeasonalBookings` e i rinnovi pass stagionali (usa
  `catalog_pricing_seasons.next_season_*`).

### is_split
- **Legacy**: `bookings.isSplit` (`tinyint`; nell'archivio storico sempre `0` — feature successiva)
- **Descrizione**: la prenotazione è "split location" (postazioni diverse con periodi diversi entro lo
  stesso numero). Nel nuovo modello è **derivabile** (più righe `booking_lines` con `resource_id`/periodo
  diversi) ma si tiene il flag per compatibilità di lettura.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### price_flags
- **Legacy**: `bookings.isBookingDoneBySuggestedPrice` + `isRecalculate` + `booking_payments.isEditedFinalPrice` (3 `tinyint`/bool nel JSON)
- **Descrizione**: come si è formato il **prezzo finale** della prenotazione. Flag **non esclusivi** (si
  può accettare un prezzo suggerito, poi modificarlo a mano, poi averlo ricalcolato): insieme in un
  array invece di 3 colonne.
- **Tipo**: `enum booking.reservation_price_flag[]` — valori: `suggested_price_accepted` (confermata
  accettando il prezzo suggerito dal Revenue Management — ex `isBookingDoneBySuggestedPrice`),
  `recalculated` (prezzo ricalcolato dopo la creazione, es. spostamento/modifica — ex `isRecalculate`),
  `manually_edited` (prezzo finale forzato a mano — ex `isEditedFinalPrice`; ridondante con
  `price_override IS NOT NULL`, tenuto per audit).
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: regola di esclusività "più flag dello stesso argomento → `enum[]`"
  ([`docs/03-gestione-dati.md` §3.10](../../../progettazione/03-gestione-dati.md)).

### pay_now
- **Legacy**: `service_reservation.payNow` (`int` 0/1)
- **Descrizione**: il cliente ha scelto di **pagare subito online** al checkout (`true`) invece che
  differito / a struttura (`false`). Rilevante per il canale `customer`.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: il flusso di pagamento vero è C3; qui è solo l'intento espresso al checkout.

### is_order_filled
- **Legacy**: `bookings.isOrderFilled` / `service_reservation.isOrderFilled` (`tinyint`)
- **Descrizione**: gli ordini bar/servizio collegati sono stati **evasi** (spunta di cucina/bar,
  endpoint `update-order-filled`). Riepilogo a livello prenotazione; il dettaglio per riga è
  `booking_service_lines.is_order_filled`.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### note
- **Legacy**: `bookings.note` (`text`) / `service_reservation.note` (`varchar(255)`)
- **Descrizione**: nota libera dell'operatore sulla prenotazione.
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: **retention più corta della prenotazione che la contiene**. `note` (e `customer_tags`) si
  azzerano a **N−1**, mentre la testata resta online fino a N−2
  ([`docs/82-retention-data.md` §3.4](../../../progettazione/82-retention-data.md), riga "Campo note libero"):
  è un campo libero dove il legacy ha già visto finire dati sulla salute (art. 9 GDPR,
  [D-25](../_domande-committente.md)), quindi non eredita la finestra della testata. Riga di policy
  dedicata (`field_names = {note, customer_tags}`, `action = null_fields`) nel
  [§ Seed di `privacy_retention_policies`](../privacy/privacy_retention_policies.md).

### customer_tags
- **Legacy**: `bookings.booking_payments.selectedCustomerIcons` (array di stringhe, dentro il JSON)
- **Descrizione**: tag/icone cliente selezionati nel riepilogo (es. `kids`, `umbrella-front`), mostrati
  in agenda/mappa. Vocabolario chiuso lato UI.
- **Tipo**: `text[]` (o `enum booking.customer_tag[]` se il vocabolario si stabilizza).
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: candidato a figlia `booking_reservation_tags` se il set cresce; per ora array (poche voci,
  chiuse, solo presentazione — [`docs/03-gestione-dati.md` §2.5](../../../progettazione/03-gestione-dati.md)).

### stay_from / stay_to
- **Legacy**: min/max di `bookings.booking_start_date` / `booking_end_date` sulle righe dello stesso numero
- **Descrizione**: intervallo complessivo della prenotazione, **denormalizzato** dalle righe
  ([`booking_lines`](booking_lines.md).`start_date`/`end_date`) per i filtri di agenda/elenco. Per
  `reservation_kind = service` = min/max delle date dei servizi.
- **Tipo**: `date` ciascuno.
- **Default**: nessuno (ricalcolato a ogni modifica di riga).
- **Nullabile**: Sì (prenotazione senza righe — stato transitorio).
- **Relazioni**: mantenuto da trigger/interceptor sulle righe; **non** è autorevole per la
  disponibilità (quella è `booking_resource_consumptions` per giorno).
- **Note**: `stay_to` è anche la **decorrenza della retention** della testata
  ([`docs/80-retention.md` §3.1](../../../progettazione/80-retention.md)): finestra online **N−2** (stagione
  corrente + le due precedenti) validata dal titolare il 2026-09-17
  ([`docs/82-retention-data.md` §3.1](../../../progettazione/82-retention-data.md), chiarimento Q1/Q2 — estende
  la proposta iniziale N−1). Essendo **nullabile** (prenotazione senza righe, stato transitorio), il job
  di archiviazione deve trattare `stay_to IS NULL` come "non ancora scaduta" e non come "scaduta da
  sempre": un `WHERE stay_to < soglia` esclude già i `NULL`, ma un calcolo su `COALESCE(stay_to, …)`
  no — attenzione a non archiviare bozze appena create.

### currency
- **Legacy**: — (implicito `EUR`)
- **Descrizione**: valuta degli importi.
- **Tipo**: `char(3)` (ISO 4217).
- **Default**: `'EUR'`.
- **Nullabile**: No.
- **Relazioni**: —.

### subtotal
- **Legacy**: `bookings.booking_payments.subTotal` (JSON, numero **o** stringa) / `booking_payments.total_amount` (`float`)
- **Descrizione**: imponibile/prezzo base **prima** di sconti e supplementi. Somma dei
  `booking_lines.line_price` + `booking_service_lines.line_total` + articoli aggiuntivi.
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: mai `float` ([`docs/03-gestione-dati.md` §3.7](../../../progettazione/03-gestione-dati.md)); i
  valori legacy in `float` arrivavano a `~2,15e9` per overflow — bonifica in migrazione.

### discount
- **Legacy**: derivato da `bookings.booking_discount` + `booking_payments` (JSON)
- **Descrizione**: totale sconti/voucher applicati, **valore assoluto negativo** (o 0). Dettaglio in
  [`booking_discounts`](booking_discounts.md).
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### surcharge
- **Legacy**: `bookings.booking_payments.surchargeObj` (`{type, addSurcharge}`) + `addExtraAmount`
- **Descrizione**: supplemento/sovrapprezzo (commissione di servizio, extra manuale). La **commissione
  di incasso online** (ex `properties.commissionAmount`) è **C3**, non qui.
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### total
- **Legacy**: `bookings.booking_payments.payableAmount` (JSON) / `booking_payments.total_amount` (`float`)
- **Descrizione**: **totale dovuto** = `subtotal + surcharge + discount` (dopo sconti e supplementi).
  È l'importo autoritativo salvato/incassato/messo in ricevuta.
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: in riscrittura calcolato dal **motore prezzo server-side**
  ([`_motore-di-prezzo-e-sconti.md`](../../../analisi-pre/business-logic/stabilimento/_motore-di-prezzo-e-sconti.md));
  il legacy lo prendeva as-is dal client.

### price_override
- **Legacy**: `bookings.booking_payments.appliedPrice` (stringa, `""` se non forzato)
- **Descrizione**: prezzo finale **forzato manualmente** dall'operatore (override del calcolato).
  `NULL` = nessun override.
- **Tipo**: `numeric(12,2)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: se valorizzato, `total = price_override` e `price_flags` contiene `manually_edited`.

### deposit
- **Legacy**: `bookings.booking_payments.deposit` (JSON) — la lista acconti (`depositList`) → **C3**
- **Descrizione**: importo acconto/caparra **corrente** (riepilogo). I singoli acconti con data/metodo
  sono movimenti C3.
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### paid
- **Legacy**: `booking_payments.paid_amount` (`float`, `= payableAmount - toPay`)
- **Descrizione**: totale già incassato per la prenotazione (riepilogo derivato dai movimenti C3).
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: mantenuto in C1 come proiezione degli eventi di incasso di C3 (evento
  `PaymentRecorded` → aggiorna `paid`/`balance_due`); **non** è la sorgente di verità dei movimenti.

### balance_due
- **Legacy**: `booking_payments.pending_amount` / `bookings.booking_payments.toPay` (`float`)
- **Descrizione**: residuo da incassare = `total - paid` (può essere negativo per storni/incassi in
  eccesso, come nel legacy).
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: colonna calcolata/mantenuta insieme a `paid`; indice parziale `WHERE balance_due > 0`
  per la vista "non pagate".

### prevalent_payment_mode
- **Legacy**: `booking_payments.payment_mode` (`varchar`, drift: `NULL`/`""`/`"Bank Transfer"`/…)
- **Descrizione**: modalità di pagamento **prevalente** della prenotazione (riepilogo). Il dettaglio
  per movimento è C3.
- **Tipo**: `enum booking.payment_mode` — valori: `cash`, `credit_card`, `bank_transfer`, `other`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`NULL` = non ancora incassato / non determinato; il legacy aveva `NULL` sul 67%).
- **Relazioni**: vocabolario condiviso con C3; normalizzazione drift (`""` → `NULL`, `"Bank Transfer"`
  → `bank_transfer`) in migrazione ([`docs/03-gestione-dati.md` §2.4bis](../../../progettazione/03-gestione-dati.md)).

### created_at / updated_at
- **Legacy**: `bookings.createdAt`/`updatedAt` (gestiti a mano, `timestamps` off) — più i doppioni `created_at`/`createdAt` su `booking_payments`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` (+ trigger `updated_at`).
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: `bookings.isDeleted` (`int`) + `status = 0`
- **Descrizione**: soft-delete (annullamento/eliminazione secondo le regole di cancellazione). Distinto
  da `reservation_status = cancelled` (che è lo stato di dominio "annullata ma visibile"): `deleted_at`
  è la cancellazione logica vera.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: Global Query Filter EF; retention → archiviazione (non hard-delete) delle stagioni
  oltre **N−2**, poi cancellazione dell'archivio a 10 anni se la prenotazione è fiscalizzata
  ([`docs/80-retention.md` §3.1](../../../progettazione/80-retention.md), job `SeasonalArchive` di
  [`docs/20-jobs.md` §1.5](../../../progettazione/20-jobs.md)). Le righe soft-deleted **non** sono esenti: il job
  `CompactSoftDeleted` le rimuove dopo 90 giorni *se* non sono coperte dal termine fiscale — fra i due
  orologi vince il più lungo.

## Valutazione JSON

**Nessun JSON di dominio.** I blob legacy si dissolvono così:

| JSON legacy | Va in |
|---|---|
| `booking_consumer`, `guest_data`, `guest_ids` | `customer_id` (FK viva C5) + [`booking_guests`](booking_guests.md) |
| `booking_locations`, `packageSelectedData` | [`booking_lines`](booking_lines.md) + [`booking_line_additional_items`](booking_line_additional_items.md) (griglia prezzi giorno-per-giorno **non** persistita: ricalcolata dal motore prezzo) |
| `booking_payments` (riepilogo) | colonne `subtotal` / `discount` / `surcharge` / `total` / `deposit` / `paid` / `balance_due` / `prevalent_payment_mode` su questa testata; `isEditedFinalPrice` → `price_flags` |
| `booking_payments.paidAmountData` / `depositList` / `getCustExpenseData` | **C3** (`payment` — movimenti) |
| `booking_discount`, `additionalAutoVoucher` | [`booking_discounts`](booking_discounts.md) |
| `servicesData`, `summaryServicesData` | [`booking_service_lines`](booking_service_lines.md) (+ `is_paid_with_wallet` per il borsellino, il cui movimento è C3) |
| `additionalList`, `certainAddItemData` | [`booking_line_additional_items`](booking_line_additional_items.md) |
| `areaDetails` | [`booking_lines`](booking_lines.md) `is_area` + [`booking_resource_consumptions`](booking_resource_consumptions.md) (`resource_type = area_seat`) |
| `booking_absence_dates` | [`booking_resale_offers`](booking_resale_offers.md) |
| `AttachedFiles` | `booking_media_assets` (`origin`+`path`) — figlia annotata, non in questa iterazione |
| `selectedCustomerIcons` | `customer_tags text[]` |
| `booking_map`, `sharing_dates`, `mainPhoto`, `ofsale_payments` (su `bookings`) | **eliminati** (colonne morte) |

## Suggerimenti per le prestazioni

- La testata è "hot": ~350.000 righe storiche, letta in agenda/planner/elenco. Indici parziali
  `WHERE deleted_at IS NULL` su `(property_id, stay_from)` e `(property_id, reservation_status)`
  coprono i percorsi caldi; `stay_from`/`stay_to` denormalizzati evitano il join alle righe per i soli
  filtri di data.
- Le viste "crediti / incassi / non pagati" (ex `creditDetails`, join `bookings`↔`booking_payments`)
  qui sono una `SELECT` sulla sola testata (riepilogo già in colonna) filtrata per intervallo e
  `balance_due > 0` — niente join e niente `GROUP BY` sui movimenti.
- `booking_number` e `reservation_code` con `UNIQUE` parziale servono anche i lookup puntuali di C3/C4/C6
  e l'apertura pubblica via link.
- `paid`/`balance_due` mantenuti per proiezione da eventi C3: evitano di ricalcolare il saldo sommando i
  movimenti a ogni lettura; un job di riconciliazione periodico li riallinea.
- Evitare un covering index `INCLUDE` sugli importi finché il profiling non mostra accesso frequente
  alla heap sulle liste.

## Implementazione .NET

`Reservation` è l'**aggregate root** dell'aggregato Prenotazione, modulo `Module.Booking`, schema
`booking`. Espone le collezioni figlie (`Lines`, `AdditionalItems` per riga, `ServiceLines`,
`Discounts`, `Guests`, `Participants`, `AgreementLines`, `QuickActionDates`) come proprietà di sola
lettura popolate da EF via navigazione; le relative FK (`reservation_id`) sono configurate lato figlia
(vedi le rispettive schede). Tutte le regole di transizione di stato (`Confirm()`, `Cancel()`,
`RecalculatePrice(...)`, `RegisterPayment(...)`) vivono sull'aggregato, mai come setter pubblici.

### Enum propri

Introdotti da questa scheda (prima occorrenza nel batch); `BookingChannel` e `PaymentMode` sono
riusati da altre schede dello stesso schema `booking` (`booking_service_lines.md`) — le altre schede
linkano questa sezione invece di ridefinirli.

```csharp
// Module.Booking.Domain.Enums

/// Canale/natura della prenotazione: con postazioni (ex tabella bookings) o soli servizi (ex service_reservation).
public enum ReservationKind
{
	Spot = 0,
	Service = 1
}

/// Origine della prenotazione — vocabolario condiviso anche da booking_service_lines (booking_type) e
/// booking_resource_consumptions (fuori da questo batch).
public enum BookingChannel
{
	Customer = 0,
	Manager = 1
}

/// Stato del ciclo di vita della prenotazione, distinto dal soft-delete e dal lock anti-doppia prenotazione.
public enum ReservationStatus
{
	Pending = 0,
	Confirmed = 1,
	Cancelled = 2,
	NoShow = 3
}

/// Stato del flusso di conferma online via reservation_code (requestBookingConfirmation).
public enum OnlineConfirmationState
{
	NotRequired = 0,
	Pending = 1,
	Confirmed = 2
}

/// Tipo di listino applicato alla prenotazione.
public enum ListingType
{
	Standard = 0,
	AgreementPriceList = 1,
	AgreementVoucher = 2
}

/// Come si è formato il prezzo finale — flag non esclusivi, mappato come enum[] (§3.10).
public enum ReservationPriceFlag
{
	SuggestedPriceAccepted = 0,
	Recalculated = 1,
	ManuallyEdited = 2
}

/// Modalità di pagamento prevalente (riepilogo) — vocabolario condiviso con Module.Payment (schema payment ha
/// la propria copia dell'enum PG: i tipi ENUM di Postgres sono per-schema, non condivisibili cross-schema).
public enum PaymentMode
{
	Cash = 0,
	CreditCard = 1,
	BankTransfer = 2,
	Other = 3
}
```

```csharp
// Module.Booking.Infrastructure.DbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ReservationKind>("booking", "reservation_kind");
modelBuilder.HasPostgresEnum<BookingChannel>("booking", "booking_channel");
modelBuilder.HasPostgresEnum<ReservationStatus>("booking", "reservation_status");
modelBuilder.HasPostgresEnum<OnlineConfirmationState>("booking", "online_confirmation_state");
modelBuilder.HasPostgresEnum<ListingType>("booking", "listing_type");
modelBuilder.HasPostgresEnum<ReservationPriceFlag>("booking", "reservation_price_flag");
modelBuilder.HasPostgresEnum<PaymentMode>("booking", "payment_mode");
```

```csharp
// Module.Booking Infrastructure DI — registrazione data source Npgsql
dataSourceBuilder.MapEnum<ReservationKind>("booking.reservation_kind");
dataSourceBuilder.MapEnum<BookingChannel>("booking.booking_channel");
dataSourceBuilder.MapEnum<ReservationStatus>("booking.reservation_status");
dataSourceBuilder.MapEnum<OnlineConfirmationState>("booking.online_confirmation_state");
dataSourceBuilder.MapEnum<ListingType>("booking.listing_type");
dataSourceBuilder.MapEnum<ReservationPriceFlag>("booking.reservation_price_flag");
dataSourceBuilder.MapEnum<PaymentMode>("booking.payment_mode");
```

### `ReservationConfiguration`

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
	public void Configure(EntityTypeBuilder<Reservation> builder)
	{
		builder.ToTable("booking_reservations", "booking", t => t.HasComment(
			"Testata unica di una prenotazione logica (un booking_number), spot o solo servizi."));

		builder.HasKey(x => x.Id).HasName("pk_booking_reservations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, copiato cross-modulo da C3/C4/C6.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; discriminatore tenant.");

		builder.Property(x => x.BathhouseId)
			.HasColumnName("bathhouse_id")
			.HasComment("public_id di catalog.bathhouses, copiato — NULL = vetrina di default della property.");

		builder.Property(x => x.BookingNumber)
			.HasColumnName("booking_number")
			.HasMaxLength(40)
			.IsRequired()
			.HasComment("Numero prenotazione generato server-side da booking_number_sequences, univoco fra righe vive.");

		builder.Property(x => x.ReservationCode)
			.HasColumnName("reservation_code")
			.HasMaxLength(16)
			.IsRequired()
			.HasComment("Codice pubblico (URL/QR) generato con RNG sicuro, univoco fra righe vive.");

		builder.Property(x => x.Kind)
			.HasColumnName("reservation_kind")
			.HasColumnType("booking.reservation_kind")
			.IsRequired()
			.HasComment("spot (con postazioni) o service (soli servizi).");

		builder.Property(x => x.CustomerId)
			.HasColumnName("customer_id")
			.HasComment("public_id di identity.customers, copiato — riferimento vivo, nessuna FK cross-schema.");

		builder.Property(x => x.Channel)
			.HasColumnName("booking_channel")
			.HasColumnType("booking.booking_channel")
			.IsRequired()
			.HasComment("Origine della prenotazione: customer (online) o manager (gestionale). Immutabile dopo la creazione.");

		builder.Property(x => x.Status)
			.HasColumnName("reservation_status")
			.HasColumnType("booking.reservation_status")
			.IsRequired()
			.HasComment("Stato del ciclo di vita, separato da soft-delete e da pagamento.");

		builder.Property(x => x.OnlineConfirmationState)
			.HasColumnName("online_confirmation_state")
			.HasColumnType("booking.online_confirmation_state")
			.IsRequired()
			.HasDefaultValueSql("'not_required'")
			.HasComment("Stato del flusso di conferma online via reservation_code.");

		builder.Property(x => x.OperatorUserId)
			.HasColumnName("operator_user_id")
			.HasComment("public_id di identity.access_users, copiato dal token JWT — operatore che ha creato la prenotazione, NULL se online.");

		builder.Property(x => x.UpdatedByUserId)
			.HasColumnName("updated_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — ultimo operatore che ha modificato la prenotazione.");

		builder.Property(x => x.AgreementPartnerId)
			.HasColumnName("agreement_partner_id")
			.HasComment("public_id di catalog.agreement_partners, copiato — valorizzato solo per reservation_kind = spot.");

		builder.Property(x => x.ListingType)
			.HasColumnName("listing_type")
			.HasColumnType("booking.listing_type")
			.IsRequired()
			.HasDefaultValueSql("'standard'")
			.HasComment("Tipo di listino applicato: standard, listino convenzione o voucher convenzione.");

		builder.Property(x => x.HasSeasonalPass)
			.HasColumnName("has_seasonal_pass")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Prenotazione stagionale / abbonamento; false per reservation_kind = service.");

		builder.Property(x => x.IsSplit)
			.HasColumnName("is_split")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Prenotazione split-location (derivabile dalle righe, tenuto per compatibilità di lettura).");

		builder.Property(x => x.PriceFlags)
			.HasColumnName("price_flags")
			.HasColumnType("booking.reservation_price_flag[]")
			.IsRequired()
			.HasDefaultValueSql("'{}'")
			.HasComment("Come si è formato il prezzo finale (accettato suggerito / ricalcolato / forzato a mano) — flag non esclusivi.");

		builder.Property(x => x.PayNow)
			.HasColumnName("pay_now")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Il cliente ha scelto di pagare subito online al checkout (canale customer).");

		builder.Property(x => x.IsOrderFilled)
			.HasColumnName("is_order_filled")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Riepilogo evasione ordini bar/servizio collegati; dettaglio per riga su booking_service_lines.");

		builder.Property(x => x.Note)
			.HasColumnName("note")
			.HasColumnType("text")
			.HasComment("Nota libera dell'operatore sulla prenotazione.");

		builder.Property(x => x.CustomerTags)
			.HasColumnName("customer_tags")
			.HasColumnType("text[]")
			.IsRequired()
			.HasDefaultValueSql("'{}'")
			.HasComment("Tag/icone cliente selezionati nel riepilogo (presentazione, vocabolario chiuso lato UI).");

		builder.Property(x => x.StayFrom)
			.HasColumnName("stay_from")
			.HasColumnType("date")
			.HasComment("Data minima dell'intervallo complessivo, denormalizzata dalle righe per i filtri di agenda.");

		builder.Property(x => x.StayTo)
			.HasColumnName("stay_to")
			.HasColumnType("date")
			.HasComment("Data massima dell'intervallo complessivo, denormalizzata dalle righe per i filtri di agenda.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.IsRequired()
			.HasDefaultValue("EUR")
			.HasComment("Valuta ISO 4217 degli importi della prenotazione.");

		builder.Property(x => x.Subtotal)
			.HasColumnName("subtotal")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Imponibile prima di sconti e supplementi, somma delle righe.");

		builder.Property(x => x.Discount)
			.HasColumnName("discount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Totale sconti/voucher applicati (valore assoluto negativo o 0); dettaglio su booking_discounts.");

		builder.Property(x => x.Surcharge)
			.HasColumnName("surcharge")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Supplemento/sovrapprezzo (commissione di servizio, extra manuale).");

		builder.Property(x => x.Total)
			.HasColumnName("total")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Totale dovuto = subtotal + surcharge + discount; calcolato dal motore prezzo server-side.");

		builder.Property(x => x.PriceOverride)
			.HasColumnName("price_override")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo finale forzato manualmente dall'operatore; NULL = nessun override.");

		builder.Property(x => x.Deposit)
			.HasColumnName("deposit")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Importo acconto/caparra corrente (riepilogo); i singoli acconti sono movimenti C3.");

		builder.Property(x => x.Paid)
			.HasColumnName("paid")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Totale già incassato, proiezione dagli eventi di pagamento di C3.");

		builder.Property(x => x.BalanceDue)
			.HasColumnName("balance_due")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Residuo da incassare = total - paid; può essere negativo per storni/incassi in eccesso.");

		builder.Property(x => x.PrevalentPaymentMode)
			.HasColumnName("prevalent_payment_mode")
			.HasColumnType("booking.payment_mode")
			.HasComment("Modalità di pagamento prevalente (riepilogo); NULL finché non ancora incassata/determinata.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete (cancellazione logica); NULL = viva. Distinto da Status = Cancelled.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_booking_reservations_public_id");

		builder.HasIndex(x => x.BookingNumber)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_booking_reservations_number");

		builder.HasIndex(x => x.ReservationCode)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_booking_reservations_reservation_code");

		builder.HasIndex(x => new { x.PropertyId, x.StayFrom })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_reservations_property_stay");

		builder.HasIndex(x => new { x.PropertyId, x.Status })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_reservations_property_status");

		builder.HasIndex(x => x.CustomerId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_reservations_customer");

		builder.HasIndex(x => new { x.PropertyId, x.Channel })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_reservations_property_channel");

		builder.HasIndex(x => x.AgreementPartnerId)
			.HasFilter("agreement_partner_id IS NOT NULL AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_reservations_agreement");

		builder.HasIndex(x => new { x.PropertyId, x.StayFrom })
			.HasFilter("balance_due > 0 AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_reservations_unpaid");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("online_confirmation_state = 'pending' AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_reservations_online_pending");
	}
}
```

`Reservation` implementa `ISoftDeletable` (`deleted_at`) e `IAuditable` (`created_at`/`updated_at`).
Nessuna FK EF verso altri moduli (`property_id`, `bathhouse_id`, `customer_id`, `operator_user_id`,
`updated_by_user_id`, `agreement_partner_id` sono tutti `public_id` copiati, solo `HasComment`); le FK
reali dello schema `booking` sono configurate lato figlia (`HasOne(x => x.Reservation).WithMany(...)`
su `booking_lines`, `booking_service_lines`, `booking_discounts`, `booking_guests`,
`booking_participants`, `booking_agreement_lines`, `booking_quick_action_dates`).

### Note di migrazione

- **N righe `bookings` con lo stesso `bookingNumber` → 1 `reservation` + N `booking_lines`**: serve una
  mappa `legacy_bookings.id → (reservation_id, booking_line_id)` per riconciliare le FK applicative
  legacy durante il dual-run.
- `service_reservation` → `reservation` con `reservation_kind = service`, nessuna riga in
  `booking_lines`.
- JSON `booking_payments` (riepilogo) → colonne tipizzate `subtotal`/`discount`/`surcharge`/`total`/
  `deposit`/`paid`/`balance_due`/`prevalent_payment_mode`; i movimenti (`paidAmountData`/
  `depositList`/`getCustExpenseData`) restano fuori (C3).
- `isBookingDoneBySuggestedPrice` + `isRecalculate` + `isEditedFinalPrice` → `price_flags[]` (unione
  dei tre booleani, non esclusivi).
- `bookings.bookingByAdmin` (`"0"`/`"1"` varchar) → `booking_channel` enum; `bookings.status`
  (tinyint 0/1/2/3) + `isDeleted` → `reservation_status` enum + `deleted_at` separati.
- Colonne morte non migrate: `booking_map`, `sharing_dates`, `mainPhoto`, `ofsale_payments`,
  `booking_location_zeroth`, `consumer_name`/`consumer_mobile`/`consumer_email`, `isRepeat`.
- Importi `float` (arrivati a overflow `~2,15e9` nel legacy) → `numeric(12,2)` con bonifica outlier in
  migrazione, mai riportati as-is.
