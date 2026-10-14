# Tabella `identity_customer_bathhouse_likes`

> **Dominio**: C5 — `identity` · **sottoarea**: `customer` · **schema**: `identity` · **nome flat**:
> `identity_customer_bathhouse_likes` · **forma canonica**: `identity.customer_bathhouse_likes`.
>
> Rimpiazza la legacy [`property_like_logs`](../../../analisi-pre/db/property_like_logs.md) (**799 righe**
> in produzione; malgrado il nome, la legacy referenziava `bathouses.id`, non `properties.id`). Fonti:
> [`analisi-pre/db/property_like_logs.md`](../../../analisi-pre/db/property_like_logs.md),
> [`analisi-pre/azioni/cliente-finale/09-preferiti-e-condivisione.md`](../../../analisi-pre/azioni/cliente-finale/09-preferiti-e-condivisione.md)
> *(area preferiti)*,
> [`docs/03-gestione-dati.md` §3.1bis, §6 (`bathouses.likeCount` → C5)](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md).
>
> **Nota sulla voce "Legacy"**: `nome_tabella.campo` di origine; `— (nuovo)` = campo senza
> corrispondente legacy.
>
> **Nota su `bathhouse` vs `property`** — indicazione committente: **una `property` (entità gestionale /
> tenant) può raggruppare più `bathhouse`** (singole strutture / vetrine pubbliche), **fin da ora**.
> Il "mi piace" del cliente finale riguarda **una specifica vetrina pubblica** = un `bathhouse`, non
> l'entità gestionale → il riferimento è `bathhouse_id` di tipo **`uuid`** (il `public_id` del
> `bathhouse` copiato da C2), **non** `property_id`.

---

## Descrizione

Registro dei **"mi piace" / preferiti** delle **strutture** (`bathhouse`, vetrine pubbliche) da parte
degli **utenti consumer** (app cliente). Una riga = un utente che ha messo "mi piace" a una struttura.

Correzioni rispetto al legacy:

- **vincolo di unicità `(user_id, bathhouse_id)`** (assente nel legacy → cliccare due volte creava
  righe duplicate e gonfiava il contatore).
- **il "non mi piace" è un `deleted_at`**, non un hard-delete `destroy` (il legacy perdeva ogni
  storico; `likedStatus` era colonna morta, sempre `1`).
- **il contatore denormalizzato `bathouses.likeCount`** (che [`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md)
  assegna a C5 ma la cui *riga* resta in C2) è **per `bathhouse`** — una `property` con N vetrine ha N
  contatori distinti. Va **ricalcolato** da
  `COUNT(*) WHERE bathhouse_id = ? AND deleted_at IS NULL`, non mantenuto con `likeCount ± 1` non
  transazionale e `.catch({})` silenzioso come nel legacy. Esposto verso C2 come proiezione/evento.
- **`bathHouseId` → `bathhouse_id`**: mappatura **diretta** (stesso valore) dal legacy (il legacy puntava già a
  `bathouses.id`) — **nessun** collasso a `property_id`.
- **niente `optionalAuth` con `userId` dal body**: il "mi piace" è sempre a nome dell'utente
  autenticato (il legacy permetteva di mettere/togliere like per conto di altri e alterare il
  contatore).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_identity_customer_bathhouse_likes` | `id` | PK | — |
| `uq_..._user_bathhouse` | `(user_id, bathhouse_id) WHERE deleted_at IS NULL` | UNIQUE parziale | un like per (utente, struttura); elimina i duplicati del legacy |
| `ix_..._user` | `(user_id) WHERE deleted_at IS NULL` | parziale | pagina "i miei preferiti" |
| `ix_..._bathhouse` | `(bathhouse_id) WHERE deleted_at IS NULL` | parziale | ricalcolo `likeCount`, "a chi piace" |

## Campi

### id
- **Legacy**: `property_like_logs.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.
- **Note**: —.

### user_id
- **Legacy**: `property_like_logs.userId` (nullable, senza FK; nella `myfavourite` interpolato via stringa — SQL injection)
- **Descrizione**: utente consumer che ha messo "mi piace".
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No (il legacy lo lasciava nullable, senza FK; qui è obbligatorio e vincolato).
- **Relazioni**: **FK** → [`identity_access_users.id`](identity_access_users.md) `ON DELETE CASCADE`
  (alla chiusura account i preferiti spariscono — [`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md)).
- **Note**: sempre = utente autenticato.

### bathhouse_id
- **Legacy**: `property_like_logs.bathHouseId` (puntava già a `bathouses.id`, non a `properties.id`, malgrado il nome della tabella)
- **Descrizione**: struttura/vetrina pubblica a cui è stato messo "mi piace".
- **Tipo**: `uuid` — il **`public_id`** del `bathhouse` (C2), non il suo `bigint` interno.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** dalla vetrina pubblica di C2 (`catalog`) — nessuna FK
  cross-modulo.
- **Note**: in migrazione `bathHouseId` (bigint legacy) → risolto via mappa
  `bathouses.id → bathhouse.public_id`. Una `property` può avere più `bathhouse` → il like è granulare
  sulla singola struttura.

### liked_at
- **Legacy**: `property_like_logs.createdAt` (nei dati con valori `0000-00-00` per le righe storiche)
- **Descrizione**: quando è stato messo il "mi piace".
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: sostituisce `createdAt` legacy (che nei dati aveva valori `0000-00-00` per le righe
  storiche → in migrazione `NULL`/go-live). Se un utente rimette "mi piace" dopo averlo tolto, si
  **riattiva** la riga (`deleted_at = NULL`, `liked_at = now()`) invece di crearne una nuova.

