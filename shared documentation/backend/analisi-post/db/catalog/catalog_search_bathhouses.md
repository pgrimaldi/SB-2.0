# Tabella `catalog_search_bathhouses`

> **Dominio**: C2 — `catalog` · **sottoarea**: — (read-model di ricerca pubblica) · **schema**: `catalog`
> · **nome flat**: `catalog_search_bathhouses` · **forma canonica**: `catalog.search_bathhouses`.
>
> Tabella **derivata** (nessuna legacy diretta): read-model per la **ricerca pubblica** di stabilimenti.
> Sostituisce, come *comportamento*, il flusso legacy `serchProperty` — `findAll` senza paginazione +
> una query `getLowestPrice` **per ogni** risultato + filtro servizi in JavaScript
> ([`analisi-pre/db/properties.md`](../../../analisi-pre/db/properties.md),
> [`docs/01-criticita-e-indicazioni-backend.md` §5](../../../progettazione/01-criticita-e-indicazioni-backend.md)).
> Fonte di verità = `catalog_bathhouses` + `catalog_pricing_*` + `catalog_bathhouse_amenities` +
> `identity_customer_bathhouse_likes` (C5); **sempre ricostruibile**. Disegno:
> [`_config-snapshot-proposta.md` §4.6](../_config-snapshot-proposta.md).

---

## Descrizione

Una riga per **vetrina pubblicata** (`catalog_bathhouses` con `published_at IS NOT NULL`). Combina:

- **colonne vere, indicizzate** per `WHERE`/`ORDER BY`/`LIMIT`: nome, città, regione, slug, geo, prezzo
  minimo corrente, servizi (`amenity_ids smallint[]`), contatore "mi piace", flag di pubblicazione;
- un **`card jsonb`** con il resto dei dati necessari a renderizzare la card di risultato (foto
  principale come `assetRef`, snippet, orari, servizi principali) — dati che non servono per
  filtrare/ordinare.

Interrogata da `GET /public/search?...` (città/regione/geo + filtri servizi/prezzo/distanza + sort);
toccando una card il client carica poi lo snapshot `public_showcase`
([`catalog_config_snapshots`](catalog_config_snapshots.md)).

> ### Stabilimenti cessati: restano qui, in forma ridotta (decisione 2026-09-17)
>
> Alla cessazione del contratto la scheda **non** esce da questo read-model. Il titolare ha deciso che
> resta nella ricerca pubblica, **ridotta alle sole informazioni essenziali** (nome, foto, posizione,
> descrizione base) e **senza prenotazione online**, perché serve a popolare il portale
> ([`docs/82-retention-data.md` §5](../../../progettazione/82-retention-data.md),
> [`docs/80-retention.md` §5](../../../progettazione/80-retention.md)).
>
> Tre conseguenze concrete su questa tabella:
>
> 1. **Il rebuild non deve cancellare la riga** quando la property cessa. Il predicato di sorgente
>    (`catalog_bathhouses.published_at IS NOT NULL`) resta soddisfatto; va aggiunto il caso "property
>    cessata" come **variante della card**, non come esclusione.
> 2. **`card jsonb` va costruito in forma ridotta**: niente listini, orari, contatti operativi o
>    servizi prenotabili — solo i quattro campi essenziali. Non è una questione estetica: la cascata del
>    §5 **cancella** le tabelle di configurazione a +3 mesi dalla cessazione, quindi una card completa
>    resterebbe a riferire dati che nel frattempo non esistono più, e il primo rebuild successivo la
>    svuoterebbe in modo incontrollato. Meglio ridurla subito e deliberatamente.
> 3. **`min_price` diventa `NULL`** (non `0`): non c'è più un listino da cui derivarlo, e uno zero
>    ordinerebbe questi stabilimenti in cima a ogni ricerca per prezzo crescente.
>
> Il flag che distingue i due casi è lo stato della property — oggi non ancora modellato: vedi la nota
> "Manca lo stato cessato" in [`catalog_properties`](catalog_properties.md).

