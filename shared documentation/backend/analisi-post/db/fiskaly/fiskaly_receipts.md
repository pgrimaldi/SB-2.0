# Tabella `fiskaly_receipts`

> **Dominio**: C4 — `fiskaly` · **sottoarea**: — · **schema**: `fiskaly` · **nome flat**:
> `fiskaly_receipts` · **forma canonica**: `fiskaly.receipts`.
>
> **Testata unica** che sostituisce [`fiskaly_receipt`](../../../analisi-pre/db/fiskaly_receipt.md)
> (2.596 righe) **+** [`fiskaly_non_fiscal_receipt`](../../../analisi-pre/db/fiskaly_non_fiscal_receipt.md)
> (17 righe), discriminate da `document_kind` (decisione **C4-1**, coerente con C1-2 "testata unica").
> Fonti:
> [`analisi-pre/db/fiskaly_receipt.md`](../../../analisi-pre/db/fiskaly_receipt.md),
> [`analisi-pre/db/fiskaly_non_fiscal_receipt.md`](../../../analisi-pre/db/fiskaly_non_fiscal_receipt.md),
> [`analisi-pre/db/booking_number_informations.md`](../../../analisi-pre/db/booking_number_informations.md)
> (`details_payments*` = input righe fiscali),
> [`docs/03-gestione-dati.md` §2.4ter, §3.5, §3.6, §3.7, §4](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md`](../../../progettazione/80-retention.md),
> [`analisi-pre/business-logic/stabilimento/03-cassa.md` §6](../../../analisi-pre/business-logic/stabilimento/03-cassa.md).

---

## Descrizione

**Una riga = un documento commerciale emesso** per una prenotazione — **fiscale** (firmato via Fiskaly
SIGN IT, con numero progressivo) **o non fiscale** (pre-conto / proforma, stesso motore di calcolo
righe/IVA ma senza firma). Il legame con la prenotazione è `reservation_public_id` + `booking_number`
(nel legacy solo la stringa `booking_number`, che raggruppa prenotazioni ombrellone **e** servizi →
in C1 unificate in [`booking_reservations`](../booking/booking_reservations.md)).

È un **fatto fiscale storico**: gli importi e le righe sono uno **snapshot immutabile** (§3.6 punto 2);
l'annullamento **non cancella** la riga (`record_state = cancelled` + CANCELLATION su Fiskaly). Verso
C1 (prenotazione) e C3 (incasso) si copiano solo `public_id` / `booking_number` / importi — **nessuna
FK cross-modulo**.

**Perché una testata unica** (C4-1): `createNonFiscalReceipt` nel legacy riusa **lo stesso** builder
(`buildReceiptCreationData`) dello scontrino fiscale; righe e pagamenti sono identici → un solo target
FK per le figlie ([`fiskaly_receipt_lines`](fiskaly_receipt_lines.md),
[`fiskaly_receipt_payments`](fiskaly_receipt_payments.md)), nessuna figlia polimorfa. Le colonne
solo-fiscali restano `NULL` sulle righe `non_fiscal`, tenute oneste dai `CHECK` in §"Vincoli".

Correzioni rispetto al legacy:

- **Importi come `varchar`** (`amount_total`, `amount_vat`, `amount_net` + numeri dentro
  `entries`/`payments`) → `numeric(12,2)` ([`docs/03 §3.7`](../../../progettazione/03-gestione-dati.md)).
- **Tre importi con semantiche divergenti** (`amount_total` = netto pagato; `amount_vat`/`amount_net`
  = lordi; aggiustamento sconto fatto solo nel PDF) → 4 colonne coerenti
  (`total_amount` = `taxable_amount` + `vat_amount`, con `discount_amount` esplicito); divergenze
  storiche riconciliate in migrazione.
- **`document_number` scambiato per identificativo** (è una sequenza per cassa × giorno, si ripete;
  in prod un `"2"` condiviso da 14 righe) → resta `document_number` (numero fiscale) **non univoco**,
  il resume/lookup avviene per `id` / `public_id` / `record_id`.
- **`record_state` = enum interno + passthrough `content.state` Fiskaly nella stessa colonna** →
  `record_state` (enum interno del flusso) **separato** da `fiskaly_state_raw` (grezzo)
  ([`docs/03 §2.4ter`](../../../progettazione/03-gestione-dati.md)).
- **`entries` / `payments` array JSON con vocabolari come stringhe libere** → tabelle figlie
  tipizzate ([`docs/03 §2.5`](../../../progettazione/03-gestione-dati.md)).
- **`paid_deltas` JSON** (per revertire l'effetto su `booking_number_informations`) → `paid_allocation`
  per riga su [`fiskaly_receipt_lines`](fiskaly_receipt_lines.md) + i 3 scalari
  `invoiced_payment_*` qui; il revert è un movimento di storno in **C3**
  ([`payment_ledger_entries`](../payment/payment_ledger_entries.md)), append-only.
- **URL S3 completo** in `e_receipt_path` (`varchar(500)`) → FK a
  [`fiskaly_media_assets`](fiskaly_media_assets.md) (`origin` + `path`, [`docs/03 §1`](../../../progettazione/03-gestione-dati.md)).
- **Nessun `property_id`** sulla riga (scoping via `system_id → cash_register → account`, oppure 2
  query `getPropertyIdByBookingNumber`) → `property_id` copiato.
- **`emailSent` / `whatsappSent`** (il "sent" WhatsApp era la sola *generazione del link* `wa.me`) →
  **escono verso C6** (`notification.deliveries`), non migrati qui
  ([`docs/03 §4`](../../../progettazione/03-gestione-dati.md)).
- **`fiskaly_version`** colonna morta → non migrata.
- **`languageCode` doppia convenzione** (`"it"`/`"en"` **e** `2`/`1`) → `enum shared.language_code`.
- **`document_number varchar(20)` / `booking_number varchar(50)` vs `varchar(255)` su non-fiscal** →
  tipi allineati.

## Verifica di esclusività dei flag

| Gruppo | Campi | Esito |
|---|---|---|
| Tipo di documento | `document_kind` | **enum** esclusivo: `fiscal` / `non_fiscal`. |
| Origine | `source` | **enum** esclusivo: `manual` / `autoreceipt` (ex `other`/`autoreceipt`). |
| Stato del flusso | `record_state` | **enum** esclusivo (un solo stato per riga). Separato da `deleted_at` (che non si usa) e da `fiskaly_state_raw`. |
| Esito invio email/WhatsApp | ~~`emailSent` / `whatsappSent`~~ | **fuori da C4** → righe di `notification.deliveries` (C6). Sarebbero stati 2 booleani indipendenti (entrambi i canali usabili), **non** un enum. |

## Snapshot (come per `catalog`)

Fatto fiscale immutabile → colonne congelate accanto alle FK vive (§3.6 punto 2, regola `_snapshot`
di [`_feature-tabelle.md` → C1-7 / C3-8](../_feature-tabelle.md)):

| Campo | Congela | FK viva accanto | Suffisso |
|---|---|---|---|
| `seller_legal_name_snapshot`, `seller_trade_name_snapshot`, `seller_vat_number_snapshot`, `seller_fiscal_code_snapshot`, `seller_address_snapshot` | anagrafica venditore al momento dell'emissione | `fiskaly_account_id` | **Sì** (`_snapshot`) |
| `fiskaly_system_id_snapshot` | id `System` Fiskaly opaco al momento della firma | `cash_register_id` | **Sì** (identificativo esterno, C3-8) |
| `total_amount`, `taxable_amount`, `vat_amount`, `discount_amount`, `invoiced_payment_amount` | importi del documento | — | **No** — dato proprio del documento (§3.6, C1-7) |
| `fiskaly_state_raw` | stato grezzo SIGN IT | — | **No** — grezzo passthrough, come `stripe_status` in C3 |
| `booking_number` | business key prenotazione | `reservation_public_id` | **No** — business key denormalizzata (regola C3-8, come C1) |
| `fiskaly_response` | payload TRANSACTION SIGN IT | — | **No** — `jsonb` grezzo di sistema esterno (§3.6 punto 5) |

## Vincoli

- `CHECK (document_kind = 'non_fiscal' OR cash_register_id IS NOT NULL)` — un fiscale ha sempre una
  cassa.
- `CHECK (document_kind = 'non_fiscal' OR fiskaly_account_id IS NOT NULL)`.
- `CHECK (round(taxable_amount + vat_amount, 2) = total_amount)` — coerenza importi (tolleranza in
  migrazione).
- `CHECK (source = 'manual' OR created_by_user_id IS NULL)` — l'auto-scontrino non ha operatore.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_fiskaly_receipts` | `id` | PK | — |
| `uq_fiskaly_receipts_public_id` | `(public_id)` | UNIQUE | referenziato da C6 (invio) e in URL gestionale (`pdf/:id`, `send`) |
| `uq_fiskaly_receipts_record` | `(record_id) WHERE record_id IS NOT NULL` | UNIQUE parziale | id TRANSACTION Fiskaly; lookup CANCELLATION |
| `uq_fiskaly_receipts_ereceipt_uuid` | `(e_receipt_uuid) WHERE e_receipt_uuid IS NOT NULL` | UNIQUE parziale | nome file PDF su storage |
| `ix_fiskaly_receipts_property_date` | `(property_id, created_at DESC)` | btree | lista scontrini del gestionale |
| `ix_fiskaly_receipts_reservation` | `(reservation_public_id)` | btree | scontrini di una prenotazione (una prenotazione ne ha più d'uno) |
| `ix_fiskaly_receipts_booking_number` | `(booking_number)` | btree | risalita da `booking_number` (autorizzazioni, invio) |
| `ix_fiskaly_receipts_flow` | `(property_id, document_kind, record_state)` | btree | scontrini appesi / falliti da recuperare |
| `ix_fiskaly_receipts_completed` | `(property_id, signed_at DESC) WHERE record_state = 'completed' AND record_mode = 'finished'` | btree parziale | `getReceipts` (mostra solo `COMPLETED` + `FINISHED`) |
| `ix_fiskaly_receipts_cash_register` | `(cash_register_id, signed_at) WHERE document_kind = 'fiscal'` | btree parziale | scontrini firmati da una cassa (chiusure/riconciliazione) |

## Campi

### id
- **Legacy**: `fiskaly_receipt.id` / `fiskaly_non_fiscal_receipt.id` (spazi id disgiunti → riassegnati)
- **Descrizione**: PK tecnica. Chiave di *resume* del flusso di emissione.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da `fiskaly_receipt_lines.receipt_id`,
  `fiskaly_receipt_payments.receipt_id`, `fiskaly_receipt_logs.receipt_id`.

### public_id
- **Legacy**: — (nel legacy gli endpoint usavano l'`id` interno)
- **Descrizione**: id pubblico stabile (UUIDv7). Usato in URL gestionale (`pdf/{id}`, `send`) e
  **copiato cross-modulo** da C6 (invio scontrino via email/WhatsApp) e da C3 (auto-scontrino
  agganciato al `payment_transaction_charges`).
- **Tipo**: `uuid`.
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### property_id
- **Legacy**: — (risolto nel legacy via `system_id → cash_register → account.property_id` o
  `getPropertyIdByBookingNumber`)
- **Descrizione**: stabilimento emittente (copiato sulla riga = snapshot dello scoping).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md);
  [D-01](../_domande-committente.md).

### document_kind
- **Legacy**: la tabella di origine (`fiskaly_receipt` vs `fiskaly_non_fiscal_receipt`)
- **Descrizione**: discriminatore di testata.
- **Tipo**: `enum fiskaly.receipt_kind` — valori: `fiscal` (firmato SIGN IT, con numero progressivo),
  `non_fiscal` (pre-conto/proforma, nessun valore fiscale).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: governa i `CHECK` in §"Vincoli"; per `non_fiscal` restano `NULL`
  `cash_register_id`, `fiskaly_system_id_snapshot`, `document_number`, `record_id*`,
  `fiskaly_state_raw`, `record_mode`, `signed_at`, `fiskaly_response`.

### source
- **Legacy**: `fiskaly_receipt.source` (`varchar(20)` `NOT NULL` `DEFAULT 'other'`)
- **Descrizione**: origine dell'emissione.
- **Tipo**: `enum fiskaly.receipt_source` — valori: `manual` (ex `other` — operatore, `createReceipt`
  / `createReceiptFromBni` / `createNonFiscalReceipt`), `autoreceipt` (`createAutoReceiptFromStripe`
  dopo un pagamento Stripe).
- **Default**: `manual`.
- **Nullabile**: No.
- **Relazioni**: `autoreceipt` ⇒ `created_by_user_id IS NULL` (`CHECK`).

### fiskaly_account_id
- **Legacy**: risolto via `system_id` (nessuna colonna)
- **Descrizione**: account fiscale sotto cui è stato emesso — **FK viva** accanto agli snapshot
  venditore.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` per `non_fiscal` di property senza account Fiskaly).
- **Relazioni**: **FK interna** → [`fiskaly_accounts.id`](fiskaly_accounts.md).

### cash_register_id
- **Legacy**: `fiskaly_receipt.system_id` (`varchar(36)`, FK → `fiskaly_cash_register.system_id`)
- **Descrizione**: cassa che ha firmato il documento.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`NULL` per `non_fiscal`; obbligatorio per `fiscal`).
- **Relazioni**: **FK interna** → [`fiskaly_cash_registers.id`](fiskaly_cash_registers.md).

### fiskaly_system_id_snapshot
- **Legacy**: `fiskaly_receipt.system_id` (valore grezzo)
- **Descrizione**: id `System` Fiskaly (`019e…`) **congelato** al momento della firma — regola
  `_snapshot` per identificativi esterni copiati da un'entità la cui riga può essere sostituita
  (decommission → nuovo `system_id`), con la FK viva `cash_register_id` accanto. Serve a correlare
  con la dashboard Fiskaly anche dopo una ricreazione della cassa.
- **Tipo**: `varchar(36)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (solo `fiscal`).
- **Relazioni**: snapshot di `fiskaly_cash_registers.fiskaly_system_id`.

### document_number
- **Legacy**: `fiskaly_receipt.document_number` (`varchar(20)`)
- **Descrizione**: **numero progressivo fiscale** del documento commerciale, assegnato al momento
  della firma dalla sequenza giornaliera della cassa
  ([`fiskaly_receipt_sequences`](fiskaly_receipt_sequences.md)). **Non è un identificativo univoco**:
  riparte da 1 ogni giorno e per ogni cassa.
- **Tipo**: `integer`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`NULL` per `non_fiscal` e finché non firmato).
- **Relazioni**: `(cash_register_id, signed_at::date)` → `fiskaly_receipt_sequences`.

### record_id
- **Legacy**: `fiskaly_receipt.record_id` (`varchar(36)`, UUID v7, `UNIQUE`)
- **Descrizione**: id del record TRANSACTION::RECEIPT su Fiskaly. Referenziato dalla CANCELLATION.
- **Tipo**: `varchar(36)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`NULL` finché la TRANSACTION non è riuscita; tenuto `NULL` anche in stato
  `transaction_failed`).
- **Relazioni**: `UNIQUE` parziale.

### intention_record_id / intention_cancellation_record_id
- **Legacy**: `fiskaly_receipt.intention_record_id` / `intention_cancellation_record_id` (`varchar(36)`)
- **Descrizione**: id del record INTENTION consumato dalla TRANSACTION di emissione / di
  annullamento. Un INTENTION → una sola TRANSACTION: se fallisce, il resume ne conia uno nuovo.
- **Tipo**: `varchar(36)` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### record_state
- **Legacy**: `fiskaly_receipt.record_state` (`varchar(50)` — enum interno **+** passthrough
  `content.state` nella stessa colonna; costante `FiskalyReceiptState` disallineata)
- **Descrizione**: stato **interno** del flusso di emissione/annullamento (o generazione, per
  `non_fiscal`).
- **Tipo**: `enum fiskaly.receipt_record_state` — valori: `intention_created`,
  `transaction_created`, `transaction_failed`, `ereceipt_failed`, `s3upload_failed`, `completed`,
  `cancellation_intention_created`, `cancellation_transaction_failed`, `cancelled`,
  `generated` *(non_fiscal ok)*, `generation_failed` *(non_fiscal)*.
- **Default**: `intention_created` (fiscal) / `generated` (non_fiscal).
- **Nullabile**: No.
- **Relazioni**: distribuzione prod fiscale: `completed` 92%, `cancelled` ~4%, `transaction_failed`
  2%, appesi ~1%. `getReceipts` mostra solo `completed`.

### fiskaly_state_raw
- **Legacy**: `fiskaly_receipt.record_state` (quando conteneva il `content.state` Fiskaly) /
  `fiskaly_response.state`
- **Descrizione**: stato **grezzo** del record TRANSACTION restituito da SIGN IT (`COMPLETED`,
  `FAILED`, …), verbatim — snapshot per diagnostica, mai normalizzato.
- **Tipo**: `varchar(40)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: separato dallo stato interno ([`docs/03 §2.4ter`](../../../progettazione/03-gestione-dati.md)).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 2.549 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### record_mode
- **Legacy**: `fiskaly_receipt.record_mode` (`varchar(50)`, nei dati sempre `FINISHED`)
- **Descrizione**: `mode` del record TRANSACTION Fiskaly.
- **Tipo**: `enum fiskaly.record_mode` — valori: `finished`.
- **Default**: `NULL`.
- **Nullabile**: Sì (solo `fiscal`).
- **Relazioni**: `getReceipts` filtra `finished`.

### signed_at
- **Legacy**: `fiskaly_receipt.signed_date` (`datetime`, `moment.utc().tz('Europe/Rome')`)
- **Descrizione**: istante di firma. Data documento nel PDF (`DD/MM/YYYY HH:mm`).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`NULL` finché non firmato; sempre `NULL` per `non_fiscal`).
- **Relazioni**: determina il giorno fiscale per la sequenza numerica.

