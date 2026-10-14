# Tabella `booking_lines`

> **Dominio**: C1 — `booking` · **sottoarea**: — (nucleo) · **schema**: `booking` · **nome flat**:
> `booking_lines` · **forma canonica**: `booking.lines`.
>
> **Righe** dell'aggregato Prenotazione: sostituisce **la singola riga** della legacy
> [`bookings`](../../../analisi-pre/db/bookings.md) (1 riga = 1 postazione × intervallo contiguo ×
> fascia oraria) e i JSON `booking_locations` / `packageSelectedData` / `booking_payments.locationData`.
> Solo per `booking_reservations.reservation_kind = spot`. Fonti:
> [`analisi-pre/db/bookings.md`](../../../analisi-pre/db/bookings.md),
> [`analisi-pre/db/booking_payments.md`](../../../analisi-pre/db/booking_payments.md),
> [`analisi-pre/business-logic/stabilimento/_motore-di-prezzo-e-sconti.md`](../../../analisi-pre/business-logic/stabilimento/_motore-di-prezzo-e-sconti.md),
> [`docs/03-gestione-dati.md` §3.6, §3.7, §3.8](../../../progettazione/03-gestione-dati.md),
> [`analisi-pre/db/_feature-tabelle.md` §"Struttura target C1"](../../../analisi-pre/db/_feature-tabelle.md).

---

## Descrizione

**Una riga per (postazione × intervallo di date contiguo × `booking_period`)** — cioè esattamente la
granularità della vecchia riga `bookings`, ma senza `bookingNumber`/consumer/agente/totali ripetuti
(quelli sono in [`booking_reservations`](booking_reservations.md)). Se la stessa postazione ha due
tratte con periodo diverso (es. 20–21 FullDay + 22–24 Morning) sono **due righe**.

La riga porta:

- il **riferimento vivo** alla risorsa (`resource_id` → `catalog_map_spots`) e alla voce di listino
  (`price_list_id` → `catalog_pricing_price_lists`);
- lo **snapshot** dei valori che il documento-prenotazione congela alla conferma
  ([`docs/03-gestione-dati.md` §3.6 punto 4](../../../progettazione/03-gestione-dati.md)): `line_price`,
  `package_name_snapshot`, `sector_label_snapshot`, `booking_period`, `spot_label_snapshot`;
- lo **stato per postazione**: `checked_in_at` / `checked_out_at` (il check-in nel legacy è sulla
  prima riga per `booking_start_date`; qui è per riga), `checkout_effective_at`.

**Non** contiene: la griglia prezzi giorno-per-giorno (`edit_price_data`, ~90 elementi per pacchetto nel
legacy — decine di KB per riga) → **ricalcolata** dal motore prezzo server-side; gli articoli aggiuntivi
→ [`booking_line_additional_items`](booking_line_additional_items.md); il consumo inventario per giorno →
[`booking_resource_consumptions`](booking_resource_consumptions.md).

Correzioni rispetto al legacy:

- **`booking_locations[].Location` (stringa, match fragile)** → `resource_id uuid` FK a
  [`catalog_map_spots`](../catalog/catalog_map_spots.md) + `spot_label_snapshot` (etichetta mostrata,
  congelata).
- **`packageSelectedData[].selectedPackageDetails[].packageId`** → `price_list_id uuid` FK a
  [`catalog_pricing_price_lists`](../catalog/catalog_pricing_price_lists.md).
- **`currentRow` / `sectorRowsPkg` (nome settore, stringa)** → `sector_id uuid` (opz.) +
  `sector_label_snapshot`.
- **`PackageItemPrice` / `TotalSumItem` (numero o stringa nel JSON)** → `line_price numeric(12,2)`.
- **`booking_period` `'FullDay'`/`'Morning'`/`'Afternoon'` (varchar per riga in `booking_locations`)** →
  `enum booking.day_period`.
- **`checkIn`/`checkOut`/`checkOut_date_time` (sulla testata `bookings`, sulla "prima riga")** →
  `checked_in_at`/`checked_out_at`/`checkout_effective_at` (`timestamptz`) **per riga**.
