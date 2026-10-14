using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="OnlineStayLimit"/> su catalog.property_online_stay_limits.</summary>
public sealed class OnlineStayLimitConfiguration : IEntityTypeConfiguration<OnlineStayLimit>
{
	public void Configure(EntityTypeBuilder<OnlineStayLimit> builder)
	{
		builder.ToTable("property_online_stay_limits", "catalog", t =>
		{
			t.HasComment("Limite minimo/massimo di giorni consecutivi prenotabili online, per settore e finestra di date.");
			t.HasCheckConstraint("ck_catalog_property_online_stay_limits_range", "ends_on >= starts_on");
			t.HasCheckConstraint("ck_catalog_property_online_stay_limits_days", "max_days IS NULL OR min_days IS NULL OR max_days >= min_days");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_property_online_stay_limits");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SettingsId)
			.HasColumnName("settings_id")
			.IsRequired()
			.HasComment("Configurazione canale online a cui appartiene il limite.");

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.IsRequired()
			.HasComment("Settore a cui si applica il limite.");

		builder.Property(x => x.StartsOn)
			.HasColumnName("starts_on")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Primo giorno di validità del limite (incluso).");

		builder.Property(x => x.EndsOn)
			.HasColumnName("ends_on")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Ultimo giorno di validità del limite (incluso).");

		builder.Property(x => x.MinDays)
			.HasColumnName("min_days")
			.HasComment("Numero minimo di giorni consecutivi prenotabili online per il settore in quella finestra; NULL = nessun minimo.");

		builder.Property(x => x.MaxDays)
			.HasColumnName("max_days")
			.HasComment("Numero massimo di giorni consecutivi prenotabili online per il settore in quella finestra; NULL = nessun massimo.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.SettingsId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_property_online_stay_limits_settings");

		builder.HasIndex(x => new { x.SectorId, x.StartsOn, x.EndsOn })
			.HasDatabaseName("ix_catalog_property_online_stay_limits_sector_range");

		builder.HasOne<OnlineBookingSettings>()
			.WithMany(x => x.StayLimits)
			.HasForeignKey(x => x.SettingsId)
			.HasConstraintName("fk_catalog_property_online_stay_limits_settings")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<MapSector>()
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_property_online_stay_limits_sector")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
