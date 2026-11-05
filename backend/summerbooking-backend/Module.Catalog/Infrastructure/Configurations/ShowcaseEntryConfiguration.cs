using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ShowcaseEntry"/> su catalog.showcase_entries.</summary>
public sealed class ShowcaseEntryConfiguration : IEntityTypeConfiguration<ShowcaseEntry>
{
	public void Configure(EntityTypeBuilder<ShowcaseEntry> builder)
	{
		builder.ToTable("showcase_entries", "catalog", t => t.HasComment(
			"Strutture nelle sezioni della vetrina («In evidenza», «Più frequentati»), scelte e ordinate dal systemadmin."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_showcase_entries");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.Section)
			.HasColumnName("section")
			.IsRequired()
			.HasColumnType("catalog.showcase_section")
			.HasComment("Sezione della vetrina.");

		builder.Property(x => x.BathhouseId)
			.HasColumnName("bathhouse_id")
			.IsRequired()
			.HasComment("Struttura.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasComment("Posizione nella sezione.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.Section, x.BathhouseId }, "uq_catalog_showcase_entries_bathhouse")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_showcase_entries_bathhouse");

		builder.HasOne(x => x.Bathhouse)
			.WithMany()
			.HasForeignKey(x => x.BathhouseId)
			.HasConstraintName("fk_catalog_showcase_entries_bathhouse")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
