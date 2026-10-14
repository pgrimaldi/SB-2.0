# Tabella `fiscalprint_serial_numbers`

> **Dominio**: C4bis — `fiscalprint` · **sottoarea**: — · **schema**: `fiscalprint` · **nome flat**:
> `fiscalprint_serial_numbers` · **forma canonica**: `fiscalprint.serial_numbers`.
>
> Sostituisce [`receipt_serial_numbers`](../../../analisi-pre/db/receipt_serial_numbers.md) (4.476
> righe; `last_receipt_number` **`"001"` su tutte** — il contatore non incrementa mai). Fonti:
> [`analisi-pre/db/receipt_serial_numbers.md`](../../../analisi-pre/db/receipt_serial_numbers.md),
> [`docs/03-gestione-dati.md` §3.4, §3.5](../../../progettazione/03-gestione-dati.md).
> Concettualmente analoga a [`fiskaly_receipt_sequences`](../fiskaly/fiskaly_receipt_sequences.md) —
> stessa forma "sequenza per **dispositivo** × giorno" (lì la cassa Fiskaly, qui la stampante RT) — e
> a [`booking_number_sequences`](../booking/booking_number_sequences.md).

---

## Descrizione

**Contatore del numero progressivo dello scontrino stampato su registratore RT fisico, per
**stampante** e per giorno**: **1 riga per `(printer_id, fiscal_date)`**. `last_number` è l'ultimo
numero emesso su **quel dispositivo** quel giorno; il valore va su
[`fiscalprint_receipts.receipt_number`](fiscalprint_receipts.md).

**Chiave per stampante, non per stabilimento**: il progressivo scontrino su RT è **per dispositivo
fiscale** (matricola). Uno stabilimento con 2 RT ha **2 serie indipendenti** — ognuna rispecchia il
contatore interno del proprio dispositivo. Se la property ha una sola stampante RT la chiave degenera
in "per property × giorno".

