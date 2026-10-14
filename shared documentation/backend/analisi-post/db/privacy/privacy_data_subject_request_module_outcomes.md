# Tabella `privacy_data_subject_request_module_outcomes`

> **Dominio**: `Module.Privacy` — `privacy` · **sottoarea**: — · **schema**: `privacy` · **nome flat**:
> `privacy_data_subject_request_module_outcomes` · **forma canonica**:
> `privacy.data_subject_request_module_outcomes`.
>
> Tabella **nuova** (nessuna legacy). Fonti:
> [`docs/11-modulo-privacy.md` §2, §5](../../../progettazione/11-modulo-privacy.md) ("stato per modulo coinvolto",
> eventi `ErasureAppliedInModule`/esito per-modulo).

---

## Descrizione

**Figlia** di [`privacy_data_subject_requests`](privacy_data_subject_requests.md): una riga per **ogni
modulo coinvolto** in una richiesta, con l'esito che quel modulo ha applicato alla propria fetta di dati.
È il dettaglio che la testata (`status`) riassume: quando ogni riga attesa ha un `outcome` diverso da
`pending`, il saga consolida la richiesta madre in `completed` (tutti `erased`/`anonymized`/`exported`/
`not_applicable`) o `partially_completed` (almeno un `retained_legal_hold`/`retained_fiscal_obligation`).

Applica **letteralmente** i tre esiti descritti in
[`docs/11-modulo-privacy.md` §5 punto 2](../../../progettazione/11-modulo-privacy.md): nessun vincolo →
cancellazione fisica (`erased`); vincolo di integrità referenziale → anonimizzazione (`anonymized`);
vincolo di conservazione fiscale → non cancellare, motivazione esplicita
(`retained_fiscal_obligation`) — più le estensioni operative necessarie (`not_applicable`, `exported`,
`retained_legal_hold`, `failed`, vedi campo `outcome`).

## Verifica di esclusività

`outcome` è una colonna singola (enum), non un gruppo di flag booleani (`is_erased`/`is_anonymized`/…): un
modulo ha **esattamente un** esito per richiesta, garantito per costruzione dal tipo e
dall'indice `UNIQUE(request_id, module_code)`.

## Snapshot

