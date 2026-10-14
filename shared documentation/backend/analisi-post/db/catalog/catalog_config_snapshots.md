# Tabella `catalog_config_snapshots`

> **Dominio**: C2 — `catalog` · **sottoarea**: — (read-model di dominio) · **schema**: `catalog` · **nome
> flat**: `catalog_config_snapshots` · **forma canonica**: `catalog.config_snapshots`.
>
> Tabella **derivata** (nessuna legacy): read-model materializzato in `jsonb` della configurazione di
> stabilimento, servito wholesale al frontend. Fonte di verità = le tabelle normalizzate `catalog_*`;
> questa tabella è **sempre ricostruibile** da quelle. Disegno completo:
> [`_config-snapshot-proposta.md`](../_config-snapshot-proposta.md).
>
> **Nota sulla voce "Legacy"**: `— (nuovo)` ovunque. In migrazione la tabella nasce vuota e viene
> popolata da un rebuild di massa (`snapshots rebuild --all`).

---

> ### ✅ Applicato nel backend (2026-09-29) — `is_stale` sostituito da `stale_since`
>
> La colonna `is_stale` **non esiste più**: al suo posto **`stale_since timestamptz NULL`**
> (`NULL` = aggiornato). Migration `CollapseRedundantBooleanFlags` (modulo `Module.Catalog`), da
> [`docs/07-ottimizzazioni-db-2.md`](../../../progettazione/07-ottimizzazioni-db-2.md) §B.2.
>
> Una colonna al posto di una, con informazione in più: il flag diceva *che* lo snapshot va
> rigenerato, non *da quando*. Ma la decisione che questa scheda descrive — servire un payload
> invalidato con un header di avviso — dipende da **quanto è vecchia** l'invalidazione, non dal fatto
> che esista. «È stale da troppo» = `stale_since < now() - interval '…'` è la soglia su cui alzare
> l'avviso o rifiutare la lettura; con il booleano non era esprimibile.
>
> Nuovo vincolo `ck_catalog_config_snapshots_stale_after_build`:
> `stale_since IS NULL OR stale_since >= built_at` — l'invariante naturale rispetto a `built_at`.
> L'indice `ix_..._stale` passa da `(snapshot_kind) WHERE is_stale` a
> `(stale_since) WHERE stale_since IS NOT NULL`.
>
> Resta vero quanto nota la scheda: con l'aggiornamento **sincrono** la colonna è sempre `NULL`; serve
> solo se un `kind` passa ad aggiornamento asincrono.
>
> **Travaso**: `GREATEST(built_at, updated_at)` dove il flag era `true` (`updated_at` è quando la riga
> è stata toccata per alzare il flag; `built_at` fa da pavimento per rispettare il `CHECK`). Tabella
> vuota in `summer_booking_dev` (0 righe): è un read-model ricostruito dal rebuild di massa, non un
> target dell'ETL legacy.

## Descrizione

Una riga per `(property_id, snapshot_kind)`. Ogni riga contiene il `payload jsonb` pronto per il
frontend, più i metadati per l'aggiornamento incrementale (`stale_since`, `source_hash`), la cache HTTP
(`content_hash` → `ETag`) e il versionamento del contratto (`payload_schema`).

`snapshot_kind` correnti (6): `property_management_config`, `beach_map`, `public_showcase`,
`price_catalog` (listino **proprio** dello stabilimento), `services_catalog`, `agreement_catalog`
(listini/voucher delle **convenzioni** — riga presente **solo** per gli stabilimenti che hanno partner)
— vedi [`_config-snapshot-proposta.md` §4](../_config-snapshot-proposta.md) per il contenuto di ciascuno. Il read-model di **ricerca pubblica** ha una tabella propria
([`catalog_search_bathhouses`](catalog_search_bathhouses.md)) perché è interrogato con filtri/ordinamenti,
non per chiave.

