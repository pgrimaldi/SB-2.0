# Tabella `fiscalprint_printer_departments`

> **Dominio**: C4bis — `fiscalprint` · **sottoarea**: — · **schema**: `fiscalprint` · **nome flat**:
> `fiscalprint_printer_departments` · **forma canonica**: `fiscalprint.printer_departments`.
>
> Figlia di [`fiscalprint_printer_configs`](fiscalprint_printer_configs.md). Sostituisce le 3 colonne
> `printer_configs.department_1` / `department_2` / `department_3`
> ([`analisi-pre/db/printer_configs.md`](../../../analisi-pre/db/printer_configs.md), campo
> `department_1 / _2 / _3` e §"criticità"). Fonti:
> [`analisi-pre/db/printer_configs.md`](../../../analisi-pre/db/printer_configs.md),
> [`docs/03-gestione-dati.md` §2.5, §3.7](../../../progettazione/03-gestione-dati.md),
> `front-pre/src/services/printers/departmentResolver.js`.

---

## Descrizione

**Una riga = un "reparto" fisico del registratore di cassa RT mappato su un'aliquota IVA.** Un
registratore RT ha reparti numerati (1, 2, 3, …), ognuno pre-programmato *sulla stampante* con una
certa aliquota; qui si registra quella corrispondenza così che il software sappia su quale reparto
battere ogni voce (`departmentResolver.resolveDepartment(percentIVA, printerConfig)`: match esatto per
aliquota, fallback sul reparto a 22%).

Correzioni rispetto al legacy (tutte da [`printer_configs.md`](../../../analisi-pre/db/printer_configs.md)
§"criticità"):

- **Numero di reparti fissato a 3** da 3 colonne → **N righe** (i registratori RT reali ne gestiscono
  decine).
- **`"22%"` = numero salvato come stringa col simbolo** (ogni lettura fa `replace("%","")` +
  `parseFloat`; formati fuori standard `"22"` / `"22 %"` / `"iva22"` mandano a vuoto il match) →
  `vat_rate numeric(5,2)` ([`docs/03 §3.7`](../../../progettazione/03-gestione-dati.md)).
- **Legame reparto↔aliquota posizionale** (l'indice di colonna *è* il numero di reparto: non
  esplicito, non vincolabile, non indicizzabile) → `dept_number` esplicito +
  `UNIQUE(printer_id, dept_number)`. Il resolver diventa un `WHERE printer_id = ? AND vat_rate = ?`.

## Verifica di esclusività

Nessun flag `is_*`. `dept_number` e `vat_rate` sono valori scalari; l'unicità della coppia è vincolo,
non enum.

## Snapshot

Nessuno.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_fiscalprint_printer_departments` | `id` | PK | — |
| `uq_fiscalprint_printer_departments_number` | `(printer_id, dept_number) WHERE deleted_at IS NULL` | UNIQUE parziale | un solo mapping per numero di reparto |
| `uq_fiscalprint_printer_departments_rate` | `(printer_id, vat_rate) WHERE deleted_at IS NULL` | UNIQUE parziale | **candidato**: un'aliquota su un solo reparto (rende deterministico il resolver — da confermare) ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): l'unicità esisteva già, ma col nome di `ix_..._resolver`, perché la causa era EF: due `HasIndex` sulle stesse colonne senza nome sono lo stesso indice, e il secondo sovrascrive il primo; ora ha il suo nome e il resolver è tornato un btree semplice. |
| `ix_fiscalprint_printer_departments_resolver` | `(printer_id, vat_rate)` | btree | lookup del resolver (`printer_id` + `vat_rate` → `dept_number`) |

## Campi

### id
- **Legacy**: — (elemento posizionale, senza id)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### printer_id
- **Legacy**: `printer_configs.id` (le colonne stavano sulla riga stampante)
- **Descrizione**: stampante a cui il reparto appartiene.
- **Tipo**: `bigint`, `NOT NULL`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`fiscalprint_printer_configs.id`](fiscalprint_printer_configs.md),
  `ON DELETE CASCADE`.

### property_id
- **Legacy**: — (nuovo; da `fiscalprint_printer_configs.property_id`)
- **Descrizione**: stabilimento (denormalizzato per lo scoping).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2.

### dept_number
- **Legacy**: l'indice della colonna (`department_1` → `1`, `_2` → `2`, `_3` → `3`)
- **Descrizione**: numero del reparto fisico sul registratore RT (esplicito, non più posizionale).
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `CHECK (dept_number > 0)`; `UNIQUE(printer_id, dept_number)`.

### vat_rate
- **Legacy**: `printer_configs.department_N` (`varchar` `"22%"` / `"10%"` / `"4%"` / `"0%"` / `""` / `NULL`)
- **Descrizione**: aliquota IVA con cui il reparto è pre-programmato.
- **Tipo**: `numeric(5,2)`.
- **Default**: nessuno.
- **Nullabile**: No (le righe legacy vuote/`NULL` non generano riga figlia).
- **Relazioni**: `CHECK (vat_rate >= 0)`.

### created_at / updated_at / deleted_at
- **Legacy**: da `printer_configs.createdAt` / `updatedAt` / (soft-delete della stampante)
- **Descrizione**: creazione / modifica / soft-delete della mappatura.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: cascade soft-delete dalla stampante.

## Valutazione JSON

Nessun JSON. Le 3 colonne stringa posizionali diventano righe tipizzate.

## Suggerimenti per le prestazioni

- Pochissime righe per stampante (2–4 tipiche). Caricate in memoria con la config della stampante.
- Il resolver diventa una lookup su indice invece di 3 `parseFloat` su stringa.

## Implementazione .NET

Modulo `fiscalprint` (stampa fiscale su hardware RT, meccanismo legacy alternativo allo scontrino
elettronico Fiskaly, mantenuto come componente separato — decisione di business, vedi
`_feature-tabelle.md` C4bis). `PrinterDepartment` è **entità figlia** (tabella
propria, non owned type: ha `id`/`created_at`/`updated_at`/`deleted_at` propri) dell'aggregate root
`PrinterConfig` del modulo `Fiscalprint`, schema `fiscalprint`. Nessun enum proprio.

```csharp
namespace Module.Fiscalprint.Infrastructure.Configurations;

