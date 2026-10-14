using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Identity.Domain.Entities;

namespace Module.Identity.Infrastructure.Configurations;

public sealed class LicensePlanTranslationConfiguration : IEntityTypeConfiguration<LicensePlanTranslation>
{
	public void Configure(EntityTypeBuilder<LicensePlanTranslation> builder)
	{
		builder.ToTable("account_license_plan_translations", "identity", t => t.HasComment(
			"Etichette multilingua (nome commerciale + descrizione) dei piani di abbonamento."));

		builder.HasKey(x => x.Id).HasName("pk_identity_account_license_plan_translations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.LicensePlanId)
			.HasColumnName("license_plan_id")
			.IsRequired()
			.HasComment("Piano a cui la traduzione appartiene.");

		builder.Property(x => x.Culture)
			.HasColumnName("culture")
			.HasMaxLength(16)
			.IsRequired()
			.HasComment("Lingua (o lingua+regione) della traduzione, tag BCP-47.");

		builder.Property(x => x.Label)
			.HasColumnName("label")
			.HasMaxLength(80)
			.IsRequired()
			.HasComment("Nome commerciale del piano in questa lingua.");

		builder.Property(x => x.Description)
			.HasColumnName("description")
			.HasComment("Testo descrittivo/commerciale del piano in questa lingua.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune.

		builder.HasIndex(x => new { x.LicensePlanId, x.Culture })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_identity_account_license_plan_translations_plan_culture");

		builder.HasIndex(x => x.LicensePlanId)
			.HasDatabaseName("ix_identity_account_license_plan_translations_plan");

		builder.HasOne(x => x.LicensePlan)
			.WithMany(p => p.Translations)
			.HasForeignKey(x => x.LicensePlanId)
			.HasConstraintName("fk_identity_account_license_plan_translations_plan")
			.OnDelete(DeleteBehavior.Cascade);

		// Una riga per piano e lingua gestita: valori iniziali, poi li gestisce il systemadmin da back-office.
		// CreatedAt/UpdatedAt esplicite: HasData non applica i default DB (now()) alle righe seed.
		var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		builder.HasData(
			new { Id = 1L, LicensePlanId = 1L, Culture = "it", Label = "Base", CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 2L, LicensePlanId = 1L, Culture = "en", Label = "Basic", CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 3L, LicensePlanId = 2L, Culture = "it", Label = "Avanzato", CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 4L, LicensePlanId = 2L, Culture = "en", Label = "Advanced", CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 5L, LicensePlanId = 1L, Culture = "fr", Label = "Essentiel", CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 6L, LicensePlanId = 2L, Culture = "fr", Label = "Avancé", CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 7L, LicensePlanId = 1L, Culture = "es", Label = "Básico", CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 8L, LicensePlanId = 2L, Culture = "es", Label = "Avanzado", CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 9L, LicensePlanId = 1L, Culture = "de", Label = "Basis", CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 10L, LicensePlanId = 2L, Culture = "de", Label = "Erweitert", CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 11L, LicensePlanId = 1L, Culture = "zh", Label = "基础版", CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 12L, LicensePlanId = 2L, Culture = "zh", Label = "高级版", CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 13L, LicensePlanId = 1L, Culture = "ar", Label = "الأساسية", CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 14L, LicensePlanId = 2L, Culture = "ar", Label = "المتقدمة", CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp });
	}
}
