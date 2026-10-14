# Tabella `payment_ledger_entry_lines`

> **Dominio**: C3 — `payment` · **sottoarea**: `ledger` · **schema**: `payment` · **nome flat**:
> `payment_ledger_entry_lines` · **forma canonica**: `payment.ledger_entry_lines`.
>
> Figlia di [`payment_ledger_entries`](payment_ledger_entries.md). Sostituisce i campi **per
> riga-articolo** di [`transactions_payments`](../../../analisi-pre/db/transactions_payments.md)
> (`itemName`, `itemQuantity`, `itemNetAmount`, `itemVatAmount`). Fonti:
> [`analisi-pre/db/transactions_payments.md`](../../../analisi-pre/db/transactions_payments.md),
> [`docs/03-gestione-dati.md` §3.7, §4](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga = la quota di un movimento di pagamento attribuita a un articolo** (pacchetto, articolo
aggiuntivo, servizio, voucher). Serve all'**export contabile verso gestionali di terze parti** (l'API
`transactionApi` del legacy) e ai report IVA per riga. Popolata **solo per gli stabilimenti in
allow-list** (nel legacy la variabile `API_TRANSACTIONS_PROPERTYID_CSV`); per gli altri il movimento
resta a livello di sola testata ([`payment_ledger_entries`](payment_ledger_entries.md)).

Correzioni rispetto al legacy:

- **Full-replace per prenotazione** (`destroy` + `bulkCreate`) → le righe seguono il ciclo append-only
  della testata (`entry_id`); una rettifica è una nuova `payment_ledger_entries` `adjustment` con le
  proprie righe.
- **IVA cablata 22%** per i pacchetti (`DEFAULT_VAT_PERCENTAGE`) → `vat_rate` esplicito per riga.
- **`itemName` stringa libera** (`"<PackageName> (<Location>)"`, refusi) → `description_snapshot` (nome
  congelato, regola C1-7) + FK opzionale `reservation_line_public_id` all'anagrafica C1 quando risolvibile.
- **Ripartizione proporzionale** = stima, non movimento reale → resa esplicita da `is_allocated`
  ([D-35](../_domande-committente.md)).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_payment_ledger_entry_lines` | `id` | PK | — |
| `ix_..._entry` | `(entry_id)` | btree | righe di un movimento |
| `ix_..._reservation_line` | `(reservation_line_public_id) WHERE reservation_line_public_id IS NOT NULL` | btree parziale | correlazione con la riga prenotazione C1 |
| `ix_..._vat` | `(vat_rate)` | btree | report IVA per aliquota |
| `ix_..._property` | `(property_id, vat_rate)` | btree | report IVA per stabilimento senza join sulla testata |


> ### ✅ Applicato nel backend (2026-09-21)
>
> Aggiunta la colonna **`property_id`** denormalizzata dalla testata ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §10),
> migration `AddDbOptimizationsPropertyIdAndGeneratedColumns`, con l'indice che la accompagna.
>
> Serve all'export contabile e ai report IVA per riga. Senza, ogni riga richiede un hop sulla testata per sapere a quale stabilimento appartiene — e le
> query che aggregano *per riga e per stabilimento* pagano quel join su tutto il volume.
>
> Riferimento **testuale** al `public_id` di `catalog.properties`, nessuna FK cross-schema: stesso
> criterio già applicato a `fiskaly_receipt_lines` e alle altre figlie denormalizzate.

## Campi

### id
- **Legacy**: `transactions_payments.id` (una riga legacy = un movimento **già** per articolo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### entry_id
- **Legacy**: derivato (il legacy non separava testata e riga)
- **Descrizione**: movimento di pagamento a cui la riga appartiene.
- **Tipo**: `bigint`, `NOT NULL`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`payment_ledger_entries.id`](payment_ledger_entries.md) (`ON DELETE
  CASCADE` logica; entrambe append-only).

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

### line_kind
- **Legacy**: `transactions_payments.itemName` prefissi + tipo desunto
- **Descrizione**: tipo di articolo a cui è attribuita la quota.
- **Tipo**: `enum payment.ledger_line_kind` — valori: `package`, `additional_item`, `service`,
  `agreement_voucher`, `surcharge`, `discount`, `unspecified` (ex `"Deposit"`/`"Payment"` quando gli
  articoli non erano estraibili).
- **Default**: `unspecified`.
- **Nullabile**: No.
- **Relazioni**: allineato agli enum riga di C1 / al `LineItemType` di C4 in fase implementativa.

### description_snapshot
- **Legacy**: `transactions_payments.itemName` (`varchar(255)`)
- **Descrizione**: descrizione dell'articolo (pacchetto/servizio/voucher), **congelata** al momento del
  movimento — regola `_snapshot`: nome copiato da un'entità mutabile (il listino/l'anagrafica C2, la
  riga C1) con la FK viva `reservation_line_public_id` accanto ([`_feature-tabelle.md` → C1-7](../_feature-tabelle.md)).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: snapshot; refusi/`\t` normalizzati in migrazione. Se `reservation_line_public_id` è
  valorizzato, il nome "vivo" si legge da C1 [`booking_lines`](../booking/booking_lines.md).

### reservation_line_public_id
- **Legacy**: nessuno (correlazione assente)
- **Descrizione**: riga prenotazione C1 a cui la quota si riferisce, quando risolvibile.
- **Tipo**: `uuid` (= `public_id` di `booking_lines`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C1 [`booking_lines`](../booking/booking_lines.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 2.540 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### quantity
- **Legacy**: `transactions_payments.itemQuantity` (`int`, default `1`)
- **Descrizione**: quantità dell'articolo (pacchetti `1`; servizi `count`).
- **Tipo**: `integer`.
- **Default**: `1`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (quantity > 0)`.

