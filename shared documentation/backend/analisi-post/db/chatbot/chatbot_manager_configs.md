# Tabella `chatbot_manager_configs`

> **Dominio**: C8 — `chatbot` · **sottoarea**: — (nessuna sottoarea) · **schema**: `chatbot`
> · **nome flat**: `chatbot_manager_configs` · **forma canonica**: `chatbot.manager_configs`.
>
> **Redesign** di [`chatbot_manager_config`](../../../analisi-pre/db/chatbot_manager_config.md)
> (0 righe in `local_sb`/dump di produzione; **1 riga reale** verificata su `summer_booking_dev` — RDS
> `database-1.cmmdmd7jtypl.eu-south-1.rds.amazonaws.com`, credenziali in
> [`front-chatbot/app/config/db.config.js`](../../../../../../../back-pre/app/config/db.config.js) — property
> `96`, `max_call_per_day=10`, `max_returned_history=10`, `created_by NULL`, verificato 2026-09-11 via
> `SHOW CREATE TABLE` + lettura righe). Limiti/preferenze del chatbot **per stabilimento** — una riga
> per `property_id`.
>
> **Fonti**: [`analisi-pre/db/chatbot_manager_config.md`](../../../analisi-pre/db/chatbot_manager_config.md),
> [`docs/26-chatbot.md` §2 / §6`](../../../progettazione/26-chatbot.md),
> [`front-chatbot/app/Service/chatbotManagerService.js`](../../../../../../../back-chatbot/app/Service/chatbotManagerService.js)
> (`getChatbotConfig`, `checkRateLimit`).

---

## Descrizione

Una riga = i limiti di utilizzo (e, nel target, la preferenza di provider LLM) del chatbot per uno
stabilimento. Nel legacy la riga è creata **implicitamente alla prima chiamata** se assente
(`getChatbotConfig`: `findOne` poi `create` con i default se `null`).

Correzioni rispetto al legacy:

- **`max_call_per_day` → `daily_request_limit`**: rinominato per non legare il nome colonna al termine
  tecnico "call" (dell'SDK del provider) — il concetto di dominio è "richieste al chatbot consentite al
  giorno", indipendente dal vendor.
- **`max_returned_history` → `history_page_size_limit`**: rinominato per chiarire che è un **tetto per
  risposta** dell'endpoint storico (`count` richiesto dal chiamante, limitato a questo massimo), non la
  quantità di storico conservata (quella è governata dalla retention, non da questa colonna).
- **`property_id int` (FK diretta a `properties.id`) → `property_id uuid`** (= `public_id` di
  [`catalog_properties`](../catalog/catalog_properties.md)), copiato per valore, come C1-1/C3-3/C4-5/
  C6-8/C7-1: nessuna FK cross-schema.
- **`created_by int` (FK `users.id`, nullabile — nei dati osservati sempre `NULL`, riga creata dal
  servizio, non da un operatore) → `created_by_user_id uuid` nullabile** (= `public_id` di
  [`identity_access_users`](../identity/identity_access_users.md)): stesso significato, riferimento per
  valore.
- **+ `llm_provider_id`** (nuovo): consente di **forzare il provider LLM per singolo stabilimento**
  ([`docs/26-chatbot.md` §2](../../../progettazione/26-chatbot.md)) — `NULL` = usa il provider di default di
  piattaforma (nel target, una configurazione, non più l'unica variabile d'ambiente globale
  `CHATBOT_MANAGER_LLM` del legacy).
- **Provisioning implicito su path di lettura** (la riga nasce dentro `getChatbotConfig`, chiamata sia
  da `/chat` che da `/history`, quindi una richiesta "di sola lettura" può eseguire una `INSERT`) →
  segnalato come correzione architetturale in "Implementazione .NET" (provisioning esplicito
  all'attivazione del modulo per lo stabilimento), non un cambio di schema.

## Verifica di esclusività

Nessun gruppo di flag booleani correlati in questa tabella (nessuna colonna `is_...`).

## Snapshot

Non applicabile: valori di configurazione vivi (limiti e preferenza provider), nessun dato congelato
copiato da un'entità mutabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_chatbot_manager_configs` | `id` | PK | — |
| `uq_chatbot_manager_configs_property` | `(property_id) WHERE deleted_at IS NULL` | UNIQUE parziale | una configurazione per stabilimento (il legacy aveva già `UNIQUE(property_id)`, qui reso parziale per coerenza col soft-delete) |
| `ix_chatbot_manager_configs_llm_provider` | `(llm_provider_id) WHERE llm_provider_id IS NOT NULL` | parziale | analisi di adozione per provider forzato |

## Campi

### id
- **Legacy**: `chatbot_manager_config.id`
- **Descrizione**: PK tecnica interna allo schema `chatbot`.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### property_id
- **Legacy**: `chatbot_manager_config.property_id` (`int NOT NULL`, `UNIQUE`)
- **Descrizione**: stabilimento a cui si applica la configurazione. Chiave di accesso di tutti gli
  endpoint del modulo, dal claim del token — mai dal body
  ([`docs/34-multitenant.md` §1](../../../progettazione/34-multitenant.md)).
- **Tipo**: `uuid` (= `public_id` di [`catalog_properties`](../catalog/catalog_properties.md)), copiato
  per valore (C8-1, come C1-1/C3-3/C4-5/C6-8/C7-1).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: riferimento cross-modulo a C2 per valore; `UNIQUE` parziale; nessuna FK cross-schema.

### daily_request_limit
- **Legacy**: `chatbot_manager_config.max_call_per_day` (`int NOT NULL DEFAULT 10`)
- **Descrizione**: numero massimo di richieste al chatbot consentite per giorno solare (Europe/Rome),
  per stabilimento. Limite anti-abuso / controllo costi del provider LLM. Oltre soglia, `/chat` risponde
  con un errore `RATE_LIMIT_EXCEEDED` senza invocare il provider.
- **Tipo**: `integer`.
- **Default**: `10`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (daily_request_limit > 0)`.

### history_page_size_limit
- **Legacy**: `chatbot_manager_config.max_returned_history` (`int NOT NULL DEFAULT 10`)
- **Descrizione**: quante voci di [`chatbot_conversation_turns`](chatbot_conversation_turns.md)
  restituisce al massimo `/history` per stabilimento, anche se il chiamante ne richiede di più
  (`Math.min(count, max)` nel legacy).
- **Tipo**: `integer`.
- **Default**: `10`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (history_page_size_limit > 0)`.

### llm_provider_id
- **Legacy**: — (nuovo; oggi solo `CHATBOT_MANAGER_LLM`, variabile d'ambiente globale di piattaforma)
- **Descrizione**: override del provider/modello LLM per questo stabilimento. `NULL` = usa il default
  di piattaforma.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`chatbot_llm_providers`](chatbot_llm_providers.md)`.id` (stesso schema).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 1 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_by_user_id
- **Legacy**: `chatbot_manager_config.created_by` (`int`, nullabile; nei dati osservati **sempre
  `NULL`** — 1/1 riga su `summer_booking_dev`, coerente con la creazione automatica dal servizio)
- **Descrizione**: operatore che ha creato/modificato manualmente la configurazione, quando applicabile
  (pannello admin). `NULL` se la riga è stata provisionata automaticamente dal sistema.
- **Tipo**: `uuid` (= `public_id` di [`identity_access_users`](../identity/identity_access_users.md)).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: riferimento cross-modulo a C5 per valore.

### created_at / updated_at / deleted_at
- **Legacy**: `chatbot_manager_config.created_at` / `.updated_at` (`timestamps: true`)
- **Descrizione**: creazione / ultima modifica / soft-delete (disattivazione del modulo chatbot per lo
  stabilimento).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. Il legacy `chatbot_manager_config` non aveva colonne JSON; tutte le informazioni restano
colonne tipizzate.

## Suggerimenti per le prestazioni

- Tabella minuscola (≤ 1 riga per stabilimento). Nessun indice oltre a quelli elencati.
- Letta a **ogni** chiamata a `/chat` (rate limit) e `/history` (page size) → buona candidata a cache
  Redis per property, invalidata sull'`updated_at` (config che cambia raramente,
  [`docs/16-redis.md`](../../../progettazione/16-redis.md)), come già suggerito per
  [`switchboard_configs`](../switchboard/switchboard_configs.md).

