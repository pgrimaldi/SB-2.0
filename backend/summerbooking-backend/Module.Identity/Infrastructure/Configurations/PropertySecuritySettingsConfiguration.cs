using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Identity.Domain.Entities;

namespace Module.Identity.Infrastructure.Configurations;

public sealed class PropertySecuritySettingsConfiguration : IEntityTypeConfiguration<PropertySecuritySettings>
{
	public void Configure(EntityTypeBuilder<PropertySecuritySettings> builder)
	{
		builder.ToTable("access_property_security_settings", "identity", t => t.HasComment(
			"Impostazioni di sicurezza applicate a tutto lo staff di uno stabilimento (property)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_access_property_security_settings");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id della property (catalog) a cui si applicano le impostazioni, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.IsTwoFactorEnabled)
			.HasColumnName("is_two_factor_enabled")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se true, richiede un secondo fattore al login dello staff di questo stabilimento.");

		builder.Property(x => x.TwoFactorChannel)
			.HasColumnName("two_factor_channel")
			.HasColumnType("identity.second_factor_channel")
			.IsRequired()
			.HasDefaultValueSql("'email'")
			.HasComment("Canale del secondo fattore.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune.

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasDatabaseName("uq_identity_access_property_security_settings_property");
	}
}
