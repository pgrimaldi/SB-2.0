using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingServiceLineConfiguration : IEntityTypeConfiguration<BookingServiceLine>
{
	public void Configure(EntityTypeBuilder<BookingServiceLine> builder)
	{
		builder.ToTable("service_lines", "booking", t =>
		{
			t.HasComment("Riga d'ordine per un servizio (bar/normale/orario) venduto su una prenotazione, agganciata o meno a una postazione.");
			t.HasCheckConstraint("ck_booking_service_lines_quantity", "quantity > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_booking_service_lines");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico della riga servizio, copiato cross-modulo da C4 (line item) e C3 (riga economica).");

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione a cui il servizio appartiene.");

		builder.Property(x => x.BookingLineId)
			.HasColumnName("booking_line_id")
			.HasComment("Riga postazione a cui il servizio è agganciato; NULL = servizio sulla prenotazione nel complesso.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("booking.service_category")
			.IsRequired()
			.HasComment("Categoria del servizio venduto: bar, normale o orario.");

		builder.Property(x => x.SaleItemId)
			.HasColumnName("sale_item_id")
			.HasComment("public_id di catalog.service_sale_items, copiato — NULL per servizio ad hoc non a catalogo.");

		builder.Property(x => x.NameSnapshot)
			.HasColumnName("name_snapshot")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Nome del servizio congelato al momento della vendita.");

		builder.Property(x => x.VariantLabel)
			.HasColumnName("variant_label")
			.HasMaxLength(80)
			.HasComment("Variante scelta del prodotto (es. \"Macchiato\"); NULL = nessuna variante.");

		builder.Property(x => x.VariantSurcharge)
			.HasColumnName("variant_surcharge")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Sovrapprezzo unitario della variante.");

		builder.Property(x => x.UnitPrice)
			.HasColumnName("unit_price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Prezzo unitario bloccato, senza variante.");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.IsRequired()
			.HasComment("Unità vendute (totale della riga); il dettaglio per giorno è in booking_resource_consumptions.");

		builder.Property(x => x.VatRate)
			.HasColumnName("vat_rate")
			.HasColumnType("numeric(5,2)")
			.HasComment("Aliquota IVA % congelata per lo scontrino.");

		// Colonna generata: i tre fattori sono tutti sulla stessa
		// riga e il modello non prevede alcun campo di override. Il DEFAULT 0 sparisce: una colonna
		// GENERATED ALWAYS non ammette default, il valore e' sempre la formula.
		builder.Property(x => x.LineTotal)
			.HasColumnName("line_total")
			.HasColumnType("numeric(12,2)")
			.HasComputedColumnSql("(unit_price + variant_surcharge) * quantity", stored: true)
			.HasComment("Totale bloccato della riga — colonna generata: (unit_price + " +
				"variant_surcharge) * quantity. Concorre al subtotal della testata; sola lettura.");

		builder.Property(x => x.ServiceDate)
			.HasColumnName("service_date")
			.HasColumnType("date")
			.HasComment("Data di riferimento del servizio quando è una sola; per servizi multi-data le date sono in booking_resource_consumptions.");

		builder.Property(x => x.BookingChannel)
			.HasColumnName("booking_channel")
			.HasColumnType("booking.booking_channel")
			.IsRequired()
			.HasComment("Canale della prenotazione di origine, denormalizzato dalla testata.");

		builder.Property(x => x.IsOrderFilled)
			.HasColumnName("is_order_filled")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("La riga è stata evasa (bar/cucina); il riepilogo a livello prenotazione è su booking_reservations.");

		builder.Property(x => x.IsPaidWithWallet)
			.HasColumnName("is_paid_with_wallet")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("La riga è stata pagata col borsellino/tessera prepagata; il movimento wallet è in C3.");


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


		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_booking_service_lines_public_id");

		builder.HasIndex(x => x.ReservationId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_service_lines_reservation");

		builder.HasIndex(x => new { x.PropertyId, x.Kind })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_service_lines_property_kind");

		builder.HasIndex(x => x.SaleItemId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_service_lines_sale_item");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("is_order_filled = false AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_service_lines_order_open");

		builder.HasOne(x => x.Reservation)
			.WithMany(r => r.ServiceLines)
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_service_lines_reservation")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.BookingLine)
			.WithMany(l => l.ServiceLines)
			.HasForeignKey(x => x.BookingLineId)
			.HasConstraintName("fk_booking_service_lines_line")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
