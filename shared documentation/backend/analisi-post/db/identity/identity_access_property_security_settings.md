# Tabella `identity_access_property_security_settings`

> **Dominio**: C5 — `identity` · **sottoarea**: `access` · **schema**: `identity` · **nome flat**:
> `identity_access_property_security_settings` · **forma canonica**:
> `identity.access_property_security_settings`.
>
> **Nuova** — accoglie i campi di **sicurezza dell'account a livello di stabilimento** che
> [`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md) assegna a C5 pur vivendo, nel legacy,
> su tabelle di altri domini. Fonti:
> [`analisi-pre/db/users.md`](../../../analisi-pre/db/users.md) (`two_factor_otp`/`expiry`,
> nota "2FA a livello di stabilimento"),
> [`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md) (`bathouses.isTwoFactorEnabled` → C5),
> [`docs/01-...backend.md` §7](../../../progettazione/01-criticita-e-indicazioni-backend.md) (OTP 2FA debole),
> [`docs/pre/autenticazione-autorizzazione-dotnet.md`](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md).
>
> **Nota sulla voce "Legacy"**: `nome_tabella.campo` di origine; `— (nuovo)` = campo senza
> corrispondente legacy.

---

## Descrizione

**Una riga per stabilimento**. Raccoglie le impostazioni di sicurezza che si applicano a **tutto lo
staff** di quella property. Oggi contiene solo l'interruttore 2FA (nel legacy
`bathouses.isTwoFactorEnabled`, dominio C2 vetrina — ma è una scelta di sicurezza dell'account, non
della pagina pubblica), predisposta per future opzioni (durata sessione, policy password, lista IP
consentiti, ecc.) senza toccare né `bathouses` né `identity_access_users`.

Gli account `consumer` non sono coinvolti (non appartengono a una property).

> **`property` con più `bathhouse`** (indicazione committente): una `property` può raggruppare più
> strutture pubbliche. Il 2FA vale per **tutto lo staff della `property`** (che appartiene a
> `property_id`, non a un singolo `bathhouse`) → questa tabella resta **per `property`** (1 riga per
> `property_id`). Il flag legacy `bathouses.isTwoFactorEnabled` è invece per `bathhouse`: in migrazione
> si **aggrega** (`bool_or`) sui `bathhouse` della stessa `property`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_identity_access_property_security_settings` | `id` | PK | — |
| `uq_..._property` | `property_id` | UNIQUE | 1 riga per stabilimento |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.
- **Note**: —.

### property_id
- **Legacy**: — (nuovo come colonna; nel legacy la riga equivalente è `bathouses`, identificata da `bathouses.id`)
- **Descrizione**: `property` (entità gestionale / tenant) a cui si applicano le impostazioni.
- **Tipo**: `uuid` — il **`public_id`** della `property` (C2), non il suo `bigint` interno.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 (`catalog`) — nessuna FK cross-modulo; `UNIQUE`.
- **Note**: discriminatore tenant.

### is_two_factor_enabled
- **Legacy**: `bathouses.isTwoFactorEnabled` (dominio C2 → C5 per [`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md); **per `bathhouse`** nel legacy → aggregato **per `property`** qui, `bool_or` sui bathhouse della property; da non confondere con la colonna morta `bathouses.isTwoFactorAuthentication`)
- **Descrizione**: se `true`, al login **dello staff** di questo stabilimento (ruoli non `consumer`)
  viene richiesto un secondo fattore (OTP via email) gestito da
  [`identity_access_user_auth_challenges`](identity_access_user_auth_challenges.md).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: erede di `bathouses.isTwoFactorEnabled`. Nel dump nessuno stabilimento lo ha attivo. Da
  chiarire col committente ([`analisi-pre/db/users.md`](../../../analisi-pre/db/users.md)) se il 2FA debba
  restare una **politica per stabilimento** (come oggi) o diventare configurabile per singolo account —
  in quel caso il flag passerebbe (anche) su `identity_access_users`.

