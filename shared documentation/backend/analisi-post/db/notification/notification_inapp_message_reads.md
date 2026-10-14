# Tabella `notification_inapp_message_reads`

> **Dominio**: C6 — `notification` · **sottoarea**: `inapp` · **schema**: `notification` · **nome flat**:
> `notification_inapp_message_reads` · **forma canonica**: `notification.inapp_message_reads`.
>
> **Nuova.** Nel legacy lo stato "letta" era il campo `notification_details.read` (`tinyint`) **per
> property, non per utente**: il primo operatore che apriva la campanella azzerava il contatore per
> tutti (`updateNotification` senza `id` → `update({ read: true }, { where: { property_id } })`). Fonti:
> [`analisi-pre/db/notification_details.md`](../../../analisi-pre/db/notification_details.md) (§"criticità"),
> [`docs/03-gestione-dati.md` §2.4quater](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Stato di lettura per (messaggio × utente).** Una riga = "l'operatore U ha visto/aperto la notifica
M". L'assenza di riga = non letta. Il badge "non lette" della campanella è
`count(messaggi visibili) − count(righe qui per l'utente)`.

## Verifica di esclusività

Nessun `is_*` esclusivo: `read_at` e `dismissed_at` sono due momenti indipendenti (visualizzata vs
scartata dall'elenco).

## Snapshot

Non applicabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_inapp_message_reads` | `id` | PK | — |
| `uq_..._message_user` | `(message_id, user_id)` | UNIQUE | una riga per (messaggio, utente) |
| `ix_..._user_recent` | `(user_id, read_at DESC)` | btree | "cosa ho già letto" / calcolo badge |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### message_id
- **Legacy**: `notification_details.id` (la riga stessa portava `read`)
- **Descrizione**: notifica a cui lo stato si riferisce.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`notification_inapp_messages.id`](notification_inapp_messages.md) `ON DELETE CASCADE`.

### user_id
- **Legacy**: — (non esisteva il concetto "per utente")
- **Descrizione**: operatore che ha letto/scartato.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_access_users`](../identity/identity_access_users.md).

### property_id
- **Legacy**: `notification_details.property_id`
- **Descrizione**: stabilimento (denormalizzato dal messaggio; utile per il purge e per lo scoping
  multi-tenant delle query di badge).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: dal messaggio.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2.

### read_at
- **Legacy**: momento in cui `read` passava a `1` (`updatedAt`)
- **Descrizione**: quando l'utente ha aperto/visto la notifica.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### dismissed_at
- **Legacy**: — (non esisteva)
- **Descrizione**: quando l'utente ha rimosso la notifica dal proprio elenco (senza cancellarla per
  gli altri).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / modifica. `deleted_at` esposto per uniformità; queste righe sono
  rimosse in cascata col messaggio (retention 6–12 mesi).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- Tabella potenzialmente grande (messaggi × operatori). L'inserimento è **lazy**: si crea la riga solo
  quando l'utente apre la campanella / clicca la notifica, non a ogni evento.
- "Segna tutte come lette" = un `INSERT … SELECT` delle notifiche visibili senza riga per l'utente,
  `ON CONFLICT DO NOTHING` — non un `UPDATE` globale per property.
- Cascade e retention allineate a `notification_inapp_messages` (stesse partizioni per mese).

## Implementazione .NET

`InAppMessageRead` è un'**entità figlia** di `InAppMessage` nel modulo `Module.Notification` (schema
`notification`): stato di lettura/dismissal per coppia (messaggio, utente), esposta come collezione
`InAppMessage.Reads`. Nessun enum proprio. `user_id` e `property_id` sono id copiati da altri moduli
(C5/C2), nessuna FK; `message_id` è FK reale interna allo schema.

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class InAppMessageReadConfiguration : IEntityTypeConfiguration<InAppMessageRead>
{
	public void Configure(EntityTypeBuilder<InAppMessageRead> builder)
	{
		builder.ToTable("inapp_message_reads", "notification", t => t.HasComment(
			"Stato di lettura per (messaggio × utente): l'assenza di riga significa non letta."));

		builder.HasKey(x => x.Id).HasName("pk_notification_inapp_message_reads");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.MessageId)
			.HasColumnName("message_id")
			.IsRequired()
			.HasComment("Notifica a cui lo stato di lettura si riferisce.");

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.IsRequired()
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema. Operatore che ha letto/scartato.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato dal messaggio per purge e scoping multi-tenant.");

		builder.Property(x => x.ReadAt)
			.HasColumnName("read_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Istante in cui l'utente ha aperto/visto la notifica.");

		builder.Property(x => x.DismissedAt)
			.HasColumnName("dismissed_at")
			.HasComment("Istante in cui l'utente ha rimosso la notifica dal proprio elenco, senza cancellarla per gli altri.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: le righe sono normalmente rimosse in cascata col messaggio, non soft-deleted a mano.");

		builder.HasOne(x => x.Message)
			.WithMany(m => m.Reads)
			.HasForeignKey(x => x.MessageId)
			.HasConstraintName("fk_notification_inapp_message_reads_message")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => new { x.MessageId, x.UserId })
			.IsUnique()
			.HasDatabaseName("uq_notification_inapp_message_reads_message_user");

		builder.HasIndex(x => new { x.UserId, x.ReadAt })
			.HasDatabaseName("ix_notification_inapp_message_reads_user_recent");
	}
}
```

`InAppMessageRead` implementa `IAuditable`; `ISoftDeletable`/Global Query Filter presenti per uniformità
ma il percorso normale di rimozione è la cascade da `InAppMessage`, non un soft-delete applicativo.

### Note di migrazione

- Nessuna riga preesistente da importare 1:1: il concetto "per utente" non esisteva nel legacy
  (`notification_details.read` era un flag per property).
- Scelta di migrazione: **non** creare righe storiche di lettura (opzione (a) fra le due valutate) — si
  riparte con feed "tutto da leggere" per ogni operatore, coerente con la stessa decisione presa su
  `notification_inapp_messages` (nessuna riga qui per le notifiche migrate). L'alternativa (b) —
  approssimare una riga `read` per ogni operatore attivo sulle notifiche con `read = 1` — è stata
  scartata: genererebbe letture "finte" mai avvenute per operatori specifici, fuorviante quanto il bug
  che la nuova tabella corregge.
- `read_at` proviene sempre da `now()` all'inserimento applicativo (lazy, al primo apri/click), mai
  scritto in batch.
