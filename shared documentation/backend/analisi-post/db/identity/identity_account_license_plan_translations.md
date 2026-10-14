# Tabella `identity_account_license_plan_translations`

> **Dominio**: C5 — `identity` · **sottoarea**: `account` · **schema**: `identity` · **nome flat**:
> `identity_account_license_plan_translations` · **forma canonica**:
> `identity.account_license_plan_translations`.
>
> **Nuova** — etichette multilingua dei piani di
> [`identity_account_license_plans`](identity_account_license_plans.md). Introdotta per la domanda del
> committente ("`label_it`/`label_en` possono servire in altre lingue? gestirle in altro modo?"): i
> piani sono **gestiti a runtime dal SuperAdmin** e portano **testo commerciale** → non colonne
> per-lingua sul piano ([`docs/03-gestione-dati.md` §2.2.B / §2.3 / §2.5](../../../progettazione/03-gestione-dati.md)).
>
> **Nota sulla voce "Legacy"**: **nessun antecedente legacy** (nel legacy `licenses` aveva solo `type`
> + `months`, nessuna etichetta). Tutti i campi sono `— (nuovo)`.
>
> *(Per i **ruoli** — vocabolario chiuso e code-bound — la scelta è diversa: etichette da **risorse di
> localizzazione** del modulo, non da tabella — vedi [`identity_access_roles`](identity_access_roles.md).)*

---

## Descrizione

**Una riga per (piano × lingua)**: il nome commerciale e la descrizione del piano in una lingua. N
lingue senza limiti, aggiungibili da pannello senza deploy. La UI risolve l'etichetta per la lingua
dell'utente/richiesta, con **fallback** a una `culture` di default se la traduzione manca.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_identity_account_license_plan_translations` | `id` | PK | — |
| `uq_..._plan_culture` | `(license_plan_id, culture)` `WHERE deleted_at IS NULL` | UNIQUE parziale | una traduzione per lingua per piano |
| `ix_..._plan` | `(license_plan_id)` | FK | caricamento di tutte le traduzioni di un piano |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.
- **Note**: —.

### license_plan_id
- **Legacy**: — (nuovo)
- **Descrizione**: piano a cui la traduzione appartiene.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`identity_account_license_plans.id`](identity_account_license_plans.md)
  `ON DELETE CASCADE` (le traduzioni seguono il piano).
- **Note**: —.

### culture
- **Legacy**: — (nuovo)
- **Descrizione**: lingua (o lingua+regione) della traduzione.
- **Tipo**: `varchar(16)` — tag **BCP-47** (`it`, `en`, `de`, `fr`, `fr-CH`, …).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: insieme "di libreria" (non un enum di dominio — come `country`/`timezone`,
  [`docs/03-gestione-dati.md` §2.4quater](../../../progettazione/03-gestione-dati.md)); validare che sia un tag
  BCP-47 ben formato. Normalizzare la capitalizzazione (`it`, non `IT`; `fr-CH`, non `fr-ch`).

