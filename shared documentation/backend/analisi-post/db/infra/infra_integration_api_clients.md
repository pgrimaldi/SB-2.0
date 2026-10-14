# Tabella `infra_integration_api_clients`

> **Dominio**: INFRA — `infra` · **sottoarea**: `integration` · **schema**: `infra` · **nome flat**:
> `infra_integration_api_clients` · **forma canonica**: `infra.integration_api_clients`.
>
> Sostituisce [`keysecret`](../../../analisi-pre/db/keysecret.md) (**2 righe reali** in produzione —
> `appName` `OfSale` e `Transactions`, `TeamSystem` mai valorizzato). Fonti:
> [`analisi-pre/db/keysecret.md`](../../../analisi-pre/db/keysecret.md),
> [`docs/03-gestione-dati.md` §2.4ter, §3.1bis, §6](../../../progettazione/03-gestione-dati.md),
> [`docs/17-secrets.md`](../../../progettazione/17-secrets.md),
> [`docs/35-integrazioni-esterne.md`](../../../progettazione/35-integrazioni-esterne.md),
> [`docs/80-retention.md` §3.10](../../../progettazione/80-retention.md).
>
> **Nota di nomenclatura**: [`docs/03-gestione-dati.md` §3.1bis](../../../progettazione/03-gestione-dati.md) elenca
> come nome illustrativo `infra.integration_credentials` — **corretto qui** in
> `integration_api_clients`: la tabella **non contiene alcuna credenziale** (il segreto vive nel secret
> manager, §"Valutazione JSON"/campo `client_secret_ref`), è un **registro di client applicativi**, non
> un deposito di credenziali. Correzione da riportare in `docs/03 §3.1bis`.

---

## Descrizione

**Una riga per applicazione partner integrata** (POS/cassa esterni, export dati) che autentica
chiamate server-to-server verso l'API. Nel legacy la riga porta anche il segreto **in chiaro**
(`secret`, usato come chiave HMAC o come `client_secret` OAuth *client credentials*); nel nuovo schema
il segreto **esce dal database**: resta solo un riferimento (`client_secret_ref`) alla sua posizione in
AWS Secrets Manager / SSM Parameter Store, che il servizio di autenticazione risolve a runtime con una
cache breve ([`docs/17-secrets.md`](../../../progettazione/17-secrets.md), come già deciso per `fiskaly_accounts`
[C4-2] e `switchboard_configs` [C7-3]).

In produzione **2 sole righe attive**: `OfSale` (POS/cassa) e `Transactions` (export transazioni
giornaliere, vincolata a `property_id = 10588`); il codice prevede anche `TeamSystem` (Cassa in Cloud)
ma nessuna riga esiste — coerente con `teamsystem_expenses` a 0 righe ([`payment_pos_charges`](../payment/payment_pos_charges.md)):
quell'integrazione non è mai stata attivata (vedi [D-57](../_domande-committente.md)).

Correzioni rispetto al legacy:

- **Segreto in chiaro a DB** (`secret`, mai un hash perché la verifica HMAC/OAuth richiede il valore in
  chiaro) → **eliminato dalla tabella**: solo `client_secret_ref` (ARN/nome nel secret manager), come il
  pattern già adottato per Fiskaly/SMTP/SIP.
- **`appName` `varchar(255)` libero**, vocabolario già chiuso in `APP_NAMES` lato codice → **enum**
  `infra.integration_app`.
- **`apiKey` senza indice** (`checkApiKey` fa `findOne({ where: { apiKey } })` a full scan, 2 righe oggi
  ma non garantito) → `client_id` con `UNIQUE`.
- **`params` JSON generico** (`{ property_id }`, unica chiave mai usata) → colonna tipizzata
  `property_id uuid`, nullable (righe non vincolate a uno stabilimento, come `OfSale`).
