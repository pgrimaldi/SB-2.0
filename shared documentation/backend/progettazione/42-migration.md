# Migration EF Core — creare, applicare, revert

> Come lavorare con le migration del backend .NET (`backend/summerbooking-backend/`). Copre solo la
> parte **schema/persistenza** (Domain + Infrastructure di ogni modulo, code-first EF Core 10 + Npgsql),
> coerente con [`analisi-post/db/_dotnet-template.md`](../analisi-post/db/_dotnet-template.md) e
> [`50-standard-di-codifica.md`](50-standard-di-codifica.md). Non copre API/BL (non ancora implementate,
> [`note.md`](note.md)).

---

## 1. Un modulo, uno schema, una `DbContext`, una history di migration

Ogni modulo `Module.<Nome>` (`backend/summerbooking-backend/Module.<Nome>/`) possiede:

- **una tabella** dello schema PostgreSQL omonimo (`analisi-post/db/<dominio>/`) — nessuna FK cross-schema,
  nessuna dipendenza da un altro modulo (id copiati per valore, [`_feature-tabelle.md`](../analisi-post/db/_feature-tabelle.md));
- **un solo `DbContext`** (`Module.<Nome>.Infrastructure.DbContext` — la classe si chiama letteralmente
  `DbContext`, il namespace la disambigua, [`50-standard-di-codifica.md` §2](50-standard-di-codifica.md));
- **una propria cartella di migration** (`Module.<Nome>/Infrastructure/Migrations/`);
- **una propria tabella di storico** nel DB, `__ef_migrations_history`, ma **nello schema del modulo**
  (`Schema.MigrationsHistoryTable(...)` in `DbContext.OnConfiguring`) — più moduli condividono lo stesso
  database fisico, quindi ognuno deve avere il proprio storico, altrimenti si pesterebbero i piedi sui
  nomi delle migration.

Conseguenza pratica: **le migration si creano/applicano un modulo alla volta**, mai per l'intera
solution. Non serve il progetto `Boot` (che oggi referenzia solo `Module.Identity` e non ha ancora
API/BL) — ogni modulo è **auto-sufficiente** per `dotnet ef` tramite la propria
`Module.<Nome>.Infrastructure.DesignTimeDbContextFactory` (`IDesignTimeDbContextFactory<DbContext>`).

Moduli e schema (11 totali; per il dettaglio tabella-per-tabella vedi
[`_feature-tabelle.md`](../analisi-post/db/_feature-tabelle.md)):

| Modulo | Schema | Dominio | Tabelle |
|---|---|---|---|
| `Module.Identity` | `identity` | C5 | `identity_*` (14) |
| `Module.Catalog` | `catalog` | C2 | `catalog_*` (49) |
| `Module.Booking` | `booking` | C1 | `booking_*` (19) |
| `Module.Payment` | `payment` | C3 | `payment_*` (13) |
| `Module.Fiskaly` | `fiskaly` | C4 | `fiskaly_*` (9) |
| `Module.FiscalPrint` | `fiscalprint` | C4bis | `fiscalprint_*` (4) |
| `Module.Notification` | `notification` | C6 | `notification_*` (21) |
| `Module.Switchboard` | `switchboard` | C7 | `switchboard_*` (10) |
| `Module.Chatbot` | `chatbot` | C8 | `chatbot_*` (3) |
| `Module.Privacy` | `privacy` | — | `privacy_*` (5) |
| `Module.Infra` | `infra` | INFRA | `infra_*` (1) |
| `Module.Audit` | `audit` | — | `audit_*` (1) — aggiunto il 2026-10-07, [86-modulo-audit.md](86-modulo-audit.md) |

