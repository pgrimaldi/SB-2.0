using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="RevenueStandardPeriod"/> su catalog.pricing_revenue_standard_periods.</summary>
public sealed class RevenueStandardPeriodConfiguration : IEntityTypeConfiguration<RevenueStandardPeriod>
{
	public void Configure(EntityTypeBuilder<RevenueStandardPeriod> builder)
	{
		builder.ToTable("pricing_revenue_standard_periods", "catalog", t =>
		{
			t.HasComment("Periodi standard del prezzo suggerito (systemadmin), uguali per tutti e ripetuti ogni anno; fuori dai periodi vale la bassa stagione.");
			t.HasCheckConstraint("ck_catalog_pricing_revenue_standard_periods_dates", "start_month BETWEEN 1 AND 12 AND end_month BETWEEN 1 AND 12 AND start_day BETWEEN 1 AND 31 AND end_day BETWEEN 1 AND 31");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_revenue_standard_periods");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.IsRequired()
			.HasMaxLength(120)
			.HasComment("Nome del periodo.");

		builder.Property(x => x.StartMonth)
			.HasColumnName("start_month")
			.IsRequired()
			.HasComment("Mese di inizio.");

		builder.Property(x => x.StartDay)
			.HasColumnName("start_day")
			.IsRequired()
			.HasComment("Giorno di inizio.");

		builder.Property(x => x.EndMonth)
			.HasColumnName("end_month")
			.IsRequired()
			.HasComment("Mese di fine.");

		builder.Property(x => x.EndDay)
			.HasColumnName("end_day")
			.IsRequired()
			.HasComment("Giorno di fine (può essere prima dell'inizio: il periodo scavalca il capodanno).");

		builder.Property(x => x.Tier)
			.HasColumnName("tier")
			.IsRequired()
			.HasColumnType("catalog.revenue_season_tier")
			.HasComment("Bassa o alta stagione.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Ordine di visualizzazione.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");
	}
}