### label
- **Legacy**: — (nuovo)
- **Descrizione**: nome commerciale del piano in questa lingua (es. "Base", "Avanzato").
- **Tipo**: `varchar(80)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: mostrato in pannelli e pagine di upgrade abbonamento. Il **`code`** del piano
  (`basic`/`advanced`) resta tecnico e non tradotto — vedi
  [`identity_account_license_plans.code`](identity_account_license_plans.md).

### description
- **Legacy**: — (nuovo)
- **Descrizione**: testo descrittivo/commerciale del piano in questa lingua (feature incluse, ecc.).
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: opzionale; se assente, la UI mostra solo `label`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL, per scelta**. Le quattro
  righe di questa tabella sono seminate dalla migration EF Core (`HasData`), non dall'ETL:
  nel legacy non esiste alcuna descrizione commerciale dei piani da recuperare, quindi non
  c'è un dato mancante ma un dato che non è mai esistito. Chi scriverà la schermata dei
  piani deve prevedere che `description` possa essere vuota (decisione D6 di
  `analisi-pre/db-no-copied/_colonne-vuote.md`), non attenderne la comparsa da una migrazione futura.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / ultima modifica della riga.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: §3.3–§3.4.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: soft-delete della singola traduzione (`NULL` ⇔ attiva).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: rimuovere una lingua = `deleted_at` (l'unicità parziale libera subito `(piano, culture)`
  per un eventuale re-inserimento).

## Valutazione JSON

Nessun JSON: le traduzioni sono righe (indicizzabili per `culture`, gestibili singolarmente da
pannello), **non** un blob `labels jsonb` sul piano — [`docs/03-gestione-dati.md` §2.5](../../../progettazione/03-gestione-dati.md).

## Suggerimenti per le prestazioni

- Poche righe in totale (n. piani × n. lingue). Si **carica tutto in memoria** insieme al catalogo
  piani all'avvio; invalidazione all'update. Nessuna query a runtime.
- L'indice `(license_plan_id, culture)` serve il caricamento e garantisce l'unicità.

## Implementazione .NET

Entità figlia del lookup [`identity_account_license_plans`](identity_account_license_plans.md) (modulo
`Module.Identity`, sottoarea `Account`, schema `identity`). `LicensePlanTranslation` implementa
`ISoftDeletable`/`IAuditable`; nessun `public_id` (referenziata solo entro lo schema).

### `LicensePlanTranslationConfiguration`

```csharp
namespace Module.Identity.Infrastructure.Configurations;

public sealed class LicensePlanTranslationConfiguration : IEntityTypeConfiguration<LicensePlanTranslation>
{
	public void Configure(EntityTypeBuilder<LicensePlanTranslation> builder)
	{
		builder.ToTable("account_license_plan_translations", "identity", t => t.HasComment(
			"Etichette multilingua (nome commerciale + descrizione) dei piani di abbonamento."));

		builder.HasKey(x => x.Id).HasName("pk_identity_account_license_plan_translations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.LicensePlanId)
			.HasColumnName("license_plan_id")
			.IsRequired()
			.HasComment("Piano a cui la traduzione appartiene.");

		builder.Property(x => x.Culture)
			.HasColumnName("culture")
			.HasMaxLength(16)
			.IsRequired()
			.HasComment("Lingua (o lingua+regione) della traduzione, tag BCP-47.");

		builder.Property(x => x.Label)
			.HasColumnName("label")
			.HasMaxLength(80)
			.IsRequired()
			.HasComment("Nome commerciale del piano in questa lingua.");

		builder.Property(x => x.Description)
			.HasColumnName("description")
			.HasComment("Testo descrittivo/commerciale del piano in questa lingua.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune (§2, §6 del template).

		builder.HasIndex(x => new { x.LicensePlanId, x.Culture })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_identity_account_license_plan_translations_plan_culture");

		builder.HasIndex(x => x.LicensePlanId)
			.HasDatabaseName("ix_identity_account_license_plan_translations_plan");

		builder.HasOne(x => x.LicensePlan)
			.WithMany(p => p.Translations)
			.HasForeignKey(x => x.LicensePlanId)
			.HasConstraintName("fk_identity_account_license_plan_translations_plan")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasData(
			new { Id = 1L, LicensePlanId = 1L, Culture = "it", Label = "Basic" },
			new { Id = 2L, LicensePlanId = 1L, Culture = "en", Label = "Basic" },
			new { Id = 3L, LicensePlanId = 2L, Culture = "it", Label = "Advanced" },
			new { Id = 4L, LicensePlanId = 2L, Culture = "en", Label = "Advanced" });
	}
}
```

### Servizio di risoluzione

`ILicensePlanTranslationResolver.Resolve(planId, requestedCulture)` — cerca la riga viva per
`(planId, requestedCulture)`; se assente, fallback a `Auth:DefaultCulture` (o `it`). Validazione del tag
BCP-47 in scrittura (`CultureInfo.GetCultureInfo` in `try/catch`, o lista consentita), normalizzando la
capitalizzazione (`it`, non `IT`; `fr-CH`, non `fr-ch`).

### Note di migrazione

- Nessun antecedente legacy: seed `it`/`en` per i 2 piani esistenti (sopra) in migration versionata.
- Endpoint SuperAdmin per aggiungere/modificare lingue senza deploy (N lingue, nessun limite).
