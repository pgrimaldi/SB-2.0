using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="Country"/> su catalog.countries.</summary>
public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
	public void Configure(EntityTypeBuilder<Country> builder)
	{
		builder.ToTable("countries", "catalog", t => t.HasComment(
			"Elenco (non esaustivo) delle nazioni selezionabili nei form; lookup di dominio referenziata dai moduli tramite il code ISO copiato."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_countries");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasColumnType("char(2)")
			.IsRequired()
			.HasComment("Codice ISO 3166-1 alpha-2 (IT, ES, HR, ...), valore copiato dai moduli che referenziano un paese — mai FK cross-schema.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Paese selezionabile nei form.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Ordine nelle tendine (es. Italia in cima).");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete: un paese referenziato da property/bathhouse/cliente non si cancella fisicamente.");

		builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_catalog_countries_code");
	}
}
