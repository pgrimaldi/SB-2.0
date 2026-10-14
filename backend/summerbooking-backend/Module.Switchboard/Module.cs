using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Switchboard.Infrastructure.Infras;

namespace Module.Switchboard;

/// <summary>
/// Punto di composizione del modulo Switchboard: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddSwitchboardModule();
/// Per ora registra solo la persistenza (DbContext); Application/Endpoints arriveranno con
/// l'implementazione di API e BL del modulo (fuori dal perimetro di questa fase).
/// </summary>
public static class Module
{
	public static IServiceCollection AddSwitchboardModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModuleSwitchboardPersistence(configuration);
		return services;
	}
}
