# Tabella `switchboard_voice_softphone_devices`

> **Dominio**: C7 — `switchboard` · **sottoarea**: `voice` · **schema**: `switchboard` · **nome flat**:
> `switchboard_voice_softphone_devices` · **forma canonica**: `switchboard.voice_softphone_devices`.
>
> **Redesign** delle colonne `device_fcm_token` / `device_voip_token` di
> [`switchboard_config`](../../../analisi-pre/db/switchboard_config.md) (0 righe su `local_sb`/dump
> di produzione — ma **verificato 2026-09-11**: `device_fcm_token` **valorizzato con un token FCM
> reale** su `summer_booking_dev`, a conferma che il flusso `registerPushToken`/wake-push è stato
> realmente esercitato; `device_voip_token` osservato `NULL`, colonne già correttamente `snake_case` in
> quell'ambiente — il drift camelCase è un'anomalia isolata di `local_sb`, vedi
> [`switchboard_config.md`](../../../analisi-pre/db/switchboard_config.md)). Stesso criterio di C6
> [`notification_push_device_tokens`](../notification/notification_push_device_tokens.md):
> colonna singola senza metadati → **1 riga per dispositivo**.
>
> **Fonti**: [`analisi-pre/db/switchboard_config.md`](../../../analisi-pre/db/switchboard_config.md)
> (§`device_fcm_token` / `device_voip_token`, `registerPushToken`),
> [`analisi-post/db/notification/notification_push_device_tokens.md`](../notification/notification_push_device_tokens.md),
> [`docs/03-gestione-dati.md` §2.4quinquies / §6](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §3.7](../../../progettazione/80-retention.md).

---

## Descrizione

**Una riga = un dispositivo softphone** registrato per ricevere il **push "wake"** che ri-aggiorna la
registrazione SIP prima di inoltrargli una chiamata (`sendWakePush` / `sendVoipPush` in
`handleIncomingCall`). Nel legacy erano due colonne singole su `switchboard_config`
(`device_fcm_token` per Android/FCM, `device_voip_token` per iOS/APNs PushKit): **un solo dispositivo
per stabilimento**, senza data né invalidazione.

Correzioni rispetto al legacy:

- **2 colonne / 1 dispositivo** → tabella figlia, **N dispositivi** per centralino.
- **Nessuna piattaforma tipizzata** (dedotta solo dal nome colonna, non da una colonna dedicata) →
  `token` + `platform` enum.
- **Nessuna scadenza / invalidazione** → `last_seen_at` + `is_active`, disattivazione quando il
  provider risponde `UNREGISTERED` / `InvalidRegistration`.
- **Autenticazione della registrazione** (`registerPushToken` via `sip_username` + `sip_password` in
  chiaro, nessun JWT) → verifica del segreto risolto (vedi
  [`switchboard_configs`](switchboard_configs.md)`.sip_password_secret_ref`) + rate limiting.
- **Retention**: dispositivo non visto da N mesi → 🗑️ (PII di dispositivo, nessun valore).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `platform` | **enum esclusivo** — un token appartiene a una piattaforma (`fcm_android` / `apns_voip` / `web`). |
| `is_active` | booleano; il token disattivato/invalidato non si usa più. Nessun altro flag correlato → nessun `enum[]`. |

## Snapshot

