using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Payment.Infrastructure.Infras;

namespace Module.Payment;

/// <summary>
/// Punto di composizione del modulo Payment: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddPaymentModule();
/// Per ora registra solo la persistenza (DbContext); Application/Endpoints arriveranno con
/// l'implementazione di API e BL del modulo (fuori dal perimetro di questa fase).
/// </summary>
public static class Module
{
	public static IServiceCollection AddPaymentModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModulePaymentPersistence(configuration);
		return services;
	}
}
