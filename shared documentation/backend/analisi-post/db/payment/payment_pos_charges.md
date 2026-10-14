# Tabella `payment_pos_charges`

> **Dominio**: C3 — `payment` · **sottoarea**: `pos` · **schema**: `payment` · **nome flat**:
> `payment_pos_charges` · **forma canonica**: `payment.pos_charges`.
>
> Sostituisce [`teamsystem_expenses`](../../../analisi-pre/db/teamsystem_expenses.md) (0 righe —
> integrazione TeamSystem/Cassa in Cloud mai attivata) e la **fetta C3** dei JSON
> [`booking_number_informations.ofsale_payments`](../../../analisi-pre/db/booking_number_informations.md)
> / `teamsystem_payment` (`NULL` su tutte le righe di produzione). Fonti:
> [`analisi-pre/db/teamsystem_expenses.md`](../../../analisi-pre/db/teamsystem_expenses.md),
> [`analisi-pre/db/booking_number_informations.md`](../../../analisi-pre/db/booking_number_informations.md),
> [`analisi-pre/db/keysecret.md`](../../../analisi-pre/db/keysecret.md),
> [`docs/03-gestione-dati.md` §2.4ter, §4](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md).

---

## Descrizione

**Una riga = uno scontrino/addebito trasmesso da un POS esterno** (app di cassa **OfSale** o cassa
**TeamSystem / Cassa in Cloud**) e associato a una prenotazione dello stabilimento. Porta gli addebiti
fatti in cassa dentro il conto della prenotazione (analogo al borsellino, ma da un sistema terzo). Il
dettaglio prodotti è nella figlia [`payment_pos_charge_items`](payment_pos_charge_items.md); l'eventuale
riga contabile è in [`payment_ledger_entries`](payment_ledger_entries.md) (`source = pos_ofsale` /
`pos_teamsystem`). La fiscalizzazione è **solo consultata** su C4, non gestita qui.

Correzioni rispetto al legacy:

- **`teamsystem_expenses.idRef` `bigint` a schema vs stringa UUID attesa dal codice** (probabile causa
  della tabella vuota) → `external_ref` `varchar` con `UNIQUE(pos_system, property_id, external_ref)`.
- **`payload` JSON** (`{app, description, amount, data[]}`) → colonne tipizzate + figlia righe.
- **`amount` ridondante** (colonna `decimal` + `payload.amount` + `Σ payload.data[].price`) → una sola
  colonna `total_amount`, la coerenza si verifica sulle righe.
- **`fiscalized` colonna morta** (mai portata a `1`; il valore vero è `isBookingFiscalized` live su
  `fiskaly_receipt`) → non migrata; lo stato fiscale si legge da C4 a evento/consultazione.
- **`registrationId` = id di una prenotazione "rappresentativa" del gruppo `bookingNumber`** (semantica
  fragile) → legame diretto al `reservation_public_id` (testata C1).
- **`payload.app` stringa fissa `"Cassa in Cloud"`** → `pos_system` enum.
- **`payload.data[].vat` intero (`22`) vs `"22.00"` altrove** → `numeric(5,2)` uniforme sulle righe.
- **`timestamps: false`, charset `latin1`** → `timestamptz` standard, UTF-8.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_payment_pos_charges` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | id restituito al POS come identificativo della spesa (idempotenza lato partner) |
| `uq_..._external_ref` | `(pos_system, property_id, external_ref)` | UNIQUE | **deduplicazione**: un retry del POS con lo stesso documento restituisce la riga originale |
| `ix_..._reservation` | `(reservation_public_id)` | btree | spese di una prenotazione (conto) |
| `ix_..._property_date` | `(property_id, charged_at DESC)` | btree | report addebiti POS per stabilimento |
| `ix_..._booking_number` | `(booking_number)` | btree | correlazione via numero prenotazione (legacy "specchio") |

## Campi

### id
- **Legacy**: `teamsystem_expenses.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da `payment_pos_charge_items.charge_id`.

### public_id
- **Legacy**: `teamsystem_expenses.id` (restituito al POS: `res.json({ id: result.id })`)
- **Descrizione**: id pubblico stabile restituito al sistema POS come identificativo della spesa
  registrata; su un retry con lo stesso `external_ref` si restituisce **lo stesso** valore
  (idempotenza).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### pos_system