### currency
- **Legacy**: `fiskaly_receipt.currency` / `fiskaly_non_fiscal_receipt.currency` (`varchar(3)`
  `DEFAULT 'EUR'`, hardcoded)
- **Descrizione**: valuta ISO 4217 del documento.
- **Tipo**: `char(3)`.
- **Default**: `'EUR'`.
- **Nullabile**: No.
- **Relazioni**: insieme chiuso condiviso (`SharedKernel`, [`docs/03 §2.4ter`](../../../progettazione/03-gestione-dati.md)).

### language_code
- **Legacy**: `fiskaly_receipt.languageCode` / `fiskaly_non_fiscal_receipt.languageCode`
  (`varchar(5)` `DEFAULT 'it'`; endpoint `send` accettano anche `1`/`2`)
- **Descrizione**: lingua del PDF / eReceipt.
- **Tipo**: `enum fiskaly.language_code` — valori: `it`, `en`. Vocabolario **SharedKernel**
  (`LanguageCode`, [`docs/03 §2.4ter`](../../../progettazione/03-gestione-dati.md), [`docs/12-lingue-e-date.md`](../../../progettazione/12-lingue-e-date.md)).
- **Default**: `it`.
- **Nullabile**: No.
- **Relazioni**: unificata la doppia convenzione stringa/numero in migrazione.

