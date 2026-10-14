# Tabella `identity_account_contract_documents`

> **Dominio**: C5 — `identity` · **sottoarea**: `account` · **schema**: `identity` · **nome flat**:
> `identity_account_contract_documents` · **forma canonica**: `identity.account_contract_documents`.
>
> Rimpiazza la legacy [`contract_documents`](../../../analisi-pre/db/contract_documents.md) (**0 righe**
> in produzione: feature completa backend + `front-modern-pre`, mai attivata). Fonti:
> [`analisi-pre/db/contract_documents.md`](../../../analisi-pre/db/contract_documents.md),
> [`analisi-pre/db/contract_acceptation.md`](../../../analisi-pre/db/contract_acceptation.md),
> [`docs/03-gestione-dati.md` §1 (URL scomposti), §3.6 (snapshot immutabile)](../../../progettazione/03-gestione-dati.md),
> [`docs/11-modulo-privacy.md` §1](../../../progettazione/11-modulo-privacy.md) (resta in C5, registro legale B2B),
> [`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md).
>
> **Nota sulla voce "Legacy"**: `nome_tabella.campo` di origine; `— (nuovo)` = campo senza
> corrispondente legacy.

---

> ### ✅ Applicato nel backend (2026-09-29) — `is_current` sostituito da `superseded_at`
>
> La colonna `is_current` **non esiste più**: al suo posto **`superseded_at timestamptz NULL`**
> (`NULL` = versione corrente). Migration `CollapseRedundantBooleanFlags` (modulo `Module.Identity`),
> da [`docs/07-ottimizzazioni-db-2.md`](../../../progettazione/07-ottimizzazioni-db-2.md) §B.1.
>
> Erano tre colonne per un unico ciclo di vita: `is_current` è la riga con `status = 'published'` e
> `published_at` massimo, ed è anche ricavabile da `supersedes_id` (chi ha un successore è superato).
> L'invariante «al più una riga corrente» era tenuta a mano da due scritture che dovevano restare in
> accordo; se la seconda non avveniva, il sistema proponeva due documenti correnti.
>
> **L'invariante è ora un vincolo reale di database**, non più una promessa del codice:
>
> ```sql
> CREATE UNIQUE INDEX uq_identity_account_contract_documents_single_current
>     ON identity.account_contract_documents ((true))
>  WHERE superseded_at IS NULL
>    AND status = 'published'::identity.contract_document_status
>    AND deleted_at IS NULL;
> ```
>
> È un `UNIQUE` su espressione costante — non esprimibile in Fluent API, quindi SQL a mano nella
> migration ([`docs/42-migration.md`](../../../progettazione/42-migration.md) §7). Verificato su
> `summer_booking_dev`: il secondo documento pubblicato non superato viene rifiutato dal DB.
>
> In più si guadagna la **data di supersessione**, prima non ricostruibile. `status = 'retired'` resta
> la dismissione definitiva, concettualmente distinta dall'essere stato superato da una versione più
> recente. L'indice `ix_..._current` passa da `(is_current) WHERE is_current` a
> `(status) WHERE superseded_at IS NULL AND deleted_at IS NULL`.
>
> **Travaso** in due passaggi: (1) chi ha un successore prende la data di pubblicazione del
> successore — esatto, via `supersedes_id`; (2) le righe pubblicate non correnti e senza successore
> dichiarato prendono `updated_at`, così l'insieme «pubblicate non superate» coincide con quello che
> aveva `is_current = true`. Le bozze restano `NULL`: non sono superate, non sono mai state correnti.
> Tabella vuota in `summer_booking_dev` (0 righe).

## Descrizione

Catalogo dei **documenti contrattuali** (addendum, es. "Addendum Service Fee") che la piattaforma
pubblica e che l'admin/`superadmin` di uno stabilimento deve accettare. Coppia con
[`identity_account_contract_acceptances`](identity_account_contract_acceptances.md) (registro delle
accettazioni).

Il puntatore che **attiva** la feature per un singolo stabilimento (legacy
`properties.contractDocumentsId`) **resta in C2** ([`docs/03-gestione-dati.md` §6, contro-esempio](../../../progettazione/03-gestione-dati.md)):
è uno stato dello stabilimento. C5 possiede il documento e il suo ciclo di vita (versionamento, hash);
C2 vi si collega copiando il **`public_id` (uuid)** di questa tabella (colonna
`property.contract_document_public_id` in C2), mai il `bigint`.

Rispetto al legacy:

- **versionamento immutabile**: una riga non si modifica in place dopo la pubblicazione; una nuova
  versione è una **nuova riga** (`supersedes_id`) — [`docs/03-gestione-dati.md` §3.6](../../../progettazione/03-gestione-dati.md).
- **URL del PDF scomposto** in `origin` + `path` (§1), mai l'URL completo.
- **hash del contenuto** calcolato **alla pubblicazione** e conservato qui (non solo nell'accettazione):
  così ogni accettazione può confrontarlo senza ri-scaricare il file.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_identity_account_contract_documents` | `id` | PK | — |
| `uq_..._public_id` | `public_id` | UNIQUE | referenziato cross-modulo da C2 (`properties`) → serve un id pubblico stabile |
| `uq_..._version` | `version_code` `WHERE deleted_at IS NULL` | UNIQUE parziale | una versione = un codice |
| `ix_..._current` | `(status) WHERE superseded_at IS NULL AND deleted_at IS NULL` | parziale | "documento corrente da proporre" |
| `uq_..._single_current` | `((true)) WHERE superseded_at IS NULL AND status = 'published' AND deleted_at IS NULL` | UNIQUE su espressione costante | impone **al più un documento corrente pubblicato**; SQL a mano nella migration, non esprimibile in Fluent API |

## Campi

### id
- **Legacy**: `contract_documents.id`
- **Descrizione**: PK tecnica interna.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; auto-referenziata da `supersedes_id`; referenziata da
  [`identity_account_contract_acceptances.contract_document_id`](identity_account_contract_acceptances.md).
- **Note**: —.

### public_id
- **Legacy**: — (nuovo; nel legacy C2 puntava all'`id` intero via `properties.contractDocumentsId`)
- **Descrizione**: id pubblico stabile, referenziato da C2 (ex `properties.contractDocumentsId`) e
  usato nelle API.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: nessuno (assegnato alla creazione).
- **Nullabile**: No.
- **Relazioni**: chiave alternativa; è il valore che C2 copia in `property.contract_document_public_id`.
- **Note**: rientra pienamente nella regola "`public_id` sulle entità referenziate cross-modulo" —
  qui il referenziante è **C2** (`properties.contractDocumentsId` → `property.contract_document_public_id`).

### version_code
- **Legacy**: `contract_documents.version`
- **Descrizione**: etichetta di versione dell'addendum, mostrata all'utente.
- **Tipo**: `varchar(120)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: ex `contract_documents.version` (testo libero, es. `"Addendum Service Fee v1.0 del
  13/07/2026"`). `UNIQUE` fra le righe vive (assente nel legacy). È l'unico identificatore "umano"
  della versione accettata.

### title
- **Legacy**: — (nuovo; nel legacy si mostrava solo `version`)
- **Descrizione**: titolo del documento (per elenchi/UI).
- **Tipo**: `varchar(200)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: nuovo (il legacy mostrava solo `version`).

### document_origin / document_path
- **Legacy**: `contract_documents.link` (URL completo → scomposto in `origin` + `path`, [`docs/03-gestione-dati.md` §1](../../../progettazione/03-gestione-dati.md))
- **Descrizione**: riferimento scomposto al PDF del documento.
- **Tipo**: `document_origin` → enum `storage_origin` (`s3_us_east_2` | `s3_us_east_1` | `cloudfront` |
  `external`); `document_path` → `varchar(500)`.
- **Default**: nessuno.
- **Nullabile**: No (entrambi).
- **Relazioni**: —.
- **Note**: ex `contract_documents.link` (URL completo). [`docs/03-gestione-dati.md` §1](../../../progettazione/03-gestione-dati.md).

### content_sha256
- **Legacy**: — (nuovo; nel legacy l'hash del PDF era in `contract_acceptation.documentHash`, calcolato all'accettazione scaricando il file)
- **Descrizione**: hash SHA-256 (hex) del PDF **al momento della pubblicazione**.
- **Tipo**: `char(64)`.
- **Default**: nessuno.
- **Nullabile**: Sì (finché il file non è raggiungibile/verificato — stato `draft`).
- **Relazioni**: —.
- **Note**: nel legacy l'hash era calcolato **solo** all'accettazione (scaricando il file in sincrono,
  con `DOCUMENT_FETCH_FAILED` se il GET scadeva a 20 s). Calcolarlo alla pubblicazione **disaccoppia**
  l'accettazione dalla raggiungibilità del file e dà un riferimento immutabile.

### status
- **Legacy**: — (nuovo; nel legacy non c'era un ciclo di vita del documento)
- **Descrizione**: stato del documento.
- **Tipo**: enum PG `identity.contract_document_status` — valori: `draft`, `published`, `retired`.
- **Default**: `'draft'`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: solo un documento `published` può essere assegnato a uno stabilimento / accettato.

### superseded_at
- **Legacy**: — (nuovo)
- **Descrizione**: quando questa versione è stata superata da una successiva; `NULL` = **versione
  corrente**. La "versione corrente da proporre" è quindi
  `superseded_at IS NULL AND status = 'published' AND deleted_at IS NULL`.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: valorizzata quando si pubblica la versione che la sostituisce — la riga che la
  sostituisce è quella con `supersedes_id` uguale a questo `id`.
- **Note**: "al più un documento pubblicato non superato" è un **vincolo di database**
  (`uq_identity_account_contract_documents_single_current`, `UNIQUE` su espressione costante), non più
  una promessa del codice. Le bozze hanno `superseded_at NULL` ma `status = 'draft'`, quindi non
  concorrono. Sostituisce il flag `is_current` (vedi riquadro in testa alla scheda).

### supersedes_id
- **Legacy**: — (nuovo; nel legacy non c'era versionamento)
- **Descrizione**: versione precedente che questa sostituisce.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì (prima versione).
- **Relazioni**: **FK** → `identity_account_contract_documents.id` (auto-referenza) `ON DELETE SET NULL`.
- **Note**: catena di versioni immutabili.

### notes
- **Legacy**: `contract_documents.note`
- **Descrizione**: note libere sul documento.
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: ex `contract_documents.note`.

### published_at
- **Legacy**: — (nuovo)
- **Descrizione**: quando il documento è passato a `published`.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: —.

### created_at / updated_at
- **Legacy**: `contract_documents.createdAt` / `contract_documents.updatedAt`
- **Descrizione**: creazione / ultima modifica della riga (le modifiche post-`published` sono limitate
  a `superseded_at`/`status`).
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
- **Note**: un documento con accettazioni collegate non va cancellato (FK `RESTRICT` da
  `identity_account_contract_acceptances`); si usa `status = 'retired'`. Retention: durata contratto
  **+ 10 anni** ([`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md)).

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- Tabella minuscola (feature mai attivata → 0 righe oggi; anche a regime, pochissime righe). Nessuna
  esigenza di tuning; gli indici sopra sono per correttezza (unicità versione, documento corrente).