### two_factor_channel
- **Legacy**: — (nuovo; nel legacy il canale era implicitamente l'email, cablato nel codice `sendOtpOnMail`)
- **Descrizione**: canale del secondo fattore.
- **Tipo**: enum PG `identity.second_factor_channel` — valori: `email` *(unico oggi)*, `sms`,
  `totp` *(predisposti)*.
- **Default**: `'email'`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: la generazione/invio effettivi del codice sono responsabilità del canale (C6 per l'email);
  qui si tiene solo la **scelta**.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / ultima modifica della riga.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: §3.3–§3.4.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: soft-delete (`NULL` ⇔ riga attiva).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: si valorizza alla cessazione dello stabilimento (cascata "stabilimento non più attivo",
  [`docs/80-retention.md` §5](../../../progettazione/80-retention.md)).

## Valutazione JSON

Nessun JSON: ogni impostazione è una colonna tipizzata. Se in futuro le opzioni di sicurezza
diventassero molte e sparse, si aggiungono colonne (o una tabella `*_policies` con FK), **non** un blob
`settings jsonb` — [`docs/03-gestione-dati.md` §2.5 / §3.6 punto 5](../../../progettazione/03-gestione-dati.md).

## Suggerimenti per le prestazioni

- Una riga per stabilimento (~175 righe reali). Lettura al login dello staff: `SELECT ... WHERE
  property_id = $1` → coperto da `UNIQUE(property_id)`. Facilmente cache-abile (TTL breve) con
  invalidazione all'update.
- Nessun altro indice necessario.

## Implementazione .NET

Entità semplice (non aggregate root, nessun evento) del modulo `Module.Identity`, sottoarea `Access`,
schema `identity`: una riga per stabilimento. Implementa `ISoftDeletable`/`IAuditable`.

### Enum proprio

```csharp
// Domain/Enums/SecondFactorChannel.cs
public enum SecondFactorChannel { Email = 0, Sms = 1, Totp = 2 }
```

```csharp
modelBuilder.HasPostgresEnum<SecondFactorChannel>("identity", "second_factor_channel");
dataSourceBuilder.MapEnum<SecondFactorChannel>("identity.second_factor_channel");
```

### `PropertySecuritySettingsConfiguration`

```csharp
namespace Module.Identity.Infrastructure.Configurations;

public sealed class PropertySecuritySettingsConfiguration : IEntityTypeConfiguration<PropertySecuritySettings>
{
	public void Configure(EntityTypeBuilder<PropertySecuritySettings> builder)
	{
		builder.ToTable("access_property_security_settings", "identity", t => t.HasComment(
			"Impostazioni di sicurezza applicate a tutto lo staff di uno stabilimento (property)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_access_property_security_settings");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id della property (catalog) a cui si applicano le impostazioni, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.IsTwoFactorEnabled)
			.HasColumnName("is_two_factor_enabled")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se true, richiede un secondo fattore al login dello staff di questo stabilimento.");

		builder.Property(x => x.TwoFactorChannel)
			.HasColumnName("two_factor_channel")
			.IsRequired()
			.HasDefaultValue(SecondFactorChannel.Email)
			.HasComment("Canale del secondo fattore.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune (§2, §6 del template).

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasDatabaseName("uq_identity_access_property_security_settings_property");
	}
}
```

### Uso nel flusso di login

Lettura cache-abile (TTL breve, invalidata all'update) nel flusso di login dello staff: se
`IsTwoFactorEnabled`, dopo la verifica password si emette una
[`identity_access_user_auth_challenges`](identity_access_user_auth_challenges.md) di `purpose =
SecondFactorLogin` sul `TwoFactorChannel` configurato.

### Note di migrazione

- Una riga per `property`: `is_two_factor_enabled = bool_or(bathouses.isTwoFactorEnabled)` aggregato
  sui `bathhouse` di quella `property` (relazione N:1, confermata dal committente); la colonna morta
  `bathouses.isTwoFactorAuthentication` **non** contribuisce.
- Decisione committente **ancora da raccogliere**: 2FA per property vs per singolo account staff (se
  emerge, il flag si sposta/duplica su `identity_access_users`).