public sealed class PrinterDepartmentConfiguration : IEntityTypeConfiguration<PrinterDepartment>
{
	public void Configure(EntityTypeBuilder<PrinterDepartment> builder)
	{
		builder.ToTable("printer_departments", "fiscalprint", t =>
		{
			t.HasComment("Mappatura reparto fisico del registratore di cassa RT → aliquota IVA con cui il reparto è pre-programmato.");
			t.HasCheckConstraint("ck_fiscalprint_printer_departments_number", "dept_number > 0");
			t.HasCheckConstraint("ck_fiscalprint_printer_departments_rate", "vat_rate >= 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_fiscalprint_printer_departments");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PrinterId)
			.HasColumnName("printer_id")
			.IsRequired()
			.HasComment("Stampante (fiscalprint.printer_configs) a cui il reparto appartiene.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, denormalizzato per lo scoping — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.DeptNumber)
			.HasColumnName("dept_number")
			.IsRequired()
			.HasComment("Numero del reparto fisico sul registratore RT (esplicito, non più posizionale come nel legacy).");

		builder.Property(x => x.VatRate)
			.HasColumnName("vat_rate")
			.HasColumnType("numeric(5,2)")
			.IsRequired()
			.HasComment("Aliquota IVA con cui il reparto è pre-programmato sulla stampante.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione della mappatura.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di ultima modifica della mappatura.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete, in cascata dalla cancellazione della stampante proprietaria.");

		builder.HasIndex(x => new { x.PrinterId, x.DeptNumber })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_fiscalprint_printer_departments_number");

		builder.HasIndex(x => new { x.PrinterId, x.VatRate })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_fiscalprint_printer_departments_rate");

		builder.HasIndex(x => new { x.PrinterId, x.VatRate })
			.HasDatabaseName("ix_fiscalprint_printer_departments_resolver");

		builder.HasOne(x => x.Printer)
			.WithMany(p => p.Departments)
			.HasForeignKey(x => x.PrinterId)
			.HasConstraintName("fk_fiscalprint_printer_departments_printer")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

- `IDepartmentResolver.Resolve(printerId, vatRate)` (Application, sostituto server-side di
  `departmentResolver.js`): lookup su `ix_fiscalprint_printer_departments_resolver` invece dei 3
  `parseFloat` su stringa del legacy; fallback configurabile (oggi hardcoded al reparto a 22%) da
  rendere esplicito in `PrinterConfig` o in configurazione di modulo.
- Nota: due indici unique parziali coincidono sulla stessa coppia di colonne
  (`(printer_id, dept_number)` e `(printer_id, vat_rate)`, entrambi filtrati `deleted_at IS NULL`) più
  un terzo non-unique identico a quest'ultimo per il resolver — ridondanza accettata perché
  l'unicità su `vat_rate` resta **candidata** (da confermare in fase implementativa, vedi sezione
  Indici); se confermata, l'indice non-unique del resolver diventa superfluo e va rimosso.
- **Note di migrazione**: per ogni `department_1`/`_2`/`_3` non vuoto/non-`NULL` di
  `printer_configs` → una riga `(dept_number = N, vat_rate = parse("NN%") → NN.00)`; valori fuori
  formato (`"iva22"`, `"22 %"` malformati oltre il semplice trim del simbolo) loggati e scartati,
  non forzati a un default.