> **Deviazione dalla nota "SharedKernel" di [`identity_access_users.md`](../analisi-post/db/identity/identity_access_users.md)**:
> l'enum `storage_origin` (origine foto profilo) è usato **solo** da due tabelle, entrambe in `identity`
> (`identity_access_users`, `identity_account_contract_documents`) — per non introdurre un tipo Postgres
> davvero cross-schema (che romperebbe l'indipendenza delle migration tra moduli, §4), è registrato come
> `identity.storage_origin`, non un tipo condiviso in uno schema a parte. Se in futuro un altro modulo lo
> userà, si valuta allora se estrarlo davvero (schema dedicato + migration di modulo che lo crea una sola
> volta, gli altri moduli senza `HasPostgresEnum` locale).

## 2. Prerequisiti

```bash
# .NET SDK 10 (già presente in questo ambiente: dotnet --version → 10.0.401)
dotnet tool install --global dotnet-ef   # se "dotnet ef" non è già installato
dotnet tool update --global dotnet-ef    # per allinearlo alla versione del runtime
```

Un'istanza PostgreSQL 16+ raggiungibile. In locale, la scorciatoia più veloce (nessun docker-compose nel
repo, se ne può aggiungere uno in `infra/` quando servirà davvero):

```bash
docker run -d --name summerbooking-postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16
createdb -h localhost -U postgres summer_booking_dev
```

### Connection string

Tutti i moduli leggono la **stessa** convenzione, centralizzata in
`BuildingBlocks.Infrastructure.Persistence.ModuleConnectionStrings`:

1. a runtime (dentro Boot), la chiave `ConnectionStrings:SummerBooking` della configurazione
   ASP.NET Core standard — `Boot/appsettings.Development.json` la definisce gia con il default di
   sviluppo locale; `Boot/appsettings.json` (produzione) non la definisce, va fornita dall'ambiente;
2. a design-time (`dotnet ef`, che non passa da Boot), la stessa chiave letta da
   `Boot/appsettings.json`/`Boot/appsettings.{Environment}.json` (i comandi si lanciano dalla root
   di summerbooking-backend, §3);
3. in entrambi i casi, la variabile d'ambiente **`ConnectionStrings__SummerBooking`** (stessa
   convenzione di IConfiguration, doppio underscore) ha sempre precedenza sui file.

Nessun default e' piu' hardcoded nel codice: se la chiave non e' configurata ne' a file ne' a
variabile d'ambiente, `ModuleConnectionStrings.Resolve` lancia un'eccezione esplicita invece di
usare un fallback silenzioso.

```bash
# solo se non si vuole toccare appsettings.Development.json (es. per lanciare le migration contro un altro DB)
export ConnectionStrings__SummerBooking="Host=...;Port=5432;Database=summer_booking_dev;Username=...;Password=..."
```

```powershell
$env:ConnectionStrings__SummerBooking = "Host=...;Port=5432;Database=summer_booking_dev;Username=...;Password=..."
```

Stesso database fisico per tutti i moduli in sviluppo (schemi separati bastano a isolarli); in produzione
resta comunque possibile puntare moduli diversi a istanze fisiche diverse cambiando la connection string
per modulo, dato che ognuno la risolve in modo indipendente.

### Estensioni PostgreSQL richieste da alcuni moduli

Alcuni campi usano tipi Postgres non built-in (es. `citext` per `identity_access_users.email`,
[`identity_access_users.md`](../analisi-post/db/identity/identity_access_users.md)). Il modulo che
introduce per primo un tipo del genere lo abilita nella propria **prima migration** con SQL grezzo:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS citext;");
    // ... resto della migration generata
}
```

(`Down()` non disinstalla l'estensione: potrebbe servire ad altri schemi/moduli sullo stesso database.)

## 3. Creare una migration

Da `backend/summerbooking-backend/`, un modulo alla volta:

```bash
dotnet ef migrations add <NomeMigration> \
  --project Module.Identity/Module.Identity.csproj \
  --output-dir Infrastructure/Migrations
```

```powershell
dotnet ef migrations add <NomeMigration> `
  --project Module.Identity/Module.Identity.csproj `
  --output-dir Infrastructure/Migrations
