using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Module.Booking.Application.Services;
using Shared.Infrastructure;

namespace Module.Booking.Infrastructure.Infras;

/// <summary>
/// A intervalli di <see cref="BookingJobOptions.OnlineExpiryEvery"/>: le prenotazioni online in attesa con il pagamento non
/// arrivato entro il tempo di attesa o non confermate entro la scadenza si annullano, le postazioni si liberano e il cliente riceve il messaggio di scadenza.
/// </summary>
public sealed class OnlineExpiryJob : BackgroundService
{
	private readonly IServiceScopeFactory _scopes;
	private readonly IDateTimeProvider _clock;
	private readonly ILogger<OnlineExpiryJob> _logger;

	private readonly TimeSpan _every;

	public OnlineExpiryJob(IServiceScopeFactory scopes, IDateTimeProvider clock, ILogger<OnlineExpiryJob> logger, IOptions<BookingJobOptions> options)
	{
		_every = options.Value.OnlineExpiryEvery;
		_scopes = scopes;
		_clock = clock;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var timer = new PeriodicTimer(_every);
		do
		{
			try
			{
				using var scope = _scopes.CreateScope();
				var expired = await scope.ServiceProvider.GetRequiredService<OnlineBookingService>().ExpireAsync(_clock.UtcNow, stoppingToken);
				if (expired > 0)
					_logger.LogInformation("Prenotazioni online scadute annullate: {Count}", expired);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogError(ex, "Scadenza delle prenotazioni online non riuscita");
			}
		}
		while (await timer.WaitForNextTickAsync(stoppingToken));
	}
}
