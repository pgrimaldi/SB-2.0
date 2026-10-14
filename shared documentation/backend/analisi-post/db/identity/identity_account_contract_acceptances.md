# Tabella `identity_account_contract_acceptances`

> **Dominio**: C5 — `identity` · **sottoarea**: `account` · **schema**: `identity` · **nome flat**:
> `identity_account_contract_acceptances` · **forma canonica**: `identity.account_contract_acceptances`.
>
> Rimpiazza la legacy [`contract_acceptation`](../../../analisi-pre/db/contract_acceptation.md) (**0
> righe** in produzione). Fonti:
> [`analisi-pre/db/contract_acceptation.md`](../../../analisi-pre/db/contract_acceptation.md),
> [`analisi-pre/db/contract_documents.md`](../../../analisi-pre/db/contract_documents.md),
> [`docs/03-gestione-dati.md` §3.2 (nomi colonna utente), §3.6 (registro append-only)](../../../progettazione/03-gestione-dati.md),
> [`docs/11-modulo-privacy.md` §1, §4 (`ipAddress`)](../../../progettazione/11-modulo-privacy.md),
> [`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md).
>
> **Nota sulla voce "Legacy"**: `nome_tabella.campo` di origine; `— (nuovo)` = campo senza
> corrispondente legacy.

---

## Descrizione

**Registro append-only** delle accettazioni (e revoche) dei documenti contrattuali di
[`identity_account_contract_documents`](identity_account_contract_documents.md) da parte di uno
stabilimento: **chi**, **quando**, **da quale IP**, **con quale hash** del documento accettato.

Differenza chiave dal legacy: `contract_acceptation` aveva `propertyId` **`UNIQUE`** → **una sola
riga per stabilimento**, aggiornata in place a ogni cambio di stato (`existing.update(payload)`),
perdendo lo storico e azzerando `acceptedAt` in revoca. Per un dato di **valenza legale** serve invece
un **registro append-only**: una riga per ogni **evento** (accettazione / revoca), con la versione e
l'hash del momento — [`docs/03-gestione-dati.md` §3.6](../../../progettazione/03-gestione-dati.md).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_identity_account_contract_acceptances` | `id` | PK | — |
| `ix_..._property_doc` | `(property_id, contract_document_id, occurred_at DESC)` | — | "stato corrente per stabilimento+documento" = ultima riga; storico ordinato |
| `ix_..._document` | `(contract_document_id)` | FK | quante property hanno accettato una versione |

## Campi

### id
- **Legacy**: `contract_acceptation.id`
- **Descrizione**: PK tecnica dell'evento.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.
- **Note**: —.

### property_id
- **Legacy**: `contract_acceptation.propertyId` (nel legacy con vincolo `UNIQUE` — rimosso: registro append-only)
- **Descrizione**: `property` che ha eseguito l'azione.
- **Tipo**: `uuid` — il **`public_id`** della `property` (C2), non il suo `bigint` interno.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 (`catalog`) — nessuna FK cross-modulo
  ([`docs/03-gestione-dati.md` §3.1ter](../../../progettazione/03-gestione-dati.md)). **Non** `UNIQUE` (append-only).
- **Note**: nel legacy era ricavato da `user.propertyId`, non dal client — comportamento da mantenere.

### contract_document_id
- **Legacy**: `contract_acceptation.contractDocumentsId`
- **Descrizione**: quale documento/versione è oggetto dell'evento.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`identity_account_contract_documents.id`](identity_account_contract_documents.md)
  `ON DELETE RESTRICT`.
- **Note**: snapshot: resta la versione dell'evento anche se la property viene poi puntata a un altro
  documento.

### event_type
- **Legacy**: `contract_acceptation.accepted` (booleano `0`/`1` sovrascritto in place → qui diventa un evento tipizzato)
- **Descrizione**: tipo di evento.
- **Tipo**: enum PG `identity.contract_acceptance_event` — valori: `accepted`, `revoked`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: sostituisce il booleano legacy `accepted` (0/1) sovrascritto in place. Lo stato corrente
  di (property, documento) = `event_type` dell'ultima riga per `occurred_at`.

### occurred_at
- **Legacy**: `contract_acceptation.acceptedAt`
- **Descrizione**: data/ora dell'azione.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: ex `contract_acceptation.acceptedAt` — ma **mai** rimesso a `NULL` (in revoca si scrive una
  **nuova riga** `revoked`, non si azzera la data dell'accettazione precedente).

### actor_user_id
- **Legacy**: `contract_acceptation.userId` (`req.userId` dal JWT, nessuna FK)
- **Descrizione**: account (admin/`superadmin`) che ha eseguito l'azione.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`identity_access_users.id`](identity_access_users.md) `ON DELETE RESTRICT`
  (attore di un fatto legale: non deve sparire).
- **Note**: ex `contract_acceptation.userId`. Nome allineato a
  [`docs/03-gestione-dati.md` §3.2](../../../progettazione/03-gestione-dati.md) (l'attore dell'evento).

### document_version_code
- **Legacy**: — (nuovo; snapshot tipizzato di `contract_documents.version`, che nel legacy si otteneva solo via join)
- **Descrizione**: `version_code` del documento **copiato** al momento dell'evento (snapshot leggibile).
- **Tipo**: `varchar(120)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: snapshot di
  [`identity_account_contract_documents.version_code`](identity_account_contract_documents.md).
- **Note**: [`docs/03-gestione-dati.md` §3.6 punto 2](../../../progettazione/03-gestione-dati.md): colonna
  tipizzata di snapshot accanto alla FK, per audit senza join a una riga che potrebbe cambiare stato.

### document_sha256
- **Legacy**: `contract_acceptation.documentHash`
- **Descrizione**: hash SHA-256 (hex) del PDF **accettato**, come prova di *quale contenuto esatto* è
  stato accettato.
- **Tipo**: `char(64)`.
- **Default**: nessuno.
- **Nullabile**: Sì (solo per `event_type = revoked`, dove non c'è download).
- **Relazioni**: —.
- **Note**: ex `contract_acceptation.documentHash`. Confrontabile con
  `identity_account_contract_documents.content_sha256` per rilevare divergenze.

### ip_address
- **Legacy**: `contract_acceptation.ipAddress` (`varchar(64)`)
- **Descrizione**: IP del client al momento dell'azione (audit legale).
- **Tipo**: `inet`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: ex `contract_acceptation.ipAddress` (`varchar(64)`, normalizzato: `::1`→`127.0.0.1`,
  primo valore di `X-Forwarded-For`). **Dato personale**: retention = durata utile a fini probatori,
  poi **troncamento dell'ultimo ottetto** dopo 24 mesi ([`docs/11-modulo-privacy.md` §4](../../../progettazione/11-modulo-privacy.md),
  [`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md)). `inet` supporta sia IPv4 sia IPv6.

