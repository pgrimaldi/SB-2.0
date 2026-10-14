# Tabella `chatbot_conversation_turns`

> **Dominio**: C8 — `chatbot` · **sottoarea**: — (nessuna sottoarea) · **schema**: `chatbot`
> · **nome flat**: `chatbot_conversation_turns` · **forma canonica**: `chatbot.conversation_turns`.
>
> **Redesign** di [`chatbot_history`](../../../analisi-pre/db/chatbot_history.md) (0 righe in
> `local_sb`/dump di produzione; **10 righe reali** verificate su `summer_booking_dev` — RDS
> `database-1.cmmdmd7jtypl.eu-south-1.rds.amazonaws.com`, credenziali in
> [`front-chatbot/app/config/db.config.js`](../../../../../../../back-pre/app/config/db.config.js) — tutte
> `property_id=96`, `app='manag'`, `created_by=199`, verificato 2026-09-11 via `SHOW CREATE TABLE` +
> lettura righe). **Una riga = uno scambio completo** domanda/risposta tra un operatore di stabilimento
> e l'assistente.
>
> **Fonti**: [`analisi-pre/db/chatbot_history.md`](../../../analisi-pre/db/chatbot_history.md),
> [`docs/26-chatbot.md`](../../../progettazione/26-chatbot.md) (§4 prompt/guardrail, §5 lingua, §6 rate limit,
> §9 punti aperti), [`docs/11-modulo-privacy.md`](../../../progettazione/11-modulo-privacy.md),
> [`docs/80-retention.md`](../../../progettazione/80-retention.md),
> [`front-chatbot/app/Service/chatbotManagerService.js`](../../../../../../../back-chatbot/app/Service/chatbotManagerService.js)
> (`saveChatHistory`, `getChatHistory`, `getTodayCallCount`).

---

## Descrizione

Una riga per ogni messaggio inviato dall'utente e la risposta generata dal provider LLM configurato
(oggi sempre lato app gestionale — vedi `channel`). Alimenta sia lo storico mostrato in pannello
(`/history`) sia il conteggio giornaliero per il rate limit di
[`chatbot_manager_configs`](chatbot_manager_configs.md).

Correzioni rispetto al legacy, **evidenziate dai dati reali** osservati su `summer_booking_dev`:

- **`app varchar(10) DEFAULT 'manag'` → `channel enum` (`manager`/`customer`)**: valori più leggibili;
  confermato nei dati (**10/10** righe reali con `app='manag'`) — `customer` resta riservato a un
  futuro canale lato cliente finale, mai implementato ([`docs/26-chatbot.md` §9](../../../progettazione/26-chatbot.md)).
- **`property_id int` (FK diretta) → `property_id uuid`** (= `public_id` di
  [`catalog_properties`](../catalog/catalog_properties.md)), copiato per valore, come le altre tabelle
  C8.
- **`created_by int` nullabile → `actor_user_id uuid` nullabile** (= `public_id` di
  [`identity_access_users`](../identity/identity_access_users.md)): oggi **sempre valorizzato**, perché
  entrambi gli endpoint richiedono un JWT valido (`authJwt.verifyToken`) e valorizzano sempre
  `req.userId` — confermato: **10/10** righe reali hanno `created_by` popolato (sempre `199`). Resta
  **nullabile per progetto**, non per fedeltà al legacy: se in futuro il canale `customer` ammetterà un
  cliente **non autenticato** (widget pubblico, nessun account), quel turno non avrà un
  `identity_access_users` da referenziare. Tenere la colonna già nullabile con il `CHECK` di §"Campi"
  (obbligatoria solo per `channel = 'manager'`) evita una migrazione quando quel canale verrà acceso —
  basta smettere di popolarla lato codice. Vedi anche `actor_session_id`, sotto.
