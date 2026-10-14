# ~~Tabella `identity_access_user_resource_scopes`~~ — ELIMINATA il 2026-09-30

> **Questa tabella non esiste più.** È stata collassata nella colonna array
> [`identity_access_users.resource_scopes`](identity_access_users.md) (`text[]`)
> ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §13.1), migration
> `FixRoleFlagDefaultsAndCollapseResourceScopes` di `Module.Identity` (dal 2026-10-06 consolidata nella
> `InitialCreate` — [`docs/42-migration.md` §10](../../../progettazione/42-migration.md)).
>
> La conversione era rimasta «condizionata» il 2026-09-21 perché si perdono `created_at`/`deleted_at`
> per singola assegnazione. Il travaso ha preso solo le assegnazioni attive, ordinate. Il contenuto
> originale resta qui sotto come traccia del disegno precedente — **non è più lo schema corrente**.

---

# Tabella `identity_access_user_resource_scopes`

> **Dominio**: C5 — `identity` · **sottoarea**: `access` · **schema**: `identity` · **nome flat**:
> `identity_access_user_resource_scopes` · **forma canonica**: `identity.access_user_resource_scopes`.
>
> **Nuova** — normalizza il JSON legacy `users.affiliate_selected_locations`. Fonti:
> [`analisi-pre/db/users.md`](../../../analisi-pre/db/users.md) (campi `affiliate_selected_locations`,
> `isAffiliateFlag`), [`analisi-pre/db/roles.md`](../../../analisi-pre/db/roles.md) (ruolo `affiliate`),
> [`docs/03-gestione-dati.md` §2.5](../../../progettazione/03-gestione-dati.md) (no JSON polimorfico),
> [`docs/note.md`](../../../progettazione/note.md) ("i JSON del vecchio schema devono diventare valori").
>
> **Nota sulla voce "Legacy"**: `nome_tabella.campo` di origine; `— (nuovo)` = campo senza
> corrispondente legacy.

---

## Descrizione

Elenca, **una riga per risorsa**, le postazioni/settori della mappa spiaggia su cui un account con
restrizione attiva (`identity_access_users.is_resource_scope_restricted = true`, tipicamente ruolo
`affiliate`) è autorizzato a operare. Se l'account non ha restrizione, **non ha righe qui** e vede
tutto.

Nel legacy questa informazione era l'array JSON `users.affiliate_selected_locations`
(`["R4C18","R4C22"]`) e l'enforcement era **solo lato client** (opacizzazione delle celle sulla mappa).
Nel nuovo modello:

- l'insieme è **relazionale** (interrogabile, indicizzabile, senza riscrivere un blob per aggiungere
  una location);
- l'enforcement passa **server-side** sugli endpoint di prenotazione/gestione.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_identity_access_user_resource_scopes` | `id` | PK | — |
| `uq_..._user_resource` | `(user_id, resource_code)` `WHERE deleted_at IS NULL` | UNIQUE parziale | niente doppioni; una risorsa per utente una volta sola |
| `ix_..._user` | `(user_id) WHERE deleted_at IS NULL` | parziale/FK | "quali risorse può gestire questo utente" (lettura all'apertura mappa) |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.
- **Note**: PK tecnica su tabella di dettaglio (non una PK composita, così la riga è indirizzabile per
  update/delete singolo).

### user_id
- **Legacy**: deriva dalla riga `users` che conteneva l'array `users.affiliate_selected_locations` (cioè `users.id`)
- **Descrizione**: account a cui si applica la restrizione.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`identity_access_users.id`](identity_access_users.md) `ON DELETE CASCADE`.
- **Note**: —.