**Aggiornamento**: stesso modello degli snapshot — rebuild **sincrono** su evento
(`BathhousePublished`, `AmenitiesChanged`, `PriceListChanged`, `BathhouseLikeCountChanged` da C5, …) +
job **notturno** di ricostruzione totale + utility manuale
([`_config-snapshot-proposta.md` §5, §8](../_config-snapshot-proposta.md)).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_search_bathhouses` | `bathhouse_public_id` | PK | chiave naturale (= `catalog_bathhouses.public_id`) |
| `ix_..._published_region_city` | `(region, city) WHERE is_published` | parziale | ricerca per area |
| `ix_..._published_name_trgm` | `name gin_trgm_ops WHERE is_published` | GIN trigram | "digita e cerca" per nome |
| `ix_..._published_price` | `(min_price) WHERE is_published` | parziale | filtro/sort per prezzo |
| `ix_..._amenities` | `amenity_ids` | GIN | filtro "stabilimenti con Wi-Fi e Bar" (`@> ARRAY[…]`) |
| `ix_..._property` | `(property_id)` | — | invalidazione per property |

## Campi

### bathhouse_public_id
- **Legacy**: `bathouses.id` (via `catalog_bathhouses.public_id`)
- **Descrizione**: identità della vetrina; PK di questo read-model.
- **Tipo**: `uuid`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: = [`catalog_bathhouses.public_id`](catalog_bathhouses.md) (copiato, no FK — read-model
  ricostruibile). Il client usa questo id per poi chiedere `public_showcase`.

### property_id
- **Legacy**: `bathouses.propertyId`
- **Descrizione**: stabilimento (per invalidazione e per raggruppare le vetrine di una stessa property).
- **Tipo**: come il discriminatore tenant del progetto ([D-01](../_domande-committente.md)).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE`.

### name
- **Legacy**: `bathouses.bathouseName`
- **Descrizione**: nome commerciale (colonna, per il match testuale e il sort).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: da `catalog_bathhouses.name`.

### city / region / province
- **Legacy**: `bathouses.city` / `bathouses.region` / `bathouses.province`
- **Descrizione**: area geografica, per i filtri "per città/regione" della ricerca.
- **Tipo**: `varchar(255)` ciascuno.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: da `catalog_bathhouses`.

### slug
- **Legacy**: `bathouses.slug`
- **Descrizione**: per costruire il link alla scheda pubblica direttamente dalla card.
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: da `catalog_bathhouses.slug`.

### latitude / longitude
- **Legacy**: `bathouses.latitude` / `bathouses.longitude` (`varchar`)
- **Descrizione**: posizione per la ricerca "vicino a me" / entro raggio.
- **Tipo**: `double precision` ciascuna.
- **Default**: nessuno.
- **Nullabile**: Sì / Sì (vetrine senza coordinate non compaiono nelle ricerche per distanza).
- **Relazioni**: da `catalog_bathhouses.latitude`/`longitude`.
- **Scostamento dal progetto iniziale**: la scheda prevedeva una sola colonna `location
  geography(Point, 4326)` con indice GiST. Nell'implementazione sono due colonne numeriche, **senza
  indice spaziale**, per non portare nella solution la dipendenza da PostGIS e NetTopologySuite. Con
  ~180 vetrine pubblicate il calcolo della distanza in scansione costa poco; se il volume crescesse
  di ordini di grandezza, la strada è un indice su `ll_to_earth` (estensione `earthdistance`) o il
  ritorno a PostGIS.

### min_price
- **Legacy**: calcolato al volo da `getLowestPrice` (una query per risultato — anti-pattern)
- **Descrizione**: prezzo minimo corrente prenotabile online (il più basso fra le voci di listino con
  `sales_channel <> 'manager_only'` per il periodo corrente).
