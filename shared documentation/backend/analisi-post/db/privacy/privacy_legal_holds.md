# Tabella `privacy_legal_holds`

> **Dominio**: `Module.Privacy` — `privacy` · **sottoarea**: — · **schema**: `privacy` · **nome flat**:
> `privacy_legal_holds` · **forma canonica**: `privacy.legal_holds`.
>
> Tabella **nuova** (nessuna legacy: il legacy non ha alcun meccanismo di sospensione della
> cancellazione per contenzioso). Fonti:
> [`docs/11-modulo-privacy.md` §2 (`LegalHold`), §3 (eccezione contenzioso), §6](../../../progettazione/11-modulo-privacy.md).

---

## Descrizione

**Flag esplicito** che sospende una cancellazione/anonimizzazione automatica di retention per un singolo
soggetto (e, opzionalmente, per un singolo modulo) in presenza di un contenzioso/accertamento in corso —
[`docs/11-modulo-privacy.md` §3](../../../progettazione/11-modulo-privacy.md): la conservazione fiscale ordinaria
(10 anni, art. 2220 c.c.) si **estende** fino alla conclusione della vertenza. **Impostato manualmente
dall'ufficio legale/amministrativo, mai dal job automatico** (doc §6) — nessun job di retention scrive
mai su questa tabella, solo la **legge**.

Ogni job di retention di modulo ([`privacy_retention_policies`](privacy_retention_policies.md)) e ogni
esecuzione di una richiesta di cancellazione
([`privacy_data_subject_requests`](privacy_data_subject_requests.md)) deve verificare l'assenza di un hold
attivo per il soggetto **prima** di agire.

## Verifica di esclusività

Nessun gruppo di flag booleani in questa tabella: `released_at IS NULL` è l'unico stato binario
("attivo"/"rilasciato"), espresso come timestamp nullable (pattern standard del progetto per stato
attivo/chiuso, es. `identity_account_subscription_periods.closed_at`), non un booleano separato
ridondante.

## Snapshot

Non applicabile — nessun dato personale del soggetto copiato: solo `subject_id` (riferimento vivo) e i
metadati del contenzioso (`reason`, `case_reference`, testo libero **sulla causa**, non sul soggetto).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_privacy_legal_holds` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | id pubblico per il pannello legale/amministrativo |
| `ix_..._subject_active` | `(subject_kind, subject_id) WHERE released_at IS NULL AND deleted_at IS NULL` | parziale | verifica veloce "questo soggetto ha un hold attivo?" — interrogata da **ogni** job di retention prima di agire |
| `ix_..._module_active` | `(module_code) WHERE released_at IS NULL` | parziale | job per-modulo: i propri hold attivi |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico per il pannello legale/amministrativo (creazione/rilascio hold).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: stesso criterio di
  [`infra_integration_api_clients.public_id`](../infra/infra_integration_api_clients.md).

### subject_kind
- **Legacy**: — (nuovo)
- **Descrizione**: tipo di entità sotto hold.
- **Tipo**: `enum privacy.subject_kind` — valori: `customer`, `user` (stesso enum di
  [`privacy_data_subject_requests`](privacy_data_subject_requests.md)).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### subject_id
- **Legacy**: — (nuovo)
- **Descrizione**: il soggetto sotto hold — `public_id` copiato per valore, stesso pattern di
  [`privacy_data_subject_requests.subject_id`](privacy_data_subject_requests.md).
- **Tipo**: `uuid`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: → `public_id` di `identity_customer_customers` o `identity_access_users` a seconda di
  `subject_kind`.

### module_code
- **Legacy**: — (nuovo)
- **Descrizione**: se valorizzato, l'hold si applica **solo** a quel modulo (es. contenzioso
  esclusivamente fiscale → hold solo su `payment`/`fiskaly`); se `NULL`, si applica a **tutti** i moduli
  coinvolti nel trattamento di quel soggetto.
- **Tipo**: `enum privacy.module_code` (stesso vocabolario di
  [`privacy_data_subject_request_module_outcomes.module_code`](privacy_data_subject_request_module_outcomes.md)).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### reason
- **Legacy**: — (nuovo)
- **Descrizione**: motivo del contenzioso/hold — obbligatorio per audit (Art. 5.2 accountability): un
  hold non motivato non è impostabile.
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### case_reference
- **Legacy**: — (nuovo)
- **Descrizione**: riferimento pratica legale/numero causa (facoltativo, per rintracciabilità presso
  l'ufficio legale).
- **Tipo**: `varchar(120)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### placed_by_user_id
- **Legacy**: — (nuovo)
- **Descrizione**: chi ha impostato l'hold — **sempre** un umano dell'ufficio legale/amministrativo, mai
  un job (doc §6, vincolo esplicito).
