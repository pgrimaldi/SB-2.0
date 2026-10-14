using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Notification.Infrastructure.ExternalApi;
using Module.Notification.Infrastructure.Infras;
using Shared.Contracts.Notification;

namespace Module.Notification;

/// <summary>
/// Punto di composizione del modulo Notification: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddNotificationModule();
/// Oltre alla persistenza espone l'invio minimo delle email di servizio (ITransactionalEmailSender).
/// </summary>
public static class Module
{
	public static IServiceCollection AddNotificationModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModuleNotificationPersistence(configuration);
		services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.Section));
		services.AddSingleton<ITransactionalEmailSender, SmtpTransactionalEmailSender>();
		return services;
	}
}
