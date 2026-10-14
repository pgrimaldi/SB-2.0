using System;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Convenzione unica di connection string per tutti i moduli: stesso database fisico
/// PostgreSQL, uno schema per modulo. Ogni DbContext/DesignTimeDbContextFactory
/// di modulo la richiama, cosi la stringa non e duplicata/reinventata in ogni modulo. Non contiene
/// alcuna connection string: quelle vivono in Boot/appsettings*.json (o nella variabile d'ambiente
/// di sovrascrittura), mai nel codice.
/// </summary>
public static class ModuleConnectionStrings
{
	/// <summary>
	/// Chiave in ConnectionStrings (appsettings.json/appsettings.{Environment}.json). Sovrascrivibile
	/// senza toccare i file, in ogni ambiente, con la variabile d'ambiente standard di IConfiguration
	/// ConnectionStrings__SummerBooking (ha sempre precedenza, e la stessa convenzione usata dal
	/// percorso design-time in <see cref="ResolveForDesignTime"/>).
	/// </summary>
	public const string ConnectionStringName = "SummerBooking";

	/// <summary>
	/// Nome della tabella di storico migration, distinto per schema (vedi
	/// UseNpgsql(..., npgsql =&gt; npgsql.MigrationsHistoryTable(MigrationsHistoryTable, DbContext.Schema))):
	/// piu moduli condividono lo stesso database fisico, ognuno deve avere il proprio storico.
	/// </summary>
	public const string MigrationsHistoryTable = "__ef_migrations_history";

	/// <summary>
	/// Timeout dei comandi a design-time ("dotnet ef"), in secondi. Il default Npgsql e' 30, che basta
	/// per il DDL ordinario ma non per una migration che riscrive dati: il partizionamento di
	/// booking.resource_consumptions copia 3,8 milioni di righe (786 MB) e a 30 secondi fallisce a
	/// meta' — il DDL PostgreSQL e' transazionale, quindi non corrompe nulla, ma la migration non
	/// passa mai. Vale solo per le DesignTimeDbContextFactory: il percorso di runtime dentro Boot
	/// resta sul default, dove un comando che dura mezz'ora sarebbe un errore e non una feature.
	/// </summary>
	public const int DesignTimeCommandTimeoutSeconds = 1800;

	/// <summary>
	/// Tetto di connessioni del pool di UN modulo. Serve perche' ogni modulo costruisce il proprio
	/// NpgsqlDataSource (necessario: le mappature degli enum nativi sono per data source), quindi
	/// l'host ha <b>un pool per modulo</b>, non uno solo.
	/// </summary>
	/// <remarks>
	/// Il default Npgsql e' 100 per pool. Con i 12 moduli cablati in Boot questo significa fino a
	/// 1.200 connessioni possibili verso un PostgreSQL che di default ne accetta 100
	/// (<c>max_connections</c>), e su RDS qualche centinaio secondo la taglia: sotto carico
	/// simultaneo l'istanza rifiuterebbe le connessioni prima che i pool raggiungano il proprio
	/// tetto, con errori che sembrano problemi di rete e non di dimensionamento.
	/// <para>
	/// Il valore 7 nasce da 100 meno una riserva per migration, monitoraggio e sessioni
	/// amministrative, diviso per i 12 moduli (12 x 7 = 84, riserva 16). Va rivisto a ogni modulo aggiunto. E' volutamente tarato sull'ambiente piu' piccolo:
	/// un tetto troppo basso si manifesta come attesa in coda, misurabile e diagnosticabile; uno
	/// troppo alto come rifiuto di connessione a livello di istanza, che colpisce anche chi sta
	/// gia' lavorando.
	/// </para>
	/// <para>
	/// Non e' una prenotazione: <c>MinPoolSize</c> resta 0, quindi un modulo inattivo non tiene
	/// aperta alcuna connessione e il costo reale a riposo e' nullo. Il tetto conta solo sotto
	/// carico contemporaneo su piu' moduli.
	/// </para>
	/// <para>
	/// Si sovrascrive per ambiente con <c>Database:MaxPoolSizePerModule</c> in appsettings, oppure
	/// dichiarando <c>Maximum Pool Size</c> direttamente nella connection string (che ha la
	/// precedenza e disattiva questo default). La soluzione strutturale, quando i moduli avranno
	/// carichi molto diversi fra loro, non e' alzare questo numero ma mettere un pooler
	/// (PgBouncer) davanti al database: qui si dividono in parti uguali connessioni che i moduli
	/// non usano affatto in parti uguali.
	/// </para>
	/// </remarks>
	public const int DefaultMaxPoolSizePerModule = 7;

	/// <summary>Chiave di configurazione per sovrascrivere <see cref="DefaultMaxPoolSizePerModule"/>.</summary>
	public const string MaxPoolSizeConfigurationKey = "Database:MaxPoolSizePerModule";

	/// <summary>
	/// Applica il tetto del pool al data source di un modulo, se l'ambiente non ne ha gia' dichiarato
	/// uno proprio nella connection string. Da chiamare in ogni ModulePersistenceRegistration prima
	/// di Build().
	/// </summary>
	public static void ApplyModulePoolLimit(NpgsqlDataSourceBuilder dataSourceBuilder, IConfiguration configuration)
	{
		var connectionString = dataSourceBuilder.ConnectionStringBuilder;

		// Una scelta esplicita dell'ambiente vince sempre: chi ha scritto "Maximum Pool Size" nella
		// connection string sapeva cosa stava facendo, e sovrascriverlo qui sarebbe una sorpresa.
		if (connectionString.ContainsKey("Maximum Pool Size"))
			return;

		var limite = DefaultMaxPoolSizePerModule;
		if (int.TryParse(configuration[MaxPoolSizeConfigurationKey], out var configurato) && configurato > 0)
			limite = configurato;

		connectionString.MaxPoolSize = limite;
	}

