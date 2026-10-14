using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Payment.Domain.Entities;
using Module.Payment.Domain.Enums;

namespace Module.Payment.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="PosCharge"/> su payment.pos_charges.</summary>
public sealed class PosChargeConfiguration : IEntityTypeConfiguration<PosCharge>
{
	public void Configure(EntityTypeBuilder<PosCharge> builder)
	{
		builder.ToTable("pos_charges", "payment", t =>
		{
			t.HasComment("Scontrino/addebito trasmesso da un POS esterno (OfSale o TeamSystem) e associato a una prenotazione dello stabilimento.");
			t.HasCheckConstraint("ck_payment_pos_charges_total_amount", "total_amount > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_payment_pos_charges");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile restituito al sistema POS come identificativo della spesa; su un retry con lo stesso external_ref si restituisce lo stesso valore (idempotenza).");

		builder.Property(x => x.PosSystem)
			.HasColumnName("pos_system")
			.HasColumnType("payment.pos_system")
			.IsRequired()
			.HasComment("Sistema POS di origine, esclusivo per riga.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento a cui la spesa è addebitata.");

		builder.Property(x => x.ReservationPublicId)
			.HasColumnName("reservation_public_id")
			.HasComment("public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; prenotazione al cui conto la spesa confluisce.");

		builder.Property(x => x.BookingNumber)
			.HasColumnName("booking_number")
			.HasMaxLength(32)
			.HasComment("Numero prenotazione, denormalizzato per correlazione col legacy.");

		builder.Property(x => x.ExternalRef)
			.HasColumnName("external_ref")
			.HasMaxLength(64)
			.IsRequired()
			.HasComment("Id del documento/scontrino sul POS di origine, chiave di deduplicazione.");

		builder.Property(x => x.Description)
			.HasColumnName("description")
			.HasMaxLength(160)
			.HasComment("Nome del punto vendita / descrizione dello scontrino, testo libero come inviato dal POS (nessuna entità nostra corrispondente).");

		builder.Property(x => x.TotalAmount)
			.HasColumnName("total_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Totale dello scontrino, IVA inclusa; deve coincidere con la somma delle righe entro 1 cent.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.HasDefaultValue("EUR")
			.IsRequired()
			.HasComment("Valuta ISO 4217.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("payment.pos_charge_status")
			.HasDefaultValueSql("'recorded'")
			.IsRequired()
			.HasComment("Stato dello scontrino POS nel gestionale: acquisito o annullato dal POS con un documento di storno.");

		builder.Property(x => x.LedgerEntryRef)
			.HasColumnName("ledger_entry_ref")
			.HasComment("public_id di payment.ledger_entries — correlazione debole (non FK EF) verso la riga contabile generata dall'addebito, valorizzata a posteriori dall'handler dell'evento.");

		builder.Property(x => x.ChargedAt)
			.HasColumnName("charged_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora dello scontrino sul POS, decorrenza retention.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: append-only (concorre al fiscale, retention 10 anni), sempre NULL.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_payment_pos_charges_public_id");

		builder.HasIndex(x => new { x.PosSystem, x.PropertyId, x.ExternalRef })
			.IsUnique()
			.HasDatabaseName("uq_payment_pos_charges_external_ref");

		builder.HasIndex(x => x.ReservationPublicId)
			.HasDatabaseName("ix_payment_pos_charges_reservation");

		builder.HasIndex(x => new { x.PropertyId, x.ChargedAt })
			.HasDatabaseName("ix_payment_pos_charges_property_date");

		builder.HasIndex(x => x.BookingNumber)
			.HasDatabaseName("ix_payment_pos_charges_booking_number");

		builder.OwnsMany(x => x.Items, items => PosChargeItemConfiguration.Configure(items));
	}
}
