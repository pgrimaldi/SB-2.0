# Tabella `notification_marketing_contact_list_members`

> **Dominio**: C6 — `notification` · **sottoarea**: `marketing` · **schema**: `notification` · **nome
> flat**: `notification_marketing_contact_list_members` · **forma canonica**:
> `notification.marketing_contact_list_members`.
>
> Sostituisce [`contact_list_member`](../../../analisi-pre/db/contact_list_member.md) (29 488 righe
> vive in prod, `id` fino a 63 649 → metà hard-deleted; 13% `contactId` `NULL`). Fonti:
> [`analisi-pre/db/contact_list_member.md`](../../../analisi-pre/db/contact_list_member.md),
> [`docs/03-gestione-dati.md` §3.1bis](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §3.6, §4](../../../progettazione/80-retention.md).

---

## Descrizione

**Vera tabella ponte (lista × contatto).** Una riga = "il contatto C è nella lista L". Sostituisce il
pattern legacy "`destroy` di tutti i membri + `bulkCreate`" con inserimento/rimozione **puntuale** e
soft-delete.

Correzioni rispetto al legacy:

- **`contactId` = `customers.id`** (13% `NULL`, nome fuorviante) → `contact_id` FK a
  [`notification_marketing_contacts`](notification_marketing_contacts.md).
- **Nessun `UNIQUE(contactListId, contactId)`** (doppioni possibili) → UNIQUE parziale.
- **Nessuna FK** → FK interne reali verso lista e contatto.
- **Churn degli id** (destroy+recreate per ogni modifica lista) → soft-delete, id stabili.
- **`status int` colonna morta**, **`updated_at` mai aggiornato** → eliminati/gestiti.
- **Nessuna storicizzazione di chi era in quale lista** → `added_at` / `removed_at` (via `deleted_at`).

## Verifica di esclusività

Nessun `is_*` / enum: pura tabella ponte.

## Snapshot

Non applicabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_marketing_contact_list_members` | `id` | PK | — |
| `uq_..._list_contact` | `(list_id, contact_id) WHERE deleted_at IS NULL` | UNIQUE parziale | niente doppioni |
| `ix_..._list` | `(list_id) WHERE deleted_at IS NULL` | btree parziale | membri di una lista (risoluzione destinatari all'invio) |
| `ix_..._contact` | `(contact_id) WHERE deleted_at IS NULL` | btree parziale | "in quali liste è questo contatto" (GDPR, disiscrizione) |

## Campi

### id
- **Legacy**: `contact_list_member.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### list_id
- **Legacy**: `contact_list_member.contactListId`
- **Descrizione**: lista di appartenenza.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`notification_marketing_contact_lists.id`](notification_marketing_contact_lists.md) `ON DELETE CASCADE`.

### contact_id
- **Legacy**: `contact_list_member.contactId` (`customers.id`)
- **Descrizione**: contatto membro della lista.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`notification_marketing_contacts.id`](notification_marketing_contacts.md) `ON DELETE CASCADE`.

### property_id
- **Legacy**: ereditato dalla lista
- **Descrizione**: stabilimento (denormalizzato; coerente con lista e contatto).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: dalla lista.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2.

### added_by
- **Legacy**: dedotto (`createContactList` manuale / `addCustomer` da prenotazione)
- **Descrizione**: come il contatto è entrato nella lista.
- **Tipo**: `enum notification.list_member_source` — valori: `manual`, `booking_auto_enroll`, `import`, `duplicate`.
- **Default**: `manual`.
- **Nullabile**: No.
- **Relazioni**: —.

