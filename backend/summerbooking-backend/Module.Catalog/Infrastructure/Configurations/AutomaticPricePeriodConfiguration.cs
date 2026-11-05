using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="AutomaticPricePeriod"/> su catalog.automatic_price_periods.</summary>
public sealed class AutomaticPricePeriodConfiguration : IEntityTypeConfiguration<AutomaticPricePeriod>
{
	public void Configure(EntityTypeBuilder<AutomaticPricePeriod> builder)
	{
		builder.ToTable("automatic_price_periods", "catalog", t =>
		{
			t.HasComment("Periodi di validità di un prezzo automatico, ciascuno con i suoi giorni e le sue fasce orarie.");
			t.HasCheckConstraint("ck_catalog_automatic_price_periods_range", "starts_on IS NULL OR ends_on IS NULL OR ends_on >= starts_on");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_automatic_price_periods");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.AutomaticPriceId)
			.HasColumnName("automatic_price_id")
			.IsRequired()
			.HasComment("Prezzo automatico.");

		builder.Property(x => x.StartsOn)
			.HasColumnName("starts_on")
			.HasComment("Primo giorno; NULL = dall'inizio della stagione.");

		builder.Property(x => x.EndsOn)
			.HasColumnName("ends_on")
			.HasComment("Ultimo giorno; NULL = fino alla fine della stagione.");

		builder.Property(x => x.Weekdays)
			.HasColumnName("weekdays")
			.IsRequired()
			.HasColumnType("catalog.weekday[]")
			.HasDefaultValueSql("'{}'")
			.HasComment("Giorni di validità nel periodo; vuoto = tutti.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.AutomaticPriceId, "ix_catalog_automatic_price_periods_price")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_automatic_price_periods_price");

		builder.HasOne<AutomaticPrice>()
			.WithMany(p => p.Periods)
			.HasForeignKey(x => x.AutomaticPriceId)
			.HasConstraintName("fk_catalog_automatic_price_periods_price")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