### user_agent
- **Legacy**: — (nuovo; rafforza il valore probatorio)
- **Descrizione**: user-agent del client (audit).
- **Tipo**: `varchar(400)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: nuovo (rafforza il valore probatorio).

### created_at / updated_at
- **Legacy**: `contract_acceptation.createdAt` / `contract_acceptation.updatedAt`
- **Descrizione**: creazione della riga / (per uniformità) ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `created_at` coincide con `occurred_at`. La tabella è **append-only**: `updated_at` non
  cambia mai dopo l'inserimento (nessun update funzionale; solo eventuale pseudonimizzazione IP da
  retention, che è un'operazione tracciata a parte).

### deleted_at
- **Legacy**: — (nuovo; presente per uniformità, non usato — un registro legale non si cancella logicamente)
- **Descrizione**: presente per uniformità di schema, ma **non usato** funzionalmente.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: un registro legale non si "cancella" logicamente; la rimozione avviene solo per
  **hard-delete di retention** alla scadenza del termine (contratto + 10 anni). Coerente con
  [`docs/03-gestione-dati.md` §3.5](../../../progettazione/03-gestione-dati.md) (le tabelle di puro registro
  possono non implementare `ISoftDeletable`); la colonna resta per allineamento con la richiesta
  trasversale "ogni tabella: created_at, updated_at, deleted_at".

## Valutazione JSON

Nessun JSON: ogni fatto (chi/quando/IP/hash/versione) è una colonna tipizzata e indicizzabile —
[`docs/03-gestione-dati.md` §3.6 punto 2](../../../progettazione/03-gestione-dati.md).

## Suggerimenti per le prestazioni

- Volume trascurabile (feature mai attivata; a regime poche righe per stabilimento).
- L'accesso tipico è "stato corrente per (property, documento)": `ORDER BY occurred_at DESC LIMIT 1` →
  coperto da `ix_..._property_doc`.
- Append-only: nessun costo di UPDATE, nessun bloat da soft-delete.

## Implementazione .NET

Entità di **registro append-only** (modulo `Module.Identity`, sottoarea `Account`, schema `identity`).
`ContractAcceptanceEvent` è **immutabile** dopo l'inserimento: nessun metodo di update, solo la factory
`ContractAcceptanceEvent.Record(...)`. Non implementa `ISoftDeletable` funzionalmente (la colonna
`deleted_at` resta per uniformità di schema, §3.5 del template, ma non è mai valorizzata da codice
applicativo — solo da un eventuale job di retention hard-delete).

### Enum proprio

```csharp
// Domain/Enums/ContractAcceptanceEvent.cs (tipo evento)
public enum ContractAcceptanceEventType { Accepted = 0, Revoked = 1 }
```

```csharp
modelBuilder.HasPostgresEnum<ContractAcceptanceEventType>("identity", "contract_acceptance_event");
dataSourceBuilder.MapEnum<ContractAcceptanceEventType>("identity.contract_acceptance_event");
```

### `ContractAcceptanceEventConfiguration`

```csharp
namespace Module.Identity.Infrastructure.Configurations;

