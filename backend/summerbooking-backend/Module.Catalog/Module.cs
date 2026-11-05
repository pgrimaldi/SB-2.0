using BuildingBlocks.Infrastructure.Storage;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Services;
using Module.Catalog.Endpoints;
using Module.Catalog.Infrastructure.Infras;
using Module.Catalog.Infrastructure.Repositories;
using Shared.Contracts.Events;
using Shared.Contracts.Identity;

namespace Module.Catalog;

/// <summary>
/// Punto di composizione del modulo Catalog: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddCatalogModule();
/// </summary>
public static class Module
{
	public static IServiceCollection AddCatalogModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModuleCatalogPersistence(configuration);

		// Infrastructure: repository sul DbContext (Scoped).
		services.AddScoped<IUnitOfWork, UnitOfWork>();
		services.AddScoped<IPropertyRepository, PropertyRepository>();
		services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
		services.AddScoped<ISeasonRepository, SeasonRepository>();
		services.AddScoped<ISectorRepository, SectorRepository>();
		services.AddScoped<IMapRepository, MapRepository>();
		services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
		services.AddScoped<ILookupRepository, LookupRepository>();
		services.AddScoped<IPropertyInfoRepository, PropertyInfoRepository>();
		services.AddScoped<IOnlineSettingsRepository, OnlineSettingsRepository>();
		services.AddScoped<IServiceCatalogRepository, ServiceCatalogRepository>();
		services.AddScoped<IPriceListRepository, PriceListRepository>();
		services.AddScoped<IDiscountRepository, DiscountRepository>();
		services.AddScoped<IAgreementRepository, AgreementRepository>();
		services.AddScoped<IRevenueRepository, RevenueRepository>();
		services.AddScoped<IPlatformRepository, PlatformRepository>();
		services.AddScoped<IPublicCatalogRepository, PublicCatalogRepository>();
		services.AddScoped<ISearchProjection, SearchProjection>();
		services.AddScoped<IConfigSnapshotRepository, ConfigSnapshotRepository>();
		services.AddHostedService<CatalogNightlyJob>();
		services.ConfigureSwaggerGen(CatalogOpenApi.Configure);
		services.AddHttpClient(OpenMeteoWeather.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(10));
		services.AddScoped<IWeatherForecast, OpenMeteoWeather>();
		services.AddSingleton<ITabularFiles, TabularFiles>();

		services.TryAddSingleton(new PublicFileUrlResolver(configuration.GetSection(PublicFileOptions.Section).Get<PublicFileOptions>() ?? new PublicFileOptions()));
		services.AddSingleton<IMediaUrls, MediaUrls>();
		var media = configuration.GetSection(PropertyFileStorage.Section).Get<ObjectStorageOptions>() ?? new ObjectStorageOptions();
		services.AddSingleton<IPropertyFileStorage>(new PropertyFileStorage(new S3ObjectStorage(media)));

		// Application: validatori senza stato, servizi per richiesta.
		foreach (var validator in typeof(Module).Assembly.GetTypes()
			.Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IValidator).IsAssignableFrom(t)))
			services.AddSingleton(validator);

		services.AddScoped<CurrentProperty>();
		services.AddScoped<CatalogAudit>();
		services.AddScoped<InventoryService>();
		services.AddScoped<SeasonService>();
		services.AddScoped<SectorService>();
		services.AddScoped<CatalogMedia>();
		services.AddScoped<LookupService>();
		services.AddScoped<PropertyInfoService>();
		services.AddScoped<PropertyRegistrationService>();
		services.AddScoped<OnlineSettingsService>();
		services.AddScoped<VatPolicy>();
		services.AddScoped<ServiceItemsService>();
		services.AddScoped<AutomaticPricesService>();
		services.AddScoped<TableServicesService>();
		services.AddScoped<PriceListsService>();
		services.AddScoped<PriceListTransferService>();
		services.AddScoped<ReducedSeasonsService>();
		services.AddScoped<DiscountsService>();
		services.AddScoped<AgreementsService>();
		services.AddScoped<RevenueService>();
		services.AddScoped<RevenueAdminService>();
		services.AddScoped<PlatformPropertiesService>();
		services.AddScoped<PlatformMapCatalogService>();
		services.AddScoped<PlatformShowcaseService>();
		services.AddScoped<PublicShowcaseService>();
		services.AddScoped<PublicBathhouseService>();
		services.AddScoped<QuoteService>();
		services.AddScoped<ConfigSnapshotBuilder>();
		services.AddSingleton<ConfigSnapshotService>();

		// Gestori degli eventi che pubblica Identity: titolare, prenotazione online, abbonamento, «mi piace».
		services.AddScoped<IIntegrationEventHandler<PropertyOwnerAssigned>, PropertyOwnerAssignedHandler>();
		services.AddScoped<IIntegrationEventHandler<PropertyOnlineReservationSet>, PropertyOnlineReservationSetHandler>();
		services.AddScoped<IIntegrationEventHandler<PropertySubscriptionChanged>, PropertySubscriptionChangedHandler>();
		services.AddScoped<IIntegrationEventHandler<BathhouseLikeChanged>, BathhouseLikeChangedHandler>();
		services.AddScoped<MapProjection>();
		services.AddScoped<MapSettingsService>();
		services.AddScoped<MapEditorService>();

		return services;
	}
}
