using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Identity.Domain.Entities;

namespace Module.Identity.Infrastructure.Configurations;

public sealed class LicensePlanConfiguration : IEntityTypeConfiguration<LicensePlan>
{
	public void Configure(EntityTypeBuilder<LicensePlan> builder)
	{
		builder.ToTable("account_license_plans", "identity", t => t.HasComment(
			"Catalogo dei piani di abbonamento della piattaforma (lookup gestibile da pannello SuperAdmin)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_account_license_plans");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();
		// seed id 1–2 forzati via HasData, poi ALTER SEQUENCE ... RESTART in migration.

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasMaxLength(40)
			.IsRequired()
			.HasComment("Codice tecnico del piano (basic, advanced), non tradotto.");

		builder.Property(x => x.DurationMonths)
			.HasColumnName("duration_months")
			.IsRequired()
			.HasComment("Durata suggerita del piano in mesi; default di comodo per precompilare le date del periodo, mai fonte di verità di ends_at.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Se true, il piano è proponibile per nuove sottoscrizioni.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Ordinamento di presentazione.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune.

		builder.HasIndex(x => x.Code)
			.IsUnique()
			.HasDatabaseName("uq_identity_account_license_plans_code");

		// CreatedAt/UpdatedAt esplicite: HasData non applica i default DB (now()) alle righe seed.
		var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		builder.HasData(
			new { Id = 1L, Code = "basic", DurationMonths = (short)3, IsActive = true, SortOrder = (short)0, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 2L, Code = "advanced", DurationMonths = (short)12, IsActive = true, SortOrder = (short)10, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp });
	}
}
