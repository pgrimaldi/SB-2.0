# Tabella `notification_establishment_claim_requests`

> **Dominio**: C6 — `notification` · **sottoarea**: `marketing` (lead in ingresso) · **schema**:
> `notification` · **nome flat**: `notification_establishment_claim_requests` · **forma canonica**:
> `notification.establishment_claim_requests`.
>
> Sostituisce [`csv_manager_details`](../../../analisi-pre/db/csv_manager_details.md) (13 righe in
> prod; nome legacy `csv_manager_detail` / `db.csv_owner` / `createCsvOwnerDetails` = residuo di un
> vecchio import CSV). È il registro dei **lead "Richiedi la tua pagina"**: un titolare che trova il
> proprio stabilimento già elencato sul sito ma non gestito da lui compila nome/email/telefono/
> messaggio dal riquadro *"Sei il gestore di questo stabilimento balneare?"*. Fonti:
> [`analisi-pre/db/csv_manager_details.md`](../../../analisi-pre/db/csv_manager_details.md),
> [`docs/03-gestione-dati.md` §3.1bis](../../../progettazione/03-gestione-dati.md).
>
> ⚠️ **Feature marginale, candidata a dismissione** ([D-47](../_domande-committente.md)): 13 richieste
> in 3 anni; il legacy invia solo un'email a `info@summerbooking.it` (hardcoded) senza persistere
> `bathhouseId`. Alternativa: invio email semplice senza tabella. Creata qui su richiesta.

---

## Descrizione

**Una riga = un contatto commerciale/di onboarding in ingresso** dal sito pubblico (rivendica scheda).
Non è "contatta lo stabilimento" né una richiesta di prenotazione (anche se i dati reali mostrano che
il form viene frainteso e usato per prenotare — vedi `message` nei dati legacy).

Correzioni rispetto al legacy:

- **`bathhouseId` letto ma non persistito** (si perde il collegamento allo stabilimento) →
  `bathhouse_public_id` salvato (id copiato da C2).
- **Nessuno stato di lavorazione** → `status` enum (`new` → `contacted` → `approved`/`rejected`/`spam`).
- **Crash se `bathhouseId` non valido** (`bathHouseData.bathouseName` dereferenziato senza controllo)
  → validazione; `bathhouse_name_snapshot` nullable.
