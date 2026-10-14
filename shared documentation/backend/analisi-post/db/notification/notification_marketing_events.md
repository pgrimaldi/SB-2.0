# Tabella `notification_marketing_events`

> **Dominio**: C6 — `notification` · **sottoarea**: `marketing` · **schema**: `notification` · **nome
> flat**: `notification_marketing_events` · **forma canonica**: `notification.marketing_events`.
>
> Sostituisce [`marketing_event`](../../../analisi-pre/db/marketing_event.md) (39 righe in prod, 4
> `propertyId`; metà eventi reali `Disco Party` / `PISCINA SOTTO LE STELLE`, metà di prova). Fonti:
> [`analisi-pre/db/marketing_event.md`](../../../analisi-pre/db/marketing_event.md),
> [`docs/03-gestione-dati.md` §2.4quater](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md). **Feature marginale** — [D-47](../_domande-committente.md).

---

## Descrizione

**Una riga = un evento/iniziativa dello stabilimento** (serata, torneo, offerta a tema) con nome,
periodo, orario, immagine di copertina, descrizione — da mostrare opzionalmente sulla pagina pubblica
dello stabilimento (proiezione verso la vetrina C2) e da promuovere opzionalmente con reminder a una
lista di contatti ([`notification_marketing_event_reminders`](notification_marketing_event_reminders.md)).

Correzioni rispetto al legacy:

- **Ricorrenza materializzata in N righe** (`createEvent` genera una riga per occorrenza,
  `AUTO_INCREMENT` 4066 per 13 righe; modificare "l'evento" ne modifica una sola) → **la ricorrenza è
  una regola** sull'unica riga evento (`repeat_type` + `repeat_interval` + `repeat_unit` +
  `repeat_until`); le occorrenze si calcolano a runtime.
- **`repeatType` / `repeatUnit`** (`ENUM` nativi MySQL) → enum PG diretti.
- **`repeatInterval` sempre `0`** (loop infinito con `custom`) → `CHECK (repeat_interval >= 1)`.
- **`startHour` / `endHour` `varchar "HH:mm"` / `""`** → `start_time` / `end_time` `time` nullable +
  `is_full_day`.
- **`startDate` / `endDate` `datetime`** con occorrenza = stesso giorno → `starts_on` / `ends_on`
  `date` + orari separati.
- **`showOnEstablishmentPage` senza effetto** (il carosello mostrava tutti gli eventi attivi) →
  `is_public` **usato davvero** come filtro della proiezione verso C2.
- **`notificationsEnabled` + `contactListId` + reminder** = catena senza mittente → nel target o si
  completa con uno scheduler o si dismette la sotto-feature reminder ([D-47](../_domande-committente.md)).
- **`coverImage` URL S3 in colonna** → `cover_image_asset_id` FK a
  [`notification_media_assets`](notification_media_assets.md).
- **`status tinyint`** → `deleted_at` + `is_active`.
- **SQL injection** (`propertyId`/`id` interpolati) → annotato, fuori scope schema.

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `repeat_type` | **enum esclusivo**: `never` / `daily` / `weekly` / `monthly` / `yearly` / `custom`. |
| `repeat_unit` | **enum esclusivo** (solo con `repeat_type = custom`): `day` / `week` / `month` / `year`. |
| `is_full_day` / `is_public` / `reminders_enabled` | **booleani indipendenti** (argomenti diversi: orario, visibilità vetrina, promozione). |

## Snapshot

Non applicabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_marketing_events` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | API/URL + proiezione vetrina |
| `ix_..._property_active` | `(property_id, starts_on) WHERE is_active AND deleted_at IS NULL` | btree parziale | eventi di uno stabilimento per data ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): a DB mancava perché la causa era EF: due `HasIndex` sulle stesse colonne senza nome sono lo stesso indice, e il secondo sovrascrive il primo. |
| `ix_..._public` | `(property_id, starts_on) WHERE is_public AND is_active AND deleted_at IS NULL` | btree parziale | carosello pubblico (ora filtrato davvero) |

## Campi

### id
- **Legacy**: `marketing_event.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da
  `notification_marketing_event_reminders.event_id`.