- **`isSplit` / `areaItemsFlag`** → `is_split` / `is_area` sulla riga.
- **`lineItemId` (uuid sparso in `packageSelectedData`)** → **non migrato, e la colonna che lo
  ospitava è stata rimossa** il 2026-10-01 (M5 di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`). Era una pezza del legacy:
  le voci di una prenotazione non erano righe ma elementi di array JSON, che non hanno chiave
  primaria, e il backend se ne inventava una con `uuidv4()` per usarla nel payload dello
  scontrino. Nel nuovo schema quelle voci **sono righe vere**, con `id` e `public_id` propri.
  Il collegamento che la colonna prometteva non è mai esistito nemmeno nel legacy: in
  `fiskaly_receipt.entries` gli elementi hanno sei chiavi (`type`, `description`, `category`,
  `quantity`, `unit_price`, `vat`) e nessun `lineItemId` — verificato, zero occorrenze su 2.609
  righe. Il riferimento corretto esiste già nel verso opposto:
  `fiskaly.receipt_lines.source_line_public_id`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_lines` | `id` | PK | — |
| `uq_booking_lines_public_id` | `(public_id)` | UNIQUE | referenziato cross-modulo (C3 righe, C4 line item) |
| `uq_booking_lines_number` | `(booking_line_number) WHERE deleted_at IS NULL` | UNIQUE parziale | numero riga subordinato (per etichette e correlazione) |
| `ix_..._reservation` | `(reservation_id) WHERE deleted_at IS NULL` | parziale | righe di una prenotazione |
| `ix_..._property_resource_dates` | `(property_id, resource_id, start_date, end_date) WHERE deleted_at IS NULL` | parziale | "chi occupa questa postazione in queste date" (agenda/mappa) |
| `ix_..._property_stay` | `(property_id, start_date) WHERE deleted_at IS NULL` | parziale | scansione per data della griglia planner ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): a DB mancava perché la causa era EF: due `HasIndex` sulle stesse colonne senza nome sono lo stesso indice, e il secondo sovrascrive il primo. |
| `ix_..._price_list` | `(price_list_id) WHERE deleted_at IS NULL` | parziale | reportistica per pacchetto |
| `ix_..._checkin_open` | `(property_id, start_date) WHERE checked_in_at IS NOT NULL AND checked_out_at IS NULL AND deleted_at IS NULL` | parziale | presenze in corso |
| `ix_..._booking_number` | `(booking_number)` | btree | correlazione diretta con C3/C4 senza join alla testata |

## Campi

### id
- **Legacy**: `bookings.id` (di una singola riga)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da `booking_line_additional_items`, `booking_resource_consumptions`,
  `booking_agreement_lines`, `booking_resale_offers` (via `booking_line_id`).

### public_id
- **Legacy**: `packageSelectedData[].PackageItem[].lineItemId` (uuid, ma non stabile/riusato)
- **Descrizione**: id pubblico stabile della riga, **copiato cross-modulo** (C3 righe economiche, C4
  line item scontrino).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: UUIDv7.
- **Nullabile**: No.
- **Relazioni**: copiato da C3/C4.

### reservation_id
- **Legacy**: righe con lo stesso `bookings.bookingNumber`
- **Descrizione**: prenotazione (testata) a cui appartiene la riga.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`booking_reservations.id`](booking_reservations.md) `ON DELETE CASCADE`
  (interna allo schema `booking`).

### booking_line_number
- **Legacy**: — (nuovo; nel legacy la riga non aveva un numero proprio)
- **Descrizione**: numero riga leggibile, **subordinato** al `booking_number` (es.
  `10795-0000012345-01`), per etichette e per correlazioni fini con C3/C4.