- **+ `actor_session_id`** (nuovo, preparatorio): correlatore **opaco** generato dal client (cookie/
  storage locale), popolato **solo** quando `actor_user_id` è `NULL` (visitatore anonimo del canale
  `customer`). Permette di raggruppare i turni di una stessa sessione anonima (storico "conversazione
  corrente", rate limiting leggero) senza alcun dato identificativo della persona. Nessun equivalente
  legacy: introdotta ora proprio per non dover aggiungere una colonna con una migrazione futura quando
  il canale anonimo verrà davvero costruito — a quel punto basta un piccolo cambiamento lato codice
  (generare/propagare l'id e smettere di richiedere il JWT su quel canale), non uno schema.
- **`id` sequenziale esposto direttamente in risposta** (`historyId` nel payload di `/chat`, `id` in
  `/history`) → **`public_id` (uuid)**: nel target è questo l'identificativo da restituire al client,
  non l'`id` interno, per non offrire un identificativo enumerabile a un endpoint autenticato ma
  multi-tenant.
- **Esito del turno dedotto da una stringa vuota**: il codice prevede un messaggio di fallback quando
  la risposta del modello è vuota (*"Non sono riuscito a generare una risposta..."*), ma **4 righe reali
  su 10** hanno `output_text = ''` (non il fallback, non `NULL`) — evidenza che quel percorso non è
  sempre scattato (verosimilmente una versione del codice precedente all'attuale, o un'eccezione non
  intercettata prima del salvataggio). Introdotta una colonna esplicita **`status`**
  (`answered`/`failed`) invece di dedurre il fallimento da una stringa vuota; su `failed`,
  `output_text` resta `NULL` (mai stringa vuota).
- **Lingua di risposta**: il legacy la determina a runtime con un'euristica a parole chiave sul
  messaggio dell'utente e **non la persiste** ([`docs/26-chatbot.md` §5](../../../progettazione/26-chatbot.md)
  ne raccomanda il superamento a favore della preferenza nota dell'utente). Introdotta colonna esplicita
  **`response_language`** (nuova) per audit/analisi, valorizzata dalla preferenza utente nota, non da
  un'euristica sul singolo messaggio.
- **+ `llm_provider_id` / `llm_model_snapshot`** (nuovi): tracciano **quale** provider/modello ha
  effettivamente generato la risposta di quel turno — utile per audit/costi ora che il provider è
  configurabile e può cambiare nel tempo ([`docs/26-chatbot.md` §2](../../../progettazione/26-chatbot.md)).
  Valore **congelato** al momento della generazione (da cui il suffisso `_snapshot` su
  `llm_model_snapshot`): non riflette la configurazione corrente se questa cambia in seguito.
- **Conteggio giornaliero sul campo sbagliato**: `getTodayCallCount` filtra su `updatedAt >= oggi`, non
  su `createdAt`, per contare le richieste odierne — funzionalmente innocuo perché la riga non viene mai
  aggiornata dopo la creazione (`updated_at` coincide sempre con `created_at`), ma semanticamente
  fragile. Nel target il conteggio si basa su **`created_at`**.
- **Riferimenti a immagini della documentazione** nel testo di risposta (placeholder `[[[url]]]`,
  confermato nei dati reali, es. link a `summerbooking.s3.us-east-2.amazonaws.com/faq/images/...`)
  restano **testo libero dentro `output_text`**: è un dettaglio di rendering del frontend (sostituzione
  di un segnaposto RAG), non un dato di dominio da scomporre in tabella.
- **Dati personali di clienti nel testo libero**: **osservato realmente** — una risposta nei dati di
  `summer_booking_dev` riporta nome e cognome di due clienti finali estratti da una fonte dati
  (`"L'ombrellone 122 ... risulta essere stato prenotato da [nome] e da [nome]"`). `input_text` e
  `output_text` sono testo libero e **possono contenere dati personali di terzi** (clienti, non solo
  dell'operatore che scrive). Nessuna retention né minimizzazione nel legacy (righe conservate a tempo
  indefinito). Segnalato come [D-54](../_domande-committente.md) — vedi anche
  [`docs/11-modulo-privacy.md`](../../../progettazione/11-modulo-privacy.md).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `channel` | **enum esclusivo**: `manager` **o** `customer`, mai entrambi (un turno appartiene a un solo canale). |
| `status` | **enum esclusivo**: `answered` **o** `failed`. |

Nessun gruppo di flag booleani correlati: niente `enum[]` in questa tabella.

## Snapshot

`llm_model_snapshot` è l'unico campo "congelato" della tabella: copia il modello effettivamente usato al
momento della generazione, non aggiornato retroattivamente se la configurazione (o il catalogo
[`chatbot_llm_providers`](chatbot_llm_providers.md)) cambia in seguito — coerente con la regola
`_snapshot` di C1-7/C3-8.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_chatbot_conversation_turns` | `id` | PK | — |
| `uq_chatbot_conversation_turns_public_id` | `(public_id)` | UNIQUE | id pubblico restituito da `/chat` e `/history` |
| `ix_chatbot_conversation_turns_property_time` | `(property_id, created_at DESC) WHERE deleted_at IS NULL` | parziale | `/history`: elenco più recenti per stabilimento (corregge l'uso legacy di `updated_at`) |
| `ix_chatbot_conversation_turns_rate_limit` | `(property_id, channel, created_at)` | — | conteggio richieste odierne per il rate limit (`checkRateLimit`) |
| `ix_chatbot_conversation_turns_actor_time` | `(actor_user_id, created_at DESC) WHERE actor_user_id IS NOT NULL` | parziale | audit / export per operatore ([`docs/11-modulo-privacy.md`](../../../progettazione/11-modulo-privacy.md)) |
| `ix_chatbot_conversation_turns_session` | `(actor_session_id, created_at DESC) WHERE actor_session_id IS NOT NULL` | parziale | storico della conversazione corrente per un visitatore anonimo (canale `customer`, non ancora attivo) |
| `ix_chatbot_conversation_turns_provider` | `(llm_provider_id) WHERE llm_provider_id IS NOT NULL` | parziale | analisi di utilizzo/costo per provider |


> ### ✅ Applicato nel backend (2026-09-21)
>
> Indice **BRIN** su `created_at` ([`docs/05-ottimizzazioni-db.md` §3](../../../progettazione/05-ottimizzazioni-db.md)), migration
> `AddBrinTimeIndexes`. Gli indici B-tree della scheda hanno tutti `property_id` come prima colonna e non servono al job di retention, che scansiona per sola data.
>
> BRIN e non B-tree perché la tabella è in **solo append** e l'ordine fisico dei blocchi coincide già
> con l'ordine temporale: l'indice memorizza un intervallo per blocco invece di una voce per riga, e
> costa una frazione in spazio e in manutenzione. Non sostituisce gli indici B-tree esistenti, che
> servono query diverse (tutte con un'altra colonna come prefisso).

## Campi

### id
- **Legacy**: `chatbot_history.id`
- **Descrizione**: PK tecnica interna allo schema `chatbot`.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### public_id
- **Legacy**: — (nuovo; sostituisce l'`id` sequenziale oggi esposto come `historyId`/`id`)
- **Descrizione**: id pubblico stabile restituito dalle API (`/chat` → `historyId`, `/history` → `id`
  di ogni voce).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato applicativamente.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### property_id
- **Legacy**: `chatbot_history.property_id` (`int NOT NULL`, FK `chatbot_history_ibfk_1` →
  `properties.id`)
- **Descrizione**: stabilimento del turno di conversazione. Dal claim del token (mai dal body),
  come tutti gli endpoint del modulo ([`docs/34-multitenant.md` §1](../../../progettazione/34-multitenant.md)).
- **Tipo**: `uuid` (= `public_id` di [`catalog_properties`](../catalog/catalog_properties.md)), copiato
  per valore (C8-1).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: riferimento cross-modulo a C2 per valore; nessuna FK cross-schema.

### channel
- **Legacy**: `chatbot_history.app` (`varchar(10) NOT NULL DEFAULT 'manag'`)
- **Descrizione**: canale applicativo da cui parte il turno. Oggi sempre `manager` (confermato: 10/10
  righe reali); `customer` riservato a un futuro assistente lato cliente finale, mai implementato —
  eventualmente raggiungibile **senza autenticazione** (vedi `actor_user_id`/`actor_session_id`).
- **Tipo**: `enum chatbot.channel_kind` — valori: `manager`, `customer`.
- **Default**: `manager`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (channel <> 'manager' OR actor_user_id IS NOT NULL)` — il canale gestionale
  resta sempre autenticato.

### actor_user_id
- **Legacy**: `chatbot_history.created_by` (`int`, nullabile)
- **Descrizione**: operatore autenticato che ha inviato il messaggio. Oggi **sempre** valorizzato
  (confermato: 10/10 righe reali con `created_by` popolato) perché l'unico canale attivo (`manager`)
  richiede sempre un JWT valido. Resta nullabile **per progetto**: un futuro turno `customer` **non
  autenticato** (nessun account) non avrà un utente da referenziare — in quel caso valorizzare invece
  `actor_session_id`.
- **Tipo**: `uuid` (= `public_id` di [`identity_access_users`](../identity/identity_access_users.md)).
- **Default**: `NULL`.
- **Nullabile**: Sì (obbligatorio solo per `channel = 'manager'`, vedi `CHECK` su `channel`).
- **Relazioni**: riferimento cross-modulo a C5 per valore.

### actor_session_id
- **Legacy**: — (nuovo, preparatorio — vedi Descrizione)
- **Descrizione**: correlatore opaco generato lato client (non un dato personale), usato **solo** per
  turni anonimi (`actor_user_id IS NULL`) del futuro canale `customer`, per raggruppare i turni di una
  stessa sessione/conversazione senza identificare la persona. `NULL` su ogni turno con
  `actor_user_id` valorizzato.
- **Tipo**: `varchar(64)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `CHECK (actor_user_id IS NOT NULL OR actor_session_id IS NOT NULL OR channel = 'manager')`* —
  vedi nota.

  \* la combinazione `channel='manager'` rende il ramo ridondante (già coperto dal `CHECK` su
  `channel`, che impone `actor_user_id NOT NULL`): la clausola resta per rendere esplicito, senza
  ambiguità, che un turno `customer` deve portare **almeno uno** dei due correlatori (utente noto o
  sessione anonima) — mai entrambi assenti.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 26 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### status
- **Legacy**: — (nuovo; dedotto nel legacy da `output_text = ''`, vedi Descrizione)
- **Descrizione**: esito del turno. `failed` quando il provider non ha prodotto una risposta valida
  (errore, timeout, risposta vuota) — mai rappresentato con una stringa vuota in `output_text`.
- **Tipo**: `enum chatbot.turn_status` — valori: `answered`, `failed`.
- **Default**: `answered`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (status <> 'failed' OR output_text IS NULL)`.

### llm_provider_id
- **Legacy**: — (nuovo; il legacy non persiste quale variante Gemini ha risposto)
- **Descrizione**: provider/modello che ha effettivamente generato la risposta di questo turno.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (assente sui turni `failed` prima della selezione del provider).
- **Relazioni**: **FK** → [`chatbot_llm_providers`](chatbot_llm_providers.md)`.id` (stesso schema).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 26 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### llm_model_snapshot
- **Legacy**: — (nuovo)
- **Descrizione**: identificativo del modello così come riportato dal provider al momento della
  generazione (di norma coincide col `code` di `llm_provider_id`, ma **congelato**: non cambia se in
  seguito quella riga di `chatbot_llm_providers` viene rinominata/aggiornata).
- **Tipo**: `varchar(64)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 26 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### response_language
- **Legacy**: — (nuovo; il legacy calcola la lingua a runtime per scegliere il prompt di sistema, non
  la persiste — [`docs/26-chatbot.md` §5`](../../../progettazione/26-chatbot.md))