- Il PDF **non** è nel DB: solo il riferimento `origin`+`path` e l'hash.

## Implementazione .NET

Aggregate root del modulo `Module.Identity`, sottoarea `Account`, schema `identity`. `ContractDocument`
gestisce il proprio ciclo di vita (`draft` → `published` → `retired`) e il versionamento immutabile
(`supersedes_id`); implementa `ISoftDeletable`/`IAuditable`. Factory `ContractDocument.Draft(...)`
assegna `PublicId = Guid.CreateVersion7()`.

### Enum proprio

```csharp
// Domain/Enums/ContractDocumentStatus.cs
public enum ContractDocumentStatus { Draft = 0, Published = 1, Retired = 2 }
```

```csharp
modelBuilder.HasPostgresEnum<ContractDocumentStatus>("identity", "contract_document_status");
dataSourceBuilder.MapEnum<ContractDocumentStatus>("identity.contract_document_status");
// storage_origin: enum condiviso di SharedKernel, già registrato una volta — vedi identity_access_users.md.
```

### `ContractDocumentConfiguration`

```csharp
namespace Module.Identity.Infrastructure.Configurations;

public sealed class ContractDocumentConfiguration : IEntityTypeConfiguration<ContractDocument>
{
	public void Configure(EntityTypeBuilder<ContractDocument> builder)
	{
		builder.ToTable("account_contract_documents", "identity", t => t.HasComment(
			"Documenti contrattuali (addendum) pubblicati dalla piattaforma, con versionamento immutabile."));

		builder.HasKey(x => x.Id).HasName("pk_identity_account_contract_documents");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, referenziato da catalog (property.contract_document_public_id).");

		builder.Property(x => x.VersionCode)
			.HasColumnName("version_code")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("Etichetta di versione dell'addendum, mostrata all'utente.");

		builder.Property(x => x.Title)
			.HasColumnName("title")
			.HasMaxLength(200)
			.HasComment("Titolo del documento per elenchi/UI.");

		builder.Property(x => x.DocumentOrigin)
			.HasColumnName("document_origin")
			.IsRequired()
			.HasComment("Provenienza del PDF (storage interno o provider esterno).");

		builder.Property(x => x.DocumentPath)
			.HasColumnName("document_path")
			.HasMaxLength(500)
			.IsRequired()
			.HasComment("Percorso relativo del PDF, o URL intero se origin = external.");

		builder.Property(x => x.ContentSha256)
			.HasColumnName("content_sha256")
			.HasColumnType("char(64)")
			.HasComment("Hash SHA-256 del PDF al momento della pubblicazione.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.IsRequired()
			.HasDefaultValue(ContractDocumentStatus.Draft)
			.HasComment("Stato del ciclo di vita del documento.");

		builder.Property(x => x.SupersededAt)
			.HasColumnName("superseded_at")
			.HasComment("Quando questa versione è stata superata da una successiva; NULL = versione corrente. Sostituisce il flag is_current, che era derivabile da status + published_at + supersedes_id.");

		builder.Property(x => x.SupersedesId)
			.HasColumnName("supersedes_id")
			.HasComment("Versione precedente che questa sostituisce.");

		builder.Property(x => x.Notes)
			.HasColumnName("notes")
			.HasComment("Note libere sul documento.");

		builder.Property(x => x.PublishedAt)
			.HasColumnName("published_at")
			.HasComment("Quando il documento è passato a published.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune (§2, §6 del template).

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_identity_account_contract_documents_public_id");

		builder.HasIndex(x => x.VersionCode)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_identity_account_contract_documents_version");

		// "documento corrente da proporre": le righe non superate, filtrate poi per status = published.
		// L'unicità "al più un documento corrente pubblicato" è un indice UNIQUE su espressione costante,
		// non esprimibile in Fluent API: aggiunto a mano nella migration
		// (uq_identity_account_contract_documents_single_current, vedi docs/42-migration.md §7).
		builder.HasIndex(x => x.Status)
			.HasFilter("superseded_at IS NULL AND deleted_at IS NULL")
			.HasDatabaseName("ix_identity_account_contract_documents_current");

		builder.HasOne(x => x.Supersedes)
			.WithMany()
			.HasForeignKey(x => x.SupersedesId)
			.HasConstraintName("fk_identity_account_contract_documents_supersedes")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

### Relazioni cross-modulo

C2 (`catalog`) copia `PublicId` in `property.contract_document_public_id` — nessuna FK verso C2 da
questa tabella; l'evento `ContractDocumentPublished` (payload: `PublicId`, `VersionCode`) è il canale di
propagazione.

### Note di migrazione

- Nessun dato da migrare (0 righe in produzione: feature mai attivata) — si crea solo lo schema.
- Endpoint di gestione CRUD (assenti nel legacy: la creazione avveniva via scrittura diretta su DB).
- `ContentSha256` calcolato **alla pubblicazione** (download una tantum), non più all'accettazione come
  nel legacy (che falliva con `DOCUMENT_FETCH_FAILED` a 20s di timeout).
