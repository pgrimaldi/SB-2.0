# Tabella `chatbot_llm_providers`

> **Dominio**: C8 — `chatbot` · **sottoarea**: — (nessuna sottoarea) · **schema**: `chatbot`
> · **nome flat**: `chatbot_llm_providers` · **forma canonica**: `chatbot.llm_providers`.
>
> **Nuova** tabella (nessun equivalente legacy a schema): sostituisce la mappa cablata nel codice
> `LLM_IMPLEMENTATIONS` di [`llmFactory.js`](../../../../../../../back-chatbot/app/utils/llmFactory.js) — oggi 2
> varianti Gemini, selezionate globalmente da un'unica variabile d'ambiente `CHATBOT_MANAGER_LLM` — con
> un catalogo interrogabile a DB, gestibile senza deploy, da cui pescano
> [`chatbot_manager_configs.llm_provider_id`](chatbot_manager_configs.md) e
> [`chatbot_conversation_turns.llm_provider_id`](chatbot_conversation_turns.md).
>
> **Fonti**: [`docs/26-chatbot.md` §2](../../../progettazione/26-chatbot.md) (provider LLM intercambiabile),
> [`docs/35-integrazioni-esterne.md` §1](../../../progettazione/35-integrazioni-esterne.md) (contratto
> astratto/implementazione concreta per vendor esterni),
> [`front-chatbot/app/utils/llmFactory.js`](../../../../../../../back-chatbot/app/utils/llmFactory.js),
> [`front-chatbot/app/utils/llmManagerGemini2FlashLite.helper.js`](../../../../../../../back-chatbot/app/utils/llmManagerGemini2FlashLite.helper.js).

---

## Descrizione

Catalogo dei modelli/provider LLM che il modulo Chatbot può usare per generare le risposte. **Una riga
per modello selezionabile** (non per vendor: due modelli dello stesso vendor, es. due varianti Gemini,
sono due righe).

Correzioni rispetto al legacy:

- **Vocabolario cablato nel codice** (`LLM_IMPLEMENTATIONS`, una sola variabile d'ambiente globale) →
  **tabella**: aggiungere un modello selezionabile diventa un inserimento a riga (non un deploy) —
  resta comunque necessaria, lato `Infrastructure/ExternalApi`, l'implementazione concreta di
  `IChatCompletionProvider` per un vendor mai integrato prima ([`docs/35 §1`](../../../progettazione/35-integrazioni-esterne.md));
  questa tabella cataloga **quali** modelli sono disponibili e selezionabili, non sostituisce il codice
  del client.
- **Nessuna gestione di deprecazione**: il commento nel codice legacy
  ([`llmManagerGemini2FlashLite.helper.js`](../../../../../../../back-chatbot/app/utils/llmManagerGemini2FlashLite.helper.js)
  riga 4) segnala *"This model is deprecated and will be discontinued on March 31, 2026"* ma **nulla lo
  applica** — il modello resta selezionabile via `CHATBOT_MANAGER_LLM` esattamente come l'altro. **Nota**:
  quella data (31 marzo 2026) è **già passata** rispetto alla data corrente di questa analisi
  (2026-09-11) — il modello avrebbe dovuto essere già escluso dalla selezione. Qui `is_selectable` +
  `retires_on` rendono la scadenza **applicata**, senza rompere le righe storiche che referenziano un
  provider ormai ritirato.

## Verifica di esclusività

Nessun gruppo di flag booleani correlati: `is_selectable` è un interruttore singolo e autonomo.

## Snapshot

Non applicabile: catalogo vivo, nessun dato congelato.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_chatbot_llm_providers` | `id` | PK | — |
| `uq_chatbot_llm_providers_code` | `(code)` | UNIQUE | chiave di lookup usata dalla configurazione (property override) e dal factory dei provider |
| `ix_chatbot_llm_providers_selectable` | `(vendor) WHERE is_selectable AND deleted_at IS NULL` | parziale | popolare il selettore del pannello (modelli oggi proponibili) |

## Campi

### id
- **Legacy**: — (nuova tabella)
- **Descrizione**: PK tecnica interna allo schema `chatbot`.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da `llm_provider_id` di
  [`chatbot_manager_configs`](chatbot_manager_configs.md) e
  [`chatbot_conversation_turns`](chatbot_conversation_turns.md).

### code
- **Legacy**: chiave di `LLM_IMPLEMENTATIONS` / valore atteso di `CHATBOT_MANAGER_LLM` (es.
  `"gemini-3-flash-preview"`, `"gemini-2.5-flash"`)
- **Descrizione**: identificativo tecnico del modello, passato al SDK del vendor. Stabile, usato come
  chiave applicativa (non solo la PK interna).
- **Tipo**: `varchar(64)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### vendor
- **Legacy**: — (implicito: entrambi i modelli legacy sono Google GenAI)
- **Descrizione**: fornitore del modello. Stringa libera (**non enum**): a differenza di `channel`/`status`
  di questo stesso dominio, il vocabolario dei vendor LLM è per natura **aperto e in crescita** (oggi solo
  `google`; domani potenzialmente `openai`, `anthropic`, un modello self-hosted, …) — coerente col
  criterio "lookup/passthrough dove il vocabolario cresce senza deploy"
  ([`docs/03-gestione-dati.md` §2.2–§2.4](../../../progettazione/03-gestione-dati.md)).
- **Tipo**: `varchar(32)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### display_name
- **Legacy**: — (nuovo)
- **Descrizione**: etichetta leggibile per il pannello di configurazione (es. "Gemini 3 Flash
  Preview"). Solo presentazione, nessun effetto sulla logica.
- **Tipo**: `varchar(120)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### is_selectable
- **Legacy**: — (nuovo; correzione della deprecazione mai applicata, vedi Descrizione)
- **Descrizione**: se `false`, il modello non è proponibile per **nuove** selezioni (pannello e default
  di piattaforma) ma le righe storiche di
  [`chatbot_conversation_turns`](chatbot_conversation_turns.md) che lo referenziano restano valide.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### retires_on
- **Legacy**: commento in codice, mai applicato (vedi Descrizione)
- **Descrizione**: data oltre la quale il modello è considerato ritirato dal vendor. Puramente
  informativa/di audit: l'unico interruttore **applicato** dalla logica è `is_selectable` (impostato
  manualmente o da un job che confronta questa data con `today()`).
