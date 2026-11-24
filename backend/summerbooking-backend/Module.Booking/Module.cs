using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Booking.Application.Abstractions;
using Module.Booking.Application.Services;
using Module.Booking.Infrastructure.Infras;
using Module.Booking.Infrastructure.Repositories;
using BuildingBlocks.Infrastructure.Storage;
using Shared.Contracts.Booking;
using Shared.Contracts.Catalog;
using Shared.Contracts.Events;
using Shared.Contracts.Payment;

namespace Module.Booking;

/// <summary>
/// Punto di composizione del modulo Booking: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddBookingModule();
/// </summary>
public static class Module
{
	public static IServiceCollection AddBookingModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModuleBookingPersistence(configuration);

		// Infrastructure: repository sul DbContext (Scoped) e job notturno del cestino.
		services.AddScoped<IReservationRepository, ReservationRepository>();
		services.AddScoped<IdempotencyService>();
		services.AddSingleton<IOrphanReportWriter, Infrastructure.Infras.OrphanReportWriter>();
		services.AddScoped<ISpotHoldRepository, SpotHoldRepository>();
		services.AddScoped<IBookingGroupRepository, BookingGroupRepository>();
		services.AddScoped<IResaleRepository, ResaleRepository>();
		services.AddScoped<IProcessedCommandRepository, ProcessedCommandRepository>();
		services.AddScoped<IAttachmentRepository, AttachmentRepository>();
		services.AddScoped<AttachmentService>();
		var attachments = configuration.GetSection(AttachmentStorage.Section).Get<ObjectStorageOptions>() ?? new ObjectStorageOptions();
		services.AddSingleton<IAttachmentStorage>(new AttachmentStorage(new S3ObjectStorage(attachments)));
		services.AddScoped<SyncService>();
		services.Configure<BookingJobOptions>(configuration.GetSection(BookingJobOptions.Section));
		services.AddHostedService<BookingNightlyJob>();

		// Application: validatori senza stato, servizi per richiesta.
		foreach (var validator in typeof(Module).Assembly.GetTypes()
			.Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IValidator).IsAssignableFrom(t)))
			services.AddSingleton(validator);

		services.AddScoped<BookingScope>();
		services.AddScoped<BookingAudit>();
		services.AddScoped<SpotDirectory>();
		services.AddScoped<ReservationNumbers>();
		services.AddScoped<SharingLedger>();
		services.AddScoped<ServiceWriter>();
		services.AddScoped<InventoryPlanner>();
		services.AddScoped<ServicePlannerService>();
		services.AddScoped<BookingMessages>();
		services.AddScoped<CustomerViews>();
		services.AddScoped<OnlineBookingService>();
		services.AddScoped<IOnlineBookingPayments>(sp => sp.GetRequiredService<OnlineBookingService>());
		services.AddScoped<CustomerAreaService>();
		services.AddHostedService<OnlineExpiryJob>();
		services.AddScoped<SharingService>();
		services.AddScoped<ReservationWriter>();
		services.AddScoped<ReservationService>();
		services.AddScoped<TrashService>();
		services.AddScoped<MapService>();
		services.AddScoped<GroupService>();
		services.AddScoped<RenewalService>();
		services.AddScoped<SplitService>();
		services.AddScoped<SearchService>();

		// Totali degli incassi pubblicati da Payment.
		services.AddScoped<IIntegrationEventHandler<ReservationPaymentsChanged>, ReservationPaymentsChangedHandler>();

		// Stabilimento nel cestino o ripristinato (Catalog).
		services.AddScoped<IIntegrationEventHandler<PropertyTrashChanged>, PropertyTrashChangedHandler>();

		// Postazioni tolte o create nell'editor della mappa (Catalog).
		services.AddScoped<IIntegrationEventHandler<MapSpotsChanged>, MapSpotsChangedHandler>();
		services.AddScoped<OrphanReportService>();

		return services;
	}
}
