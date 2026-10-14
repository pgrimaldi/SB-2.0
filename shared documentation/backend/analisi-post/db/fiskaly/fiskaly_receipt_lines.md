# Tabella `fiskaly_receipt_lines`

> **Dominio**: C4 — `fiskaly` · **sottoarea**: — · **schema**: `fiskaly` · **nome flat**:
> `fiskaly_receipt_lines` · **forma canonica**: `fiskaly.receipt_lines`.
>
> Figlia di [`fiskaly_receipts`](fiskaly_receipts.md). Sostituisce l'array JSON
> [`fiskaly_receipt.entries`](../../../analisi-pre/db/fiskaly_receipt.md) /
> [`fiskaly_non_fiscal_receipt.entries`](../../../analisi-pre/db/fiskaly_non_fiscal_receipt.md)
> **+** la parte `paid_deltas.items[]` (quota pagata per voce). Input a monte:
> [`booking_number_informations.details_payments*`](../../../analisi-pre/db/booking_number_informations.md).
> Fonti:
> [`analisi-pre/db/fiskaly_receipt.md`](../../../analisi-pre/db/fiskaly_receipt.md),
> [`docs/03-gestione-dati.md` §2.4ter, §2.5, §3.6, §3.7](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga = una voce del documento commerciale** (pacchetto, articolo aggiuntivo, servizio,
sovrapprezzo, voucher convenzione) — descrizione, quantità, prezzo unitario lordo/netto, IVA — **tutti
congelati** al momento dell'emissione (§3.6 punto 2: fatto fiscale immutabile). Duplica volutamente ciò
che C1 registra sulle righe prenotazione: qui è la prova del *cosa* è stato scontrinato.

Correzioni rispetto al legacy:

- **Blob JSON `entries`** con vocabolari come stringhe libere (`SALE`/`RETURN`, `SERVICE`/`GOODS`,
  `VAT_RATE`/`VAT_EXEMPTION`, `STANDARD`/`REDUCED_*`) → colonne tipizzate + `enum` / FK a lookup
  ([`docs/03 §2.5, §2.4ter`](../../../progettazione/03-gestione-dati.md)).
- **Numeri come stringa** (`quantity` `"1"`, `unit_price.incl_vat` `"75.00"`, `vat.rate` `"22.00"`,
  `vat.amount` `"13.52"`) → `numeric` ([`docs/03 §3.7`](../../../progettazione/03-gestione-dati.md)).
- **`vat.code`** (`STANDARD` / `REDUCED_1..3` / `NOT_SUBJECT` / `NOT_TAXABLE` / `CAUSE_1..4`) →
  FK a [`fiskaly_vat_codes`](fiskaly_vat_codes.md) (**lookup**: può crescere senza deploy,
  [`docs/03 §2.4ter`](../../../progettazione/03-gestione-dati.md)).
- **`paid_deltas.items[]` separato** (`{ id, type, lastPaid }`) → `paid_allocation` + `line_kind` +
  `source_line_public_id` qui, sulla riga a cui si riferiscono (era un secondo array parallelo).
- **`entries[].id` = `lineItemId` sparso in `bookings.packageSelectedData`** → `source_line_public_id`
  (FK viva C1); la correlazione `entries[].id ↔ paid_deltas.items[].id` si risolve **in memoria
  durante la migrazione** mentre si costruiscono le righe — l'id grezzo legacy **non** si persiste
  (righe non risolvibili → `source_line_public_id = NULL`).

## Verifica di esclusività

| Gruppo | Campi | Esito |
|---|---|---|
| Vendita / reso | `fiskaly_entry_type` | **enum** esclusivo: `sale` / `return` (nei dati solo `sale`). |
| Natura fiscale della voce | `category` | **enum** esclusivo: `service` / `goods` (nei dati solo `service`). |
| Tipo IVA | `vat_kind` | **enum** esclusivo: `vat_rate` / `vat_exemption`. Denormalizzato dal `fiskaly_vat_codes` per il fatto congelato. |
| Tipo di voce-prenotazione | `line_kind` | **enum** esclusivo (una voce è di **un** tipo): `package` / `additional_item` / `bar_service` / `normal_service` / `hourly_service` / `agreement_voucher_package` / `standard_voucher_fixed` / `standard_voucher_percentage` / `surcharge_fixed` / `surcharge_percentage` / `pos_charge`. |

## Snapshot

| Campo | Congela | FK viva | Suffisso |
|---|---|---|---|
| `description_snapshot` | nome articolo (pacchetto/servizio) | `source_line_public_id` | **Sì** (`_snapshot`, regola C1-7) |
| `quantity`, `unit_price_gross`, `unit_price_net`, `vat_rate`, `vat_amount`, `line_gross`, `line_net`, `paid_allocation` | numeri propri del documento fiscale | — | **No** — la natura "congelata" è nella descrizione (§3.6, C1-7) |
| `line_kind`, `fiskaly_entry_type`, `category`, `vat_kind` | enum propri del documento | — | **No** |

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_fiskaly_receipt_lines` | `id` | PK | — |
| `ix_fiskaly_receipt_lines_receipt` | `(receipt_id, line_no)` | btree | righe di uno scontrino in ordine |
| `ix_fiskaly_receipt_lines_source` | `(source_line_public_id) WHERE source_line_public_id IS NOT NULL` | btree parziale | coerenza "voci pagate vs modifica prenotazione" (`checkPaidLineItemsWithPrevious`) |
| `ix_fiskaly_receipt_lines_vat` | `(property_id, vat_code_id)` | btree | riepiloghi IVA per stabilimento |

## Campi

### id
- **Legacy**: elemento dell'array `entries` (senza id proprio — `entries[].id` è il `lineItemId`)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### receipt_id
- **Legacy**: `fiskaly_receipt.id` (l'array stava sulla testata)
- **Descrizione**: documento a cui la voce appartiene.
- **Tipo**: `bigint`, `NOT NULL`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`fiskaly_receipts.id`](fiskaly_receipts.md), `ON DELETE CASCADE`
  (di fatto mai: fatto fiscale immutabile).

### property_id
- **Legacy**: — (nuovo; da `fiskaly_receipts.property_id`)
- **Descrizione**: stabilimento (denormalizzato per i riepiloghi IVA senza join).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2.

### line_no
- **Legacy**: posizione nell'array `entries`
- **Descrizione**: ordine della voce nel documento (1-based).
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE (receipt_id, line_no)`.

### line_kind
- **Legacy**: `fiskaly_receipt.paid_deltas.items[].type` / `details_payments_new[].type`
  (`"package"` / `"additional"` / `"service"` / `"agreement_voucher_package"` / `"surcharge_fixed"` / …)
- **Descrizione**: tipo di voce-prenotazione sottostante. Vocabolario **condiviso** con
  `bookings.booking_element` e le voci di `booking_number_informations`
  (`LineItemTypeEnum` di `fiskaly.helper.js`).
- **Tipo**: `enum fiskaly.line_item_type` — vedi §"Verifica di esclusività".
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### fiskaly_entry_type
- **Legacy**: `entries[].type` (`"SALE"` / `"RETURN"`)
- **Descrizione**: vendita o reso.
- **Tipo**: `enum fiskaly.entry_type` — valori: `sale`, `return`.
- **Default**: `sale`.
- **Nullabile**: No.
- **Relazioni**: `FiskalyLineItemType` ([`docs/03 §2.4ter`](../../../progettazione/03-gestione-dati.md)).

### category
- **Legacy**: `entries[].category` (`"SERVICE"` / `"GOODS"`; `item.category || "SERVICE"`; verso
  Fiskaly `details.concept`)
- **Descrizione**: natura fiscale della voce.
- **Tipo**: `enum fiskaly.entry_category` — valori: `service`, `goods`.
- **Default**: `service`.
- **Nullabile**: No.
- **Relazioni**: `FiskalyLineItemCategory`.

### source_line_public_id
- **Legacy**: `entries[].id` (= `lineItemId` in `bookings.packageSelectedData` / `servicesData`)
- **Descrizione**: riga prenotazione C1 di provenienza (postazione, servizio o articolo aggiuntivo),
  quando risolvibile. `NULL` per le voci **sintetiche** (`surcharge_fixed`/`surcharge_percentage`,
  voci storiche non mappabili) — identificate comunque da `line_no` + `line_kind` +
  `description_snapshot`.
- **Tipo**: `uuid`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C1 — [`booking_lines`](../booking/booking_lines.md),
  [`booking_service_lines`](../booking/booking_service_lines.md) o
  [`booking_line_additional_items`](../booking/booking_line_additional_items.md) a seconda di
  `line_kind`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 2.852 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### description_snapshot
- **Legacy**: `entries[].description` (nome pacchetto/servizio, es. `"Ombrellone e 3 lettini … Non
  rimborsabile"`, `"Pedalò"`, `"Cabina"`)
- **Descrizione**: descrizione articolo **congelata** all'emissione — regola `_snapshot` (nome
  copiato da un'entità mutabile C1 con la FK viva `source_line_public_id` accanto).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: se `source_line_public_id` è valorizzato, il nome "vivo" si legge da C1/C2.

### quantity
- **Legacy**: `entries[].quantity` (`string`, es. `"1"`)
- **Descrizione**: quantità della voce.
- **Tipo**: `numeric(12,3)` (ammette frazioni per servizi a peso/ore; nei dati sempre intera).
- **Default**: `1`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (quantity > 0)`.

### unit_price_gross / unit_price_net
- **Legacy**: `entries[].unit_price.incl_vat` / `entries[].unit_price.excl_vat` (`string`)
- **Descrizione**: prezzo unitario IVA inclusa / esclusa.
- **Tipo**: `numeric(12,2)` ciascuno.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### vat_code_id
- **Legacy**: `entries[].vat.code` (`"STANDARD"` / `"REDUCED_1..3"` / `"NOT_SUBJECT"` / `"CAUSE_1..4"`)
- **Descrizione**: codice IVA/esenzione della voce.
- **Tipo**: `integer`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`fiskaly_vat_codes.id`](fiskaly_vat_codes.md).

### vat_kind
- **Legacy**: `entries[].vat.type` (`"VAT_RATE"` / `"VAT_EXEMPTION"`)
- **Descrizione**: tipo IVA — denormalizzato dal `fiskaly_vat_codes` **e** congelato sulla riga (il
  codice IVA in lookup potrebbe cambiare label/attivazione, il fatto fiscale no).
- **Tipo**: `enum fiskaly.vat_kind` — valori: `vat_rate`, `vat_exemption`.
- **Default**: `vat_rate`.
- **Nullabile**: No.
- **Relazioni**: coerente con `fiskaly_vat_codes.kind` del `vat_code_id`.

### vat_rate
- **Legacy**: `entries[].vat.rate` (`string`, es. `"22.00"`)
- **Descrizione**: aliquota % applicata (per `vat_exemption` = `0`). **Dato proprio del documento**,
  nessun suffisso `_snapshot` (§3.6, C1-7).
- **Tipo**: `numeric(5,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### vat_amount
- **Legacy**: `entries[].vat.amount` (`string`, es. `"13.52"`)
- **Descrizione**: IVA della riga.
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `Σ` → `fiskaly_receipts.vat_amount`.

### line_gross / line_net
- **Legacy**: derivati (`quantity × unit_price.*`)
- **Descrizione**: totale riga IVA inclusa / imponibile riga. Materializzati per i riepiloghi.
- **Tipo**: `numeric(12,2)` ciascuno.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `Σ line_net` → `fiskaly_receipts.taxable_amount`.

### paid_allocation
- **Legacy**: `fiskaly_receipt.paid_deltas.items[].lastPaid` (`number`)
- **Descrizione**: quota di incasso che **questo** scontrino ha attribuito a questa voce (era il
  delta da revertire su `booking_number_informations` in caso di annullamento).
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: usata dalla coerenza `checkPaidLineItemsWithPrevious` in modifica prenotazione; il
  revert è uno storno append-only in [`payment_ledger_entries`](../payment/payment_ledger_entries.md).

### created_at / updated_at / deleted_at
- **Legacy**: da `fiskaly_receipt.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica. `deleted_at` esposto per uniformità: append-only con la
  testata (fatto fiscale, retention 10 anni).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF (sempre `NULL`).

## Valutazione JSON

`entries` (array) e la parte `paid_deltas.items[]` → questa tabella. Nessun JSON residuo: ogni
sotto-oggetto (`unit_price`, `vat`) è esploso in colonne tipizzate.

## Suggerimenti per le prestazioni

- Poche righe per scontrino (1–5 tipiche). Volume = `~righe scontrini × 2`. Indici minimi.
- `(property_id, vat_code_id)` per i riepiloghi IVA periodici (chiusure, dichiarazioni).
- `source_line_public_id` parziale per il check di coerenza in fase di modifica prenotazione (evita
  lo scan del blob legacy).

## Implementazione .NET

`ReceiptLine` è **owned entity** (owned collection `Lines`) dell'aggregato `Receipt`
([`fiskaly_receipts`](fiskaly_receipts.md)), modulo `Module.Fiskaly`, schema `fiskaly`: una voce del
documento commerciale, congelata all'emissione.

### Enum propri

```csharp
// Domain/Enums/LineItemType.cs — vocabolario condiviso con bookings.booking_element
public enum LineItemType
{
	Package = 0,
	AdditionalItem = 1,
	BarService = 2,
	NormalService = 3,
	HourlyService = 4,
	AgreementVoucherPackage = 5,
	StandardVoucherFixed = 6,
	StandardVoucherPercentage = 7,
	SurchargeFixed = 8,
	SurchargePercentage = 9,
	PosCharge = 10
}

// Domain/Enums/FiskalyEntryType.cs
public enum FiskalyEntryType { Sale = 0, Return = 1 }

// Domain/Enums/FiskalyEntryCategory.cs
public enum FiskalyEntryCategory { Service = 0, Goods = 1 }
```

`FiskalyVatKind` (`fiskaly.vat_kind`) è già definito e registrato su
[`fiskaly_vat_codes`](fiskaly_vat_codes.md) — riusato qui senza ridefinizione (valore congelato sulla
riga, denormalizzato dal lookup).

```csharp
// FiskalyDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<LineItemType>("fiskaly", "line_item_type");
modelBuilder.HasPostgresEnum<FiskalyEntryType>("fiskaly", "entry_type");
modelBuilder.HasPostgresEnum<FiskalyEntryCategory>("fiskaly", "entry_category");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<LineItemType>("fiskaly.line_item_type");
dataSourceBuilder.MapEnum<FiskalyEntryType>("fiskaly.entry_type");
dataSourceBuilder.MapEnum<FiskalyEntryCategory>("fiskaly.entry_category");
```

### Configuration

```csharp
namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class ReceiptLineConfiguration : IEntityTypeConfiguration<ReceiptLine>
{
	public void Configure(EntityTypeBuilder<ReceiptLine> builder)
	{
		builder.ToTable("fiskaly_receipt_lines", "fiskaly", t =>
		{
			t.HasComment("Voce del documento commerciale (pacchetto, articolo, servizio, sovrapprezzo) congelata al momento dell'emissione.");
			t.HasCheckConstraint("ck_fiskaly_receipt_lines_quantity", "quantity > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_receipt_lines");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReceiptId)
			.HasColumnName("receipt_id")
			.IsRequired()
			.HasComment("Documento a cui la voce appartiene.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per i riepiloghi IVA senza join.");
		builder.HasIndex(x => new { x.PropertyId, x.VatCodeId })
			.HasDatabaseName("ix_fiskaly_receipt_lines_vat");

		builder.Property(x => x.LineNo)
			.HasColumnName("line_no")
			.HasColumnType("smallint")
			.IsRequired()
			.HasComment("Ordine della voce nel documento (1-based).");
		builder.HasIndex(x => new { x.ReceiptId, x.LineNo })
			.IsUnique()
			.HasDatabaseName("ix_fiskaly_receipt_lines_receipt");

		builder.Property(x => x.LineKind)
			.HasColumnName("line_kind")
			.HasColumnType("fiskaly.line_item_type")
			.IsRequired()
			.HasComment("Tipo di voce-prenotazione sottostante (pacchetto/servizio/articolo/sovrapprezzo/…), vocabolario condiviso con booking.");

		builder.Property(x => x.FiskalyEntryType)
			.HasColumnName("fiskaly_entry_type")
			.HasColumnType("fiskaly.entry_type")
			.IsRequired()
			.HasDefaultValue(FiskalyEntryType.Sale)
			.HasComment("Vendita o reso.");

		builder.Property(x => x.Category)
			.HasColumnName("category")
			.HasColumnType("fiskaly.entry_category")
			.IsRequired()
			.HasDefaultValue(FiskalyEntryCategory.Service)
			.HasComment("Natura fiscale della voce: servizio o bene.");

		builder.Property(x => x.SourceLinePublicId)
			.HasColumnName("source_line_public_id")
			.HasComment("public_id della riga prenotazione C1 di provenienza (booking_lines / booking_service_lines / booking_line_additional_items secondo line_kind), copiato — nessuna FK cross-schema. NULL per voci sintetiche non mappabili.");
		builder.HasIndex(x => x.SourceLinePublicId)
			.HasFilter("source_line_public_id IS NOT NULL")
			.HasDatabaseName("ix_fiskaly_receipt_lines_source");

		builder.Property(x => x.DescriptionSnapshot)
			.HasColumnName("description_snapshot")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Descrizione articolo congelata all'emissione (nome pacchetto/servizio al momento del documento).");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasColumnType("numeric(12,3)")
			.IsRequired()
			.HasDefaultValue(1m)
			.HasComment("Quantità della voce (ammette frazioni per servizi a peso/ore).");

		builder.Property(x => x.UnitPriceGross)
			.HasColumnName("unit_price_gross")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Prezzo unitario IVA inclusa.");

		builder.Property(x => x.UnitPriceNet)
			.HasColumnName("unit_price_net")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Prezzo unitario IVA esclusa.");

		builder.Property(x => x.VatCodeId)
			.HasColumnName("vat_code_id")
			.IsRequired()
			.HasComment("Codice IVA/esenzione della voce.");
		builder.HasOne<VatCode>()
			.WithMany()
			.HasForeignKey(x => x.VatCodeId)
			.HasConstraintName("fk_fiskaly_receipt_lines_vat_code")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.VatKind)
			.HasColumnName("vat_kind")
			.HasColumnType("fiskaly.vat_kind")
			.IsRequired()
			.HasDefaultValue(FiskalyVatKind.VatRate)
			.HasComment("Tipo IVA denormalizzato dal codice IVA e congelato sulla riga al momento dell'emissione.");

		builder.Property(x => x.VatRate)
			.HasColumnName("vat_rate")
			.HasColumnType("numeric(5,2)")
			.IsRequired()
			.HasComment("Aliquota percentuale applicata (0 per le esenzioni) — dato proprio del documento, non uno snapshot.");

		builder.Property(x => x.VatAmount)
			.HasColumnName("vat_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("IVA della riga.");

		builder.Property(x => x.LineGross)
			.HasColumnName("line_gross")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Totale riga IVA inclusa (quantity × unit_price_gross), materializzato per i riepiloghi.");

		builder.Property(x => x.LineNet)
			.HasColumnName("line_net")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Imponibile riga, materializzato per i riepiloghi.");

		builder.Property(x => x.PaidAllocation)
			.HasColumnName("paid_allocation")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Quota di incasso che questo scontrino ha attribuito a questa voce.");

		// created_at/updated_at/deleted_at — plumbing comune; deleted_at di fatto sempre NULL (append-only con la testata)
	}
}
```

`ReceiptLine` implementa `IAuditable`; `deleted_at` esposto per uniformità ma di fatto mai valorizzato
(append-only insieme alla testata, retention fiscale 10 anni).

### Note di migrazione

- `JSON.parse(entries)` → una riga per elemento; `unit_price.incl_vat/excl_vat` e `vat.rate`/`vat.amount`
  (stringhe) → colonne `numeric`.
- `vat.code` → `vat_code_id` via lookup su `fiskaly_vat_codes.code`.
- `entries[].id` (`lineItemId`) → `source_line_public_id` via mappa C1; voci sintetiche
  (`surcharge_fixed`/`surcharge_percentage`) o non risolvibili → `NULL` (identificate comunque da
  `line_no` + `line_kind` + `description_snapshot`).
- `paid_deltas.items[]` allineato **in memoria durante la migrazione** per `id` alla riga `entries`
  corrispondente → popola `paid_allocation`; l'id grezzo legacy non si persiste su questa tabella.
