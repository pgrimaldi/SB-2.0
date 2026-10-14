using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Fiskaly.Infrastructure.Infras;

namespace Module.Fiskaly;

/// <summary>
/// Punto di composizione del modulo Fiskaly: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddFiskalyModule();
/// Per ora registra solo la persistenza (DbContext); Application/Endpoints arriveranno con
/// l'implementazione di API e BL del modulo (fuori dal perimetro di questa fase).
/// </summary>
public static class Module
{
	public static IServiceCollection AddFiskalyModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModuleFiskalyPersistence(configuration);
		return services;
	}
}
