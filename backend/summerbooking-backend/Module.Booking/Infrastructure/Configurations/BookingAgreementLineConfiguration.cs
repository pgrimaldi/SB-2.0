using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingAgreementLineConfiguration : IEntityTypeConfiguration<BookingAgreementLine>
{
	public void Configure(EntityTypeBuilder<BookingAgreementLine> builder)
	{
		builder.ToTable("agreement_lines", "booking", t =>
		{
			t.HasComment("Voce di listino/voucher di una convenzione consumata da una riga prenotazione, con snapshot del prezzo applicato.");
			t.HasCheckConstraint("ck_booking_agreement_lines_quantity", "quantity > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_booking_agreement_lines");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico, copiato cross-modulo verso C3 che tiene importo/stato pagamento della voce.");

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione a cui la voce appartiene.");

		builder.Property(x => x.BookingLineId)
			.HasColumnName("booking_line_id")
			.HasComment("Riga postazione che ha consumato la voce di convenzione; NULL = voce a livello prenotazione.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.AgreementPartnerId)
			.HasColumnName("agreement_partner_id")
			.IsRequired()
			.HasComment("public_id di catalog.agreement_partners, copiato — struttura partner della convenzione.");

		builder.Property(x => x.AgreementPriceListId)
			.HasColumnName("agreement_price_list_id")
			.HasComment("public_id di catalog.agreement_price_lists, copiato — voce di listino/voucher di convenzione usata.");

		builder.Property(x => x.CustomerId)
			.HasColumnName("customer_id")
			.HasComment("public_id di identity.customer_customers, copiato dalla testata — per il conteggio utilizzi voucher per cliente/stagione senza join.");

		builder.Property(x => x.ListType)
			.HasColumnName("list_type")
			.HasColumnType("booking.agreement_line_type")
			.IsRequired()
			.HasDefaultValueSql("'regular'")
			.HasComment("Natura della voce: voce di listino ordinaria o voucher.");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasColumnType("smallint")
			.IsRequired()
			.HasDefaultValue((short)1)
			.HasComment("Quantità di voci di listino consumate.");

		builder.Property(x => x.PriceCalculation)
			.HasColumnName("price_calculation")
			.HasColumnType("smallint")
			.HasComment("Id copiato della lookup catalog.pricing_calculation_methods (metodo di calcolo prezzo congelato) — nessuna FK cross-schema.");

		builder.Property(x => x.AppliedPrice)
			.HasColumnName("applied_price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Prezzo bloccato della voce per la riga, già risolto dal motore prezzo (non l'intero listino).");

		builder.Property(x => x.IsUsedAsDiscount)
			.HasColumnName("is_used_as_discount")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("La voce è applicata come sconto sulla prenotazione invece che come addebito.");


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
			.HasDatabaseName("uq_booking_agreement_lines_public_id");

		builder.HasIndex(x => x.ReservationId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_agreement_lines_reservation");

		builder.HasIndex(x => x.BookingLineId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_agreement_lines_line");

		builder.HasIndex(x => x.AgreementPartnerId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_agreement_lines_partner");

		builder.HasIndex(x => new { x.PropertyId, x.AgreementPriceListId, x.CustomerId })
			.HasFilter("list_type = 'voucher' AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_agreement_lines_voucher_limit");

		builder.HasOne(x => x.Reservation)
			.WithMany(r => r.AgreementLines)
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_agreement_lines_reservation")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.BookingLine)
			.WithMany()
			.HasForeignKey(x => x.BookingLineId)
			.HasConstraintName("fk_booking_agreement_lines_line")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