Non applicabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_switchboard_voice_softphone_devices` | `id` | PK | — |
| `uq_..._token` | `(token_hash)` | UNIQUE | un token registrato una sola volta (dedup) |
| `ix_..._config_active` | `(config_id) WHERE is_active AND deleted_at IS NULL` | parziale | i dispositivi da "svegliare" prima dell'inoltro |
| `ix_..._stale` | `(last_seen_at) WHERE is_active` | parziale | purge dei dispositivi inattivi |

## Campi

### id
- **Legacy**: elemento concettuale di `switchboard_config.device_fcm_token` / `.device_voip_token`
- **Descrizione**: PK tecnica interna allo schema.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### config_id
- **Legacy**: `switchboard_config.id` (le colonne token erano sulla stessa riga)
- **Descrizione**: centralino a cui è legato il dispositivo (l'endpoint SIP è per-stabilimento).
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`switchboard_configs`](switchboard_configs.md)`.id`, `ON DELETE CASCADE`.

### property_id
- **Legacy**: `switchboard_config.property_id`
- **Descrizione**: stabilimento (denormalizzato per scoping/retention).
- **Tipo**: `uuid` (= `public_id` di [`catalog_properties`](../catalog/catalog_properties.md)).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: riferimento cross-modulo per valore.

### registered_by_user_id
- **Legacy**: — (la registrazione avveniva via credenziali SIP, senza utente)
- **Descrizione**: operatore C5 che ha registrato il dispositivo, se la registrazione avviene dal
  gestionale autenticato (diagnostica / gestione dispositivi).
- **Tipo**: `uuid` (= `public_id` di [`identity_access_users`](../identity/identity_access_users.md)).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: riferimento cross-modulo per valore.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 1 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### platform
- **Legacy**: dedotta dal nome colonna (`device_fcm_token` → Android; `device_voip_token` → iOS)
- **Descrizione**: piattaforma del dispositivo; determina l'adapter di invio del push wake.
- **Tipo**: `enum switchboard.softphone_push_platform` — valori: `fcm_android`, `apns_voip`, `web`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### token
- **Legacy**: `switchboard_config.device_fcm_token` / `.device_voip_token` (`varchar(255)`)
- **Descrizione**: token di registrazione push del provider (FCM registration token o APNs VoIP
  token). Valore opaco.
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: usato da `IFcmSender` / `IApnsVoipSender`.

### token_hash
- **Legacy**: —
- **Descrizione**: SHA-256 del token, per l'indice `UNIQUE` (i token sono lunghi e variabili).
- **Tipo**: `char(64)`.
- **Default**: calcolato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### device_label
- **Legacy**: —
- **Descrizione**: descrizione leggibile del dispositivo (modello / user-agent abbreviato), per la
  gestione dei dispositivi.
- **Tipo**: `varchar(120)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 1 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### is_active
- **Legacy**: — (i token restavano per sempre)
- **Descrizione**: token utilizzabile. Portato a `false` su `UNREGISTERED` / `InvalidRegistration` o
  rimozione del dispositivo.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### registered_at / last_seen_at
- **Legacy**: — (nessuna data)
- **Descrizione**: prima registrazione / ultimo refresh o ultimo push riuscito.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()`.
- **Nullabile**: No / No.
- **Relazioni**: `last_seen_at` guida il purge.

### last_error
- **Legacy**: — (`.catch(console.error)` sui push)
- **Descrizione**: ultimo errore del provider per questo token.
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 1 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: — (colonne di `switchboard_config`, `timestamps: false`)
- **Descrizione**: creazione / modifica / soft-delete (dispositivo rimosso). Retention: dispositivo
  inattivo da N mesi → hard-delete.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

De-jsonizzazione/de-colonnizzazione: due colonne singole di `switchboard_config` → righe tipizzate con
i metadati che il legacy non aveva (piattaforma, data, stato).

## Suggerimenti per le prestazioni

- Poche righe per centralino (1–2 dispositivi tipici). L'inoltro di una chiamata legge i dispositivi
  attivi del `config_id` (`ix_..._config_active`) e invia il push in **batch** (FCM multicast /
  APNs), non un `await` per token.
- Il purge dei dispositivi stantii (`last_seen_at` vecchio) tiene la tabella minima; i token
  `UNREGISTERED` si disattivano al primo push fallito (feedback provider).

## Implementazione .NET

`VoiceSoftphoneDevice` è un'**entità figlia** (N per centralino) del modulo `Module.Switchboard`, area
`Voice`: registra i dispositivi softphone da "svegliare" con un push prima di inoltrare una chiamata.
Stesso criterio di C6 `notification_push_device_tokens`. 0 righe su `local_sb`/dump di produzione, ma
`device_fcm_token` è **valorizzato con un token FCM reale** su `summer_booking_dev`.

### Enum

```csharp
// Module.Switchboard.Domain.Enums.Voice/SoftphonePushPlatform.cs
public enum SoftphonePushPlatform
{
	FcmAndroid = 0,
	ApnsVoip = 1,
	Web = 2
}
```

```csharp
// SwitchboardDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<SoftphonePushPlatform>("switchboard", "softphone_push_platform");
```

```csharp
// Module.Switchboard.Infrastructure DI
dataSourceBuilder.MapEnum<SoftphonePushPlatform>("switchboard.softphone_push_platform");
```

### Configuration

```csharp
namespace Module.Switchboard.Infrastructure.Configurations.Voice;