### resource_code
- **Legacy**: elementi dell'array JSON `users.affiliate_selected_locations` (es. `"R4C18"`)
- **Descrizione**: codice della postazione/settore (griglia mappa spiaggia), es. `R4C18`.
- **Tipo**: `varchar(32)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **chiave naturale copiata** dalla mappa spiaggia di C2 (`catalog`, sottoarea `map`) —
  **nessuna FK cross-modulo**. Coerente con [`docs/03-gestione-dati.md` §3.1ter](../../../progettazione/03-gestione-dati.md).
- **Note**: nel legacy i valori sono codici stringa `R<riga>C<colonna>`; validare il formato in input.
  Se in futuro la mappa espone id stabili per cella, `resource_code` diventa `resource_id uuid` (il
  `public_id` della cella, copiato da C2) — la struttura della tabella non cambia.

### created_at / updated_at
- **Legacy**: — (nuovo; nel legacy la modifica dell'array riscriveva l'intero campo su `users`)
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: §3.3–§3.4.

### deleted_at
- **Legacy**: — (nuovo; nel legacy togliere una location era una riscrittura dell'array, senza storico)
- **Descrizione**: soft-delete della singola assegnazione (`NULL` ⇔ attiva).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: togliere una location all'affiliato = `deleted_at` (mantiene lo storico di *quando*), non
  una riscrittura di array come nel legacy.

## Valutazione JSON

**È il caso d'uso opposto al JSON.** L'array `users.affiliate_selected_locations` era un JSON solo per
accidente di modellazione: valori omogenei, cardinalità bassa, interrogati per appartenenza. Diventano
righe con vincolo di unicità e FK — [`docs/03-gestione-dati.md` §2.5](../../../progettazione/03-gestione-dati.md).

## Suggerimenti per le prestazioni

- Tabella piccolissima (nel dump ~14 utenti coinvolti). L'indice `(user_id) WHERE deleted_at IS NULL`
  copre l'unica lettura calda (all'apertura della mappa da parte di un affiliato).
- Nessun bisogno di indice su `resource_code` da solo (non si cerca "chi può gestire R4C18" nei flussi
  noti; se emergesse, aggiungere `(resource_code, user_id)`).

## Implementazione .NET

Entità figlia dell'aggregato `Account` (modulo `Module.Identity`, sottoarea `Access`, schema
`identity`), gestita come collection navigabile da `Account` (`Account.ResourceScopes`). Implementa
`ISoftDeletable` (togliere una location è un `Delete()`, non una rimozione fisica).

Nessun enum proprio.

### `UserResourceScopeConfiguration`

```csharp
namespace Module.Identity.Infrastructure.Configurations;

public sealed class UserResourceScopeConfiguration : IEntityTypeConfiguration<UserResourceScope>
{
	public void Configure(EntityTypeBuilder<UserResourceScope> builder)
	{
		builder.ToTable("access_user_resource_scopes", "identity", t => t.HasComment(
			"Postazioni/settori della mappa spiaggia su cui un account con restrizione attiva può operare."));

		builder.HasKey(x => x.Id).HasName("pk_identity_access_user_resource_scopes");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.IsRequired()
			.HasComment("Account a cui si applica la restrizione.");

		builder.Property(x => x.ResourceCode)
			.HasColumnName("resource_code")
			.HasMaxLength(32)
			.IsRequired()
			.HasComment("Codice della postazione/settore mappa spiaggia (es. R4C18), copiato da catalog — nessuna FK cross-modulo.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune (§2, §6 del template).

		builder.HasIndex(x => new { x.UserId, x.ResourceCode })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_identity_access_user_resource_scopes_user_resource");

		builder.HasIndex(x => x.UserId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_access_user_resource_scopes_user");

		builder.HasOne(x => x.User)
			.WithMany(u => u.ResourceScopes)
			.HasForeignKey(x => x.UserId)
			.HasConstraintName("fk_identity_access_user_resource_scopes_user")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

`ResourceCode` referenzia la mappa spiaggia di C2 (`catalog`) per **chiave naturale copiata**: solo
`.HasComment`, nessuna FK cross-modulo.

### Autorizzazione

`AuthorizationHandler` (policy-based) che, per un account con `Account.IsResourceScopeRestricted =
true`, verifica server-side che la risorsa della richiesta sia fra le righe vive di questo insieme —
l'enforcement legacy era solo lato client (opacizzazione celle).

### Note di migrazione

- `jsonb_array_elements_text(users.affiliate_selected_locations)` → una riga per elemento, `user_id`
  = `users.id`, `resource_code` = elemento dell'array.
- Solo i ~14 utenti con `isAffiliateFlag = true` e array non vuoto nel dump hanno righe qui.
