using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.FiscalPrint.Infrastructure.Infras;

namespace Module.FiscalPrint;

/// <summary>
/// Punto di composizione del modulo FiscalPrint: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddFiscalPrintModule();
/// Per ora registra solo la persistenza (DbContext); Application/Endpoints arriveranno con
/// l'implementazione di API e BL del modulo (fuori dal perimetro di questa fase).
/// </summary>
public static class Module
{
	public static IServiceCollection AddFiscalPrintModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModuleFiscalPrintPersistence(configuration);
		return services;
	}
}
