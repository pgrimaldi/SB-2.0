using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Module.Identity.Infrastructure.ExternalApi;
using Module.Identity.Infrastructure.Security;
using Shared.Infrastructure;

namespace Module.Identity.Infrastructure.Infras;

/// <summary>
/// Job mensile notturno che aggiorna l'elenco delle password diffuse. Gira nel processo dell'host finché
/// non esiste un host dedicato ai job; con più istanze ognuna aggiorna la propria copia, il che è innocuo.
/// </summary>
public sealed class CommonPasswordRefreshJob : BackgroundService
{
	private readonly IServiceScopeFactory _scopes;
	private readonly IDateTimeProvider _clock;
	private readonly CommonPasswordOptions _options;
	private readonly ILogger<CommonPasswordRefreshJob> _logger;

	public CommonPasswordRefreshJob(IServiceScopeFactory scopes, IDateTimeProvider clock, IOptions<CommonPasswordOptions> options, ILogger<CommonPasswordRefreshJob> logger)
	{
		_scopes = scopes;
		_clock = clock;
		_options = options.Value;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var zone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZone);

		while (!stoppingToken.IsCancellationRequested)
		{
			var next = NextRunUtc(_clock.UtcNow, zone);
			_logger.LogInformation("Prossimo aggiornamento delle password diffuse: {Next:u}", next);

			// A tratti di un giorno al massimo: un'attesa di settimane in un colpo solo non sopravvive bene
			// a cambi d'ora e regolazioni dell'orologio.
			while (_clock.UtcNow < next)
			{
				var wait = next - _clock.UtcNow;
				await Task.Delay(wait > TimeSpan.FromDays(1) ? TimeSpan.FromDays(1) : wait, stoppingToken);
			}

			try
			{
				using var scope = _scopes.CreateScope();
				await scope.ServiceProvider.GetRequiredService<CommonPasswordDownloader>().RefreshAsync(stoppingToken);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				// Il job non deve fermarsi: l'elenco in uso resta valido, si riprova il mese dopo.
				_logger.LogError(ex, "Aggiornamento delle password diffuse non riuscito");
			}
		}
	}

	/// <summary>Il prossimo giorno <see cref="CommonPasswordOptions.DayOfMonth"/> all'ora locale configurata, dopo <paramref name="nowUtc"/>.</summary>
	public DateTime NextRunUtc(DateTime nowUtc, TimeZoneInfo zone)
	{
		var local = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone);
		for (var monthOffset = 0; ; monthOffset++)
		{
			var month = new DateTime(local.Year, local.Month, 1).AddMonths(monthOffset);
			var day = Math.Min(_options.DayOfMonth, DateTime.DaysInMonth(month.Year, month.Month));
			var candidate = new DateTime(month.Year, month.Month, day, _options.Hour, 0, 0, DateTimeKind.Unspecified);
			if (zone.IsInvalidTime(candidate))
				candidate = candidate.AddHours(1);   // ora saltata dal passaggio all'ora legale
			var candidateUtc = TimeZoneInfo.ConvertTimeToUtc(candidate, zone);
			if (candidateUtc > nowUtc)
				return candidateUtc;
		}
	}
}