- **Legacy**: `keysecret.appName` (`OfSale` / `TeamSystem`) / `payload.app` (`"Cassa in Cloud"`)
- **Descrizione**: sistema POS di origine. **Esclusivo** (una riga = un POS).
- **Tipo**: `enum payment.pos_system` — valori: `ofsale`, `teamsystem`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: determina `source` in `payment_ledger_entries` (`pos_ofsale` / `pos_teamsystem`).

### property_id
- **Legacy**: `teamsystem_expenses.beachId` (`int`, dal token OAuth `req.client.params.property_id`)
- **Descrizione**: stabilimento a cui la spesa è addebitata.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md); [D-01](../_domande-committente.md).

### reservation_public_id
- **Legacy**: `teamsystem_expenses.registrationId` (`int`, prenotazione "rappresentativa" del gruppo)
- **Descrizione**: prenotazione al cui conto la spesa confluisce.
- **Tipo**: `uuid` (= `public_id` di `booking_reservations`).
- **Default**: nessuno.
- **Nullabile**: Sì (spesa non ancora agganciata).
- **Relazioni**: **`public_id` (uuid) copiato** da C1 [`booking_reservations`](../booking/booking_reservations.md).

### booking_number
- **Legacy**: `teamsystem_expenses.bookingNumber` (`varchar(255)`; chiave dello "specchio" in `booking_number_informations`)
- **Descrizione**: numero prenotazione (denormalizzato, correlazione legacy).
- **Tipo**: `varchar(32)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: = `booking_reservations.booking_number`.

### external_ref
- **Legacy**: `teamsystem_expenses.idRef` (`bigint` a schema / stringa UUID nel codice) — chiave di dedup
- **Descrizione**: id del documento/scontrino sul POS di origine.
- **Tipo**: `varchar(64)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: parte dell'indice `UNIQUE(pos_system, property_id, external_ref)` — retry idempotente.

### description
- **Legacy**: `teamsystem_expenses.payload.description` (`"Bar Centrale"`) / OfSale charge
- **Descrizione**: nome del punto vendita / descrizione dello scontrino, come inviato dal POS.
  **Niente suffisso `_snapshot`**: non è la copia dell'etichetta di un'entità nostra (il "punto vendita"
  del POS esterno non è in anagrafica, nessuna FK viva) — è testo libero proprio del documento POS.
