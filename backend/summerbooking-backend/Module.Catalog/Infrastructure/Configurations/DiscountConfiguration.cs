using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="Discount"/> su catalog.pricing_discounts.</summary>
public sealed class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
	public void Configure(EntityTypeBuilder<Discount> builder)
	{
		builder.ToTable("pricing_discounts", "catalog", t =>
		{
			t.HasComment("Voucher/codice sconto configurato da uno stabilimento.");
			t.HasCheckConstraint("ck_catalog_pricing_discounts_value", "value >= 0");
			t.HasCheckConstraint("ck_catalog_pricing_discounts_max_uses", "max_uses IS NULL OR max_uses > 0");
			t.HasCheckConstraint("ck_catalog_pricing_discounts_used_count", "max_uses IS NULL OR used_count <= max_uses");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_discounts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico, copiato negli snapshot sconto di C1 (booking_discount, discountData).");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_pricing_discounts_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento proprietario del voucher.");
		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_pricing_discounts_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasMaxLength(60)
			.IsRequired()
			.HasComment("Codice che il cliente digita per riscattare lo sconto.");

		// Colonna generata per l'unicità case-insensitive per property (upper(code)),
		// il legacy non la garantiva.
		builder.Property<string>("CodeNormalized")
			.HasColumnName("code_normalized")
			.HasMaxLength(60)
			.HasComputedColumnSql("upper(code)", stored: true)
			.HasComment("Proiezione upper(code) per l'indice univoco case-insensitive.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(120)
			.HasComment("Nome descrittivo del voucher, uso interno/gestore.");

		builder.Property(x => x.Type)
			.HasColumnName("type")
			.HasColumnType("catalog.discount_type")
			.IsRequired()
			.HasComment("Tipo di sconto: percentuale o importo fisso.");

		builder.Property(x => x.Value)
			.HasColumnName("value")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Entità dello sconto: 0-100 per percentage, euro per fixed.");

		builder.Property(x => x.Scope)
			.HasColumnName("scope")
			.HasColumnType("catalog.discount_scope")
			.HasDefaultValueSql("'all'")
			.IsRequired()
			.HasComment("Ambito di applicabilità del voucher (all / custom / without_custom).");

		builder.Property(x => x.AppliesToServiceCategories)
			.HasColumnName("applies_to_service_categories")
			.HasColumnType("catalog.discount_service_category[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Categorie di servizio a cui è limitato il voucher (scope = custom); vuoto = nessuna restrizione.");

		builder.Property(x => x.ApplicableChannel)
			.HasColumnName("applicable_channel")
			.HasColumnType("catalog.discount_applicable_channel")
			.HasDefaultValueSql("'manager_and_online'")
			.IsRequired()
			.HasComment("Canale/i di prenotazione su cui il voucher è valido.");

		builder.Property(x => x.MaxUses)
			.HasColumnName("max_uses")
			.HasComment("Numero massimo di utilizzi consentiti, aggregato (NULL = illimitato).");

		builder.Property(x => x.UsedCount)
			.HasColumnName("used_count")
			.HasDefaultValue(0)
			.IsRequired()
			.HasComment("Utilizzi già effettuati, incrementato atomicamente da C1 alla conferma prenotazione.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Voucher attivo/riscattabile.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access_users, copiato — nessuna FK cross-schema.");

		// bug reale osservato il 2026-09-16: created_at/updated_at non avevano default now() a
		// schema (nessuna convenzione EF globale lo applica, verificato in ModuleDbContextConventions
		// - solo converter UTC e query filter soft-delete) - un ETL che omette queste colonne
		// dall'INSERT (comportamento normale, per lasciare scattare il default) falliva NOT NULL.
		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga.");


		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete del voucher.");

		builder.HasIndex("PropertyId", "CodeNormalized")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_discounts_property_code");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_discounts_property_active");

		builder.HasMany(x => x.ValidityWindows).WithOne().HasForeignKey(w => w.DiscountId);
	}
}
