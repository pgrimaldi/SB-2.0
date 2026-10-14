# Implementazione .NET — pattern comune per tutte le schede

> Ogni scheda `<nome_tabella>.md` chiude con `## Implementazione .NET`: la traduzione **code-first**
> (EF Core 10 + Npgsql, coerente con [`linee-guida-implementative-dotnet.md`](../../progettazione/pre/linee-guida-implementative-dotnet.md)
> e [`50-standard-di-codifica.md`](../../progettazione/50-standard-di-codifica.md)) di quanto già descritto nelle
> sezioni **Descrizione / Indici / Campi / Valutazione JSON** della scheda stessa.
>
> Questo file fissa **una volta sola** il pattern comune (classi base, convenzioni di nome, enum, commenti
> DB, timestamp, soft-delete) così che ogni scheda documenti **solo le specificità** di quella tabella:
> entità, enum propri, indici, relazioni, note di migrazione. Non ripetere qui la plumbing comune.

---

## 1. Classi di base condivise (`SharedKernel` / `BuildingBlocks.Infrastructure`)

```csharp
// SharedKernel/ISoftDeletable.cs
public interface ISoftDeletable
{
	DateTime? DeletedAt { get; }
	void Delete(DateTime utcNow);
	void Restore();
}

// SharedKernel/IAuditable.cs
public interface IAuditable
{
	DateTime CreatedAt { get; }
	DateTime UpdatedAt { get; }
}

// SharedKernel/AggregateRoot.cs — solo sugli aggregate root che sollevano eventi di dominio
public abstract class AggregateRoot
{
	private readonly List<IDomainEvent> _domainEvents = new();
	public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
	protected void Raise(IDomainEvent @event) => _domainEvents.Add(@event);
	public void ClearDomainEvents() => _domainEvents.Clear();
}
```

- **Entità semplici** (figlie, lookup, log) non ereditano `AggregateRoot`: `sealed class`, nessun evento.
- `ISoftDeletable`/`IAuditable` si implementano **solo** sulle tabelle che hanno davvero `deleted_at` /
  `created_at`+`updated_at` (quasi tutte le tabelle di dominio; **non** le tabelle di puro log/registro
  append-only, che espongono al più `created_at`).
- `BuildingBlocks.Infrastructure` fornisce due `SaveChangesInterceptor`:
  - `AuditInterceptor`: su `Added`/`Modified`, se l'entità è `IAuditable` imposta `CreatedAt`/`UpdatedAt`
    da `IDateTimeProvider.UtcNow` (mai `DateTime.UtcNow` diretto in Infrastructure — regola
    [`12-lingue-e-date.md` §2.3](../../progettazione/12-lingue-e-date.md)).
  - `SoftDeleteInterceptor`: intercetta `EntityState.Deleted` su `ISoftDeletable` e lo trasforma in
    `UPDATE ... SET deleted_at = now()` (mai una `DELETE` fisica sulle tabelle di dominio).
- **Global Query Filter**: nel `DbContext` di ogni modulo, in `OnModelCreating`, un loop riflessivo applica
  `entityType.SetQueryFilter(...)` a ogni entità `ISoftDeletable` (`WHERE deleted_at IS NULL`) — non si
  scrive un filtro per entità.

## 2. Timestamp (`timestamptz`)

- Tipo C#: **`DateTime`** (mai `DateTimeOffset`, mai `DateTimeOffset.Now`/`DateTime.Now`), sempre UTC.
- Converter unico condiviso, registrato una volta per modulo:
  ```csharp
  // BuildingBlocks.Infrastructure/Persistence/UtcDateTimeConverter.cs
  public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
  {
  	public UtcDateTimeConverter() : base(
  		v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
  		v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
  	{ }
  }
  ```
  Applicato con `configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>()` in
  `ConfigureConventions` del `DbContext`, **non** ripetuto proprietà per proprietà.
- Colonne "solo giorno" (`stay_from`, `booking_start_date`, `absence_date`, …): tipo C# **`DateOnly`**,
  mappato nativamente da Npgsql su `date`.
- `created_at`/`updated_at` non si valorizzano mai a mano nell'Application: li imposta l'
  `AuditInterceptor`. Default DB `now()` come rete di sicurezza (`HasDefaultValueSql("now()")`), non come
  meccanismo primario.

## 3. Identificatori

- `id`: `long` (`bigint GENERATED ALWAYS AS IDENTITY`), mai esposto fuori dal modulo.
  ```csharp
  builder.HasKey(x => x.Id).HasName("pk_<tabella>");
  builder.Property(x => x.Id).UseIdentityAlwaysColumn();
  ```
- `public_id` (`Guid`, UUIDv7): **solo** sulle entità esposte in API/URL/GDPR o referenziate da altri
  moduli (vedi [`identificatori-e-uuid.md`](../../progettazione/pre/identificatori-e-uuid.md)). Assegnato dalla
  factory dell'aggregato con `Guid.CreateVersion7()`, mai dal DB:
  ```csharp
  builder.Property(x => x.PublicId).ValueGeneratedNever();
  builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_<tabella>_public_id");
  ```
