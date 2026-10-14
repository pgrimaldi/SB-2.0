# Procedura: aggiungere una migration (nuova entità/tabella a un modulo esistente)

> Checklist per lo sviluppatore che aggiunge **una nuova tabella** (e gli enum che porta con sé) a un
> modulo `Module.<Nome>` già esistente. Copre **solo la parte codice** (quali file toccare, in che ordine);
> per le convenzioni di modellazione (enum, timestamp, id, indici, commenti) vedi
> [`analisi-post/db/_dotnet-template.md`](../analisi-post/db/_dotnet-template.md), per i comandi
> `dotnet ef`/Visual Studio vedi [`42-migration.md`](42-migration.md). Presuppone che la scheda di design
> `analisi-post/db/<dominio>/<tabella>.md` esista già con la sezione **Implementazione .NET** compilata
> (entità, enum propri, blocco `Configuration`, indici, relazioni) — qui si parla solo di come cablarla nel
> codice.

---

## Checklist rapida — quali file, quando

| File | Quando |
|---|---|
| `Domain/Enums/<Enum>.cs` | solo se la tabella introduce un enum **nuovo** |
| `Domain/Entities/<Entita>.cs` | sempre |
| `Infrastructure/Configurations/<Entita>Configuration.cs` | sempre |
| `Infrastructure/DbContext.cs` | sempre (`DbSet<Entita>`) + se enum nuovo (`HasPostgresEnum<TEnum>`) |
| `Infrastructure/Infras/ModulePersistenceRegistration.cs` | solo se enum nuovo (`dataSourceBuilder.MapEnum<TEnum>`) |
| `Infrastructure/DesignTimeDbContextFactory.cs` | solo se enum nuovo — **stessa identica riga** del file sopra |
| `Infrastructure/Migrations/*.cs` | generato da `dotnet ef migrations add`, **mai scritto a mano** (eccezioni: SQL non esprimibile in Fluent API o `CREATE EXTENSION`, §7/§2 di `42-migration.md`, sempre aggiunto a mano al file generato) |

`Module.cs` e `Boot/Program.cs` **non cambiano** per una nuova tabella: `ApplyConfigurationsFromAssembly`
nel `DbContext` scopre da solo ogni nuova `IEntityTypeConfiguration<T>` nell'assembly del modulo.

## 1. Enum nuovo (solo se la tabella ne introduce uno)

```csharp
// Domain/Enums/<Enum>.cs
public enum ReservationStatus { Pending = 0, Confirmed = 1, Cancelled = 2, NoShow = 3 }
```

Poi due registrazioni, in punti diversi, che devono restare **sincronizzate** (stessa lista di enum, stesso
nome Postgres):

```csharp
// Infrastructure/DbContext.cs, dentro OnModelCreating — dichiara il tipo per le migration
modelBuilder.HasPostgresEnum<ReservationStatus>(Schema, "reservation_status");
```

```csharp
// Infrastructure/Infras/ModulePersistenceRegistration.cs (runtime) *e*
// Infrastructure/DesignTimeDbContextFactory.cs (design-time, "dotnet ef") — riga identica in entrambi
dataSourceBuilder.MapEnum<ReservationStatus>("<schema>.reservation_status");
```

Se ci si dimentica di aggiungerla in uno dei due file, `dotnet ef migrations add` funziona comunque (usa
`DesignTimeDbContextFactory`), ma **a runtime dentro Boot** l'enum non è mappato sulla connessione e le
query su quella colonna falliscono — motivo per cui i due file vanno editati insieme, mai uno solo.

## 2. Entità

```csharp
// Domain/Entities/<Entita>.cs — anemica: solo proprietà a setter privato, nessun metodo di dominio/factory
// (arriveranno con la BL, fuori dal perimetro attuale — vedi docs/note.md)
public sealed class Reservation : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public ReservationStatus Status { get; private set; }
	// ... resto delle colonne della scheda
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private Reservation() { }   // unico costruttore, per il materializzatore di EF Core
}
```

`ISoftDeletable`/`IAuditable` solo se la tabella ha davvero `deleted_at`/`created_at`+`updated_at` (quasi
sempre, non sulle tabelle di puro log — `_dotnet-template.md` §1).

