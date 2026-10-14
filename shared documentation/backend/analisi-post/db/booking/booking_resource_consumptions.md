# Tabella `booking_resource_consumptions`

> **Dominio**: C1 — `booking` · **sottoarea**: — (nucleo) · **schema**: `booking` · **nome flat**:
> `booking_resource_consumptions` · **forma canonica**: `booking.resource_consumptions`.
>
> **Tabella unica di consumo/occupazione per data**. Accorpa **sei** tabelle legacy sovrapposte più le
> due "sul lato opposto" (postazione che *torna* disponibile):
> [`booking_items_counts_details`](../../../analisi-pre/db/booking_items_counts_details.md),
> [`additional_items_counts_details`](../../../analisi-pre/db/additional_items_counts_details.md),
> [`services_booking_counts`](../../../analisi-pre/db/services_booking_counts.md),
> [`hourly_service_booking_management`](../../../analisi-pre/db/hourly_service_booking_management.md)
> (parte conteggio), [`location_reservations`](../../../analisi-pre/db/location_reservations.md) (blocco
> morbido), [`booking_areas`](../../../analisi-pre/db/booking_areas.md) (di fatto morta),
> più la parte "consumo" di [`booking_absence_details`](../../../analisi-pre/db/booking_absence_details.md)
> e [`sharing_details`](../../../analisi-pre/db/sharing_details.md). Disegno:
> [`docs/03-gestione-dati.md` §3.8 e §3.9](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga per (risorsa × giorno occupato × sorgente)**. La disponibilità di qualunque cosa (postazione,
articolo di magazzino, servizio bar/normale, slot orario, posto d'area) si legge da **questa sola
tabella** (aggregata in [`booking_resource_availability_daily`](booking_resource_availability_daily.md)).
Un intervallo di N giorni produce **N righe** (una per giorno — risolve il bug legacy di
`bookingCountsDetails1` che registrava tutto su un solo giorno).

`state` è **separato** dal `payment_status` (che il legacy riusava come stato riga): `held` (blocco
temporaneo / checkout / riserva morbida), `confirmed` (occupazione effettiva), `released` (liberato —
assenza/sharing/check-out/annullamento). La concorrenza è garantita da un **exclusion constraint GiST**
(§3.9), non da `SELECT` + insert non transazionale come nel legacy.

Correzioni rispetto al legacy:

- **6+ schemi diversi, `status` = `payment_status`, nomi articolo stringa, update come
  `destroy`+`recreate` non transazionale** → **1 schema**, `state` dedicato, `resource_id` FK, upsert.
- **`booking_element` stringa libera** (`"Umbrella"`, `"Lettino Standard\t"`, `"Restuarant"`, refusi,
  case) confrontata per uguaglianza con colonne fisse di `warehouses` → `resource_type enum` +
  `resource_id uuid` FK all'anagrafica di C2.
- **`booking_date` = `booking_start_date + 1` (compensazione fuso non documentata, un solo giorno)** →
  `consumption_date` per ogni giorno reale dell'intervallo.
- **`hourly_service_booking_management` con `HH:MM` stringa e `:`/`.` misti** → `slot_start`/`slot_end`
  `time` + `time_slot_id uuid` (C2).
- **`location_reservations` senza scadenza né stato** → `state = held` + `held_until` (§3.9) + job di
  scadenza.
- **`booking_absence_details` / `sharing_details` (stato doppio `status`/`isBooked`, disallineati su 33
  righe reali)** → la parte "giorno ripreso" è una riga `confirmed` qui con `source = resale`; la
  "offerta" resta in [`booking_resale_offers`](booking_resale_offers.md); i crediti in
  [`booking_resale_credit_ledger`](booking_resale_credit_ledger.md).
- **`quantity` con `0` e negativi in produzione** → `CHECK (quantity > 0)`.


> ### ✅ Applicato nel backend (2026-09-21)
>
> Partizionamento **annuale per anno solare** su `consumption_date`
> ([`docs/05-ottimizzazioni-db.md` §2](../../../progettazione/05-ottimizzazioni-db.md)), migration
> `PartitionResourceTablesByYear` di `Module.Booking`. Partizioni `_y2021`…`_y2035` più una `_default`
> come rete di sicurezza per date fuori intervallo. Fino al 2026-09-30 arrivavano al 2030: le
> 2031–2035 le ha aggiunte `ExtendPartitionsAndResaleOfferExclude`, perché la `_default` non fallisce —
> raccoglie in silenzio le righe di un anno senza partizione, e finché le contiene la partizione di
> quell'anno non è più creabile. Dal 2026-10-06 entrambe le migration sono consolidate nella
> `InitialCreate` di `Module.Booking` ([`docs/42-migration.md` §10](../../../progettazione/42-migration.md));
> manutenzione in [`docs/06-partizioni-manutenzione.md`](../../../progettazione/06-partizioni-manutenzione.md).
>
> **La chiave primaria è ora `(id, consumption_date)`**, non `id` da solo: PostgreSQL impone che la PK
> di una tabella partizionata contenga la chiave di partizione. Non è una scelta di modellazione, è il
> prezzo del partizionamento — `id` resta comunque un'identità globale sul padre.
>
> **Perché anno solare e non stagione**: le stagioni sono definite per stabilimento
> ([`catalog_pricing_seasons`](../catalog/catalog_pricing_seasons.md)), quindi non esiste un confine
> unico su cui tagliare. L'anno solare è l'unico criterio condiviso da tutte le property.
>
> Misurato sul DB di sviluppo: **3.809.485 righe per 786 MB**, con il 76% concentrato nel solo 2026.
> Dopo la conversione una query su un mese legge **una sola partizione** invece dell'intera tabella
> (verificato con `EXPLAIN`).
>
> **Nota sul confronto con §2.1.** La stima di ~1,02M righe di §2.1 è un volume *a regime*, calcolato
> sul **dump di produzione**; questo è il DB di **sviluppo**, che contiene anche dati sintetici. I due
> numeri non sono confrontabili e il secondo non dice che il primo sia sbagliato. Quello che i dati di
> sviluppo confermano — in modo molto più netto della stima — è la **conclusione** di §2.1: applicando
> la finestra di retention N-1 restano **3.794.143 righe su 3.809.485**, cioè la potatura elimina lo
> **0,4%**. La retention non sgonfia questa tabella, che è esattamente la ragione per cui è l'unica
> candidata reale al partizionamento.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_resource_consumptions` | `id` | PK | — |
| `ix_..._availability` | `(property_id, resource_id, consumption_date) WHERE state <> 'released'` | parziale | disponibilità puntuale di una risorsa in una data |
| `ix_..._property_date_state` | `(property_id, consumption_date, state)` | btree | scansione "stato spiaggia del {giorno}" (mappa/planner) |
| `ix_..._type_date` | `(property_id, resource_type, consumption_date) WHERE state <> 'released'` | parziale | conteggi per tipo (ricerca "disponibile il {data}") |
| `ix_..._source` | `(source_kind, source_id)` | btree | risalita alla prenotazione/offerta di origine; cancellazione a cascata logica |
| `ix_..._booking_number` | `(booking_number)` | btree | correlazione diretta con C3/C4 |
| `ix_..._holds_expiry` | `(held_until) WHERE state = 'held' AND held_until IS NOT NULL` | parziale | job di scadenza degli hold |
| `xc_..._no_overlap` | `EXCLUDE USING gist (property_id WITH =, resource_id WITH =, daterange(consumption_date, consumption_date, '[]') WITH &&, period_mask WITH &&) WHERE (state <> 'released' AND resource_id IS NOT NULL AND resource_type IN ('umbrella','sunbed','cabin','deckchair','chair','tent','parking'))` | EXCLUSION | il DB garantisce nessun doppio consumo attivo sulla stessa postazione/giorno/fascia ⚠️ **Non presente a DB** (verifica del 2026-10-06): progettato, mai creato. Riquadro sotto. |

*(`period_mask` = colonna generata `int` da `booking_period` per la sovrapposizione fascia: `full_day`
maschera `morning`+`afternoon`.)*

> ### ⚠️ Il vincolo anti-doppia-prenotazione non esiste a DB (verifica del 2026-10-06)
>
> Né `xc_booking_resource_consumptions_no_overlap` né la colonna generata `period_mask` sono mai stati
> creati: non c'erano nella prima `InitialCreate` del 2026-09-14 e non sono stati aggiunti dopo (lo SQL
> a mano descritto in «Implementazione .NET» non è mai stato scritto nella migration). Oggi **nessun
> vincolo di database impedisce due consumi attivi sulla stessa postazione nella stessa fascia**: la
> garanzia, se c'è, è solo applicativa. Anche [`docs/05-ottimizzazioni-db.md` §5](../../../progettazione/05-ottimizzazioni-db.md)
> elenca `period_mask` fra le colonne generate «già implementate»: non è così.
>
> Prima di crearlo vanno sciolti tre punti:
>
> 1. **Dati**: sul DB di sviluppo ci sono **744 coppie** di consumi attivi sulla stessa postazione e
>    giorno con fasce che si sovrappongono (tipi postazione dell'elenco sopra, `deleted_at IS NULL`). Il
>    vincolo oggi fallirebbe. Va capito, come per le offerte di rivendita, se è la chiave a essere
>    sbagliata o i dati.
> 2. **Tabella partizionata**: dal 2026-09-21 la tabella è partizionata per `consumption_date`, e
>    PostgreSQL (qui 17.6) accetta un `EXCLUDE` su una tabella partizionata solo se include la chiave di
>    partizione **con l'operatore `=`**. Il `daterange(consumption_date, consumption_date, '[]') WITH &&`
>    progettato equivale a un'uguaglianza ma non è scritto così: va riscritto `consumption_date WITH =`.
> 3. **`period_mask WITH &&`** su un `int` non funziona: `&&` non è definito per gli interi. La forma
>    corretta è quella già usata per [`booking_resale_offers`](booking_resale_offers.md): un `int4range`
>    (mattina `[1,2)`, pomeriggio `[2,3)`, giornata intera `[1,3)`), che rende inutile la colonna
>    generata.

## Campi

### id
- **Legacy**: `*.id` delle sei tabelle
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### property_id
- **Legacy**: `*.property_id` (`int`/`varchar`; in `location_reservations`/`sharing_details` con
  placeholder sporchi `9999`/`99999`)
- **Descrizione**: stabilimento. Prima colonna di ogni indice e dell'exclusion constraint.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2; placeholder legacy bonificati in migrazione.

### resource_type
- **Legacy**: `booking_element` / `service_type` / `hourly_service_name` (stringhe libere)
- **Descrizione**: tipo di risorsa consumata.
- **Tipo**: `enum booking.resource_type` — valori: `umbrella`, `sunbed`, `sunbed_xl`, `cabin`,
  `special_cabin`, `deckchair`, `chair`, `tent`, `parking`, `ticket`, `area_seat`, `bar_service`,
  `normal_service`, `hourly_service`, `inventory_item` (articolo di magazzino generico non a tipo fisso).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: allineato all'enum `resource_type` di
  [`docs/03-gestione-dati.md` §3.8](../../../progettazione/03-gestione-dati.md) e a `catalog.spot_type` /
  `catalog.service_sale_item.kind`.

### resource_id
- **Legacy**: nessuno (il legame era per stringa nome)
- **Descrizione**: id dell'anagrafica risorsa in C2: postazione
  ([`catalog_map_spots`](../catalog/catalog_map_spots.md)), servizio
  ([`catalog_service_sale_items`](../catalog/catalog_service_sale_items.md)), articolo di magazzino
  ([`catalog_inventory_items`](../catalog/catalog_inventory_items.md)).
- **Tipo**: `uuid` (= `public_id` della relativa tabella C2).
- **Default**: nessuno.
- **Nullabile**: Sì (articolo personalizzato non a catalogo — allora conta solo `resource_name_snapshot`).
- **Relazioni**: **`public_id` (uuid) copiato** da C2 (tipo di tabella secondo `resource_type`).

### resource_name_snapshot
- **Legacy**: `booking_element` / `service_name` / `hourly_service_name`
- **Descrizione**: nome della risorsa **congelato** (per reportistica anche se rinominata / non a
  catalogo).
- **Tipo**: `varchar(160)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: snapshot; refusi/`\t` normalizzati in migrazione.

### consumption_date
- **Legacy**: `booking_date` / `selected_date` / `sharing_date` / `absence_date` / `startDate` (`datetime`)
- **Descrizione**: **il giorno occupato** — sempre una riga per giorno.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### booking_period
- **Legacy**: `bookings.booking_period` (per la postazione)
- **Descrizione**: fascia occupata.
- **Tipo**: `enum booking.day_period` — valori: `full_day`, `morning`, `afternoon`.
- **Default**: `full_day`.
- **Nullabile**: No.
- **Relazioni**: progettata per alimentare, tramite la colonna generata `period_mask`, l'exclusion
  constraint per la sovrapposizione di fascia — ⚠️ né la colonna né il vincolo esistono a DB (riquadro
  in «Indici»).

### slot_start / slot_end
- **Legacy**: `hourly_service_booking_management.booking_slot_start_time` / `_end_time` (`varchar` `HH:MM`, `:`/`.` misti)
- **Descrizione**: fascia oraria interna — valorizzati **solo** per `resource_type = hourly_service`.
- **Tipo**: `time` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: coppia coerente con `time_slot_id`.

### time_slot_id
- **Legacy**: — (il legacy non legava lo slot alla fascia di catalogo)
- **Descrizione**: fascia di catalogo prenotata (prezzo dedicato / capienza).
- **Tipo**: `uuid` (= `public_id` di `catalog_service_sale_item_time_slots`).
- **Default**: `NULL`.
- **Nullabile**: Sì (solo `hourly_service`).
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_service_sale_item_time_slots`](../catalog/catalog_service_sale_item_time_slots.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 3.809.485 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### quantity
- **Legacy**: `quantity` / `booked_count` (`int`; con `0` e negativi in produzione)
- **Descrizione**: unità consumate in quel giorno (per la postazione puntuale = `1`; per magazzino/area
  = N).
- **Tipo**: `integer`.
- **Default**: `1`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (quantity > 0)`.

### state
- **Legacy**: `status` = copia del `payment_status` (`0`/`1`/`2`/`3`); `isBooked` (0/1)
- **Descrizione**: stato del consumo, **indipendente dal pagamento**.
- **Tipo**: `enum booking.consumption_state` — valori: `held` (blocco temporaneo: checkout in corso,
  riserva morbida, hold), `confirmed` (occupazione effettiva), `released` (liberato: check-out
  anticipato, annullamento, giorno ceduto in assenza/sharing).
- **Default**: `confirmed`.
- **Nullabile**: No.
- **Relazioni**: l'exclusion constraint e gli indici di disponibilità filtrano `state <> 'released'`.

### held_until
- **Legacy**: `hold_locations.hold_until` (dal client, mai usato) — feature incompleta
- **Descrizione**: scadenza del blocco temporaneo (per `state = held` da checkout/hold). Un job (o un
  `WHERE held_until > now()` in verifica) lo fa scadere → `released`.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: calcolata **server-side** (`now() + Booking:HoldTtl` da configurazione), mai dal body.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 3.809.485 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### source_kind
- **Legacy**: implicito nel nome della tabella / `booking_type` polimorfo
- **Descrizione**: cosa ha generato il consumo.
- **Tipo**: `enum booking.consumption_source` — valori: `reservation_line` (riga postazione),
  `service_line` (riga servizio), `additional_item` (articolo aggiuntivo per data),
  `manual_hold` (blocco morbido operativo — ex `location_reservations`/`hold_locations`),
  `resale` (giorno ripreso da assenza/sharing — ex la riga di "consumo" di
  `booking_absence_details`/`sharing_details`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: guida quale tabella referenzia `source_id`.

### source_id
- **Legacy**: `booking_id` / `reference_id` (polimorfo, per stringa)
- **Descrizione**: id (interno C1) dell'entità di origine: `booking_lines.id`, `booking_service_lines.id`,
  `booking_line_additional_items.id`, `booking_spot_holds.id`, `booking_resale_offers.id`.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (blocco morbido "manuale" senza entità di dominio a monte).
- **Relazioni**: FK **interna allo schema** verso la tabella indicata da `source_kind` (nessuna FK
  polimorfa DB: coerenza in applicazione + `ON DELETE` gestito dai comandi di dominio).

### booking_number
- **Legacy**: `bookingNumber` (spesso non popolato: `"0"`)
- **Descrizione**: numero prenotazione **copiato** per query dirette (coerente con
  [`docs/03-gestione-dati.md` §3.8](../../../progettazione/03-gestione-dati.md)). `NULL` per `manual_hold`.
- **Tipo**: `varchar(40)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: denormalizzato.

### booking_channel
- **Legacy**: `services_booking_counts.booking_type` (`bookingByAdmin` travestito)
- **Descrizione**: canale della prenotazione di origine (per statistiche online/gestionale sui consumi).
- **Tipo**: `enum booking.booking_channel` — valori: `customer`, `manager`.
- **Default**: `NULL`.
- **Nullabile**: Sì (per `manual_hold`).
- **Relazioni**: vocabolario condiviso con `booking_reservations`.

### created_at / updated_at
- **Legacy**: `createdAt` / `updatedAt` (spesso `0000-00-00` o assenti)
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` (+ trigger).
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: `destroy` fisico
- **Descrizione**: presente per uniformità. Nella pratica il "annullamento" di un consumo si esprime con
  `state = released` (mantiene la storia); `deleted_at` si usa solo per correzioni/bonifiche.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: retention → hard-delete oltre **N−1** (stagione corrente + la precedente),
  **subordinato all'aggregazione**: il titolare ha richiesto esplicitamente un **aggregato mensile**
  prima della potatura, perché la reportistica storica di occupazione non deve sparire col dettaglio
  ([`docs/82-retention-data.md` §3.1](../../../progettazione/82-retention-data.md),
  [`docs/80-retention.md` §3.1](../../../progettazione/80-retention.md)).

  > Il vincolo "aggrega, poi cancella" **non è esprimibile** in
  > [`privacy_retention_policies`](../privacy/privacy_retention_policies.md) (che conosce solo
  > tabella + colonna data + durata + azione): vive nel job `SeasonalArchive`, che deve fallire in modo
  > rumoroso se l'aggregato del periodo non è stato prodotto, invece di cancellare comunque. La riga di
  > policy dice *quando*, il job garantisce il *prima*.

## Valutazione JSON

Nessun JSON. È il punto di convergenza di sei tabelle legacy + due JSON (`areaDetails`,
`booking_absence_dates`). Non ha `jsonb`.

## Suggerimenti per le prestazioni

- **Tabella ad alta scrittura**: set di indici **minimo** e mirato ai percorsi noti (disponibilità
  puntuale, scansione per giorno, scadenza hold). Niente indici "per ogni evenienza".
- La disponibilità **aggregata** (per ricerca e badge) si legge da
  [`booking_resource_availability_daily`](booking_resource_availability_daily.md), non da un `GROUP BY`
  qui.
- L'**exclusion constraint GiST** rende superfluo il `SELECT … FOR UPDATE` applicativo per le
  postazioni: è il DB a rifiutare il secondo consumo concorrente. Per magazzino/servizi (quantità,
  non esclusività) si usa `SELECT … FOR UPDATE` sull'aggregato o un `UPSERT` atomico sul contatore.
- Redis solo come **cache di lettura** della mappa (`property:{id}:availability:{date}`), invalidata
  a evento; la validazione in prenotazione è sempre contro il DB
  ([`docs/03-gestione-dati.md` §3.8](../../../progettazione/03-gestione-dati.md)).
- `consumption_date` `date` (non `timestamptz`) tiene l'indice compatto; il legacy salvava
  `datetime` con `+1 giorno` di compensazione.

## Implementazione .NET

`ResourceConsumption` è un **aggregate root** del nucleo del modulo `Module.Booking`, schema `booking` —
tabella di convergenza di sei tabelle legacy. Scritta **solo** dai command handler di
`Reservation`/`ServiceLine`/`SpotHold`/`ResaleOffer` (mai direttamente dall'Application layer generico):
è il "single source of truth" della disponibilità, aggregata poi in
[`booking_resource_availability_daily`](booking_resource_availability_daily.md). Implementa
`ISoftDeletable`/`IAuditable` (con la nota che `deleted_at` è d'uso raro — vedi sezione Campi: il rilascio
normale passa da `state = released`, non da soft-delete).

```csharp
// Domain/Enums/ResourceType.cs
public enum ResourceType
{
	Umbrella = 0, Sunbed = 1, SunbedXl = 2, Cabin = 3, SpecialCabin = 4, Deckchair = 5, Chair = 6,
	Tent = 7, Parking = 8, Ticket = 9, AreaSeat = 10, BarService = 11, NormalService = 12,
	HourlyService = 13, InventoryItem = 14
}

// Domain/Enums/ConsumptionState.cs
public enum ConsumptionState { Held = 0, Confirmed = 1, Released = 2 }

// Domain/Enums/ConsumptionSource.cs
public enum ConsumptionSource
{
	ReservationLine = 0, ServiceLine = 1, AdditionalItem = 2, ManualHold = 3, Resale = 4
}
```

`DayPeriod` (`booking_period`) e `BookingChannel` (`booking_channel`) sono **enum condivisi**, definiti
rispettivamente in [`booking_lines.md`](booking_lines.md) e in `booking_reservations.md` — non ridefiniti
qui.

```csharp
// <Modulo>DbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ResourceType>("booking", "resource_type");
modelBuilder.HasPostgresEnum<ConsumptionState>("booking", "consumption_state");
modelBuilder.HasPostgresEnum<ConsumptionSource>("booking", "consumption_source");
```

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class ResourceConsumptionConfiguration : IEntityTypeConfiguration<ResourceConsumption>
{
	public void Configure(EntityTypeBuilder<ResourceConsumption> builder)
	{
		builder.ToTable("booking_resource_consumptions", "booking", t =>
		{
			t.HasComment(
				"Una riga per (risorsa × giorno occupato × sorgente): unica fonte di verità della " +
				"disponibilità di postazioni, articoli, servizi e slot orari.");
			t.HasCheckConstraint("ck_booking_resource_consumptions_quantity_positive", "quantity > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_booking_resource_consumptions");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; prima colonna di ogni indice e dell'exclusion constraint");

		builder.Property(x => x.ResourceType)
			.HasColumnName("resource_type")
			.HasColumnType("booking.resource_type")
			.IsRequired()
			.HasComment("Tipo di risorsa consumata (postazione, articolo, servizio, slot orario)");

		builder.Property(x => x.ResourceId)
			.HasColumnName("resource_id")
			.HasComment("public_id C2 della risorsa (map_spots / service_sale_items / inventory_items secondo resource_type), copiato — nessuna FK cross-schema; NULL per articolo personalizzato non a catalogo");

		builder.Property(x => x.ResourceNameSnapshot)
			.HasColumnName("resource_name_snapshot")
			.HasMaxLength(160)
			.HasComment("Nome della risorsa congelato al momento del consumo, per reportistica anche se rinominata/non a catalogo");

		builder.Property(x => x.ConsumptionDate)
			.HasColumnName("consumption_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Il giorno occupato: una riga per giorno, mai un intervallo");

		builder.Property(x => x.BookingPeriod)
			.HasColumnName("booking_period")
			.HasColumnType("booking.day_period")
			.HasDefaultValue(DayPeriod.FullDay)
			.IsRequired()
			.HasComment("Fascia occupata; alimenta la colonna generata period_mask usata dall'exclusion constraint");

		builder.Property(x => x.SlotStart)
			.HasColumnName("slot_start")
			.HasColumnType("time")
			.HasComment("Inizio fascia oraria interna, valorizzato solo per resource_type = hourly_service");

		builder.Property(x => x.SlotEnd)
			.HasColumnName("slot_end")
			.HasColumnType("time")
			.HasComment("Fine fascia oraria interna, valorizzato solo per resource_type = hourly_service");

		builder.Property(x => x.TimeSlotId)
			.HasColumnName("time_slot_id")
			.HasComment("public_id di catalog.service_sale_item_time_slots (fascia di catalogo prenotata), copiato — nessuna FK cross-schema; solo hourly_service");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasDefaultValue(1)
			.IsRequired()
			.HasComment("Unità consumate in quel giorno (1 per postazione puntuale, N per magazzino/area)");

		builder.Property(x => x.State)
			.HasColumnName("state")
			.HasColumnType("booking.consumption_state")
			.HasDefaultValue(ConsumptionState.Confirmed)
			.IsRequired()
			.HasComment("Stato del consumo, indipendente dal pagamento: bloccato temporaneamente, occupazione effettiva, liberato");

		builder.Property(x => x.HeldUntil)
			.HasColumnName("held_until")
			.HasComment("Scadenza del blocco temporaneo (state = held), calcolata server-side (now() + Booking:HoldTtl), mai dal body");

		builder.Property(x => x.SourceKind)
			.HasColumnName("source_kind")
			.HasColumnType("booking.consumption_source")
			.IsRequired()
			.HasComment("Cosa ha generato il consumo: riga postazione, riga servizio, articolo aggiuntivo, blocco manuale, ripresa di rivendita");

		builder.Property(x => x.SourceId)
			.HasColumnName("source_id")
			.HasComment("Id (interno C1) dell'entità di origine indicata da source_kind — nessuna FK polimorfa DB, coerenza gestita in applicazione; NULL per manual_hold senza entità a monte");

		builder.Property(x => x.BookingNumber)
			.HasColumnName("booking_number")
			.HasMaxLength(40)
			.HasComment("Numero prenotazione copiato per query dirette; NULL per manual_hold");

		builder.Property(x => x.BookingChannel)
			.HasColumnName("booking_channel")
			.HasColumnType("booking.booking_channel")
			.HasComment("Canale della prenotazione di origine, per statistiche sui consumi; NULL per manual_hold");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione della riga di consumo");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di ultima modifica della riga di consumo");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Presente per uniformità; il rilascio normale passa da state = released, non da soft-delete — usato solo per correzioni/bonifiche e retention");

		builder.HasIndex(x => new { x.PropertyId, x.ResourceId, x.ConsumptionDate })
			.HasFilter("state <> 'released'")
			.HasDatabaseName("ix_booking_resource_consumptions_availability");

		builder.HasIndex(x => new { x.PropertyId, x.ConsumptionDate, x.State })
			.HasDatabaseName("ix_booking_resource_consumptions_property_date_state");

		builder.HasIndex(x => new { x.PropertyId, x.ResourceType, x.ConsumptionDate })
			.HasFilter("state <> 'released'")
			.HasDatabaseName("ix_booking_resource_consumptions_type_date");

		builder.HasIndex(x => new { x.SourceKind, x.SourceId })
			.HasDatabaseName("ix_booking_resource_consumptions_source");

		builder.HasIndex(x => x.BookingNumber)
			.HasDatabaseName("ix_booking_resource_consumptions_booking_number");

		builder.HasIndex(x => x.HeldUntil)
			.HasFilter("state = 'held' AND held_until IS NOT NULL")
			.HasDatabaseName("ix_booking_resource_consumptions_holds_expiry");

		// source_id: nessuna FK EF (riferimento polimorfo secondo source_kind verso booking_lines,
		// booking_service_lines, booking_line_additional_items, booking_spot_holds, booking_resale_offers)
	}
}
```

**Exclusion constraint GiST anti-doppia-prenotazione** — non esprimibile in Fluent API pura, va aggiunto
con SQL raw nella migration generata (insieme alla colonna generata `period_mask`).

> ⚠️ **Mai applicato, e così com'è non si applicherebbe**: vedi il riquadro in «Indici» (dati che lo
> violano, tabella nel frattempo partizionata, `&&` non definito su `int`). Il blocco resta come traccia
> del progetto iniziale.

```csharp
// Migration <timestamp>_AddBookingResourceConsumptions.cs, dopo il CreateTable generato da EF
migrationBuilder.Sql("""
	ALTER TABLE booking.booking_resource_consumptions
		ADD COLUMN period_mask int GENERATED ALWAYS AS (
			CASE booking_period
				WHEN 'full_day' THEN 3   -- bit0=morning, bit1=afternoon
				WHEN 'morning' THEN 1
				WHEN 'afternoon' THEN 2
			END
		) STORED;

	CREATE EXTENSION IF NOT EXISTS btree_gist;

	ALTER TABLE booking.booking_resource_consumptions
		ADD CONSTRAINT xc_booking_resource_consumptions_no_overlap
		EXCLUDE USING gist (
			property_id WITH =,
			resource_id WITH =,
			daterange(consumption_date, consumption_date, '[]') WITH &&,
			period_mask WITH &&
		) WHERE (
			state <> 'released'
			AND resource_id IS NOT NULL
			AND resource_type IN ('umbrella','sunbed','sunbed_xl','cabin','special_cabin','deckchair',
				'chair','tent','parking')
		);
	""");
```

Nella `Down()` della stessa migration: `DROP CONSTRAINT xc_booking_resource_consumptions_no_overlap` e
`DROP COLUMN period_mask` prima del `DropTable` generato. Il modello EF espone `PeriodMask` come proprietà
calcolata **read-only** (`.HasComputedColumnSql(...)` oppure semplicemente ignorata con `.Ignore()` se non
serve leggerla in applicazione — la colonna esiste solo per l'exclusion constraint).

**Note di migrazione**:

- Sei tabelle legacy (`booking_items_counts_details`, `additional_items_counts_details`,
  `services_booking_counts`, `hourly_service_booking_management`, `location_reservations`,
  `booking_areas`) + la parte "consumo" di `booking_absence_details`/`sharing_details` → righe unificate
  con `source_kind` a indicare la provenienza.
- Espansione **per-giorno** di ogni intervallo legacy: risolve il bug di `bookingCountsDetails1` che
  registrava tutto su un solo giorno (`booking_date = booking_start_date + 1`, compensazione fuso non
  documentata).
- `booking_element`/`service_type`/`hourly_service_name` (stringhe libere, refusi, `\t`, case diverso) →
  `resource_type` (enum, matching normalizzato) + `resource_id` risolto contro l'anagrafica C2
  corrispondente; se non risolvibile, `resource_id = NULL` e nome congelato in `resource_name_snapshot`.
- `status` (copia di `payment_status`) → `state`: `1` (pagato) → `Confirmed`; `0`/altri → `Held` o
  `Released` secondo il contesto della tabella di origine (`location_reservations`/`hold_locations` senza
  pagamento → `Held`; righe di assenza/sharing "riprese" → `Confirmed` con `source_kind = Resale`).
- `quantity <= 0` (righe sporche in prod) → scartate, non migrate (violerebbero il nuovo
  `CHECK (quantity > 0)`).
- Placeholder `property_id` `9999`/`99999` → bonificati secondo il mapping property già usato dalle altre
  schede C1; righe non riconducibili a una property reale → scartate.
- `hourly_service_booking_management.booking_slot_start_time`/`_end_time` (`HH:MM` con `:`/`.` misti) →
  parsing tollerante verso `time`, `time_slot_id` risolto contro il catalogo C2 delle fasce orarie (nuovo
  legame, non esisteva nel legacy).
- `booking_areas` (di fatto morta) e la gemella `sharing_details.sharings`: nessuna riga significativa da
  migrare oltre a quanto già coperto dalle altre fonti.