- **`expiresAt` colonna morta** (dichiarata a schema, mai letta né scritta — `checkApiKey` non la
  seleziona nemmeno) → **resa reale**: nullable, ma quando valorizzata va effettivamente verificata
  all'autenticazione (vedi [D-58](../_domande-committente.md)).
- **Nessuna FK / associazione** (il legame allo stabilimento viveva solo dentro il JSON `params`) →
  `property_id` è un riferimento tipizzato cross-modulo (per valore, `public_id` di C2).
- **Timestamp non automatici** (`timestamps: false`; una riga con `createdAt`/`updatedAt` `NULL`) →
  `created_at`/`updated_at` `NOT NULL DEFAULT now()` standard.
- **`+ rotated_at`** (nuovo): traccia l'ultima rotazione del segreto — dato che il segreto stesso non è
  più in tabella, questo è l'unico modo di sapere "da quando" senza consultare il secret manager.

## Verifica di esclusività

- **`app_name`**: colonna singola (enum), non un gruppo di booleani `is_ofsale`/`is_transactions`/
  `is_teamsystem` — la mutua esclusività ("una riga serve esattamente un'applicazione partner") è
  garantita per costruzione dal tipo enum, non da una convenzione applicativa. Corretto così, nessuna
  modifica necessaria.
- **`is_active`**: unico booleano della tabella, non fa parte di alcun gruppo — nessun conflitto di
  esclusività.

## Snapshot