### total_amount
- **Legacy**: `fiskaly_receipt.amount_total` (netto pagato = `payments[0].amount − discount`) /
  `fiskaly_non_fiscal_receipt.amount_total` (`full_amount.incl_vat`) — **semantiche divergenti**
- **Descrizione**: totale del documento **IVA inclusa**, sconto già applicato — quanto il cliente
  paga con questo scontrino.
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `CHECK (round(taxable_amount + vat_amount, 2) = total_amount)`.

### taxable_amount
- **Legacy**: `fiskaly_receipt.amount_net` (`full_amount.excl_vat`, ma **lordo** rispetto allo sconto)
- **Descrizione**: imponibile del documento (al netto dello sconto, coerente con `total_amount`).
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `Σ fiskaly_receipt_lines.line_net` (con arrotondamento).

### vat_amount
- **Legacy**: `fiskaly_receipt.amount_vat` (`Σ vat_amounts[].vat`, lordo rispetto allo sconto)
- **Descrizione**: totale IVA del documento (coerente con `total_amount`/`taxable_amount`).
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `Σ fiskaly_receipt_lines.vat_amount`.

### discount_amount
- **Legacy**: `payments[].details.discount` / `payments[].discount`
- **Descrizione**: sconto totale applicato al documento (già scontato da `total_amount`; qui
  esplicito per l'aggiustamento IVA che il legacy faceva **solo nel PDF**).
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### invoiced_payment_amount / invoiced_payment_at / invoiced_payment_mode
- **Legacy**: `fiskaly_receipt.paid_deltas.payment` (`{ amount, date, paymentMethod, source }`)
- **Descrizione**: l'acconto/incasso **specifico** che questo scontrino ha fatturato (una
  prenotazione ha più scontrini, uno per incasso). Serviva nel legacy a "liberarlo" in caso di
  revert; qui è dato di consultazione — il revert è uno storno in **C3**.