- **`emailTo` fuorviante nel log** (la riga risultava inviata all'email del visitatore) → l'invio è
  una riga [`notification_deliveries`](notification_deliveries.md) con `recipient_type = platform_admin`.
- **Nessuna validazione né rate limiting** (endpoint `optionalAuth`, spam nei dati) → annotato:
  captcha + `publicLimiter` server-side.
- **Nome tabella/legacy senza relazione con la funzione** → rinominata.
- **`message varchar(255)` troncato** → `text`.

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `status` | **enum esclusivo**: `new` / `in_review` / `contacted` / `approved` / `rejected` / `spam`. |
| `source_channel` | **enum esclusivo**: `public_site` / `mobile_app`. |

## Snapshot

`bathhouse_name_snapshot`: nome dello stabilimento rivendicato, congelato alla richiesta (resta anche
se la scheda C2 viene rinominata o rimossa).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_establishment_claim_requests` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | pannello admin |
| `ix_..._status_created` | `(status, created_at DESC)` | btree | coda di lavorazione lead |
| `ix_..._bathhouse` | `(bathhouse_public_id) WHERE bathhouse_public_id IS NOT NULL` | btree parziale | richieste per uno stabilimento |

## Campi

### id
- **Legacy**: `csv_manager_details.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### public_id
- **Legacy**: — (nuovo)
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### bathhouse_public_id
- **Legacy**: `csv_manager_details` — `bathhouseId` **letto ma non salvato**
- **Descrizione**: stabilimento (bathhouse C2) a cui la richiesta si riferisce.
- **Tipo**: `uuid` (= `public_id` di `catalog_bathhouses`).
- **Default**: `NULL`.
- **Nullabile**: Sì (form senza `bathhouseId` valido).
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_bathhouses`](../catalog/catalog_bathhouses.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 13 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### bathhouse_name_snapshot
- **Legacy**: `bathouses.bathouseName` (usato solo nel corpo dell'email)
- **Descrizione**: nome dello stabilimento al momento della richiesta.
- **Tipo**: `varchar(200)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: snapshot.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 13 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### full_name
- **Legacy**: `csv_manager_details.fullName`
- **Descrizione**: nome dichiarato dal richiedente.
- **Tipo**: `varchar(160)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### email
- **Legacy**: `csv_manager_details.email`
- **Descrizione**: email di contatto del richiedente.
- **Tipo**: `varchar(320)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: validazione formato server-side (assente nel legacy).

### phone
- **Legacy**: `csv_manager_details.phoneNumber`
- **Descrizione**: telefono del richiedente (formato libero, spesso con prefisso internazionale).
- **Tipo**: `varchar(32)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### message
- **Legacy**: `csv_manager_details.message` (`varchar(255)`, troncato)
- **Descrizione**: testo libero della richiesta.
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### source_channel
- **Legacy**: dedotto dal frontend chiamante (`front-customer-pre` / `front-app-pre`)
- **Descrizione**: da dove è arrivata la richiesta.
- **Tipo**: `enum notification.claim_source_channel` — valori: `public_site`, `mobile_app`.
- **Default**: `public_site`.
- **Nullabile**: No.
- **Relazioni**: —.

### status
- **Legacy**: — (nessuno stato di lavorazione)
- **Descrizione**: avanzamento della gestione del lead.
- **Tipo**: `enum notification.claim_status` — valori: `new`, `in_review`, `contacted`, `approved`,
  `rejected`, `spam`.
- **Default**: `new`.
- **Nullabile**: No.
- **Relazioni**: —.

### handled_by_user_id / handled_at / resolution_note
- **Legacy**: —
- **Descrizione**: chi ha lavorato il lead, quando, con quale nota.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`) · `timestamptz` · `varchar(500)`.
- **Default**: `NULL`.
- **Nullabile**: Sì / Sì / Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 13 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### client_ip_snapshot / user_agent_snapshot
- **Legacy**: — (nessun anti-abuso)
- **Descrizione**: metadati della richiesta per l'anti-spam.
- **Tipo**: `inet` · `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì / Sì.
- **Relazioni**: retention breve (si azzerano dopo la lavorazione).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 13 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: `csv_manager_details.createdAt` / `updatedAt` (mai aggiornato) / —
- **Descrizione**: richiesta / modifica / soft-delete (archiviazione lead).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- Volume trascurabile (~13 righe totali in prod; poche l'anno). Nessun accorgimento particolare.
- L'invio della notifica all'admin è una riga `notification_deliveries` (`kind = establishment_claim`),
  non una colonna qui.

## Implementazione .NET

`EstablishmentClaimRequest` è un'**entità principale** del modulo `Module.Notification` (schema
`notification`), area lead in ingresso: nessuna FK interna allo schema (`bathhouse_public_id`,
`handled_by_user_id` sono id copiati da C2/C5). Alla creazione genera una `Delivery`
(`kind = EstablishmentClaim`, `recipient_type = PlatformAdmin`) verso il recapito admin configurato —
non più l'indirizzo hardcoded del legacy. Feature marginale, candidata a dismissione
([D-47](../_domande-committente.md)): la Configuration sotto resta valida se la tabella viene
confermata in fase implementativa.

### Enum propri

```csharp
// Domain/Enums/ClaimStatus.cs
public enum ClaimStatus { New = 0, InReview = 1, Contacted = 2, Approved = 3, Rejected = 4, Spam = 5 }

