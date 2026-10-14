using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Catalog.Infrastructure.Infras;

namespace Module.Catalog;

/// <summary>
/// Punto di composizione del modulo Catalog: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddCatalogModule();
/// Per ora registra solo la persistenza (DbContext); Application/Endpoints arriveranno con
/// l'implementazione di API e BL del modulo (fuori dal perimetro di questa fase).
/// </summary>
public static class Module
{
	public static IServiceCollection AddCatalogModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModuleCatalogPersistence(configuration);
		return services;
	}
}
