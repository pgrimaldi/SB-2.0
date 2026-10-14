# Tabella `fiskaly_receipt_payments`

> **Dominio**: C4 — `fiskaly` · **sottoarea**: — · **schema**: `fiskaly` · **nome flat**:
> `fiskaly_receipt_payments` · **forma canonica**: `fiskaly.receipt_payments`.
>
> Figlia di [`fiskaly_receipts`](fiskaly_receipts.md). Sostituisce l'array JSON
> [`fiskaly_receipt.payments`](../../../analisi-pre/db/fiskaly_receipt.md) /
> [`fiskaly_non_fiscal_receipt.payments`](../../../analisi-pre/db/fiskaly_non_fiscal_receipt.md)
> (strutture **diverse** fra le due: `{ type, details:{amount,discount}, name, kind, number, concept }`
> vs `{ type?, amount, discount }` — con drift). Fonti:
> [`analisi-pre/db/fiskaly_receipt.md`](../../../analisi-pre/db/fiskaly_receipt.md),
> [`docs/03-gestione-dati.md` §2.4ter, §2.5, §3.7](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga = una modalità di pagamento con cui è stato incassato l'importo del documento** (uno
scontrino può essere saldato con più forme: contanti + carta, ecc. → più righe). È la sezione
"pagamenti" del documento commerciale, **congelata** all'emissione (§3.6 punto 2).

Correzioni rispetto al legacy:

- **Due strutture JSON diverse** (fiscale con `details:{}`, non-fiscale "piatta") → una sola tabella;
  il drift su `type` (assente, `"BANK TRANSFER"` verbatim, `"CASH"`) → normalizzato in migrazione
  (`mapToFiskalyPaymentType` è già la logica), con l'etichetta libera che finisce in `label`.
- **Numeri come stringa** (`amount` `"75.00"`, `discount` `"0.00"`) → `numeric`
  ([`docs/03 §3.7`](../../../progettazione/03-gestione-dati.md)).
- **Vocabolari come stringhe libere** (`type`, `kind`, `concept`) → `enum` PG
  ([`docs/03 §2.4ter`](../../../progettazione/03-gestione-dati.md)).
- **Nessun collegamento al movimento contabile** → `ledger_entry_public_id` (C3, per valore).

## Verifica di esclusività

| Gruppo | Campi | Esito |
|---|---|---|
| Forma di pagamento | `payment_type` | **enum** esclusivo per riga: `cash` / `card` / `online` / `other` / `voucher` / `outstanding`. Più forme sullo stesso scontrino = **più righe** (non un `enum[]`). |
| Tipo carta | `card_kind` | **enum** esclusivo, valorizzato **solo** se `payment_type = 'card'`: `credit` / `debit`. |
| Tipo voucher | `voucher_kind` | **enum**, solo se `payment_type = 'voucher'`: `single`. |
| Concetto insoluto | `outstanding_concept` | **enum**, solo se `payment_type = 'outstanding'`: `service`. |

## Snapshot

Nessun suffisso `_snapshot`: importi ed etichette sono **dati propri del documento** fiscale (§3.6,
C1-7). `label` è testo libero congelato (come `variants_description` in C3), non copia di un'entità con
FK viva.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_fiskaly_receipt_payments` | `id` | PK | — |
| `ix_fiskaly_receipt_payments_receipt` | `(receipt_id, payment_no)` | btree | righe pagamento di uno scontrino in ordine |
| `ix_fiskaly_receipt_payments_ledger` | `(ledger_entry_public_id) WHERE ledger_entry_public_id IS NOT NULL` | btree parziale | correlazione con il movimento contabile C3 |
| `ix_fiskaly_receipt_payments_type` | `(property_id, payment_type)` | btree | riepilogo incassi per forma di pagamento |

## Campi

### id
- **Legacy**: elemento dell'array `payments` (senza id)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### receipt_id
- **Legacy**: `fiskaly_receipt.id`
- **Descrizione**: documento a cui la riga pagamento appartiene.
- **Tipo**: `bigint`, `NOT NULL`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`fiskaly_receipts.id`](fiskaly_receipts.md), `ON DELETE CASCADE`.

### property_id
- **Legacy**: — (nuovo; da `fiskaly_receipts.property_id`)
- **Descrizione**: stabilimento (denormalizzato per i riepiloghi senza join).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2.

### payment_no
- **Legacy**: posizione nell'array `payments`
- **Descrizione**: ordine della riga pagamento (1-based).
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE (receipt_id, payment_no)`.

### payment_type
- **Legacy**: `payments[].type` (`CASH` / `CARD` / `ONLINE` / `OTHER` / `VOUCHER` / `OUTSTANDING`;
  drift: assente, `"BANK TRANSFER"` → `ONLINE`)
- **Descrizione**: forma di pagamento normalizzata a un tipo valido Fiskaly.
- **Tipo**: `enum fiskaly.payment_type` — valori: `cash`, `card`, `online`, `other`, `voucher`,
  `outstanding`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `FiskalyPaymentType` ([`docs/03 §2.4ter`](../../../progettazione/03-gestione-dati.md)); negli
  auto-scontrini sempre `online`.

