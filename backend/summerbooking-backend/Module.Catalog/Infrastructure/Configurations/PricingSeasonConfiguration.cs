using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="PricingSeason"/> su catalog.pricing_seasons.</summary>
public sealed class PricingSeasonConfiguration : IEntityTypeConfiguration<PricingSeason>
{
	public void Configure(EntityTypeBuilder<PricingSeason> builder)
	{
		builder.ToTable("pricing_seasons", "catalog", t =>
		{
			t.HasComment("Confini della stagione corrente (e, opzionale, della successiva) di uno stabilimento.");
			t.HasCheckConstraint("ck_catalog_pricing_seasons_season_range", "season_ends_on >= season_starts_on");
			t.HasCheckConstraint("ck_catalog_pricing_seasons_next_season_range",
				"next_season_ends_on IS NULL OR next_season_starts_on IS NULL OR next_season_ends_on >= next_season_starts_on");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_seasons");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento proprietario (relazione 1:1).");
		builder.HasOne<Property>()
			.WithOne()
			.HasForeignKey<PricingSeason>(x => x.PropertyId)
			.HasConstraintName("fk_catalog_pricing_seasons_property")
			.OnDelete(DeleteBehavior.Cascade);
		builder.HasIndex(x => x.PropertyId).IsUnique().HasDatabaseName("uq_catalog_pricing_seasons_property");

		builder.Property(x => x.SeasonStartsOn)
			.HasColumnName("season_starts_on")
			.IsRequired()
			.HasComment("Primo giorno della stagione corrente.");

		builder.Property(x => x.SeasonEndsOn)
			.HasColumnName("season_ends_on")
			.IsRequired()
			.HasComment("Ultimo giorno della stagione corrente.");

		builder.Property(x => x.NextSeasonStartsOn)
			.HasColumnName("next_season_starts_on")
			.HasComment("Primo giorno pre-impostato della stagione dell'anno successivo (rinnovo pass stagionali).");

		builder.Property(x => x.NextSeasonEndsOn)
			.HasColumnName("next_season_ends_on")
			.HasComment("Ultimo giorno pre-impostato della stagione dell'anno successivo.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete (praticamente non usato, 1:1 con la property; presente per uniformità).");

		builder.HasMany(x => x.TariffPeriods).WithOne(p => p.Season).HasForeignKey(p => p.SeasonId);
	}
}
