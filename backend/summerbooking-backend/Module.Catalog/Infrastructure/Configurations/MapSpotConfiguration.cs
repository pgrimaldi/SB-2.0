using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="MapSpot"/> su catalog.map_spots.</summary>
public sealed class MapSpotConfiguration : IEntityTypeConfiguration<MapSpot>
{
	public void Configure(EntityTypeBuilder<MapSpot> builder)
	{
		builder.ToTable("map_spots", "catalog", t =>
		{
			t.HasComment("Elementi del reticolo della mappa: postazioni prenotabili (ombrelloni, lettini, cabine, aree, ...) ed elementi spiaggia solo grafici. Sostituisce il blob JSON configurations.beachMapData.");
			t.HasCheckConstraint("ck_catalog_map_spots_rotation", "rotation_degrees >= 0 AND rotation_degrees < 360 AND rotation_degrees % 45 = 0");
			t.HasCheckConstraint("ck_catalog_map_spots_offset", "offset_x BETWEEN -2 AND 2 AND offset_y BETWEEN -2 AND 2");
			t.HasCheckConstraint("ck_catalog_map_spots_span", "row_span >= 1 AND column_span >= 1");
			t.HasCheckConstraint("ck_catalog_map_spots_beach_element",
				"(spot_type::text = 'beach_element') = (beach_element_id IS NOT NULL) AND (spot_type::text = 'beach_element' OR label IS NOT NULL)");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_spots");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, copiato cross-modulo (Booking resource_id, snapshot mappa).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento proprietario della postazione.");

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.HasComment("Settore a cui appartiene la postazione.");

		builder.Property(x => x.Label)
			.HasColumnName("label")
			.HasMaxLength(MapSpot.LabelMaxLength)
			.HasComment("Etichetta/numero della postazione visibile al cliente e all'operatore (es. A12); univoca per stabilimento. NULL solo per gli elementi spiaggia.");

		builder.Property(x => x.SpotType)
			.HasColumnName("spot_type")
			.HasColumnType("catalog.spot_type")
			.IsRequired()
			.HasComment("Tipo di elemento posizionato nella cella.");

		builder.Property(x => x.IsArea)
			.HasColumnName("is_area")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("True se la postazione rappresenta un'area con più posti, non una singola postazione.");

		builder.Property(x => x.AreaSeatCount)
			.HasColumnName("area_seat_count")
			.HasComment("Numero di posti dell'area, rilevante solo se is_area = true.");

		builder.Property(x => x.GridRow)
			.HasColumnName("grid_row")
			.HasComment("Riga della postazione nella griglia del layout.");
		builder.Property(x => x.GridColumn)
			.HasColumnName("grid_column")
			.HasComment("Colonna della postazione nella griglia del layout.");

		builder.Property(x => x.RowSpan)
			.HasColumnName("row_span")
			.IsRequired()
			.HasDefaultValue((short)1)
			.HasComment("Righe occupate dall'elemento (più di una solo per gli elementi spiaggia).");
		builder.Property(x => x.ColumnSpan)
			.HasColumnName("column_span")
			.IsRequired()
			.HasDefaultValue((short)1)
			.HasComment("Colonne occupate dall'elemento (più di una solo per gli elementi spiaggia).");

		builder.Property(x => x.RotationDegrees)
			.HasColumnName("rotation_degrees")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Rotazione dell'articolo sul proprio asse, a passi di 45 gradi.");
		builder.Property(x => x.OffsetX)
			.HasColumnName("offset_x")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Spostamento orizzontale dentro la casella, in quinti di casella (da -2 a 2; 0 = centro).");
		builder.Property(x => x.OffsetY)
			.HasColumnName("offset_y")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Spostamento verticale dentro la casella, in quinti di casella (da -2 a 2; 0 = centro).");

		builder.Property(x => x.IsAgreementOnly)
			.HasColumnName("is_agreement_only")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Riservata alle strutture convenzionate: non prenotabile online.");

		builder.Property(x => x.BeachElementId)
			.HasColumnName("beach_element_id")
			.HasComment("Elemento spiaggia del catalogo di piattaforma; valorizzato solo per spot_type = beach_element.");

		builder.HasOne(x => x.BeachElement)
			.WithMany()
			.HasForeignKey(x => x.BeachElementId)
			.HasConstraintName("fk_catalog_map_spots_beach_element")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.IsBookable)
			.HasColumnName("is_bookable")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("La postazione è offerta alla prenotazione (esclude celle vuote/decorative della griglia).");

		builder.Property(x => x.CreatedByUserPublicId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha configurato la mappa.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete: la postazione non si cancella fisicamente se ha prenotazioni storiche in Booking.");

		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_map_spots_public_id");

		builder.HasIndex(x => new { x.PropertyId, x.Label })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_map_spots_property_label");

		builder.HasIndex(x => new { x.PropertyId, x.SpotType })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_map_spots_property_type");

		builder.HasIndex(x => x.BeachElementId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_map_spots_beach_element");

		builder.HasIndex(x => x.SectorId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_map_spots_sector");


		builder.HasIndex(x => new { x.PropertyId, x.GridRow, x.GridColumn })
			.HasDatabaseName("ix_catalog_map_spots_grid");

		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_map_spots_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Sector)
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_map_spots_sector")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
