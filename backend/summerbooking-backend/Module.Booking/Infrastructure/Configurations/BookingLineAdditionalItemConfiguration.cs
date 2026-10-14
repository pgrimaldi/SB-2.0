using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingLineAdditionalItemConfiguration : IEntityTypeConfiguration<BookingLineAdditionalItem>
{
	public void Configure(EntityTypeBuilder<BookingLineAdditionalItem> builder)
	{
		builder.ToTable("line_additional_items", "booking", t =>
		{
			t.HasComment("Articolo aggiuntivo (lettino extra, cabina, tassa di soggiorno, ...) acquistato su una riga o sull'intera prenotazione.");
			t.HasCheckConstraint("ck_booking_line_additional_items_quantity", "quantity > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_booking_line_additional_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione a cui appartiene l'articolo.");

		builder.Property(x => x.BookingLineId)
			.HasColumnName("booking_line_id")
			.HasComment("Riga postazione a cui l'articolo è agganciato; NULL = articolo sulla prenotazione nel complesso.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema.");

		builder.Property(x => x.ItemSource)
			.HasColumnName("item_source")
			.HasColumnType("booking.additional_item_source")
			.IsRequired()
			.HasComment("Origine dell'anagrafica dell'articolo: voce di listino o articolo di magazzino.");

		builder.Property(x => x.CatalogItemId)
			.HasColumnName("catalog_item_id")
			.HasComment("public_id di catalog.pricing_price_lists o catalog.inventory_items (secondo item_source), copiato — NULL per articolo personalizzato non a catalogo.");

		builder.Property(x => x.NameSnapshot)
			.HasColumnName("name_snapshot")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Nome dell'articolo congelato al momento della prenotazione.");

		builder.Property(x => x.DateScope)
			.HasColumnName("date_scope")
			.HasColumnType("booking.additional_item_date_scope")
			.IsRequired()
			.HasDefaultValueSql("'whole_stay'")
			.HasComment("Copertura temporale: tutta la durata della riga o solo date puntuali.");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasColumnType("smallint")
			.IsRequired()
			.HasComment("Unità acquistate (per giorno se specific_dates, per l'intera durata se whole_stay).");

		builder.Property(x => x.UnitPrice)
			.HasColumnName("unit_price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Prezzo unitario bloccato dell'articolo.");

		builder.Property(x => x.LineTotal)
			.HasColumnName("line_total")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Totale bloccato dell'articolo (quantità × prezzo unitario, eventualmente × giorni); concorre al subtotal della testata.");


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


		builder.HasIndex(x => x.BookingLineId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_line_additional_items_line");

		builder.HasIndex(x => x.ReservationId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_line_additional_items_reservation");

		builder.HasIndex(x => x.CatalogItemId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_line_additional_items_catalog_item");

		builder.HasOne(x => x.Reservation)
			.WithMany(r => r.AdditionalItems)
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_line_additional_items_reservation")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.BookingLine)
			.WithMany(l => l.AdditionalItems)
			.HasForeignKey(x => x.BookingLineId)
			.HasConstraintName("fk_booking_line_additional_items_line")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
