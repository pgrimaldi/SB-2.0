# Tabella `notification_push_device_tokens`

> **Dominio**: C6 — `notification` · **sottoarea**: `inapp` (canale push) · **schema**: `notification`
> · **nome flat**: `notification_push_device_tokens` · **forma canonica**:
> `notification.push_device_tokens`.
>
> **Nuova.** Materializza [`users.deviceTokens`](../../../analisi-pre/db/users.md) — la registrazione
> del canale **push** (FCM) usata da `sendCampaignEmail` / `sendAutomationEmail` /
> `sendPushNotification` per gli utenti C5 attivi. [`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md)
> lo assegna a **C6**. Fonti:
> [`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md),
> [`analisi-post/db/identity/identity_access_users.md`](../identity/identity_access_users.md)
> (§"Campi legacy che escono da C5"),
> [`analisi-pre/db/marketing_email_campaign.md`](../../../analisi-pre/db/marketing_email_campaign.md)
> (§"enablePushNotifications").

---

## Descrizione

**Una riga = un token di dispositivo** registrato da un utente C5 per ricevere le push. `users.deviceTokens`
nel legacy era un **array** (JSON/colonna) senza data, senza scadenza, senza tracciamento del
dispositivo — impossibile sapere quali token sono ancora validi. Qui ogni token è una riga con
piattaforma, dispositivo, ultimo uso e stato.

Correzioni rispetto al legacy:

- **Array `deviceTokens` su `users`** → 1 riga per token (`UNIQUE` sul token).
- **Nessuna scadenza / invalidazione** → `last_seen_at` + `is_active` + rimozione dei token che il
  provider segnala come non registrati (`UNREGISTERED` / `InvalidRegistration`).
- **Nessuna piattaforma** → `platform` enum (Android/iOS/Web).
- **Nessuna data di registrazione** → `registered_at`.
- Retention: token non usato da N mesi → 🗑️ (nessun valore, PII di dispositivo).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `platform` | **enum esclusivo**: un token appartiene a una piattaforma. |
| `is_active` | booleano; il token disattivato/invalidato non si usa più. |

## Snapshot

Non applicabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_push_device_tokens` | `id` | PK | — |
| `uq_..._token` | `(token_hash)` | UNIQUE | un token registrato una volta sola (dedup cross-utente: un device che cambia login) |
| `ix_..._user_active` | `(user_id) WHERE is_active AND deleted_at IS NULL` | btree parziale | token attivi di un utente (invio push) |
| `ix_..._stale` | `(last_seen_at) WHERE is_active` | btree parziale | purge dei token inattivi |

## Campi

### id
- **Legacy**: elemento di `users.deviceTokens`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### user_id
- **Legacy**: `users.id` (contenitore dell'array)
- **Descrizione**: utente C5 che ha registrato il token.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_access_users`](../identity/identity_access_users.md).

### property_id
- **Legacy**: `users.propertyId`
- **Descrizione**: stabilimento dell'utente (denormalizzato per scoping/retention).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C2.

### token
- **Legacy**: elemento di `users.deviceTokens`
- **Descrizione**: token di registrazione push del provider (FCM registration token, APNs token, web
  push endpoint). Valore opaco.
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: usato dall'`IFcmSender` / `IApnsSender`.

### token_hash
- **Legacy**: —
- **Descrizione**: SHA-256 del token, per l'indice `UNIQUE` (i token FCM sono lunghi e variabili).
- **Tipo**: `char(64)`.
- **Default**: calcolato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### platform
- **Legacy**: — (non tracciata)
- **Descrizione**: piattaforma del dispositivo.
- **Tipo**: `enum notification.push_platform` — valori: `fcm_android`, `fcm_ios`, `fcm_web`, `apns`,
  `web_push`.
- **Default**: `fcm_android`.
- **Nullabile**: No.
- **Relazioni**: determina l'adapter di invio.

### device_label
- **Legacy**: —
- **Descrizione**: descrizione leggibile del dispositivo (modello / user-agent abbreviato), per far
  gestire i dispositivi all'utente.
