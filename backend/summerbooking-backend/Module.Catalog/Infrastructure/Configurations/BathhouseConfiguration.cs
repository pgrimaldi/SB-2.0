using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="Bathhouse"/> su catalog.bathhouses.</summary>
public sealed class BathhouseConfiguration : IEntityTypeConfiguration<Bathhouse>
{
	public void Configure(EntityTypeBuilder<Bathhouse> builder)
	{
		builder.ToTable("bathhouses", "catalog", t =>
		{
			t.HasComment("Vetrina pubblica dello stabilimento: nome commerciale, indirizzo, contatti, descrizione, foto, geolocalizzazione, orari, politiche.");
			t.HasCheckConstraint("ck_catalog_bathhouses_cancellation_notice", "is_free_cancellation OR cancellation_notice_hours IS NULL");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_bathhouses");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, copiato cross-modulo (C5 identity_customer_bathhouse_likes.bathhouse_id) e usato in URL/API pubbliche.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Property (tenant) proprietaria della vetrina.");

		builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).HasComment("Nome commerciale pubblico; sorgente dello slug.");
		builder.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(255).HasComment("Identificatore leggibile nell'URL pubblico, generato una sola volta alla prima pubblicazione.");
		builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text").HasComment("Descrizione libera (HTML sanificato).");
		builder.Property(x => x.Regulation).HasColumnName("regulation").HasColumnType("text").HasComment("Regolamento interno (HTML sanificato).");

		builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(255).HasComment("Indirizzo pubblico.");
		builder.Property(x => x.City).HasColumnName("city").HasMaxLength(255).HasComment("Città della vetrina.");
		builder.Property(x => x.Cap).HasColumnName("cap").HasMaxLength(16).HasComment("CAP.");
		builder.Property(x => x.Province).HasColumnName("province").HasMaxLength(255).HasComment("Provincia.");
		builder.Property(x => x.Region).HasColumnName("region").HasMaxLength(255).HasComment("Regione, usata dalla ricerca pubblica per area.");

		builder.Property(x => x.CountryId).HasColumnName("country_id").HasComment("Paese della vetrina.");

		builder.Property(x => x.Latitude).HasColumnName("latitude").HasColumnType("numeric(9,6)").HasComment("Latitudine GPS per mappa e ricerca geografica.");
		builder.Property(x => x.Longitude).HasColumnName("longitude").HasColumnType("numeric(9,6)").HasComment("Longitudine GPS per mappa e ricerca geografica.");

		builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(32).HasComment("Telefono pubblico.");
		builder.Property(x => x.Email).HasColumnName("email").HasColumnType("citext").HasComment("Email pubblica.");
		builder.Property(x => x.Website).HasColumnName("website").HasMaxLength(255).HasComment("Sito web.");
		builder.Property(x => x.Facebook).HasColumnName("facebook").HasMaxLength(255).HasComment("Link Facebook.");
		builder.Property(x => x.Instagram).HasColumnName("instagram").HasMaxLength(255).HasComment("Link Instagram.");
		builder.Property(x => x.Whatsapp).HasColumnName("whatsapp").HasMaxLength(32).HasComment("Numero WhatsApp pubblico.");

		builder.Property(x => x.OpenHoursFrom).HasColumnName("open_hours_from").HasColumnType("time").HasComment("Orario di apertura giornaliero.");
		builder.Property(x => x.OpenHoursTo).HasColumnName("open_hours_to").HasColumnType("time").HasComment("Orario di chiusura giornaliero.");

		builder.OwnsOne(x => x.CancellationPolicy, cp =>
		{
			cp.Property(p => p.IsFreeCancellation)
				.HasColumnName("is_free_cancellation")
				.IsRequired()
				.HasDefaultValue(false)
				.HasComment("Cancellazione gratuita entro un preavviso.");
			cp.Property(p => p.NoticeHours)
				.HasColumnName("cancellation_notice_hours")
				.HasComment("Ore di preavviso richieste, valorizzato solo se is_free_cancellation = true.");
		});
		builder.Navigation(x => x.CancellationPolicy).IsRequired();

		builder.Property(x => x.PetsPolicy)
			.HasColumnName("pets_policy")
			.HasColumnType("catalog.pets_policy")
			.HasDefaultValueSql("'not_allowed'")
			.HasComment("Politica animali della vetrina.");

		builder.Property(x => x.MainPhotoId).HasColumnName("main_photo_id").HasComment("Foto di copertina (kind = photo).");
		builder.Property(x => x.MenuPdfId).HasColumnName("menu_pdf_id").HasComment("PDF menu bar (kind = pdf).");
		builder.Property(x => x.PriceListPdfId).HasColumnName("price_list_pdf_id").HasComment("PDF listino cartaceo (kind = pdf).");

		builder.Property(x => x.PublishedAt)
			.HasColumnName("published_at")
			.HasComment("Quando la vetrina è stata resa pubblica per la prima volta; NULL = bozza/non pubblicata.");

		builder.Property(x => x.UpdatedByUserPublicId)
			.HasColumnName("updated_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Account che ha salvato per ultimo la vetrina.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete della vetrina.");

		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_bathhouses_public_id");
		builder.HasIndex(x => x.Slug).IsUnique().HasFilter("slug IS NOT NULL").HasDatabaseName("uq_catalog_bathhouses_slug");
		builder.HasIndex(x => x.PropertyId).HasDatabaseName("ix_catalog_bathhouses_property");
		builder.HasIndex(x => x.PublishedAt)
			.HasFilter("published_at IS NOT NULL AND deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_bathhouses_published");
		builder.HasIndex(x => new { x.Latitude, x.Longitude }).HasDatabaseName("ix_catalog_bathhouses_geo");
		builder.HasIndex(x => new { x.Region, x.City }).HasDatabaseName("ix_catalog_bathhouses_region");

		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_bathhouses_property")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.Country)
			.WithMany()
			.HasForeignKey(x => x.CountryId)
			.HasConstraintName("fk_catalog_bathhouses_country")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.MainPhoto).WithMany().HasForeignKey(x => x.MainPhotoId).HasConstraintName("fk_catalog_bathhouses_main_photo");
		builder.HasOne(x => x.MenuPdf).WithMany().HasForeignKey(x => x.MenuPdfId).HasConstraintName("fk_catalog_bathhouses_menu_pdf");
		builder.HasOne(x => x.PriceListPdf).WithMany().HasForeignKey(x => x.PriceListPdfId).HasConstraintName("fk_catalog_bathhouses_price_list_pdf");
	}
}
