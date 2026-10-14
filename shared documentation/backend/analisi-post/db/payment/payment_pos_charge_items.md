# Tabella `payment_pos_charge_items`

> **Dominio**: C3 — `payment` · **sottoarea**: `pos` · **schema**: `payment` · **nome flat**:
> `payment_pos_charge_items` · **forma canonica**: `payment.pos_charge_items`.
>
> Figlia di [`payment_pos_charges`](payment_pos_charges.md). Sostituisce l'array
> [`teamsystem_expenses.payload.data[]`](../../../analisi-pre/db/teamsystem_expenses.md) (`{qnt, name,
> price, vat}`) e i `saleItems[]` dei documenti OfSale in
> [`booking_number_informations.ofsale_payments`](../../../analisi-pre/db/booking_number_informations.md)
> (`{id, description, quantity, unitPrice, vatRate}`). Fonti:
> [`analisi-pre/db/teamsystem_expenses.md`](../../../analisi-pre/db/teamsystem_expenses.md),
> [`analisi-pre/db/booking_number_informations.md`](../../../analisi-pre/db/booking_number_informations.md),
> [`docs/03-gestione-dati.md` §2.5, §3.7](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga = un prodotto/riga di uno scontrino POS**: descrizione, quantità, prezzo, aliquota IVA,
totale riga. Dati **congelati** al momento della trasmissione dal POS (lo stabilimento non li modifica).
Serve al conto della prenotazione e al report IVA degli addebiti esterni.

Correzioni rispetto al legacy:

- **Array dentro JSON** (`payload.data` / `saleItems`) → righe tipizzate.
- **`vat` intero (`22`)** su TeamSystem vs `vatRate` `"22"` stringa su OfSale → `numeric(5,2)` uniforme.
- **`price` = "totale riga" su TeamSystem** vs `unitPrice` su OfSale → si normalizzano entrambi in
  `unit_price` + `line_total` espliciti.
- **Nome prodotto testo libero** → `description_snapshot` (congelato, regola C1-7) + FK opzionale
  `sale_item_public_id` per la mappatura SKU POS ↔ catalogo (assente nel legacy).


> ### ✅ Applicato nel backend (2026-09-21)
>
> Aggiunta la colonna **`property_id`** denormalizzata dalla testata ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §10),
> migration `AddDbOptimizationsPropertyIdAndGeneratedColumns`, con l'indice che la accompagna.
>
> Serve ai report IVA degli addebiti POS e al report unificato per articolo. Senza, ogni riga richiede un hop sulla testata per sapere a quale stabilimento appartiene — e le
> query che aggregano *per riga e per stabilimento* pagano quel join su tutto il volume.
>
> Riferimento **testuale** al `public_id` di `catalog.properties`, nessuna FK cross-schema: stesso
> criterio già applicato a `fiskaly_receipt_lines` e alle altre figlie denormalizzate.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_payment_pos_charge_items` | `id` | PK | — |
| `ix_..._charge` | `(charge_id)` | btree | righe di uno scontrino |
| `ix_..._sale_item` | `(sale_item_public_id) WHERE sale_item_public_id IS NOT NULL` | btree parziale | addebiti POS per articolo di catalogo (report unificato) |
| `ix_..._vat` | `(vat_rate)` | btree | report IVA degli addebiti POS per aliquota |
| `ix_..._property` | `(property_id, vat_rate)` | btree | report IVA per stabilimento senza join sulla testata |


> ### ✅ Applicato nel backend (2026-09-21)
>
> La colonna **`net_amount` e `vat_amount`** è ora `GENERATED ALWAYS AS (scorporo da line_total e vat_rate) STORED` ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §5.1),
> migration `AddDbOptimizationsPropertyIdAndGeneratedColumns`.
>
> **È un cambio di contratto, non solo di schema**: PostgreSQL *rifiuta* un `INSERT`/`UPDATE` che
> valorizzi esplicitamente la colonna. Il codice applicativo che oggi la scrive va rimosso, non
> adattato. La formula di `vat_amount` ripete lo scorporo invece di referenziare `net_amount`: PostgreSQL non ammette che una colonna generata ne referenzi un'altra generata. Verificato: lo storno di un addebito POS porta `status = 'voided'` e un documento di storno a parte, mai una modifica di questi importi.

## Campi

> ### ✅ Corretta il 2026-10-06 — la FK è su `charge_id`
>
> Fino al 2026-10-06 la tabella aveva una seconda colonna **`pos_charge_id`**, *shadow property* creata da EF
> per la relazione owned (`OwnsMany`) perché la configurazione non diceva che la chiave esterna era
> `charge_id`. Era lei a portare la FK reale verso la testata; `charge_id`, la colonna usata da codice ed ETL,
> non aveva vincolo. Stesso difetto già corretto il 2026-09-15 per
> [`payment_ledger_entry_lines`](payment_ledger_entry_lines.md), non esteso allora alle altre figlie owned.
>
> Migration `FixOwnedItemsForeignKeys` di `Module.Payment`: configurazione con
> `WithOwner().HasForeignKey(...)`, `pos_charge_id` eliminata, FK `ON DELETE CASCADE` spostata su `charge_id`. La
> migration verifica prima che le due colonne coincidano su ogni riga (si ferma altrimenti); il `Down`
> ripristina la colonna travasando i valori. Aggiornato anche il transform ETL, che scriveva entrambe.


### id
- **Legacy**: elemento di `payload.data[]` / `saleItems[]` (OfSale assegna un `id` uuid, TeamSystem no)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### charge_id
- **Legacy**: derivato (le righe stavano dentro il documento)
- **Descrizione**: scontrino POS a cui la riga appartiene.
- **Tipo**: `bigint`, `NOT NULL`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`payment_pos_charges.id`](payment_pos_charges.md), `ON DELETE CASCADE`.

### property_id
- **Legacy**: derivato dalla testata
- **Descrizione**: stabilimento, denormalizzato dalla testata per aggregare per riga e per stabilimento
  senza join ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §10).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno. (Fino al 2026-10-06 c'era `DEFAULT '00000000-…'::uuid`, il segnaposto che EF mette
  aggiungendo una colonna `NOT NULL` a una tabella piena: faceva passare in silenzio una riga senza
  stabilimento. Tolto dal consolidamento delle migration — [`docs/42-migration.md` §10](../../../progettazione/42-migration.md).)
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md),
  nessuna FK cross-schema; deve coincidere con quello della testata (garanzia applicativa).

### external_line_ref
- **Legacy**: `saleItems[].id` (uuid, solo OfSale)
- **Descrizione**: id della riga sul POS di origine, quando fornito.
- **Tipo**: `varchar(64)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### sale_item_public_id
- **Legacy**: nessuno (il POS manda solo il nome prodotto come testo libero, nessuna mappatura al catalogo)
- **Descrizione**: articolo di catalogo corrispondente al prodotto del POS, **quando mappabile** (SKU
  del POS ↔ `catalog_service_sale_items`). Assente nel legacy; predisposto per il report unificato
  bar/servizi/POS.
