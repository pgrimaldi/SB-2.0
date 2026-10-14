# Tabella `notification_email_channel_settings`

> **Dominio**: C6 — `notification` · **sottoarea**: `email` · **schema**: `notification` · **nome flat**:
> `notification_email_channel_settings` · **forma canonica**: `notification.email_channel_settings`.
>
> Sostituisce [`bathhouse_email_configrations`](../../../analisi-pre/db/bathhouse_email_configrations.md)
> (22 righe in prod, 1 per `propertyId`, ~10 attive) — la configurazione **SMTP in uscita per
> stabilimento**. La tabella sorella legacy `email_configrations` (0 righe, nessun model) **non** si
> migra. Fonti:
> [`analisi-pre/db/bathhouse_email_configrations.md`](../../../analisi-pre/db/bathhouse_email_configrations.md),
> [`docs/03-gestione-dati.md` §2.4quater, §6](../../../progettazione/03-gestione-dati.md),
> [`docs/17-secrets.md`](../../../progettazione/17-secrets.md),
> [`docs/01-…backend.md` §8](../../../progettazione/01-criticita-e-indicazioni-backend.md).

---

> ### ✅ Applicato nel backend (2026-09-29) — `is_verified` eliminato, l'esito è una coppia
>
> La colonna `is_verified` **non esiste più** e `last_verified_at` è stata rinominata
> **`last_verification_at`**. L'esito della verifica SMTP è ora la coppia:
>
> | Colonna | Significato |
> |---|---|
> | `last_verification_at timestamptz NULL` | quando la verifica è stata **tentata** (`NULL` = mai) |
> | `last_verification_error text NULL` | `NULL` = riuscita; testo = motivo del fallimento |
>
> Migration `CollapseRedundantBooleanFlags` (modulo `Module.Notification`), da
> [`docs/07-ottimizzazioni-db-2.md`](../../../progettazione/07-ottimizzazioni-db-2.md) §A.2.
>
> Erano tre colonne per un solo esito: `is_verified` era derivabile come
> `last_verified_at IS NOT NULL AND last_verification_error IS NULL`. Il vecchio nome lasciava anche
> irrisolta l'ambiguità «ultimo tentativo o ultimo successo?», che ora è sciolta dal nome stesso.
>
> **Guadagno di informazione**: l'avviso in gestionale diventa
> `is_active AND (last_verification_at IS NULL OR last_verification_error IS NOT NULL)` e finalmente
> distingue **«mai verificata»** da **«verificata e fallita»**, che con il booleano erano lo stesso stato.
>
> **Travaso**: la migration deduce la data da `updated_at` dove il flag era `true` senza data, e
> registra un esito negativo esplicito dove il flag era `false` con data e senza errore. La rinomina è
> un `RENAME COLUMN`, non un drop+add (EF lo scaffolda come coppia drop/add, che azzererebbe i valori).
> Su `summer_booking_dev`: 20 righe, nessuna mai verificata (il legacy `sendTestEmail` non salvava
> l'esito) — nulla da travasare in questo ambiente.

## Descrizione

Una riga per stabilimento che ha configurato un **mittente email proprio** (dominio/casella dello
stabilimento) al posto di `noreply@summerbooking.it`. Se non configurato → **nessuna riga** (fallback
al mittente di piattaforma). Usato da tutti gli invii `channel = email` del modulo `Notification`.

Correzioni rispetto al legacy:

- **`smtpPassword varchar` in chiaro** (a DB **e** restituita in chiaro da `getEmailConfig`; 13 righe
  condividono la password di piattaforma) → **fuori dalla tabella**: solo `smtp_password_secret_ref`
  (ARN/nome nel secret manager), [`docs/03 §6`](../../../progettazione/03-gestione-dati.md),
  [`docs/17-secrets.md`](../../../progettazione/17-secrets.md). Stesso trattamento di `keysecret` /
  `fiskaly_account`.
- **`secure: true` cablato** in ogni consumatore, `smtpPort`/`smtpSecurity` ignorati → `security` enum
  usato davvero per `secure`/`requireTLS`; `port` coerente.
- **`status tinyint`** (attivo/soft-deleted via `deleteEmailConfig`) → `is_active` + `deleted_at`.
- **`fromName` / `testEmailRecipients` salvati ma mai applicati** → `from_name` usato davvero nel
  `"fromName" <fromEmail>`; `test_recipients` → `text[]` (non JSON), usato dal "invia prova".
- **nessun `UNIQUE(propertyId)`** (unicità solo applicativa) → `UNIQUE (property_id)`.
- **`rejectUnauthorized: false` ripetuto** ([`docs/01 §8`](../../../progettazione/01-criticita-e-indicazioni-backend.md))
  → `verify_certificate` esplicito, `true` di default.
- **campagne/automazioni che ignoravano questa config** (spedivano dal mittente globale) → nel target
  usano il canale email della property se presente.

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `security` | **enum esclusivo**: `none` / `starttls` / `ssl` (valori reali di prod: `SSL` 19, `None` 1). |
| `is_active` | booleano (ex `status`); il soft-delete vero è `deleted_at`. |
| `last_verification_at` + `last_verification_error` | coppia indipendente (esito dell'ultimo "invia prova" / verifica SMTP); non un booleano. |

## Snapshot

Non applicabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_email_channel_settings` | `id` | PK | — |
| `uq_..._property` | `(property_id) WHERE deleted_at IS NULL` | UNIQUE parziale | 1 config per stabilimento |

## Campi

### id
- **Legacy**: `bathhouse_email_configrations.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### property_id
- **Legacy**: `bathhouse_email_configrations.propertyId`
- **Descrizione**: stabilimento a cui la config si applica.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md);
  `UNIQUE` parziale; [D-01](../_domande-committente.md).

### from_email
- **Legacy**: `bathhouse_email_configrations.fromEmail`
- **Descrizione**: indirizzo mittente.
- **Tipo**: `varchar(320)`.
- **Default**: nessuno.
- **Nullabile**: No (nel legacy nullabile; qui la riga esiste solo se il canale è configurato).
- **Relazioni**: —.

### from_name
- **Legacy**: `bathhouse_email_configrations.fromName` (salvato, mai applicato)
- **Descrizione**: nome visualizzato del mittente; usato davvero nel `From` (`"from_name" <from_email>`).
- **Tipo**: `varchar(160)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### smtp_host
- **Legacy**: `bathhouse_email_configrations.smtpServer`
- **Descrizione**: host SMTP (`authsmtp.securemail.pro`, `smtp.gmail.com`, …).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### smtp_port
- **Legacy**: `bathhouse_email_configrations.smtpPort`
- **Descrizione**: porta SMTP (465 TLS implicito, 587 STARTTLS, 25).
- **Tipo**: `integer`.
- **Default**: `465`.
- **Nullabile**: No.
- **Relazioni**: coerenza con `security` validata in applicazione (465 ⇒ `ssl`, 587 ⇒ `starttls`).

### smtp_username
- **Legacy**: `bathhouse_email_configrations.smtpUsername`
- **Descrizione**: utente per l'autenticazione SMTP.
- **Tipo**: `varchar(320)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### smtp_password_secret_ref
- **Legacy**: `bathhouse_email_configrations.smtpPassword` (in chiaro)
- **Descrizione**: **riferimento** alla password nel secret manager (ARN AWS Secrets Manager / nome
  SSM Parameter Store). Nessun materiale segreto in tabella.
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì (config senza auth — raro).
- **Relazioni**: risolto a runtime dall'`ISmtpSender`; mai restituito da API ("mostra una sola volta").

### security
- **Legacy**: `bathhouse_email_configrations.smtpSecurity` (`SSL` / `None`)
- **Descrizione**: modalità di cifratura del canale.
- **Tipo**: `enum notification.smtp_security` — valori: `none`, `starttls`, `ssl`.
- **Default**: `ssl`.
- **Nullabile**: No.
- **Relazioni**: mappa su `secure` / `requireTLS` dell'`ISmtpSender` (non più cablati).

### verify_certificate
- **Legacy**: — (`rejectUnauthorized: false` cablato ovunque)
- **Descrizione**: se validare il certificato TLS del server SMTP.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: `false` ammesso solo con motivazione (server con cert self-signed noto).

### test_recipients
- **Legacy**: `bathhouse_email_configrations.testEmailRecipients` (JSON, sempre `NULL` in prod)
- **Descrizione**: destinatari dell'invio di prova dal pannello.
- **Tipo**: `text[]`.
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: —.

### is_active
- **Legacy**: `bathhouse_email_configrations.status` (`1` attiva / `0` disattivata)
- **Descrizione**: se il canale è usato per gli invii della property.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: se `false` → fallback al mittente di piattaforma.

### last_verification_at / last_verification_error
- **Legacy**: — (`sendTestEmail` non salvava l'esito)
- **Descrizione**: esito dell'ultima verifica SMTP (handshake + auth + invio di prova), come **coppia**:
  `last_verification_at` = quando la verifica è stata **tentata**; `last_verification_error` = `NULL`
  se è riuscita, altrimenti il motivo del fallimento. "Verificata con successo" è quindi
  `last_verification_at IS NOT NULL AND last_verification_error IS NULL` — non c'è un flag dedicato.
- **Tipo**: `timestamptz` · `text`.
- **Default**: `NULL` · `NULL`.
- **Nullabile**: Sì / Sì.
- **Relazioni**: una config `is_active` mai verificata o con verifica fallita genera un avviso nel
  gestionale: `is_active AND (last_verification_at IS NULL OR last_verification_error IS NOT NULL)`.
  I due casi sono distinguibili, cosa che con il flag booleano non era possibile.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 20 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: `createdAt` / `updatedAt` (Sequelize) / `status = 0` (`deleteEmailConfig`)
- **Descrizione**: creazione / modifica / soft-delete.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. `testEmailRecipients` (JSON, mai valorizzato in prod) → `text[]`.

## Suggerimenti per le prestazioni

- ~22 righe totali. Lookup 1:1 su `property_id` (UNIQUE). Costo trascurabile.
- La config + il segreto risolto vanno in **cache in memoria** (TTL breve) nell'`ISmtpSender`, per non
  interrogare secret manager ad ogni email.

## Implementazione .NET

`EmailChannelSettings` è un'**entità principale** (1:1 con lo stabilimento) del modulo
`Module.Notification` (schema `notification`): esiste solo per le property che hanno configurato un
mittente proprio (nessuna riga = fallback al mittente di piattaforma). Nessun evento di dominio; letta
dall'`ISmtpSender` insieme al segreto risolto via `ISecretResolver` (stesso pattern di `api_clients`/
`fiskaly_account` — mai materiale segreto in tabella, solo un riferimento).

### Enum propri

```csharp
// Domain/Enums/SmtpSecurity.cs
public enum SmtpSecurity { None = 0, Starttls = 1, Ssl = 2 }
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<SmtpSecurity>("notification", "smtp_security");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<SmtpSecurity>("notification.smtp_security");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class EmailChannelSettingsConfiguration : IEntityTypeConfiguration<EmailChannelSettings>
{
	public void Configure(EntityTypeBuilder<EmailChannelSettings> builder)
	{
		builder.ToTable("email_channel_settings", "notification", t => t.HasComment(
			"Configurazione SMTP in uscita per stabilimento: mittente proprio al posto di noreply@summerbooking.it."));

		builder.HasKey(x => x.Id).HasName("pk_notification_email_channel_settings");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");
		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_notification_email_channel_settings_property");

		builder.Property(x => x.FromEmail)
			.HasColumnName("from_email")
			.HasMaxLength(320)
			.IsRequired()
			.HasComment("Indirizzo mittente delle email inviate per conto della property.");

		builder.Property(x => x.FromName)
			.HasColumnName("from_name")
			.HasMaxLength(160)
			.HasComment("Nome visualizzato del mittente, usato nel From (\"from_name\" <from_email>).");

		builder.Property(x => x.SmtpHost)
			.HasColumnName("smtp_host")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Host del server SMTP.");

		builder.Property(x => x.SmtpPort)
			.HasColumnName("smtp_port")
			.HasDefaultValue(465)
			.IsRequired()
			.HasComment("Porta SMTP (465 TLS implicito, 587 STARTTLS, 25).");

		builder.Property(x => x.SmtpUsername)
			.HasColumnName("smtp_username")
			.HasMaxLength(320)
			.IsRequired()
			.HasComment("Utente per l'autenticazione SMTP.");

		builder.Property(x => x.SmtpPasswordSecretRef)
			.HasColumnName("smtp_password_secret_ref")
			.HasMaxLength(255)
			.HasComment("Riferimento (ARN/nome) alla password SMTP nel secret manager; nessun materiale segreto in tabella, risolto a runtime.");

		builder.Property(x => x.Security)
			.HasColumnName("security")
			.HasDefaultValue(SmtpSecurity.Ssl)
			.IsRequired()
			.HasComment("Modalità di cifratura del canale SMTP.");

		builder.Property(x => x.VerifyCertificate)
			.HasColumnName("verify_certificate")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Se validare il certificato TLS del server SMTP; false ammesso solo con motivazione.");

		builder.Property(x => x.TestRecipients)
			.HasColumnName("test_recipients")
			.HasColumnType("text[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Destinatari dell'invio di prova dal pannello gestionale.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Se il canale è usato per gli invii della property; false = fallback al mittente di piattaforma.");

		builder.Property(x => x.LastVerificationAt)
			.HasColumnName("last_verification_at")
			.HasComment("Istante dell'ultimo TENTATIVO di verifica SMTP (handshake + auth + invio di prova); NULL = mai verificata.");

		builder.Property(x => x.LastVerificationError)
			.HasColumnName("last_verification_error")
			.HasColumnType("text")
			.HasComment("Dettaglio testuale dell'errore dell'ultima verifica SMTP; NULL con last_verification_at valorizzato = verifica riuscita. Sostituisce il flag is_verified, che era derivabile da questa coppia.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione della configurazione.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete (ex deleteEmailConfig).");
	}
}
```

`EmailChannelSettings` implementa `ISoftDeletable` e `IAuditable`.

### Note di migrazione

- 1:1 dalle 22 righe di `bathhouse_email_configrations` (~10 attive, 12 disattivate).
- `smtpPassword` (testo in chiaro, 13 righe che condividevano la password di piattaforma) → creare un
  secret dedicato per property nel secret manager, salvare solo `smtp_password_secret_ref`; **non**
  scrivere mai il valore in chiaro nei log/script di migrazione.
- `smtpSecurity`: `SSL` → `Ssl`, `None` → `None` (nessun valore legacy mappa a `Starttls`, introdotto per
  configurazioni future con porta 587).
- `status 0` → `deleted_at = updatedAt` (le 12 righe disattivate).
- `smtpPort NULL` → `465` (default).
- `testEmailRecipients` sempre `NULL` in prod → importato come `'{}'`.
- `last_verification_at`/`last_verification_error` non esistevano nel legacy → tutte le righe migrate
  nascono con entrambe `NULL` ("mai verificata"); una verifica va eseguita manualmente dopo il go-live
  per ogni config attiva.