### public_id
- **Legacy**: — (nuovo)
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`; copiato nella proiezione verso la vetrina C2.

### property_id
- **Legacy**: `marketing_event.propertyId`
- **Descrizione**: stabilimento proprietario.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2; [D-01](../_domande-committente.md).

### name
- **Legacy**: `marketing_event.eventName`
- **Descrizione**: nome dell'evento.
- **Tipo**: `varchar(200)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: il duplicato aggiunge `"_copy"`.

### description
- **Legacy**: `marketing_event.description`
- **Descrizione**: testo descrittivo.
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### starts_on / ends_on
- **Legacy**: `marketing_event.startDate` / `endDate` (`datetime`)
- **Descrizione**: primo giorno / ultimo giorno dell'evento (della **prima** occorrenza se ricorrente).
- **Tipo**: `date` ciascuno.
- **Default**: nessuno.
- **Nullabile**: No / No.
- **Relazioni**: `CHECK (ends_on >= starts_on)`.

### start_time / end_time
- **Legacy**: `marketing_event.startHour` / `endHour` (`varchar "HH:mm"` / `""`)
- **Descrizione**: orario dell'evento.
- **Tipo**: `time` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì (obbligatori se `is_full_day = false`).
- **Relazioni**: —.

### is_full_day
- **Legacy**: `marketing_event.isFullDay`
- **Descrizione**: evento "tutto il giorno".
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: se `true`, `start_time`/`end_time` ignorati.

### repeat_type
- **Legacy**: `marketing_event.repeatType` (`ENUM`)
- **Descrizione**: regola di ricorrenza.
- **Tipo**: `enum notification.event_repeat_type` — valori: `never`, `daily`, `weekly`, `monthly`,
  `yearly`, `custom`.
- **Default**: `never`.
- **Nullabile**: No.
- **Relazioni**: `custom` richiede `repeat_interval` + `repeat_unit`.

### repeat_interval
- **Legacy**: `marketing_event.repeatInterval` (`DEFAULT 1`, sempre `0` nei dati)
- **Descrizione**: ogni quante unità ripetere (solo `repeat_type = custom`).
- **Tipo**: `smallint`.
- **Default**: `1`.
- **Nullabile**: Sì (`NULL` se non `custom`).
- **Relazioni**: `CHECK (repeat_interval IS NULL OR repeat_interval >= 1)`.

### repeat_unit
- **Legacy**: `marketing_event.repeatUnit` (`ENUM('day','week','month','year')`, sempre `NULL`)
- **Descrizione**: unità di ripetizione per `custom`.
- **Tipo**: `enum notification.event_repeat_unit` — valori: `day`, `week`, `month`, `year`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 26 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### repeat_until
- **Legacy**: implicito (`createEvent` generava fino a `endDate`)
- **Descrizione**: data limite oltre cui non generare occorrenze.
- **Tipo**: `date`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`NULL` = nessun limite / evento singolo).
- **Relazioni**: —.

### cover_image_asset_id
- **Legacy**: `marketing_event.coverImage` (URL S3 in colonna)
- **Descrizione**: immagine di copertina.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK interna** → [`notification_media_assets.id`](notification_media_assets.md)
  (`kind = event_cover_image`) `ON DELETE SET NULL`.

### contact_list_id
- **Legacy**: `marketing_event.contactListId` (sempre `NULL`)
- **Descrizione**: lista destinataria dei reminder dell'evento.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK interna** → [`notification_marketing_contact_lists.id`](notification_marketing_contact_lists.md) `ON DELETE SET NULL`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 26 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### is_public
- **Legacy**: `marketing_event.showOnEstablishmentPage` (senza effetto)
- **Descrizione**: se l'evento è mostrato sulla pagina pubblica dello stabilimento — **usato davvero**
  come filtro della proiezione verso C2.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: alimenta un read-model della vetrina (C2), per valore.

### reminders_enabled
- **Legacy**: `marketing_event.notificationsEnabled` (sempre `0`, feature incompleta)
- **Descrizione**: se inviare i reminder pianificati dell'evento.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: richiede `contact_list_id` e almeno una riga in
  `notification_marketing_event_reminders`; scheduler da completare ([D-47](../_domande-committente.md)).