- **Tipo**: `uuid` (= `public_id` di `catalog_service_sale_items`).
- **Default**: `NULL`.
- **Nullabile**: Sì (prodotto POS non a catalogo / non mappato — caso prevalente all'avvio).
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_service_sale_items`](../catalog/catalog_service_sale_items.md).

### description_snapshot
- **Legacy**: `payload.data[].name` (`"Caffè"`) / `saleItems[].description` (`"Birra media"`)
- **Descrizione**: descrizione del prodotto, **congelata** al momento della trasmissione dal POS —
  regola `_snapshot`: nome che (quando `sale_item_public_id` è valorizzato) duplica un nome vivo di C2;
  quando non lo è, resta comunque la forma di uno scontrino congelato ([`_feature-tabelle.md` → C1-7](../_feature-tabelle.md)).
- **Tipo**: `varchar(160)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: snapshot; il POS non modifica retroattivamente le righe trasmesse.

### quantity
- **Legacy**: `payload.data[].qnt` / `saleItems[].quantity` (`> 0`)
- **Descrizione**: quantità.
- **Tipo**: `integer`.
- **Default**: `1`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (quantity > 0)`.

### unit_price
- **Legacy**: `saleItems[].unitPrice` (OfSale) / derivato da `payload.data[].price / qnt` (TeamSystem, dove `price` è il totale riga)
- **Descrizione**: prezzo unitario, IVA inclusa.
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### line_total
- **Legacy**: `payload.data[].price` (TeamSystem: totale riga) / `quantity * unitPrice` (OfSale)
- **Descrizione**: totale della riga, IVA inclusa.
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `Σ(line_total)` = `payment_pos_charges.total_amount` entro 1 cent (vincolo in
  scrittura).

### vat_rate
- **Legacy**: `payload.data[].vat` (intero, `22`) / `saleItems[].vatRate` (`"10"`)
- **Descrizione**: aliquota IVA % della riga.
- **Tipo**: `numeric(5,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: da riconciliare col vocabolario aliquote di C4.

### net_amount / vat_amount
- **Legacy**: non presenti (il POS manda solo il lordo)
- **Descrizione**: scorporo IVA (`line_total / (1 + vat_rate/100)` e differenza) — evita di
  ricalcolarlo a ogni report.
- **Tipo**: `numeric(12,2)` ciascuno, **colonne generate** (riquadro in «Indici»):
  `net_amount GENERATED ALWAYS AS (round(line_total / (1 + vat_rate / 100), 2)) STORED`,
  `vat_amount GENERATED ALWAYS AS (line_total - round(line_total / (1 + vat_rate / 100), 2)) STORED`.
- **Default**: — (colonne generate).
- **Nullabile**: No / No.
- **Relazioni**: `net_amount + vat_amount = line_total`.

### created_at / updated_at / deleted_at
- **Legacy**: da `teamsystem_expenses.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica. `deleted_at` per uniformità: append-only con lo scontrino.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at` (sempre `NULL`).

## Valutazione JSON

Array JSON (`payload.data`, `saleItems`) → righe. Nessun JSON residuo.

## Suggerimenti per le prestazioni

- Volume nullo oggi, basso a regime. Solo indici su `charge_id` e `vat_rate`.
- Lo scorporo IVA materializzato (`net_amount`/`vat_amount`) evita l'aritmetica ripetuta nei report.

## Implementazione .NET

`PosChargeItem` è un **owned type** (collezione owned `Items`) dell'aggregate root `PosCharge`
([`payment_pos_charges`](payment_pos_charges.md)), area `Pos` in `Module.Payment`, schema `payment`:
nessuna identità pubblica, nessun repository proprio, dati congelati al momento della trasmissione dal
POS. Popolata da `IPosChargeMapper` (implementazioni `OfSaleMapper` / `TeamSystemMapper`) che normalizza
le due forme sorgente (`price` totale vs unitario; `vat` intero vs stringa) nel modello comune. Nessun
enum proprio.

### `PosChargeItemConfiguration`

Applicata dentro `PosChargeConfiguration.OwnsMany`:
`builder.OwnsMany(x => x.Items, items => PosChargeItemConfiguration.Configure(items))`, con
`builder.WithOwner().HasForeignKey(x => x.ChargeId)` nella configurazione della figlia (senza, EF crea
una colonna FK propria: riquadro in «Campi»).

```csharp
namespace Module.Payment.Infrastructure.Configurations;

public sealed class PosChargeItemConfiguration : IEntityTypeConfiguration<PosChargeItem>
{
	public void Configure(EntityTypeBuilder<PosChargeItem> builder)
	{
		builder.ToTable("pos_charge_items", "payment", t =>
		{
			t.HasComment("Prodotto/riga di uno scontrino POS: descrizione, quantità, prezzo, aliquota IVA, totale, congelati al momento della trasmissione.");
			t.HasCheckConstraint("ck_payment_pos_charge_items_quantity", "quantity > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_payment_pos_charge_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ChargeId)
			.HasColumnName("charge_id")
			.IsRequired()
			.HasComment("Scontrino POS a cui la riga appartiene.");

		builder.Property(x => x.ExternalLineRef)
			.HasColumnName("external_line_ref")
			.HasMaxLength(64)
			.HasComment("Id della riga sul POS di origine, quando fornito (solo OfSale).");

		builder.Property(x => x.SaleItemPublicId)
			.HasColumnName("sale_item_public_id")
			.HasComment("public_id di catalog.service_sale_items, copiato — nessuna FK cross-schema; articolo di catalogo corrispondente, quando mappabile.");

		builder.Property(x => x.DescriptionSnapshot)
			.HasColumnName("description_snapshot")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Descrizione del prodotto congelata al momento della trasmissione dal POS; il POS non modifica retroattivamente le righe trasmesse.");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasDefaultValue(1)
			.IsRequired()
			.HasComment("Quantità.");

		builder.Property(x => x.UnitPrice)
			.HasColumnName("unit_price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Prezzo unitario, IVA inclusa.");

		builder.Property(x => x.LineTotal)
			.HasColumnName("line_total")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Totale della riga, IVA inclusa; la somma delle righe deve coincidere con il totale scontrino entro 1 cent.");

		builder.Property(x => x.VatRate)
			.HasColumnName("vat_rate")
			.HasColumnType("numeric(5,2)")
			.IsRequired()
			.HasComment("Aliquota IVA % della riga.");

		builder.Property(x => x.NetAmount)
			.HasColumnName("net_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Imponibile scorporato in acquisizione (line_total / (1 + vat_rate/100)).");

		builder.Property(x => x.VatAmount)
			.HasColumnName("vat_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Quota IVA scorporata in acquisizione (line_total - net_amount).");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: append-only con lo scontrino, sempre NULL.");

		builder.HasIndex(x => x.ChargeId)
			.HasDatabaseName("ix_payment_pos_charge_items_charge");

		builder.HasIndex(x => x.SaleItemPublicId)
			.HasFilter("sale_item_public_id IS NOT NULL")
			.HasDatabaseName("ix_payment_pos_charge_items_sale_item");

		builder.HasIndex(x => x.VatRate)
			.HasDatabaseName("ix_payment_pos_charge_items_vat");
	}
}
```

### Relazioni

`charge_id` è la chiave dell'owned collection verso `PosCharge` (stesso schema `payment`, gestita da EF
tramite `OwnsMany`). `sale_item_public_id` è un `uuid` copiato da C2
[`catalog_service_sale_items`](../catalog/catalog_service_sale_items.md): solo `.HasComment`, nessuna FK
cross-schema.

Nessun `ISoftDeletable`/`IAuditable` dedicato: owned type, segue il ciclo di vita dello scontrino;
`deleted_at` resta per uniformità di schema ma sempre `NULL`.

### Note di migrazione

- Nessun dato da migrare: array sorgente `NULL`/tabella vuota nel legacy (`teamsystem_expenses` 0 righe;
  `ofsale_payments`/`teamsystem_payment` sempre `NULL` in produzione).
- Se in futuro arrivassero dati reali: `payload.data[].vat` (intero) e `saleItems[].vatRate` (stringa) →
  `numeric(5,2)` uniforme; `payload.data[].price` (totale riga su TeamSystem) vs `saleItems[].unitPrice`
  (OfSale) → normalizzati esplicitamente in `unit_price` + `line_total` da `IPosChargeMapper`, mai
  confusi come nel legacy.
