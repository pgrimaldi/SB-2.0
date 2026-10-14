using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Payment.Domain.Entities;
using Module.Payment.Domain.Enums;

namespace Module.Payment.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="GatewayCommissionSetting"/> su payment.gateway_commission_settings.</summary>
public sealed class GatewayCommissionSettingConfiguration : IEntityTypeConfiguration<GatewayCommissionSetting>
{
	public void Configure(EntityTypeBuilder<GatewayCommissionSetting> builder)
	{
		builder.ToTable("gateway_commission_settings", "payment", t => t.HasComment(
			"Tasso di commissione trattenuto dalla piattaforma sugli incassi online, per stabilimento e canale."));

		builder.HasKey(x => x.Id).HasName("pk_payment_gateway_commission_settings");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento a cui si applica la commissione.");

		builder.Property(x => x.OnlineRate)
			.HasColumnName("online_rate")
			.HasColumnType("numeric(5,2)")
			.HasDefaultValue(6.00m)
			.IsRequired()
			.HasComment("Percentuale trattenuta sulle prenotazioni concluse sul sito pubblico (fuori iframe).");

		builder.Property(x => x.IframeRate)
			.HasColumnName("iframe_rate")
			.HasColumnType("numeric(5,2)")
			.HasDefaultValue(6.00m)
			.IsRequired()
			.HasComment("Percentuale trattenuta sulle prenotazioni concluse nel widget iframe su sito terzo.");

		builder.Property(x => x.FixedFeeAmount)
			.HasColumnName("fixed_fee_amount")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0.50m)
			.IsRequired()
			.HasComment("Fee fissa in euro aggiunta alla commissione percentuale su ogni transazione online.");

		builder.Property(x => x.Exemption)
			.HasColumnName("exemption")
			.HasColumnType("payment.commission_exemption")
			.HasDefaultValueSql("'none'")
			.IsRequired()
			.HasComment("Motivo di esenzione dalla commissione, esclusivo per stabilimento.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete alla cessazione dello stabilimento.");

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasDatabaseName("uq_payment_gateway_commission_settings_property");
	}
}