Non applicabile — nessun dato personale del soggetto (regola del modulo, doc §2): solo l'esito del
trattamento (`outcome`, `outcome_detail` testuale motivazionale, `retained_until`).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_privacy_data_subject_request_module_outcomes` | `id` | PK | — |
| `uq_..._request_module` | `(request_id, module_code)` | UNIQUE | un modulo risponde una sola volta per richiesta |
| `ix_..._pending` | `(outcome) WHERE outcome = 'pending'` | parziale | coda cross-richiesta "cosa manca ancora" per il monitoraggio SLA |
| `ix_..._module` | `(module_code, outcome)` | — | dashboard per modulo (es. "quante retention fiscali attive nel modulo Payment") |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### request_id
- **Legacy**: — (nuovo)
- **Descrizione**: richiesta a cui l'esito appartiene.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`privacy_data_subject_requests.id`](privacy_data_subject_requests.md)
  `ON DELETE CASCADE` (FK interna allo schema `privacy`: `bigint`, non `public_id`).

### module_code
- **Legacy**: — (nuovo)
- **Descrizione**: modulo/dominio proprietario dei dati che ha risposto.
- **Tipo**: `enum privacy.module_code` — valori: `identity`, `catalog`, `booking`, `payment`, `fiskaly`,
  `fiscal_print`, `notification`, `switchboard`, `chatbot`, `infra`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: vocabolario chiuso = i domini applicativi del progetto (§ `_feature-tabelle.md`); cambia
  solo quando nasce un nuovo modulo (evento raro, già un deploy) → enum, non lookup, stesso criterio di
  [`infra_integration_api_clients.app_name`](../infra/infra_integration_api_clients.md).
- **Note**: non ogni richiesta coinvolge tutti i moduli — un modulo senza alcun dato per quel soggetto
  risponde comunque con `outcome = not_applicable` (traccia esplicita "verificato, nulla da fare"), non
  con l'assenza della riga.

### outcome
- **Legacy**: — (nuovo)
- **Descrizione**: esito applicato da quel modulo.
- **Tipo**: `enum privacy.module_outcome` — valori: `pending` (in attesa di risposta), `not_applicable`
  (il modulo non possiede dati per questo soggetto), `erased` (cancellazione fisica), `anonymized`
  (sostituito con placeholder, id/importi mantenuti), `retained_legal_hold` (trattenuto per
  [`privacy_legal_holds`](privacy_legal_holds.md) attivo), `retained_fiscal_obligation` (trattenuto per
  obbligo di legge, Art. 17.3.b), `exported` (solo `request_kind = export`: il modulo ha prodotto il
  proprio sottoinsieme dati per il bundle), `failed` (errore tecnico, richiede intervento manuale).
- **Default**: `pending`.
- **Nullabile**: No.
- **Relazioni**: —.

### outcome_detail
- **Legacy**: — (nuovo)
- **Descrizione**: motivazione leggibile (es. "trattenuto fino al 2036 per obbligo fiscale art. 2220
  c.c."), mostrata al richiedente nella risposta "accolta parzialmente" (doc §5 punto 3).
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì — tipicamente valorizzato per `retained_*`/`failed`.
- **Relazioni**: —.

### retained_until
- **Legacy**: — (nuovo)
- **Descrizione**: data fino a cui il vincolo di conservazione è previsto durare, quando calcolabile (es.
  scadenza del termine fiscale decennale). Permette un job di ri-tentativo automatico alla scadenza, senza
  dover riaprire manualmente la richiesta.
- **Tipo**: `date`.
- **Default**: `NULL`.
- **Nullabile**: Sì — valorizzato solo per `retained_fiscal_obligation` (calcolabile) e opzionalmente
  `retained_legal_hold` (se il contenzioso ha una scadenza nota; spesso non calcolabile a priori).
- **Relazioni**: —.

### responded_at
- **Legacy**: — (nuovo)
- **Descrizione**: data/ora in cui il modulo ha applicato l'esito e pubblicato l'evento di risposta.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì — `NULL` finché `outcome = pending`.
- **Relazioni**: —.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione della riga (alla pubblicazione dell'evento verso il modulo) / ultima modifica
  (la risposta, o un ri-tentativo su `retained_*` a scadenza).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: presente per uniformità di schema, **non usato** funzionalmente — segue lo stesso
  ciclo di vita (registro di conformità) della testata.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON: ogni esito è una riga tipizzata (§3.6 punto 2 dello stile snapshot, applicato qui a un esito
di workflow anziché a un fatto storico di dominio).

## Suggerimenti per le prestazioni

- Cardinalità: poche righe per richiesta (un dominio applicativo su 10 al massimo) — tabella comunque
  piccola anche a piena scala.
- `UNIQUE(request_id, module_code)` è anche l'indice naturale per il join più frequente ("tutti gli esiti
  di una richiesta", usato dal saga per verificare il consolidamento).
- Nessun bisogno di partizionamento.

## Implementazione .NET

**Entità figlia** `ModuleOutcome`, interna all'aggregato `DataSubjectRequest` (modulo `Module.Privacy`,
schema `privacy`): nessun aggregate root proprio, nessun repository dedicato — le scritture passano sempre
dalla testata [`privacy_data_subject_requests`](privacy_data_subject_requests.md), che sottoscrive
`ErasureAppliedInModule(requestId, modulo, esito)`/`ExportProducedInModule(...)` pubblicati da ciascun
modulo proprietario, aggiorna la riga corrispondente e verifica il consolidamento di `Status` sulla
testata quando ogni riga attesa è uscita da `pending`.

`IAuditable` (`created_at`/`updated_at`). **Non** implementa `ISoftDeletable`: `deleted_at` è presente per
uniformità di schema ma non usato — stesso ciclo di vita da registro di conformità della testata (nessun
query filter globale su questa entità).

### Enum propri

```csharp
// Domain/Enums/ModuleCode.cs
public enum ModuleCode
{
	Identity = 0,
	Catalog = 1,
	Booking = 2,
	Payment = 3,
	Fiskaly = 4,
	FiscalPrint = 5,
	Notification = 6,
	Switchboard = 7,
	Chatbot = 8,
	Infra = 9
}

