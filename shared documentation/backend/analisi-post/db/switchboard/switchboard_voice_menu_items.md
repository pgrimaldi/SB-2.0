# Tabella `switchboard_voice_menu_items`

> **Dominio**: C7 — `switchboard` · **sottoarea**: `voice` · **schema**: `switchboard` · **nome flat**:
> `switchboard_voice_menu_items` · **forma canonica**: `switchboard.voice_menu_items`.
>
> **Redesign** di [`switchboard_menu_item`](../../../analisi-pre/db/switchboard_menu_item.md)
> (0 righe in `local_sb` e in produzione — ma **verificato 2026-09-11**: 4 righe reali, un albero di
> test a 2 livelli, su `summer_booking_dev`; vedi la scheda legacy per il dettaglio). Nodo dell'**IVR ad
> albero** del centralino.
>
> **Fonti**: [`analisi-pre/db/switchboard_menu_item.md`](../../../analisi-pre/db/switchboard_menu_item.md),
> [`analisi-pre/db/switchboard_menu_action.md`](../../../analisi-pre/db/switchboard_menu_action.md),
> [`docs/03-gestione-dati.md` §2.4quinquies](../../../progettazione/03-gestione-dati.md),
> [`analisi-pre/azioni/stabilimento/04-gestionale-header-navigazione-account.md`](../../../analisi-pre/azioni/stabilimento/04-gestionale-header-navigazione-account.md).

---

## Descrizione

**Una riga = una voce di menù IVR**, nel contesto di un genitore (`parent_id` auto-referenziale;
`NULL` = voce di primo livello). Alla pressione di un tasto DTMF, il runtime cerca la voce
`(config_id, parent_id = <contesto>, dtmf_key = <cifra>, is_active)` e decide se:
eseguire le [azioni](switchboard_voice_menu_actions.md) (nodo terminale), scendere in un sotto-menù
(voce con figli + audio) o riprodurre l'audio "foglia" e chiudere.

Correzioni rispetto al legacy:

- **Nessun vincolo di unicità** `(switchboard_config_id, parent_id, dtmf_key)` → due voci con lo stesso
  tasto nello stesso livello erano ammesse e l'IVR ne prendeva una arbitraria (`findOne`). Qui
  **`UNIQUE` parziale** su `(config_id, parent_id, dtmf_key) WHERE deleted_at IS NULL`.
- **`is_active smallint 1/0`** → `boolean`. Il legacy filtrava `is_active` **solo** a runtime, non in
  `getMenuTree`; qui il filtro è coerente (o entrambi, o esplicito nella query del pannello).
- **`dtmf_key varchar(2)`** senza vincolo di formato → `varchar(1)` + `CHECK (dtmf_key ~ '^[0-9*#]$')`
  (raccolta con `valid_digits: "0123456789*#"`, `maximum_digits: 1`).
- **Profondità albero implicita** (`getMenuTree` carica fisso 3 livelli) → `depth smallint` calcolato +
  `CHECK (depth BETWEEN 0 AND 4)` per rendere il limite esplicito e uniforme fra pannello e runtime.
- **Input non valido → hangup immediato** (nessun "tasto errato, riprova") → `invalid_input_retries`
  in [`switchboard_configs`](switchboard_configs.md) *(annotazione: non modellato qui, valutare in
  fase implementativa)*.
- **`position`** (ordinamento solo-UI, ignorato dal runtime che seleziona per `dtmf_key`) → mantenuto
  come `sort_order`, con la stessa semantica (solo pannello).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `is_active` | booleano autonomo (voce abilitata/disabilitata). Nessun altro flag correlato → nessun `enum[]`. |
| `dtmf_key` | **non** enum: singolo carattere `[0-9*#]` con `CHECK`, non un vocabolario di dominio. |
| tipo di nodo (sotto-menù / foglia-audio / terminale-azioni) | **non** materializzato come colonna: è **derivato** a runtime da `EXISTS(figli)` + `audio_asset_id IS NOT NULL` + `EXISTS(azioni)`. Se in fase implementativa serve un discriminatore esplicito → `node_kind` enum `{submenu, audio_leaf, action_leaf}` con invariante. |

## Snapshot

