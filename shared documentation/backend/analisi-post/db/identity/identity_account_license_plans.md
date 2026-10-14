# Tabella `identity_account_license_plans`

> **Dominio**: C5 — `identity` · **sottoarea**: `account` · **schema**: `identity` · **nome flat**:
> `identity_account_license_plans` · **forma canonica**: `identity.account_license_plans`.
>
> Rimpiazza la legacy [`licenses`](../../../analisi-pre/db/licenses.md). Fonti:
> [`analisi-pre/db/licenses.md`](../../../analisi-pre/db/licenses.md),
> [`analisi-pre/db/users.md`](../../../analisi-pre/db/users.md) (`license`, `startDate`, `endDate`,
> `subscriptionStatus`), [`docs/03-gestione-dati.md` §2.4 (`users.license` → FK, non enum)](../../../progettazione/03-gestione-dati.md),
> [`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md) (campi licenza/abbonamento restano
> in C5, isolati in sottoarea `account`).
>
> **Nota sulla voce "Legacy"**: `nome_tabella.campo` di origine; `— (nuovo)` = campo senza
> corrispondente legacy.

---

## Descrizione

Catalogo (**lookup**) dei piani di abbonamento della piattaforma.
[`identity_account_subscriptions.license_plan_id`](identity_account_subscriptions.md) vi punta con **FK
reale** — nel legacy `users.license` era una **stringa libera** che *per coincidenza* usava lo stesso
vocabolario di `licenses.type` senza alcun vincolo (con il valore sporco `""` su 78 righe dev).

Resta **tabella di lookup**, non un `enum`: un piano nuovo è un dato gestibile dal SuperAdmin senza
release ([`docs/03-gestione-dati.md` §2.4 riga `licenses.type`](../../../progettazione/03-gestione-dati.md)).

**Contenuto seed** (2 righe, dai `licenses` di produzione, create il 2021-03-18, mai modificate).
`duration_months` è ora solo un **default di comodo** (i contratti hanno date esplicite — vedi
[`identity_account_subscriptions`](identity_account_subscriptions.md)). Le **etichette tradotte** non
stanno qui ma in [`identity_account_license_plan_translations`](identity_account_license_plan_translations.md)
(N lingue):

| id | code | duration_months (default) | etichette |
|---|---|---|---|
| 1 | `basic` | 3 | → `identity_account_license_plan_translations` (`it`=Basic, `en`=Basic) |
| 2 | `advanced` | 12 | → `identity_account_license_plan_translations` (`it`=Advanced, `en`=Advanced) |

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_identity_account_license_plans` | `id` | PK | — |
| `uq_identity_account_license_plans_code` | `code` | UNIQUE | riferimento stabile per codice |

*(Nessun `public_id`: referenziata solo entro lo schema `identity`.)*

## Campi

### id
- **Legacy**: `licenses.id`
- **Descrizione**: PK tecnica del piano.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY` (seed forzato 1–2 in migrazione).
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziato da [`identity_account_subscriptions.license_plan_id`](identity_account_subscriptions.md).
- **Note**: —.

### code
- **Legacy**: `licenses.type`
- **Descrizione**: codice tecnico del piano.
- **Tipo**: `varchar(40)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: referenziato da
  [`identity_account_license_plan_translations.license_plan_id`](identity_account_license_plan_translations.md)
  (via `id`).
- **Note**: `UNIQUE`. Valori attuali `basic`, `advanced` (ex `licenses.type`). Il gating del menu
  gestionale (legacy `SideBar.js`: `licenseType !== "basic"`) diventa un controllo sul `code` del
  piano collegato alla subscription. **Non** è tradotto (è tecnico); l'etichetta mostrata viene dalle
  traduzioni.

### ~~label_it / label_en~~ → tabella figlia di traduzioni (nessuna colonna a DB)
- **Legacy**: — (nel legacy `licenses` aveva solo `type` + `months`, nessuna etichetta)
- **Valutazione (richiesta committente: servono altre lingue oltre IT/EN?)**: **sì, plausibile** —
  e a differenza dei ruoli (chiusi, code-bound → risorse di localizzazione, vedi
  [`identity_access_roles`](identity_access_roles.md)) i **piani sono gestiti a runtime dal SuperAdmin**
  (un piano nuovo può nascere senza deploy) e portano **testo commerciale** (nome + descrizione). Il
  posto giusto è quindi una **tabella di traduzioni per riga**, gestibile da pannello, N lingue:
  → [`identity_account_license_plan_translations`](identity_account_license_plan_translations.md)
  (`license_plan_id`, `culture`, `label`, `description`, `UNIQUE(license_plan_id, culture)`).
- **Conseguenza sullo schema**: `identity_account_license_plans` **non** ha colonne di etichetta. È il
  pattern "tabella di lookup con metadati propri (più traduzioni)" di
  [`docs/03-gestione-dati.md` §2.2.B / §2.3](../../../progettazione/03-gestione-dati.md) — **non** una `label_it`,
  `label_en`, `label_de`, … che crescono a colonne (anti-pattern), né una tabella EAV globale (§2.5).

### duration_months
- **Legacy**: `licenses.months`
- **Descrizione**: durata **suggerita** del piano in mesi (3 per `basic`, 12 per `advanced`).
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: dopo l'indicazione del committente (contratti **a termine fisso** annuali/biennali/triennali
  con date esplicite), `duration_months` è solo un **default di comodo** per precompilare le date del
  periodo, **non** più la fonte di verità di `ends_at` (che sta su
  [`identity_account_subscriptions`](identity_account_subscriptions.md) /
  [`identity_account_subscription_periods`](identity_account_subscription_periods.md)). Resta comunque
  **server-side**: nel legacy `endDate` era calcolato da `req.body.licenseMonths` fornito dal client
  senza verifica ([`analisi-pre/db/licenses.md`](../../../analisi-pre/db/licenses.md): un client poteva
  chiedere `licenseMonths: 999`).