- **Tipo**: `varchar(120)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### app_version
- **Legacy**: —
- **Descrizione**: versione dell'app mobile al momento della registrazione (diagnostica).
- **Tipo**: `varchar(20)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### is_active
- **Legacy**: — (i token restavano nell'array per sempre)
- **Descrizione**: token utilizzabile. Portato a `false` quando il provider risponde
  `UNREGISTERED`/`InvalidRegistration` o l'utente rimuove il dispositivo.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### registered_at / last_seen_at
- **Legacy**: —
- **Descrizione**: prima registrazione / ultimo refresh o ultimo invio riuscito.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()`.
- **Nullabile**: No / No.
- **Relazioni**: `last_seen_at` guida il purge dei token stantii.

### last_error
- **Legacy**: — (catch vuoto sulle push, [`docs/01 §8`](../../../progettazione/01-criticita-e-indicazioni-backend.md))
- **Descrizione**: ultimo errore del provider per questo token.
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: — (elementi di array)
- **Descrizione**: creazione / modifica / soft-delete (dispositivo rimosso). Retention: token inattivo
  da N mesi → hard-delete.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

È la **de-jsonizzazione** dell'array `users.deviceTokens`: 1 riga per token, con metadati che l'array
non aveva.

## Suggerimenti per le prestazioni

- Poche righe per utente. L'invio push a un utente = `SELECT token FROM … WHERE user_id = ? AND is_active`.
- Il purge dei token stantii (`last_seen_at` vecchio) tiene la tabella piccola; i token
  `UNREGISTERED` si disattivano al primo invio fallito (feedback FCM/APNs).
- Invio push in **batch** al provider (FCM multicast), non un `await` per token come nel legacy.

## Implementazione .NET

`PushDeviceToken` è un'**entità principale** del modulo `Module.Notification` (schema `notification`):
un token per riga, dedup cross-utente su `token_hash`. Nessuna FK interna (referenzia solo `user_id`/
`property_id` di altri moduli). Consultata da `IFcmSender`/`IApnsSender` per l'invio push in batch.

### Enum propri

```csharp
// Domain/Enums/PushPlatform.cs
public enum PushPlatform { FcmAndroid = 0, FcmIos = 1, FcmWeb = 2, Apns = 3, WebPush = 4 }
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<PushPlatform>("notification", "push_platform");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<PushPlatform>("notification.push_platform");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class PushDeviceTokenConfiguration : IEntityTypeConfiguration<PushDeviceToken>
{
	public void Configure(EntityTypeBuilder<PushDeviceToken> builder)
	{
		builder.ToTable("push_device_tokens", "notification", t => t.HasComment(
			"Un token di dispositivo registrato da un utente C5 per ricevere notifiche push, con piattaforma, ultimo uso e stato."));

		builder.HasKey(x => x.Id).HasName("pk_notification_push_device_tokens");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.IsRequired()
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per scoping/retention.");

		builder.Property(x => x.Token)
			.HasColumnName("token")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Token di registrazione push del provider (FCM/APNs/web push), valore opaco.");

		builder.Property(x => x.TokenHash)
			.HasColumnName("token_hash")
			.HasColumnType("char(64)")
			.IsRequired()
			.HasComment("SHA-256 del token, usato per l'indice UNIQUE (i token FCM sono lunghi e variabili).");
		builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("uq_notification_push_device_tokens_token");

		builder.Property(x => x.Platform)
			.HasColumnName("platform")
			.HasDefaultValue(PushPlatform.FcmAndroid)
			.IsRequired()
			.HasComment("Piattaforma del dispositivo; determina l'adapter di invio.");

		builder.Property(x => x.DeviceLabel)
			.HasColumnName("device_label")
			.HasMaxLength(120)
			.HasComment("Descrizione leggibile del dispositivo (modello/user-agent abbreviato).");

		builder.Property(x => x.AppVersion)
			.HasColumnName("app_version")
			.HasMaxLength(20)
			.HasComment("Versione dell'app mobile al momento della registrazione, per diagnostica.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Token utilizzabile; portato a false quando il provider segnala UNREGISTERED/InvalidRegistration o l'utente rimuove il dispositivo.");

		builder.Property(x => x.RegisteredAt)
			.HasColumnName("registered_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Istante di prima registrazione del token.");

		builder.Property(x => x.LastSeenAt)
			.HasColumnName("last_seen_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Istante dell'ultimo refresh o dell'ultimo invio riuscito; guida il purge dei token stantii.");

		builder.Property(x => x.LastError)
			.HasColumnName("last_error")
			.HasColumnType("text")
			.HasComment("Ultimo errore del provider riportato per questo token.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete: dispositivo rimosso. Retention: token inattivo da N mesi → hard-delete.");

		builder.HasIndex(x => x.UserId)
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_push_device_tokens_user_active");

		builder.HasIndex(x => x.LastSeenAt)
			.HasFilter("is_active")
			.HasDatabaseName("ix_notification_push_device_tokens_stale");
	}
}
```

`PushDeviceToken` implementa `ISoftDeletable` e `IAuditable`.

### Note di migrazione

- Esplodere l'array `users.deviceTokens` in una riga per elemento; `token_hash` calcolato in ETL
  (SHA-256) con dedup: se due utenti condividono lo stesso token (device che ha cambiato login), la
  riga più recente vince e sostituisce quella precedente.
- `platform` non tracciata nel legacy → `FcmAndroid` di default per tutte le righe migrate (la
  maggioranza dell'utenza C5 è su Android); nessuna euristica affidabile disponibile in migrazione.
- `registered_at`/`last_seen_at` → `users.updatedAt` (unico timestamp disponibile, approssimazione).
- `is_active = true` per tutte le righe migrate: i token non più validi verranno potati al primo
  fallimento di invio (`UNREGISTERED`/`InvalidRegistration`), non pre-filtrati in migrazione.
- `device_label`, `app_version`, `last_error` non hanno equivalente legacy → sempre `NULL` per lo
  storico migrato.