// Domain/Enums/ClaimSourceChannel.cs
public enum ClaimSourceChannel { PublicSite = 0, MobileApp = 1 }
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ClaimStatus>("notification", "claim_status");
modelBuilder.HasPostgresEnum<ClaimSourceChannel>("notification", "claim_source_channel");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<ClaimStatus>("notification.claim_status");
dataSourceBuilder.MapEnum<ClaimSourceChannel>("notification.claim_source_channel");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class EstablishmentClaimRequestConfiguration : IEntityTypeConfiguration<EstablishmentClaimRequest>
{
	public void Configure(EntityTypeBuilder<EstablishmentClaimRequest> builder)
	{
		builder.ToTable("establishment_claim_requests", "notification", t => t.HasComment(
			"Lead 'Richiedi la tua pagina': un titolare rivendica la gestione di uno stabilimento già elencato sul sito."));

		builder.HasKey(x => x.Id).HasName("pk_notification_establishment_claim_requests");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, usato nel pannello admin.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_notification_establishment_claim_requests_public_id");

		builder.Property(x => x.BathhousePublicId)
			.HasColumnName("bathhouse_public_id")
			.HasComment("public_id di catalog.bathhouses, copiato — nessuna FK cross-schema. NULL se il form non passa un bathhouseId valido.");

		builder.Property(x => x.BathhouseNameSnapshot)
			.HasColumnName("bathhouse_name_snapshot")
			.HasMaxLength(200)
			.HasComment("Nome dello stabilimento rivendicato, congelato al momento della richiesta.");

		builder.Property(x => x.FullName)
			.HasColumnName("full_name")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Nome dichiarato dal richiedente.");

		builder.Property(x => x.Email)
			.HasColumnName("email")
			.HasMaxLength(320)
			.IsRequired()
			.HasComment("Email di contatto del richiedente, validata server-side.");

		builder.Property(x => x.Phone)
			.HasColumnName("phone")
			.HasMaxLength(32)
			.HasComment("Telefono del richiedente, formato libero.");

		builder.Property(x => x.Message)
			.HasColumnName("message")
			.HasColumnType("text")
			.HasComment("Testo libero della richiesta.");

		builder.Property(x => x.SourceChannel)
			.HasColumnName("source_channel")
			.HasDefaultValue(ClaimSourceChannel.PublicSite)
			.IsRequired()
			.HasComment("Canale di provenienza della richiesta (sito pubblico o app mobile).");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasDefaultValue(ClaimStatus.New)
			.IsRequired()
			.HasComment("Avanzamento della gestione del lead.");

		builder.Property(x => x.HandledByUserId)
			.HasColumnName("handled_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema. Chi ha lavorato il lead.");

		builder.Property(x => x.HandledAt)
			.HasColumnName("handled_at")
			.HasComment("Istante di lavorazione del lead.");

		builder.Property(x => x.ResolutionNote)
			.HasColumnName("resolution_note")
			.HasMaxLength(500)
			.HasComment("Nota libera sull'esito della lavorazione.");

		builder.Property(x => x.ClientIpSnapshot)
			.HasColumnName("client_ip_snapshot")
			.HasColumnType("inet")
			.HasComment("Indirizzo IP del richiedente, per anti-spam; retention breve.");

		builder.Property(x => x.UserAgentSnapshot)
			.HasColumnName("user_agent_snapshot")
			.HasMaxLength(255)
			.HasComment("User-agent del richiedente, per anti-spam; retention breve.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data della richiesta.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica (cambio status, note).");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete: archiviazione del lead.");

		builder.HasIndex(x => new { x.Status, x.CreatedAt })
			.HasDatabaseName("ix_notification_establishment_claim_requests_status_created");

		builder.HasIndex(x => x.BathhousePublicId)
			.HasFilter("bathhouse_public_id IS NOT NULL")
			.HasDatabaseName("ix_notification_establishment_claim_requests_bathhouse");
	}
}
```

`EstablishmentClaimRequest` implementa `ISoftDeletable` e `IAuditable`.

### Note di migrazione

- 13 righe totali di `csv_manager_details` → 1:1.
- `bathhouseId` letto dal legacy ma mai persistito → non disponibile nello storico:
  `bathhouse_public_id = NULL` per tutte le righe migrate (a differenza delle nuove richieste, che lo
  risolvono e salvano server-side).
- `bathhouse_name_snapshot` non ricostruibile dallo storico (era usato solo nel corpo dell'email, non
  salvato) → `NULL` per le righe migrate.
- `message` (`varchar(255)`, troncato nel legacy) → importato as-is nel nuovo `text`, senza tentativi di
  recupero del testo troncato.
- `updatedAt` legacy mai aggiornato di fatto → `updated_at = created_at` per tutte le righe migrate.
- `status`, `handled_by_user_id`, `handled_at`, `resolution_note`, `client_ip_snapshot`,
  `user_agent_snapshot` non hanno equivalente legacy → tutte le righe migrate nascono `status = New` con
  gli altri campi `NULL`.