- **Tipo**: `numeric(12,2)` / `timestamptz` / `enum fiskaly.payment_mode` (valori `cash` /
  `credit_card` / `bank_transfer` / `other` — **vocabolario condiviso** con `booking.payment_mode` /
  `payment.payment_mode`, [`docs/03 §2.4bis`](../../../progettazione/03-gestione-dati.md)).
- **Default**: `NULL` / `NULL` / `NULL`.
- **Nullabile**: Sì / Sì / Sì.
- **Relazioni**: correlato a una riga di [`payment_ledger_entries`](../payment/payment_ledger_entries.md)
  via `booking_number` + importo (nessuna FK cross-modulo).

### seller_legal_name_snapshot / seller_trade_name_snapshot / seller_vat_number_snapshot / seller_fiscal_code_snapshot / seller_address_snapshot
- **Legacy**: derivati da `fiskaly_account.taxpayer_*` al momento della generazione del PDF (letti
  live nel legacy, non congelati → un cambio anagrafica alterava retroattivamente i PDF rigenerati)
- **Descrizione**: identità del venditore **congelata** all'emissione (regola `_snapshot`, §3.6
  punto 2). `seller_address_snapshot` = via + civico + CAP + città + `ITA` come stringa già
  formattata per il documento.
- **Tipo**: `varchar(128)` ×3 · `varchar(16)` · `varchar(300)`.
- **Default**: nessuno (fiscal) / — .
- **Nullabile**: Sì (`NULL` per `non_fiscal` di property senza account, altrimenti valorizzati).
- **Relazioni**: snapshot di [`fiskaly_accounts`](fiskaly_accounts.md) accanto alla FK viva
  `fiskaly_account_id`.

### reservation_public_id
- **Legacy**: `fiskaly_receipt.booking_number` (risolto a `bookings`/`service_reservation`)
- **Descrizione**: prenotazione fatturata (testata unica C1).
- **Tipo**: `uuid` (= `public_id` di `booking_reservations`).
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` solo se la migrazione non risolve il `booking_number` legacy — atteso
  raro).
- **Relazioni**: **`public_id` (uuid) copiato** da C1 [`booking_reservations`](../booking/booking_reservations.md).

### booking_number
- **Legacy**: `fiskaly_receipt.booking_number` (`varchar(50)`) / `fiskaly_non_fiscal_receipt.booking_number`
  (`varchar(255)`)
- **Descrizione**: numero prenotazione — **business key denormalizzata** (nessun suffisso `_snapshot`,
  regola C3-8), comoda per autorizzazioni e invio senza join.
- **Tipo**: `varchar(32)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `booking_reservations.booking_number` (per valore).

### pdf_asset_id
- **Legacy**: `fiskaly_receipt.e_receipt_path` (`varchar(500)`, **URL S3 completo**)
- **Descrizione**: riferimento al PDF eReceipt scomposto in `origin` + `path`.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`NULL` se l'upload non è ancora riuscito — stato `s3upload_failed`).
- **Relazioni**: **FK interna** → [`fiskaly_media_assets.id`](fiskaly_media_assets.md).

