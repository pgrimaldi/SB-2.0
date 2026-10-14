using Microsoft.Extensions.DependencyInjection;
using Module.ZTest.Application.Abstractions;
using Module.ZTest.Application.Services;
using Module.ZTest.Application.Validators;
using Module.ZTest.Infrastructure.Infras;
using Module.ZTest.Infrastructure.Repositories;

namespace Module.ZTest;

/// <summary>
/// Punto di composizione del modulo ZTest: registra i servizi del modulo nel container DI.
/// Viene invocato dal progetto Boot in fase di avvio: builder.Services.AddZTestModule();
/// </summary>
public static class Module
{
	public static IServiceCollection AddZTestModule(this IServiceCollection services)
	{
		// Infrastructure (implementazioni delle porte dell'Application)
		services.AddSingleton<IReservationRepository, ReservationRepository>();
		services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

		// Application (use case e validatori)
		services.AddSingleton<CreateReservationValidator>();
		services.AddScoped<ReservationService>();

		return services;
	}
}
