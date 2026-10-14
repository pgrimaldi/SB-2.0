using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="Property"/> su catalog.properties.</summary>
public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
	public void Configure(EntityTypeBuilder<Property> builder)
	{
		builder.ToTable("properties", "catalog", t => t.HasComment(
			"Anagrafica gestionale/di piattaforma dello stabilimento (tenant): stato di approvazione, contatti amministrativi, titolare."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_properties");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile (UUIDv7) per URL di back-office / endpoint admin; non è il discriminatore tenant.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Nome amministrativo dello stabilimento (il nome commerciale pubblico vive su catalog.bathhouses).");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("catalog.property_status")
			.IsRequired()
			.HasDefaultValueSql("'pending'")
			.HasComment("Stato del ciclo di vita amministrativo dello stabilimento.");

		builder.Property(x => x.AdminComment)
			.HasColumnName("admin_comment")
			.HasColumnType("text")
			.HasComment("Nota del systemadmin all'approvazione/rifiuto/cancellazione, mostrata anche al titolare nelle email automatiche.");

		builder.Property(x => x.OwnerUserPublicId)
			.HasColumnName("owner_user_public_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Titolare storico della registrazione.");

		builder.Property(x => x.ContactFullName)
			.HasColumnName("contact_full_name")
			.HasMaxLength(255)
			.HasComment("Nome e cognome della persona di riferimento (contatto interno).");

		builder.Property(x => x.ContactEmail)
			.HasColumnName("contact_email")
			.HasColumnType("citext")
			.HasComment("Email di contatto amministrativo; destinataria delle comunicazioni di benvenuto/approvazione/rifiuto/cancellazione.");

		builder.Property(x => x.ContactPhone)
			.HasColumnName("contact_phone")
			.HasMaxLength(32)
			.HasComment("Telefono di contatto amministrativo.");

		builder.Property(x => x.ContactPhoneCountryCode)
			.HasColumnName("contact_phone_country_code")
			.HasMaxLength(8)
			.HasComment("Prefisso internazionale del telefono di contatto.");

		builder.Property(x => x.City)
			.HasColumnName("city")
			.HasMaxLength(255)
			.HasComment("Città indicata in registrazione (dato amministrativo; la città pubblica è su catalog.bathhouses).");

		builder.Property(x => x.CountryId)
			.HasColumnName("country_id")
			.HasComment("Paese dello stabilimento.");

		builder.Property(x => x.IsOnlineSalesSuspended)
			.HasColumnName("is_online_sales_suspended")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Kill-switch di piattaforma che sospende le vendite online dello stabilimento (es. abbonamento scaduto), a prescindere dalle impostazioni del gestore.");

		builder.Property(x => x.ContractDocumentId)
			.HasColumnName("contract_document_id")
			.HasComment("public_id di identity.account_contract_documents, copiato — nessuna FK cross-schema. NULL = feature contratto B2B disattivata per la property.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete della property (evento raro e ad alto impatto: trascina vetrine, listini, mappe).");

		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_properties_public_id");

		builder.HasIndex(x => x.Status)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_properties_status");

		builder.HasIndex(x => x.OwnerUserPublicId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_properties_owner");

		builder.HasIndex(x => x.CountryId)
			.HasDatabaseName("ix_catalog_properties_country");

		// Indice trigram gin_trgm_ops su lower(name): non esprimibile in Fluent API, aggiunto a mano
		// nella migration AddMissingDesignedIndexes — richiede l'estensione
		// pg_trgm. Fino al 2026-10-06 quel passo a mano non era mai stato fatto.

		builder.HasOne(x => x.Country)
			.WithMany()
			.HasForeignKey(x => x.CountryId)
			.HasConstraintName("fk_catalog_properties_country")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