// Domain/Enums/ModuleOutcomeResult.cs
public enum ModuleOutcomeResult
{
	Pending = 0,
	NotApplicable = 1,
	Erased = 2,
	Anonymized = 3,
	RetainedLegalHold = 4,
	RetainedFiscalObligation = 5,
	Exported = 6,
	Failed = 7
}
```

```csharp
// PrivacyDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ModuleCode>("privacy", "module_code");
modelBuilder.HasPostgresEnum<ModuleOutcomeResult>("privacy", "module_outcome");
```

```csharp
// Registrazione data source (Module.cs / Infrastructure DI)
dataSourceBuilder.MapEnum<ModuleCode>("privacy.module_code");
dataSourceBuilder.MapEnum<ModuleOutcomeResult>("privacy.module_outcome");
```

### Configuration

```csharp
namespace Module.Privacy.Infrastructure.Configurations;

public sealed class ModuleOutcomeConfiguration : IEntityTypeConfiguration<ModuleOutcome>
{
	public void Configure(EntityTypeBuilder<ModuleOutcome> builder)
	{
		builder.ToTable("privacy_data_subject_request_module_outcomes", "privacy", t => t.HasComment(
			"Esito applicato da un singolo modulo a una richiesta dell'interessato: una riga per ogni " +
			"modulo coinvolto, dettaglio che la testata riassume in status."));

		builder.HasKey(x => x.Id).HasName("pk_privacy_data_subject_request_module_outcomes");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.RequestId)
			.HasColumnName("request_id")
			.HasComment("Richiesta a cui l'esito appartiene.");

		builder.Property(x => x.ModuleCode)
			.HasColumnName("module_code")
			.HasComment("Modulo/dominio proprietario dei dati che ha risposto (vocabolario chiuso dei " +
				"domini applicativi del progetto).");

		builder.Property(x => x.Outcome)
			.HasColumnName("outcome")
			.HasDefaultValue(ModuleOutcomeResult.Pending)
			.HasComment("Esito applicato da quel modulo: pending, not_applicable, erased, anonymized, " +
				"retained_legal_hold, retained_fiscal_obligation, exported o failed.");

		builder.Property(x => x.OutcomeDetail)
			.HasColumnName("outcome_detail")
			.HasColumnType("text")
			.HasComment("Motivazione leggibile mostrata al richiedente nella risposta parzialmente accolta.");

		builder.Property(x => x.RetainedUntil)
			.HasColumnName("retained_until")
			.HasColumnType("date")
			.HasComment("Data fino a cui il vincolo di conservazione è previsto durare, quando calcolabile " +
				"(tipicamente retained_fiscal_obligation); abilita il job di ri-tentativo automatico.");

		builder.Property(x => x.RespondedAt)
			.HasColumnName("responded_at")
			.HasComment("Data/ora in cui il modulo ha applicato l'esito e pubblicato l'evento di risposta; " +
				"NULL finché outcome = pending.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga, alla pubblicazione dell'evento verso il modulo.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica: la risposta del modulo, o un ri-tentativo su un esito trattenuto.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Presente per uniformità di schema, non usato funzionalmente — stesso ciclo di " +
				"vita da registro di conformità della testata.");

		builder.HasIndex(x => new { x.RequestId, x.ModuleCode })
			.IsUnique()
			.HasDatabaseName("uq_privacy_data_subject_request_module_outcomes_request_module");

		builder.HasIndex(x => x.Outcome)
			.HasFilter("outcome = 'pending'")
			.HasDatabaseName("ix_privacy_data_subject_request_module_outcomes_pending");

		builder.HasIndex(x => new { x.ModuleCode, x.Outcome })
			.HasDatabaseName("ix_privacy_data_subject_request_module_outcomes_module");

		builder.HasOne(x => x.Request)
			.WithMany(r => r.ModuleOutcomes)
			.HasForeignKey(x => x.RequestId)
			.HasConstraintName("fk_privacy_data_subject_request_module_outcomes_request")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

### Relazioni

- `request_id` → [`privacy_data_subject_requests.id`](privacy_data_subject_requests.md): FK reale interna
  allo schema `privacy` (`bigint`, non `public_id`), `ON DELETE CASCADE` — configurata qui, unico punto
  dove la relazione è definita.
- Nessun riferimento cross-modulo diretto su questa tabella: `module_code` è un enum chiuso, non un id
  copiato da un altro schema.

### Note di migrazione

Tabella **nuova**: nessun dato legacy da migrare (0 righe all'avvio) — è il dettaglio per-modulo del saga
di orchestrazione GDPR, un meccanismo che non esisteva nel legacy.
