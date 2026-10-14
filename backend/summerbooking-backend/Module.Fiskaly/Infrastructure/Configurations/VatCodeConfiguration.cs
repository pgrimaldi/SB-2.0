using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Fiskaly.Domain.Entities;

namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class VatCodeConfiguration : IEntityTypeConfiguration<VatCode>
{
	public void Configure(EntityTypeBuilder<VatCode> builder)
	{
		builder.ToTable("vat_codes", "fiskaly", t => t.HasComment(
			"Catalogo dei codici IVA e di esenzione ammessi da Fiskaly SIGN IT per le righe del documento commerciale."));

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_vat_codes");
		builder.Property(x => x.Id)
			.HasColumnType("smallint")
			.UseIdentityAlwaysColumn();

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasMaxLength(20)
			.IsRequired()
			.HasComment("Codice SIGN IT, chiave di business inviata a Fiskaly verbatim; chiave delle risorse di localizzazione.");
		builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_fiskaly_vat_codes_code");

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("fiskaly.vat_kind")
			.IsRequired()
			.HasComment("Se il codice è un'aliquota (VatRate) o un caso di esenzione (VatExemption).");

		builder.Property(x => x.Rate)
			.HasColumnName("rate")
			.HasColumnType("numeric(5,2)")
			.HasComment("Aliquota IVA in percentuale; NULL per i codici di esenzione.");

		builder.Property(x => x.NatureCode)
			.HasColumnName("nature_code")
			.HasMaxLength(4)
			.HasComment("Codice Natura (N1-N6) per fatturazione elettronica / SdI; NULL per le aliquote.");

		builder.ToTable(t => t.HasCheckConstraint(
			"ck_fiskaly_vat_codes_kind_coherence",
			"(kind = 'vat_rate' AND rate IS NOT NULL AND nature_code IS NULL) OR (kind = 'vat_exemption' AND nature_code IS NOT NULL AND rate IS NULL)"));

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Se il codice è proponibile nelle UI; i codici storici restano per gli scontrini già emessi con is_active=false.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.HasColumnType("smallint")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Ordinamento nelle select di aliquote/esenzioni.");
		builder.HasIndex(x => new { x.Kind, x.SortOrder })
			.HasFilter("is_active")
			.HasDatabaseName("ix_fiskaly_vat_codes_active");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga di lookup.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga di lookup.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Esposta per uniformità ma non usata: un codice IVA non si cancella, si disattiva con is_active.");
	}
}