### is_active
- **Legacy**: — (nuovo)
- **Descrizione**: se `true`, il piano è proponibile per nuove sottoscrizioni.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: un piano ritirato resta referenziabile dalle subscription storiche.

### sort_order
- **Legacy**: — (nuovo)
- **Descrizione**: ordinamento di presentazione.
- **Tipo**: `smallint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: solo UI.

### created_at / updated_at
- **Legacy**: `licenses.createdAt` / `licenses.updatedAt`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: §3.3–§3.4. Migrazione: porta `licenses.createdAt`.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: soft-delete del piano (`NULL` ⇔ attivo).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: cancellabile solo se nessuna subscription lo referenzia (FK `RESTRICT`); di norma si usa
  `is_active = false`.

## Valutazione JSON

Nessun JSON. Le etichette multilingua **non** sono un blob `labels jsonb` sul piano: sono righe
tipizzate in [`identity_account_license_plan_translations`](identity_account_license_plan_translations.md)
(indicizzabili per `culture`, gestibili da pannello) — [`docs/03-gestione-dati.md` §2.5](../../../progettazione/03-gestione-dati.md).

## Suggerimenti per le prestazioni

- 2 righe (piano) + poche righe di traduzione: **cache in memoria** l'intero catalogo (piani + tutte le
  traduzioni) all'avvio, con invalidazione all'update; nessuna query a runtime.
- Seed in migration versionata.

## Implementazione .NET

Entità **lookup** (non un `enum` PG — gestita a runtime dal SuperAdmin, confermato dalla sezione
Descrizione) del modulo `Module.Identity`, sottoarea `Account`, schema `identity`. `LicensePlan` non è
aggregate root con eventi: `sealed class` semplice con collection figlia `Translations`.

Nessun `public_id`: referenziata solo entro lo schema `identity`
([`identity_account_subscriptions.license_plan_id`](identity_account_subscriptions.md)).

### `LicensePlanConfiguration`

```csharp
namespace Module.Identity.Infrastructure.Configurations;

public sealed class LicensePlanConfiguration : IEntityTypeConfiguration<LicensePlan>
{
	public void Configure(EntityTypeBuilder<LicensePlan> builder)
	{
		builder.ToTable("account_license_plans", "identity", t => t.HasComment(
			"Catalogo dei piani di abbonamento della piattaforma (lookup gestibile da pannello SuperAdmin)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_account_license_plans");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();
		// seed id 1–2 forzati via HasData, poi ALTER SEQUENCE ... RESTART in migration.

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasMaxLength(40)
			.IsRequired()
			.HasComment("Codice tecnico del piano (basic, advanced), non tradotto.");

		builder.Property(x => x.DurationMonths)
			.HasColumnName("duration_months")
			.IsRequired()
			.HasComment("Durata suggerita del piano in mesi; default di comodo per precompilare le date del periodo, mai fonte di verità di ends_at.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Se true, il piano è proponibile per nuove sottoscrizioni.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Ordinamento di presentazione.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune (§2, §6 del template).

		builder.HasIndex(x => x.Code)
			.IsUnique()
			.HasDatabaseName("uq_identity_account_license_plans_code");

		builder.HasData(
			new { Id = 1L, Code = "basic", DurationMonths = (short)3, IsActive = true, SortOrder = (short)0 },
			new { Id = 2L, Code = "advanced", DurationMonths = (short)12, IsActive = true, SortOrder = (short)10 });
	}
}
```

Nessuna colonna di etichetta: label e descrizione commerciale vivono in
[`identity_account_license_plan_translations`](identity_account_license_plan_translations.md)
(`LicensePlan.Translations`, `HasMany(...).WithOne()` configurato in quella scheda).

### Servizi applicativi

- `Subscription.Renew(...)` calcola `ends_at` da `DurationMonths` **solo** come default server-side —
  mai da input client (nel legacy `endDate` derivava da `req.body.licenseMonths` non verificato).
- Feature-gating del menu gestionale sul `Code` del piano collegato alla subscription attiva (mai
  sull'etichetta tradotta).
- Risoluzione etichetta: `ILicensePlanTranslationResolver.Resolve(planId, culture)` →
  `(label, description)`, con fallback a una `culture` di default se la traduzione richiesta manca.

### Note di migrazione

- Seed `HasData` (id 1–2) in migration versionata, preservati 1:1 da `licenses.id`.
- `users.license` (stringa libera, con `""` sporco su 78 righe dev) → FK reale; le righe `""` risolte
  al piano effettivo dello stabilimento o `basic` di default (decisione in fase di migrazione).