- **Tipo**: `numeric(12,2)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (nessun listino online → non filtrabile per prezzo, non "da X €").
- **Relazioni**: materializzato da `catalog_pricing_price_lists` + `catalog_pricing_price_list_period_prices` +
  `catalog_pricing_seasons`/`catalog_pricing_tariff_periods`.
- **Note**: qui **sì** materializzato (a differenza delle schede di dominio, dove il prezzo minimo è
  calcolato dal motore prezzo) — è lo scopo di questo read-model: evitare la query-per-risultato.

### amenity_ids
- **Legacy**: filtro servizi fatto in JavaScript dopo aver caricato tutto
- **Descrizione**: id delle dotazioni offerte dalla vetrina, per il filtro "con questi servizi".
- **Tipo**: `smallint[]`.
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: da [`catalog_bathhouse_amenities`](catalog_bathhouse_amenities.md); indice **GIN** per
  `amenity_ids @> ARRAY[1,7]`.
- **Note**: si può tenere in parallelo un `amenity_codes text[]` se il frontend filtra per codice; l'id
  è più compatto per l'indice.

### like_count
- **Legacy**: `bathouses.likeCount` (denormalizzato, con drift e valori negativi)
- **Descrizione**: numero di "mi piace" della vetrina, per il sort "più popolari" e la card.
- **Tipo**: `integer`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: ricalcolato da C5 `identity_customer_bathhouse_likes`
  (`COUNT(*) WHERE bathhouse_id = ? AND deleted_at IS NULL`), aggiornato all'evento
  `BathhouseLikeCountChanged`.

### is_published
- **Legacy**: derivava da `properties.status` (mai da `bathouses.status`, vestigiale)
- **Descrizione**: la vetrina è visibile nella ricerca pubblica.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: `catalog_bathhouses.published_at IS NOT NULL` **e** `catalog_bathhouses.deleted_at IS
  NULL` **e** property `approved` (+ `imported` se [D-05](../_domande-committente.md) lo consente) **e**
  abbonamento non sospeso.
- **Note**: le righe restano in tabella anche con `is_published = false` (aggiornamento più semplice); gli
  indici parziali `WHERE is_published` le escludono dalle query.

### card
- **Legacy**: — (nuovo)
- **Descrizione**: dati non filtrabili necessari a renderizzare la card di risultato.
- **Tipo**: `jsonb`.
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: composto dal rebuild.
- **Contenuto**: `mainPhoto` (`assetRef`), `shortDescription`, `openHours` `{from,to}`, `topAmenities`
  (nomi tradotti dei servizi principali), eventuale `badge` ("Novità", "Pet friendly").
- **Note**: stessa eccezione "JSON derivato" di [`catalog_config_snapshots.payload`](catalog_config_snapshots.md);
  file come `assetRef`, mai URL completo.

### content_hash
- **Legacy**: — (nuovo)
- **Descrizione**: hash della riga (colonne + `card`), per `ETag` sulle risposte di ricerca a pagina
  singola / dettaglio card.
- **Tipo**: `bytea` (32 byte).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### built_at
- **Legacy**: — (nuovo)
- **Descrizione**: ultimo rebuild della riga.
- **Tipo**: `timestamptz`.
- **Default**: `now()` al rebuild.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / ultima modifica della riga.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: soft-delete (per uniformità; non usato funzionalmente — la visibilità è
  `is_published`).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

`card` è `jsonb` **derivato** (stessa eccezione consentita di
[`catalog_config_snapshots`](catalog_config_snapshots.md)); tutto ciò che serve per filtrare/ordinare è
in **colonne tipizzate e indicizzate** — è la ragione per cui questa è una tabella dedicata e non un
`snapshot_kind` dentro `catalog_config_snapshots`.

## Suggerimenti per le prestazioni

- Una sola query per la pagina di ricerca: `WHERE is_published AND <filtri>` con indici GIN
  (`amenity_ids`), B-tree (`min_price`, `region`/`city`), trigram (`name`), `ORDER BY`
  + `LIMIT`/`OFFSET` (o keyset). Elimina il `findAll` + N×`getLowestPrice` + filtro JS del legacy.
- `min_price` materializzato qui è l'ottimizzazione chiave: il calcolo pesante avviene al rebuild (a
  evento / notturno), non ad ogni ricerca.
- Volume: ~180 righe pubblicate a regime (le ~10.000 vetrine stub non entrano). Anche con crescita 10×
  resta piccola.
- `ETag` per la card / il dettaglio di una singola vetrina dalla lista; per la lista paginata, cache
  applicativa breve per (query normalizzata).

## Implementazione .NET

`SearchBathhouse` è un **read-model** (proiezione denormalizzata, non una vista SQL): è una tabella vera,
scritta **solo** dal processo di rebuild (sincrono a evento + job notturno), mai dal dominio applicativo
tramite `SaveChanges` di un aggregate. Modulo `Module.Catalog`, area di sola lettura (`Search`), schema
`catalog`. Mappata con `ToTable` (non `ToView`: è materializzata e mantenuta da codice applicativo, non
una `CREATE VIEW`). Chiave naturale `bathhouse_public_id` (= `catalog_bathhouses.public_id`): **nessun**
`id bigint` surrogato, **nessuna** FK reale in uscita (read-model sempre ricostruibile — id copiati per
valore). Non implementa `IAuditable`/`ISoftDeletable` nel senso pieno: ha timestamp ma la visibilità è
governata da `is_published`, non da soft-delete funzionale.

### Entità

```csharp
// Domain/ReadModels/SearchBathhouse.cs
namespace Module.Catalog.Domain.ReadModels;

