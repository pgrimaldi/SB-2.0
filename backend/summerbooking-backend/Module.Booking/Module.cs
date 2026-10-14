using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Booking.Infrastructure.Infras;

namespace Module.Booking;

/// <summary>
/// Punto di composizione del modulo Booking: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddBookingModule();
/// Per ora registra solo la persistenza (DbContext); Application/Endpoints arriveranno con
/// l'implementazione di API e BL del modulo (fuori dal perimetro di questa fase).
/// </summary>
public static class Module
{
	public static IServiceCollection AddBookingModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModuleBookingPersistence(configuration);
		return services;
	}
}
