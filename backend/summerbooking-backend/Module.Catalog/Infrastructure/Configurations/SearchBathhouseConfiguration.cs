using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="SearchBathhouse"/> su catalog.search_bathhouses.</summary>
public sealed class SearchBathhouseConfiguration : IEntityTypeConfiguration<SearchBathhouse>
{
	public void Configure(EntityTypeBuilder<SearchBathhouse> builder)
	{
		builder.ToTable("search_bathhouses", "catalog", t => t.HasComment(
			"Read-model di ricerca pubblica: una riga per vetrina pubblicata, ricostruibile da catalog_bathhouses + catalog_pricing_* + catalog_bathhouse_amenities + identity_customer_bathhouse_likes."));

		builder.HasKey(x => x.BathhousePublicId).HasName("pk_catalog_search_bathhouses");
		builder.Property(x => x.BathhousePublicId)
			.HasColumnName("bathhouse_public_id")
			.ValueGeneratedNever()
			.HasComment("Identità della vetrina = catalog_bathhouses.public_id, copiato (no FK: read-model ricostruibile).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento proprietario, copiato — per invalidazione e raggruppamento; no FK.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Nome commerciale, per il match testuale e il sort.");

		builder.Property(x => x.City).HasColumnName("city").HasMaxLength(255).HasComment("Città, per i filtri per area.");
		builder.Property(x => x.Region).HasColumnName("region").HasMaxLength(255).HasComment("Regione, per i filtri per area.");
		builder.Property(x => x.Province).HasColumnName("province").HasMaxLength(255).HasComment("Provincia, per i filtri per area.");

		builder.Property(x => x.Slug)
			.HasColumnName("slug")
			.HasMaxLength(255)
			.HasComment("Per costruire il link alla scheda pubblica direttamente dalla card.");

		builder.Property(x => x.Latitude)
			.HasColumnName("latitude")
			.HasComment("Latitudine della vetrina, per la ricerca \"vicino a me\"/entro raggio (al posto di una colonna geography(Point,4326): nessuna dipendenza PostGIS/NetTopologySuite nella solution).");

		builder.Property(x => x.Longitude)
			.HasColumnName("longitude")
			.HasComment("Longitudine della vetrina, per la ricerca \"vicino a me\"/entro raggio.");

		builder.Property(x => x.MinPrice)
			.HasColumnName("min_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo minimo corrente prenotabile online, materializzato dal rebuild (evita la query-per-risultato del legacy).");

		builder.Property(x => x.AmenityIds)
			.HasColumnName("amenity_ids")
			.HasColumnType("smallint[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Id delle dotazioni offerte dalla vetrina, per il filtro \"con questi servizi\".");

		builder.Property(x => x.LikeCount)
			.HasColumnName("like_count")
			.HasDefaultValue(0)
			.IsRequired()
			.HasComment("Numero di \"mi piace\" della vetrina, per il sort \"più popolari\" e la card.");

		builder.Property(x => x.IsPublished)
			.HasColumnName("is_published")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("La vetrina è visibile nella ricerca pubblica.");

		builder.Property(x => x.Card)
			.HasColumnName("card")
			.HasColumnType("jsonb")
			.HasDefaultValueSql("'{}'::jsonb")
			.IsRequired()
			.HasComment("Dati non filtrabili per renderizzare la card di risultato (foto, snippet, orari, servizi principali) — JSON derivato, eccezione consentita.");

		builder.Property(x => x.ContentHash)
			.HasColumnName("content_hash")
			.HasColumnType("bytea")
			.IsRequired()
			.HasComment("Hash della riga (colonne + card), per ETag sulle risposte di ricerca.");

		builder.Property(x => x.BuiltAt)
			.HasColumnName("built_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Timestamp dell'ultimo rebuild della riga.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete per uniformità; non usato funzionalmente — la visibilità è is_published.");

		builder.HasIndex(x => new { x.Region, x.City })
			.HasFilter("is_published")
			.HasDatabaseName("ix_catalog_search_bathhouses_published_region_city");

		builder.HasIndex(x => x.Name)
			.HasMethod("gin")
			.HasOperators("gin_trgm_ops")
			.HasFilter("is_published")
			.HasDatabaseName("ix_catalog_search_bathhouses_published_name_trgm");

		builder.HasIndex(x => x.MinPrice)
			.HasFilter("is_published")
			.HasDatabaseName("ix_catalog_search_bathhouses_published_price");

		builder.HasIndex(x => x.AmenityIds)
			.HasMethod("gin")
			.HasDatabaseName("ix_catalog_search_bathhouses_amenities");

		builder.HasIndex(x => x.PropertyId)
			.HasDatabaseName("ix_catalog_search_bathhouses_property");
	}
}
