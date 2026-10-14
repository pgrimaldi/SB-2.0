# Tabella `notification_marketing_contact_lists`

> **Dominio**: C6 — `notification` · **sottoarea**: `marketing` · **schema**: `notification` · **nome
> flat**: `notification_marketing_contact_lists` · **forma canonica**:
> `notification.marketing_contact_lists`.
>
> Sostituisce [`contact_list`](../../../analisi-pre/db/contact_list.md) (125 righe in prod, 99 con
> `isDefaultList = 1` nome `"Marketing"`). Fonti:
> [`analisi-pre/db/contact_list.md`](../../../analisi-pre/db/contact_list.md),
> [`docs/03-gestione-dati.md` §3.1bis](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md).

---

## Descrizione

**Una riga = un segmento di clientela** di uno stabilimento, target di campagne email e automazioni
marketing. I membri sono in [`notification_marketing_contact_list_members`](notification_marketing_contact_list_members.md)
(→ [`notification_marketing_contacts`](notification_marketing_contacts.md)). Ogni property ha (o
dovrebbe avere) **una** lista `is_default` "Marketing", bersaglio dell'auto-iscrizione da prenotazione.

Correzioni rispetto al legacy:

- **`status int` filtrato in lettura ma mai scritto ≠ 1** (il delete era `destroy` fisico) →
  `deleted_at` (soft-delete reale) + purge.
- **Hard delete + churn** (in prod `id` fino a 241 per 125 righe vive; `contact_list_member` `id` fino
  a 63 649 per 29 488 righe) → soft-delete, membri non distrutti-e-ricreati.
- **`updated_at` mai aggiornato** (timestamps disattivati) → gestito.
- **Nessun `UNIQUE(propertyId, isDefaultList=1)`** → UNIQUE parziale su `is_default`.
- **Route senza auth, SQL injection (`id` interpolato)** → fuori scope schema; la scheda annota
  l'enforcement server-side ([`docs/34-multitenant.md`](../../../progettazione/34-multitenant.md)).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `is_default` | booleano; **unico** per `(property_id) WHERE is_default AND deleted_at IS NULL`. |
| `origin` | **enum esclusivo**: `manual` / `default_seed` / `duplicate` (come è nata la lista). |

## Snapshot

Non applicabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_marketing_contact_lists` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | API/URL (`get-contact-list/:id`) |
| `ix_..._property` | `(property_id) WHERE deleted_at IS NULL` | btree parziale | elenco liste di uno stabilimento ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): a DB mancava perché la causa era EF: due `HasIndex` sulle stesse colonne senza nome sono lo stesso indice, e il secondo sovrascrive il primo. |
| `uq_..._default` | `(property_id) WHERE is_default AND deleted_at IS NULL` | UNIQUE parziale | una sola lista di default per property |

## Campi

### id
- **Legacy**: `contact_list.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da
  `notification_marketing_contact_list_members.list_id`,
  `notification_marketing_email_campaigns.contact_list_id`,
  `notification_marketing_automations.contact_list_id`,
  `notification_marketing_events.contact_list_id`.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile (API/URL).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### property_id
- **Legacy**: `contact_list.propertyId`
- **Descrizione**: stabilimento proprietario della lista.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2; [D-01](../_domande-committente.md).

### name
- **Legacy**: `contact_list.name`
- **Descrizione**: nome della lista (`"Marketing"` per la default; `"Seasonal Customer"`, …).
- **Tipo**: `varchar(160)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: il duplicato genera `"<name>_copy[_N]"`.

### is_default
- **Legacy**: `contact_list.isDefaultList` (`int` `0`/`1`)
- **Descrizione**: lista predefinita della property; bersaglio dell'auto-iscrizione da
  `bookings.isMarketingCampaign` (regola del modulo Marketing, non della prenotazione).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE` parziale.

### origin
- **Legacy**: dedotto (`createDefaultContactListAndAutomation` / `duplicateContactList` / `createContactList`)
- **Descrizione**: come è stata creata la lista.
- **Tipo**: `enum notification.contact_list_origin` — valori: `manual`, `default_seed`, `duplicate`.
- **Default**: `manual`.
- **Nullabile**: No.
- **Relazioni**: —.

### member_count
- **Legacy**: — (calcolato con `COUNT` a ogni lettura)
- **Descrizione**: numero di membri vivi (denormalizzato, aggiornato per evento) — per la lista liste
  senza `COUNT` per riga.
- **Tipo**: `integer`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: riconciliato da un job periodico.