**Riprogettata** (decisione **C4bis-1**): nel legacy **non incrementa mai** per due bug concorrenti —
(1) un `ReferenceError` (`receipt_created_date` variabile non definita) nel ramo di update; (2) match
fragile per uguaglianza stringa fra `last_updated_date` (`"YYYY-MM-DD"`) e `req.body.receipt_date`
(che arriva dalla stampante con l'ora) → si va sempre nel ramo `create` con `"001"`.

Correzioni rispetto al legacy:

- **Contatore che non incrementa** → incremento **atomico** lato DB
  (`INSERT … ON CONFLICT (printer_id, fiscal_date) DO UPDATE SET last_number =
  fiscalprint_serial_numbers.last_number + 1 RETURNING`).
- **`findAll` senza filtro `propertyId` né `printerId`** (il contatore era cercato **globalmente** per
  data — semanticamente errato) → chiave `(printer_id, fiscal_date)` (+ `property_id` denormalizzato
  per lo scoping/indici).
- **`last_updated_date` / `last_receipt_number` come `varchar(255)`** → `date` / `integer`
  ([`docs/03 §3.7`](../../../progettazione/03-gestione-dati.md)).
- **Nessun `UNIQUE`** (migliaia di righe duplicate per lo stesso giorno) → `UNIQUE(printer_id,
  fiscal_date)`.
- **`AUTO_INCREMENT` in pura crescita di righe inutili** → retention per giorno fiscale.
- **`receipt_serial_numbers` non aveva `printerId`** → in migrazione lo storico (numeri `"001"` privi
  di significato) **non si ricostruisce**: si parte da zero per `(printer_id, fiscal_date)` a
  go-live.

## Verifica di esclusività

Nessun flag `is_*`. `last_number` è un intero progressivo monotòno.

## Snapshot

Nessuno.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_fiscalprint_serial_numbers` | `id` | PK | — |
| `uq_fiscalprint_serial_numbers_key` | `(printer_id, fiscal_date)` | UNIQUE | chiave logica del contatore; target dell'`ON CONFLICT` |
| `ix_fiscalprint_serial_numbers_property_date` | `(property_id, fiscal_date DESC)` | btree | riepilogo numerazione emessa per stabilimento (tutte le sue stampanti) |

## Campi

### id
- **Legacy**: `receipt_serial_numbers.id`
- **Descrizione**: PK tecnica; la chiave "vera" è `(printer_id, fiscal_date)`.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### printer_id
- **Legacy**: — (la tabella legacy **non aveva** `printerId`; il numero era di fatto globale per data)
- **Descrizione**: stampante RT (= dispositivo fiscale) a cui la serie appartiene.
- **Tipo**: `bigint`, `NOT NULL`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`fiscalprint_printer_configs.id`](fiscalprint_printer_configs.md).
  Parte della chiave `UNIQUE`.

### property_id
- **Legacy**: `receipt_serial_numbers.propertyId` (`int`, `properties.id`, nessuna FK; **non** usato
  come filtro — vedi criticità legacy)
- **Descrizione**: stabilimento (denormalizzato da `fiscalprint_printer_configs.property_id` per lo
  scoping/indici).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2; [D-01](../_domande-committente.md).

### fiscal_date
- **Legacy**: `receipt_serial_numbers.last_updated_date` (`varchar(255)`; in creazione
  `moment().format("YYYY-MM-DD")`, in ricerca confrontata per uguaglianza stringa — match fallisce
  sempre)
- **Descrizione**: giorno a cui si riferisce il contatore (fuso `Europe/Rome`).
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: parte della chiave `UNIQUE`.

### last_number
- **Legacy**: `receipt_serial_numbers.last_receipt_number` (`varchar(255)`, sempre `"001"`)
- **Descrizione**: ultimo numero progressivo scontrino emesso quel giorno su quella stampante. Il
  prossimo userà `last_number + 1`.
- **Tipo**: `integer`, `NOT NULL`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (last_number >= 0)`.

### created_at / updated_at
- **Legacy**: `receipt_serial_numbers.createdAt` / `updatedAt` (`datetime` `NOT NULL`, Sequelize)
- **Descrizione**: creazione della riga / ultimo incremento.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: `updated_at` aggiornato dall'`ON CONFLICT DO UPDATE`.

### deleted_at
- **Legacy**: — (nessun soft-delete)
- **Descrizione**: esposto per uniformità ([`docs/03 §3.5`](../../../progettazione/03-gestione-dati.md)) ma
  **non usato**: tabella di sequenza/contatore.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- Poche righe/anno (stampanti RT × giorni operativi). Nessun problema di volume.
- Incremento atomico `INSERT … ON CONFLICT DO UPDATE … RETURNING` (elimina il `ReferenceError` e il
  match fragile del legacy).
- Retention: righe di giorni chiusi archiviabili dopo il termine di conservazione fiscale.

## Implementazione .NET

Modulo `fiscalprint` (stampa fiscale su hardware RT, meccanismo legacy alternativo allo scontrino
elettronico Fiskaly, mantenuto come componente separato — decisione di business, vedi
`_feature-tabelle.md` C4bis). `FiscalPrintNumberSequence` è **entità figlia**/
tabella di contatore (non aggregate root: nessuna identità di dominio oltre la chiave logica
`(printer_id, fiscal_date)`) del modulo `Fiscalprint`, schema `fiscalprint`. Nessun enum proprio. Non
implementa `ISoftDeletable` nel senso comportamentale (`deleted_at` esposto solo per uniformità, mai
scritto — tabella di sequenza/contatore, non entità di dominio cancellabile) né eventi di dominio.

```csharp
namespace Module.Fiscalprint.Infrastructure.Configurations;

public sealed class FiscalPrintNumberSequenceConfiguration : IEntityTypeConfiguration<FiscalPrintNumberSequence>
{
	public void Configure(EntityTypeBuilder<FiscalPrintNumberSequence> builder)
	{
		builder.ToTable("serial_numbers", "fiscalprint", t =>
		{
			t.HasComment("Contatore del numero progressivo scontrino RT, una riga per (printer_id, fiscal_date); incrementato atomicamente a ogni stampa.");
			t.HasCheckConstraint("ck_fiscalprint_serial_numbers_last_number", "last_number >= 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_fiscalprint_serial_numbers");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PrinterId)
			.HasColumnName("printer_id")
			.IsRequired()
			.HasComment("Stampante RT (dispositivo fiscale) a cui la serie appartiene — il progressivo è per dispositivo, non per stabilimento.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, denormalizzato da fiscalprint.printer_configs per lo scoping/indici — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.FiscalDate)
			.HasColumnName("fiscal_date")
			.IsRequired()
			.HasComment("Giorno a cui si riferisce il contatore, fuso Europe/Rome.");

		builder.Property(x => x.LastNumber)
			.HasColumnName("last_number")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Ultimo numero progressivo scontrino emesso quel giorno su quella stampante; il prossimo scontrino usa last_number + 1.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione della riga (primo scontrino del giorno su quella stampante).");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di ultimo incremento, aggiornata a ogni upsert.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete esposto per uniformità con le altre entità del modulo; non usato — tabella di sequenza/contatore.");

		builder.HasIndex(x => new { x.PrinterId, x.FiscalDate })
			.IsUnique()
			.HasDatabaseName("uq_fiscalprint_serial_numbers_key");

		builder.HasIndex(x => new { x.PropertyId, x.FiscalDate })
			.HasDatabaseName("ix_fiscalprint_serial_numbers_property_date");

		builder.HasOne(x => x.Printer)
			.WithMany()
			.HasForeignKey(x => x.PrinterId)
			.HasConstraintName("fk_fiscalprint_serial_numbers_printer")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
```

- `IFiscalPrintNumberSequence.NextAsync(printerId, fiscalDate)` (Infrastructure, `Dapper`/SQL puro o
  `ExecuteSqlRawAsync` — **non** un round-trip EF read-then-write): incremento **atomico**
  `INSERT ... ON CONFLICT (printer_id, fiscal_date) DO UPDATE SET last_number =
  fiscalprint.serial_numbers.last_number + 1 RETURNING last_number`, target dell'`ON CONFLICT` è
  l'indice unique `uq_fiscalprint_serial_numbers_key`; elimina sia il `ReferenceError` sia il confronto
  per uguaglianza stringa del legacy (le due cause concorrenti per cui il contatore non incrementava
  mai). `fiscalDate` calcolato dal chiamante in fuso `Europe/Rome` (mai `DateTime.UtcNow.Date`).
- **Note di migrazione**: lo storico `receipt_serial_numbers` (righe `"001"` per `(property_id, date)`,
  **senza** `printerId`, numeri privi di significato per il bug del contatore mai incrementato) **non
  si migra**: la serie parte da zero per `(printer_id, fiscal_date)` a go-live. Se serve continuità
  operativa, back-fill manuale delle sole righe recenti sul `printer_id` attivo `mode = fiscal` della
  property, non un backfill automatico dai dati legacy (non affidabili).