- **Tipo**: `date`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: — (nuova tabella)
- **Descrizione**: creazione / ultima modifica / soft-delete (rimozione di un provider mai stato usato;
  un provider **già usato** si ritira con `is_selectable = false`, non si cancella, per non rompere lo
  storico).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON: catalogo piccolo e piatto, nessuna struttura da scomporre.

## Suggerimenti per le prestazioni

- Tabella lookup minuscola (ordine delle unità/decine di righe). **Cache-abile interamente in memoria**
  (es. `IMemoryCache`/singleton ricaricato all'avvio + invalidazione sulla scrittura), nessuna query per
  richiesta.
- Nessun indice oltre a quelli elencati.

## Implementazione .NET

`LlmProvider` è entità **lookup** (Pattern B: `code` `UNIQUE`, nessun `ENUM` PG — il vocabolario dei
vendor LLM è aperto e cresce senza deploy) del modulo `Chatbot`, schema `chatbot`. Nessun enum proprio.
Referenziata da `chatbot_manager_configs.llm_provider_id` e `chatbot_conversation_turns.llm_provider_id`
(FK reali, stesso schema). Implementa `IAuditable` + `ISoftDeletable` (un provider mai usato si
cancella soft; uno già usato si ritira con `is_selectable = false`, mai cancellato, per non rompere lo
storico).

```csharp
namespace Module.Chatbot.Infrastructure.Configurations;

public sealed class LlmProviderConfiguration : IEntityTypeConfiguration<LlmProvider>
{
	public void Configure(EntityTypeBuilder<LlmProvider> builder)
	{
		builder.ToTable("llm_providers", "chatbot", t => t.HasComment(
			"Catalogo dei modelli/provider LLM utilizzabili dal modulo Chatbot per generare le risposte — una riga per modello selezionabile, non per vendor."));

		builder.HasKey(x => x.Id).HasName("pk_chatbot_llm_providers");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasMaxLength(64)
			.IsRequired()
			.HasComment("Identificativo tecnico del modello passato al SDK del vendor (es. \"gemini-3-flash-preview\"); chiave applicativa, non solo la PK interna.");

		builder.Property(x => x.Vendor)
			.HasColumnName("vendor")
			.HasMaxLength(32)
			.IsRequired()
			.HasComment("Fornitore del modello (es. \"google\"); stringa libera, vocabolario aperto e in crescita — non enum.");

		builder.Property(x => x.DisplayName)
			.HasColumnName("display_name")
			.HasMaxLength(120)
			.HasComment("Etichetta leggibile per il pannello di configurazione; solo presentazione.");

		builder.Property(x => x.IsSelectable)
			.HasColumnName("is_selectable")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Se false, il modello non è proponibile per nuove selezioni; le righe storiche che lo referenziano restano valide.");

		builder.Property(x => x.RetiresOn)
			.HasColumnName("retires_on")
			.HasComment("Data oltre la quale il modello è considerato ritirato dal vendor; puramente informativa — l'interruttore applicato è is_selectable.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione del catalogo provider.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di ultima modifica.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete: rimozione di un provider mai usato. Un provider già usato si ritira con is_selectable = false, non si cancella.");

		builder.HasIndex(x => x.Code)
			.IsUnique()
			.HasDatabaseName("uq_chatbot_llm_providers_code");

		builder.HasIndex(x => x.Vendor)
			.HasFilter("is_selectable AND deleted_at IS NULL")
			.HasDatabaseName("ix_chatbot_llm_providers_selectable");
	}
}
```

- Caricata in **cache in memoria** (`IMemoryCache`/singleton ricaricato all'avvio, invalidato sulla
  scrittura da pannello admin) — catalogo minuscolo, nessuna query per richiesta (già in Suggerimenti
  per le prestazioni).
- `IChatCompletionProvider` (Infrastructure/ExternalApi) risolto per `code` — un'implementazione per
  vendor/modello, come [`docs/26-chatbot.md` §2`](../../../progettazione/26-chatbot.md); questa tabella cataloga
  solo **quali** modelli sono disponibili, non sostituisce il client del vendor.
- **Note di migrazione**: nessun dato legacy da riportare (tabella nuova, sostituisce la mappa cablata
  `LLM_IMPLEMENTATIONS`). Seed iniziale con i 2 modelli oggi noti: `gemini-3-flash-preview`
  (`is_selectable = true`) e `gemini-2.5-flash` (da seminare con `is_selectable = false` e
  `retires_on = 2026-03-31`, dato che quella data è già passata alla data di questa analisi — la
  deprecazione mai applicata nel legacy va quindi già corretta al seed, non lasciata per un job
  successivo).