Non applicabile: `label` è testo proprio della configurazione (nessuna copia da entità mutabile con FK
viva).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_switchboard_voice_menu_items` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | id pubblico (API pannello `/menu/:menuItemId`) |
| `uq_..._dtmf` | `(config_id, parent_id, dtmf_key) NULLS NOT DISTINCT WHERE deleted_at IS NULL` | UNIQUE parziale | un solo tasto per livello (mancava nel legacy); NB: `parent_id NULL` → usare `coalesce(parent_id, 0)` o `NULLS NOT DISTINCT` ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): a DB mancava perché la causa era EF: due `HasIndex` sulle stesse colonne senza nome sono lo stesso indice, e il secondo sovrascrive il primo; `NULLS NOT DISTINCT` fa collidere anche due voci radice (`parent_id NULL`) con lo stesso tasto. |
| `ix_..._runtime` | `(config_id, parent_id, dtmf_key) WHERE is_active AND deleted_at IS NULL` | parziale | lookup a ogni pressione di tasto (`processDtmfInput`) |
| `ix_..._tree` | `(config_id, parent_id, sort_order) WHERE deleted_at IS NULL` | parziale | resa dell'albero nel pannello (`getMenuTree`) |
| `ix_..._audio` | `(audio_asset_id) WHERE audio_asset_id IS NOT NULL` | parziale | integrità / GC audio |

## Campi

### id
- **Legacy**: `switchboard_menu_item.id`
- **Descrizione**: PK tecnica interna allo schema.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da `parent_id` (auto) e da `menu_item_id` di
  [`switchboard_voice_menu_actions`](switchboard_voice_menu_actions.md).

### public_id
- **Legacy**: — (il legacy usava l'`id` numerico nelle rotte `/menu/:menuItemId`)
- **Descrizione**: id pubblico stabile per l'API del pannello e i deep-link.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato applicativamente.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`; copiato in `final_menu_item_public_id` di
  [`switchboard_voice_call_records`](switchboard_voice_call_records.md).

### config_id
- **Legacy**: `switchboard_menu_item.switchboard_config_id` (FK → `switchboard_config.id`)
- **Descrizione**: centralino a cui appartiene la voce.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`switchboard_configs`](switchboard_configs.md)`.id`, `ON DELETE CASCADE`
  (il legacy cancellava a mano in `deleteConfig`).

### property_id
- **Legacy**: — (ricavabile via `switchboard_config.property_id`)
- **Descrizione**: stabilimento (denormalizzato per scoping multi-tenant senza join).
- **Tipo**: `uuid` (= `public_id` di [`catalog_properties`](../catalog/catalog_properties.md)).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: riferimento cross-modulo per valore.

### parent_id
- **Legacy**: `switchboard_menu_item.parent_id` (`int DEFAULT NULL`, FK auto `_ibfk_5`)
- **Descrizione**: voce genitore; `NULL` = voce di primo livello. Il runtime naviga l'albero usando
  `parent_id` come contesto corrente.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** auto-referenziale → `switchboard_voice_menu_items.id`, `ON DELETE CASCADE`
  (cancellazione ricorsiva dei figli, come `deleteMenuItem`). **Corregge un rischio reale**: nel legacy
  questa FK è `ON DELETE SET NULL` (verificato 2026-09-11 su `summer_booking_dev`), quindi
  un'eliminazione a livello DB orfanerebbe i figli come nuove voci di **primo livello** invece di
  cancellarli — la cascata "ricorsiva" esiste solo lato applicazione, non come vincolo. Qui è imposta a
  livello di schema.

### depth
- **Legacy**: — (implicito: `getMenuTree` carica 3 livelli)
- **Descrizione**: profondità del nodo (0 = radice). Mantenuto coerente in applicazione a ogni
  spostamento; rende esplicito il limite di profondità.
- **Tipo**: `smallint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (depth BETWEEN 0 AND 4)`.

### dtmf_key
- **Legacy**: `switchboard_menu_item.dtmf_key` (`varchar(2) NOT NULL`)
- **Descrizione**: tasto che il chiamante preme per raggiungere la voce nel contesto del genitore.
- **Tipo**: `char(1)` (`.IsFixedLength()` in EF), `CHECK (dtmf_key ~ '^[0-9*#]$')`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: parte dell'`UNIQUE` parziale per livello (`uq_..._dtmf`).

