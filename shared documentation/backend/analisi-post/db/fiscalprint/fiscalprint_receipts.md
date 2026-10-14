# Tabella `fiscalprint_receipts`

> **Dominio**: C4bis — `fiscalprint` · **sottoarea**: — · **schema**: `fiscalprint` · **nome flat**:
> `fiscalprint_receipts` · **forma canonica**: `fiscalprint.receipts`.
>
> Sostituisce [`booking_receipts`](../../../analisi-pre/db/booking_receipts.md) (4.476 righe; `bookingId`
> **`NULL` su tutte**, `receipt_number` **`"001"` su tutte**, `printer_request_object` /
> `printer_response_object` **`NULL` su tutte**; uso concentrato su 3-4 stabilimenti). Fonti:
> [`analisi-pre/db/booking_receipts.md`](../../../analisi-pre/db/booking_receipts.md),
> [`docs/03-gestione-dati.md` §2.4ter, §3.5, §3.6 punto 5, §3.7, §4](../../../progettazione/03-gestione-dati.md),
> [`analisi-pre/business-logic/stabilimento/03-cassa.md` §6](../../../analisi-pre/business-logic/stabilimento/03-cassa.md).

---

## Descrizione

**Una riga = uno scontrino/ricevuta stampato su registratore di cassa RT fisico** (registratore
telematico) — **non** lo scontrino elettronico Fiskaly (C4). Conserva l'esito restituito dal
dispositivo. Nel legacy `bookingId` è sempre `NULL` (nessuna stampa è legata a una prenotazione): la
tabella è di fatto un **log di stampe di cassa**; nel target il legame alla prenotazione resta
**opzionale** (`reservation_public_id` nullable) per il caso "stampo lo scontrino di *questa*
prenotazione".

**Target pulito ridisegnato** (decisione **C4bis-1**), non migrazione 1:1.

Correzioni rispetto al legacy:

- **`receipt_number` / `document_no` sempre `"001"`** (il progressivo non incrementa mai — bug
  confermato dai dati) → `receipt_number int` alimentato dalla sequenza **ridisegnata**
  [`fiscalprint_serial_numbers`](fiscalprint_serial_numbers.md) `(printer_id, fiscal_date) →
  last_number` con incremento atomico (serie **per stampante RT** = per dispositivo fiscale).
- **Date come stringhe** (`receipt_print_date` `"31/5/2023"`, `receiptISODateTime` `"20260727T101700"`)
  → `printed_at timestamptz` ([`docs/03 §3.7`](../../../progettazione/03-gestione-dati.md)).
- **Importi con virgola decimale in stringa** (`"30,00"`) dentro `receipt_object` → colonne di
  riepilogo `numeric` derivate all'acquisizione.
- **`printer_request_object` / `printer_response_object` `NULL` su tutte le righe** → **non migrati**.
- **`receipt_object` `longtext` JSON** (risposta del registratore telematico) → `jsonb` **passthrough**
  del dispositivo (schema di terzi, [`docs/03 §3.6 punto 5`](../../../progettazione/03-gestione-dati.md)) +
  poche colonne derivate read-only.
- **`bookingId int` senza FK, sempre `NULL`** → `reservation_public_id uuid` nullable (C1, per valore).
- **`sequelize-slugify` importato ma non usato**, associazioni ORM **tutte commentate** → FK interne
  reali dove servono.

## Verifica di esclusività

Nessun flag `is_*`. I campi `rt_*` sono valori derivati dal dispositivo, non flag.

## Snapshot

