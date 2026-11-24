using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Module.Booking.Application.Abstractions;
using Module.Booking.Application.Services;
using Shared.Infrastructure;

namespace Module.Booking.Infrastructure.Infras;

/// <summary>
/// Job notturno di Booking, all'ora italiana di <see cref="BookingJobOptions.NightlyAt"/>: elimina davvero le prenotazioni
/// rimaste nel cestino oltre il tempo di conservazione (della prenotazione resta la traccia nel registro; incassi e scontrini
/// restano in Payment e Fiskaly), dimentica i comandi offline vecchi e cancella i file allegati degli anni passati. Gira nel
/// processo dell'host finché non esiste un host dedicato ai job; con più istanze il lavoro si ripete, il che è innocuo.
/// </summary>
public sealed class BookingNightlyJob : BackgroundService
{
	private static readonly TimeZoneInfo Rome = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

	private readonly IServiceScopeFactory _scopes;
	private readonly IDateTimeProvider _clock;
	private readonly ILogger<BookingNightlyJob> _logger;

	private readonly TimeSpan _runAt;

	public BookingNightlyJob(IServiceScopeFactory scopes, IDateTimeProvider clock, ILogger<BookingNightlyJob> logger, IOptions<BookingJobOptions> options)
	{
		_runAt = options.Value.NightlyAt;
		_scopes = scopes;
		_clock = clock;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
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
				var purged = await scope.ServiceProvider.GetRequiredService<TrashService>().PurgeExpiredAsync(_clock.UtcNow, stoppingToken);
				_logger.LogInformation("Job notturno di Booking: {Purged} prenotazioni eliminate dal cestino", purged);
				var commands = await scope.ServiceProvider.GetRequiredService<IProcessedCommandRepository>().PurgeBeforeAsync(_clock.UtcNow.AddDays(-7), stoppingToken);
				_logger.LogInformation("Job notturno di Booking: {Commands} comandi offline dimenticati", commands);
				var files = await scope.ServiceProvider.GetRequiredService<AttachmentService>().PurgePastYearsAsync(_clock.UtcNow, stoppingToken);
				_logger.LogInformation("Job notturno di Booking: {Files} allegati degli anni passati cancellati (resta il nome)", files);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogError(ex, "Job notturno di Booking non riuscito: si riprova la notte dopo");
			}
		}
	}

	private DateTime NextRunUtc(DateTime nowUtc)
	{
		var local = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, Rome);
		var candidate = local.Date + _runAt;
		if (candidate <= local)
			candidate = candidate.AddDays(1);
		return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(candidate, DateTimeKind.Unspecified), Rome);
	}
}