### label
- **Legacy**: `switchboard_menu_item.label` (`varchar(255) NOT NULL`)
- **Descrizione**: etichetta della voce, mostrata nel pannello e riportata nei log di step. Nessun uso
  vocale (l'audio è un file, non TTS).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### audio_asset_id
- **Legacy**: `switchboard_menu_item.audio_file_id` (`int`, FK → `switchboard_audio_file.id`)
- **Descrizione**: audio associato alla voce. Se la voce ha figli **e** audio → riproduce l'audio e
  riapre un `gather` nel sotto-menù; se non ha figli ma ha audio → lo riproduce come "foglia" e chiude;
  se non ha né figli né audio → chiude la chiamata.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`switchboard_media_assets`](switchboard_media_assets.md)`.id`.

### is_active
- **Legacy**: `switchboard_menu_item.is_active` (`smallint NOT NULL DEFAULT 1`)
- **Descrizione**: voce abilitata. Una voce disattivata è invisibile all'IVR (input trattato come DTMF
  non valido).
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### sort_order
- **Legacy**: `switchboard_menu_item.position` (`int NOT NULL DEFAULT 0`)
- **Descrizione**: ordinamento fra voci con lo stesso genitore, **solo** per la resa nel pannello
  (`getMenuTree`); l'IVR seleziona per `dtmf_key`, non per posizione.
- **Tipo**: `integer`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: `switchboard_menu_item` — `timestamps: false`
- **Descrizione**: creazione / modifica / soft-delete (voce rimossa dal pannello). La cancellazione
  ricorsiva dei figli e delle azioni collegate è gestita in applicazione (come `deleteMenuItem`), qui
  supportata da `ON DELETE CASCADE`.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON (il legacy non ne aveva su questa tabella). L'albero è modellato con `parent_id`
auto-referenziale, non con un blob annidato.

## Suggerimenti per le prestazioni

- Pochi nodi per centralino (decine al massimo). Il lookup runtime
  `(config_id, parent_id, dtmf_key) WHERE is_active` è coperto da `ix_..._runtime`.
- `getMenuTree` è una lettura ricorsiva limitata a `depth ≤ 4` (`WITH RECURSIVE` o caricamento a
  livelli) → **cache-abile** con la stessa chiave/invalidazione di `switchboard_configs`
  ([`docs/16-redis.md`](../../../progettazione/16-redis.md)): l'albero cambia solo da pannello.
- L'`UNIQUE` parziale per livello elimina il non-determinismo del `findOne` legacy: a runtime al più
  una riga per `(config_id, parent_id, dtmf_key)`.

## Implementazione .NET

`VoiceMenuItem` è un'**entità figlia** del modulo `Module.Switchboard`, area `Voice`: nodo dell'IVR ad
albero, **auto-referenziale** via `ParentId` (`NULL` = radice). Nessun enum proprio (`dtmf_key` è un
`CHECK` di formato, non un vocabolario; il "tipo di nodo" resta derivato a runtime, non colonna — vedi
§"Verifica di esclusività"). 0 righe su `local_sb`/dump di produzione, ma **4 righe reali** (albero di
test a 2 livelli) su `summer_booking_dev`.

### Configuration