### e_receipt_uuid
- **Legacy**: `fiskaly_receipt.e_receipt_id` (`varchar(36)`, UUID v4, `UNIQUE`)
- **Descrizione**: id del PDF eReceipt (generato con `uuidv4()`), usato come **nome file** su storage.
- **Tipo**: `varchar(36)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `UNIQUE` parziale; ridondante con `pdf_asset_id.path` ma mantenuto per tracciabilità.

### fiskaly_response
- **Legacy**: `fiskaly_receipt.fiskaly_response` (`longtext` JSON — `CHECK (json_valid(...))`)
- **Descrizione**: **risposta grezza completa** di Fiskaly per il record TRANSACTION
  (`transactionResponse.content`) — audit/diagnostica. Documento di un sistema esterno di cui non
  possediamo lo schema (§3.6 punto 5): tenuto `jsonb` **così com'è**, **niente** suffisso `_snapshot`.
- **Tipo**: `jsonb`.
- **Default**: `NULL`.
- **Nullabile**: Sì (solo `fiscal`).
- **Relazioni**: —. Eventuali colonne di riepilogo derivate (es. `journal_sequence`) sono read-only,
  non autorevoli.

### cancelled_at / cancelled_by_user_id / cancellation_reason
- **Legacy**: dedotti da `record_state = CANCELLED` + log `fiskaly_receipt_log` (`"Receipt cancelled
  by user"`)
- **Descrizione**: quando / chi ha annullato lo scontrino e perché.
- **Tipo**: `timestamptz` · `uuid` (= `public_id` di `identity_access_users`) · `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì / Sì / Sì.
- **Relazioni**: `cancelled_by_user_id` **`public_id` (uuid) copiato** da C5.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 2.549 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_by_user_id
- **Legacy**: `fiskaly_receipt.createdBy` / `fiskaly_non_fiscal_receipt.createdBy` (`int`, `users.id`,
  nessuna FK; `NULL` per gli automatici)
- **Descrizione**: operatore che ha emesso il documento.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` per `source = autoreceipt`).
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_access_users`](../identity/identity_access_users.md).

### created_at / updated_at
- **Legacy**: `fiskaly_receipt.createdAt` / `updatedAt` (`datetime`, `timestamps: false`)
- **Descrizione**: creazione riga (inizio flusso) / ultimo step. `getReceipts` filtra per intervallo
  su `createdAt`.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: automatici.

### deleted_at
- **Legacy**: nessuno (l'annullamento è `record_state = CANCELLED`, non un soft-delete)
- **Descrizione**: esposto per uniformità ([`docs/03 §3.5`](../../../progettazione/03-gestione-dati.md)) ma
  **non usato**: fatto fiscale, conservazione **10 anni** ([`docs/80-retention.md`](../../../progettazione/80-retention.md));
  l'annullamento è uno stato.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: Global Query Filter EF (di fatto sempre `NULL`).

## Valutazione JSON

- `entries` (array) → [`fiskaly_receipt_lines`](fiskaly_receipt_lines.md) (righe tipizzate).
- `payments` (array) → [`fiskaly_receipt_payments`](fiskaly_receipt_payments.md).
- `paid_deltas` (oggetto) → `paid_allocation` per riga su `fiskaly_receipt_lines` + i 3 scalari
  `invoiced_payment_*` qui. Il revert (che nel legacy azzerava `paid_deltas`) diventa un movimento di
  **storno append-only in C3**.
- `fiskaly_response` (oggetto) → **resta `jsonb`**: passthrough di SIGN IT, schema di terzi (§3.6
  punto 5). Unica eccezione "niente JSON" ammessa per questa tabella.

## Suggerimenti per le prestazioni

- ~2.6k righe oggi, crescita moderata (2-3 stabilimenti emettono davvero). Diventerà "calda" con
  l'adozione: gli indici su `booking_number` / `reservation_public_id` eliminano le 2 query
  `getPropertyIdByBookingNumber` del legacy.
- Indice parziale su `record_state IN (…falliti/appesi…)` per il job di riconciliazione/resume.
- `fiskaly_response jsonb` può pesare (payload SIGN IT): valutare `EXTERNAL`/`toast` di default (lo è)
  e non selezionarlo nelle liste.
- Partizionamento per anno di `created_at` quando lo storico supera qualche milione di righe
  (retention fiscale 10 anni, nessun hard-delete).

  > **Finestra online validata (2026-09-21): 36 mesi.** Il titolare ha esteso la proposta di ~24 mesi
  > ([`docs/82-retention-data.md` §3.3](../../../progettazione/82-retention-data.md)): oltre i 36 mesi metadati e
  > PDF si spostano su storage freddo (S3 Glacier), **mai** si cancellano prima dei 10 anni previsti
  > dall'art. 2220 c.c. — nemmeno su richiesta dell'interessato (art. 17.3.b GDPR). Attenzione a non
  > applicare qui il "24 mesi" del cheat sheet C4 di [`80-retention.md` §7](../../../progettazione/80-retention.md):
  > quella riga accorpa scontrini e movimenti contabili, e per gli scontrini **vince il valore
  > specifico** di 36 mesi. Job `ArchiveFiscalReceipts`, [`docs/20-jobs.md` §1.5](../../../progettazione/20-jobs.md).
- La testata unica evita una `UNION` fra due tabelle nella schermata "documenti della prenotazione".

## Vista derivata `fiskaly.v_daily_cash_closure`

Unica vista dello schema nuovo, decisa dal titolare il 2026-09-18
([`docs/05-ottimizzazioni-db.md` §11.2](../../../progettazione/05-ottimizzazioni-db.md)): la **chiusura cassa
giornaliera** richiesta dalla normativa sui corrispettivi telematici. Vista semplice, non materializzata.
Creata dalla migration `AddDailyCashClosureView`, dal 2026-10-06 nella `InitialCreate` di
`Module.Fiskaly` fra gli «oggetti che il modello EF non sa esprimere»
([`docs/42-migration.md` §10](../../../progettazione/42-migration.md)); il `Down` la toglie per prima.

Legge solo scontrini `record_state = 'completed'`, firmati (`signed_at IS NOT NULL`) e non cancellati, con
righe e pagamenti non cancellati. Una riga per (stabilimento, cassa, giorno fiscale, ripartizione):

| Colonna | Tipo | Contenuto |
|---|---|---|
| `property_id` | `uuid` | da `receipts.property_id` |
| `cash_register_id` | `bigint` | da `receipts.cash_register_id` |
| `fiscal_date` | `date` | `(signed_at AT TIME ZONE 'Europe/Rome')::date`: il giorno fiscale è quello italiano, non UTC |
| `breakdown_kind` | `text` | `payment` (per forma di pagamento) o `vat` (per aliquota) |
| `breakdown_key` | `text` | `payment`: `receipt_payments.payment_type`; `vat`: `vat_codes.code`, o `vat_kind` se la riga non ha codice |
| `vat_rate` | `numeric(5,2)` | solo `vat`; `NULL` su `payment` |
| `taxable_amount` / `vat_amount` | `numeric(12,2)` | solo `vat`: somma di `line_net` / `vat_amount`; `NULL` su `payment` |
| `total_amount` | `numeric` | `payment`: somma di `receipt_payments.amount`; `vat`: somma di `line_gross` |
| `receipt_count` | `bigint` | scontrini distinti del gruppo |

⚠️ **Le due famiglie non si sommano fra loro**: sono due ripartizioni dello stesso incasso, non due
addendi. Per questo la vista è un `UNION ALL` di due aggregazioni separate e non un solo `JOIN`: la
specifica originale di §11.2 univa pagamenti e righe IVA nello stesso `JOIN`, che moltiplica gli importi
(2 pagamenti × 3 righe = somma contata tre volte). Errore corretto in implementazione, totali riconciliati.

## Implementazione .NET

`Receipt` è l'**aggregate root** del modulo `Module.Fiskaly` (schema `fiskaly`): il documento
commerciale emesso (fiscale o non fiscale), testata unica. Possiede come owned collection le
[`fiskaly_receipt_lines`](fiskaly_receipt_lines.md) (`Lines`) e le
[`fiskaly_receipt_payments`](fiskaly_receipt_payments.md) (`Payments`); le
[`fiskaly_receipt_logs`](fiskaly_receipt_logs.md) sono un'entità **separata** append-only (non owned:
deve poter scrivere anche quando l'aggregato fa rollback). Referenzia
[`fiskaly_accounts`](fiskaly_accounts.md) e [`fiskaly_cash_registers`](fiskaly_cash_registers.md) con
FK interne reali; verso C1/C3/C5/C6 solo id copiati per valore, nessuna FK cross-schema.

### Enum propri

```csharp
// Domain/Enums/ReceiptKind.cs
public enum ReceiptKind { Fiscal = 0, NonFiscal = 1 }

