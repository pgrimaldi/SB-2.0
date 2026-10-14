using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Module.Booking.Domain.Entities;



namespace Module.Booking.Infrastructure.Configurations;



public sealed class BookingLineConfiguration : IEntityTypeConfiguration<BookingLine>

{

	public void Configure(EntityTypeBuilder<BookingLine> builder)

	{

		builder.ToTable("lines", "booking", t =>

		{

			t.HasComment("Riga dell'aggregato Prenotazione: una postazione per un intervallo di date contiguo e una fascia oraria.");

			t.HasCheckConstraint("ck_booking_lines_dates", "end_date >= start_date");

			t.HasCheckConstraint("ck_booking_lines_quantity", "quantity >= 1");

		});



		builder.HasKey(x => x.Id).HasName("pk_booking_lines");

		builder.Property(x => x.Id).UseIdentityAlwaysColumn();



		builder.Property(x => x.PublicId)

			.HasColumnName("public_id")

			.ValueGeneratedNever()

			.IsRequired()

			.HasComment("Id pubblico stabile della riga, copiato cross-modulo da C3 (righe economiche) e C4 (line item scontrino).");



		builder.Property(x => x.ReservationId)

			.HasColumnName("reservation_id")

			.IsRequired()

			.HasComment("Prenotazione (testata) a cui appartiene la riga.");



		builder.Property(x => x.BookingLineNumber)

			.HasColumnName("booking_line_number")

			.HasMaxLength(48)

			.IsRequired()

			.HasComment("Numero riga leggibile, subordinato al booking_number della testata (es. 10795-0000012345-01).");



		builder.Property(x => x.BookingNumber)

			.HasColumnName("booking_number")

			.HasMaxLength(40)

			.IsRequired()

			.HasComment("Numero prenotazione copiato dalla testata, per correlazioni dirette con C3/C4 senza join.");



		builder.Property(x => x.PropertyId)

			.HasColumnName("property_id")

			.IsRequired()

			.HasComment("public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema.");



		builder.Property(x => x.ResourceId)

			.HasColumnName("resource_id")

			.HasComment("public_id di catalog.map_spots, copiato — la postazione prenotata; NULL per le righe ad area.");



		builder.Property(x => x.SpotLabelSnapshot)

			.HasColumnName("spot_label_snapshot")

			.HasMaxLength(40)

			.HasComment("Etichetta della postazione congelata al momento della prenotazione.");



		builder.Property(x => x.SectorId)

			.HasColumnName("sector_id")

			.HasComment("public_id di catalog.map_sectors, copiato — settore contro cui è stata calcolata la tariffa.");



		builder.Property(x => x.SectorLabelSnapshot)

			.HasColumnName("sector_label_snapshot")

			.HasMaxLength(100)

			.HasComment("Etichetta del settore/fila congelata al momento della prenotazione.");



		builder.Property(x => x.PriceListId)

			.HasColumnName("price_list_id")

			.HasComment("public_id di catalog.pricing_price_lists, copiato — voce di listino (pacchetto) applicata alla riga.");



		builder.Property(x => x.PackageNameSnapshot)

			.HasColumnName("package_name_snapshot")

			.HasMaxLength(255)

			.HasComment("Nome del pacchetto congelato al momento della prenotazione.");



		builder.Property(x => x.StartDate)

			.HasColumnName("start_date")

			.HasColumnType("date")

			.IsRequired()

			.HasComment("Inizio dell'intervallo contiguo coperto dalla riga (incluso).");



		builder.Property(x => x.EndDate)

			.HasColumnName("end_date")

			.HasColumnType("date")

			.IsRequired()

			.HasComment("Fine dell'intervallo contiguo coperto dalla riga (incluso).");



		builder.Property(x => x.BookingPeriod)

			.HasColumnName("booking_period")

			.HasColumnType("booking.day_period")

			.IsRequired()

			.HasDefaultValueSql("'full_day'")

			.HasComment("Fascia oraria della riga.");



		builder.Property(x => x.Quantity)

			.HasColumnName("quantity")

			.HasColumnType("smallint")

			.IsRequired()

			.HasDefaultValue((short)1)

			.HasComment("Numero di postazioni coperte dalla riga; > 1 solo per riga ad area.");



		builder.Property(x => x.LinePrice)

			.HasColumnName("line_price")

			.HasColumnType("numeric(12,2)")

			.IsRequired()

			.HasDefaultValue(0m)

			.HasComment("Prezzo bloccato della riga (pacchetto), al netto degli articoli aggiuntivi.");



		builder.Property(x => x.IsSplit)

			.HasColumnName("is_split")

			.IsRequired()

			.HasDefaultValue(false)

			.HasComment("La riga fa parte di una prenotazione split-location.");



		builder.Property(x => x.IsArea)

			.HasColumnName("is_area")

			.IsRequired()

			.HasDefaultValue(false)

			.HasComment("Prenotazione ad area a capienza (senza postazione puntuale); se true resource_id è NULL.");



		builder.Property(x => x.AreaResourceId)

			.HasColumnName("area_resource_id")

			.HasComment("public_id di catalog.map_spots (spot_type d'area), copiato — l'area occupata quando is_area = true.");



		builder.Property(x => x.AreaSeatCount)

			.HasColumnName("area_seat_count")

			.HasColumnType("smallint")

			.HasComment("Posti occupati nell'area da questa riga (solo se is_area).");



		builder.Property(x => x.CheckedInAt)

			.HasColumnName("checked_in_at")

			.HasComment("Quando il cliente si è presentato per questa postazione; NULL = non arrivato.");



		builder.Property(x => x.CheckedOutAt)

			.HasColumnName("checked_out_at")

			.HasComment("Quando la postazione è stata liberata / cliente uscito.");



		builder.Property(x => x.CheckoutEffectiveAt)

			.HasColumnName("checkout_effective_at")

			.HasComment("Fine occupazione effettiva in caso di check-out anticipato; limite superiore per la disponibilità al posto di end_date.");





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

			.HasDatabaseName("uq_booking_lines_public_id");



		builder.HasIndex(x => x.BookingLineNumber)

			.IsUnique()

			.HasFilter("deleted_at IS NULL")

			.HasDatabaseName("uq_booking_lines_number");



		builder.HasIndex(x => x.ReservationId)

			.HasFilter("deleted_at IS NULL")

			.HasDatabaseName("ix_booking_lines_reservation");



		builder.HasIndex(x => new { x.PropertyId, x.ResourceId, x.StartDate, x.EndDate })

			.HasFilter("deleted_at IS NULL")

			.HasDatabaseName("ix_booking_lines_property_resource_dates");



		// Overload con nome: due HasIndex sulle stesse colonne senza nome sono LO STESSO indice per EF,
		// e il secondo ne sovrascrive nome, filtro e unicità. Fino al 2026-10-06 a DB ne esisteva uno solo.
		builder.HasIndex(x => new { x.PropertyId, x.StartDate }, "ix_booking_lines_property_stay")

			.HasFilter("deleted_at IS NULL")

			.HasDatabaseName("ix_booking_lines_property_stay");



		builder.HasIndex(x => x.PriceListId)

			.HasFilter("deleted_at IS NULL")

			.HasDatabaseName("ix_booking_lines_price_list");



		builder.HasIndex(x => new { x.PropertyId, x.StartDate }, "ix_booking_lines_checkin_open")

			.HasFilter("checked_in_at IS NOT NULL AND checked_out_at IS NULL AND deleted_at IS NULL")

			.HasDatabaseName("ix_booking_lines_checkin_open");



		builder.HasIndex(x => x.BookingNumber)

			.HasDatabaseName("ix_booking_lines_booking_number");



		builder.HasOne(x => x.Reservation)

			.WithMany(r => r.Lines)

			.HasForeignKey(x => x.ReservationId)

			.HasConstraintName("fk_booking_lines_reservation")

			.OnDelete(DeleteBehavior.Cascade);

	}

}