Non applicabile: nessun valore "congelato" copiato da un'entità mutabile. `client_secret_ref` è un
**puntatore vivo** alla versione corrente del segreto nel secret manager (la rotazione aggiorna il
segreto a quel path, non serve toccare la riga se il nome/ARN non cambia); se la rotazione genera un
nuovo path, si aggiorna `client_secret_ref` e si valorizza `rotated_at` — non uno storico di versioni a
DB (il versioning è nativo del secret manager).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_infra_integration_api_clients` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | id esposto al pannello systemadmin |
| `uq_..._client_id` | `(client_id) WHERE deleted_at IS NULL` | UNIQUE parziale | lookup di autenticazione (`checkApiKey`) — **mancava a schema nel legacy** |
| `ix_..._property` | `(property_id) WHERE property_id IS NOT NULL` | btree parziale | filtro/audit per stabilimento nel pannello admin |
| `ix_..._app_active` | `(app_name) WHERE is_active AND deleted_at IS NULL` | btree parziale | elenco client attivi per integrazione (pannello admin) |

## Campi

### id
- **Legacy**: `keysecret.id`
- **Descrizione**: PK tecnica interna allo schema `infra`.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile per il pannello systemadmin (CRUD dei client API partner) — non
  usato dal flusso di autenticazione HMAC/OAuth stesso, che chiave su `client_id`.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: entità gestita da `systemadmin` fuori dallo schema `infra` → id pubblico per §3.1ter.

### app_name
- **Legacy**: `keysecret.appName` (`varchar(255) NOT NULL`)
- **Descrizione**: applicazione/integrazione partner a cui il client appartiene. Instrada le
  autorizzazioni (es. l'endpoint OAuth *client credentials* di TeamSystem esige
  `app_name = teamsystem`).
- **Tipo**: `enum infra.integration_app` — valori: `of_sale`, `transactions`, `team_system`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: vocabolario chiuso, già centralizzato lato codice legacy (`APP_NAMES`); un nuovo
  partner richiede comunque codice dedicato (routing delle autorizzazioni), quindi enum e non lookup —
  vedi [D-57](../_domande-committente.md) sull'inclusione di `team_system` mai attivato.

### client_id
- **Legacy**: `keysecret.apiKey` (`varchar(255) NOT NULL`, nessun indice)
- **Descrizione**: identificativo pubblico del client (128 bit, alfanumerico). È il `client_id` OAuth
  di TeamSystem e la `apiKey` firmata in `signinApi`; chiave di ricerca dell'autenticazione.
- **Tipo**: `varchar(255)`.
- **Default**: generato alla creazione (server-side).
- **Nullabile**: No.
- **Relazioni**: `UNIQUE` parziale — mancava a schema nel legacy (full scan a ogni chiamata).

### client_secret_ref
- **Legacy**: `keysecret.secret` (`varchar(255) NOT NULL`, **in chiaro**)
- **Descrizione**: riferimento (ARN / nome parametro) al segreto condiviso nel secret manager, path
  convenzionale `infra/integration-api-clients/{client_id}` (come proposto in
  [`keysecret.md`](../../../analisi-pre/db/keysecret.md)). Il segreto vero **non è mai in questa colonna
  né altrove a DB**: viene rimandato in chiaro solo al momento della verifica HMAC / dello scambio
  OAuth *client credentials*, letto dal secret manager con una cache applicativa a TTL breve (~5 min),
  stesso pattern di `fiskaly_accounts.subject_credentials_secret_ref` e
  `switchboard_configs.sip_password_secret_ref`.
- **Tipo**: `varchar(255)`.
- **Default**: nessuno (valorizzato al provisioning insieme alla creazione del segreto).
- **Nullabile**: No.
- **Relazioni**: risolto da `ISecretResolver.Resolve(client_secret_ref)` (stessa astrazione usata per
  gli altri `*_secret_ref` del progetto). Supporta la rotazione con doppio segreto valido nella finestra
  di transizione (§3.2 di [`docs/17-secrets.md`](../../../progettazione/17-secrets.md)).

### property_id
- **Legacy**: `keysecret.params.property_id` (JSON, unica chiave mai osservata/usata: `{"property_id": 10588}` sulla riga `Transactions`, `NULL` su `OfSale`)
- **Descrizione**: stabilimento a cui il client è vincolato. `NULL` = client non scoped a una singola
  property (es. `OfSale`, che serve più stabilimenti). Letto da `getTokenPropertyId` /
  `resolveBeachId` per lo scoping delle chiamate.
- **Tipo**: `uuid` (= `public_id` di [`catalog_properties`](../catalog/catalog_properties.md)), copiato
  per valore — nessuna FK cross-schema (come C1-1/C3-3/C4-5/C6-8/C7-1/C8-1). Tipo `uuid`/`bigint`
  soggetto a [D-01](../_domande-committente.md).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: riferimento cross-modulo a C2 per valore; portato nel JWT emesso all'autenticazione
  (come nel legacy) per lo scoping a valle.

### is_active
- **Legacy**: `keysecret.isEnabled` (`tinyint(1) NOT NULL DEFAULT 1`)
- **Descrizione**: interruttore attivo/revocato. Un client con `is_active = false` non autentica più
  (`checkApiKey` filtra `isEnabled: true`), senza doverlo cancellare (storico/audit preservato).
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### expires_at
- **Legacy**: `keysecret.expiresAt` (`datetime`, **colonna morta**: mai letta né scritta da alcun
  percorso — la scadenza reale era solo quella del JWT emesso, 24h / 7gg)
- **Descrizione**: scadenza effettiva del client, quando impostata. A differenza del legacy, **va
  verificata** dal servizio di autenticazione (`WHERE expires_at IS NULL OR expires_at > now()`).
- **Tipo**: `timestamptz`.
- **Default**: `NULL` (nessuna scadenza).
- **Nullabile**: Sì.
- **Relazioni**: policy di scadenza obbligatoria o opzionale → [D-58](../_domande-committente.md).

### rotated_at
- **Legacy**: — (nuovo)
- **Descrizione**: data dell'ultima rotazione del segreto (aggiornamento di `client_secret_ref` o
  provisioning di una nuova versione allo stesso path). Unico modo di sapere "da quando" senza
  consultare il secret manager, dato che il segreto non è più in tabella.
- **Tipo**: `timestamptz`.
- **Default**: `NULL` (mai ruotato dalla creazione).
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 2 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: `keysecret.createdAt` / `.updatedAt` (`timestamps: false` lato model, valorizzati solo dal
  default Sequelize — nei dati la riga `OfSale` ha entrambi `NULL`) / —
- **Descrizione**: creazione / ultima modifica / revoca definitiva (soft-delete).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`. Le righe legacy con `createdAt NULL` prendono
  la data di migrazione come `created_at` (nessun valore storico disponibile).