### net_amount
- **Legacy**: `transactions_payments.itemNetAmount` (`decimal(10,2)`, default `0.00`)
- **Descrizione**: imponibile della quota di movimento attribuita alla riga.
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: `Σ(net_amount)` delle righe = `payment_ledger_entries.net_amount` della testata.

### vat_amount
- **Legacy**: `transactions_payments.itemVatAmount` (`decimal(10,2)`, default `0.00`)
- **Descrizione**: quota IVA della riga.
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: `Σ(vat_amount)` = testata.

### vat_rate
- **Legacy**: `percentIVA` dal JSON / `22` cablato per i pacchetti
- **Descrizione**: aliquota IVA % della riga.
- **Tipo**: `numeric(5,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: da riconciliare col vocabolario aliquote C4.

### is_allocated
- **Legacy**: implicito (ogni movimento spalmato proporzionalmente su tutte le righe)
- **Descrizione**: `true` = la quota è una **ripartizione proporzionale** del movimento (stima), non un
  importo pagato distintamente per quell'articolo.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: [D-35](../_domande-committente.md).

### created_at / updated_at / deleted_at
- **Legacy**: `transactions_payments.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica. `deleted_at` per uniformità: append-only con la testata.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at` (sempre `NULL`).

## Valutazione JSON

Nessun JSON. È il livello più fine del ledger: già relazionale nel prototipo `transactions_payments`.

## Suggerimenti per le prestazioni

- Popolata solo per le property in allow-list → volume contenuto. Se l'allow-list si allarga,
  partizionare insieme alla testata.
- Query tipiche = "righe del movimento X" (`entry_id`) e "totali IVA per aliquota su intervallo"
  (join con la testata per `occurred_at` + `vat_rate`): entrambe su indice.
- Non indicizzare `description_snapshot` (testo libero congelato, non filtrato).

## Implementazione .NET

`LedgerEntryLine` è un **owned type** (collezione owned `Lines`) dell'aggregate root `LedgerEntry`
([`payment_ledger_entries`](payment_ledger_entries.md)), area `Ledger` in `Module.Payment`, schema
`payment`: nessun repository proprio, nessuna identità pubblica, segue append-only la testata. Popolata
**solo** per gli stabilimenti in allow-list (`PaymentExport:PropertyIds` in `appsettings`, non più CSV in
env come nel legacy `API_TRANSACTIONS_PROPERTYID_CSV`); letta da `ITransactionExportService` (ex
`transactionApiService`) per l'export contabile verso gestionali terzi.

### Enum propri

```csharp
// Module.Payment.Domain.Enums.LedgerLineKind
public enum LedgerLineKind
{
	Package = 0,
	AdditionalItem = 1,
	Service = 2,
	AgreementVoucher = 3,
	Surcharge = 4,
	Discount = 5,
	Unspecified = 6
}
```

```csharp
// Module.Payment.Infrastructure.PaymentDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<LedgerLineKind>("payment", "ledger_line_kind");
```

```csharp
// Module.Payment.Infrastructure DI (Npgsql data source)
dataSourceBuilder.MapEnum<LedgerLineKind>("payment.ledger_line_kind");
```

### `LedgerEntryLineConfiguration`

Applicata dentro `LedgerEntryConfiguration.OwnsMany` (vedi [`payment_ledger_entries.md`](payment_ledger_entries.md)):
`builder.OwnsMany(x => x.Lines, lines => lines.ApplyConfiguration(new LedgerEntryLineConfiguration()))`.

```csharp
namespace Module.Payment.Infrastructure.Configurations;

