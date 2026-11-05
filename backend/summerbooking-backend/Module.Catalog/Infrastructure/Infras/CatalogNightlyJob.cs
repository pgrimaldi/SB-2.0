using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Services;
using Shared.Infrastructure;

namespace Module.Catalog.Infrastructure.Infras;

/// <summary>
/// Job notturno di Catalog (alle 00:05 ora italiana): azzera i contatori del banner sconto e ricostruisce tutta la
/// proiezione di ricerca (così si aggiornano anche i prezzi «da» che dipendono dal giorno) e tutti gli snapshot di
/// configurazione. All'avvio dell'host ricostruisce
/// subito la proiezione (pochi secondi), così dopo una migrazione dei dati la ricerca è già allineata. Gira nel processo dell'host
/// finché non esiste un host dedicato ai job; con più istanze il lavoro si ripete, il che è innocuo.
/// </summary>
public sealed class CatalogNightlyJob : BackgroundService
{
	private static readonly TimeZoneInfo Rome = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
	private static readonly TimeSpan RunAt = new(0, 5, 0);

	private readonly IServiceScopeFactory _scopes;
	private readonly IDateTimeProvider _clock;
	private readonly ILogger<CatalogNightlyJob> _logger;

	public CatalogNightlyJob(IServiceScopeFactory scopes, IDateTimeProvider clock, ILogger<CatalogNightlyJob> logger)
	{
		_scopes = scopes;
		_clock = clock;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			using var scope = _scopes.CreateScope();
			var rows = await scope.ServiceProvider.GetRequiredService<ISearchProjection>().RefreshAsync(SearchScope.All, stoppingToken);
			_logger.LogInformation("Ricerca pubblica ricostruita all'avvio: {Rows} righe", rows);
			var snapshots = await scope.ServiceProvider.GetRequiredService<ConfigSnapshotService>()
				.RebuildAsync(null, ConfigSnapshotService.AllKinds, onlyMissing: true, stoppingToken);
			_logger.LogInformation("Snapshot mancanti costruiti all'avvio: {Written} (errori {Failed})", snapshots.Written, snapshots.Failed);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Ricostruzione della ricerca pubblica all'avvio non riuscita");
		}

		while (!stoppingToken.IsCancellationRequested)
		{
			var next = NextRunUtc(_clock.UtcNow);
			while (_clock.UtcNow < next)
			{
				var wait = next - _clock.UtcNow;
				await Task.Delay(wait > TimeSpan.FromHours(1) ? TimeSpan.FromHours(1) : wait, stoppingToken);
			}
			try
			{
				using var scope = _scopes.CreateScope();
				var reset = await scope.ServiceProvider.GetRequiredService<IPublicCatalogRepository>().ResetBannerCountersAsync(stoppingToken);
				var rows = await scope.ServiceProvider.GetRequiredService<ISearchProjection>().RefreshAsync(SearchScope.All, stoppingToken);
				var snapshots = await scope.ServiceProvider.GetRequiredService<ConfigSnapshotService>()
					.RebuildAsync(null, ConfigSnapshotService.AllKinds, ct: stoppingToken);
				_logger.LogInformation("Job notturno di Catalog: {Reset} contatori del banner azzerati, {Rows} righe di ricerca ricostruite, " +
					"snapshot {Written} scritti, {Unchanged} invariati, {Removed} tolti, {Failed} in errore",
					reset, rows, snapshots.Written, snapshots.Unchanged, snapshots.Removed, snapshots.Failed);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogError(ex, "Job notturno di Catalog non riuscito: si riprova la notte dopo");
			}
		}
	}

	private static DateTime NextRunUtc(DateTime nowUtc)
	{
		var local = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, Rome);
		var candidate = local.Date + RunAt;
		if (candidate <= local)
			candidate = candidate.AddDays(1);
		return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(candidate, DateTimeKind.Unspecified), Rome);
	}
}