- **Tipo**: `varchar(48)`.
- **Default**: generato server-side.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE` parziale.

### booking_number
- **Legacy**: `bookings.bookingNumber`
- **Descrizione**: numero prenotazione **copiato** dalla testata per query dirette (coerente con la
  scelta "chiave = `booking_number`" verso C3/C4, [`docs/03-gestione-dati.md` §3.8](../../../progettazione/03-gestione-dati.md)).
- **Tipo**: `varchar(40)`.
- **Default**: dalla testata.
- **Nullabile**: No.
- **Relazioni**: denormalizzato; coerenza garantita in applicazione (il numero è immutabile).

### property_id
- **Legacy**: `bookings.property_id`
- **Descrizione**: stabilimento — copiato dalla testata per indici locali senza join.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: dalla testata.
- **Nullabile**: No.
- **Relazioni**: **`public_id` copiato** da C2 (denormalizzato dalla testata).

### resource_id
- **Legacy**: `bookings.booking_locations[].Location` (stringa: numero o codice `"A12"`, match per uguaglianza)
- **Descrizione**: la **postazione** prenotata (ombrellone/lettino/cabina/…).
- **Tipo**: `uuid` (= `public_id` di `catalog_map_spots`).
- **Default**: nessuno.
- **Nullabile**: Sì (prenotazione "ad area" senza postazione puntuale — vedi `is_area`).
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_map_spots`](../catalog/catalog_map_spots.md) — riferimento vivo all'anagrafica risorse.
- **Note**: il legacy confrontava per stringa `Location` con nomi non normalizzati (numeri vs codici) —
  causa di conteggi errati; qui è una FK per valore stabile.

### spot_label_snapshot
- **Legacy**: `bookings.booking_locations[].Location` (il valore mostrato)
- **Descrizione**: etichetta della postazione **congelata** al momento della prenotazione (es. `A12`),
  per ricevute/agenda anche se la postazione viene poi rinominata.
- **Tipo**: `varchar(40)`.
- **Default**: nessuno.
- **Nullabile**: Sì (per le prenotazioni ad area).
- **Relazioni**: snapshot ([`docs/03-gestione-dati.md` §3.6](../../../progettazione/03-gestione-dati.md)); la FK
  `resource_id` resta accanto per risalire.

### sector_id
- **Legacy**: `selectedPackageDetails[].currentRow` / `sectorRowsPkg` (nome settore, stringa)
- **Descrizione**: settore contro cui è stata calcolata la tariffa della riga.
- **Tipo**: `uuid` (= `public_id` di `catalog_map_sectors`).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_map_sectors`](../catalog/catalog_map_sectors.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 310.802 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### sector_label_snapshot
- **Legacy**: `currentRow` (es. `"Dalla 4ª Fila in poi"`, `"Fila unica"`)
- **Descrizione**: etichetta del settore/fila **congelata** (contesto tariffario del documento). Suffisso
  `_snapshot` come le altre etichette copiate da un'entità mutabile con FK viva accanto (`spot_label_snapshot`,
  `package_name_snapshot`).
- **Tipo**: `varchar(100)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: snapshot.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 310.802 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### price_list_id
- **Legacy**: `selectedPackageDetails[].packageId` (→ `price_lists.id`)
- **Descrizione**: voce di listino (**pacchetto**) applicata alla riga.
- **Tipo**: `uuid` (= `public_id` di `catalog_pricing_price_lists`).
- **Default**: nessuno.
- **Nullabile**: Sì (riga senza pacchetto — solo articoli singoli).
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_pricing_price_lists`](../catalog/catalog_pricing_price_lists.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 310.802 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### package_name_snapshot
- **Legacy**: `packageSelectedData[].PackageName` / `selectedPackageDetails[].packageName`
- **Descrizione**: nome del pacchetto **congelato** (il gestore può rinominarlo dopo). Corrisponde a
  `booking_items_counts_details.packageName` legacy (in produzione **popolato** al 99%).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: snapshot.

### start_date / end_date
- **Legacy**: `bookings.booking_start_date` / `booking_end_date` (`datetime`, normalizzati a inizio/fine giornata)
- **Descrizione**: intervallo di date **contiguo** coperto dalla riga (estremi inclusi).
- **Tipo**: `date` ciascuno.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `end_date >= start_date` (`CHECK`); una prenotazione multi-periodo produce più righe.
- **Note**: `date`, non `timestamptz` — l'ora non ha significato
  ([`docs/03-gestione-dati.md` §3.3](../../../progettazione/03-gestione-dati.md)); il legacy salvava
  `00:00:00` / `23:59:59` con compensazioni di fuso sparse.

### booking_period
- **Legacy**: `bookings.booking_period` / `booking_locations[].booking_period` (`varchar`, default `'FullDay'`)
- **Descrizione**: fascia oraria della riga.
- **Tipo**: `enum booking.day_period` — valori: `full_day`, `morning`, `afternoon`.
- **Default**: `full_day`.
- **Nullabile**: No.
- **Relazioni**: vocabolario condiviso con `booking_resource_consumptions`, `booking_resale_offers`
  (ex `book_period_allowed`) — [`docs/03-gestione-dati.md` §2.4bis](../../../progettazione/03-gestione-dati.md).

### quantity
- **Legacy**: — (implicito `1` per postazione; `booking_locations[]` = 1 elemento per riga)
- **Descrizione**: numero di postazioni coperte dalla riga. `1` per la postazione puntuale; `> 1` solo
  per la riga "ad area" (`is_area = true`).
- **Tipo**: `smallint`.
- **Default**: `1`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (quantity >= 1)`.