- **Tipo**: `varchar(160)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### total_amount
- **Legacy**: `teamsystem_expenses.amount` (`decimal(10,2)`) + `payload.amount` (ridondante)
- **Descrizione**: totale dello scontrino, IVA inclusa.
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `CHECK (total_amount > 0)`; deve coincidere con `Σ payment_pos_charge_items.line_total`
  entro 1 cent (nel legacy era solo un warning) — vincolo verificato in scrittura.

### currency
- **Legacy**: nessuno (implicito EUR)
- **Descrizione**: valuta ISO 4217.
- **Tipo**: `char(3)`.
- **Default**: `'EUR'`.
- **Nullabile**: No.
- **Relazioni**: —.

### status
- **Legacy**: nessuno (la riga era un fatto immutabile)
- **Descrizione**: stato dello scontrino POS nel gestionale.
- **Tipo**: `enum payment.pos_charge_status` — valori: `recorded` (acquisito), `voided` (annullato dal
  POS con un documento di storno).
- **Default**: `recorded`.
- **Nullabile**: No.
- **Relazioni**: `voided` → eventuale riga `payment_ledger_entries` di storno.

### ledger_entry_ref
- **Legacy**: nessuno
- **Descrizione**: `public_id` della riga contabile generata dall'addebito (quando concorre al
  conto/fiscale).
- **Tipo**: `uuid` (= `public_id` di `payment_ledger_entries`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: correlazione ledger ↔ POS.

### charged_at
- **Legacy**: `teamsystem_expenses.createdAt` (usato come `date` ISO dell'entry specchiata)
- **Descrizione**: data/ora dello scontrino sul POS.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: decorrenza retention: **24 mesi online**, poi archivio freddo, e **cancellazione
  dell'archivio solo a 10 anni** ([`docs/80-retention.md` §3.2](../../../progettazione/80-retention.md)).

  > **Aggiornamento 2026-09-21 (chiarimento Q4)** — la precedente indicazione *"cancella archivio a 5
  > anni"* era una contraddizione col fatto che queste spese **concorrono al conto del cliente** e
  > quindi al fiscale. Il titolare ha confermato l'obbligo pieno: **10 anni**, art. 2220 c.c.
  > ([`docs/82-retention-data.md` §3.2](../../../progettazione/82-retention-data.md)). Sono quindi **due** righe
  > di policy su questa tabella — archiviazione a 24 mesi, cancellazione a 10 anni — non una sola.

### created_at / updated_at / deleted_at
- **Legacy**: `teamsystem_expenses.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica. `deleted_at` esposto per uniformità: **append-only** (concorre
  al fiscale, retention 10 anni).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at` (sempre `NULL`).

## Valutazione JSON

`teamsystem_expenses.payload` (oggetto `{app, description, amount, data[]}`) e
`booking_number_informations.ofsale_payments` / `teamsystem_payment` (array di documenti POS) → colonne
tipizzate qui + righe in [`payment_pos_charge_items`](payment_pos_charge_items.md). Nessun `jsonb`
residuo (il payload grezzo, se serve per audit dell'integrazione, va in un log tecnico, non in questa
tabella di dominio).

## Suggerimenti per le prestazioni

- Volume oggi nullo (integrazioni non attive); previsto basso anche a regime. Indici mirati sulla dedup
  (`UNIQUE(pos_system, property_id, external_ref)`) e sulla correlazione prenotazione.
- L'idempotenza sul retry POS è a livello DB (unique), non applicativa: un `INSERT ... ON CONFLICT DO
  NOTHING` + `SELECT` restituisce l'id originale senza self-heal ("`ensureMirrored`" del legacy sparisce).

## Implementazione .NET

`PosCharge` è l'**aggregate root** dell'area `Pos` in `Module.Payment`, schema `payment`: 1 riga per
scontrino/addebito trasmesso da un POS esterno (OfSale o TeamSystem/Cassa in Cloud), con collezione owned
`Items` ([`payment_pos_charge_items`](payment_pos_charge_items.md)). Esposta a un endpoint partner dietro
un **gateway API sottile** (anti-corruption layer, [`architettura-evolutiva-microservizi.md` §"API esterne"](../../../progettazione/pre/architettura-evolutiva-microservizi.md));
autenticazione dei client partner via `api_clients` + secret manager (**non** la tabella `keysecret`, che
resta INFRA). Lo stato fiscale è **solo consultato** da C4 (consumer dell'evento `FiscalReceiptEmitted`),
non è una colonna scritta qui.

### Enum propri

```csharp
// Module.Payment.Domain.Enums.PosSystem
public enum PosSystem
{
	Ofsale = 0,
	Teamsystem = 1
}

// Module.Payment.Domain.Enums.PosChargeStatus
public enum PosChargeStatus
{
	Recorded = 0,
	Voided = 1
}
```

```csharp
// Module.Payment.Infrastructure.PaymentDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<PosSystem>("payment", "pos_system");
modelBuilder.HasPostgresEnum<PosChargeStatus>("payment", "pos_charge_status");
```

```csharp
// Module.Payment.Infrastructure DI (Npgsql data source)
dataSourceBuilder.MapEnum<PosSystem>("payment.pos_system");
dataSourceBuilder.MapEnum<PosChargeStatus>("payment.pos_charge_status");
```

### `PosChargeConfiguration`

```csharp
namespace Module.Payment.Infrastructure.Configurations;

