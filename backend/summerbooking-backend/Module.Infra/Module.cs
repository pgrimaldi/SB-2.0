using BuildingBlocks.Infrastructure.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Module.Infra.Infrastructure.Infras;
using Shared.Contracts.Infra;
using Shared.Infrastructure;

namespace Module.Infra;

/// <summary>
/// Punto di composizione del modulo Infra: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddInfraModule();
/// Oltre alla persistenza espone a Identity la verifica dei client M2M (IPartnerClientAuthenticator).
/// </summary>
public static class Module
{
	public static IServiceCollection AddInfraModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModuleInfraPersistence(configuration);
		services.TryAddSingleton<ISecretResolver, ConfigurationSecretResolver>();
		services.AddScoped<IPartnerClientAuthenticator, PartnerClientAuthenticator>();
		return services;
	}
}
