using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Chatbot.Infrastructure.Infras;

namespace Module.Chatbot;

/// <summary>
/// Punto di composizione del modulo Chatbot: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddChatbotModule();
/// Per ora registra solo la persistenza (DbContext); Application/Endpoints arriveranno con
/// l'implementazione di API e BL del modulo (fuori dal perimetro di questa fase).
/// </summary>
public static class Module
{
	public static IServiceCollection AddChatbotModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModuleChatbotPersistence(configuration);
		return services;
	}
}