### is_active
- **Legacy**: `marketing_event.status`
- **Descrizione**: evento visibile in gestionale/vetrina.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_by_user_id
- **Legacy**: — (route senza auth)
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 26 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: `createdAt` / `updatedAt` (`timestamps: false`) / `status = 0`
- **Descrizione**: creazione / modifica / soft-delete.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. La ricorrenza (che il legacy materializzava in righe) diventa **campi di regola**; le
occorrenze sono calcolate a runtime (o materializzate in un read-model se serve al calendario, non in
questa tabella).

## Suggerimenti per le prestazioni

- ~39 righe. Un evento ricorrente = **una** riga (non centinaia). Il calendario espande la regola in
  memoria per la finestra richiesta.
- Il carosello pubblico usa l'indice parziale `WHERE is_public AND is_active` — ora `is_public` filtra
  davvero.

## Implementazione .NET

`MarketingEvent` è l'**aggregate root** dell'evento/iniziativa di stabilimento, modulo
`Module.Notification`, sottoarea `Marketing`, schema `notification`. Radice per i reminder
([`EventReminder`](notification_marketing_event_reminders.md)) e sorgente della proiezione verso la
vetrina pubblica C2.

### Enum propri

```csharp
// Domain/Enums/EventRepeatType.cs
public enum EventRepeatType
{
	Never = 0,
	Daily = 1,
	Weekly = 2,
	Monthly = 3,
	Yearly = 4,
	Custom = 5
}

// Domain/Enums/EventRepeatUnit.cs
public enum EventRepeatUnit
{
	Day = 0,
	Week = 1,
	Month = 2,
	Year = 3
}
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<EventRepeatType>("notification", "event_repeat_type");
modelBuilder.HasPostgresEnum<EventRepeatUnit>("notification", "event_repeat_unit");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<EventRepeatType>("notification.event_repeat_type");
dataSourceBuilder.MapEnum<EventRepeatUnit>("notification.event_repeat_unit");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class MarketingEventConfiguration : IEntityTypeConfiguration<MarketingEvent>
{
	public void Configure(EntityTypeBuilder<MarketingEvent> builder)
	{
		builder.ToTable("marketing_events", "notification", t => t.HasComment(
			"Evento/iniziativa di uno stabilimento (serata, torneo, offerta a tema), promuovibile in vetrina e con reminder."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_events");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico stabile (API/URL), copiato anche nella proiezione verso la vetrina C2.");
		builder.HasIndex(x => x.PublicId).IsUnique()
			.HasDatabaseName("uq_notification_marketing_events_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(200)
			.IsRequired()
			.HasComment("Nome dell'evento.");

		builder.Property(x => x.Description)
			.HasColumnName("description")
			.HasColumnType("text")
			.HasComment("Testo descrittivo.");

		builder.Property(x => x.StartsOn)
			.HasColumnName("starts_on")
			.IsRequired()
			.HasComment("Primo giorno dell'evento (della prima occorrenza se ricorrente).");

		builder.Property(x => x.EndsOn)
			.HasColumnName("ends_on")
			.IsRequired()
			.HasComment("Ultimo giorno dell'evento.");

		builder.Property(x => x.StartTime)
			.HasColumnName("start_time")
			.HasColumnType("time")
			.HasComment("Orario di inizio; ignorato se is_full_day.");

		builder.Property(x => x.EndTime)
			.HasColumnName("end_time")
			.HasColumnType("time")
			.HasComment("Orario di fine; ignorato se is_full_day.");

		builder.Property(x => x.IsFullDay)
			.HasColumnName("is_full_day")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Evento \"tutto il giorno\": se vero, start_time/end_time sono ignorati.");

		builder.Property(x => x.RepeatType)
			.HasColumnName("repeat_type")
			.IsRequired()
			.HasDefaultValue(EventRepeatType.Never)
			.HasComment("Regola di ricorrenza dell'evento.");

		builder.Property(x => x.RepeatInterval)
			.HasColumnName("repeat_interval")
			.HasDefaultValue((short)1)
			.HasComment("Ogni quante unità ripetere (solo repeat_type = custom).");

		builder.Property(x => x.RepeatUnit)
			.HasColumnName("repeat_unit")
			.HasComment("Unità di ripetizione per repeat_type = custom (giorno/settimana/mese/anno).");

		builder.Property(x => x.RepeatUntil)
			.HasColumnName("repeat_until")
			.HasComment("Data limite oltre cui non generare occorrenze; NULL = nessun limite / evento singolo.");

		builder.Property(x => x.CoverImageAssetId)
			.HasColumnName("cover_image_asset_id")
			.HasComment("Immagine di copertina dell'evento.");

		builder.Property(x => x.ContactListId)
			.HasColumnName("contact_list_id")
			.HasComment("Lista destinataria dei reminder dell'evento.");

		builder.Property(x => x.IsPublic)
			.HasColumnName("is_public")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se l'evento è mostrato sulla pagina pubblica dello stabilimento (filtra davvero la proiezione verso C2).");

		builder.Property(x => x.RemindersEnabled)
			.HasColumnName("reminders_enabled")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se inviare i reminder pianificati dell'evento.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Evento visibile in gestionale/vetrina.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione dell'evento.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di soft-delete.");

		builder.HasIndex(x => new { x.PropertyId, x.StartsOn })
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_events_property_active");

		builder.HasIndex(x => new { x.PropertyId, x.StartsOn })
			.HasFilter("is_public AND is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_events_public");

		builder.HasOne(x => x.CoverImageAsset)
			.WithMany()
			.HasForeignKey(x => x.CoverImageAssetId)
			.HasConstraintName("fk_notification_marketing_events_cover_image_asset")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(x => x.ContactList)
			.WithMany()
			.HasForeignKey(x => x.ContactListId)
			.HasConstraintName("fk_notification_marketing_events_contact_list")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasCheckConstraint(
			"ck_notification_marketing_events_ends_on", "ends_on >= starts_on");
		builder.HasCheckConstraint(
			"ck_notification_marketing_events_repeat_interval", "repeat_interval IS NULL OR repeat_interval >= 1");
	}
}
```

