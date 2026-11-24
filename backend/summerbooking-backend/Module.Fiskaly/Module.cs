using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Fiskaly.Infrastructure.Infras;

namespace Module.Fiskaly;

/// <summary>
/// Punto di composizione del modulo Fiskaly: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddFiskalyModule();
/// Per ora registra la persistenza (DbContext) e l'annullo degli scontrini per Booking, non ancora attivo;
/// Application/Endpoints arriveranno con l'implementazione di API e BL del modulo.
/// </summary>
public static class Module
{
	public static IServiceCollection AddFiskalyModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModuleFiskalyPersistence(configuration);
		services.AddScoped<Shared.Contracts.Fiskaly.IReservationReceipts, Application.Services.InactiveReservationReceipts>();
		return services;
	}
}