## Valutazione JSON

Nessun JSON nel target. L'unico JSON legacy (`params`) conteneva **una sola chiave mai osservata
diversa da `property_id`** → colonna tipizzata. Se in futuro un partner richiedesse più parametri di
scoping (non solo `property_id`), l'estensione corretta è una **tabella figlia** di parametri tipizzati
per client, non un ritorno a un JSON generico (`docs/03 §2.5`).

## Suggerimenti per le prestazioni

- Tabella minuscola (2 righe reali oggi, poche decine attese: "i partner sono 2-3, non centinaia" —
  [`keysecret.md`](../../../analisi-pre/db/keysecret.md)). L'`UNIQUE(client_id)` risolve il problema reale
  del legacy (nessun indice, full scan ad ogni chiamata autenticata).
- Letta a **ogni** chiamata autenticata dei partner (`checkApiKey` / scambio OAuth) → buona candidata a
  **cache applicativa in memoria a TTL breve** (~1-5 min, invalidata su `updated_at`), come già
  suggerito per `client_secret_ref` lato secret manager — evita una query per ogni richiesta di un
  sistema esterno ad alta frequenza (es. `Transactions`).
- Nessun bisogno di partizionamento o indici aggiuntivi al volume atteso.

## Implementazione .NET

`ApiClient` è **entità di registro** (nessun aggregate root complesso, nessun evento di dominio: CRUD di
configurazione gestito dal pannello systemadmin) del modulo `Infra`, area `Integration`, schema `infra`.
Implementa `IAuditable` + `ISoftDeletable` (soft-delete = revoca definitiva, distinta da `is_active`
che è la disattivazione reversibile). **Il segreto non è mai persistito in chiaro**: `client_secret_ref`
è solo un riferimento (ARN/nome) alla sua posizione nel secret manager — nessuna colonna/proprietà porta
il valore del segreto, risolto a runtime da `ISecretResolver`, mai materializzato in un campo
dell'entità né loggato.

### Enum propri

```csharp
// Domain/Enums/IntegrationApp.cs
public enum IntegrationApp { OfSale = 0, Transactions = 1, TeamSystem = 2 }
```

```csharp
// InfraDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<IntegrationApp>("infra", "integration_app");
```

```csharp
// Registrazione data source (Module.cs / Infrastructure DI)
dataSourceBuilder.MapEnum<IntegrationApp>("infra.integration_app");
```

### Configuration