- **Tipo**: `uuid` (= `public_id` di [`identity_access_users`](../identity/identity_access_users.md)).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: riferimento cross-modulo a C5 per valore.

### released_at
- **Legacy**: — (nuovo)
- **Descrizione**: data/ora di rilascio dell'hold (chiusura del contenzioso). `NULL` = hold attivo.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### released_by_user_id
- **Legacy**: — (nuovo)
- **Descrizione**: chi ha rilasciato l'hold — stesso vincolo di `placed_by_user_id` (umano, non job).
- **Tipo**: `uuid` (= `public_id` di [`identity_access_users`](../identity/identity_access_users.md)).
- **Default**: `NULL`.
- **Nullabile**: Sì — valorizzato solo insieme a `released_at`.
- **Relazioni**: riferimento cross-modulo a C5 per valore.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione dell'hold / ultima modifica (tipicamente il rilascio).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: presente per uniformità di schema, **non usato** funzionalmente.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: un hold rilasciato **non** si cancella (`released_at` valorizzato, riga mantenuta) — è la
  prova che il vincolo è stato gestito correttamente e per quanto tempo, stessa logica registro-legale di
  [`identity_account_contract_acceptances`](../identity/identity_account_contract_acceptances.md).

## Valutazione JSON

Nessun JSON: ogni fatto (chi, quando, perché, per quale soggetto/modulo) è una colonna tipizzata.

## Suggerimenti per le prestazioni