### line_price
- **Legacy**: `packageSelectedData[].PackageItemPrice` / `TotalSumItem` (numero o stringa)
- **Descrizione**: **prezzo bloccato** della riga (pacchetto, per l'intero intervallo/periodo della
  riga), al netto degli articoli aggiuntivi (che hanno prezzo proprio in
  [`booking_line_additional_items`](booking_line_additional_items.md)). Parte legittima dell'aggregato
  ([`docs/03-gestione-dati.md` §3.6 punto 4](../../../progettazione/03-gestione-dati.md)).
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: somma delle `line_price` + additional item + servizi = `booking_reservations.subtotal`.

### is_split
- **Legacy**: `bookings.isSplit`
- **Descrizione**: la riga fa parte di una prenotazione "split location".
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### is_area
- **Legacy**: `bookings.areaDetails != null` / `booking_locations[].isAreaBooked`
- **Descrizione**: prenotazione **ad area a capienza** (es. "Prato"): occupa `quantity` posti su un
  totale, senza postazione puntuale (`resource_id` = `NULL`).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: se `true` la disponibilità è per `resource_type = area_seat` in
  [`booking_resource_consumptions`](booking_resource_consumptions.md); `area_resource_id` (sotto) indica
  quale area.

### area_resource_id
- **Legacy**: `bookings.areaDetails[].areaName` (stringa)
- **Descrizione**: l'**area** occupata (quando `is_area = true`).
- **Tipo**: `uuid` (= `public_id` di `catalog_map_spots` con `spot_type` d'area, es. `umbrella_area`).
- **Default**: nessuno.
- **Nullabile**: Sì (valorizzato solo se `is_area`).
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_map_spots`](../catalog/catalog_map_spots.md).

### area_seat_count
- **Legacy**: `bookings.areaDetails[].selectedAreaCount` (numero o stringa)
- **Descrizione**: posti occupati nell'area da questa riga.
- **Tipo**: `smallint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (solo se `is_area`).
- **Relazioni**: la capienza totale dell'area è in C2 (non duplicata qui, il legacy la copiava in
  `totalAreaCount`).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 310.802 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### checked_in_at
- **Legacy**: `bookings.checkIn` (`tinyint`, sulla riga con `booking_start_date` minima)
- **Descrizione**: quando il cliente si è presentato per **questa** postazione (`NULL` = non arrivato).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### checked_out_at
- **Legacy**: `bookings.checkOut` (`tinyint`)
- **Descrizione**: quando la postazione è stata liberata / cliente uscito (anche anticipato).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### checkout_effective_at
- **Legacy**: `bookings.checkOut_date_time` (`datetime`, inizialmente = `booking_end_date`)
- **Descrizione**: fine occupazione effettiva quando c'è un check-out anticipato (usata come limite
  superiore per la disponibilità al posto di `end_date`).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### created_at / updated_at
