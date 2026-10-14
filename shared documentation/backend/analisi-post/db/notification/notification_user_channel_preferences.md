# Tabella `notification_user_channel_preferences`

> **Dominio**: C6 — `notification` · **sottoarea**: `inapp` (preferenze di notifica dello staff) ·
> **schema**: `notification` · **nome flat**: `notification_user_channel_preferences` · **forma
> canonica**: `notification.user_channel_preferences`.
>
> **Nuova.** Raccoglie i campi di preferenza notifica che nel legacy stavano su
> [`users`](../../../analisi-pre/db/users.md) e che [`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md)
> assegna a **C6**: `users.notifyEmail`, `users.notifyWhatsapp` (preferenze di canale per le notifiche
> allo staff), `users.disableNotification` (opt-out delle notifiche in-app). Fonti:
> [`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md),
> [`analisi-post/db/identity/identity_access_users.md`](../identity/identity_access_users.md)
> (§"Campi legacy che escono da C5").

---

## Descrizione

**Una riga per utente C5** (staff/gestore) che ha personalizzato come vuole ricevere le notifiche
operative: canali abilitati (in-app / email / WhatsApp / push) e "non disturbare". Se l'utente non ha
mai toccato le impostazioni → **nessuna riga** (valgono i default di modulo).

Correzioni rispetto al legacy:

- **3 flag booleani su `users`** (`notifyEmail`, `notifyWhatsapp`, `disableNotification` — quest'ultimo
  **mai letto dal backend** per filtrare il feed) → un'unica riga di preferenze con `enabled_channels`
  (`enum[]` — combinazioni lecite) + `is_muted`.
- **`disableNotification` letto solo dall'app mobile, ignorato dal server** → nel target il modulo
  `Notification` **rispetta** la preferenza prima di creare la consegna.
- **Nessuna granularità per tipo di evento** → `muted_kinds` (`enum[]`) opzionale per silenziare
  singoli `inapp_kind` / `delivery_kind` senza spegnere tutto.

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `enabled_channels` | **`enum[]`**: i canali di notifica sono flag **dello stesso argomento** che possono essere attivi **insieme** (in-app + email + push) → array, non enum singolo né N booleane ([`docs/03 §2.4quater`](../../../progettazione/03-gestione-dati.md), esempio `pay_*` di `catalog_property_online_booking_settings`). |
| `muted_kinds` | **`enum[]`**: sottoinsieme di tipi silenziati. |
| `is_muted` | booleano (kill-switch globale, ≠ `enabled_channels` vuoto). |

## Snapshot

Non applicabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_user_channel_preferences` | `id` | PK | — |
| `uq_..._user` | `(user_id) WHERE deleted_at IS NULL` | UNIQUE parziale | 1 riga per utente |

## Campi

### id
- **Legacy**: — (nuovo; erano colonne su `users`)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### user_id
- **Legacy**: `users.id`
- **Descrizione**: utente C5 (staff/gestore) a cui le preferenze appartengono.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_access_users`](../identity/identity_access_users.md);
  `UNIQUE` parziale.

### property_id
- **Legacy**: `users.propertyId` (staff legato a una property)
- **Descrizione**: stabilimento dell'utente (denormalizzato per scoping/retention).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: `NULL`.
- **Nullabile**: Sì (utenti multi-property / piattaforma).
- **Relazioni**: **`public_id` (uuid) copiato** da C2.

### enabled_channels
- **Legacy**: `users.notifyEmail` + `users.notifyWhatsapp` (+ in-app implicito) 
- **Descrizione**: canali su cui l'utente vuole ricevere le notifiche operative.
- **Tipo**: `notification.staff_channel[]` — enum: `in_app`, `email`, `whatsapp`, `push`.
- **Default**: `'{in_app}'`.
- **Nullabile**: No.
- **Relazioni**: indicizzabile GIN se in futuro serve "utenti che vogliono le email".

### is_muted
- **Legacy**: `users.disableNotification` (`tinyint`, mai letto dal backend)
- **Descrizione**: "non disturbare" globale — nessuna notifica operativa finché attivo.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: **rispettato dal modulo** (il legacy lo ignorava).

### muted_kinds
- **Legacy**: — (nessuna granularità)
- **Descrizione**: tipi di notifica silenziati singolarmente (es. l'utente vuole tutto tranne
  `payment_received`).
- **Tipo**: `notification.notification_kind[]` (unione di `inapp_kind` + `delivery_kind` rilevanti).
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: —.

### quiet_hours_start / quiet_hours_end
- **Legacy**: —
- **Descrizione**: finestra oraria in cui non recapitare push/WhatsApp (le notifiche restano nel feed
  in-app).
- **Tipo**: `time` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: interpretate nel fuso della property.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 850 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: — (colonne su `users`, nessun timestamp dedicato)
- **Descrizione**: creazione / modifica / soft-delete (di fatto non usato; le preferenze si
  ripristinano ai default cancellando la riga).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun `jsonb`. Le 3 colonne booleane legacy → `enabled_channels` (`enum[]`) + `is_muted`. `enum[]`