	/// <summary>
	/// Sezione di configurazione con i pezzi sciolti della connessione (<c>Database:Host</c>,
	/// <c>Database:Port</c>, <c>Database:Name</c>, <c>Database:Username</c>, <c>Database:Password</c>),
	/// usata solo quando <see cref="ConnectionStringName"/> non c'e.
	/// </summary>
	public const string DatabaseSectionKey = "Database";

	/// <summary>Uso a runtime dentro Boot: la configurazione arriva gia da WebApplicationBuilder
	/// (appsettings.json + appsettings.{Environment}.json + variabili d'ambiente).</summary>
	/// <remarks>
	/// <para>
	/// La chiave <c>ConnectionStrings:SummerBooking</c> ha <b>sempre la precedenza</b>: in sviluppo
	/// resta quella di Boot/appsettings.Development.json e non cambia nulla, e la variabile
	/// d'ambiente <c>ConnectionStrings__SummerBooking</c> continua a essere la via di sovrascrittura
	/// documentata.
	/// </para>
	/// <para>
	/// Se quella chiave manca si compone la stringa dai pezzi della sezione <c>Database</c>, che e'
	/// il modo in cui arriva su AWS: <c>infra/summerbooking-aws.yaml</c> inietta in ognuno dei tre
	/// container (<c>web</c>, <c>worker</c>, <c>job</c>) <c>Database__Host</c> — l'endpoint di RDS
	/// Proxy — piu <c>Database__Port</c>, <c>Database__Name</c> e, da Secrets Manager,
	/// <c>Database__Username</c> e <c>Database__Password</c>. Una password che viene da Secrets
	/// Manager non si puo concatenare dentro una variabile d'ambiente in una task definition ECS,
	/// quindi la composizione deve avvenire qui; ed e preferibile all'alternativa (mettere la
	/// stringa intera in un segreto) perche cosi <b>il segreto resta la sola password</b>, mentre
	/// host, porta e nome del database — che segreti non sono — restano in chiaro nel template.
	/// </para>
	/// <para>
	/// <c>SslMode.Require</c> non e un extra: il proxy e dichiarato con <c>RequireTLS: true</c> e
	/// senza TLS rifiuta la connessione. E sovrascrivibile con <c>Database:SslMode</c> per gli
	/// ambienti che non lo vogliono (un PostgreSQL locale in chiaro, per esempio).
	/// </para>
	/// </remarks>
	public static string Resolve(IConfiguration configuration)
	{
		var completa = configuration.GetConnectionString(ConnectionStringName);
		if (!string.IsNullOrWhiteSpace(completa))
			return completa;

		var sezione = configuration.GetSection(DatabaseSectionKey);
		var host = sezione["Host"];
		if (string.IsNullOrWhiteSpace(host))
			throw new InvalidOperationException(
				$"Connection string '{ConnectionStringName}' non configurata e sezione '{DatabaseSectionKey}' " +
				$"incompleta (manca {DatabaseSectionKey}:Host). Impostare ConnectionStrings:{ConnectionStringName} " +
				$"in appsettings.json/appsettings.{{Environment}}.json (vedi Boot/appsettings.Development.json per " +
				$"lo sviluppo locale), oppure la variabile d'ambiente ConnectionStrings__{ConnectionStringName}, " +
				$"oppure i pezzi {DatabaseSectionKey}__Host/Port/Name/Username/Password (come fa " +
				"infra/summerbooking-aws.yaml).");

		var builder = new NpgsqlConnectionStringBuilder
		{
			Host = host,
			Database = sezione["Name"],
			Username = sezione["Username"],
			Password = sezione["Password"],
			SslMode = Enum.TryParse<SslMode>(sezione["SslMode"], ignoreCase: true, out var modo)
				? modo
				: SslMode.Require,
		};
		if (int.TryParse(sezione["Port"], out var porta) && porta > 0)
			builder.Port = porta;

		return builder.ConnectionString;
	}

	/// <summary>
	/// Uso da "dotnet ef" (design-time): non c'e un host che costruisca gia la configurazione, quindi qui
	/// se ne ricostruisce una equivalente leggendo Boot/appsettings.json e Boot/appsettings.{Environment}.json.
	/// "dotnet ef" imposta sempre la directory corrente sulla cartella del progetto modulo target (anche
	/// se il comando viene lanciato da un'altra cartella), quindi Boot si trova
	/// risalendo di un livello (i moduli sono cartelle sorelle di Boot dentro summerbooking-backend). Se quella
	/// cartella non esiste (es. factory eseguita fuori dal contesto della solution) si ricade sulle sole
	/// variabili d'ambiente, stessa convenzione di sovrascrittura del runtime.
	/// </summary>
	public static string ResolveForDesignTime()
	{
		var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
		var bootDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Boot"));

		var configurationBuilder = new ConfigurationBuilder();
		if (Directory.Exists(bootDirectory))
		{
			configurationBuilder
				.SetBasePath(bootDirectory)
				.AddJsonFile("appsettings.json", optional: true)
				.AddJsonFile($"appsettings.{environmentName}.json", optional: true);
		}

		configurationBuilder.AddEnvironmentVariables();

		return Resolve(configurationBuilder.Build());
	}
}