- Colonne che referenziano un'entità **di un altro modulo/schema** (`property_id`, `customer_id`,
  `operator_user_id`, …): tipo `Guid`, **nessuna FK EF** (`HasOne`/`WithMany` non si usa cross-schema — è
  un id copiato per valore, non una relazione navigabile). Si documenta con un commento e basta:
  ```csharp
  builder.Property(x => x.PropertyId)
  	.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema");
  ```
- Colonne che referenziano un'entità **dello stesso schema/modulo** (`reservation_id` su `booking_lines`,
  `country_id` su `catalog_country_translations`, …): FK EF reale su `long`:
  ```csharp
  builder.HasOne(x => x.Reservation)
  	.WithMany(r => r.Lines)
  	.HasForeignKey(x => x.ReservationId)
  	.HasConstraintName("fk_booking_lines_reservation")
  	.OnDelete(DeleteBehavior.Cascade); // o Restrict, secondo la semantica della scheda
  ```

## 4. Enum

Pattern **A — `ENUM` nativo PostgreSQL** (default per i vocabolari stabili, dettaglio in
[`03-gestione-dati.md` §3.10](../../progettazione/03-gestione-dati.md)):

```csharp
// Domain/Enums/ReservationStatus.cs
public enum ReservationStatus { Pending = 0, Confirmed = 1, Cancelled = 2, NoShow = 3 }
```

```csharp
// <Modulo>DbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ReservationStatus>("booking", "reservation_status");
```

```csharp
// Registrazione data source (ModulePersistenceRegistration.cs *e* DesignTimeDbContextFactory.cs —
// stessa identica lista in entrambi, altrimenti la connection string usata da "dotnet ef" a design-time
// non conosce il tipo)
dataSourceBuilder.MapEnum<ReservationStatus>("booking.reservation_status");
```

```csharp
// <Entita>Configuration.Configure — OBBLIGATORIO, non lo fa nessuna convenzione automatica
builder.Property(x => x.Status)
	.HasColumnName("status")
	.HasColumnType("booking.reservation_status")   // <-- senza, la colonna diventa "integer" silenziosamente
	.IsRequired()
	.HasDefaultValueSql("'pending'");               // <-- MAI HasDefaultValue(ReservationStatus.Pending) qui
```

**Tre insidie verificate in questo codebase, tutte silenziose fino a
`dotnet ef database update` (le prime due) o alla prima scrittura via EF (la terza):**

1. **`HasColumnType("<schema>.<enum>")` sulla property è obbligatorio.** `HasPostgresEnum<TEnum>` (nel
   `DbContext`) e `MapEnum<TEnum>` (nel data source) bastano a far *esistere* il tipo Postgres e a farlo
   *leggere/scrivere* correttamente via Npgsql, ma **non** fanno scegliere a EF quel tipo come colonna: senza
   `HasColumnType` esplicito la migration genera `type: "integer"` — compila, la migration si applica senza
   errori, ma qualunque filtro/indice/`Sql(...)` scritto a mano che confronta la colonna con un'etichetta
   testuale (`WHERE status = 'active'`) fallisce con `22P02: sintassi di input non valida per il tipo
   integer`. Bug reale trovato/corretto su ~90 colonne in 3 moduli il 2026-09-14.