public sealed class PosChargeConfiguration : IEntityTypeConfiguration<PosCharge>
{
	public void Configure(EntityTypeBuilder<PosCharge> builder)
	{
		builder.ToTable("pos_charges", "payment", t =>
		{
			t.HasComment("Scontrino/addebito trasmesso da un POS esterno (OfSale o TeamSystem) e associato a una prenotazione dello stabilimento.");
			t.HasCheckConstraint("ck_payment_pos_charges_total_amount", "total_amount > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_payment_pos_charges");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile restituito al sistema POS come identificativo della spesa; su un retry con lo stesso external_ref si restituisce lo stesso valore (idempotenza).");

		builder.Property(x => x.PosSystem)
			.HasColumnName("pos_system")
			.HasColumnType("payment.pos_system")
			.IsRequired()
			.HasComment("Sistema POS di origine, esclusivo per riga.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento a cui la spesa è addebitata.");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; prenotazione al cui conto la spesa confluisce.");

		builder.Property(x => x.BookingNumber)
			.HasColumnName("booking_number")
			.HasMaxLength(32)
			.HasComment("Numero prenotazione, denormalizzato per correlazione col legacy.");

		builder.Property(x => x.ExternalRef)
			.HasColumnName("external_ref")
			.HasMaxLength(64)
			.IsRequired()
			.HasComment("Id del documento/scontrino sul POS di origine, chiave di deduplicazione.");

		builder.Property(x => x.Description)
			.HasColumnName("description")
			.HasMaxLength(160)
			.HasComment("Nome del punto vendita / descrizione dello scontrino, testo libero come inviato dal POS (nessuna entità nostra corrispondente).");

		builder.Property(x => x.TotalAmount)
			.HasColumnName("total_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Totale dello scontrino, IVA inclusa; deve coincidere con la somma delle righe entro 1 cent.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.HasDefaultValue("EUR")
			.IsRequired()
			.HasComment("Valuta ISO 4217.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("payment.pos_charge_status")
			.HasDefaultValue(PosChargeStatus.Recorded)
			.IsRequired()
			.HasComment("Stato dello scontrino POS nel gestionale: acquisito o annullato dal POS con un documento di storno.");

		builder.Property(x => x.LedgerEntryRef)
			.HasColumnName("ledger_entry_ref")
			.HasComment("public_id di payment.ledger_entries — correlazione debole (non FK EF) verso la riga contabile generata dall'addebito, valorizzata a posteriori dall'handler dell'evento.");

		builder.Property(x => x.ChargedAt)
			.HasColumnName("charged_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora dello scontrino sul POS, decorrenza retention.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: append-only (concorre al fiscale, retention 10 anni), sempre NULL.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_payment_pos_charges_public_id");

		builder.HasIndex(x => new { x.PosSystem, x.PropertyId, x.ExternalRef })
			.IsUnique()
			.HasDatabaseName("uq_payment_pos_charges_external_ref");

		builder.HasIndex(x => x.ReservationPublicId)
			.HasDatabaseName("ix_payment_pos_charges_reservation");

		builder.HasIndex(x => new { x.PropertyId, x.ChargedAt })
			.HasDatabaseName("ix_payment_pos_charges_property_date");

		builder.HasIndex(x => x.BookingNumber)
			.HasDatabaseName("ix_payment_pos_charges_booking_number");
	}
}
```

### Relazioni

`property_id`, `reservation_public_id` sono `uuid` copiati da C2/C1: solo `.HasComment`, nessuna FK
cross-schema. `ledger_entry_ref`, pur nello stesso schema `payment`, resta `.HasComment` senza FK EF:
correlazione a posteriori come su `payment_wallet_transactions.ledger_entry_ref`. Referenziata (FK
interna) da `payment_pos_charge_items.charge_id`.

`ISoftDeletable` implementato solo per uniformità (sempre `NULL`, append-only); `IAuditable` implementato.

### Note di migrazione

- Nessun dato da migrare: `teamsystem_expenses` ha 0 righe (integrazione mai attivata) e
  `ofsale_payments`/`teamsystem_payment` sono `NULL` su tutte le righe di produzione — tabella creata
  vuota.
- `SavePosChargeAsync` idempotente su `(pos_system, property_id, external_ref)` via `INSERT ... ON
  CONFLICT DO NOTHING` + `SELECT` (non un "ensureMirrored" applicativo come nel legacy); verifica
  `total_amount == Σ items.line_total` come **errore bloccante**, non warning.
  `fiscalized` (colonna morta nel legacy) non è migrata: lo stato fiscale si legge da C4 a evento.