```

- `<NomeMigration>`: `PascalCase`, descrittivo (`InitialCreate`, `AddRefreshTokens`, …).
- `--project` punta al modulo: sostituire `Module.Identity` con `Module.Catalog`, `Module.Booking`, ecc.
- Non serve `--startup-project`: la `DesignTimeDbContextFactory` del modulo basta da sola (nessuna
  connessione reale al DB è necessaria per **generare** una migration, solo per applicarla — §4).
- Il file finisce in `Module.<Nome>/Infrastructure/Migrations/`, insieme allo `<Nome>DbContextModelSnapshot.cs`
  aggiornato: **entrambi vanno committati**.

Per la primissima migration di un modulo, usare il nome convenzionale `InitialCreate`.

## 4. Applicare le migration (`database update`)

Un modulo alla volta:

```bash
dotnet ef database update --project Module.Identity/Module.Identity.csproj
```

Per applicarle **a tutti i moduli** (setup iniziale di un ambiente nuovo), in ordine qualsiasi — nessuna
dipendenza tra schemi per costruzione (§1):

```bash
cd backend/summerbooking-backend
for m in Identity Catalog Booking Payment Fiskaly FiscalPrint Notification Switchboard Chatbot Privacy Infra Audit; do
  echo "== Module.$m =="
  dotnet ef database update --project "Module.$m/Module.$m.csproj" || exit 1
done
```

```powershell
Set-Location backend/summerbooking-backend
foreach ($m in "Identity","Catalog","Booking","Payment","Fiskaly","FiscalPrint","Notification","Switchboard","Chatbot","Privacy","Infra","Audit") {
  Write-Host "== Module.$m =="
  dotnet ef database update --project "Module.$m/Module.$m.csproj"
  if ($LASTEXITCODE -ne 0) { break }
}
```

EF/Npgsql crea automaticamente lo schema del modulo (`CREATE SCHEMA IF NOT EXISTS <schema>`) alla prima
migration applicata — non serve crearlo a mano.

## 5. Revert

**Tornare a una migration precedente** (applica i `Down()` di tutte le migration successive a quella
indicata):

```bash
dotnet ef database update <NomeMigrationPrecedente> --project Module.Identity/Module.Identity.csproj
```

Per annullare **tutte** le migration di un modulo (schema del modulo svuotato, storico compreso):

```bash
dotnet ef database update 0 --project Module.Identity/Module.Identity.csproj
```

**Rimuovere l'ultima migration non ancora applicata** (es. per correggerla prima del commit):

```bash
dotnet ef migrations remove --project Module.Identity/Module.Identity.csproj
```

`migrations remove` prova a connettersi al DB per verificare se la migration risulta già applicata; se il
DB non è raggiungibile lo segnala ma rimuove comunque i file locali (`*.cs`/aggiorna lo snapshot) — va
bene in un ambiente di sola scrittura del codice, **non** eseguirlo contro un DB condiviso dove la
migration è già stata applicata da altri senza prima fare `database update <precedente>` lì.

## 6. Generare lo script SQL (senza applicarlo)

Utile per una review, o per un deploy dove le migration le applica una pipeline separata invece di
`dotnet ef database update` diretto:

```bash
dotnet ef migrations script --project Module.Identity/Module.Identity.csproj -o identity.sql --idempotent
```

`--idempotent` avvolge ogni blocco in un controllo sulla tabella di storico (`__ef_migrations_history`
dello schema `identity`), così lo script è sicuro da rieseguire.

## 6bis. Le stesse operazioni da Visual Studio (Package Manager Console)

Le sezioni §3-6 usano il CLI `dotnet ef`; da Visual Studio 2022 le stesse operazioni si fanno dalla
**Package Manager Console** (`Strumenti` → `Gestione pacchetti NuGet` → `Console di Gestione Pacchetti`),
coi cmdlet `Add-Migration`/`Update-Database`/`Remove-Migration`/`Script-Migration`.

**Prerequisiti**, entrambi già presenti in ogni `Module.<Nome>.csproj`:

- `Microsoft.EntityFrameworkCore.Tools` — è il pacchetto che *definisce* i cmdlet PMC (distinto da
  `Microsoft.EntityFrameworkCore.Design`, che serve al CLI/build design-time e basta da solo per
  `dotnet ef`); senza `.Tools` in almeno un progetto della solution i cmdlet non compaiono proprio in
  PMC. Se in futuro si aggiunge un nuovo modulo dal template `Module._Template`, verificare che il suo
  `.csproj` porti anche questo pacchetto, non solo `.Design`.
- Un **progetto di avvio** impostato in Esplora soluzioni (tasto destro → `Imposta come progetto di
  avvio`): è un vincolo di Visual Studio/PMC (serve un eseguibile da compilare per ospitare gli
  strumenti), non ha a che fare con quale modulo si sta migrando — dato che oggi solo `Boot` è
  eseguibile, va impostato lui, **anche se non referenzia ancora il modulo** su cui si lavora (§1: ogni
  modulo è auto-sufficiente tramite la propria `DesignTimeDbContextFactory`, che PMC usa allo stesso
  modo del CLI).

**Selezionare il modulo target** nel dropdown "Progetto predefinito" in cima alla PMC — è l'equivalente
di `--project` nei comandi CLI. Da lì:

| CLI (§3-6) | Package Manager Console |
|---|---|
| `dotnet ef migrations add <Nome> --project Module.Identity/... --output-dir Infrastructure/Migrations` | `Add-Migration <Nome> -OutputDir Infrastructure/Migrations` |
| `dotnet ef database update --project Module.Identity/...` | `Update-Database` |
| `dotnet ef database update <Precedente> --project Module.Identity/...` | `Update-Database -Migration <Precedente>` |
| `dotnet ef database update 0 --project Module.Identity/...` | `Update-Database -Migration 0` |
| `dotnet ef migrations remove --project Module.Identity/...` | `Remove-Migration` |
| `dotnet ef migrations script --project Module.Identity/... -o identity.sql --idempotent` | `Script-Migration -Output identity.sql -Idempotent` |

Il progetto è già selezionato nel dropdown, quindi `-Project`/`-StartupProject` nei cmdlet non servono
(a differenza del `-Context`, utile solo se un progetto avesse più `DbContext`, mai il caso qui — §1).
Il resto vale identico al CLI: un modulo alla volta, mai sull'intera solution; `<Nome>` in
`PascalCase`; i file generati (migration + `<Nome>DbContextModelSnapshot.cs`) vanno committati.

## 7. Indici funzionali e altro SQL non esprimibile in Fluent API

Alcune schede (es. [`identity_access_users.md`](../analisi-post/db/identity/identity_access_users.md)
§"Indici funzionali") richiedono indici su espressione (`lower(email)`) non rappresentabili con
`HasIndex(x => x.Prop)`. Si aggiungono **in coda** al `Up()` della migration generata (e uno speculare
`DROP INDEX` in `Down()`), mai come migration SQL scritta a mano fuori dal ciclo di vita EF:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // ... colonne/indici generati da EF ...

    migrationBuilder.Sql("""
        CREATE UNIQUE INDEX uq_identity_access_users_email_alive
            ON identity.access_users (lower(email)) WHERE deleted_at IS NULL;
        """);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DROP INDEX identity.uq_identity_access_users_email_alive;");
    // ... resto del Down generato da EF, in ordine inverso ...
}
```