public sealed class ContractAcceptanceEventConfiguration : IEntityTypeConfiguration<ContractAcceptanceEvent>
{
	public void Configure(EntityTypeBuilder<ContractAcceptanceEvent> builder)
	{
		builder.ToTable("account_contract_acceptances", "identity", t => t.HasComment(
			"Registro append-only delle accettazioni/revoche dei documenti contrattuali da parte degli stabilimenti."));

		builder.HasKey(x => x.Id).HasName("pk_identity_account_contract_acceptances");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id della property (catalog) che ha eseguito l'azione, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.ContractDocumentId)
			.HasColumnName("contract_document_id")
			.IsRequired()
			.HasComment("Documento/versione oggetto dell'evento.");

		builder.Property(x => x.EventType)
			.HasColumnName("event_type")
			.IsRequired()
			.HasComment("Tipo di evento (accettazione o revoca).");

		builder.Property(x => x.OccurredAt)
			.HasColumnName("occurred_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora dell'azione.");

		builder.Property(x => x.ActorUserId)
			.HasColumnName("actor_user_id")
			.IsRequired()
			.HasComment("Account (admin/superadmin) che ha eseguito l'azione.");

		builder.Property(x => x.DocumentVersionCode)
			.HasColumnName("document_version_code")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("version_code del documento copiato al momento dell'evento (snapshot leggibile).");

		builder.Property(x => x.DocumentSha256)
			.HasColumnName("document_sha256")
			.HasColumnType("char(64)")
			.HasComment("Hash SHA-256 del PDF accettato, come prova del contenuto esatto accettato.");

		builder.Property(x => x.IpAddress)
			.HasColumnName("ip_address")
			.HasColumnType("inet")
			.HasComment("IP del client al momento dell'azione (audit legale); dato personale a retention limitata.");

		builder.Property(x => x.UserAgent)
			.HasColumnName("user_agent")
			.HasMaxLength(400)
			.HasComment("User-agent del client (rafforza il valore probatorio).");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune (§2, §6 del
		// template); deleted_at presente per uniformità, non usato funzionalmente (registro legale).

		builder.HasIndex(x => new { x.PropertyId, x.ContractDocumentId, x.OccurredAt })
			.HasDatabaseName("ix_identity_account_contract_acceptances_property_doc");

		builder.HasIndex(x => x.ContractDocumentId)
			.HasDatabaseName("ix_identity_account_contract_acceptances_document");

		builder.HasOne(x => x.ContractDocument)
			.WithMany()
			.HasForeignKey(x => x.ContractDocumentId)
			.HasConstraintName("fk_identity_account_contract_acceptances_document")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.ActorUser)
			.WithMany()
			.HasForeignKey(x => x.ActorUserId)
			.HasConstraintName("fk_identity_account_contract_acceptances_actor")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
```

### Query "stato corrente"

Proiezione read-model (o `SELECT DISTINCT ON (property_id, contract_document_id) ... ORDER BY
property_id, contract_document_id, occurred_at DESC`) sopra `ix_..._property_doc`: lo stato corrente di
(property, documento) è `EventType` dell'ultima riga per `OccurredAt`.

### Autorizzazione

Policy `admin`/`superadmin` esplicita sull'endpoint — non più un gate solo nel service con
`optionalAuth` a livello route come nel legacy.

### Note di migrazione

- Nessun dato da migrare (0 righe in produzione) — si crea solo lo schema.
- Job di retention: troncamento dell'ultimo ottetto di `IpAddress` dopo 24 mesi; hard-delete a
  contratto + 10 anni ([`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md)).