public sealed class LedgerEntryLineConfiguration : IEntityTypeConfiguration<LedgerEntryLine>
{
	public void Configure(EntityTypeBuilder<LedgerEntryLine> builder)
	{
		builder.ToTable("ledger_entry_lines", "payment", t => t.HasComment(
			"Quota di un movimento di pagamento attribuita a un articolo (pacchetto, servizio, voucher, ...), per l'export contabile e i report IVA per riga."));

		builder.HasKey(x => x.Id).HasName("pk_payment_ledger_entry_lines");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.EntryId)
			.HasColumnName("entry_id")
			.IsRequired()
			.HasComment("Movimento di pagamento a cui la riga appartiene.");

		builder.Property(x => x.LineKind)
			.HasColumnName("line_kind")
			.HasColumnType("payment.ledger_line_kind")
			.HasDefaultValue(LedgerLineKind.Unspecified)
			.IsRequired()
			.HasComment("Tipo di articolo a cui è attribuita la quota.");

		builder.Property(x => x.DescriptionSnapshot)
			.HasColumnName("description_snapshot")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Descrizione dell'articolo congelata al momento del movimento; il nome vivo si legge da C1 quando reservation_line_public_id è valorizzato.");

		builder.Property(x => x.ReservationLinePublicId)
			.HasColumnName("reservation_line_public_id")
			.HasComment("public_id di booking.booking_lines, copiato — nessuna FK cross-schema; riga prenotazione a cui la quota si riferisce, quando risolvibile.");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasDefaultValue(1)
			.IsRequired()
			.HasComment("Quantità dell'articolo (pacchetti 1; servizi count).");

		builder.Property(x => x.NetAmount)
			.HasColumnName("net_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Imponibile della quota di movimento attribuita alla riga.");

		builder.Property(x => x.VatAmount)
			.HasColumnName("vat_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Quota IVA della riga.");

		builder.Property(x => x.VatRate)
			.HasColumnName("vat_rate")
			.HasColumnType("numeric(5,2)")
			.IsRequired()
			.HasComment("Aliquota IVA % della riga.");

		builder.Property(x => x.IsAllocated)
			.HasColumnName("is_allocated")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("True = la quota è una ripartizione proporzionale del movimento (stima), non un importo pagato distintamente per l'articolo.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: append-only con la testata, sempre NULL.");

		builder.HasIndex(x => x.EntryId)
			.HasDatabaseName("ix_payment_ledger_entry_lines_entry");

		builder.HasIndex(x => x.ReservationLinePublicId)
			.HasFilter("reservation_line_public_id IS NOT NULL")
			.HasDatabaseName("ix_payment_ledger_entry_lines_reservation_line");

		builder.HasIndex(x => x.VatRate)
			.HasDatabaseName("ix_payment_ledger_entry_lines_vat");
	}
}
```

### Relazioni

`entry_id` è la chiave dell'owned collection verso `LedgerEntry` (stesso schema `payment`, gestita da EF
tramite `OwnsMany`, non una FK EF navigabile indipendente). `reservation_line_public_id` è un `uuid`
copiato da C1 [`booking_lines`](../booking/booking_lines.md): solo `.HasComment`, nessuna FK
cross-schema.

Nessun `ISoftDeletable`/`IAuditable` dedicato: essendo owned type, segue il ciclo di vita (e i timestamp)
della testata `LedgerEntry`; `deleted_at` resta per uniformità di schema ma sempre `NULL`.

### Note di migrazione

- `transactions_payments` (già relazionale, una riga legacy = un movimento già per articolo) → 1
  `LedgerEntry` per `(bookingId, transactionDate, type)` con N `Lines` (una per `itemName` distinto).
- `itemName` normalizzato (refusi/`\t`) → `description_snapshot`; se riconducibile a una riga C1, valorizza
  anche `reservation_line_public_id`.
- `is_allocated = true` per tutte le righe migrate (la ripartizione legacy era sempre proporzionale, mai
  un importo pagato distintamente per articolo).
- IVA reale quando presente nel JSON sorgente; `22` cablato altrimenti, mantenendo `is_allocated = true`
  come segnale di stima anche sull'aliquota.
