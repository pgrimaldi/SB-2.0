# Tabella `identity_access_roles`

> **Dominio**: C5 — `identity` · **sottoarea**: `access` · **schema**: `identity` · **nome flat**:
> `identity_access_roles` · **forma canonica**: `identity.access_roles`.
>
> Rimpiazza la legacy [`roles`](../../../analisi-pre/db/roles.md). Fonti:
> [`analisi-pre/db/roles.md`](../../../analisi-pre/db/roles.md),
> [`analisi-pre/db/user_roles.md`](../../../analisi-pre/db/user_roles.md),
> `back-pre/app/middleware/authJwt.js` (verifica ruoli lato server),
> [`docs/03-gestione-dati.md` §2.2–§2.4](../../../progettazione/03-gestione-dati.md) (enum vs lookup),
> [`docs/pre/autenticazione-autorizzazione-dotnet.md`](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md).
>
> **Nota sulla voce "Legacy"**: per ogni campo indica la colonna legacy di origine (`nome_tabella.campo`);
> `— (nuovo)` = campo senza corrispondente legacy.

---

## Descrizione

Catalogo (**lookup**) dei ruoli applicativi. `identity_access_users.role_id` vi punta con **FK reale**
(nel legacy `user_roles.roleId` non aveva alcuna FK, e `roles.name` non aveva `UNIQUE` — con duplicati
già osservati, es. `ofsale` due volte).

Resta una **tabella di lookup**, non un `enum` PG, perché il vocabolario **cresce nel tempo senza
deploy** (`waiter` 2022, `affiliate`/`advancedlifeguard` 2025, `marketing` 2026) e porta metadati
propri (label tradotta, ordinamento, flag "assegnabile da pannello") —
[`docs/03-gestione-dati.md` §2.2.B / §2.3](../../../progettazione/03-gestione-dati.md). In C# è comunque risolto
una volta all'avvio in un tipo/costante condivisa, non confrontato per stringa in ogni handler come nel
legacy (che ha già prodotto il typo `requirelifeguardRole`).

**Contenuto seed** (13 righe, dai `roles` di produzione — `id` preservati 1:1). **Tutti i 13 ruoli sono
mantenuti** (indicazione del committente): nessuna dismissione, nemmeno per `user1` / `websiteadmin` /
`systemadmin`.

