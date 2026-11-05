using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="NextSeasonTariffPeriod"/> su catalog.pricing_next_season_periods.</summary>
public sealed class NextSeasonTariffPeriodConfiguration : IEntityTypeConfiguration<NextSeasonTariffPeriod>
{
	public void Configure(EntityTypeBuilder<NextSeasonTariffPeriod> builder)
	{
		builder.ToTable("pricing_next_season_periods", "catalog", t =>
		{
			t.HasComment("Periodi tariffari della stagione successiva, preparati in anticipo: una riga per intervallo. "
				+ "Separati da pricing_tariff_periods perché listini, preventivi e ricerca usano solo la stagione corrente.");
			t.HasCheckConstraint("ck_catalog_pricing_next_season_periods_range", "ends_on >= starts_on");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_next_season_periods");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SeasonId)
			.HasColumnName("season_id")
			.IsRequired()
			.HasComment("Stagione (pricing_seasons) di cui è la stagione successiva.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(TariffPeriod.NameMaxLength)
			.IsRequired()
			.HasComment("Nome del periodo tariffario (es. Alta stagione).");

		builder.Property(x => x.PeriodGroupKey)
			.HasColumnName("period_group_key")
			.IsRequired()
			.HasComment("Chiave che raggruppa gli intervalli dello stesso periodo nella stagione successiva.");

		builder.Property(x => x.StartsOn)
			.HasColumnName("starts_on")
			.IsRequired()
			.HasComment("Inizio dell'intervallo.");

		builder.Property(x => x.EndsOn)
			.HasColumnName("ends_on")
			.IsRequired()
			.HasComment("Fine dell'intervallo.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.SeasonId, x.PeriodGroupKey }, "ix_catalog_pricing_next_season_periods_season_group")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_next_season_periods_season_group");

		builder.HasOne<PricingSeason>()
			.WithMany(s => s.NextSeasonPeriods)
			.HasForeignKey(x => x.SeasonId)
			.HasConstraintName("fk_catalog_pricing_next_season_periods_season")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