public sealed class VoiceSoftphoneDeviceConfiguration : IEntityTypeConfiguration<VoiceSoftphoneDevice>
{
	public void Configure(EntityTypeBuilder<VoiceSoftphoneDevice> builder)
	{
		builder.ToTable("switchboard_voice_softphone_devices", "switchboard", t => t.HasComment(
			"Dispositivo softphone registrato per ricevere il push wake prima dell'inoltro di una chiamata al centralino."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_voice_softphone_devices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.IsRequired()
			.HasComment("Centralino a cui è legato il dispositivo (l'endpoint SIP è per-stabilimento).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping/retention.");

		builder.Property(x => x.RegisteredByUserId)
			.HasColumnName("registered_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema; operatore che ha registrato il dispositivo dal gestionale, se applicabile.");

		builder.Property(x => x.Platform)
			.HasColumnName("platform")
			.HasColumnType("switchboard.softphone_push_platform")
			.IsRequired()
			.HasComment("Piattaforma del dispositivo; determina l'adapter di invio del push wake.");

		builder.Property(x => x.Token)
			.HasColumnName("token")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Token di registrazione push del provider (FCM registration token o APNs VoIP token).");

		builder.Property(x => x.TokenHash)
			.HasColumnName("token_hash")
			.HasMaxLength(64)
			.IsFixedLength()
			.IsRequired()
			.HasComment("SHA-256 del token, usato per l'indice univoco (i token sono lunghi e variabili).");

		builder.Property(x => x.DeviceLabel)
			.HasColumnName("device_label")
			.HasMaxLength(120)
			.HasComment("Descrizione leggibile del dispositivo (modello / user-agent abbreviato).");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Token utilizzabile; portato a false su UNREGISTERED/InvalidRegistration o rimozione del dispositivo.");

		builder.Property(x => x.RegisteredAt)
			.HasColumnName("registered_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Prima registrazione del dispositivo.");

		builder.Property(x => x.LastSeenAt)
			.HasColumnName("last_seen_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultimo refresh o ultimo push riuscito; guida il purge dei dispositivi stantii.");

		builder.Property(x => x.LastError)
			.HasColumnName("last_error")
			.HasColumnType("text")
			.HasComment("Ultimo errore del provider push per questo token.");

		builder.HasIndex(x => x.TokenHash)
			.IsUnique()
			.HasDatabaseName("uq_switchboard_voice_softphone_devices_token");

		builder.HasIndex(x => x.ConfigId)
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_switchboard_voice_softphone_devices_config_active");

		builder.HasIndex(x => x.LastSeenAt)
			.HasFilter("is_active")
			.HasDatabaseName("ix_switchboard_voice_softphone_devices_stale");

		builder.HasOne(x => x.Config)
			.WithMany(c => c.SoftphoneDevices)
			.HasForeignKey(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_voice_softphone_devices_config")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

`VoiceSoftphoneDevice` implementa `ISoftDeletable`/`IAuditable`.

### Note di migrazione

- `RegisterPushToken` (upsert per `token_hash`, aggiorna `last_seen_at`) autenticato dal segreto SIP
  risolto ([`switchboard_configs.sip_password_secret_ref`](switchboard_configs.md)) + rate limiting,
  non dal confronto in chiaro del legacy. `SendWakePush` invia in multicast ai dispositivi attivi del
  `config_id` prima dell'inoltro SIP, disattivando i token che il provider segnala `UNREGISTERED`.
- 0 righe su `local_sb`/dump di produzione → nessun backfill da quelle copie; **1 riga reale** su
  `summer_booking_dev` (`device_fcm_token` valorizzato) → migra come `platform = fcm_android`;
  `device_voip_token` (osservato `NULL`) → nessuna riga; `registered_at`/`last_seen_at` = 
  `switchboard_config.updated_at` (il legacy non tracciava la data, usare la data di creazione della
  riga se disponibile in migrazione).