**Aggiornamento** ([`_config-snapshot-proposta.md` §5](../_config-snapshot-proposta.md)): rebuild **sincrono**
nella stessa transazione del comando che modifica una tabella sorgente (evento di dominio → handler →
`rebuild(property_id, kind)`), + job **notturno** di ricostruzione totale come rete di sicurezza, +
utility di rigenerazione manuale.

**URL**: mai completi nel `payload` — ogni file è un `assetRef` `{origin, path}`; la mappa
`origin → base URL` è servita a parte (`GET /config/media-origins`).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_config_snapshots` | `id` | PK | — |
| `uq_catalog_config_snapshots_property_kind` | `(property_id, snapshot_kind)` | UNIQUE | 1 riga per (stabilimento, tipo); chiave di lettura e upsert |
| `ix_catalog_config_snapshots_stale` | `(stale_since) WHERE stale_since IS NOT NULL` | parziale | coda dei rebuild pendenti, in ordine di anzianità (se un kind passa ad async) |
| `ix_catalog_config_snapshots_built_at` | `(built_at)` | — | job notturno / diagnostica "snapshot più vecchi di X" |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### property_id
- **Legacy**: — (nuovo)
- **Descrizione**: stabilimento a cui si riferisce lo snapshot.
- **Tipo**: come il discriminatore tenant del progetto (`bigint` o `uuid` — vedi
  [`_domande-committente.md` D-01](../_domande-committente.md)).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE` (cancellare
  lo stabilimento elimina i suoi snapshot).

### snapshot_kind
- **Legacy**: — (nuovo)
- **Descrizione**: quale vista di configurazione contiene la riga.
- **Tipo**: `enum catalog.config_snapshot_kind` — valori: `property_management_config`, `beach_map`,
  `public_showcase`, `price_catalog`, `services_catalog`, `agreement_catalog`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: aggiungere un tipo = un valore enum + una funzione di build, **nessun DDL** sulla tabella.

### payload
- **Legacy**: — (nuovo; deriva dalle tabelle `catalog_*` di quel `kind`)
- **Descrizione**: il documento JSON servito al frontend.
- **Tipo**: `jsonb`, `NOT NULL`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: composto da `rebuild(property_id, snapshot_kind)`.
- **Note**: chiavi camelCase; importi come numero JSON a 2 decimali; file come `assetRef` `{origin,
  path}`, **mai** URL completi ([§3](../_config-snapshot-proposta.md)); dimensioni tipiche 10–250 KB, soglia
  di allarme ~1 MB.

### payload_schema
- **Legacy**: — (nuovo)
- **Descrizione**: versione della **forma** del `payload` (contratto verso il client). Cambia solo quando
  cambia il codice di serializzazione in modo non retrocompatibile, non quando cambiano i dati.
- **Tipo**: `smallint`.
- **Default**: `1`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: un incremento fa scattare un rebuild di massa; il client tollera la versione corrente e la
  precedente ([§7](../_config-snapshot-proposta.md)).

### source_hash
- **Legacy**: — (nuovo)
- **Descrizione**: hash deterministico degli **input** (righe delle tabelle sorgente rilevanti). Se
  invariato rispetto all'ultimo build → il rebuild salta la riscrittura del `payload`.
- **Tipo**: `bytea` (32 byte, SHA-256) — o `char(64)` hex.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: evita rebuild no-op quando un evento non tocca davvero lo snapshot.

### content_hash
- **Legacy**: — (nuovo)
- **Descrizione**: hash del `payload` corrente. Usato come **`ETag`** nelle risposte HTTP (richieste
  condizionali `If-None-Match` → `304`).
- **Tipo**: `bytea` (32 byte) — o `char(64)` hex.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: precalcolato qui ⇒ generare l'ETag a runtime costa zero
  ([§7](../_config-snapshot-proposta.md)).

