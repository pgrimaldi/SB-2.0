# Tabella `privacy_compliance_audit_logs`

> **Dominio**: `Module.Privacy` — `privacy` · **sottoarea**: — · **schema**: `privacy` · **nome flat**:
> `privacy_compliance_audit_logs` · **forma canonica**: `privacy.compliance_audit_logs`.
>
> Tabella **nuova** (nessuna legacy: nessuna traccia di audit sulle azioni di conformità dati nel legacy).
> Fonti: [`docs/11-modulo-privacy.md` §2 (`ComplianceAuditLog`), §6](../../../progettazione/11-modulo-privacy.md)
> (audit dei job di retention), [`docs/80-retention.md` §8](../../../progettazione/80-retention.md) ("Registro
> delle richieste GDPR: è la prova di conformità, non un dato da minimizzare").

---

## Descrizione

**Registro append-only** di ogni azione di cancellazione/anonimizzazione/export eseguita da un modulo
proprietario sui propri dati — riportata dai moduli stessi, non eseguita da qui. È la **prova di
conformità** richiesta dall'accountability (Art. 5.2 GDPR): per ogni riga toccata, **chi/cosa/quando/
perché** (doc §6: "ogni riga toccata (id, campo, azione, policy applicata, motivazione) loggata").

Due sorgenti distinte, entrambe tracciate qui con lo stesso schema:
1. **Job di retention schedulato** ([`privacy_retention_policies`](privacy_retention_policies.md)) →
   `retention_policy_id` valorizzato.
2. **Richiesta esplicita dell'interessato** ([`privacy_data_subject_requests`](privacy_data_subject_requests.md))
   → `data_subject_request_id` valorizzato.

Granularità **più fine** della richiesta/policy che l'ha generata: mentre la testata di una richiesta
resta come prova permanente (`privacy_data_subject_requests`, ~10 anni —
[`docs/80-retention.md` §7](../../../progettazione/80-retention.md), "Registro richieste GDPR"), questo registro
riga-per-riga-toccata ha una **finestra più corta** (1-2 anni, doc §6) — il riepilogo per soggetto resta
comunque disponibile nella testata/negli esiti-modulo anche dopo che il dettaglio granulare è stato
potato.

## Verifica di esclusività

Nessun gruppo di flag booleani: `action`/`performed_by` sono colonne enum singole. `retention_policy_id`
e `data_subject_request_id` **non** sono reciprocamente esclusive per vincolo di schema (entrambe
nullable, nessun `CHECK`): tipicamente una sola è valorizzata (l'azione nasce o da una policy schedulata o
da una richiesta), ma un'azione manuale ad-hoc dell'ufficio legale (fuori da entrambi i meccanismi
standard) è un caso legittimo con entrambe `NULL` — vincolarle a "esattamente una" escluderebbe quel caso
senza un beneficio pratico.

## Snapshot

Non applicabile — nessun dato personale del soggetto: la riga descrive **l'azione** (dove, cosa, perché),
mai il contenuto prima/dopo. Se serve un raffronto "cosa conteneva quel campo prima della cancellazione",
non è compito di questo registro: è un problema di backup/PITR (fuori perimetro, §7 di
[`docs/80-retention.md`](../../../progettazione/80-retention.md)).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_privacy_compliance_audit_logs` | `id` | PK | — |
| `ix_..._module_time` | `(module_code, occurred_at DESC)` | — | audit per modulo, ordine temporale (uso tipico: "cosa ha fatto Booking nell'ultimo mese") |
| `ix_..._target_row` | `(schema_name, table_name, record_id)` | — | storico completo delle azioni su una riga specifica |
| `ix_..._request` | `(data_subject_request_id) WHERE data_subject_request_id IS NOT NULL` | parziale | dettaglio riga-per-riga di una richiesta GDPR |
| `ix_..._policy` | `(retention_policy_id) WHERE retention_policy_id IS NOT NULL` | parziale | audit per policy di retention |


> ### ✅ Applicato nel backend (2026-09-21)
>
> Indice **BRIN** su `occurred_at` ([`docs/05-ottimizzazioni-db.md` §3](../../../progettazione/05-ottimizzazioni-db.md)), migration
> `AddBrinTimeIndexes`. Il partizionamento è escluso dalla scheda ("non necessario al lancio"): il BRIN è l'alternativa a costo pressoché nullo, utile al job di retention del modulo e alle estrazioni per periodo.
>
> BRIN e non B-tree perché la tabella è in **solo append** e l'ordine fisico dei blocchi coincide già
> con l'ordine temporale: l'indice memorizza un intervallo per blocco invece di una voce per riga, e
> costa una frazione in spazio e in manutenzione. Non sostituisce gli indici B-tree esistenti, che
> servono query diverse (tutte con un'altra colonna come prefisso).

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### occurred_at
- **Legacy**: — (nuovo)
- **Descrizione**: data/ora dell'azione.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### module_code
- **Legacy**: — (nuovo)
- **Descrizione**: modulo che ha eseguito l'azione sui propri dati.
- **Tipo**: `enum privacy.module_code` (stesso vocabolario di
  [`privacy_data_subject_request_module_outcomes.module_code`](privacy_data_subject_request_module_outcomes.md)).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### schema_name / table_name
- **Legacy**: — (nuovo)
- **Descrizione**: schema/tabella toccata (stesso significato di
  [`privacy_retention_policies.schema_name`](privacy_retention_policies.md)`/table_name`).
- **Tipo**: `varchar(63)` ciascuno.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: riferimento testuale, non FK (stesso motivo di `privacy_retention_policies`).

### record_id
- **Legacy**: — (nuovo)
- **Descrizione**: id interno (`bigint`) della riga toccata nello schema di origine — puramente
  informativo/di audit, **mai** una FK cross-schema (coerente con la regola generale
  [`docs/03-gestione-dati.md` §3.1ter](../../../progettazione/03-gestione-dati.md)); la riga sorgente può anche non
  esistere più al momento della lettura dell'audit (es. dopo un `delete_row`).
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No — doc §6: "ogni riga toccata" è tracciata individualmente, non in aggregato.
- **Relazioni**: nessuna FK (per costruzione).

### field_names
- **Legacy**: — (nuovo)
- **Descrizione**: campi toccati dall'azione (rilevante per `null_fields`/`anonymize`); `NULL`/vuoto per
  `delete_row`/`archive_cold` (l'intera riga).
- **Tipo**: `varchar(120)[]`.
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: —.

### action
- **Legacy**: — (nuovo)
- **Descrizione**: azione eseguita.
- **Tipo**: `enum privacy.audit_action` — valori: `delete_row`, `null_fields`, `anonymize`,
  `archive_cold` (stesso vocabolario di
  [`privacy_retention_policies.action`](privacy_retention_policies.md)) **+ `exported`** (il modulo ha
  prodotto/consegnato il proprio sottoinsieme dati per un bundle di export — azione che merita audit
  quanto una cancellazione, per lo stesso principio di accountability) **+ `delete_object`,
  `archive_object`** (aggiunti con la retention dei file, il 2026-10-03: oggetto rimosso dallo storage /
  spostato su classe fredda, in coppia con le azioni omonime di `privacy_retention_policies.action`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### retention_policy_id
- **Legacy**: — (nuovo)
- **Descrizione**: policy di retention che ha generato l'azione, se schedulata.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (vedi "Verifica di esclusività").
- **Relazioni**: **FK** → [`privacy_retention_policies.id`](privacy_retention_policies.md)
  `ON DELETE SET NULL` (l'audit sopravvive anche se la policy viene poi ritirata).

### data_subject_request_id
- **Legacy**: — (nuovo)
- **Descrizione**: richiesta GDPR che ha generato l'azione, se esplicita.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (vedi "Verifica di esclusività").
- **Relazioni**: **FK** → [`privacy_data_subject_requests.id`](privacy_data_subject_requests.md)
  `ON DELETE SET NULL`.

### reason
- **Legacy**: — (nuovo)
- **Descrizione**: motivazione/riferimento normativo dell'azione (ereditata dalla policy o dalla
  richiesta, o inserita a mano per un'azione ad-hoc).
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### performed_by
- **Legacy**: — (nuovo)
- **Descrizione**: chi ha eseguito materialmente l'azione.
- **Tipo**: `enum privacy.actor_kind` — valori: `system_job` (job di retention schedulato), `staff_user`
  (azione manuale di un operatore).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### performed_by_user_id
- **Legacy**: — (nuovo)
- **Descrizione**: operatore che ha eseguito l'azione, quando manuale.
- **Tipo**: `uuid` (= `public_id` di [`identity_access_users`](../identity/identity_access_users.md)).
- **Default**: `NULL`.
- **Nullabile**: Sì — valorizzato solo se `performed_by = staff_user`.
- **Relazioni**: riferimento cross-modulo a C5 per valore.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione della riga (coincide con `occurred_at`) / non usato funzionalmente.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: registro **append-only** — `updated_at` non cambia mai dopo l'inserimento, stesso pattern di
  [`identity_account_contract_acceptances`](../identity/identity_account_contract_acceptances.md).

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: presente per uniformità di schema, **non usato** dall'applicazione.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: la rimozione avviene solo per hard-delete della **propria** retention (1-2 anni, doc §6),
  applicata dal job come una riga [`privacy_retention_policies`](privacy_retention_policies.md)
  auto-riferita — vedi la nota "Auto-riferimento" nell'implementazione .NET di quella scheda.

## Valutazione JSON

Nessun JSON: ogni fatto d'audit è una colonna tipizzata, indicizzabile per modulo/tabella/riga/tempo.

## Suggerimenti per le prestazioni

- Tabella potenzialmente ad **alto volume** nel tempo (ogni riga toccata da ogni job di retention di ogni
  modulo genera una riga qui) — a differenza delle altre tabelle di questo modulo.
- La retention corta (1-2 anni, doc §6) è **strutturale al design**, non solo consigliata: mantiene la
  tabella limitata anche con job di retention molto attivi. Applicata dalla stessa
  [`privacy_retention_policies`](privacy_retention_policies.md) del modulo su sé stessa.
- `(module_code, occurred_at DESC)` è l'indice per il pattern di accesso principale (audit per modulo in
  ordine temporale); valutare in futuro il **partizionamento per range su `occurred_at`** (mensile) se il
  volume reale supera le decine di migliaia di righe/mese — non necessario al lancio.
- Scritture solo `INSERT` (append-only): nessun costo di `UPDATE`, nessun bloat da soft-delete durante la
  vita utile della riga.

## Implementazione .NET

**Registro append-only** `ComplianceAuditEntry`, modulo `Module.Privacy`, schema `privacy`: entità
immutabile, nessun metodo di update dopo la creazione. Scritta da una libreria condivisa
`IComplianceAuditWriter`, richiamata da ogni `IRetentionJob` di modulo (vedi
[`privacy_retention_policies`](privacy_retention_policies.md)) e da ogni handler
`ErasureAppliedInModule`/`ExportProducedInModule`, per non duplicare la logica di scrittura audit in
ciascun modulo — è la prova di conformità Art. 5.2, granularità riga-per-riga-toccata, più fine della
richiesta/policy che l'ha generata.

Espone solo `CreatedAt` (coincide con `occurred_at`): **non** implementa `IAuditable` nonostante la colonna
`updated_at` esista a schema — coerente con la regola generale per i log append-only (§1 del pattern
comune, "espongono al più `created_at`"), `UpdatedAt` non cambia mai dopo l'inserimento e non deve passare
dall'`AuditInterceptor`. **Non** implementa `ISoftDeletable`: `deleted_at` è presente per uniformità di
schema ma la sola rimozione ammessa è un **hard-delete** a 1-2 anni guidato dalla riga
`privacy_retention_policies` auto-riferita su questa stessa tabella (vedi le note di migrazione di quella
scheda).

### Enum propri e riusati

`module_code` **non** è ridefinito: stesso enum `ModuleCode` → `privacy.module_code` già dichiarato in
[`privacy_data_subject_request_module_outcomes`](privacy_data_subject_request_module_outcomes.md).

```csharp
// Domain/Enums/AuditAction.cs
public enum AuditAction { DeleteRow = 0, NullFields = 1, Anonymize = 2, ArchiveCold = 3, Exported = 4 }

// Domain/Enums/ActorKind.cs
public enum ActorKind { SystemJob = 0, StaffUser = 1 }
```

```csharp
// PrivacyDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<AuditAction>("privacy", "audit_action");
modelBuilder.HasPostgresEnum<ActorKind>("privacy", "actor_kind");
```

```csharp
// Registrazione data source (Module.cs / Infrastructure DI)
dataSourceBuilder.MapEnum<AuditAction>("privacy.audit_action");
dataSourceBuilder.MapEnum<ActorKind>("privacy.actor_kind");
```

### Configuration

```csharp
namespace Module.Privacy.Infrastructure.Configurations;

public sealed class ComplianceAuditEntryConfiguration : IEntityTypeConfiguration<ComplianceAuditEntry>
{
	public void Configure(EntityTypeBuilder<ComplianceAuditEntry> builder)
	{
		builder.ToTable("privacy_compliance_audit_logs", "privacy", t => t.HasComment(
			"Registro append-only di ogni azione di cancellazione/anonimizzazione/export eseguita da un " +
			"modulo proprietario sui propri dati — prova di conformità Art. 5.2 GDPR (chi/cosa/quando/perché)."));

		builder.HasKey(x => x.Id).HasName("pk_privacy_compliance_audit_logs");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.OccurredAt)
			.HasColumnName("occurred_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora dell'azione.");

		builder.Property(x => x.ModuleCode)
			.HasColumnName("module_code")
			.HasComment("Modulo che ha eseguito l'azione sui propri dati.");

		builder.Property(x => x.SchemaName)
			.HasColumnName("schema_name")
			.HasMaxLength(63)
			.IsRequired()
			.HasComment("Schema toccato — riferimento testuale, non FK.");

		builder.Property(x => x.TableName)
			.HasColumnName("table_name")
			.HasMaxLength(63)
			.IsRequired()
			.HasComment("Tabella toccata — riferimento testuale, non FK.");

		builder.Property(x => x.RecordId)
			.HasColumnName("record_id")
			.HasComment("Id interno (bigint) della riga toccata nello schema di origine — puramente " +
				"informativo, mai una FK cross-schema; la riga sorgente può non esistere più alla lettura.");

		builder.Property(x => x.FieldNames)
			.HasColumnName("field_names")
			.HasColumnType("varchar(120)[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Campi toccati dall'azione (null_fields/anonymize); vuoto per delete_row/archive_cold.");

		builder.Property(x => x.Action)
			.HasColumnName("action")
			.HasComment("Azione eseguita: delete_row, null_fields, anonymize, archive_cold o exported.");

		builder.Property(x => x.RetentionPolicyId)
			.HasColumnName("retention_policy_id")
			.HasComment("Policy di retention che ha generato l'azione, se schedulata.");

		builder.Property(x => x.DataSubjectRequestId)
			.HasColumnName("data_subject_request_id")
			.HasComment("Richiesta GDPR che ha generato l'azione, se esplicita.");

		builder.Property(x => x.Reason)
			.HasColumnName("reason")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Motivazione dell'azione, ereditata dalla policy/richiesta o inserita a mano.");

		builder.Property(x => x.PerformedBy)
			.HasColumnName("performed_by")
			.HasComment("Chi ha eseguito materialmente l'azione: system_job o staff_user.");

		builder.Property(x => x.PerformedByUserId)
			.HasColumnName("performed_by_user_id")
			.HasComment("public_id di identity_access_users, copiato — valorizzato solo se performed_by = " +
				"staff_user; nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga, coincide con occurred_at.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Presente per uniformità di schema, non usato: registro append-only, non cambia " +
				"mai dopo l'inserimento.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Presente per uniformità di schema, non usato dall'applicazione — la sola " +
				"rimozione è un hard-delete a scadenza della propria retention (1-2 anni).");

		builder.HasIndex(x => new { x.ModuleCode, x.OccurredAt })
			.HasDatabaseName("ix_privacy_compliance_audit_logs_module_time")
			.IsDescending(false, true);

		builder.HasIndex(x => new { x.SchemaName, x.TableName, x.RecordId })
			.HasDatabaseName("ix_privacy_compliance_audit_logs_target_row");

		builder.HasIndex(x => x.DataSubjectRequestId)
			.HasFilter("data_subject_request_id IS NOT NULL")
			.HasDatabaseName("ix_privacy_compliance_audit_logs_request");

		builder.HasIndex(x => x.RetentionPolicyId)
			.HasFilter("retention_policy_id IS NOT NULL")
			.HasDatabaseName("ix_privacy_compliance_audit_logs_policy");

		builder.HasOne(x => x.RetentionPolicy)
			.WithMany()
			.HasForeignKey(x => x.RetentionPolicyId)
			.HasConstraintName("fk_privacy_compliance_audit_logs_retention_policy")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(x => x.DataSubjectRequest)
			.WithMany()
			.HasForeignKey(x => x.DataSubjectRequestId)
			.HasConstraintName("fk_privacy_compliance_audit_logs_data_subject_request")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

### Relazioni

- `retention_policy_id` → [`privacy_retention_policies.id`](privacy_retention_policies.md): FK reale
  interna allo schema `privacy`, `ON DELETE SET NULL` (l'audit sopravvive al ritiro della policy).
- `data_subject_request_id` → [`privacy_data_subject_requests.id`](privacy_data_subject_requests.md): FK
  reale interna allo schema `privacy`, `ON DELETE SET NULL`.
- `performed_by_user_id`: riferimento cross-modulo per valore (`Guid` copiato da identity), solo
  `.HasComment`, nessuna FK EF. `schema_name`/`table_name`/`record_id` sono riferimenti testuali/numerici
  puramente informativi verso tabelle di altri moduli, mai una FK — per costruzione, dato che la riga
  sorgente può anche non esistere più al momento della lettura dell'audit.

### Note di migrazione

Tabella **nuova**: nessun dato legacy (nessuna traccia di audit sulle azioni di conformità dati nel
legacy). Solo schema; il job di hard-delete a 1-2 anni e il partizionamento per `occurred_at` (da valutare
solo se il volume reale post go-live supera le decine di migliaia di righe/mese) sono note operative, non
migration.