2. **`HasDefaultValue(TEnum.Member)` è incompatibile con `HasColumnType` esplicito.** Una volta che la
   colonna è tipizzata come enum nativo, `HasDefaultValue` di un membro enum continua a emettere il valore
   intero sottostante come default — Postgres rifiuta la migration con `42804: la colonna "..." è di tipo
   <schema>.<enum> ma l'espressione predefinita è di tipo integer`. Usare sempre
   `HasDefaultValueSql("'<label_snake_case>'")` per un default su una colonna enum nativa (la label esatta è
   quella nell'annotazione `Npgsql:Enum:<schema>.<enum>` della migration generata, non va indovinata).
3. **Senza `MapEnum` anche nelle opzioni EF, EF scrive l'enum come intero** (trovato il 2026-10-07, prima
   scrittura EF di un enum: l'ETL è in Python e non passa da EF). Con un data source esterno, la
   registrazione sul data source non basta: va ripetuta in `UseNpgsql(dataSource, o => o.MapEnum<TEnum>("<enum>", "<schema>"))`,
   altrimenti l'`INSERT` fallisce con `42804: la colonna "..." è di tipo <schema>.<enum> ma l'espressione è di tipo integer`
   e la lettura di un valore enum fallisce allo stesso modo. Il modello delle migration non cambia. Modello:
   `Module.Identity/Infrastructure/Infras/IdentityEnumMappings.cs`, un solo elenco usato da runtime e design-time.
   ⚠️ Applicato a Identity e Infra; gli altri moduli lo richiedono prima della prima scrittura EF (PROMEMORIA-85-8).

**Prima di lanciare `dotnet ef database update`**, controllare la migration appena generata (procedura
completa in [`43-migration-procedure.md`](../../progettazione/43-migration-procedure.md)):
```bash
grep -n 'type: "<schema>\.' Infrastructure/Migrations/*_<NomeMigration>.cs | grep 'defaultValue:'
```
Nessun risultato = ok. Un risultato = un `HasDefaultValue(...)` da convertire in `HasDefaultValueSql(...)`.

Ogni scheda elenca solo gli enum **propri** con nome C#/PG e mapping valori legacy → label (già presente
nella sezione Campi); non ripetere la meccanica sopra.

Pattern **B — lookup tabellare** (vocabolario che cresce da pannello senza deploy, es.
`identity_access_roles`, `identity_account_license_plans`): entità normale con `code` `UNIQUE`, nessun
`ENUM` PG — lo dice esplicitamente la scheda quando si applica.

Flags multipli sullo stesso argomento (regola [`03-gestione-dati.md` §3.10bis/§3.10](../../progettazione/03-gestione-dati.md)):
`enum` **array** Postgres, mappato con `HasPostgresArrayConversion` / `HasConversion` a `List<TEnum>` —
tipo colonna `<schema>.<enum>[]`.

## 5. Value Object / owned type

Cluster di colonne che rappresentano un concetto di valore (es. riepilogo economico, intervallo date,
indirizzo) → `sealed record` immutabile mappato con `OwnsOne`/`OwnsMany`, **non** colonne sciolte
sull'entità proprietaria se il gruppo ha comportamento proprio. Le schede che hanno un candidato owned
type lo indicano esplicitamente; altrimenti le colonne restano proprietà scalari dell'entità.

## 6. Commenti DB (`HasComment`) — descrizione di tabella e colonne

Ogni `IEntityTypeConfiguration<T>` valorizza **sempre**:

- un commento sulla tabella (`ToTable(name, schema, t => t.HasComment("..."))`), una frase che riassume
  cosa rappresenta la riga;
- un commento per **ogni** colonna (`.HasComment("...")`), una frase in italiano, concisa, che finisce
  come `COMMENT ON COLUMN` reale nel DB (visibile in pgAdmin, utile a chi interroga a mano) — condensata
  dalla sezione **Descrizione** già scritta nella scheda per quel campo, non ripetuta parola per parola.

Questi commenti **sono** la "descrizione che va nel DB": non serve un elenco separato nella scheda, sono
già dentro il blocco di configurazione EF (che genera `COMMENT ON TABLE …` / `COMMENT ON COLUMN …` nella
migration, non SQL scritto a mano).

## 7. Indici

Traduzione 1:1 della sezione **Indici** già presente in ogni scheda:

```csharp
builder.HasIndex(x => x.BookingNumber)
	.IsUnique()
	.HasFilter("deleted_at IS NULL")
	.HasDatabaseName("uq_booking_reservations_number");

builder.HasIndex(x => new { x.PropertyId, x.StayFrom })
	.HasFilter("deleted_at IS NULL")
	.HasDatabaseName("ix_booking_reservations_property_stay");
```

Ordine colonne = uguaglianza prima del range (già rispettato nella sezione Indici della scheda). Indici
parziali `WHERE deleted_at IS NULL` di norma su tutte le tabelle di dominio con soft-delete.

## 8. `IEntityTypeConfiguration<T>` — scheletro

```csharp
namespace Module.<Modulo>.Infrastructure.Configurations;

public sealed class <Entita>Configuration : IEntityTypeConfiguration<<Entita>>
{
	public void Configure(EntityTypeBuilder<<Entita>> builder)
	{
		builder.ToTable("<tabella>", "<schema>", t => t.HasComment("<descrizione tabella>"));

		builder.HasKey(x => x.Id).HasName("pk_<tabella>");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.Campo)
			.HasColumnName("campo")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("<descrizione campo>");

		// indici — vedi §7
		// relazioni — vedi §3
		// query filter soft-delete — applicato una volta nel DbContext, non qui
	}
}
```

## 9. Cosa scrive ogni scheda (solo le specificità)

1. **Entità/e** coinvolte (aggregate root / entità figlia / owned type / lookup) e modulo.
2. **Enum propri** (nome C#, tipo PG, valori — se non già coperti da un enum condiviso di un'altra
   scheda, nel qual caso si linka quella scheda invece di ridefinirlo).
3. Il blocco `<Entita>Configuration` (§8) **compilato per intero** con tutte le colonne della sezione
   Campi, tutti gli indici della sezione Indici, le relazioni interne allo schema.
4. **Relazioni** verso altri moduli (id copiato, nessuna FK) e verso entità dello stesso schema (FK reale).
5. **Note di migrazione** specifiche (trasformazioni dai campi legacy, non generiche).

Non ripetere: meccanica soft-delete/audit (§1), converter timestamp (§2), meccanica enum PG (§4),
significato di `HasComment` (§6) — questi sono definiti qui una volta sola.