## 7bis. Insidie ricorrenti di EF Core 10 + Npgsql (emerse implementando gli 11 moduli)

- **Niente `[PgName]` per rinominare un singolo membro enum**: quella API non esiste in Npgsql 10.0.3 (né
  in versioni recenti — è un residuo dei provider EF6). Se la traduzione snake_case di default produce
  un'etichetta diversa da quella voluta (tipico con cifre adiacenti a lettere: `S3UsEast2` → `s3_us_east2`,
  non `s3_us_east_2`), si incorporano gli underscore **direttamente nell'identificatore C#** del membro
  (`S3_us_east_2`, `Fp_81`) — identificatore valido, il traduttore lo lascia passare invariato dove già
  contiene `_`. Verificare sempre l'etichetta reale nella migration generata
  (`Annotation("Npgsql:Enum:<schema>.<nome>", "...")`), non fidarsi a occhio del nome del membro.
- **`HasData(...)` non applica i default del DB**: se una colonna ha `HasDefaultValueSql("now()")` ma è
  `IsRequired()` a livello CLR, un seed via `HasData` senza quel valore fallisce a design-time con
  *"no value was provided for required property"*. Ogni riga seed deve valorizzare **esplicitamente**
  `CreatedAt`/`UpdatedAt` (e ogni altra colonna obbligatoria), tipicamente con un timestamp fisso
  (`new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)`), mai lasciarli al default DB.