- **Descrizione**: lingua della risposta generata, per audit/analisi. Valorizzata dalla preferenza nota
  dell'utente, non da un'euristica sul singolo messaggio.
- **Tipo**: `varchar(10)` (codice cultura, es. `it-IT`, `en-US`).
- **Default**: `NULL`.
- **Nullabile**: Sì (assente sui turni `failed` prima della risoluzione della lingua).
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 26 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### input_text
- **Legacy**: `chatbot_history.input_text` (`text NOT NULL`)
- **Descrizione**: messaggio inviato dall'utente. Testo libero — **può contenere dati personali di
  terzi** (vedi Descrizione, [D-54](../_domande-committente.md)).
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `CHECK (char_length(input_text) <= <limite configurato>)` — allineato a
  `CHATBOT_MANAGER_MAX_USER_CHAR` (oggi 5000, validato in [`chatbotRequest.dto.js`](../../../../../../../back-chatbot/app/models/dto/chatbotRequest.dto.js)).

### output_text
- **Legacy**: `chatbot_history.output_text` (`text NULL`; osservato anche `''` nei dati reali, vedi
  Descrizione)
- **Descrizione**: risposta generata dal provider LLM. `NULL` quando `status = 'failed'` — **mai**
  stringa vuota. Testo libero — stessa cautela GDPR di `input_text`.
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: `chatbot_history.created_at` / `.updated_at` (`timestamps: true`; il rate limit legacy
  filtra erroneamente su `updated_at`, vedi Descrizione)
