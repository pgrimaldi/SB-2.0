using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="OnlineAllowedPeriod"/> su catalog.property_online_allowed_periods.</summary>
public sealed class OnlineAllowedPeriodConfiguration : IEntityTypeConfiguration<OnlineAllowedPeriod>
{
	public void Configure(EntityTypeBuilder<OnlineAllowedPeriod> builder)
	{
		builder.ToTable("property_online_allowed_periods", "catalog", t =>
		{
			t.HasComment("Finestra di date in cui è consentita la prenotazione online quando is_period_restricted è true.");
			t.HasCheckConstraint("ck_catalog_property_online_allowed_periods_range", "ends_on >= starts_on");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_property_online_allowed_periods");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SettingsId)
			.HasColumnName("settings_id")
			.IsRequired()
			.HasComment("Configurazione canale online a cui appartiene la finestra.");

		builder.Property(x => x.StartsOn)
			.HasColumnName("starts_on")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Primo giorno della finestra (incluso).");

		builder.Property(x => x.EndsOn)
			.HasColumnName("ends_on")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Ultimo giorno della finestra (incluso).");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.SettingsId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_property_online_allowed_periods_settings");

		builder.HasIndex(x => new { x.SettingsId, x.StartsOn, x.EndsOn })
			.HasDatabaseName("ix_catalog_property_online_allowed_periods_range");

		builder.HasOne<OnlineBookingSettings>()
			.WithMany(x => x.AllowedPeriods)
			.HasForeignKey(x => x.SettingsId)
			.HasConstraintName("fk_catalog_property_online_allowed_periods_settings")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