### amount
- **Legacy**: `payments[].details.amount` / `payments[].amount` (`string`)
- **Descrizione**: importo **lordo** incassato con questa forma di pagamento.
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `Σ (amount − discount_amount)` coerente con `fiskaly_receipts.total_amount`.

### discount_amount
- **Legacy**: `payments[].details.discount` / `payments[].discount` (`string`, spesso `"0.00"`)
- **Descrizione**: sconto applicato su questa forma di pagamento.
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### label
- **Legacy**: `payments[].name` (es. `"Pagamento e-commerce"`, `"Bonifico bancario"`) — o il valore
  di `type` non mappato (`"BANK TRANSFER"`)
- **Descrizione**: etichetta descrittiva libera per `online` / `other` (e per conservare il valore
  originale del drift). Testo congelato.
- **Tipo**: `varchar(120)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### card_kind
- **Legacy**: `payments[].kind` (per `CARD`: `"CREDIT"` / `"DEBIT"`)
- **Descrizione**: tipo carta.
- **Tipo**: `enum fiskaly.card_kind` — valori: `credit`, `debit`.
- **Default**: `NULL`.
- **Nullabile**: Sì (valorizzato solo se `payment_type = 'card'`).
- **Relazioni**: `CHECK (payment_type = 'card' OR card_kind IS NULL)`.

### card_number_masked
- **Legacy**: `payments[].number` (mascherato `"****"`)
- **Descrizione**: ultime cifre / maschera della carta. **Mai PAN completo**.
- **Tipo**: `varchar(20)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### voucher_kind
- **Legacy**: `payments[].kind` (per `VOUCHER`: `"SINGLE"`)
- **Descrizione**: tipo voucher.
- **Tipo**: `enum fiskaly.voucher_kind` — valori: `single`.
- **Default**: `NULL`.
- **Nullabile**: Sì (solo se `payment_type = 'voucher'`).
- **Relazioni**: `CHECK (payment_type = 'voucher' OR voucher_kind IS NULL)`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 2.549 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### outstanding_concept
- **Legacy**: `payments[].concept` (per `OUTSTANDING`: `"SERVICE"`)
- **Descrizione**: concetto dell'insoluto.
- **Tipo**: `enum fiskaly.outstanding_concept` — valori: `service`.
- **Default**: `NULL`.
- **Nullabile**: Sì (solo se `payment_type = 'outstanding'`).
- **Relazioni**: `CHECK (payment_type = 'outstanding' OR outstanding_concept IS NULL)`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 2.549 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### ledger_entry_public_id
- **Legacy**: nessuno (il legacy non correlava la riga pagamento fiscale al movimento contabile)
- **Descrizione**: movimento di incasso corrispondente nel libro incassi C3.
- **Tipo**: `uuid` (= `public_id` di `payment_ledger_entries`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C3
  [`payment_ledger_entries`](../payment/payment_ledger_entries.md); nessuna FK cross-modulo.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 2.549 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: da `fiskaly_receipt.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica. `deleted_at` esposto per uniformità: append-only con la
  testata.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF (sempre `NULL`).

## Valutazione JSON

`payments` (array, 2 strutture diverse) → questa tabella. `details` (oggetto annidato del fiscale)
esploso in `amount` + `discount_amount`. Nessun JSON residuo.

## Suggerimenti per le prestazioni

- 1–2 righe per scontrino nella quasi totalità dei casi. Indici minimi.
- `(property_id, payment_type)` per il report "incassato per forma di pagamento" (chiusura cassa).

## Implementazione .NET

`ReceiptPayment` è **owned entity** (owned collection `Payments`) dell'aggregato `Receipt`
([`fiskaly_receipts`](fiskaly_receipts.md)), modulo `Module.Fiskaly`, schema `fiskaly`: una modalità di
pagamento con cui è stato incassato l'importo del documento, congelata all'emissione.

### Enum propri

```csharp
// Domain/Enums/FiskalyPaymentType.cs
public enum FiskalyPaymentType { Cash = 0, Card = 1, Online = 2, Other = 3, Voucher = 4, Outstanding = 5 }

// Domain/Enums/CardKind.cs
public enum CardKind { Credit = 0, Debit = 1 }

// Domain/Enums/VoucherKind.cs
public enum VoucherKind { Single = 0 }

// Domain/Enums/OutstandingConcept.cs
public enum OutstandingConcept { Service = 0 }
```

```csharp
// FiskalyDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<FiskalyPaymentType>("fiskaly", "payment_type");
modelBuilder.HasPostgresEnum<CardKind>("fiskaly", "card_kind");
modelBuilder.HasPostgresEnum<VoucherKind>("fiskaly", "voucher_kind");
modelBuilder.HasPostgresEnum<OutstandingConcept>("fiskaly", "outstanding_concept");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<FiskalyPaymentType>("fiskaly.payment_type");
dataSourceBuilder.MapEnum<CardKind>("fiskaly.card_kind");
dataSourceBuilder.MapEnum<VoucherKind>("fiskaly.voucher_kind");
dataSourceBuilder.MapEnum<OutstandingConcept>("fiskaly.outstanding_concept");
```

Nota: non confondere `fiskaly.payment_type` (forma di pagamento della riga, questa tabella) con
`fiskaly.payment_mode` (già definito su [`fiskaly_receipts`](fiskaly_receipts.md) per
`invoiced_payment_mode`, vocabolario condiviso con `booking`/`payment`) — due vocabolari distinti con
valori diversi, non lo stesso enum.

### Configuration

```csharp
namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class ReceiptPaymentConfiguration : IEntityTypeConfiguration<ReceiptPayment>
{
	public void Configure(EntityTypeBuilder<ReceiptPayment> builder)
	{
		builder.ToTable("fiskaly_receipt_payments", "fiskaly", t =>
		{
			t.HasComment("Modalità di pagamento con cui è stato incassato l'importo del documento commerciale, congelata all'emissione.");
			t.HasCheckConstraint("ck_fiskaly_receipt_payments_card_kind", "payment_type = 'card' OR card_kind IS NULL");
			t.HasCheckConstraint("ck_fiskaly_receipt_payments_voucher_kind", "payment_type = 'voucher' OR voucher_kind IS NULL");
			t.HasCheckConstraint("ck_fiskaly_receipt_payments_outstanding_concept", "payment_type = 'outstanding' OR outstanding_concept IS NULL");
		});

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_receipt_payments");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReceiptId)
			.HasColumnName("receipt_id")
			.IsRequired()
			.HasComment("Documento a cui la riga pagamento appartiene.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per i riepiloghi senza join.");
		builder.HasIndex(x => new { x.PropertyId, x.PaymentType })
			.HasDatabaseName("ix_fiskaly_receipt_payments_type");

		builder.Property(x => x.PaymentNo)
			.HasColumnName("payment_no")
			.HasColumnType("smallint")
			.IsRequired()
			.HasComment("Ordine della riga pagamento nel documento (1-based).");
		builder.HasIndex(x => new { x.ReceiptId, x.PaymentNo })
			.IsUnique()
			.HasDatabaseName("ix_fiskaly_receipt_payments_receipt");

		builder.Property(x => x.PaymentType)
			.HasColumnName("payment_type")
			.HasColumnType("fiskaly.payment_type")
			.IsRequired()
			.HasComment("Forma di pagamento normalizzata a un tipo valido Fiskaly.");

		builder.Property(x => x.Amount)
			.HasColumnName("amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Importo lordo incassato con questa forma di pagamento.");

		builder.Property(x => x.DiscountAmount)
			.HasColumnName("discount_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Sconto applicato su questa forma di pagamento.");

		builder.Property(x => x.Label)
			.HasColumnName("label")
			.HasMaxLength(120)
			.HasComment("Etichetta descrittiva libera (es. per online/other) o valore originale del drift legacy, testo congelato.");

		builder.Property(x => x.CardKind)
			.HasColumnName("card_kind")
			.HasColumnType("fiskaly.card_kind")
			.HasComment("Tipo carta, valorizzato solo se payment_type = card.");

		builder.Property(x => x.CardNumberMasked)
			.HasColumnName("card_number_masked")
			.HasMaxLength(20)
			.HasComment("Ultime cifre / maschera della carta — mai il PAN completo.");

		builder.Property(x => x.VoucherKind)
			.HasColumnName("voucher_kind")
			.HasColumnType("fiskaly.voucher_kind")
			.HasComment("Tipo voucher, valorizzato solo se payment_type = voucher.");

		builder.Property(x => x.OutstandingConcept)
			.HasColumnName("outstanding_concept")
			.HasColumnType("fiskaly.outstanding_concept")
			.HasComment("Concetto dell'insoluto, valorizzato solo se payment_type = outstanding.");

		builder.Property(x => x.LedgerEntryPublicId)
			.HasColumnName("ledger_entry_public_id")
			.HasComment("public_id di payment.ledger_entries, copiato — nessuna FK cross-schema. Movimento di incasso corrispondente nel libro incassi C3.");
		builder.HasIndex(x => x.LedgerEntryPublicId)
			.HasFilter("ledger_entry_public_id IS NOT NULL")
			.HasDatabaseName("ix_fiskaly_receipt_payments_ledger");

		// created_at/updated_at/deleted_at — plumbing comune; deleted_at di fatto sempre NULL (append-only con la testata)
	}
}
```

`ReceiptPayment` implementa `IAuditable`; `deleted_at` esposto per uniformità ma di fatto mai
valorizzato (append-only insieme alla testata).

### Note di migrazione

- `JSON.parse(payments)` gestendo **entrambe** le forme legacy (fiscale con `details:{amount,discount}`,
  non-fiscale "piatta" con `amount`/`discount` diretti).
- `type` con drift (assente, `"BANK TRANSFER"` verbatim, ecc.) normalizzato tramite una funzione pura
  `NormalizeFiskalyPaymentType` (porta di `mapToFiskalyPaymentType` legacy, riusata sia a runtime sia in
  migrazione) → `payment_type` + valore originale conservato in `label` quando non mappabile 1:1.
