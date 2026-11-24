using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Payment.Application.Abstractions;
using Module.Payment.Application.Services;
using Module.Payment.Infrastructure.Infras;
using Module.Payment.Infrastructure.Repositories;
using Shared.Contracts.Payment;

namespace Module.Payment;

/// <summary>
/// Punto di composizione del modulo Payment: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddPaymentModule();
/// Per ora solo il nucleo minimo: incassi manuali delle prenotazioni, annullo e rimborso (contratto per Booking).
/// </summary>
public static class Module
{
	public static IServiceCollection AddPaymentModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModulePaymentPersistence(configuration);

		services.AddScoped<ILedgerRepository, LedgerRepository>();
		services.AddScoped<ReservationPayments>();
		services.AddScoped<IReservationPayments>(sp => sp.GetRequiredService<ReservationPayments>());
		services.AddScoped<IOnlineCardPayments, InactiveOnlineCardPayments>();
		services.AddScoped<Shared.Contracts.Events.IIntegrationEventHandler<Shared.Contracts.Booking.ResaleCreditEarned>, ResaleCreditEarnedHandler>();
		services.AddScoped<Shared.Contracts.Events.IIntegrationEventHandler<Shared.Contracts.Booking.ResaleCreditReversed>, ResaleCreditReversedHandler>();
		return services;
	}
}