- **Proprietà array/lista in un indice richiede un tipo comparabile**: una colonna `text[]`/`varchar[]`
  modellata come `List<string>` e usata in un `HasIndex(...)` (anche composito) fallisce a design-time con
  *"cannot be used as a key because ... does not implement IComparable/IStructuralComparable"*. Usare
  `string[]` invece di `List<string>` per queste proprietà (gli array implementano
  `IStructuralComparable`, `List<T>` no); se la proprietà non entra in nessun indice, `List<T>` resta
  comunque accettabile.
- **`OwnedNavigationBuilder<TOwner,TDependent>` non ha `ApplyConfiguration(IEntityTypeConfiguration<T>)`**
  — quell'overload esiste solo su `ModelBuilder`. La configurazione di un owned type (`OwnsOne`/`OwnsMany`)
  va scritta come **metodo statico** `Configure(OwnedNavigationBuilder<TOwner,TDependent> builder)` su una
  classe (non `IEntityTypeConfiguration<T>`), richiamato esplicitamente dal genitore:
  `builder.OwnsMany(x => x.Righe, r => RigaConfiguration.Configure(r));`.
- **`Microsoft.EntityFrameworkCore.Relational` va pinnato esplicitamente** in `Shared.csproj` alla stessa
  versione di `Microsoft.EntityFrameworkCore`: `Npgsql.EntityFrameworkCore.PostgreSQL` dichiara solo un
  floor (`[10.0.4, 11.0.0)`), quindi senza un pin NuGet risolve la versione più bassa compatibile invece di
  allinearla, causando un conflitto di assembly (`MSB3277`) visibile solo quando si builda l'intera
  solution (`Boot`/`ArchitectureTests`, che referenziano tutti i moduli insieme) — non nei singoli
  `dotnet build Module.<Nome>/...csproj`. Se si aggiorna `Microsoft.EntityFrameworkCore`, aggiornare anche
  questo pin.
- **Colonne `inet`**: mappare con `System.Net.IPAddress`, non `string` — Npgsql non converte
  automaticamente una stringa in `inet`.
- **Due `HasIndex` sulle stesse colonne sono lo stesso indice.** `builder.HasIndex(x => new { x.A, x.B })`
  chiamato due volte (per esempio un indice generico e uno parziale, o uno normale e uno `UNIQUE`)
  restituisce **lo stesso** `IndexBuilder`: il secondo sovrascrive nome, filtro e — se il primo era
  `IsUnique()` — eredita l'unicità. Nessun errore, nessun avviso: a DB esiste un solo indice. Per averne
  due serve l'overload con nome, `HasIndex(x => new { x.A, x.B }, "ix_...")`. Trovato il 2026-10-06 su
  13 configurazioni in 7 moduli: in ogni coppia sopravviveva solo il secondo indice, e due erano diventati
  `UNIQUE` per sbaglio (`ix_switchboard_voice_menu_items_runtime`, `ix_fiscalprint_printer_departments_resolver`). Corretto
  con le migration `AddMissingDesignedIndexes`.
- **Owned collection senza `WithOwner().HasForeignKey(...)`**: EF crea una shadow property con una colonna
  FK propria (`<owner>_id`) e lascia la proprietà scalare della figlia senza vincolo. Già visto su
  `ledger_entry_lines` (2026-09-15) e poi su `pos_charge_items` / `wallet_transaction_items` (corretto il
  2026-10-06, `FixOwnedItemsForeignKeys`). Ogni `OwnsMany` con una FK scalare esplicita deve dichiararla.
- **`NULLS NOT DISTINCT` si esprime in Fluent API**: `.AreNullsDistinct(false)` su un indice `UNIQUE`
  (PostgreSQL 15+). Usarlo invece di `coalesce(...)` su espressione, che richiede SQL a mano e su una
  colonna enum non si può nemmeno scrivere (`coalesce(status, '')`).

## 8. Errori comuni

