# Tabella `fiskaly_vat_codes`

> **Dominio**: C4 — `fiskaly` · **sottoarea**: — · **schema**: `fiskaly` · **nome flat**:
> `fiskaly_vat_codes` · **forma canonica**: `fiskaly.vat_codes`.
>
> Tabella di **lookup nuova**: nel legacy il vocabolario vive nelle costanti
> [`back-pre/app/models/constants/fiskaly_consts.js`](../../../../../../../back-pre/app/models/constants/fiskaly_consts.js)
> (`FiskalyVatRate`, `FiskalyVatExemption`), esposto dagli endpoint
> `fiskaly-helper/vat-rates|vat-exemptions`, ma **non** usato come tipo di colonna (le voci
> `fiskaly_receipt.entries[].vat.code` erano stringhe libere). Fonti:
> [`analisi-pre/db/fiskaly_receipt.md`](../../../analisi-pre/db/fiskaly_receipt.md) (campo `entries`),
> [`docs/03-gestione-dati.md` §2.2.B, §2.3, §2.4ter](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

Catalogo (**lookup**) dei codici IVA e di esenzione ammessi da Fiskaly SIGN IT per le righe del
documento commerciale. Resta una **tabella di lookup**, non un `enum` PG, perché:

- il vocabolario **può crescere senza deploy** (nuove aliquote/casi Natura per modifica normativa —
  come per `identity_access_roles` e `catalog_pricing_calculation_methods`,
  [`docs/03 §2.2.B / §2.3`](../../../progettazione/03-gestione-dati.md));
- porta **metadati propri** (aliquota %, codice Natura N1–N6, ordinamento);
- è già di fatto un vocabolario con metadati (`fiskaly_consts.js`) servito da endpoint dedicati.

[`fiskaly_receipt_lines.vat_code_id`](fiskaly_receipt_lines.md) vi punta con **FK reale**; l'aliquota
applicata è comunque **congelata** sulla riga scontrino (`vat_rate`, `vat_kind`) come parte del fatto
fiscale.

**Etichette**: nessuna colonna `label_it`/`label_en` — vocabolario **code-bound**, testo da
**risorse di localizzazione** (`FiskalyVatCodes.*.resx`, chiave = `code`),
[`docs/03 §2.2 / §3.1quater.C`](../../../progettazione/03-gestione-dati.md).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `kind` | **enum** esclusivo: `vat_rate` / `vat_exemption`. Un codice è dell'uno o dell'altro tipo. |
| `is_active` | booleano singolo (non un gruppo). |

## Snapshot

Non applicabile (tabella di riferimento). Il valore "congelato" vive su
[`fiskaly_receipt_lines`](fiskaly_receipt_lines.md).

## Contenuto seed

| code | kind | rate | nature_code | note |
|---|---|---|---|---|
| `STANDARD` | `vat_rate` | `22.00` | — | aliquota ordinaria |
| `REDUCED_1` | `vat_rate` | `10.00` | — | — |
| `REDUCED_2` | `vat_rate` | `5.00` | — | — |
| `REDUCED_3` | `vat_rate` | `4.00` | — | — |
| `NOT_SUBJECT` | `vat_exemption` | `NULL` | `N2` | operazioni non soggette |
| `NOT_TAXABLE` | `vat_exemption` | `NULL` | `N3` | operazioni non imponibili |
| `CAUSE_1` | `vat_exemption` | `NULL` | `N1` | escluse ex art. 15 |
| `CAUSE_2` | `vat_exemption` | `NULL` | `N4` | esenti |
| `CAUSE_3` | `vat_exemption` | `NULL` | `N5` | regime del margine / IVA non esposta |
| `CAUSE_4` | `vat_exemption` | `NULL` | `N6` | inversione contabile (reverse charge) |

*(mappatura `code → Natura` da confermare con il commercialista in fase implementativa — vedi
[`_domande-committente.md`](../_domande-committente.md); i `code` sono quelli attesi da SIGN IT.)*

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_fiskaly_vat_codes` | `id` | PK | — |
| `uq_fiskaly_vat_codes_code` | `(code)` | UNIQUE | risoluzione per codice; FK da `fiskaly_receipt_lines` |
| `ix_fiskaly_vat_codes_active` | `(kind, sort_order) WHERE is_active` | btree parziale | popolamento delle `<select>` aliquote/esenzioni |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica (surrogata). Seed con `id` stabili.
- **Tipo**: `smallint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da `fiskaly_receipt_lines.vat_code_id`.
- **Note**: **solo interna** allo schema `fiskaly` → nessun `public_id`.

### code
- **Legacy**: `fiskaly_receipt.entries[].vat.code` (stringa libera; costanti `FiskalyVatRate` /
  `FiskalyVatExemption`)
- **Descrizione**: codice SIGN IT. Chiave di business, inviata a Fiskaly verbatim.
- **Tipo**: `varchar(20)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`; chiave delle risorse di localizzazione.

### kind
- **Legacy**: `fiskaly_receipt.entries[].vat.type` (`"VAT_RATE"` / `"VAT_EXEMPTION"`)
- **Descrizione**: se il codice è un'aliquota o un caso di esenzione.
- **Tipo**: `enum fiskaly.vat_kind` — valori: `vat_rate`, `vat_exemption`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `CHECK (kind = 'vat_rate') = (rate IS NOT NULL)` e
  `CHECK (kind = 'vat_exemption') = (nature_code IS NOT NULL)`.

### rate
- **Legacy**: `FiskalyVatRate` (22 / 10 / 5 / 4)
- **Descrizione**: aliquota IVA in percentuale. `NULL` per i codici di esenzione.
- **Tipo**: `numeric(5,2)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### nature_code
- **Legacy**: `FiskalyVatExemption` (Natura N1–N6)
- **Descrizione**: codice Natura per fatturazione elettronica / SdI. `NULL` per le aliquote.
- **Tipo**: `varchar(4)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### is_active
- **Legacy**: — (nuovo; nel legacy tutti i codici erano sempre "disponibili")
- **Descrizione**: se il codice è proponibile nelle UI. I codici storici non più validi restano per
  gli scontrini già emessi ma `is_active = false`.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### sort_order
- **Legacy**: — (nuovo)
- **Descrizione**: ordinamento nelle `<select>`.
- **Tipo**: `smallint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: audit della riga di lookup. `deleted_at` esposto per uniformità ma **non usato**
  (si usa `is_active`; un codice IVA non si "cancella").
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON. Il vocabolario, oggi in costanti JS, diventa righe di lookup.

## Suggerimenti per le prestazioni

- ~10 righe, praticamente statiche. Caricata una volta in memoria all'avvio (dizionario `code → riga`).
- Nessun accesso "caldo" a runtime: la FK da `fiskaly_receipt_lines` si risolve dalla cache.

## Implementazione .NET

`VatCode` è un'entità di **lookup** del modulo `Module.Fiskaly` (schema `fiskaly`): catalogo dei codici
IVA/esenzione ammessi da Fiskaly SIGN IT, seedato in migration e caricato in cache singleton all'avvio
(vocabolario quasi statico, ~10 righe). Referenziata con **FK reale** da
[`fiskaly_receipt_lines.vat_code_id`](fiskaly_receipt_lines.md).

### Enum propri

```csharp
// Domain/Enums/FiskalyVatKind.cs
public enum FiskalyVatKind { VatRate = 0, VatExemption = 1 }
```

```csharp
// FiskalyDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<FiskalyVatKind>("fiskaly", "vat_kind");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<FiskalyVatKind>("fiskaly.vat_kind");
```

`FiskalyVatKind` è definito qui e riusato senza ridefinizione da
[`fiskaly_receipt_lines.vat_kind`](fiskaly_receipt_lines.md) (valore congelato sulla riga scontrino).

### Configuration

```csharp
namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class VatCodeConfiguration : IEntityTypeConfiguration<VatCode>
{
	public void Configure(EntityTypeBuilder<VatCode> builder)
	{
		builder.ToTable("fiskaly_vat_codes", "fiskaly", t => t.HasComment(
			"Catalogo dei codici IVA e di esenzione ammessi da Fiskaly SIGN IT per le righe del documento commerciale."));

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_vat_codes");
		builder.Property(x => x.Id)
			.HasColumnType("smallint")
			.UseIdentityAlwaysColumn();

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasMaxLength(20)
			.IsRequired()
			.HasComment("Codice SIGN IT, chiave di business inviata a Fiskaly verbatim; chiave delle risorse di localizzazione.");
		builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_fiskaly_vat_codes_code");

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("fiskaly.vat_kind")
			.IsRequired()
			.HasComment("Se il codice è un'aliquota (VatRate) o un caso di esenzione (VatExemption).");

		builder.Property(x => x.Rate)
			.HasColumnName("rate")
			.HasColumnType("numeric(5,2)")
			.HasComment("Aliquota IVA in percentuale; NULL per i codici di esenzione.");

		builder.Property(x => x.NatureCode)
			.HasColumnName("nature_code")
			.HasMaxLength(4)
			.HasComment("Codice Natura (N1-N6) per fatturazione elettronica / SdI; NULL per le aliquote.");

		builder.ToTable(t => t.HasCheckConstraint(
			"ck_fiskaly_vat_codes_kind_coherence",
			"(kind = 'vat_rate' AND rate IS NOT NULL AND nature_code IS NULL) OR (kind = 'vat_exemption' AND nature_code IS NOT NULL AND rate IS NULL)"));

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Se il codice è proponibile nelle UI; i codici storici restano per gli scontrini già emessi con is_active=false.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.HasColumnType("smallint")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Ordinamento nelle select di aliquote/esenzioni.");
		builder.HasIndex(x => new { x.Kind, x.SortOrder })
			.HasFilter("is_active")
			.HasDatabaseName("ix_fiskaly_vat_codes_active");

		// created_at/updated_at/deleted_at — audit di uniformità; deleted_at non usato (si usa is_active)
	}
}
```

`VatCode` implementa `IAuditable`; **non** implementa `ISoftDeletable` nel senso operativo — la colonna
`deleted_at` è esposta per uniformità ma un codice IVA non si "cancella", si disattiva con `is_active`.

### Note di migrazione

- Seed in migration dalle costanti `FiskalyVatRate` / `FiskalyVatExemption` di
  `fiskaly_consts.js` (§"Contenuto seed" della scheda), con `id` stabili.
- Le `entries[].vat.code` storiche (su `fiskaly_receipt_lines`, ex JSON `entries`) si risolvono a
  `vat_code_id` per `code`; i valori fuori catalogo generano una nuova riga con `is_active = false`
  (mai una FK rotta).
- Endpoint applicativo `GET fiskaly/vat-codes` rimpiazza i due endpoint legacy `vat-rates` +
  `vat-exemptions`; etichette da risorse `.resx` (`FiskalyVatCodes.*.resx`, chiave = `code`), non da
  colonna.
- Mappatura `code → Natura` da confermare col commercialista in fase implementativa
  ([`_domande-committente.md`](../_domande-committente.md)); riconciliazione del vocabolario aliquote
  con `payment_ledger_*` / `payment_pos_charge_items` annotata in
  [`_feature-tabelle.md` → C3](../_feature-tabelle.md).