// Domain/Enums/ReceiptSource.cs
public enum ReceiptSource { Manual = 0, Autoreceipt = 1 }

// Domain/Enums/ReceiptRecordState.cs
public enum ReceiptRecordState
{
	IntentionCreated = 0,
	TransactionCreated = 1,
	TransactionFailed = 2,
	EreceiptFailed = 3,
	S3UploadFailed = 4,
	Completed = 5,
	CancellationIntentionCreated = 6,
	CancellationTransactionFailed = 7,
	Cancelled = 8,
	Generated = 9,        // non_fiscal
	GenerationFailed = 10 // non_fiscal
}

// Domain/Enums/RecordMode.cs
public enum RecordMode { Finished = 0 }

// Domain/Enums/FiskalyPaymentMode.cs — vocabolario condiviso con booking.payment_mode / payment.payment_mode
public enum FiskalyPaymentMode { Cash = 0, CreditCard = 1, BankTransfer = 2, Other = 3 }
```

`LanguageCode` (`it`/`en`) è il vocabolario **SharedKernel** `shared.language_code` (vedi
[`docs/12-lingue-e-date.md`](../../../progettazione/12-lingue-e-date.md)) — non ridefinito qui, solo mappato.

```csharp
// FiskalyDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ReceiptKind>("fiskaly", "receipt_kind");
modelBuilder.HasPostgresEnum<ReceiptSource>("fiskaly", "receipt_source");
modelBuilder.HasPostgresEnum<ReceiptRecordState>("fiskaly", "receipt_record_state");
modelBuilder.HasPostgresEnum<RecordMode>("fiskaly", "record_mode");
modelBuilder.HasPostgresEnum<FiskalyPaymentMode>("fiskaly", "payment_mode");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<ReceiptKind>("fiskaly.receipt_kind");
dataSourceBuilder.MapEnum<ReceiptSource>("fiskaly.receipt_source");
dataSourceBuilder.MapEnum<ReceiptRecordState>("fiskaly.receipt_record_state");
dataSourceBuilder.MapEnum<RecordMode>("fiskaly.record_mode");
dataSourceBuilder.MapEnum<FiskalyPaymentMode>("fiskaly.payment_mode");
```

### Configuration

```csharp
namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
	public void Configure(EntityTypeBuilder<Receipt> builder)
	{
		builder.ToTable("fiskaly_receipts", "fiskaly", t =>
		{
			t.HasComment("Documento commerciale emesso (fiscale o non fiscale) per una prenotazione — testata unica, fatto fiscale storico immutabile.");
			t.HasCheckConstraint("ck_fiskaly_receipts_fiscal_cash_register",
				"document_kind = 'non_fiscal' OR cash_register_id IS NOT NULL");
			t.HasCheckConstraint("ck_fiskaly_receipts_fiscal_account",
				"document_kind = 'non_fiscal' OR fiskaly_account_id IS NOT NULL");
			t.HasCheckConstraint("ck_fiskaly_receipts_amounts",
				"round(taxable_amount + vat_amount, 2) = total_amount");
			t.HasCheckConstraint("ck_fiskaly_receipts_autoreceipt_no_user",
				"source = 'manual' OR created_by_user_id IS NULL");
		});

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_receipts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile (UUIDv7); usato in URL gestionale e copiato cross-modulo da C3/C6.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_fiskaly_receipts_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Stabilimento emittente.");
		builder.HasIndex(x => new { x.PropertyId, x.CreatedAt })
			.HasDatabaseName("ix_fiskaly_receipts_property_date");
		builder.HasIndex(x => new { x.PropertyId, x.DocumentKind, x.RecordState })
			.HasDatabaseName("ix_fiskaly_receipts_flow");
		builder.HasIndex(x => new { x.PropertyId, x.SignedAt })
			.HasFilter("record_state = 'completed' AND record_mode = 'finished'")
			.HasDatabaseName("ix_fiskaly_receipts_completed");

		builder.Property(x => x.DocumentKind)
			.HasColumnName("document_kind")
			.HasColumnType("fiskaly.receipt_kind")
			.IsRequired()
			.HasComment("Discriminatore di testata: fiscal (firmato SIGN IT) oppure non_fiscal (pre-conto/proforma).");

		builder.Property(x => x.Source)
			.HasColumnName("source")
			.HasColumnType("fiskaly.receipt_source")
			.IsRequired()
			.HasDefaultValue(ReceiptSource.Manual)
			.HasComment("Origine dell'emissione: manual (operatore) oppure autoreceipt (post-pagamento Stripe).");

		builder.Property(x => x.FiskalyAccountId)
			.HasColumnName("fiskaly_account_id")
			.HasComment("Account fiscale sotto cui è stato emesso il documento.");
		builder.HasOne<FiskalyAccount>()
			.WithMany()
			.HasForeignKey(x => x.FiskalyAccountId)
			.HasConstraintName("fk_fiskaly_receipts_account")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.CashRegisterId)
			.HasColumnName("cash_register_id")
			.HasComment("Registratore di cassa che ha firmato il documento.");
		builder.HasOne<CashRegister>()
			.WithMany()
			.HasForeignKey(x => x.CashRegisterId)
			.HasConstraintName("fk_fiskaly_receipts_cash_register")
			.OnDelete(DeleteBehavior.Restrict);
		builder.HasIndex(x => new { x.CashRegisterId, x.SignedAt })
			.HasFilter("document_kind = 'fiscal'")
			.HasDatabaseName("ix_fiskaly_receipts_cash_register");

		builder.Property(x => x.FiskalySystemIdSnapshot)
			.HasColumnName("fiskaly_system_id_snapshot")
			.HasMaxLength(36)
			.HasComment("Id System Fiskaly congelato al momento della firma — snapshot accanto alla FK viva cash_register_id.");

		builder.Property(x => x.DocumentNumber)
			.HasColumnName("document_number")
			.HasComment("Numero progressivo fiscale assegnato dalla sequenza giornaliera della cassa; non univoco (riparte ogni giorno/cassa).");

		builder.Property(x => x.RecordId)
			.HasColumnName("record_id")
			.HasMaxLength(36)
			.HasComment("Id del record TRANSACTION::RECEIPT su Fiskaly, referenziato dalla CANCELLATION.");
		builder.HasIndex(x => x.RecordId)
			.IsUnique()
			.HasFilter("record_id IS NOT NULL")
			.HasDatabaseName("uq_fiskaly_receipts_record");

		builder.Property(x => x.IntentionRecordId)
			.HasColumnName("intention_record_id")
			.HasMaxLength(36)
			.HasComment("Id del record INTENTION consumato dalla TRANSACTION di emissione.");

		builder.Property(x => x.IntentionCancellationRecordId)
			.HasColumnName("intention_cancellation_record_id")
			.HasMaxLength(36)
			.HasComment("Id del record INTENTION consumato dalla TRANSACTION di annullamento.");

		builder.Property(x => x.RecordState)
			.HasColumnName("record_state")
			.HasColumnType("fiskaly.receipt_record_state")
			.IsRequired()
			.HasComment("Stato interno del flusso di emissione/annullamento/generazione.");

		builder.Property(x => x.FiskalyStateRaw)
			.HasColumnName("fiskaly_state_raw")
			.HasMaxLength(40)
			.HasComment("Stato grezzo del record TRANSACTION restituito da SIGN IT, verbatim, per diagnostica.");

		builder.Property(x => x.RecordMode)
			.HasColumnName("record_mode")
			.HasColumnType("fiskaly.record_mode")
			.HasComment("Mode del record TRANSACTION Fiskaly.");

		builder.Property(x => x.SignedAt)
			.HasColumnName("signed_at")
			.HasComment("Istante di firma; determina il giorno fiscale della sequenza numerica e la data documento sul PDF.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.IsRequired()
			.HasDefaultValue("EUR")
			.HasComment("Valuta ISO 4217 del documento.");

		builder.Property(x => x.LanguageCode)
			.HasColumnName("language_code")
			.HasColumnType("shared.language_code")
			.IsRequired()
			.HasDefaultValue(LanguageCode.It)
			.HasComment("Lingua del PDF / eReceipt.");

		builder.Property(x => x.TotalAmount)
			.HasColumnName("total_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Totale del documento IVA inclusa, sconto già applicato — quanto il cliente paga con questo scontrino.");

		builder.Property(x => x.TaxableAmount)
			.HasColumnName("taxable_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Imponibile del documento, al netto dello sconto.");

		builder.Property(x => x.VatAmount)
			.HasColumnName("vat_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Totale IVA del documento.");

		builder.Property(x => x.DiscountAmount)
			.HasColumnName("discount_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Sconto totale applicato al documento, già scontato da total_amount.");

		builder.Property(x => x.InvoicedPaymentAmount)
			.HasColumnName("invoiced_payment_amount")
			.HasColumnType("numeric(12,2)")
			.HasComment("Importo dell'incasso specifico che questo scontrino ha fatturato.");

		builder.Property(x => x.InvoicedPaymentAt)
			.HasColumnName("invoiced_payment_at")
			.HasComment("Data dell'incasso fatturato da questo scontrino.");

		builder.Property(x => x.InvoicedPaymentMode)
			.HasColumnName("invoiced_payment_mode")
			.HasColumnType("fiskaly.payment_mode")
			.HasComment("Forma dell'incasso fatturato da questo scontrino (vocabolario condiviso con booking/payment).");

		builder.Property(x => x.SellerLegalNameSnapshot)
			.HasColumnName("seller_legal_name_snapshot")
			.HasMaxLength(128)
			.HasComment("Ragione sociale del venditore congelata al momento dell'emissione.");

		builder.Property(x => x.SellerTradeNameSnapshot)
			.HasColumnName("seller_trade_name_snapshot")
			.HasMaxLength(128)
			.HasComment("Nome commerciale del venditore congelato al momento dell'emissione.");

		builder.Property(x => x.SellerVatNumberSnapshot)
			.HasColumnName("seller_vat_number_snapshot")
			.HasMaxLength(128)
			.HasComment("Partita IVA del venditore congelata al momento dell'emissione.");

		builder.Property(x => x.SellerFiscalCodeSnapshot)
			.HasColumnName("seller_fiscal_code_snapshot")
			.HasMaxLength(16)
			.HasComment("Codice fiscale del venditore congelato al momento dell'emissione.");

		builder.Property(x => x.SellerAddressSnapshot)
			.HasColumnName("seller_address_snapshot")
			.HasMaxLength(300)
			.HasComment("Indirizzo del venditore congelato e già formattato per il documento al momento dell'emissione.");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.reservations, copiato — nessuna FK cross-schema. Prenotazione fatturata.");
		builder.HasIndex(x => x.ReservationPublicId).HasDatabaseName("ix_fiskaly_receipts_reservation");

		builder.Property(x => x.BookingNumber)
			.HasColumnName("booking_number")
			.HasMaxLength(32)
			.IsRequired()
			.HasComment("Numero prenotazione — business key denormalizzata per autorizzazioni e invio senza join.");
		builder.HasIndex(x => x.BookingNumber).HasDatabaseName("ix_fiskaly_receipts_booking_number");

		builder.Property(x => x.PdfAssetId)
			.HasColumnName("pdf_asset_id")
			.HasComment("Riferimento al PDF eReceipt del documento.");
		builder.HasOne<MediaAsset>()
			.WithMany()
			.HasForeignKey(x => x.PdfAssetId)
			.HasConstraintName("fk_fiskaly_receipts_pdf_asset")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.EReceiptUuid)
			.HasColumnName("e_receipt_uuid")
			.HasMaxLength(36)
			.HasComment("Id del PDF eReceipt, usato come nome file su storage.");
		builder.HasIndex(x => x.EReceiptUuid)
			.IsUnique()
			.HasFilter("e_receipt_uuid IS NOT NULL")
			.HasDatabaseName("uq_fiskaly_receipts_ereceipt_uuid");

		builder.Property(x => x.FiskalyResponse)
			.HasColumnName("fiskaly_response")
			.HasColumnType("jsonb")
			.HasComment("Risposta grezza completa di Fiskaly per il record TRANSACTION — passthrough di sistema esterno, solo audit/diagnostica.");

		builder.Property(x => x.CancelledAt)
			.HasColumnName("cancelled_at")
			.HasComment("Istante di annullamento dello scontrino.");

		builder.Property(x => x.CancelledByUserId)
			.HasColumnName("cancelled_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Chi ha annullato lo scontrino.");

		builder.Property(x => x.CancellationReason)
			.HasColumnName("cancellation_reason")
			.HasMaxLength(255)
			.HasComment("Motivo dell'annullamento.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha emesso il documento; NULL per source=autoreceipt.");

		builder.HasMany(x => x.Lines)
			.WithOne()
			.HasForeignKey(l => l.ReceiptId)
			.HasConstraintName("fk_fiskaly_receipt_lines_receipt")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(x => x.Payments)
			.WithOne()
			.HasForeignKey(p => p.ReceiptId)
			.HasConstraintName("fk_fiskaly_receipt_payments_receipt")
			.OnDelete(DeleteBehavior.Cascade);

		// created_at/updated_at/deleted_at (ISoftDeletable/IAuditable) — plumbing comune; deleted_at di fatto sempre NULL (l'annullamento è uno stato, non un soft-delete)
	}
}
```

`Receipt` implementa `IAuditable`; espone `ISoftDeletable` solo per uniformità (`deleted_at` resta di
fatto sempre `NULL`, retention fiscale 10 anni, l'annullamento è `record_state = Cancelled`, non un
soft-delete).

### Flusso applicativo (Application/Infrastructure, non EF)

- `CreateReceipt` gira in **una transazione** + **outbox**: numero da
  `IReceiptNumberSequence.NextAsync` (upsert atomico su `fiskaly_receipt_sequences`), chiamate SIGN IT
  INTENTION → TRANSACTION con logica di resume per `Id`, upload PDF → `fiskaly_media_assets`, evento
  di dominio `ReceiptIssued` pubblicato via outbox (consumato da C3 per lo storno su annullamento, da
  C6 per l'invio email/WhatsApp).
- `FiskalyResponse` esposto in Application come `JsonDocument` read-only; eventuali proiezioni per la
  UI (es. riepilogo `journal_sequence`) sono calcolate, mai colonne autorevoli.

### Note di migrazione

- `UNION` di `fiskaly_receipt` + `fiskaly_non_fiscal_receipt` con `document_kind` a discriminare
  (spazi id disgiunti → riassegnati).
- Cast `varchar` → `numeric(12,2)` con riconciliazione dei 3 importi storicamente divergenti
  (`amount_total`/`amount_vat`/`amount_net`) verso `total_amount`/`taxable_amount`/`vat_amount` +
  `discount_amount` esplicito (tolleranza di arrotondamento in migrazione).
- `record_state` (enum interno + passthrough `content.state` nella stessa colonna) → `record_state`
  (enum) separato da `fiskaly_state_raw` (grezzo).
- `system_id` → `cash_register_id` (FK) + `fiskaly_system_id_snapshot` (congelato).
- `e_receipt_path` (URL S3 completo) → risoluzione a `fiskaly_media_assets` esistente/nuova, popolando
  `pdf_asset_id`.
- `booking_number` → `reservation_public_id` via mappa C1 (booking_number non risolvibile → resta
  `NULL`, atteso raro).
- `emailSent` / `whatsappSent` → righe `notification.deliveries` (C6), non migrati su questa tabella.
- `paid_deltas` (oggetto) → i 3 scalari `invoiced_payment_*` qui + `paid_allocation` per riga su
  `fiskaly_receipt_lines`.