```csharp
namespace Module.Infra.Infrastructure.Configurations;

public sealed class ApiClientConfiguration : IEntityTypeConfiguration<ApiClient>
{
	public void Configure(EntityTypeBuilder<ApiClient> builder)
	{
		builder.ToTable("integration_api_clients", "infra", t => t.HasComment(
			"Registro dei client applicativi partner (POS/cassa esterni, export dati) che autenticano chiamate server-to-server verso l'API — nessuna credenziale in chiaro, solo un riferimento al secret manager."));

		builder.HasKey(x => x.Id).HasName("pk_infra_integration_api_clients");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile (UUIDv7) per il pannello systemadmin; non usato dal flusso di autenticazione, che chiave su client_id.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_infra_integration_api_clients_public_id");

		builder.Property(x => x.AppName)
			.HasColumnName("app_name")
			.HasColumnType("infra.integration_app")
			.IsRequired()
			.HasComment("Applicazione/integrazione partner a cui il client appartiene; instrada le autorizzazioni (es. lo scambio OAuth di TeamSystem).");

		builder.Property(x => x.ClientId)
			.HasColumnName("client_id")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Identificativo pubblico del client (client_id OAuth / apiKey firmata in signinApi); chiave di ricerca dell'autenticazione.");

		builder.Property(x => x.ClientSecretRef)
			.HasColumnName("client_secret_ref")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Riferimento (ARN/nome) al segreto condiviso nel secret manager, path convenzionale infra/integration-api-clients/{client_id} — il segreto vero non è mai a DB.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties a cui il client è vincolato; NULL = client non scoped a una singola property — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Interruttore attivo/revocato: un client con is_active = false non autentica più, senza essere cancellato (storico/audit preservato).");

		builder.Property(x => x.ExpiresAt)
			.HasColumnName("expires_at")
			.HasComment("Scadenza effettiva del client, quando impostata; a differenza del legacy va verificata dal servizio di autenticazione.");

		builder.Property(x => x.RotatedAt)
			.HasColumnName("rotated_at")
			.HasComment("Data dell'ultima rotazione del segreto (aggiornamento di client_secret_ref); unico modo di saperlo senza consultare il secret manager.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione del client.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di ultima modifica.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete: revoca definitiva del client, distinta da is_active (disattivazione reversibile).");

		builder.HasIndex(x => x.ClientId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_infra_integration_api_clients_client_id");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("property_id IS NOT NULL")
			.HasDatabaseName("ix_infra_integration_api_clients_property");

		builder.HasIndex(x => x.AppName)
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_infra_integration_api_clients_app_active");
	}
}
```

- Contratto astratto `IPartnerAuthenticator` (Application) + implementazione concreta (Infrastructure)
  che verifica HMAC (`signinApi`) o gestisce lo scambio OAuth *client credentials* (TeamSystem) —
  coerente col pattern "contratto astratto + implementazione concreta" di
  [`docs/35-integrazioni-esterne.md` §1`](../../../progettazione/35-integrazioni-esterne.md).
- `ISecretResolver.Resolve(clientSecretRef)` (Infrastructure) con cache in memoria a TTL breve (~1-5
  min, stessa astrazione di `fiskaly_accounts`/`switchboard_configs`); supporta due segreti validi
  durante la finestra di rotazione (§3.2 di [`docs/17-secrets.md`](../../../progettazione/17-secrets.md)). Nessun
  metodo dell'aggregato espone mai il valore del segreto: solo `ClientSecretRef` (stringa di
  riferimento), la risoluzione avviene esclusivamente in Infrastructure al momento
  dell'autenticazione.
- Alternativa "zero codice di gestione token" da valutare: **IdP M2M** (AWS Cognito M2M, Auth0/Keycloak
  `client_credentials`) — in quel caso la tabella resta solo come mappatura `client_id → property_id`
  (colonna `ClientSecretRef` non più necessaria), la verifica del segreto la fa l'IdP.
- Policy di autorizzazione: `property_id` (quando valorizzato) applicato come vincolo di scoping sulle
  chiamate del partner — portato nel JWT emesso all'autenticazione, mai fidandosi di un valore lato
  client.
- **Note di migrazione**: 2 righe reali (`OfSale`, `Transactions`) → `app_name` da `appName` (lowercase
  enum); `client_id` da `apiKey`; creare il segreto nel secret manager al path
  `infra/integration-api-clients/{client_id}` con il valore di `secret` **e ruotarlo subito dopo** (era
  in chiaro nel repository/DB, considerarlo compromesso —
  [`docs/17-secrets.md` §4.1`](../../../progettazione/17-secrets.md)); `params.property_id` → `property_id`
  (mappare l'id legacy `10588` al `public_id` di `catalog_properties`); `isEnabled` → `is_active`;
  `createdAt NULL` (riga `OfSale`) → data di migrazione; `expiresAt` sempre `NULL` → `NULL`; nessuna
  riga `TeamSystem` da migrare (mai attivato, [D-57](../_domande-committente.md)).
