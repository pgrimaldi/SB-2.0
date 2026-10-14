# Tabella `fiskaly_receipt_sequences`

> **Dominio**: C4 — `fiskaly` · **sottoarea**: — · **schema**: `fiskaly` · **nome flat**:
> `fiskaly_receipt_sequences` · **forma canonica**: `fiskaly.receipt_sequences`.
>
> Sostituisce [`fiskaly_receipt_sequence`](../../../analisi-pre/db/fiskaly_receipt_sequence.md)
> (234 righe; una per `(system_id, giorno)`). Fonti:
> [`analisi-pre/db/fiskaly_receipt_sequence.md`](../../../analisi-pre/db/fiskaly_receipt_sequence.md),
> [`docs/03-gestione-dati.md` §3.4, §3.5](../../../progettazione/03-gestione-dati.md).
> Concettualmente analoga a [`booking_number_sequences`](../booking/booking_number_sequences.md)
> (contatore) e a [`fiscalprint_serial_numbers`](../fiscalprint/fiscalprint_serial_numbers.md).

---

## Descrizione

**Contatore progressivo giornaliero del numero documento commerciale per registratore di cassa**:
**1 riga per `(cash_register_id, fiscal_date)`**. `last_number` è l'ultimo numero fiscale emesso quel
giorno su quella cassa; il valore va sul documento come
[`fiskaly_receipts.document_number`](fiskaly_receipts.md) — che quindi **non è univoco**: riparte da 1
ogni giorno e per ogni cassa.

Correzioni rispetto al legacy:

- **Due implementazioni** dello stesso contatore (`peek`+`commit` **vs** `getNextDocumentNumber`) con
  **fusi orari diversi** (Europe/Rome vs UTC → a cavallo di mezzanotte incrementano righe di date
  diverse per lo stesso giorno fiscale) → **una sola**: incremento atomico lato DB
  (`INSERT … ON CONFLICT (cash_register_id, fiscal_date) DO UPDATE SET last_number =
  GREATEST(fiskaly_receipt_sequences.last_number, EXCLUDED.last_number) RETURNING`), giorno fiscale
  **sempre Europe/Rome**.
- **`system_id` come chiave** (`varchar(36)`, FK `ON UPDATE CASCADE` senza `ON DELETE` → righe
  orfane) → FK interna `cash_register_id` (`bigint`). **Nessuna colonna `fiskaly_system_id`
  denormalizzata**: si ottiene dal join a `fiskaly_cash_registers`, non ha consumatori propri.