- **Legacy**: `bookings.createdAt` / `updatedAt`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` (+ trigger).
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: `bookings.isDeleted` / `status = 0`
- **Descrizione**: soft-delete della singola riga (es. spostamento che rimuove una postazione dalla
  prenotazione).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: Global Query Filter EF.

## Valutazione JSON

Nessun JSON. `booking_locations[]` e `packageSelectedData[]` esplodono in: questa tabella (1 riga per
postazione/tratta/periodo) + [`booking_line_additional_items`](booking_line_additional_items.md) +
[`booking_resource_consumptions`](booking_resource_consumptions.md). La griglia
`edit_price_data[]` (prezzi per numero di giorni 1–90) e i vari `daily_price`/`weekend_price`/… **non**
si persistono qui: sono input del **motore prezzo** e vivono in C2
([`catalog_pricing_price_list_*`](../catalog/catalog_pricing_price_lists.md)); qui resta solo il
risultato `line_price`. `packageItemString` (`"1U+2S"`) si ricalcola in presentazione dalle righe.

## Suggerimenti per le prestazioni

- L'indice `(property_id, resource_id, start_date, end_date)` parziale è il cuore del controllo
  "postazione occupata in queste date" della mappa/planner — sostituisce la `UNION ALL` +
  `DATE()` costruita per concatenazione di stringhe del legacy `checkBookingDetails`.
- La **disponibilità** effettiva (con concorrenza) si legge/valida su
  [`booking_resource_consumptions`](booking_resource_consumptions.md) con exclusion constraint GiST
  (§3.9), non scansionando questa tabella.
- `booking_number` copiato in colonna evita il join alla testata per le correlazioni C3/C4 ad alta
  frequenza (webhook, emissione scontrino).
- Su prenotazioni multi-giorno il legacy registrava il consumo inventario **su un solo giorno**
  (`booking_start_date + 1`) — bug che sovrastimava la disponibilità; qui il consumo è per-giorno in
  `booking_resource_consumptions`, questa tabella tiene solo l'intervallo.

## Implementazione .NET

`BookingLine` è **entità figlia** dell'aggregato `Reservation` (modulo `Module.Booking`, schema
`booking`): 1 riga per postazione × intervallo contiguo × fascia oraria, sempre caricata/modificata
attraverso l'aggregate root (`Reservation.AddLine(...)`, `Reservation.MoveLine(...)`), mai persistita
in isolamento. Espone la collezione figlia `AdditionalItems`
([`booking_line_additional_items`](booking_line_additional_items.md)).

### Enum propri

```csharp
// Module.Booking.Domain.Enums

