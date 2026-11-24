namespace Module.Catalog.Infrastructure.Infras;

/// <summary>Orari dei job di Catalog (sezione <c>Catalog:Jobs</c>); senza configurazione valgono i predefiniti.</summary>
public sealed class CatalogJobOptions
{
	public const string Section = "Catalog:Jobs";

	/// <summary>Ora italiana del job notturno.</summary>
	public TimeSpan NightlyAt { get; set; } = new(0, 5, 0);
}
