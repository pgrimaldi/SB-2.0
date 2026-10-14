using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Audit.Application.Abstractions;
using Module.Audit.Application.Services;
using Module.Audit.Infrastructure.Infras;
using Module.Audit.Infrastructure.Repositories;
using Shared.Contracts.Audit;
using Shared.Contracts.Events;

namespace Module.Audit;

/// <summary>
/// Punto di composizione del modulo Audit: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddAuditModule();
/// </summary>
public static class Module
{
	public static IServiceCollection AddAuditModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModuleAuditPersistence(configuration);

		services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();
		services.AddSingleton<IAuditExportWriter, AuditExportWriter>();

		foreach (var validator in typeof(Module).Assembly.GetTypes()
			.Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IValidator).IsAssignableFrom(t)))
			services.AddSingleton(validator);

		services.AddScoped<AuditEntryService>();
		services.AddScoped<PlatformAuditService>();

		// Gli altri moduli scrivono nel registro solo pubblicando questo evento.
		services.AddScoped<IIntegrationEventHandler<AuditEntryRecorded>, AuditEntryRecordedHandler>();

		return services;
	}
}