/// Fascia oraria della riga — vocabolario condiviso anche da booking_resource_consumptions e
/// booking_resale_offers (fuori da questo batch).
public enum DayPeriod
{
	FullDay = 0,
	Morning = 1,
	Afternoon = 2
}
```

```csharp
// Module.Booking.Infrastructure.DbContext.OnModelCreating
modelBuilder.HasPostgresEnum<DayPeriod>("booking", "day_period");
```

```csharp
// Module.Booking Infrastructure DI — registrazione data source Npgsql
dataSourceBuilder.MapEnum<DayPeriod>("booking.day_period");
```

`BookingChannel` non compare qui; vedi comunque [`booking_reservations.md`](booking_reservations.md)
per il pattern generale di registrazione enum condivisi nello schema `booking`.

### `BookingLineConfiguration`

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingLineConfiguration : IEntityTypeConfiguration<BookingLine>
{
	public void Configure(EntityTypeBuilder<BookingLine> builder)
	{
		builder.ToTable("booking_lines", "booking", t =>
		{
			t.HasComment("Riga dell'aggregato Prenotazione: una postazione per un intervallo di date contiguo e una fascia oraria.");
			t.HasCheckConstraint("ck_booking_lines_dates", "end_date >= start_date");
			t.HasCheckConstraint("ck_booking_lines_quantity", "quantity >= 1");
		});

		builder.HasKey(x => x.Id).HasName("pk_booking_lines");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile della riga, copiato cross-modulo da C3 (righe economiche) e C4 (line item scontrino).");

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione (testata) a cui appartiene la riga.");

		builder.Property(x => x.BookingLineNumber)
			.HasColumnName("booking_line_number")
			.HasMaxLength(48)
			.IsRequired()
			.HasComment("Numero riga leggibile, subordinato al booking_number della testata (es. 10795-0000012345-01).");

		builder.Property(x => x.BookingNumber)
			.HasColumnName("booking_number")
			.HasMaxLength(40)
			.IsRequired()
			.HasComment("Numero prenotazione copiato dalla testata, per correlazioni dirette con C3/C4 senza join.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema.");

		builder.Property(x => x.ResourceId)
			.HasColumnName("resource_id")
			.HasComment("public_id di catalog.map_spots, copiato — la postazione prenotata; NULL per le righe ad area.");

		builder.Property(x => x.SpotLabelSnapshot)
			.HasColumnName("spot_label_snapshot")
			.HasMaxLength(40)
			.HasComment("Etichetta della postazione congelata al momento della prenotazione.");

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.HasComment("public_id di catalog.map_sectors, copiato — settore contro cui è stata calcolata la tariffa.");

		builder.Property(x => x.SectorLabelSnapshot)
			.HasColumnName("sector_label_snapshot")
			.HasMaxLength(100)
			.HasComment("Etichetta del settore/fila congelata al momento della prenotazione.");

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.HasComment("public_id di catalog.pricing_price_lists, copiato — voce di listino (pacchetto) applicata alla riga.");

		builder.Property(x => x.PackageNameSnapshot)
			.HasColumnName("package_name_snapshot")
			.HasMaxLength(255)
			.HasComment("Nome del pacchetto congelato al momento della prenotazione.");

		builder.Property(x => x.StartDate)
			.HasColumnName("start_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Inizio dell'intervallo contiguo coperto dalla riga (incluso).");

		builder.Property(x => x.EndDate)
			.HasColumnName("end_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Fine dell'intervallo contiguo coperto dalla riga (incluso).");

		builder.Property(x => x.BookingPeriod)
			.HasColumnName("booking_period")
			.HasColumnType("booking.day_period")
			.IsRequired()
			.HasDefaultValueSql("'full_day'")
			.HasComment("Fascia oraria della riga.");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasColumnType("smallint")
			.IsRequired()
			.HasDefaultValue((short)1)
			.HasComment("Numero di postazioni coperte dalla riga; > 1 solo per riga ad area.");

		builder.Property(x => x.LinePrice)
			.HasColumnName("line_price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Prezzo bloccato della riga (pacchetto), al netto degli articoli aggiuntivi.");

		builder.Property(x => x.IsSplit)
			.HasColumnName("is_split")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("La riga fa parte di una prenotazione split-location.");

		builder.Property(x => x.IsArea)
			.HasColumnName("is_area")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Prenotazione ad area a capienza (senza postazione puntuale); se true resource_id è NULL.");

		builder.Property(x => x.AreaResourceId)
			.HasColumnName("area_resource_id")
			.HasComment("public_id di catalog.map_spots (spot_type d'area), copiato — l'area occupata quando is_area = true.");

		builder.Property(x => x.AreaSeatCount)
			.HasColumnName("area_seat_count")
			.HasColumnType("smallint")
			.HasComment("Posti occupati nell'area da questa riga (solo se is_area).");

		builder.Property(x => x.CheckedInAt)
			.HasColumnName("checked_in_at")
			.HasComment("Quando il cliente si è presentato per questa postazione; NULL = non arrivato.");

		builder.Property(x => x.CheckedOutAt)
			.HasColumnName("checked_out_at")
			.HasComment("Quando la postazione è stata liberata / cliente uscito.");

		builder.Property(x => x.CheckoutEffectiveAt)
			.HasColumnName("checkout_effective_at")
			.HasComment("Fine occupazione effettiva in caso di check-out anticipato; limite superiore per la disponibilità al posto di end_date.");

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
			.HasComment("Soft-delete della singola riga (es. spostamento che rimuove una postazione dalla prenotazione).");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_booking_lines_public_id");

		builder.HasIndex(x => x.BookingLineNumber)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_booking_lines_number");

		builder.HasIndex(x => x.ReservationId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_lines_reservation");

		builder.HasIndex(x => new { x.PropertyId, x.ResourceId, x.StartDate, x.EndDate })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_lines_property_resource_dates");

		builder.HasIndex(x => new { x.PropertyId, x.StartDate })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_lines_property_stay");

		builder.HasIndex(x => x.PriceListId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_lines_price_list");

		builder.HasIndex(x => new { x.PropertyId, x.StartDate })
			.HasFilter("checked_in_at IS NOT NULL AND checked_out_at IS NULL AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_lines_checkin_open");

		builder.HasIndex(x => x.BookingNumber)
			.HasDatabaseName("ix_booking_lines_booking_number");

		builder.HasOne(x => x.Reservation)
			.WithMany(r => r.Lines)
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_lines_reservation")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

`BookingLine` implementa `ISoftDeletable` (`deleted_at`) e `IAuditable` (`created_at`/`updated_at`).
`resource_id`, `sector_id`, `price_list_id`, `area_resource_id` sono `public_id` copiati da `catalog`
(C2, nessuna FK cross-schema, solo `HasComment`). L'unica FK EF reale è `reservation_id` → `booking_reservations` (stesso schema).

### Note di migrazione

- **1 riga `bookings` → 1 `booking_line`**: la testata condivisa (`bookingNumber`, consumer, agente,
  totali) migra invece su [`booking_reservations`](booking_reservations.md) (mappa
  `legacy_bookings.id → (reservation_id, booking_line_id)`).
- `booking_locations[0].Location` (stringa, match fragile per numero/codice) → `resource_id` risolto
  contro `catalog_map_spots` per `(property_id, label)`, con `spot_label_snapshot` congelato al valore
  legacy così com'era al momento della riga.
- `edit_price_data[]` (griglia prezzi giorno-per-giorno, decine di KB per riga) **scartato**: non ha
  equivalente in questa tabella, si ricalcola dal motore prezzo a partire da `catalog_pricing_price_lists`.
- **`lineItemId` (uuid sparso in `packageSelectedData`)** → **non migrato, e la colonna che lo
  ospitava è stata rimossa** il 2026-10-01 (M5 di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`). Era una pezza del legacy:
  le voci di una prenotazione non erano righe ma elementi di array JSON, che non hanno chiave
  primaria, e il backend se ne inventava una con `uuidv4()` per usarla nel payload dello
  scontrino. Nel nuovo schema quelle voci **sono righe vere**, con `id` e `public_id` propri.
  Il collegamento che la colonna prometteva non è mai esistito nemmeno nel legacy: in
  `fiskaly_receipt.entries` gli elementi hanno sei chiavi (`type`, `description`, `category`,
  `quantity`, `unit_price`, `vat`) e nessun `lineItemId` — verificato, zero occorrenze su 2.609
  righe. Il riferimento corretto esiste già nel verso opposto:
  `fiskaly.receipt_lines.source_line_public_id`.
- `checkIn`/`checkOut`/`checkOut_date_time` (sulla "prima riga" della testata legacy) → per-riga su
  `checked_in_at`/`checked_out_at`/`checkout_effective_at`: richiede propagare lo stesso stato di
  check-in a tutte le righe generate dalla stessa riga legacy, quando serve preservare il comportamento
  "un check-in per prenotazione" osservato in produzione.
