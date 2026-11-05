using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Application.Services;

/// <summary>Quali snapshot di configurazione dipendono da un tipo di entita' (per nome del tipo o suo prefisso).</summary>
public static class SnapshotTriggers
{
	private static readonly (string Prefix, ConfigSnapshotKind[] Kinds)[] Rules =
	[
		("Property", [ConfigSnapshotKind.PropertyManagementConfig, ConfigSnapshotKind.PublicShowcase]),
		("Bathhouse", [ConfigSnapshotKind.PropertyManagementConfig, ConfigSnapshotKind.PublicShowcase]),
		("OnlineBookingSettings", [ConfigSnapshotKind.PropertyManagementConfig, ConfigSnapshotKind.BeachMap, ConfigSnapshotKind.PublicShowcase]),
		("OnlineAllowed", [ConfigSnapshotKind.PropertyManagementConfig, ConfigSnapshotKind.BeachMap]),
		("OnlineStayLimit", [ConfigSnapshotKind.PropertyManagementConfig]),
		("ServiceBookingSettings", [ConfigSnapshotKind.PropertyManagementConfig, ConfigSnapshotKind.ServicesCatalog]),
		("PricingSeason", [ConfigSnapshotKind.PropertyManagementConfig, ConfigSnapshotKind.PriceCatalog, ConfigSnapshotKind.PublicShowcase]),
		("TariffPeriod", [ConfigSnapshotKind.PropertyManagementConfig, ConfigSnapshotKind.PriceCatalog, ConfigSnapshotKind.PublicShowcase]),
		("NextSeasonTariffPeriod", [ConfigSnapshotKind.PropertyManagementConfig, ConfigSnapshotKind.PriceCatalog]),
		("MapSector", [ConfigSnapshotKind.PropertyManagementConfig, ConfigSnapshotKind.BeachMap, ConfigSnapshotKind.PriceCatalog]),
		("MapLayout", [ConfigSnapshotKind.BeachMap]),
		("MapSpot", [ConfigSnapshotKind.BeachMap]),
		("MapLegendItem", [ConfigSnapshotKind.BeachMap]),
		("Revenue", [ConfigSnapshotKind.PropertyManagementConfig]),
		("SuggestedPrice", [ConfigSnapshotKind.PriceCatalog]),
		("InventoryItem", [ConfigSnapshotKind.PropertyManagementConfig]),
		("PriceList", [ConfigSnapshotKind.PropertyManagementConfig, ConfigSnapshotKind.PriceCatalog, ConfigSnapshotKind.PublicShowcase]),
		("ReducedSeason", [ConfigSnapshotKind.PriceCatalog]),
		("Discount", [ConfigSnapshotKind.PropertyManagementConfig, ConfigSnapshotKind.PriceCatalog]),
		("ServiceSaleItem", [ConfigSnapshotKind.PropertyManagementConfig, ConfigSnapshotKind.ServicesCatalog]),
		("ServiceBookingOption", [ConfigSnapshotKind.ServicesCatalog]),
		("ServiceHourlyOption", [ConfigSnapshotKind.ServicesCatalog]),
		("AutomaticPrice", [ConfigSnapshotKind.ServicesCatalog]),
		("TableService", [ConfigSnapshotKind.ServicesCatalog]),
		("Agreement", [ConfigSnapshotKind.PropertyManagementConfig, ConfigSnapshotKind.AgreementCatalog]),
	];

	public static IEnumerable<ConfigSnapshotKind> For(Type entityType)
		=> Rules.Where(r => entityType.Name.StartsWith(r.Prefix, StringComparison.Ordinal)).SelectMany(r => r.Kinds);
}
