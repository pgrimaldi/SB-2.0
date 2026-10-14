using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="TariffPeriod"/> su catalog.pricing_tariff_periods.</summary>
public sealed class TariffPeriodConfiguration : IEntityTypeConfiguration<TariffPeriod>
{
	public void Configure(EntityTypeBuilder<TariffPeriod> builder)
	{
		builder.ToTable("pricing_tariff_periods", "catalog", t =>
		{
			t.HasComment("Intervallo di date di un periodo tariffario (bassa/media/alta stagione), eventualmente non contiguo.");
			t.HasCheckConstraint("ck_catalog_pricing_tariff_periods_range", "ends_on >= starts_on");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_tariff_periods");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SeasonId)
			.HasColumnName("season_id")
			.IsRequired()
			.HasComment("Stagione a cui appartiene il periodo.");
		builder.HasOne(x => x.Season)
			.WithMany(s => s.TariffPeriods)
			.HasForeignKey(x => x.SeasonId)
			.HasConstraintName("fk_catalog_pricing_tariff_periods_season")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(80)
			.IsRequired()
			.HasComment("Nome del periodo tariffario (es. Alta stagione); solo etichetta, le correlazioni sono per id.");

		builder.Property(x => x.PeriodGroupKey)
			.HasColumnName("period_group_key")
			.IsRequired()
			.HasComment("Chiave che raggruppa gli intervalli non contigui dello stesso periodo tariffario.");

		builder.Property(x => x.Tier)
			.HasColumnName("tier")
			.HasColumnType("catalog.tariff_tier")
			.HasDefaultValueSql("'custom'")
			.IsRequired()
			.HasComment("Livello tariffario normalizzato, indipendente dal nome/lingua.");

		builder.Property(x => x.StartsOn)
			.HasColumnName("starts_on")
			.IsRequired()
			.HasComment("Inizio dell'intervallo di date coperto dalla riga.");

		builder.Property(x => x.EndsOn)
			.HasColumnName("ends_on")
			.IsRequired()
			.HasComment("Fine dell'intervallo di date coperto dalla riga.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete dell'intervallo.");

		builder.HasIndex(x => x.SeasonId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_tariff_periods_season");

		builder.HasIndex(x => new { x.SeasonId, x.PeriodGroupKey })
			.HasDatabaseName("ix_catalog_pricing_tariff_periods_season_group");

		builder.HasIndex(x => new { x.SeasonId, x.StartsOn, x.EndsOn })
			.HasDatabaseName("ix_catalog_pricing_tariff_periods_range");
	}
}
