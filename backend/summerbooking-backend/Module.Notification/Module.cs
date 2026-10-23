using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Module.Notification.Infrastructure.ExternalApi;
using Module.Notification.Infrastructure.Infras;
using Module.Notification.Infrastructure.Outbound;
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
		// Chi puo' ricevere i messaggi di questo ambiente: controllato all'avvio, interrogato da ogni mittente.
		services.AddOptions<OutboundOptions>().Bind(configuration.GetSection(OutboundOptions.Section)).ValidateOnStart();
		services.AddSingleton<IValidateOptions<OutboundOptions>, OutboundOptionsValidator>();
		services.AddSingleton<OutboundGuard>();
		services.AddSingleton<ITransactionalEmailSender, SmtpTransactionalEmailSender>();
		return services;
	}
}