- Volume atteso molto basso (contenziosi sono l'eccezione, non la norma) — tabella piccola anche a piena
  scala.
- L'indice parziale `WHERE released_at IS NULL` è quello davvero critico: è interrogato da **ogni** job di
  retention di **ogni** modulo prima di ogni azione — deve restare piccolo e velocissimo; essendo
  parziale, la sua dimensione è proporzionale ai soli hold attivi (tipicamente pochissimi), non alla
  storia completa.
- Nessun bisogno di partizionamento.

## Implementazione .NET

**Aggregate root** `LegalHold`, modulo `Module.Privacy`, schema `privacy`. Vita propria via pannello
legale/amministrativo: creazione e rilascio riservati a un ruolo dedicato (`legal`/`systemadmin`), **mai**
raggiungibili da un job schedulato (vincolo esplicito, doc §6). Espone il servizio condiviso
`ILegalHoldChecker.IsOnHold(subjectKind, subjectId, moduleCode)`, interrogato da ogni `IRetentionJob` di
modulo (vedi [`privacy_retention_policies`](privacy_retention_policies.md)) e dal saga di
[`privacy_data_subject_requests`](privacy_data_subject_requests.md) **prima** di applicare
`erased`/`anonymized` — un hold attivo trovato durante l'esecuzione di una richiesta di cancellazione fa
transizionare l'outcome del modulo interessato a `retained_legal_hold`.

`IAuditable` (`created_at`/`updated_at`). **Non** implementa `ISoftDeletable`: `deleted_at` è presente per
uniformità di schema ma non usato — un hold rilasciato non si cancella (`released_at` valorizzato, riga
mantenuta come prova), stessa logica registro-legale delle altre tabelle del modulo. Lo stato
attivo/rilasciato è un timestamp nullable (`released_at IS NULL` = attivo), non un booleano ridondante.

### Enum riusati

`subject_kind` e `module_code` **non** sono ridefiniti qui: sono gli stessi enum già dichiarati in
[`privacy_data_subject_requests`](privacy_data_subject_requests.md) (`SubjectKind` →
`privacy.subject_kind`) e in
[`privacy_data_subject_request_module_outcomes`](privacy_data_subject_request_module_outcomes.md)
(`ModuleCode` → `privacy.module_code`); qui `module_code` è semplicemente **nullable** (`NULL` = hold su
tutti i moduli coinvolti).

### Configuration

```csharp
namespace Module.Privacy.Infrastructure.Configurations;

public sealed class LegalHoldConfiguration : IEntityTypeConfiguration<LegalHold>
{
	public void Configure(EntityTypeBuilder<LegalHold> builder)
	{
		builder.ToTable("privacy_legal_holds", "privacy", t => t.HasComment(
			"Flag esplicito che sospende cancellazione/anonimizzazione automatica di retention per un " +
			"soggetto (ed eventualmente un singolo modulo) in presenza di un contenzioso in corso. " +
			"Impostato manualmente dall'ufficio legale, mai da un job."));

		builder.HasKey(x => x.Id).HasName("pk_privacy_legal_holds");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico per il pannello legale/amministrativo.");

		builder.Property(x => x.SubjectKind)
			.HasColumnName("subject_kind")
			.HasComment("Tipo di entità sotto hold: customer o user.");

		builder.Property(x => x.SubjectId)
			.HasColumnName("subject_id")
			.HasComment("public_id copiato per valore del soggetto sotto hold (identity_customer_customers " +
				"o identity_access_users secondo subject_kind) — nessuna FK cross-schema.");

		builder.Property(x => x.ModuleCode)
			.HasColumnName("module_code")
			.HasComment("Se valorizzato, l'hold si applica solo a quel modulo; se NULL, a tutti i moduli " +
				"coinvolti nel trattamento del soggetto.");

		builder.Property(x => x.Reason)
			.HasColumnName("reason")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Motivo del contenzioso/hold, obbligatorio per audit Art. 5.2 accountability.");

		builder.Property(x => x.CaseReference)
			.HasColumnName("case_reference")
			.HasMaxLength(120)
			.HasComment("Riferimento pratica legale/numero causa, per rintracciabilità presso l'ufficio legale.");

		builder.Property(x => x.PlacedByUserId)
			.HasColumnName("placed_by_user_id")
			.HasComment("public_id di identity_access_users che ha impostato l'hold, copiato — sempre un " +
				"umano dell'ufficio legale/amministrativo, mai un job; nessuna FK cross-schema.");

		builder.Property(x => x.ReleasedAt)
			.HasColumnName("released_at")
			.HasComment("Data/ora di rilascio dell'hold (chiusura del contenzioso). NULL = hold attivo.");

		builder.Property(x => x.ReleasedByUserId)
			.HasColumnName("released_by_user_id")
			.HasComment("public_id di identity_access_users che ha rilasciato l'hold, copiato — valorizzato " +
				"solo insieme a released_at, sempre un umano; nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Creazione dell'hold.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica, tipicamente il rilascio.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Presente per uniformità di schema, non usato funzionalmente — un hold rilasciato " +
				"non si cancella, resta come prova.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_privacy_legal_holds_public_id");

		builder.HasIndex(x => new { x.SubjectKind, x.SubjectId })
			.HasFilter("released_at IS NULL AND deleted_at IS NULL")
			.HasDatabaseName("ix_privacy_legal_holds_subject_active");

		builder.HasIndex(x => x.ModuleCode)
			.HasFilter("released_at IS NULL")
			.HasDatabaseName("ix_privacy_legal_holds_module_active");
	}
}
```

### Relazioni

- `subject_id`, `placed_by_user_id`, `released_by_user_id`: riferimenti cross-modulo per valore (`Guid`
  copiato da identity), solo `.HasComment`, nessuna FK EF.
- Nessuna FK interna allo schema `privacy` su questa tabella: è consultata (mai referenziata) dal saga di
  `privacy_data_subject_requests` e dai job di retention tramite `ILegalHoldChecker`, non tramite join SQL.

### Note di migrazione

Tabella **nuova**: nessun dato legacy (il legacy non ha alcun meccanismo di sospensione della cancellazione
per contenzioso). Solo schema, nessun seed necessario — le righe nascono una a una dall'azione dell'ufficio
legale.