## 3. `DbSet` nel `DbContext`

```csharp
// Infrastructure/DbContext.cs
public DbSet<Reservation> Reservations => Set<Reservation>();
```

## 4. `IEntityTypeConfiguration<T>`

Nuovo file `Infrastructure/Configurations/<Entita>Configuration.cs`, scheletro completo in
`_dotnet-template.md` §8. Il punto che **non è opzionale e non lo fa nessuna convenzione automatica** per
ogni property di tipo enum nativo:

```csharp
builder.Property(x => x.Status)
	.HasColumnName("status")
	.HasColumnType("<schema>.reservation_status")   // <-- SEMPRE, per ogni property enum
	.IsRequired()
	.HasDefaultValueSql("'pending'");                // <-- HasDefaultValueSql, MAI HasDefaultValue(Enum.Member)
```

Perché è critico, non solo stile — dettaglio completo in `_dotnet-template.md` §4:

- **Senza `HasColumnType`**, EF genera la colonna come `integer` invece che come l'enum nativo. Compila,
  la migration si applica senza errori: il problema esplode solo dopo, il giorno in cui qualcosa (un indice
  parziale, un filtro, una query SQL a mano) confronta la colonna con un'etichetta testuale
  (`WHERE status = 'active'`) e Postgres risponde `22P02: sintassi di input non valida per il tipo integer`.
- **Con `HasColumnType` ma `HasDefaultValue(TEnum.Member)`** (invece di `HasDefaultValueSql`), la migration
  fallisce subito in `dotnet ef database update` con `42804: la colonna "..." è di tipo <schema>.<enum> ma
  l'espressione predefinita è di tipo integer` — perché EF continua a emettere il valore intero sottostante
  come default anche se la colonna è ormai tipizzata come enum.

Entrambe le insidie sono state trovate e corrette su ~90 colonne in 3 moduli (Identity, Privacy,
Notification) il 2026-09-14: non è un caso teorico, è già successo.

## 5. Generare la migration

Da `backend/summerbooking-backend/`, comandi completi in `42-migration.md` §3 (CLI) o §6bis (Visual
Studio):

```bash
dotnet ef migrations add <NomeMigration> --project Module.<Nome>/Module.<Nome>.csproj --output-dir Infrastructure/Migrations
```

## 6. Verificare la migration generata **prima** di applicarla

Questo comando avrebbe intercettato entrambe le insidie del punto 4 prima ancora di toccare il database:

```bash
# nessun risultato atteso da entrambi — se qualcosa esce, tornare al punto 4
grep -n 'table.Column<int>(type: "integer"' Module.<Nome>/Infrastructure/Migrations/*_<NomeMigration>.cs   # colonna enum finita come integer? guardarla a mano
grep -n 'type: "<schema>\.' Module.<Nome>/Infrastructure/Migrations/*_<NomeMigration>.cs | grep 'defaultValue:'   # default bare su colonna enum?
```

Il primo comando include anche colonne `integer` legittime (contatori, quantità): va letto, non basta che
sia vuoto. Il secondo invece deve essere **sempre vuoto** — qualunque riga trovata è un `HasDefaultValue`
da convertire.

## 7. Applicare e validare

```bash
dotnet ef database update --project Module.<Nome>/Module.<Nome>.csproj
```

Poi, dalla root di `backend/summerbooking-backend/`:

```bash
dotnet build                                    # 0 warning/error sull'intera solution
dotnet test ArchitectureTests/ArchitectureTests.csproj   # confini fra moduli/layer non violati
```

## 8. Commit

Entrano nel commit: `Domain/Entities/<Entita>.cs`, `Domain/Enums/<Enum>.cs` (se nuovo),
`Infrastructure/Configurations/<Entita>Configuration.cs`, `Infrastructure/DbContext.cs`, i due file di
registrazione enum (se nuovo), e **tutto** `Infrastructure/Migrations/` incluso lo
`<NomeModulo>DbContextModelSnapshot.cs` aggiornato — mai solo il file `*_<NomeMigration>.cs` da solo.