`device_response` è il **payload grezzo del registratore telematico** (schema di terzi, §3.6 punto 5)
→ `jsonb`, **niente** suffisso `_snapshot`. Le colonne `rt_*` sono **proiezioni derivate read-only**
(non autorevoli) da quel payload, congelate all'acquisizione — la loro natura è nella descrizione, non
nel nome (§3.6, come per C1-7).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_fiscalprint_receipts` | `id` | PK | — |
| `uq_fiscalprint_receipts_public_id` | `(public_id)` | UNIQUE | endpoint dettaglio (`getReceiptDetails`) / URL gestionale |
| `ix_fiscalprint_receipts_property_date` | `(property_id, printed_at DESC)` | btree | elenco stampe di uno stabilimento |
| `ix_fiscalprint_receipts_reservation` | `(reservation_public_id) WHERE reservation_public_id IS NOT NULL` | btree parziale | scontrini RT di una prenotazione (quando collegati) |
| `ix_fiscalprint_receipts_printer` | `(printer_id)` | btree | stampe per stampante (diagnostica) |

## Campi

### id
- **Legacy**: `booking_receipts.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### public_id
- **Legacy**: — (nel legacy si usava l'`id` interno)
- **Descrizione**: id pubblico stabile (UUIDv7) per l'URL del dettaglio gestionale. Nessun modulo lo
  copia per valore.
- **Tipo**: `uuid`.
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### property_id
- **Legacy**: `booking_receipts.propertyId` (`int`, `properties.id`, nessuna FK; 4 valori distinti)
- **Descrizione**: stabilimento che ha stampato.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2; [D-01](../_domande-committente.md).

### printer_id
- **Legacy**: `booking_receipts.printerId` (`int`, `printer_configs.id`, nessuna FK)
- **Descrizione**: stampante usata.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK interna** → [`fiscalprint_printer_configs.id`](fiscalprint_printer_configs.md).

### reservation_public_id
- **Legacy**: `booking_receipts.bookingId` (`int`, `bookings.id`, nessuna FK; **`NULL` su tutte le
  4.476 righe**)
- **Descrizione**: prenotazione a cui la stampa si riferisce, **quando** stampata dal dettaglio di
  una prenotazione (testata unica C1).
- **Tipo**: `uuid` (= `public_id` di `booking_reservations`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C1 [`booking_reservations`](../booking/booking_reservations.md).

### booking_number
- **Legacy**: — (dedotto dalla prenotazione, se collegata)
- **Descrizione**: numero prenotazione (business key denormalizzata, comoda per il gestionale).
- **Tipo**: `varchar(32)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `booking_reservations.booking_number` (per valore).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 4.487 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### receipt_number
- **Legacy**: `booking_receipts.receipt_number` / `document_no` (`varchar(255)`, sempre `"001"`)
- **Descrizione**: numero progressivo dello scontrino RT per `(property_id, fiscal_date)`, dalla
  sequenza [`fiscalprint_serial_numbers`](fiscalprint_serial_numbers.md).
- **Tipo**: `integer`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`NULL` se `printer_id` è ignoto o la sequenza non è stata assegnata).
- **Relazioni**: `(printer_id, printed_at::date)` → [`fiscalprint_serial_numbers`](fiscalprint_serial_numbers.md)
  (serie **per stampante** × giorno).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 4.487 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### rt_fiscal_receipt_number
- **Legacy**: `booking_receipts.receipt_object[].fiscalReceiptNumber` (`"5"`)
- **Descrizione**: numero scontrino restituito **dal registratore RT** (proiezione read-only del
  `device_response`).
- **Tipo**: `varchar(20)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: non autorevole (la verità è `device_response`).

### rt_fiscal_amount
- **Legacy**: `booking_receipts.receipt_object[].fiscalReceiptAmount` (`"30,00"` — virgola decimale)
- **Descrizione**: importo dello scontrino RT (proiezione derivata, virgola → punto → `numeric`).
- **Tipo**: `numeric(12,2)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### rt_z_report_number
- **Legacy**: `booking_receipts.receipt_object[].zRepNumber` (`"2"`)
- **Descrizione**: numero della chiusura Z in cui rientra lo scontrino.
- **Tipo**: `varchar(20)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### rt_serial_number
- **Legacy**: `booking_receipts.receipt_object[].serialNumber` (matricola stampante)
- **Descrizione**: matricola del registratore telematico che ha stampato.
- **Tipo**: `varchar(40)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### printed_at
- **Legacy**: `booking_receipts.receipt_print_date` (`varchar` `"D/M/YYYY"`) + `createdAt` (timestamp
  reale)
- **Descrizione**: data/ora della stampa. In migrazione: `createdAt` (affidabile) come valore,
  `receipt_print_date` / `receiptISODateTime` solo per verifica.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: decorrenza retention; determina il giorno fiscale per la sequenza.

### device_response
- **Legacy**: `booking_receipts.receipt_object` (`longtext` JSON — oggetto o array)
- **Descrizione**: **risposta grezza del registratore telematico** (`lastCommand`, `printerStatus`,
  `fiscalReceiptNumber`, `fiscalReceiptAmount`, `fiscalReceiptDate/Time`, `receiptISODateTime`,
  `zRepNumber`, `serialNumber`, …). Documento di un sistema esterno (§3.6 punto 5): `jsonb`
  **così com'è**, mai interrogato per campo.
- **Tipo**: `jsonb`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: fonte delle colonne `rt_*` (derivate all'acquisizione).

### created_by_user_id
- **Legacy**: — (il legacy non registrava l'operatore della stampa)
- **Descrizione**: operatore che ha lanciato la stampa.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 4.487 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at
- **Legacy**: `booking_receipts.createdAt` / `updatedAt` (`datetime` `NOT NULL`)
- **Descrizione**: creazione / modifica riga.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: automatici.

### deleted_at
- **Legacy**: — (nessun soft-delete)
- **Descrizione**: esposto per uniformità ([`docs/03 §3.5`](../../../progettazione/03-gestione-dati.md)) ma
  **non usato**: log di stampe / fatto fiscale (retention secondo obbligo di conservazione).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: Global Query Filter EF (sempre `NULL`).

## Valutazione JSON

`receipt_object` (risposta del dispositivo RT) → **resta `jsonb`** (`device_response`): passthrough di
un sistema esterno di cui non possediamo lo schema (§3.6 punto 5). Le colonne `rt_*` sono proiezioni
read-only per le liste. `printer_request_object` / `printer_response_object` (sempre `NULL`) **non
migrati**.

## Suggerimenti per le prestazioni

- ~4.5k righe storiche, crescita lenta (3-4 stabilimenti). Indici minimi.
- `device_response jsonb` piccolo (poche chiavi): nessun problema.
- Tabella piccola, nessun bisogno di investire oltre gli indici elencati.

## Implementazione .NET

Modulo `fiscalprint` (stampa fiscale su hardware RT, meccanismo legacy alternativo allo scontrino
elettronico Fiskaly, mantenuto come componente separato — decisione di business, vedi
`_feature-tabelle.md` C4bis). `FiscalPrintReceipt` è **aggregate root** (ha
`public_id`, referenziabile da fuori schema) del modulo `Fiscalprint`, schema `fiscalprint` — log di
stampe di cassa RT, distinto da `Fiskaly` (C4, scontrino elettronico). Nessun enum proprio. Implementa
`IAuditable`; espone anche `DeletedAt`/query filter per uniformità con le altre entità del modulo, pur
non essendo mai popolato nella pratica (fatto fiscale, retention da obbligo di conservazione, non da
cancellazione applicativa).

```csharp
namespace Module.Fiscalprint.Infrastructure.Configurations;

public sealed class FiscalPrintReceiptConfiguration : IEntityTypeConfiguration<FiscalPrintReceipt>
{
	public void Configure(EntityTypeBuilder<FiscalPrintReceipt> builder)
	{
		builder.ToTable("receipts", "fiscalprint", t => t.HasComment(
			"Scontrino/ricevuta stampato su registratore di cassa RT fisico, con la risposta grezza del dispositivo e le proiezioni derivate per le liste."));

		builder.HasKey(x => x.Id).HasName("pk_fiscalprint_receipts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile (UUIDv7) per l'URL del dettaglio gestionale (getReceiptDetails).");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_fiscalprint_receipts_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties (stabilimento che ha stampato), copiato — nessuna FK cross-schema.");

		builder.Property(x => x.PrinterId)
			.HasColumnName("printer_id")
			.HasComment("Stampante (fiscalprint.printer_configs) usata per la stampa.");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.reservations, valorizzato solo se lo scontrino è stampato dal dettaglio di una prenotazione — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.BookingNumber)
			.HasColumnName("booking_number")
			.HasMaxLength(32)
			.HasComment("Numero prenotazione (business key denormalizzata da booking.reservations, per comodità del gestionale).");

		builder.Property(x => x.ReceiptNumber)
			.HasColumnName("receipt_number")
			.HasComment("Numero progressivo dello scontrino RT per (printer_id, giorno fiscale), assegnato da fiscalprint.serial_numbers.");

		builder.Property(x => x.RtFiscalReceiptNumber)
			.HasColumnName("rt_fiscal_receipt_number")
			.HasMaxLength(20)
			.HasComment("Numero scontrino restituito dal registratore RT — proiezione read-only non autorevole di device_response.");

		builder.Property(x => x.RtFiscalAmount)
			.HasColumnName("rt_fiscal_amount")
			.HasColumnType("numeric(12,2)")
			.HasComment("Importo dello scontrino RT — proiezione derivata da device_response (virgola decimale del dispositivo convertita a numeric).");

		builder.Property(x => x.RtZReportNumber)
			.HasColumnName("rt_z_report_number")
			.HasMaxLength(20)
			.HasComment("Numero della chiusura Z in cui rientra lo scontrino — proiezione read-only di device_response.");

		builder.Property(x => x.RtSerialNumber)
			.HasColumnName("rt_serial_number")
			.HasMaxLength(40)
			.HasComment("Matricola del registratore telematico che ha stampato — proiezione read-only di device_response.");

		builder.Property(x => x.PrintedAt)
			.HasColumnName("printed_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora della stampa; determina il giorno fiscale usato dalla sequenza dei numeri progressivi.");

		builder.Property(x => x.DeviceResponse)
			.HasColumnName("device_response")
			.HasColumnType("jsonb")
			.HasComment("Risposta grezza del registratore telematico (lastCommand, printerStatus, fiscalReceiptNumber/Amount/Date/Time, zRepNumber, serialNumber, …) — passthrough di un sistema esterno, mai interrogata per campo.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, operatore che ha lanciato la stampa — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di ultima modifica della riga.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete esposto per uniformità con le altre entità del modulo; mai valorizzato in pratica — log di stampe / fatto fiscale, retention da obbligo di conservazione.");

		builder.HasIndex(x => new { x.PropertyId, x.PrintedAt })
			.HasDatabaseName("ix_fiscalprint_receipts_property_date");
		// PrintedAt in ordine decrescente nella query applicativa (elenco stampe più recenti prima);
		// l'indice btree copre entrambe le direzioni di scan.

		builder.HasIndex(x => x.ReservationPublicId)
			.HasFilter("reservation_public_id IS NOT NULL")
			.HasDatabaseName("ix_fiscalprint_receipts_reservation");

		builder.HasIndex(x => x.PrinterId)
			.HasDatabaseName("ix_fiscalprint_receipts_printer");

		builder.HasOne(x => x.Printer)
			.WithMany()
			.HasForeignKey(x => x.PrinterId)
			.HasConstraintName("fk_fiscalprint_receipts_printer")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

- All'acquisizione (Application): `device_response` popolato come `JsonDocument`/stringa jsonb grezza
  dalla risposta del dispositivo; le colonne `rt_*` estratte a quel momento (parse `"30,00"` →
  `30.00m` con `CultureInfo.InvariantCulture` e sostituzione virgola→punto); `receipt_number` ottenuto
  da `IFiscalPrintNumberSequence.NextAsync(printerId, printedAt)` (upsert atomico, vedi
  [`fiscalprint_serial_numbers`](fiscalprint_serial_numbers.md)) — mai calcolato lato applicazione con
  una `COUNT`/lettura-poi-scrittura (era la causa del bug `"001"` fisso nel legacy).
- **Note di migrazione**: `booking_receipts` → righe, con `public_id` generato ex-novo
  (`Guid.CreateVersion7()`); `receipt_object` → `device_response` (as-is) + estrazione delle `rt_*`;
  `receipt_number`/`document_no` (sempre `"001"` nel legacy, bug confermato) **non riportati as-is**:
  o ricalcolati a ritroso dalla sequenza ridisegnata (se l'ordine cronologico per stampante è
  ricostruibile) o lasciati `NULL` per lo storico; `bookingId` (sempre `NULL` su tutte le 4.476 righe)
  → `reservation_public_id NULL`; `printer_request_object`/`printer_response_object` (sempre `NULL`)
  **scartati**, nessuna colonna target.
