using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="AgreementPartner"/> su catalog.agreement_partners.</summary>
public sealed class AgreementPartnerConfiguration : IEntityTypeConfiguration<AgreementPartner>
{
	public void Configure(EntityTypeBuilder<AgreementPartner> builder)
	{
		builder.ToTable("agreement_partners", "catalog", t => t.HasComment(
			"Struttura partner (\"convenzione\"/hotel) con cui uno stabilimento ha un accordo commerciale."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_agreement_partners");
		builder.Property(x => x.Id).HasColumnType("integer").UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico (UUIDv7), copiato cross-modulo da C1 (booking_reservations.agreement_partner_id).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento che ha registrato la struttura partner (risolto sempre server-side, mai dal body).");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Nome della struttura partner.");

		builder.Property(x => x.City).HasColumnName("city").HasMaxLength(255)
			.HasComment("Città della struttura partner.");
		builder.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(20)
			.HasComment("CAP della struttura partner.");
		builder.Property(x => x.Province).HasColumnName("province").HasMaxLength(255)
			.HasComment("Provincia della struttura partner (testo libero, dati storicamente sporchi).");
		builder.Property(x => x.StreetName).HasColumnName("street_name").HasMaxLength(255)
			.HasComment("Via della struttura partner.");
		builder.Property(x => x.StreetNumber).HasColumnName("street_number").HasMaxLength(20)
			.HasComment("Numero civico della struttura partner.");
		builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50)
			.HasComment("Telefono di contatto della struttura partner.");
		builder.Property(x => x.Email).HasColumnName("email").HasColumnType("citext")
			.HasComment("Email di contatto della struttura partner.");
		builder.Property(x => x.Note).HasColumnName("note").HasColumnType("text")
			.HasComment("Annotazioni libere sulla struttura partner.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete; propaga a cascata (applicativa) ai listini/voucher figli.");

		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_agreement_partners_property")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasMany(x => x.PriceLists)
			.WithOne(pl => pl.Partner)
			.HasForeignKey(pl => pl.PartnerId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_catalog_agreement_partners_public_id");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_partners_property");

		// Indice GIN trigram su lower(name) per ricerca per nome (getHotelList): EF Core non genera
		// espressioni funzionali nell'IndexBuilder fluente — HasMethod/HasOperators fissano motore e
		// classe operatore, l'espressione lower(name) va rifinita a mano nella migration generata.
		builder.HasIndex(x => x.Name)
			.HasMethod("gin")
			.HasOperators("gin_trgm_ops")
			.HasDatabaseName("ix_catalog_agreement_partners_name_trgm");
	}
}
