using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="RevenueWeights"/> su catalog.pricing_revenue_weights (riga unica).</summary>
public sealed class RevenueWeightsConfiguration : IEntityTypeConfiguration<RevenueWeights>
{
	public void Configure(EntityTypeBuilder<RevenueWeights> builder)
	{
		builder.ToTable("pricing_revenue_weights", "catalog", t =>
		{
			t.HasComment("Pesi del prezzo suggerito (systemadmin), uguali per tutti gli stabilimenti: riga unica id = 1.");
			t.HasCheckConstraint("ck_catalog_pricing_revenue_weights_singleton", "id = 1");
			t.HasCheckConstraint("ck_catalog_pricing_revenue_weights_factors",
				"rain_low_season_factor > 0 AND rain_high_season_factor > 0 AND before_weekend_factor > 0 AND high_season_factor > 0 " +
				"AND free_spots_above_factor > 0 AND free_spots_below_factor > 0 AND free_spots_threshold_percent BETWEEN 0 AND 100");
		});
		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_revenue_weights");
		builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever().HasComment("Sempre 1.");
		builder.Property(x => x.RainLowSeasonFactor).HasColumnName("rain_low_season_factor").HasPrecision(5, 3).IsRequired().HasComment("Moltiplicatore con pioggia in bassa stagione.");
		builder.Property(x => x.RainHighSeasonFactor).HasColumnName("rain_high_season_factor").HasPrecision(5, 3).IsRequired().HasComment("Moltiplicatore con pioggia in alta stagione.");
		builder.Property(x => x.BeforeWeekendFactor).HasColumnName("before_weekend_factor").HasPrecision(5, 3).IsRequired().HasComment("Moltiplicatore del giorno prima del weekend.");
		builder.Property(x => x.HighSeasonFactor).HasColumnName("high_season_factor").HasPrecision(5, 3).IsRequired().HasComment("Moltiplicatore di alta stagione (se non è il giorno prima del weekend).");
		builder.Property(x => x.FreeSpotsThresholdPercent).HasColumnName("free_spots_threshold_percent").HasPrecision(5, 2).IsRequired().HasComment("Soglia dei posti liberi in percentuale.");
		builder.Property(x => x.FreeSpotsAboveFactor).HasColumnName("free_spots_above_factor").HasPrecision(5, 3).IsRequired().HasComment("Moltiplicatore con posti liberi dalla soglia in su.");
		builder.Property(x => x.FreeSpotsBelowFactor).HasColumnName("free_spots_below_factor").HasPrecision(5, 3).IsRequired().HasComment("Moltiplicatore con posti liberi sotto la soglia.");
		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
	}
}