| id | code | label IT *(illustrativa — da `Roles.it.resx`, non colonna)* | assegnabile da pannello | note |
|---|---|---|---|---|
| 1 | `systemadmin` | Amministratore di sistema | no | back-office piattaforma (frontend legacy: `front-admin/`, in repo dal 2026-09-09); **mai** più un fallback implicito |
| 2 | `superadmin` | Titolare | no | assegnato solo alla registrazione stabilimento; `⊇ manager` |
| 3 | `consumer` | Cliente | no (auto in registrazione) | default cliente finale |
| 4 | `admin` | Amministratore | sì | — |
| 5 | `lifeguard` | Bagnino | sì | permessi ristretti — vedi ["Permessi per ruolo"](#permessi-per-ruolo-indicazioni-committente) |
| 6 | `manager` | Manager | sì | — |
| 7 | `cashoperator` | Operatore di cassa | sì | — |
| 8 | `websiteadmin` | Website admin | no | **mantenuto** (committente); nessun consumer nei frontend attuali — verificare l'uso reale prima di assegnargli permessi |
| 9 | `user1` | user1 | sì | **mantenuto** (committente); scopo storico non documentato, label da rivedere |
| 10 | `waiter` | Cameriere | sì | oggi enforcement solo client → server-side nella riscrittura |
| 11 | `affiliate` | Affiliato | sì | usa `is_resource_scope_restricted` + `identity_access_users.resource_scopes` |
| 12 | `advancedlifeguard` | Bagnino avanzato | sì | = `lifeguard` **+ check-in / check-out** prenotazione — vedi ["Permessi per ruolo"](#permessi-per-ruolo-indicazioni-committente) |
| 13 | `marketing` | Marketing | sì | ruolo più recente (2026-07) |
| 14 | `dailycashoperator` | Operatore di cassa giornaliero | sì | **nuovo (2026-10-07)**: come `cashoperator`, ma non vede le prenotazioni passate — vedi ["Permessi per ruolo"](#permessi-per-ruolo-indicazioni-committente). **Seed e migration da creare** con le API |

## Permessi per ruolo (indicazioni committente)

> **`dailycashoperator` — Operatore di cassa giornaliero (indicazione del committente, 2026-10-07).**
> Stessi permessi di `cashoperator`, con una sola differenza: **non vede le prenotazioni passate**.
> - Una prenotazione **finita prima di oggi** è invisibile (finita ieri = non c'è).
> - Una prenotazione **iniziata nel passato ma ancora in corso** è visibile.
> - Nel calendario non può selezionare **date passate**.
>
> Il vincolo si applica **lato server** nel modulo che espone le prenotazioni, non solo nascondendo
> elementi nel frontend: Identity fornisce solo il ruolo nel token.

I permessi sono **policy ASP.NET Core** derivate dal `code` del ruolo (non colonne a DB al day one). Le
indicazioni raccolte finora dal committente:

### `lifeguard` — Bagnino
- **Accesso alle sole sezioni**: Mappa, Magazzino (Inventory).
- **Prenotazioni**: può **aprire** una prenotazione esistente e vederne il dettaglio; **non** vede il
  pannello riepilogo prenotazione (lato destro); **non** può modificare i dati della prenotazione;
  **non** vede i pulsanti *Salva* e *Salva ed esci*.
- **Notifiche**: ogni giorno alle **05:00** riceve l'elenco degli ospiti attesi per la giornata
  corrente (job schedulato → dominio **C6** / [`docs/20-jobs.md`](../../../progettazione/20-jobs.md), non C5).

### `advancedlifeguard` — Bagnino avanzato
- **Tutti** i permessi di `lifeguard`, **più**:
  - può eseguire il **check-in** di una prenotazione;
  - può eseguire il **check-out** di una prenotazione.
- **Notifiche**: come `lifeguard` (elenco ospiti attesi alle 05:00).

> Nota: `authJwt.isLifeguard` legacy verifica solo `roles[i].name === "lifeguard"` → un utente con
> **solo** `advancedlifeguard` non passava quel gate. Nella riscrittura le policy vanno definite in modo
> che `advancedlifeguard` erediti esplicitamente i permessi di `lifeguard`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_identity_access_roles` | `id` | PK | — |
| `uq_identity_access_roles_code` | `code` | UNIQUE | risoluzione ruolo per codice stabile; **elimina** il rischio di duplicati del legacy |

*(Nessun `public_id`: tabella referenziata solo entro lo schema `identity`; il "ruolo" che esce verso
gli altri moduli è il **`code`** copiato nel claim del token, non l'id — [`docs/pre/autenticazione-autorizzazione-dotnet.md` §2](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md).)*

## Campi

### id
- **Legacy**: `roles.id`
- **Descrizione**: identificativo tecnico del ruolo; PK.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY` (in migrazione: valori seed forzati 1–13 per
  preservare la corrispondenza con il legacy, poi `ALTER SEQUENCE ... RESTART`).
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziato da [`identity_access_users.role_id`](identity_access_users.md).
- **Note**: —.

### code
- **Legacy**: `roles.name`
- **Descrizione**: codice tecnico stabile del ruolo, usato in codice/policy/claim del token.
- **Tipo**: `varchar(40)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: **`UNIQUE`** (assente nel legacy). Valori minuscoli senza spazi (vedi seed). È il valore
  copiato nel claim `role` del JWT.

### ~~label_it / label_en~~ → etichette da risorse di localizzazione (nessuna colonna a DB)
- **Legacy**: — (nel legacy le etichette erano nel frontend, nella lista `ACCOUNT_TYPES` di `BathhouseConstant.js`, non a DB — e divergenti fra le 4 codebase)
- **Valutazione (richiesta committente: servono altre lingue oltre IT/EN?)**: **sì, potenzialmente** →
  quindi **non** due colonne `label_it`/`label_en`. Per un vocabolario **chiuso e legato al codice**
  (ogni ruolo ha le sue policy: aggiungerne uno richiede comunque un deploy) l'etichetta tradotta viene
  dalle **risorse di localizzazione del modulo** (`Modules/Identity/Resources/Roles.{culture}.resx` /
  `.json`), **chiave = `code`** — esattamente la regola di
  [`docs/03-gestione-dati.md` §2.2.A](../../../progettazione/03-gestione-dati.md) ("l'esigenza di traduzione da
  sola non giustifica una tabella / colonna a DB"). Aggiungere una lingua = aggiungere un file
  `Roles.de.resx`, nessuna modifica di schema, N lingue senza limiti.
- **Conseguenza sullo schema**: `identity_access_roles` **non** ha colonne di etichetta. Le label nella
  tabella-seed qui sotto sono solo **illustrative** (corrispondono alle chiavi in `Roles.it.resx`).
- **Fallback** (solo se emergesse un requisito di **rinominare le label da pannello senza deploy**,
  oggi non previsto): tabella figlia `identity_access_role_translations`
  (`role_id`, `culture`, `label`, `UNIQUE(role_id, culture)`) — mai colonne per-lingua.

### is_assignable_via_panel
- **Legacy**: — (nuovo; deriva dall'appartenenza alla lista `ACCOUNT_TYPES` del frontend, oggi hardcoded)
- **Descrizione**: se `true`, un admin/superadmin può assegnare questo ruolo creando/modificando un
  account dal pannello "gestione utenti"; se `false` il ruolo è assegnato solo da flussi dedicati
  (`consumer` in registrazione, `superadmin` alla registrazione stabilimento) o è di sistema.
- **Tipo**: `boolean`.
- **Default**: nessuno. Nessun default a DB (tolto il 2026-09-30, vedi la nota in «Implementazione .NET»): il valore lo dà sempre il seed.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: erede applicativo della lista `ACCOUNT_TYPES` del frontend (oggi hardcoded e divergente fra
  le 4 codebase — [`analisi-pre/db/roles.md`](../../../analisi-pre/db/roles.md)).

### is_staff
- **Legacy**: — (nuovo)
- **Descrizione**: `true` per i ruoli operativi di stabilimento (richiedono `property_id` sull'account);
  `false` per `consumer` e i ruoli di piattaforma.
- **Tipo**: `boolean`.
- **Default**: nessuno. Nessun default a DB (tolto il 2026-09-30, vedi la nota in «Implementazione .NET»): il valore lo dà sempre il seed.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: usato in validazione (`is_staff = true` ⇒ `identity_access_users.property_id` obbligatorio).

### sort_order
- **Legacy**: — (nuovo; nel legacy l'ordine era implicito nell'`id`/nell'array `db.ROLES`)
- **Descrizione**: ordinamento di presentazione nei menu a tendina.
- **Tipo**: `smallint`.
- **Default**: nessuno. Nessun default a DB (tolto il 2026-09-30, vedi la nota in «Implementazione .NET»): il valore lo dà sempre il seed.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: solo UI.

### created_at / updated_at
- **Legacy**: `roles.createdAt` / `roles.updatedAt`
- **Descrizione**: creazione / ultima modifica della riga.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: §3.3–§3.4. In migrazione si portano i valori legacy (`roles.createdAt`).

### deleted_at
- **Legacy**: — (nuovo; nel legacy non esisteva un modo di ritirare un ruolo)
- **Descrizione**: soft-delete del ruolo (`NULL` ⇔ ruolo attivo).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: un ruolo con account collegati non è cancellabile (FK `RESTRICT`); il soft-delete serve a
  ritirare dal pannello un ruolo non più assegnabile senza rompere gli account storici (il committente
  ha comunque chiesto di **mantenere tutti i 13 ruoli**).

## Valutazione JSON

Nessun JSON. La lista dei permessi per ruolo, nel legacy assente a DB (era codice in `authJwt.js`),
**non** diventa una colonna JSON: i permessi sono **policy** ASP.NET Core derivate dal `code` del ruolo
(più eventualmente una tabella `identity_access_role_permissions` se serve gestione runtime — da
valutare, non necessaria al day one).

## Suggerimenti per le prestazioni

- Tabella **statica** (13 righe): interamente **cache-abile in memoria** all'avvio dell'applicazione;
  nessuna query a runtime nel percorso di autorizzazione (il ruolo è nel token).
- Nessun indice oltre a PK + `UNIQUE(code)`.
- Il seed vive in una **migration** versionata (non uno script manuale come nel legacy).

## Implementazione .NET

Entità **lookup** (non un `enum` PG — vocabolario che cresce da pannello senza deploy, confermato dalla
sezione Descrizione) del modulo `Module.Identity`, sottoarea `Access`, schema `identity`. `Role` non è
un aggregate root con eventi: `sealed class` semplice, referenziata da `Account.RoleId`.

Nessun `public_id`: `Role` è referenziata solo entro lo schema `identity` (il valore che esce verso gli
altri moduli è il `Code`, copiato nel claim `role` del token — non l'id, vedi
[`docs/pre/autenticazione-autorizzazione-dotnet.md` §2](../../../progettazione/pre/autenticazione-autorizzazione-dotnet.md)).

### `RoleConfiguration`

```csharp
namespace Module.Identity.Infrastructure.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
	public void Configure(EntityTypeBuilder<Role> builder)
	{
		builder.ToTable("access_roles", "identity", t => t.HasComment(
			"Catalogo dei ruoli applicativi (lookup gestibile da pannello, senza deploy)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_access_roles");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();
		// seed id 1–13 forzati via HasData (vedi sotto), poi ALTER SEQUENCE ... RESTART in migration.

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasMaxLength(40)
			.IsRequired()
			.HasComment("Codice tecnico stabile del ruolo, usato in policy/claim del token.");

		// Nessun HasDefaultValue su queste tre colonne, deliberato: HasData OMETTE dall'INSERT le colonne
		// il cui valore coincide col default CLR (false, 0), quindi con un default di DB diverso il valore
		// seminato evapora. Così is_staff era finito a true per systemadmin e consumer, che il seed dichiara
		// false (corretto il 2026-09-30, analisi-pre/db-no-copied/_mancanti-da-sistemare.md voce B13).
		builder.Property(x => x.IsAssignableViaPanel)
			.HasColumnName("is_assignable_via_panel")
			.IsRequired()
			.HasComment("Se true, un admin/superadmin può assegnare questo ruolo dal pannello gestione utenti.");

		builder.Property(x => x.IsStaff)
			.HasColumnName("is_staff")
			.IsRequired()
			.HasComment("Se true, il ruolo è operativo di stabilimento e richiede property_id sull'account.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasComment("Ordinamento di presentazione nei menu a tendina.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune (§2, §6 del template).

		builder.HasIndex(x => x.Code)
			.IsUnique()
			.HasDatabaseName("uq_identity_access_roles_code");

		builder.HasData(
			new { Id = 1L, Code = "systemadmin", IsAssignableViaPanel = false, IsStaff = false, SortOrder = (short)0 },
			new { Id = 2L, Code = "superadmin", IsAssignableViaPanel = false, IsStaff = true, SortOrder = (short)10 },
			new { Id = 3L, Code = "consumer", IsAssignableViaPanel = false, IsStaff = false, SortOrder = (short)0 },
			new { Id = 4L, Code = "admin", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)20 },
			new { Id = 5L, Code = "lifeguard", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)30 },
			new { Id = 6L, Code = "manager", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)40 },
			new { Id = 7L, Code = "cashoperator", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)50 },
			new { Id = 8L, Code = "websiteadmin", IsAssignableViaPanel = false, IsStaff = true, SortOrder = (short)60 },
			new { Id = 9L, Code = "user1", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)70 },
			new { Id = 10L, Code = "waiter", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)80 },
			new { Id = 11L, Code = "affiliate", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)90 },
			new { Id = 12L, Code = "advancedlifeguard", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)100 },
			new { Id = 13L, Code = "marketing", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)110 },
			new { Id = 14L, Code = "dailycashoperator", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)55 });
		// nota: HasData richiede tipo anonimo/DTO senza navigazioni; qui semplificato — nella
		// migration reale si usa un tipo shadow o l'entità con i soli scalari valorizzati.
	}
}
```

Nessun enum PG proprio: i 13 `code` sono risolti una volta all'avvio in una costante/`enum RoleCode`
C# **puramente applicativo** (non mappato a colonna — il vocabolario a DB resta la tabella), usato per
non confrontare mai per stringa come nel legacy (`requirelifeguardRole`, typo già osservato).

### Etichette e permessi (nessuna colonna a DB)

- Etichette da `Modules/Identity/Resources/Roles.{culture}.resx|json`, chiave = `Code`, risolte via
  `IStringLocalizer<Roles>` per la lingua della richiesta/utente — nessuna colonna `label_*`.
- Permessi come `AuthorizationPolicy` ASP.NET Core derivate dal `Code`, non colonne. Policy per
  `lifeguard`/`advancedlifeguard` come da "Permessi per ruolo" sopra, con `advancedlifeguard` che
  **eredita esplicitamente** i permessi di `lifeguard` (bug del legacy `authJwt.isLifeguard` da non
  ripetere) + check-in/check-out prenotazione. Job "elenco ospiti attesi 05:00" → C6
  ([`docs/20-jobs.md`](../../../progettazione/20-jobs.md)), non C5.

### Note di migrazione

- Seed via migration versionata (13 righe, id 1–13 preservati), **senza** colonne di etichetta —
  sostituisce lo script manuale legacy.
- Regola di validazione applicativa `IsStaff ⇒ Account.PropertyId` obbligatorio.
- Servizio di **cambio ruolo** tracciato (operazione assente nelle API legacy).
- Tutti i 13 ruoli mantenuti (indicazione committente): nessuna dismissione; per `websiteadmin`/`user1`
  verificare l'uso reale prima di definirne le policy definitive.
