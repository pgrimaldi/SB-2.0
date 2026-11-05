using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ShowcaseBadge"/> su catalog.showcase_badges.</summary>
public sealed class ShowcaseBadgeConfiguration : IEntityTypeConfiguration<ShowcaseBadge>
{
	public void Configure(EntityTypeBuilder<ShowcaseBadge> builder)
	{
		builder.ToTable("showcase_badges", "catalog", t =>
		{
			t.HasComment("Badge della vetrina creati dal systemadmin.");
			t.HasCheckConstraint("ck_catalog_showcase_badges_color", "color IS NULL OR color ~ '^#[0-9A-F]{6}$'");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_showcase_badges");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico (UUIDv7) usato dalle API.");

		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_showcase_badges_public_id");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.IsRequired()
			.HasMaxLength(80)
			.HasComment("Nome in italiano.");

		builder.Property(x => x.Color)
			.HasColumnName("color")
			.HasMaxLength(7)
			.HasComment("Colore esadecimale #RRGGBB.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Ordine di visualizzazione.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");
	}
}