## Implementazione .NET

`ChatbotManagerConfig` è **aggregate root** (una riga per `property_id`, ciclo di vita proprio
indipendente da altre entità) del modulo `Chatbot`, schema `chatbot`. Nessun enum proprio. Implementa
`IAuditable` + `ISoftDeletable` (soft-delete = disattivazione del modulo chatbot per lo stabilimento).

```csharp
namespace Module.Chatbot.Infrastructure.Configurations;

public sealed class ChatbotManagerConfigConfiguration : IEntityTypeConfiguration<ChatbotManagerConfig>
{
	public void Configure(EntityTypeBuilder<ChatbotManagerConfig> builder)
	{
		builder.ToTable("manager_configs", "chatbot", t =>
		{
			t.HasComment("Limiti di utilizzo e preferenza di provider LLM del chatbot per stabilimento — una riga per property_id.");
			t.HasCheckConstraint("ck_chatbot_manager_configs_daily_limit", "daily_request_limit > 0");
			t.HasCheckConstraint("ck_chatbot_manager_configs_history_limit", "history_page_size_limit > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_chatbot_manager_configs");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties (stabilimento a cui si applica la configurazione), copiato — nessuna FK cross-schema; sempre dal claim del token, mai dal body.");

		builder.Property(x => x.DailyRequestLimit)
			.HasColumnName("daily_request_limit")
			.IsRequired()
			.HasDefaultValue(10)
			.HasComment("Numero massimo di richieste al chatbot consentite per giorno solare (Europe/Rome), per stabilimento — limite anti-abuso/controllo costi.");

		builder.Property(x => x.HistoryPageSizeLimit)
			.HasColumnName("history_page_size_limit")
			.IsRequired()
			.HasDefaultValue(10)
			.HasComment("Tetto massimo di voci di chatbot.conversation_turns restituite da /history per stabilimento, anche se il chiamante ne richiede di più.");

		builder.Property(x => x.LlmProviderId)
			.HasColumnName("llm_provider_id")
			.HasComment("Override del provider/modello LLM per questo stabilimento; NULL = usa il default di piattaforma.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, operatore che ha creato/modificato manualmente la configurazione; NULL se provisionata automaticamente dal sistema — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione della configurazione.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di ultima modifica.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete: disattivazione del modulo chatbot per lo stabilimento.");

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_chatbot_manager_configs_property");

		builder.HasIndex(x => x.LlmProviderId)
			.HasFilter("llm_provider_id IS NOT NULL")
			.HasDatabaseName("ix_chatbot_manager_configs_llm_provider");

		builder.HasOne(x => x.LlmProvider)
			.WithMany()
			.HasForeignKey(x => x.LlmProviderId)
			.HasConstraintName("fk_chatbot_manager_configs_llm_provider")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

- **Provisioning esplicito**: la riga si crea quando il modulo chatbot viene attivato per lo
  stabilimento (evento di dominio `ChatbotModuleActivated` gestito da un handler, o azione admin
  esplicita) — **mai** implicitamente dentro il path di lettura di `/chat`/`/history` come nel legacy
  (`getChatbotConfig` eseguiva una `INSERT` se la riga mancava, effetto collaterale su una richiesta
  concettualmente di sola lettura). L'Application layer per `/chat` e `/history` tratta l'assenza di
  configurazione come errore di dominio (modulo non attivato), non come condizione da sanare al volo.
- Cache Redis candidata per `property_id` (già in Suggerimenti per le prestazioni), invalidata
  sull'`updated_at`: letta a ogni chiamata a `/chat` e `/history`.
- Policy di autorizzazione di modulo: `property_id` sempre da `ClaimsPrincipal`, mai dal body.
- **Note di migrazione**: 0 righe su `local_sb`/dump di produzione → nessun backfill da quelle copie;
  **esiste però 1 riga reale** su `summer_booking_dev` (property 96) da migrare se quell'ambiente va
  portato al nuovo schema: `max_call_per_day`→`daily_request_limit`, `max_returned_history`→
  `history_page_size_limit`, `created_by NULL`→`created_by_user_id NULL`, `llm_provider_id` iniziale
  `NULL` (nessun override, usa il default di piattaforma).