### Relazioni

- `CoverImageAssetId` → FK reale verso [`notification_media_assets`](notification_media_assets.md)
  (`kind = event_cover_image`, stesso schema), `ON DELETE SET NULL`.
- `ContactListId` → FK reale verso [`notification_marketing_contact_lists`](notification_marketing_contact_lists.md)
  (stesso schema), `ON DELETE SET NULL`.
- `Id` referenziato da `EventReminder.EventId` (collezione `Reminders`, FK reale sul lato figlio).
- `PropertyId`, `CreatedByUserId`: id copiati da altri moduli (C2, C5), nessuna FK.

### Note

- `MarketingEvent` implementa `ISoftDeletable` e `IAuditable`.
- `StartTime`/`EndTime` mappati a `TimeOnly?`, `StartsOn`/`EndsOn`/`RepeatUntil` a `DateOnly`/`DateOnly?`
  (Npgsql nativo su `time`/`date`).
- Value object `RecurrenceRule` (non persistito come colonne proprie ulteriori, ma incapsula
  `RepeatType`/`RepeatInterval`/`RepeatUnit`/`RepeatUntil` con il metodo `ExpandOccurrences(DateRange)`)
  usato dal calendario gestionale per espandere le occorrenze **in memoria**; nessuna riga per
  occorrenza in DB (correzione rispetto al legacy).
- Proiezione `PublicEvent` verso C2 (read-model vetrina), alimentata solo dagli eventi con
  `IsPublic = true`, `IsActive = true`, `DeletedAt = null`.
- **Migrazione**: le righe-occorrenza legacy (una per data generata da `createEvent`) vanno
  **collassate** in un'unica riga-evento per gruppo, raggruppando per
  `(propertyId, eventName, repeatType, finestra temporale contigua)`; `repeatInterval = 0` (sempre nei
  dati) → `RepeatInterval = 1`; `startHour`/`endHour` `""` → `NULL` + `IsFullDay = true`; `coverImage`
  (URL S3) → riga in `notification_media_assets` + FK; `status` (`tinyint`) → `IsActive` +
  `DeletedAt`.
