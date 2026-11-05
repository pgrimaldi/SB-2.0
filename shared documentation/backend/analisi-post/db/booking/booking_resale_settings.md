# Tabella `booking_resale_settings`

> **Dominio**: C1 — `booking` · **sottoarea**: `hold` (rivendita) · **schema**: `booking` · **nome flat**:
> `booking_resale_settings` · **forma canonica**: `booking.resale_settings`.
>
> Sostituisce i **7 campi "sharing" di
> [`configurations`](../../../analisi-pre/db/configurations.md)** (`sharing_enable`,
> `sharing_seasonal_workers`, `credit_perday_resold`, `credit_value`, `disable_both_options`,
> `enable_sharing_on_all_article`, `plan_absence_as_sharing`) — che nel legacy vivono nella config della
> mappa spiaggia (C2) "solo per accidente": sono parametri della feature rivendita/`hold` di **C1**
> ([`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md), campo cross-dominio). Fonti:
> [`analisi-pre/db/configurations.md`](../../../analisi-pre/db/configurations.md).

---

## Descrizione

**Una riga per stabilimento**: configurazione della feature "sharing / rivendita del posto". Guida il
comportamento di [`booking_resale_offers`](booking_resale_offers.md) e
[`booking_resale_credit_ledger`](booking_resale_credit_ledger.md).

Correzioni rispetto al legacy:

- **7 campi `int`/`boolean`/`varchar` dentro `configurations` (C2)** → tabella dedicata C1, 1 riga per
  property (`UNIQUE`, mancava).
- **`credit_value` testo libero molto sporco** (`'5€'`, `'5 €'`, `'5 Euro'`, `'5%'`,
  `'30% del giornaliero'`, `''`, `'0'`) → `manual_credit_amount numeric(12,2)` in euro.
- **`disable_both_options`** → tolto (BK-24: sharing spento = `is_enabled = false`).
- **Allineata alla pagina Impostazioni › Sharing del front (2026-11-05)**: giorni minimi, tre modalità di credito
  indipendenti con il loro importo in euro (manuale, voucher automatico, ricarica tessera); niente più credito in
  percentuale.
- **`sharing_enable = 0` sul 92%** → default `is_enabled = false`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_resale_settings` | `id` | PK | — |
| `uq_..._property` | `(property_id)` | UNIQUE | 1 riga per stabilimento (mancava nel legacy) |

## Campi

### id
- **Legacy**: `configurations.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### property_id
- **Legacy**: `configurations.propertyId`
- **Descrizione**: stabilimento.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md); `UNIQUE`.

### is_enabled
- **Legacy**: `configurations.sharing_enable` (`int` 0/1; `1` su ~13 righe)
- **Descrizione**: la feature rivendita/sharing è attiva per lo stabilimento.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: se `false`, non si creano offerte di rivendita.

### min_booking_days
- **Legacy**: — (nuovo; pagina Impostazioni › Sharing, «Durata minima prenotazione per applicare lo Sharing»)
- **Descrizione**: giorni minimi della prenotazione perché compaia il pulsante Sharing (BK-24). `NULL` = nessun
  minimo (nel front `isMinimumBookingDayEnabled = false`).
- **Tipo**: `integer`, `CHECK (min_booking_days > 0)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### applies_to
- **Legacy**: `configurations.sharing_seasonal_workers` (`int` 0/1; `1` su ~7 righe) +
  `configurations.enable_sharing_on_all_article` (`int` 0/1; `1` su 3 righe)
- **Descrizione**: limiti ed estensioni della rivendita. Flag **non esclusivi** dello stesso argomento → array.
- **Tipo**: `enum booking.resale_scope[]` — valori: `seasonal_only` («Abilita solo sugli stagionali», ex
  `sharing_seasonal_workers`; front `isOnlySeasonalEnabled`), `all_articles` («Abilita su tutti gli articoli»: la
  cessione riguarda tutte le postazioni del pacchetto; ex `enable_sharing_on_all_article`; front
  `isEnabledOnAllArticles`).
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: regola «più flag dello stesso argomento → `enum[]`»
  ([`docs/03-gestione-dati.md` §3.10](../../../progettazione/03-gestione-dati.md)).

### absences_as_sharing
- **Legacy**: `configurations.plan_absence_as_sharing` (`int` 0/1)
- **Descrizione**: «Imposta assenze programmate come Sharing»: un'assenza programmata rende il giorno rivendibile
  come uno sharing (front `isScheduledAbsenceSharing`). Sostituisce l'enum `resale_mode` (`absence`/`sharing`).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: guida `booking_resale_offers.origin`.

### manual_credit_enabled / manual_credit_units_per_day / manual_credit_amount
- **Legacy**: — / `configurations.credit_perday_resold` (`int`; outlier `30`/`320`) / `configurations.credit_value`
  (`varchar` sporco)
- **Descrizione**: «Assegna crediti manualmente»: per ogni giorno rivenduto il titolare matura
  `manual_credit_units_per_day` crediti del valore di `manual_credit_amount` € ciascuno, che il gestore usa a mano
  (front `isManualCreditAssignmentEnabled`, `resoldDayCreditQuantity`, `manualCreditValue`).
- **Tipo**: `boolean` / `integer` (`CHECK >= 0`) / `numeric(12,2)` (`CHECK >= 0`), importo in euro.
- **Default**: `false` / `0` / `0`.
- **Nullabile**: No.
- **Relazioni**: alimenta `booking_resale_credit_ledger`.

### voucher_credit_enabled / voucher_credit_amount
- **Legacy**: — (nuovo)
- **Descrizione**: «Crea voucher automatico»: per il giorno rivenduto si invia al cliente per email un voucher del
  valore indicato (front `isAutomaticVoucherEnabled`, `voucherCreditValue`).
- **Tipo**: `boolean` / `numeric(12,2)` (`CHECK >= 0`), euro.
- **Default**: `false` / `0`.
- **Nullabile**: No.
- **Relazioni**: crea un voucher in Catalog (contratto da definire, T-04).

### card_top_up_enabled / card_top_up_amount
- **Legacy**: — (nuovo)
- **Descrizione**: «Ricarica tessera prepagata»: per il giorno rivenduto si ricarica la tessera prepagata del
  cliente dell'importo indicato (front `isPrepaidCardTopUpEnabled`, `prepaidCardCreditValue`).
- **Tipo**: `boolean` / `numeric(12,2)` (`CHECK >= 0`), euro.
- **Default**: `false` / `0`.
- **Nullabile**: No.
- **Relazioni**: movimento sulla tessera prepagata (Payment, contratto da definire).
- **Note**: le tre modalità di credito sono **indipendenti** (checkbox nel front). La risposta BK-24 parlava di
  «voucher o ricarica tessera scelto dal gestore una volta»: il front permette di attivarle insieme. Allineato al
  front; se il committente le vuole esclusive basta un vincolo `CHECK`.

### created_at / updated_at / deleted_at
- **Legacy**: `configurations.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica / soft-delete.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON — 7 scalari sparsi in `configurations` (C2) → colonne tipizzate in una tabella C1 dedicata.

## Suggerimenti per le prestazioni

- 1 riga per stabilimento, letta all'avvio del flusso rivendita. Nessun indice oltre a PK e
  `UNIQUE(property_id)`.
- Candidata a entrare nel read-model di configurazione di C1 (se ne emergerà uno — [D-33 analogo](../_domande-committente.md)).

## Implementazione .NET

`ResaleSettings` è un **aggregate root** (lookup di configurazione, 1 riga per stabilimento) dell'area
`Resale` del modulo `Module.Booking`, schema `booking`. Implementa `ISoftDeletable`/`IAuditable`.

```csharp
// Domain/Enums/ResaleScope.cs — flag non esclusivi → enum array PG
public enum ResaleScope { SeasonalOnly = 0, AllArticles = 1 }
```

Spariscono gli enum `ResaleMode` e `ResaleCreditCalc` (e i tipi PG `booking.resale_mode`,
`booking.resale_credit_calc`); il valore PG `seasonal_workers` di `booking.resale_scope` diventa `seasonal_only`.

Configurazione EF (stesse regole delle altre: `HasColumnType` sugli enum, `HasDefaultValueSql` per gli enum):

| Proprietà | Colonna | Tipo / default | Commento |
|---|---|---|---|
| `IsEnabled` | `is_enabled` | `boolean`, `false` | La rivendita/sharing è attiva per lo stabilimento |
| `MinBookingDays` | `min_booking_days` | `integer` NULL | Giorni minimi della prenotazione per lo sharing; NULL = nessun minimo |
| `AppliesTo` | `applies_to` | `booking.resale_scope[]`, `'{}'` | Solo stagionali, tutti gli articoli |
| `AbsencesAsSharing` | `absences_as_sharing` | `boolean`, `false` | Le assenze programmate diventano sharing |
| `ManualCreditEnabled` | `manual_credit_enabled` | `boolean`, `false` | Crediti assegnati manualmente |
| `ManualCreditUnitsPerDay` | `manual_credit_units_per_day` | `integer`, `0` | Crediti per giorno rivenduto |
| `ManualCreditAmount` | `manual_credit_amount` | `numeric(12,2)`, `0` | Valore in euro di un credito |
| `VoucherCreditEnabled` | `voucher_credit_enabled` | `boolean`, `false` | Voucher automatico via email |
| `VoucherCreditAmount` | `voucher_credit_amount` | `numeric(12,2)`, `0` | Valore in euro del voucher |
| `CardTopUpEnabled` | `card_top_up_enabled` | `boolean`, `false` | Ricarica della tessera prepagata |
| `CardTopUpAmount` | `card_top_up_amount` | `numeric(12,2)`, `0` | Importo in euro della ricarica |

Vincoli `CHECK`: `min_booking_days > 0`, importi e crediti `>= 0`. **Stato**: scheda allineata il 2026-11-05;
entità, configurazione e migration da aggiornare con le altre modifiche allo schema di Booking (prima fase delle API).

**Note di migrazione**:

- Estrazione dei 7 campi "sharing" da `configurations` (C2) per `propertyId`, spostati in questa tabella
  dedicata C1: `sharing_enable` → `is_enabled`, `plan_absence_as_sharing` → `absences_as_sharing`,
  `sharing_seasonal_workers`/`enable_sharing_on_all_article` → `applies_to[]`, `disable_both_options = 1` →
  `is_enabled = false`, `credit_perday_resold` → `manual_credit_units_per_day` (outlier `30`/`320` bonificati);
  `manual_credit_enabled = true` dove c'è un credito valorizzato. Voucher e ricarica tessera partono spenti.
- `credit_value` (testo libero sporco: `'5€'`, `'5 €'`, `'5 Euro'`, `'5%'`, `'30% del giornaliero'`, `''`,
  `'0'`) → parsing in migrazione con tabella di frequenza dei valori reali distinti e mappatura esplicita
  (§2.6 linee guida dati) verso `manual_credit_amount numeric(12,2)` in euro; i valori in **percentuale**
  (`'5%'`, `'30% del giornaliero'`) non hanno più un equivalente → `0` e riga segnalata nel registro scarti per
  revisione manuale, come i valori vuoti o non parsabili.
- Righe di `configurations` senza `propertyId` valorizzato → scartate (nessuno stabilimento a cui
  associare la configurazione).
- Vincolo `UNIQUE(property_id)` mancava nel legacy (i 7 campi erano semplici colonne di una riga di
  configurazione più ampia): qui garantito dallo schema, un solo record per property.