### created_by_user_id
- **Legacy**: — (route senza auth)
- **Descrizione**: utente che ha creato la lista.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 125 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: `contact_list.created_at` (mai aggiornato) / `updated_at` (idem) / hard-delete
- **Descrizione**: creazione / modifica / soft-delete. Retention: lista non usata da 24 mesi → 🗑️
  ([`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md)).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- ~125 righe. Nessun indice speciale. `member_count` denormalizzato evita `COUNT` sulla ponte per la
  vista elenco.
- FK in ingresso da campagne/automazioni/eventi: `ON DELETE SET NULL` (come il legacy) — la
  cancellazione lista non rompe le campagne (che comunque usano lo snapshot destinatari).

## Implementazione .NET

`ContactList` è l'**aggregate root** della sottoarea `Marketing` del modulo `Module.Notification`,
schema `notification`. Radice per i membri ([`ContactListMembership`](notification_marketing_contact_list_members.md)),
e riferita da `EmailCampaign`, `MarketingAutomation`, `MarketingEvent` (FK in ingresso, configurate
sulle rispettive entità).

### Enum proprio

```csharp
// Domain/Enums/ContactListOrigin.cs
public enum ContactListOrigin
{
	Manual = 0,
	DefaultSeed = 1,
	Duplicate = 2
}
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ContactListOrigin>("notification", "contact_list_origin");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<ContactListOrigin>("notification.contact_list_origin");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class ContactListConfiguration : IEntityTypeConfiguration<ContactList>
{
	public void Configure(EntityTypeBuilder<ContactList> builder)
	{
		builder.ToTable("marketing_contact_lists", "notification", t => t.HasComment(
			"Segmento di clientela di uno stabilimento, target di campagne e automazioni marketing."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_contact_lists");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico stabile (API/URL).");
		builder.HasIndex(x => x.PublicId).IsUnique()
			.HasDatabaseName("uq_notification_marketing_contact_lists_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Nome della lista (\"Marketing\" per la default, o nome scelto dall'utente).");

		builder.Property(x => x.IsDefault)
			.HasColumnName("is_default")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Lista predefinita della property, bersaglio dell'auto-iscrizione da prenotazione.");

		builder.Property(x => x.Origin)
			.HasColumnName("origin")
			.IsRequired()
			.HasDefaultValue(ContactListOrigin.Manual)
			.HasComment("Come è stata creata la lista: manuale, seed di default, o duplicazione.");

		builder.Property(x => x.MemberCount)
			.HasColumnName("member_count")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Numero di membri vivi, denormalizzato e aggiornato per evento; riconciliato da job periodico.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione della lista.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di soft-delete; NULL se la lista è attiva.");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_contact_lists_property");

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasFilter("is_default AND deleted_at IS NULL")
			.HasDatabaseName("uq_notification_marketing_contact_lists_default");
	}
}
```

### Relazioni

- `Id` è referenziato da `ContactListMembership.ListId`, `EmailCampaign.ContactListId`,
  `MarketingAutomation.ContactListId`, `MarketingEvent.ContactListId` — FK reali configurate sul lato
  "molti" di ciascuna di quelle entità, con `OnDelete(DeleteBehavior.SetNull)` (una lista cancellata non
  spezza campagne/automazioni/eventi, coerente col legacy).
- `PropertyId`, `CreatedByUserId`: id copiati da altri moduli (C2, C5), nessuna FK EF.

### Note

- `ContactList` implementa `ISoftDeletable` e `IAuditable` (plumbing comune, §1/§2 del template).
- L'unicità di `is_default` per property è garantita dall'indice `UNIQUE` parziale; a livello applicativo
  il comando che promuove una lista a default deve prima disattivare l'eventuale default corrente nella
  stessa transazione (nessun `UPSERT` atomico a due righe su un solo statement).
- `MemberCount` aggiornato dai domain event handler `ContactAddedToList` / `ContactRemovedFromList`
  sollevati da `ContactListMembership`; un job di riconciliazione periodico ricalcola da `COUNT(*)` per
  correggere eventuali derive.
- **Migrazione**: 1:1 dalle 125 righe di `contact_list`; `status` scartato (mai diverso da `1` in
  scrittura); `isDefaultList` (`int`) → `is_default` (`boolean`); `origin` dedotto dal nome/pattern di
  creazione (seed "Marketing" → `default_seed`, nome con suffisso `_copy` → `duplicate`, resto →
  `manual`); i membri sono migrati separatamente verso `notification_marketing_contact_list_members`
  con dedup su `(list_id, contact_id)`.