- **Descrizione**: creazione del turno (istante del messaggio) / ultima modifica (righe di fatto
  immutabili: nessun percorso applicativo le aggiorna dopo la creazione) / soft-delete (retention o
  cancellazione su richiesta).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`; `created_at` è la colonna di riferimento per il
  conteggio giornaliero del rate limit (non `updated_at`).

## Valutazione JSON

Nessun JSON. Il legacy `chatbot_history` non aveva colonne JSON; `input_text`/`output_text` restano
testo libero (non struttura da scomporre — il placeholder immagine `[[[url]]]` è un dettaglio di
rendering, non un dato tabellare).

## Suggerimenti per le prestazioni

- Volume oggi modesto (10 righe/~2 mesi su 1 sola property in ambiente di test); da rivalutare se il
  canale `customer` viene attivato o il numero di stabilimenti attivi cresce — nessun bisogno di
  partizionamento nel breve periodo.
- Il conteggio giornaliero (`checkRateLimit`) è oggi una `COUNT` ad ogni `/chat` → a regime, se il
  volume cresce, spostare su un **contatore incrementale** (Redis, TTL a mezzanotte Europe/Rome,
  [`docs/16-redis.md`](../../../progettazione/16-redis.md)) invece della `COUNT` ripetuta, stesso criterio già
  suggerito per altri domini.
- `input_text`/`output_text` possono contenere dati personali di clienti in chiaro (vedi Descrizione,
  [D-54](../_domande-committente.md)): nessuna retention nel legacy (righe conservate a tempo
  indefinito) → suggerita una finestra esplicita in coordinamento con
  [`docs/80-retention.md`](../../../progettazione/80-retention.md) e
  [`docs/11-modulo-privacy.md`](../../../progettazione/11-modulo-privacy.md).

## Implementazione .NET

`ChatbotConversationTurn` è **aggregate root** (ha `public_id`, referenziabile da fuori schema) del
modulo `Chatbot`, schema `chatbot`. Implementa `IAuditable` + `ISoftDeletable` (soft-delete = retention
o cancellazione su richiesta; righe di fatto immutabili dopo la creazione — nessun percorso applicativo
le aggiorna).

### Enum propri

```csharp
// Domain/Enums/ChannelKind.cs
public enum ChannelKind { Manager = 0, Customer = 1 }