- **Nessun timestamp** (`timestamps: false`, nessuna colonna) → `created_at` / `updated_at` (audit:
  quando la sequenza è stata toccata l'ultima volta).
- **`last_number int` come contatore** — non un vocabolario, resta intero; `CHECK (>= 0)`.

## Verifica di esclusività

Nessun flag `is_*`. `last_number` è un intero progressivo monotòno (mai decrementato:
`commitDocumentNumber` non lo abbassa), incrementato **solo su firma riuscita** (un tentativo fallito
non "brucia" un numero).

## Snapshot

Nessuno. Se una cassa viene **ricreata su Fiskaly** (decommission → nuovo `system_id`, stessa riga
`fiskaly_cash_registers`), il contatore **prosegue** invece di ripartire: accettabile perché
`document_number` si azzera comunque **ogni giorno** (`fiscal_date`), quindi al più c'è un System
attivo per cassa in una data — da confermare col commercialista se la ricreazione mid-day deve invece
forzare il reset ([D-44](../_domande-committente.md)).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_fiskaly_receipt_sequences` | `id` | PK | — |
| `uq_fiskaly_receipt_sequences_key` | `(cash_register_id, fiscal_date)` | UNIQUE | chiave logica del contatore; target dell'`ON CONFLICT` |
| `ix_fiskaly_receipt_sequences_property_date` | `(property_id, fiscal_date DESC)` | btree | riepilogo numerazione emessa per stabilimento/giorno |

## Campi

### id
- **Legacy**: `fiskaly_receipt_sequence.id`
- **Descrizione**: PK tecnica; la chiave "vera" è `(cash_register_id, fiscal_date)`.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### property_id
- **Legacy**: — (nuovo; da `fiskaly_cash_register → fiskaly_account.property_id`)
- **Descrizione**: stabilimento (denormalizzato per lo scoping/indici).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2.

### cash_register_id
- **Legacy**: `fiskaly_receipt_sequence.system_id` (`varchar(36)`, FK → `fiskaly_cash_register.system_id`)
- **Descrizione**: registratore di cassa a cui la sequenza appartiene.
- **Tipo**: `bigint`, `NOT NULL`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`fiskaly_cash_registers.id`](fiskaly_cash_registers.md).

### fiscal_date
- **Legacy**: `fiskaly_receipt_sequence.date` (`DATEONLY`; calcolata in `Europe/Rome` da `peek`/
  `commit`, in **UTC** da `getNextDocumentNumber` — incoerenza)
- **Descrizione**: **giorno fiscale** del contatore, **sempre `Europe/Rome`**.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: parte della chiave `UNIQUE`.

### last_number
- **Legacy**: `fiskaly_receipt_sequence.last_number` (`int` `NOT NULL` `DEFAULT 0`; max osservato 52)
- **Descrizione**: ultimo numero progressivo di documento **effettivamente emesso** quel giorno su
  quella cassa. Il prossimo userà `last_number + 1`. Aggiornato in modo **monotòno** e solo su firma
  riuscita.
- **Tipo**: `integer`, `NOT NULL`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (last_number >= 0)`.

### created_at / updated_at
- **Legacy**: nessuno (`timestamps: false`)
- **Descrizione**: creazione della riga / ultimo incremento (audit).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: `updated_at` aggiornato dall'`ON CONFLICT DO UPDATE`.

### deleted_at
- **Legacy**: — (nessun soft-delete; la sequenza non si cancella)
- **Descrizione**: esposto per uniformità ([`docs/03 §3.5`](../../../progettazione/03-gestione-dati.md)) ma
  **non usato**: tabella di sequenza/contatore.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- Poche centinaia di righe/anno (casse attive × giorni operativi). Nessun problema di volume.
- L'incremento atomico `INSERT … ON CONFLICT DO UPDATE … RETURNING` evita il pattern legacy
  `SELECT` + `+1` + `update` (race garantita) e le due implementazioni divergenti.
- Retention: le righe di giorni passati si possono archiviare/eliminare dopo il termine di
  conservazione fiscale del relativo giorno (i numeri sono comunque congelati su `fiskaly_receipts`).

## Implementazione .NET

`ReceiptSequence` è un'entità **figlia/di supporto** del modulo `Module.Fiskaly` (schema `fiskaly`):
contatore progressivo giornaliero del numero documento per registratore di cassa, 1 riga per
`(cash_register_id, fiscal_date)`. Non è owned da `FiskalyAccount`/`CashRegister` (vive in una propria
tabella con upsert atomico) ma referenzia `CashRegister` con FK reale.

Nessun enum proprio: `last_number` resta un contatore intero, non un vocabolario.

### Configuration

```csharp
namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class ReceiptSequenceConfiguration : IEntityTypeConfiguration<ReceiptSequence>
{
	public void Configure(EntityTypeBuilder<ReceiptSequence> builder)
	{
		builder.ToTable("fiskaly_receipt_sequences", "fiskaly", t => t.HasComment(
			"Contatore progressivo giornaliero del numero documento commerciale per registratore di cassa."));

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_receipt_sequences");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per lo scoping/indici.");
		builder.HasIndex(x => new { x.PropertyId, x.FiscalDate })
			.HasDatabaseName("ix_fiskaly_receipt_sequences_property_date");

		builder.Property(x => x.CashRegisterId)
			.HasColumnName("cash_register_id")
			.IsRequired()
			.HasComment("Registratore di cassa a cui la sequenza appartiene.");
		builder.HasOne<CashRegister>()
			.WithMany()
			.HasForeignKey(x => x.CashRegisterId)
			.HasConstraintName("fk_fiskaly_receipt_sequences_cash_register")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.FiscalDate)
			.HasColumnName("fiscal_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Giorno fiscale del contatore, sempre calcolato in Europe/Rome.");
		builder.HasIndex(x => new { x.CashRegisterId, x.FiscalDate })
			.IsUnique()
			.HasDatabaseName("uq_fiskaly_receipt_sequences_key");

		builder.Property(x => x.LastNumber)
			.HasColumnName("last_number")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Ultimo numero progressivo di documento effettivamente emesso quel giorno su quella cassa; monotòno, incrementato solo su firma riuscita.");

		builder.ToTable(t => t.HasCheckConstraint("ck_fiskaly_receipt_sequences_last_number", "last_number >= 0"));

		// created_at/updated_at (IAuditable) — plumbing comune; deleted_at esposto per uniformità ma non usato
	}
}
```

`ReceiptSequence` implementa `IAuditable`; **non** implementa `ISoftDeletable` in modo operativo (la
sequenza non si cancella — `deleted_at` resta sempre `NULL`).

### Repository dedicato

```csharp
namespace Module.Fiskaly.Domain;

public interface IReceiptNumberSequence
{
	// upsert atomico: INSERT ... ON CONFLICT (cash_register_id, fiscal_date)
	// DO UPDATE SET last_number = GREATEST(last_number, EXCLUDED.last_number) RETURNING last_number
	Task<int> NextAsync(long cashRegisterId, DateOnly fiscalDate, CancellationToken ct);
}
```

Fuso orario `Europe/Rome` per `fiscalDate` risolto da `TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome")`
lato Application, mai calcolato in UTC come faceva `getNextDocumentNumber` legacy — unifica le due
implementazioni divergenti del contatore.

### Note di migrazione

- `fiskaly_receipt_sequence` 1:1; `system_id` (`varchar(36)`) → `cash_register_id` (`bigint`) via join
  su `fiskaly_cash_registers.fiskaly_system_id` (righe orfane del legacy, senza cassa risolvibile, da
  segnalare e non migrare); `date` → `fiscal_date`.
- `peek` + `commit` legacy collassati in un solo upsert atomico consumato al momento della firma
  riuscita: si accetta un eventuale "buco" nella numerazione in caso di fallimento a valle (più
  semplice e sempre corretto) — alternativa **da confermare col committente** ([D-44](../_domande-committente.md)):
  peek + commit espliciti in transazione se si vuole garantire numerazione senza buchi.
- Nessun timestamp nel legacy (`timestamps: false`): `created_at`/`updated_at` nuovi, `updated_at`
  aggiornato dall'upsert `ON CONFLICT DO UPDATE`.