| Sintomo | Causa | Fix |
|---|---|---|
| `No DbContext was found` | `--project` punta a un modulo senza `DesignTimeDbContextFactory`, o typo nel path | verificare `Module.<Nome>/Infrastructure/DesignTimeDbContextFactory.cs` esista |
| `42P06: schema "..." already exists` riapplicando su un DB già inizializzato da un altro percorso | schema creato a mano fuori da EF | droppare lo schema a mano una volta, poi solo `dotnet ef database update` |
| `type "..." already exists` su un enum Postgres | due moduli diversi registrano lo stesso `HasPostgresEnum` nello stesso schema, o migration riapplicata senza passare da `__ef_migrations_history` | ogni enum vive nello schema di **un solo** modulo (§1); non copiare `HasPostgresEnum` tra moduli |
| `28P01` / connection refused | Postgres non in ascolto, o `ConnectionStrings:SummerBooking`/`ConnectionStrings__SummerBooking` sbagliata | verificare `docker ps` / la connection string (§2) |
| Tool EF più vecchio del runtime (warning, non errore) | `dotnet-ef` non aggiornato | `dotnet tool update --global dotnet-ef` |
| `Add-Migration`/`Update-Database` non riconosciuti in Package Manager Console | nessun progetto della solution ha `Microsoft.EntityFrameworkCore.Tools` | verificare che il modulo abbia il pacchetto (§6bis); se manca, ricompilare la solution dopo averlo aggiunto |
| PMC chiede di impostare un progetto di avvio, o fallisce senza uno | Visual Studio richiede sempre un progetto di avvio eseguibile per i cmdlet EF, indipendentemente dal modulo che si sta migrando | impostare `Boot` come progetto di avvio (§6bis) |

## 9. Cosa non fa (ancora) questa parte

Solo Domain (entità anemiche: proprietà a setter privato, nessun metodo di dominio/factory — la ricchezza
comportamentale arriva con la BL, fuori da questa fase, [`note.md`](note.md)) + Infrastructure (DbContext,
`IEntityTypeConfiguration<T>`, migration). Niente Endpoints, Application (Service/Validator/Dto),
registrazione in `Boot` oltre alla persistenza: `Module.<Nome>.AddModule<Nome>Persistence()` è già
richiamato da `Add<Nome>Module()`, ma solo `Module.Identity` è oggi referenziato da `Boot` (gli altri
moduli non hanno ancora API, quindi Boot non li carica).

## 10. Consolidamento del 2026-10-06: una sola `InitialCreate` per modulo

Il 2026-10-06 le **69 migration** accumulate negli 11 moduli sono state sostituite da **una
`InitialCreate` per modulo**, generata dal modello attuale con `dotnet ef migrations add InitialCreate`.
Lo snapshot del modello generato è **identico** a quello precedente in tutti gli 11 moduli: il modello
non è cambiato, è cambiata solo la storia.