### created_at / updated_at
- **Legacy**: `property_like_logs.createdAt` / `property_like_logs.updatedAt` (nel model si tentava di disattivarli, ma erano attivi per un bug di `sequelize.define`)
- **Descrizione**: creazione / ultima modifica della riga.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: §3.3–§3.4. `created_at` coincide con il primo `liked_at`.

### deleted_at
- **Legacy**: `property_like_logs.likedStatus` (booleano morto, sempre `1`) — nel legacy il "non mi piace" era un `destroy` (hard-delete), senza storico
- **Descrizione**: soft-delete = **"non mi piace"** (`NULL` ⇔ like attivo).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: il legacy faceva `destroy` (hard-delete) → nessuno storico di quando un utente ha tolto il
  preferito. Qui resta traccia. Retention: hard-delete alla chiusura account o dopo 24 mesi di
  inattività dell'utente ([`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md)).

## Valutazione JSON

Nessun JSON. `property_like_logs.likedStatus` (booleano legacy, sempre `1`, ridondante con l'esistenza
della riga) **non** viene migrato: lo stato "piace" è `deleted_at IS NULL`.

## Suggerimenti per le prestazioni

- Tabella piccola (~800 righe). Gli indici parziali coprono i due accessi (pagina preferiti per
  utente; ricalcolo contatore per struttura).
- **`bathouses.likeCount`** (per `bathhouse`): mantenerlo come **valore derivato** aggiornato da un
  trigger su questa tabella o da un evento di dominio verso C2, con un job di riconciliazione periodico
  (`COUNT`) — mai `± 1` applicativo non transazionale come nel legacy.
- Il vincolo unico parziale rende idempotente il doppio click (nessun bisogno di logica applicativa di
  dedup).

## Implementazione .NET

Entità figlia dell'aggregato `Account` (modulo `Module.Identity`, sottoarea `Customer`, schema
`identity`): `BathhouseLike` non è aggregate root proprio, ma è gestita tramite un servizio applicativo
dedicato (non tutte le operazioni passano per `Account`, essendo un'azione frequente e indipendente dal
resto dell'aggregato account). Implementa `ISoftDeletable`/`IAuditable` (il "non mi piace" è un
`Delete()`, mai un hard-delete).

Nessun enum proprio.

### `BathhouseLikeConfiguration`

```csharp
namespace Module.Identity.Infrastructure.Configurations;

public sealed class BathhouseLikeConfiguration : IEntityTypeConfiguration<BathhouseLike>
{
	public void Configure(EntityTypeBuilder<BathhouseLike> builder)
	{
		builder.ToTable("customer_bathhouse_likes", "identity", t => t.HasComment(
			"Registro dei \"mi piace\" delle strutture (bathhouse) da parte degli utenti consumer."));

		builder.HasKey(x => x.Id).HasName("pk_identity_customer_bathhouse_likes");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.IsRequired()
			.HasComment("Utente consumer che ha messo mi piace.");

		builder.Property(x => x.BathhouseId)
			.HasColumnName("bathhouse_id")
			.IsRequired()
			.HasComment("public_id del bathhouse (catalog) a cui è stato messo mi piace, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.LikedAt)
			.HasColumnName("liked_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Quando è stato messo il mi piace (aggiornato se il like viene riattivato).");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune (§2, §6 del template).

		builder.HasIndex(x => new { x.UserId, x.BathhouseId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_identity_customer_bathhouse_likes_user_bathhouse");

		builder.HasIndex(x => x.UserId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_customer_bathhouse_likes_user");

		builder.HasIndex(x => x.BathhouseId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_customer_bathhouse_likes_bathhouse");

		builder.HasOne(x => x.User)
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.HasConstraintName("fk_identity_customer_bathhouse_likes_user")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

### Servizio applicativo

- Comando `ToggleLike(bathhousePublicId)`, idempotente, con `UserId` **sempre** dal principal
  autenticato — mai dal body come nel legacy (SQL injection via interpolazione stringa su `userId` in
  `myfavourite`, e possibilità di alterare i "mi piace" per conto di altri).
- Se l'utente rimette "mi piace" dopo averlo tolto: riattiva la riga esistente (`DeletedAt = null`,
  `LikedAt = now()`) invece di inserirne una nuova (coerente col vincolo unico parziale).
- Proiezione/evento `BathhouseLikeCountChanged(bathhouseId, delta)` → C2 aggiorna
  `bathouses.likeCount`; job di riconciliazione periodico (`COUNT(*) WHERE bathhouse_id = ? AND
  deleted_at IS NULL`) per correggere eventuali derive, mai un `± 1` non transazionale come nel legacy.

### Note di migrazione

- `bathHouseId` (bigint legacy) → `bathhouse_id` (uuid), mappatura **diretta** (il legacy puntava già a
  `bathouses.id`, non a `properties.id`) via mappa `bathouses.id → bathhouse.public_id`.
- Dedup delle righe legacy duplicate (assenza di vincolo unico nel legacy).
- I `destroy` storici (hard-delete legacy) non sono ricostruibili: le righe superstiti partono come
  like attivi (`deleted_at = NULL`).
- `createdAt` con valori `0000-00-00` → `NULL`/data di go-live.