### added_at
- **Legacy**: `contact_list_member.created_at` (= momento dell'ultima ricreazione della lista)
- **Descrizione**: quando il contatto è stato aggiunto.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: `created_at` / `updated_at` (mai aggiornato) / hard-delete
- **Descrizione**: creazione / modifica / soft-delete (= rimozione del contatto dalla lista, con
  storico). Purge righe soft-deleted da 24 mesi (job `PurgeMarketingSnapshots`).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- Tabella la più grande della sottoarea marketing (~30 k righe vive; molte di più con lo storico
  soft-deleted → purge attivo).
- La risoluzione dei destinatari di una campagna = `JOIN` lista ↔ membri ↔ contatti con filtro
  `consent_status` — coperto dagli indici parziali; nessun `destroy`+`bulkCreate`.
- Inserimenti "aggiungi N contatti a lista" = `INSERT … ON CONFLICT DO NOTHING` (riattiva le righe
  soft-deleted invece di crearne di nuove).

## Implementazione .NET

`ContactListMembership` è l'**entità figlia** (tabella ponte lista × contatto) del modulo
`Module.Notification`, sottoarea `Marketing`, schema `notification`. Non è un owned type EF (`OwnsMany`)
perché ha PK propria, indici propri ed è raggiunta anche da query indipendenti (risoluzione destinatari,
GDPR "in quali liste è questo contatto"); è modellata come entità normale con FK reali verso
`ContactList` e `MarketingContact`, esposta come collezione di navigazione `Members` su `ContactList`.

### Enum proprio

```csharp
// Domain/Enums/ListMemberSource.cs
public enum ListMemberSource
{
	Manual = 0,
	BookingAutoEnroll = 1,
	Import = 2,
	Duplicate = 3
}
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ListMemberSource>("notification", "list_member_source");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<ListMemberSource>("notification.list_member_source");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class ContactListMembershipConfiguration : IEntityTypeConfiguration<ContactListMembership>
{
	public void Configure(EntityTypeBuilder<ContactListMembership> builder)
	{
		builder.ToTable("marketing_contact_list_members", "notification", t => t.HasComment(
			"Tabella ponte: appartenenza di un contatto marketing a una lista, con storico add/remove."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_contact_list_members");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ListId)
			.HasColumnName("list_id")
			.IsRequired()
			.HasComment("Lista di appartenenza.");

		builder.Property(x => x.ContactId)
			.HasColumnName("contact_id")
			.IsRequired()
			.HasComment("Contatto membro della lista.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, denormalizzato dalla lista — nessuna FK cross-schema.");

		builder.Property(x => x.AddedBy)
			.HasColumnName("added_by")
			.IsRequired()
			.HasDefaultValue(ListMemberSource.Manual)
			.HasComment("Come il contatto è entrato nella lista (manuale, auto-iscrizione da prenotazione, import, duplicazione).");

		builder.Property(x => x.AddedAt)
			.HasColumnName("added_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Quando il contatto è stato aggiunto alla lista.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di rimozione del contatto dalla lista (soft-delete, storicizzata).");

		builder.HasIndex(x => new { x.ListId, x.ContactId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_notification_marketing_contact_list_members_list_contact");

		builder.HasIndex(x => x.ListId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_contact_list_members_list");

		builder.HasIndex(x => x.ContactId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_contact_list_members_contact");

		builder.HasOne(x => x.List)
			.WithMany(l => l.Members)
			.HasForeignKey(x => x.ListId)
			.HasConstraintName("fk_notification_marketing_contact_list_members_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Contact)
			.WithMany()
			.HasForeignKey(x => x.ContactId)
			.HasConstraintName("fk_notification_marketing_contact_list_members_contact")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

### Relazioni

- `ListId` → FK reale verso [`notification_marketing_contact_lists`](notification_marketing_contact_lists.md)
  (stesso schema), `ON DELETE CASCADE`.
- `ContactId` → FK reale verso `notification_marketing_contacts` (stesso schema, entità editata in un
  batch parallelo — la FK si applica comunque perché entrambe le tabelle sono in `notification`),
  `ON DELETE CASCADE`.
- `PropertyId`: id copiato da C2, nessuna FK.

### Note

- `ContactListMembership` implementa `ISoftDeletable` e `IAuditable`; **non** è `AggregateRoot` — le
  operazioni add/remove passano dal metodo `ContactList.AddMember(...)` / `RemoveMember(...)`
  sull'aggregate, che solleva `ContactAddedToList` / `ContactRemovedFromList`.
- L'add "puntuale" si implementa con `INSERT … ON CONFLICT (list_id, contact_id) WHERE deleted_at IS NULL
  DO NOTHING` lato SQL generato, oppure — più idiomatico EF — un metodo applicativo che verifica
  l'esistenza di una riga soft-deleted per la stessa coppia e la **riattiva** (`DeletedAt = null`)
  invece di inserirne una nuova, per non rompere l'indice `UNIQUE` parziale e preservare lo storico.
- **Migrazione**: da `contact_list_member`, risolvendo `contactId` → `contact_id` via
  `notification_marketing_contacts` per `(property_id, email)`; righe con `contactId NULL` (13% dei
  dati) o contatto non risolvibile → **scartate** in bonifica pre-migrazione; dedup su
  `(list_id, contact_id)` tenendo la riga più recente; `status` (colonna morta) scartato;
  `created_at` diventa `added_at`.
