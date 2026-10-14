using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingDiscountConfiguration : IEntityTypeConfiguration<BookingDiscount>
{
	public void Configure(EntityTypeBuilder<BookingDiscount> builder)
	{
		builder.ToTable("discounts", "booking", t => t.HasComment(
			"Snapshot di un voucher/sconto applicato a una prenotazione o a una sua riga."));

		builder.HasKey(x => x.Id).HasName("pk_booking_discounts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione a cui si applica lo sconto.");

		builder.Property(x => x.BookingLineId)
			.HasColumnName("booking_line_id")
			.HasComment("Riga specifica a cui lo sconto è limitato; NULL = sconto sull'intera prenotazione.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.DiscountId)
			.HasColumnName("discount_id")
			.HasComment("public_id di catalog.pricing_discounts, copiato — riferimento vivo per l'audit del contatore utilizzi; NULL per sconto manuale non a catalogo.");

		builder.Property(x => x.Origin)
			.HasColumnName("origin")
			.HasColumnType("booking.discount_origin")
			.IsRequired()
			.HasDefaultValueSql("'code_entered'")
			.HasComment("Come lo sconto è entrato nella prenotazione: codice digitato o regola automatica.");

		builder.Property(x => x.CodeSnapshot)
			.HasColumnName("code_snapshot")
			.HasMaxLength(80)
			.IsRequired()
			.HasComment("Codice/nome del voucher congelato al momento dell'applicazione.");

		builder.Property(x => x.DiscountType)
			.HasColumnName("discount_type")
			.HasColumnType("booking.discount_type")
			.IsRequired()
			.HasComment("Natura del valore: importo fisso o percentuale.");

		builder.Property(x => x.Value)
			.HasColumnName("value")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Valore dello sconto congelato (importo € se fixed, percentuale se percentage).");

		builder.Property(x => x.Scope)
			.HasColumnName("scope")
			.HasColumnType("booking.discount_scope")
			.IsRequired()
			.HasDefaultValueSql("'all'")
			.HasComment("Ambito di validità del voucher, congelato dalla regola di C2.");

		builder.Property(x => x.AppliedChannel)
			.HasColumnName("applied_channel")
			.HasColumnType("booking.discount_channel")
			.IsRequired()
			.HasDefaultValueSql("'any'")
			.HasComment("Canale su cui il voucher era ammesso, congelato dalla regola di C2.");

		builder.Property(x => x.AppliesToCategories)
			.HasColumnName("applies_to_categories")
			.HasColumnType("booking.service_category[]")
			.IsRequired()
			.HasDefaultValueSql("'{}'")
			.HasComment("Categorie di servizio a cui il voucher è limitato, congelate; vuoto = nessun limite.");

		builder.Property(x => x.IsRoundup)
			.HasColumnName("is_roundup")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Arrotondamento del prezzo dopo lo sconto, congelato dalla regola.");

		builder.Property(x => x.AppliedAmount)
			.HasColumnName("applied_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Importo effettivamente scontato su questa prenotazione, calcolato dal motore prezzo server-side.");

		builder.Property(x => x.ConsumedUsage)
			.HasColumnName("consumed_usage")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("True se questa prenotazione ha consumato un utilizzo del voucher; l'incremento/decremento del contatore è atomico su C2.");

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


		builder.HasIndex(x => x.ReservationId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_discounts_reservation");

		builder.HasIndex(x => x.DiscountId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_discounts_discount");

		builder.HasIndex(x => new { x.PropertyId, x.CodeSnapshot })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_discounts_property_code");

		builder.HasOne(x => x.Reservation)
			.WithMany(r => r.Discounts)
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_discounts_reservation")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.BookingLine)
			.WithMany()
			.HasForeignKey(x => x.BookingLineId)
			.HasConstraintName("fk_booking_discounts_line")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