scelto (non tabella figlia, non N booleane) perché l'insieme è piccolo, stabile, sempre letto con la
riga, senza metadati per elemento — stesso criterio di `catalog_property_online_booking_settings.enabled_payment_methods`.

## Suggerimenti per le prestazioni

- Riga letta (una per utente) prima di creare una notifica in-app o una consegna verso staff.
  Cache in memoria per `user_id` (invalidata al salvataggio).
- Assenza di riga = default → la maggior parte degli utenti non ha riga (nessun costo).

## Implementazione .NET

`UserChannelPreferences` è un'**entità principale** (1:1 opzionale con lo staff C5) del modulo
`Module.Notification` (schema `notification`): esiste solo per gli utenti che hanno personalizzato le
preferenze (assenza di riga = default di modulo). Consultata da `INotificationPolicy.ShouldDeliver`
prima di creare una notifica in-app o una `Delivery` verso staff.

### Enum propri

```csharp
// Domain/Enums/StaffChannel.cs
public enum StaffChannel { InApp = 0, Email = 1, Whatsapp = 2, Push = 3 }

// Domain/Enums/NotificationKind.cs — unione dei kind silenziabili (inapp_kind + delivery_kind rilevanti)
public enum NotificationKind
{
	BookingCreated = 0, ServiceReservationCreated = 1, PaymentReceived = 2, EreceiptDataError = 3,
	AutoReceiptFailed = 4, ReceiptedBookingMoveBlocked = 5,
	BookingConfirmation = 6, ReservationSummary = 7, PaymentLink = 8, PaymentReceipt = 9,
	EReceipt = 10, NonFiscalReceipt = 11, AgreementReport = 12, WalletStatement = 13,
	LifeguardAttendance = 14
}
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<StaffChannel>("notification", "staff_channel");
modelBuilder.HasPostgresEnum<NotificationKind>("notification", "notification_kind");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<StaffChannel>("notification.staff_channel");
dataSourceBuilder.MapEnum<NotificationKind>("notification.notification_kind");
```

Flag multipli sullo stesso argomento (§4 template, pattern array): `enabled_channels`/`muted_kinds` sono
`List<TEnum>` mappati su array PG nativo, non colonne booleane separate.

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class UserChannelPreferencesConfiguration : IEntityTypeConfiguration<UserChannelPreferences>
{
	public void Configure(EntityTypeBuilder<UserChannelPreferences> builder)
	{
		builder.ToTable("user_channel_preferences", "notification", t => t.HasComment(
			"Preferenze di notifica di un utente C5 (staff/gestore): canali abilitati, non disturbare, tipi silenziati."));

		builder.HasKey(x => x.Id).HasName("pk_notification_user_channel_preferences");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.IsRequired()
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");
		builder.HasIndex(x => x.UserId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_notification_user_channel_preferences_user");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per scoping/retention; NULL per utenti multi-property/piattaforma.");

		builder.Property(x => x.EnabledChannels)
			.HasColumnName("enabled_channels")
			.HasColumnType("notification.staff_channel[]")
			.HasDefaultValueSql("'{in_app}'")
			.IsRequired()
			.HasComment("Canali su cui l'utente vuole ricevere le notifiche operative.");

		builder.Property(x => x.IsMuted)
			.HasColumnName("is_muted")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("\"Non disturbare\" globale: nessuna notifica operativa finché attivo (rispettato dal modulo, il legacy lo ignorava).");

		builder.Property(x => x.MutedKinds)
			.HasColumnName("muted_kinds")
			.HasColumnType("notification.notification_kind[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Tipi di notifica silenziati singolarmente, senza spegnere tutto.");

		builder.Property(x => x.QuietHoursStart)
			.HasColumnName("quiet_hours_start")
			.HasColumnType("time")
			.HasComment("Inizio della finestra oraria in cui non recapitare push/WhatsApp, interpretata nel fuso della property.");

		builder.Property(x => x.QuietHoursEnd)
			.HasColumnName("quiet_hours_end")
			.HasColumnType("time")
			.HasComment("Fine della finestra oraria di non recapito push/WhatsApp.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione delle preferenze.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete; di fatto non usato — si torna ai default cancellando la riga.");
	}
}
```

`UserChannelPreferences` implementa `ISoftDeletable` e `IAuditable`. Tipo C# per `QuietHoursStart`/
`QuietHoursEnd`: `TimeOnly` (mappato nativamente da Npgsql su `time`).

### Note di migrazione

- Da `users`: `notifyEmail`/`notifyWhatsapp` → elementi di `enabled_channels` (sempre con `in_app`
  incluso, comportamento implicito del legacy); `disableNotification = 1` → `is_muted = true`.
- Riga creata **solo** se almeno un valore differisce dal default (`enabled_channels = {in_app}`,
  `is_muted = false`) — evita di materializzare una riga per ogni utente C5 quando la maggioranza non ha
  mai toccato le impostazioni.
- `muted_kinds`, `quiet_hours_start`/`quiet_hours_end` non hanno equivalente legacy → sempre vuoti/`NULL`
  per le righe migrate.