public sealed class SearchBathhouse
{
    public Guid BathhousePublicId { get; private set; }
    public long PropertyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? City { get; private set; }
    public string? Region { get; private set; }
    public string? Province { get; private set; }
    public string? Slug { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public decimal? MinPrice { get; private set; }
    public List<short> AmenityIds { get; private set; } = new();
    public int LikeCount { get; private set; }
    public bool IsPublished { get; private set; }
    public JsonDocument Card { get; private set; } = JsonDocument.Parse("{}");
    public byte[] ContentHash { get; private set; } = [];
    public DateTime BuiltAt { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
}
```

### `IEntityTypeConfiguration<SearchBathhouse>`

```csharp
// Infrastructure/Configurations/SearchBathhouseConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class SearchBathhouseConfiguration : IEntityTypeConfiguration<SearchBathhouse>
{
    public void Configure(EntityTypeBuilder<SearchBathhouse> builder)
    {
        builder.ToTable("search_bathhouses", "catalog", t => t.HasComment(
            "Read-model di ricerca pubblica: una riga per vetrina pubblicata, ricostruibile da catalog_bathhouses + catalog_pricing_* + catalog_bathhouse_amenities + identity_customer_bathhouse_likes."));

        builder.HasKey(x => x.BathhousePublicId).HasName("pk_catalog_search_bathhouses");
        builder.Property(x => x.BathhousePublicId)
            .HasColumnName("bathhouse_public_id")
            .ValueGeneratedNever()
            .HasComment("Identità della vetrina = catalog_bathhouses.public_id, copiato (no FK: read-model ricostruibile).");

        builder.Property(x => x.PropertyId)
            .HasColumnName("property_id")
            .IsRequired()
            .HasComment("Stabilimento proprietario, copiato — per invalidazione e raggruppamento; no FK.");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired()
            .HasComment("Nome commerciale, per il match testuale e il sort.");

        builder.Property(x => x.City).HasColumnName("city").HasMaxLength(255).HasComment("Città, per i filtri per area.");
        builder.Property(x => x.Region).HasColumnName("region").HasMaxLength(255).HasComment("Regione, per i filtri per area.");
        builder.Property(x => x.Province).HasColumnName("province").HasMaxLength(255).HasComment("Provincia, per i filtri per area.");

        builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .HasMaxLength(255)
            .HasComment("Per costruire il link alla scheda pubblica direttamente dalla card.");

        builder.Property(x => x.Latitude)
            .HasColumnName("latitude")
            .HasComment("Latitudine della vetrina, per la ricerca \"vicino a me\"/entro raggio.");

        builder.Property(x => x.Longitude)
            .HasColumnName("longitude")
            .HasComment("Longitudine della vetrina, per la ricerca \"vicino a me\"/entro raggio.");

        builder.Property(x => x.MinPrice)
            .HasColumnName("min_price")
            .HasColumnType("numeric(12,2)")
            .HasComment("Prezzo minimo corrente prenotabile online, materializzato dal rebuild (evita la query-per-risultato del legacy).");

        builder.Property(x => x.AmenityIds)
            .HasColumnName("amenity_ids")
            .HasColumnType("smallint[]")
            .HasDefaultValueSql("'{}'")
            .IsRequired()
            .HasComment("Id delle dotazioni offerte dalla vetrina, per il filtro \"con questi servizi\".");

        builder.Property(x => x.LikeCount)
            .HasColumnName("like_count")
            .HasDefaultValue(0)
            .IsRequired()
            .HasComment("Numero di \"mi piace\" della vetrina, per il sort \"più popolari\" e la card.");

        builder.Property(x => x.IsPublished)
            .HasColumnName("is_published")
            .HasDefaultValue(false)
            .IsRequired()
            .HasComment("La vetrina è visibile nella ricerca pubblica.");

        builder.Property(x => x.Card)
            .HasColumnName("card")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired()
            .HasComment("Dati non filtrabili per renderizzare la card di risultato (foto, snippet, orari, servizi principali) — JSON derivato, eccezione consentita.");

        builder.Property(x => x.ContentHash)
            .HasColumnName("content_hash")
            .HasMaxLength(32)
            .IsRequired()
            .HasComment("Hash della riga (colonne + card), per ETag sulle risposte di ricerca.");

        builder.Property(x => x.BuiltAt)
            .HasColumnName("built_at")
            .HasDefaultValueSql("now()")
            .IsRequired()
            .HasComment("Timestamp dell'ultimo rebuild della riga.");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Creazione della riga.");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Ultima modifica della riga.");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
            .HasComment("Soft-delete per uniformità; non usato funzionalmente — la visibilità è is_published.");

        builder.HasIndex(x => new { x.Region, x.City })
            .HasFilter("is_published")
            .HasDatabaseName("ix_catalog_search_bathhouses_published_region_city");

        builder.HasIndex(x => x.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasFilter("is_published")
            .HasDatabaseName("ix_catalog_search_bathhouses_published_name_trgm");

        builder.HasIndex(x => x.MinPrice)
            .HasFilter("is_published")
            .HasDatabaseName("ix_catalog_search_bathhouses_published_price");

        builder.HasIndex(x => x.AmenityIds)
            .HasMethod("gin")
            .HasDatabaseName("ix_catalog_search_bathhouses_amenities");

        builder.HasIndex(x => x.PropertyId)
            .HasDatabaseName("ix_catalog_search_bathhouses_property");
    }
}
```

### Relazioni

Nessuna FK EF: `bathhouse_public_id` e `property_id` sono id **copiati per valore** dalle rispettive
entità di dominio (`catalog_bathhouses.public_id`, `catalog_properties.id`) — coerente con la natura di
read-model sempre ricostruibile (§3 del template, caso "riferimento cross-modulo" applicato anche
intra-modulo perché qui non è un vincolo di integrità ma una copia derivata).

### Rebuild e ciclo di vita

- `ISearchBathhouseBuilder.Build(bathhousePublicId)` (Application/Infrastructure) compone colonne + `card`
  + `min_price` (motore prezzo C1/C2 in modalità "minimo del periodo corrente") e scrive la riga con
  `Upsert` (mai tramite change-tracking di un aggregate — è una proiezione, non un'entità di dominio).
- Handler eventi → rebuild **sincrono** della singola riga: `BathhousePublished`, `BathhouseUnpublished`,
  `BathhouseUpdated`, `AmenitiesChanged`, `PriceListChanged`, `BathhouseLikeCountChanged` (da C5).
- Job notturno `RebuildAllSearchBathhouses()` come rete di sicurezza contro drift; comando manuale
  `snapshots rebuild --search` per un rebuild mirato.
- Repository di **sola lettura** con query specifiche (per città/regione, per geo+raggio, per servizi via
  `amenity_ids @> ARRAY[...]`, per fascia di prezzo, per nome via trigram) — nessun metodo di scrittura
  esposto all'Application al di fuori del builder di rebuild.

### Note di migrazione

Nessuna migrazione dati: tabella vuota alla creazione, popolata da un rebuild di massa che itera solo le
vetrine con `catalog_bathhouses.published_at IS NOT NULL` (le ~10.000 vetrine stub non entrano).
