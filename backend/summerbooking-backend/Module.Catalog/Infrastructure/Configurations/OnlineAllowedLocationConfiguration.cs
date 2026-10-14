using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="OnlineAllowedLocation"/> su catalog.property_online_allowed_locations.</summary>
public sealed class OnlineAllowedLocationConfiguration : IEntityTypeConfiguration<OnlineAllowedLocation>
{
	public void Configure(EntityTypeBuilder<OnlineAllowedLocation> builder)
	{
		builder.ToTable("property_online_allowed_locations", "catalog", t => t.HasComment(
			"Postazione ammessa alla prenotazione online quando is_location_restricted è true."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_property_online_allowed_locations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SettingsId)
			.HasColumnName("settings_id")
			.IsRequired()
			.HasComment("Configurazione canale online a cui appartiene la restrizione.");

		builder.Property(x => x.SpotId)
			.HasColumnName("spot_id")
			.IsRequired()
			.HasComment("La postazione ammessa alla prenotazione online.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.SettingsId, x.SpotId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_property_online_allowed_locations_settings_spot");

		builder.HasIndex(x => x.SettingsId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_property_online_allowed_locations_settings");

		builder.HasIndex(x => x.SpotId)
			.HasDatabaseName("ix_catalog_property_online_allowed_locations_spot");

		builder.HasOne<OnlineBookingSettings>()
			.WithMany(x => x.AllowedLocations)
			.HasForeignKey(x => x.SettingsId)
			.HasConstraintName("fk_catalog_property_online_allowed_locations_settings")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<MapSpot>()
			.WithMany()
			.HasForeignKey(x => x.SpotId)
			.HasConstraintName("fk_catalog_property_online_allowed_locations_spot")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