```csharp
namespace Module.Switchboard.Infrastructure.Configurations.Voice;

public sealed class VoiceMenuItemConfiguration : IEntityTypeConfiguration<VoiceMenuItem>
{
	public void Configure(EntityTypeBuilder<VoiceMenuItem> builder)
	{
		builder.ToTable("switchboard_voice_menu_items", "switchboard", t => t.HasComment(
			"Voce di menù dell'IVR ad albero del centralino, nel contesto di un genitore (parent_id auto-referenziale; NULL = primo livello)."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_voice_menu_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile per l'API del pannello e i deep-link.");

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.IsRequired()
			.HasComment("Centralino a cui appartiene la voce.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping multi-tenant senza join.");

		builder.Property(x => x.ParentId)
			.HasColumnName("parent_id")
			.HasComment("Voce genitore; NULL = voce di primo livello. Il runtime naviga l'albero usando questo campo come contesto corrente.");

		builder.Property(x => x.Depth)
			.HasColumnName("depth")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Profondità del nodo (0 = radice), mantenuta coerente in applicazione a ogni spostamento.");

		builder.Property(x => x.DtmfKey)
			.HasColumnName("dtmf_key")
			.HasMaxLength(1)
			.IsFixedLength()
			.IsRequired()
			.HasComment("Tasto che il chiamante preme per raggiungere la voce nel contesto del genitore.");

		builder.Property(x => x.Label)
			.HasColumnName("label")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Etichetta della voce, mostrata nel pannello e riportata nei log di step.");

		builder.Property(x => x.AudioAssetId)
			.HasColumnName("audio_asset_id")
			.HasComment("Audio associato alla voce (foglia o intro del sotto-menù); NULL se la voce non riproduce audio.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Voce abilitata; disattivata è invisibile all'IVR (input trattato come DTMF non valido).");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Ordinamento fra voci con lo stesso genitore, solo per la resa nel pannello — l'IVR seleziona per dtmf_key, non per posizione.");

		builder.ToTable(t =>
		{
			t.HasCheckConstraint("ck_switchboard_voice_menu_items_dtmf_key", "dtmf_key ~ '^[0-9*#]$'");
			t.HasCheckConstraint("ck_switchboard_voice_menu_items_depth", "depth BETWEEN 0 AND 4");
		});

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_switchboard_voice_menu_items_public_id");

		builder.HasIndex(x => new { x.ConfigId, x.ParentId, x.DtmfKey })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_switchboard_voice_menu_items_dtmf");

		builder.HasIndex(x => new { x.ConfigId, x.ParentId, x.DtmfKey })
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_switchboard_voice_menu_items_runtime");

		builder.HasIndex(x => new { x.ConfigId, x.ParentId, x.SortOrder })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_switchboard_voice_menu_items_tree");

		builder.HasIndex(x => x.AudioAssetId)
			.HasFilter("audio_asset_id IS NOT NULL")
			.HasDatabaseName("ix_switchboard_voice_menu_items_audio");

		builder.HasOne(x => x.Config)
			.WithMany(c => c.MenuItems)
			.HasForeignKey(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_voice_menu_items_config")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Parent)
			.WithMany(x => x.Children)
			.HasForeignKey(x => x.ParentId)
			.HasConstraintName("fk_switchboard_voice_menu_items_parent")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.AudioAsset)
			.WithMany()
			.HasForeignKey(x => x.AudioAssetId)
			.HasConstraintName("fk_switchboard_voice_menu_items_audio_asset")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

`VoiceMenuItem` implementa `ISoftDeletable`/`IAuditable`. La FK auto-referenziale `parent_id` è
**`ON DELETE CASCADE`**, non `SET NULL` come nel legacy (verificato 2026-09-11 su
`summer_booking_dev`): quell'invariante correggeva un rischio reale — a livello DB l'eliminazione di un
genitore orfanerebbe i figli come nuove voci di primo livello invece di cancellarli ricorsivamente, la
cascata "ricorsiva" esisteva solo lato applicazione.

`uq_switchboard_voice_menu_items_dtmf` include `parent_id` (nullable) nella chiave: Postgres tratta due
`NULL` come **distinti** in un indice `UNIQUE` per default, quindi due voci di primo livello con lo
stesso `dtmf_key` **non** collidono automaticamente — se serve l'unicità anche fra voci di primo livello
va aggiunta `NULLS NOT DISTINCT` all'indice (Postgres 15+); da confermare in fase implementativa.

### Note di migrazione

- `GetMenuTree` → proiezione ad albero cache-ata (stessa chiave/invalidazione di `switchboard_configs`,
  [`docs/16-redis.md`](../../../progettazione/16-redis.md)), invalidata su qualunque
  `INSERT`/`UPDATE`/`DELETE` del sottoalbero di un `config_id`. `ProcessDtmfInput` fa un lookup singolo
  nodo su `ix_switchboard_voice_menu_items_runtime`, poi decide submenu/audio-leaf/action-leaf per
  derivazione (`EXISTS(figli)` + `audio_asset_id IS NOT NULL` + `EXISTS(azioni)`); un eventuale
  discriminatore esplicito `NodeKind` enum resta un'estensione futura, non presente ora.
- 0 righe legacy → nessun backfill da quelle copie; **4 righe reali** su `summer_booking_dev` (albero
  di test a 2 livelli) migrabili calcolando `depth` risalendo `parent_id` e deduplicando eventuali
  tasti in conflitto per livello (il legacy non aveva vincolo di unicità).