// Domain/Enums/TurnStatus.cs
public enum TurnStatus { Answered = 0, Failed = 1 }
```

```csharp
// ChatbotDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ChannelKind>("chatbot", "channel_kind");
modelBuilder.HasPostgresEnum<TurnStatus>("chatbot", "turn_status");
```

```csharp
// Registrazione data source (Module.cs / Infrastructure DI)
dataSourceBuilder.MapEnum<ChannelKind>("chatbot.channel_kind");
dataSourceBuilder.MapEnum<TurnStatus>("chatbot.turn_status");
```

### Configuration

```csharp
namespace Module.Chatbot.Infrastructure.Configurations;

public sealed class ChatbotConversationTurnConfiguration : IEntityTypeConfiguration<ChatbotConversationTurn>
{
	public void Configure(EntityTypeBuilder<ChatbotConversationTurn> builder)
	{
		builder.ToTable("conversation_turns", "chatbot", t =>
		{
			t.HasComment("Uno scambio completo domanda/risposta tra un utente (operatore o, in futuro, cliente finale) e l'assistente LLM.");
			t.HasCheckConstraint("ck_chatbot_conversation_turns_channel_actor", "channel <> 'manager' OR actor_user_id IS NOT NULL");
			t.HasCheckConstraint("ck_chatbot_conversation_turns_correlator", "actor_user_id IS NOT NULL OR actor_session_id IS NOT NULL OR channel = 'manager'");
			t.HasCheckConstraint("ck_chatbot_conversation_turns_status_output", "status <> 'failed' OR output_text IS NULL");
			t.HasCheckConstraint("ck_chatbot_conversation_turns_input_length", "char_length(input_text) <= 5000");
		});

		builder.HasKey(x => x.Id).HasName("pk_chatbot_conversation_turns");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile (UUIDv7) restituito dalle API (/chat → historyId, /history → id di ogni voce) al posto dell'id interno sequenziale.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_chatbot_conversation_turns_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties (stabilimento del turno), copiato — nessuna FK cross-schema; sempre dal claim del token.");

		builder.Property(x => x.Channel)
			.HasColumnName("channel")
			.HasColumnType("chatbot.channel_kind")
			.IsRequired()
			.HasDefaultValue(ChannelKind.Manager)
			.HasComment("Canale applicativo da cui parte il turno: manager (gestionale, autenticato) o customer (futuro assistente lato cliente finale, anche anonimo).");

		builder.Property(x => x.ActorUserId)
			.HasColumnName("actor_user_id")
			.HasComment("public_id di identity.access_users, operatore autenticato che ha inviato il messaggio; obbligatorio per channel = manager — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.ActorSessionId)
			.HasColumnName("actor_session_id")
			.HasMaxLength(64)
			.HasComment("Correlatore opaco generato lato client per turni anonimi del canale customer (actor_user_id NULL); non è un dato personale.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("chatbot.turn_status")
			.IsRequired()
			.HasDefaultValue(TurnStatus.Answered)
			.HasComment("Esito del turno: answered, oppure failed quando il provider non ha prodotto una risposta valida (mai dedotto da una stringa vuota).");

		builder.Property(x => x.LlmProviderId)
			.HasColumnName("llm_provider_id")
			.HasComment("Provider/modello (chatbot.llm_providers) che ha effettivamente generato la risposta di questo turno; assente sui turni failed prima della selezione del provider.");

		builder.Property(x => x.LlmModelSnapshot)
			.HasColumnName("llm_model_snapshot")
			.HasMaxLength(64)
			.HasComment("Identificativo del modello riportato dal provider al momento della generazione — congelato, non riflette una rinomina/aggiornamento successivo di llm_providers.");

		builder.Property(x => x.ResponseLanguage)
			.HasColumnName("response_language")
			.HasMaxLength(10)
			.HasComment("Lingua della risposta generata (codice cultura, es. it-IT), da preferenza utente nota — non da un'euristica sul singolo messaggio.");

		builder.Property(x => x.InputText)
			.HasColumnName("input_text")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Messaggio inviato dall'utente — testo libero, può contenere dati personali di terzi (vedi D-54).");

		builder.Property(x => x.OutputText)
			.HasColumnName("output_text")
			.HasColumnType("text")
			.HasComment("Risposta generata dal provider LLM; NULL quando status = failed, mai stringa vuota — stessa cautela GDPR di input_text.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Istante del messaggio; colonna di riferimento per il conteggio giornaliero del rate limit (non updated_at, a differenza del legacy).");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica; righe di fatto immutabili dopo la creazione.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete: retention o cancellazione su richiesta.");

		builder.HasIndex(x => new { x.PropertyId, x.CreatedAt })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_chatbot_conversation_turns_property_time");

		builder.HasIndex(x => new { x.PropertyId, x.Channel, x.CreatedAt })
			.HasDatabaseName("ix_chatbot_conversation_turns_rate_limit");

		builder.HasIndex(x => new { x.ActorUserId, x.CreatedAt })
			.HasFilter("actor_user_id IS NOT NULL")
			.HasDatabaseName("ix_chatbot_conversation_turns_actor_time");

		builder.HasIndex(x => new { x.ActorSessionId, x.CreatedAt })
			.HasFilter("actor_session_id IS NOT NULL")
			.HasDatabaseName("ix_chatbot_conversation_turns_session");

		builder.HasIndex(x => x.LlmProviderId)
			.HasFilter("llm_provider_id IS NOT NULL")
			.HasDatabaseName("ix_chatbot_conversation_turns_provider");

		builder.HasOne(x => x.LlmProvider)
			.WithMany()
			.HasForeignKey(x => x.LlmProviderId)
			.HasConstraintName("fk_chatbot_conversation_turns_llm_provider")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

- `IChatCompletionProvider` (Infrastructure/ExternalApi, un'implementazione per provider) selezionato
  per `code` da [`chatbot_llm_providers`](chatbot_llm_providers.md), eventualmente forzato da
  [`chatbot_manager_configs`](chatbot_manager_configs.md)`.llm_provider_id` — coerente con
  [`docs/26-chatbot.md` §2`](../../../progettazione/26-chatbot.md) e
  [`docs/35-integrazioni-esterne.md` §1`](../../../progettazione/35-integrazioni-esterne.md).
- Policy di autorizzazione di modulo: `property_id` sempre da `ClaimsPrincipal`, mai dal body;
  `actor_user_id` idem. Per un futuro canale `customer` non autenticato: `actor_session_id`
  generato/validato **server-side** (mai un id proposto dal client senza vincoli — firma o emissione
  alla prima interazione), più limiti di richiesta aggiuntivi per IP/sessione (oltre al limite per
  property già su `chatbot_manager_configs`) per compensare l'assenza di autenticazione.
- `char_length(input_text) <= 5000` riflette il limite applicativo odierno
  (`CHATBOT_MANAGER_MAX_USER_CHAR`, validato oggi in `chatbotRequest.dto.js`); se il limite diventa
  configurabile a runtime, il `CHECK` va rimosso a favore di sola validazione in Application.
- Retention **12 mesi confermata dal titolare** il 2026-09-17
  ([`docs/82-retention-data.md` §3.8](../../../progettazione/82-retention-data.md)): job `PurgeChatbotHistory`,
  hard-delete su `created_at` ([`docs/20-jobs.md` §1.5](../../../progettazione/20-jobs.md)), riga seminata in
  [`privacy_retention_policies`](../privacy/privacy_retention_policies.md). Resta aperta la parte di
  [D-54](../_domande-committente.md) che **non** è una questione di durata (redazione automatica del
  testo, accesso ristretto): il titolare ha validato il *quando*, non la qualificazione giuridica del
  testo libero — la policy nasce comunque in dry-run.
- Impostazione originaria, da progettare su [D-54](../_domande-committente.md) una volta confermata la
  finestra con il committente (nessuna retention nel legacy: righe conservate a tempo indefinito).
- **Note di migrazione**: 0 righe su `local_sb`/dump di produzione → nessun backfill da quelle copie;
  **esistono però 10 righe reali** su `summer_booking_dev` (property 96) da migrare se quell'ambiente va
  portato al nuovo schema: `app='manag'`→`channel='manager'`, `created_by`→`actor_user_id` (uuid),
  `id` sequenziale → nuovo `public_id` generato ex-novo, `output_text=''`→`NULL` + `status='failed'`
  per le 4 righe osservate così (non il fallback testuale, non `NULL`); `llm_provider_id`/
  `llm_model_snapshot`/`response_language` non ricostruibili retroattivamente (`NULL`).