### stale_since
- **Legacy**: — (nuovo)
- **Descrizione**: **da quando** un evento ha segnato lo snapshot come da rigenerare, con il rebuild non
  ancora avvenuto; `NULL` = aggiornato (rilevante solo se un `kind` passa ad aggiornamento **asincrono**;
  con l'aggiornamento sincrono resta sempre `NULL`).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: vincolo `ck_catalog_config_snapshots_stale_after_build`:
  `stale_since IS NULL OR stale_since >= built_at`.
- **Note**: il read può servire un payload invalidato con un header di avviso, ma la soglia utile è
  **quanto** è vecchia l'invalidazione (`stale_since < now() - interval '…'`), non il fatto che esista
  — per questo è una data e non un flag. Sostituisce `is_stale` (vedi riquadro in testa alla scheda).

### built_at
- **Legacy**: — (nuovo)
- **Descrizione**: quando il `payload` corrente è stato generato.
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
- **Note**: `updated_at` coincide con `built_at` a ogni rebuild che cambia qualcosa.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: soft-delete (presente per uniformità di progetto).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: **non usato funzionalmente** — una riga snapshot obsoleta si rigenera o si hard-cancella
  (dipende da `catalog_properties` via `ON DELETE CASCADE`). Tabella di puro read-model.

## Valutazione JSON

`payload` **è** `jsonb` — ed è l'**eccezione consentita** alla regola "niente JSON di dominio": è un dato
**derivato**, ricostruibile in ogni momento dalle tabelle normalizzate, mai l'unica copia
([`03-gestione-dati.md` §2.5](../../../progettazione/03-gestione-dati.md), [`note.md`](../../../progettazione/note.md)). Tutto il
resto della riga è tipizzato.

## Suggerimenti per le prestazioni

- Lettura: `WHERE property_id = ? AND snapshot_kind = ?` → PK dell'indice UNIQUE, 1 riga. Con
  `If-None-Match` che porta il `content_hash`, la maggior parte delle richieste finisce in `304` senza
  toccare il `payload`.
- Scrittura: `INSERT … ON CONFLICT (property_id, snapshot_kind) DO UPDATE` idempotente; `built_at`
  avanza; se `source_hash` invariato si salta.
- Il `payload` grande (`beach_map`) beneficia della compressione TOAST di Postgres; considerare
  `ALTER TABLE … SET (toast_tuple_target = …)` solo se il profiling lo indica.
- Se in futuro si aggiunge Redis (evoluzione "Opzione D"), questa tabella resta lo store durevole; Redis
  tiene `config:{property_id}:{kind}` con invalidazione su rebuild.
- Retention: nessuna (fino a 6 righe per stabilimento — `agreement_catalog` solo per i ~17 con partner;
  ~1.000 righe totali a regime). Un `kind` dismesso → drop
  del valore enum + `DELETE WHERE snapshot_kind = …`.

## Implementazione .NET

**Read-model derivato**, non modello di dominio primario: `ConfigSnapshot` non è un aggregate root che
incapsula regole di business, è la riga materializzata che un `ISnapshotBuilder` per `kind` produce a
partire dalle entità di dominio reali (`Property`, `MapSector`/`MapSpot`, `PriceList`, …). Modulo
`Module.Catalog`, schema `catalog`. `payload` è mappato a un **record C# tipizzato** via converter
`System.Text.Json`, non lasciato come `JsonDocument`/stringa opaca.

### Enum proprio

```csharp
// Module.Catalog.Domain.Enums/ConfigSnapshotKind.cs
public enum ConfigSnapshotKind
{
	PropertyManagementConfig = 0,
	BeachMap = 1,
	PublicShowcase = 2,
	PriceCatalog = 3,
	ServicesCatalog = 4,
	AgreementCatalog = 5,
}
```

```csharp
modelBuilder.HasPostgresEnum<ConfigSnapshotKind>("catalog", "config_snapshot_kind");
```

```csharp
dataSourceBuilder.MapEnum<ConfigSnapshotKind>("catalog.config_snapshot_kind");
```

### Payload tipizzato (polimorfico per `snapshot_kind`)

```csharp
// Module.Catalog.Domain.ReadModels/ConfigSnapshotPayload.cs
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[JsonDerivedType(typeof(PropertyManagementConfigPayload), "property_management_config")]
[JsonDerivedType(typeof(BeachMapPayload), "beach_map")]
[JsonDerivedType(typeof(PublicShowcasePayload), "public_showcase")]
[JsonDerivedType(typeof(PriceCatalogPayload), "price_catalog")]
[JsonDerivedType(typeof(ServicesCatalogPayload), "services_catalog")]
[JsonDerivedType(typeof(AgreementCatalogPayload), "agreement_catalog")]
public abstract record ConfigSnapshotPayload;

public sealed record PropertyManagementConfigPayload(/* … campi §4 di _config-snapshot-proposta.md */)
	: ConfigSnapshotPayload;
public sealed record BeachMapPayload(/* … */) : ConfigSnapshotPayload;
public sealed record PublicShowcasePayload(/* … */) : ConfigSnapshotPayload;
public sealed record PriceCatalogPayload(/* … */) : ConfigSnapshotPayload;
public sealed record ServicesCatalogPayload(/* … */) : ConfigSnapshotPayload;
public sealed record AgreementCatalogPayload(/* … */) : ConfigSnapshotPayload;
```

```csharp
// BuildingBlocks.Infrastructure/Persistence/JsonPayloadConverter.cs
public sealed class ConfigSnapshotPayloadConverter : ValueConverter<ConfigSnapshotPayload, string>
{
	public ConfigSnapshotPayloadConverter() : base(
		v => JsonSerializer.Serialize(v, JsonOptions.CamelCaseWebDefaults),
		v => JsonSerializer.Deserialize<ConfigSnapshotPayload>(v, JsonOptions.CamelCaseWebDefaults)!)
	{ }
}
```

```csharp
// Module.Catalog.Domain.Entities/ConfigSnapshot.cs
namespace Module.Catalog.Domain.Entities;

public sealed class ConfigSnapshot : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PropertyId { get; private set; }
	public Property Property { get; private set; } = null!;
	public ConfigSnapshotKind SnapshotKind { get; private set; }
	public ConfigSnapshotPayload Payload { get; private set; } = null!;
	public short PayloadSchema { get; private set; }
	public byte[] SourceHash { get; private set; } = null!;
	public byte[] ContentHash { get; private set; } = null!;
	public DateTime BuiltAt { get; private set; }
	public DateTime? StaleSince { get; private set; }

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	// Scritta solo dal builder di rebuild (§5 _config-snapshot-proposta.md), mai da comandi applicativi generici.
	public void Rebuild(ConfigSnapshotPayload payload, short payloadSchema, byte[] sourceHash,
		byte[] contentHash, DateTime utcNow)
	{
		Payload = payload;
		PayloadSchema = payloadSchema;
		SourceHash = sourceHash;
		ContentHash = contentHash;
		StaleSince = null;
		BuiltAt = utcNow;
	}

	// Idempotente: una seconda invalidazione non sposta in avanti la data della prima, altrimenti
	// "stale da quanto" tornerebbe sempre a zero e la soglia di avviso non scatterebbe mai.
	public void MarkStale(DateTime utcNow) => StaleSince ??= utcNow;

	public void Delete(DateTime utcNow) => DeletedAt = utcNow;
	public void Restore() => DeletedAt = null;
}
```

```csharp
// Module.Catalog.Infrastructure.Configurations/ConfigSnapshotConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class ConfigSnapshotConfiguration : IEntityTypeConfiguration<ConfigSnapshot>
{
	public void Configure(EntityTypeBuilder<ConfigSnapshot> builder)
	{
		builder.ToTable("catalog_config_snapshots", "catalog", t => t.HasComment(
			"Read-model materializzato in jsonb della configurazione di uno stabilimento, servito wholesale al frontend; sempre ricostruibile dalle tabelle catalog_* normalizzate."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_config_snapshots");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento a cui si riferisce lo snapshot.");

		builder.Property(x => x.SnapshotKind)
			.HasColumnName("snapshot_kind")
			.IsRequired()
			.HasComment("Vista di configurazione contenuta nella riga (property_management_config, beach_map, public_showcase, price_catalog, services_catalog, agreement_catalog).");

		builder.Property(x => x.Payload)
			.HasColumnName("payload")
			.HasColumnType("jsonb")
			.HasConversion<ConfigSnapshotPayloadConverter>()
			.IsRequired()
			.HasComment("Documento JSON servito al frontend; chiavi camelCase, importi a 2 decimali, file come assetRef {origin,path} — mai URL completi.");

		builder.Property(x => x.PayloadSchema)
			.HasColumnName("payload_schema")
			.HasColumnType("smallint")
			.IsRequired()
			.HasDefaultValue((short)1)
			.HasComment("Versione della forma del payload (contratto verso il client); incrementata solo da un cambio non retrocompatibile del serializzatore.");

		builder.Property(x => x.SourceHash)
			.HasColumnName("source_hash")
			.HasColumnType("bytea")
			.IsRequired()
			.HasComment("SHA-256 delle righe sorgente rilevanti; invariato ⇒ il rebuild salta la riscrittura del payload.");

		builder.Property(x => x.ContentHash)
			.HasColumnName("content_hash")
			.HasColumnType("bytea")
			.IsRequired()
			.HasComment("SHA-256 del payload corrente, usato come ETag HTTP per le richieste condizionali.");

		builder.Property(x => x.BuiltAt)
			.HasColumnName("built_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Quando il payload corrente è stato generato.");

		builder.Property(x => x.StaleSince)
			.HasColumnName("stale_since")
			.HasComment("Da quando un evento ha segnato lo snapshot da rigenerare, con il rebuild non ancora avvenuto; NULL = aggiornato.");

		builder.ToTable(t => t.HasCheckConstraint(
			"ck_catalog_config_snapshots_stale_after_build",
			"stale_since IS NULL OR stale_since >= built_at"));

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga (coincide con built_at a ogni rebuild che cambia qualcosa).");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete presente per uniformità di progetto; non usato funzionalmente su questa tabella di puro read-model.");

		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_config_snapshots_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => new { x.PropertyId, x.SnapshotKind })
			.IsUnique()
			.HasDatabaseName("uq_catalog_config_snapshots_property_kind");

		builder.HasIndex(x => x.StaleSince)
			.HasFilter("stale_since IS NOT NULL")
			.HasDatabaseName("ix_catalog_config_snapshots_stale");

		builder.HasIndex(x => x.BuiltAt)
			.HasDatabaseName("ix_catalog_config_snapshots_built_at");
	}
}
```

**Note di migrazione**: nessuna sorgente legacy — la tabella nasce **vuota** e si popola con un
`snapshots rebuild --all` eseguito **dopo** il caricamento di tutte le tabelle `catalog_*` normalizzate
(ordine di migrazione: prima i dati di dominio, poi il rebuild dei read-model). `property_id` come
`bigint`/FK reale (coerente con le altre schede del dominio; da riallineare se
[`_domande-committente.md` D-01](../_domande-committente.md) fissa un discriminatore tenant diverso a
livello di progetto). I singoli record `*Payload` restano da rifinire con i campi esatti descritti in
[`_config-snapshot-proposta.md` §4](../_config-snapshot-proposta.md) — qui è fissata solo la meccanica di
mappatura (converter + discriminatore polimorfico), non l'elenco completo dei campi per `kind`. Handler di
dominio (`OnlineBookingSettingsChanged`, `MapSpotsChanged`, `PriceListChanged`, …), `HostedService`
notturno e comando CLI `snapshots rebuild`/`verify` restano application/infrastructure concern esterni a
questa `Configuration` (non EF).