**Copia delle migration rimosse**: `C:\D\backup\2026-10-06-squash-migrations\migrations\` (una cartella
per modulo, compresi `.Designer.cs` e snapshot). Il progetto non è sotto git: quella è l'unica copia.

### Che cosa è stato riportato a mano, e che cosa no

EF rigenera tutto ciò che il modello descrive. Lo SQL scritto a mano nelle vecchie migration no: è stato
riportato in fondo all'`Up` di ciascuna `InitialCreate`, sotto un'intestazione «Oggetti che il modello EF
non sa esprimere».

| Modulo | Riportato | Veniva da |
|---|---|---|
| Identity | `btree_gist`; EXCLUDE sui periodi di abbonamento; indice unico «un solo contratto corrente» | `AddNoOverlapExcludeConstraints`, `CollapseRedundantBooleanFlags` |
| Catalog | `pg_trgm` e `btree_gist` (in testa: servono agli indici GIN); 4 EXCLUDE su periodi e fasce; indice unico sul nome di settore | `InitialCreate`, `AddNoOverlapExcludeConstraints`, `AddSectorNameUniqueAndPeriodExcludes` |
| Booking | partizionamento per anno di `resource_consumptions` e `resource_availability_daily`, **2021-2035** + default; EXCLUDE sulle offerte di rivendita | `PartitionResourceTablesByYear`, `ExtendPartitionsAndResaleOfferExclude` |
| Fiskaly | vista `v_daily_cash_closure` e il suo commento (il `Down` la toglie per prima) | `AddDailyCashClosureView` |
| Privacy | indice unico `uq_privacy_retention_policies_target` (`NULLS NOT DISTINCT`); **seed delle 61 policy in forma finale**, un solo `INSERT` | 8 migration di seed e correzione, da `SeedRetentionPolicies` a `AddLinkedAssetArchiving` |

**Non riportati**, di proposito: gli `UPDATE` di conversione dei dati (portavano righe esistenti da una
forma all'altra: su un database nuovo non c'è niente da convertire), la guardia di
`FixMediaAssetPropertyIdType` (proteggeva una colonna valorizzata durante un cambio di tipo) e la copia
dei dati nel partizionamento.

⚠️ **Il seed delle policy non si poteva rigiocare così com'era**: il primo `INSERT … ON CONFLICT` usava
un indice unico a 4 colonne che una migration successiva aveva sostituito con quello a 6. È stato
rigenerato dal database costruito con la catena vecchia, con i valori finali di ogni riga. Le policy
nascono tutte `is_dry_run_only = true` come prima: si accendono con `retention-enable`
([`81-retention-motore.md` §4](81-retention-motore.md)).

### Come è stato verificato

Due database usa-e-getta, uno costruito con la catena vecchia e uno con la nuova, confrontati **dal
catalogo di sistema** (colonne per nome, vincoli, indici, viste, enum, estensioni, partizioni, sequenze
per colonna proprietaria) e sui dati seed per nome di colonna. Un `diff` di `pg_dump` non basta: cambia
l'ordine fisico delle colonne e il nome delle sequenze (sotto), e il diff diventa illeggibile. Poi
`database update 0` su tutti gli 11 moduli: tutti i `Down` funzionano.

**Esito: 4 differenze, tutte correzioni** — la catena vecchia era divergente dal modello:

1. ⚠️ **`payment.ledger_entries` aveva perso il CHECK `ck_payment_ledger_entries_gross_amount`**
   (`gross_amount > 0`), che il modello dichiara. `AddDbOptimizationsPropertyIdAndGeneratedColumns`
   trasformava `gross_amount` in colonna generata, e Npgsql lo fa con DROP + ADD della colonna: il
   vincolo se ne andava con lei, **senza errori**. Nessuna riga reale lo viola (verificato su 2.540).
2. Tre colonne `property_id` di payment (`ledger_entry_lines`, `pos_charge_items`,
   `wallet_transaction_items`) avevano `DEFAULT '00000000-…'::uuid`: è il segnaposto che EF mette
   quando aggiunge una colonna `NOT NULL` a una tabella esistente. Il modello non lo prevede, e un
   default a uuid zero fa passare in silenzio una riga senza stabilimento. Ora non c'è.

**Due differenze attese, non di schema:**

- **le sequenze di identità hanno il nome della tabella**: prima `booking.lines` aveva
  `booking.booking_lines_id_seq`, perché le migration di rinomina cambiavano la tabella e non la
  sequenza. Nessun codice usa quei nomi (verificato su backend e utility);
- **l'ordine fisico delle colonne** segue il modello invece della storia (prima le colonne aggiunte
  dopo finivano in coda). Conta solo per un `INSERT` senza elenco di colonne o un `SELECT *` letto per
  posizione.

### SQL a mano aggiunto dopo il consolidamento

Da riportare, insieme a quello della tabella sopra, se si rifà un consolidamento:

| Modulo | Oggetto | Migration |
|---|---|---|
| Catalog | `ix_catalog_properties_name_trgm` — GIN `lower(name) gin_trgm_ops` | `AddMissingDesignedIndexes` (2026-10-06) |
| Switchboard | `uq_switchboard_messaging_wa_templates_name_lang` — `UNIQUE (property_id, lower(template_name), language) WHERE deleted_at IS NULL` | `AddMissingDesignedIndexes` (2026-10-06) |

### Per la prossima volta

Da qui in avanti le migration riprendono a accumularsi normalmente (§3). Se si rifà un consolidamento,
il metodo è questo: catena vecchia e nuova su due database usa-e-getta, confronto da catalogo, `Down` di
tutti i moduli. Il `diff` dello snapshot del modello dice solo che il modello non è cambiato, non che lo
schema prodotto sia lo stesso — è esattamente il caso del punto 1.
