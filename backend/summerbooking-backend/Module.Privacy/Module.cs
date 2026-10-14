using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Privacy.Infrastructure.Infras;

namespace Module.Privacy;

/// <summary>
/// Punto di composizione del modulo Privacy: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddPrivacyModule();
/// Per ora registra solo la persistenza (DbContext); Application/Endpoints arriveranno con
/// l'implementazione di API e BL del modulo (fuori dal perimetro di questa fase).
/// </summary>
public static class Module
{
	public static IServiceCollection AddPrivacyModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModulePrivacyPersistence(configuration);
		return services;
	}
}
