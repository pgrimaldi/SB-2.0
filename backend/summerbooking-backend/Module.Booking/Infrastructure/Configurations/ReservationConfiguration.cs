using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
	public void Configure(EntityTypeBuilder<Reservation> builder)
	{
		builder.ToTable("reservations", "booking", t => t.HasComment(
			"Testata unica di una prenotazione logica (un booking_number), spot o solo servizi."));

		builder.HasKey(x => x.Id).HasName("pk_booking_reservations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, copiato cross-modulo da C3/C4/C6.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; discriminatore tenant.");

		builder.Property(x => x.BathhouseId)
			.HasColumnName("bathhouse_id")
			.HasComment("public_id di catalog.bathhouses, copiato — NULL = vetrina di default della property.");

		builder.Property(x => x.BookingNumber)
			.HasColumnName("booking_number")
			.HasMaxLength(40)
			.IsRequired()
			.HasComment("Numero prenotazione generato server-side da booking_number_sequences, univoco fra righe vive.");

		builder.Property(x => x.ReservationCode)
			.HasColumnName("reservation_code")
			.HasMaxLength(16)
			.IsRequired()
			.HasComment("Codice pubblico (URL/QR) generato con RNG sicuro, univoco fra righe vive.");

		builder.Property(x => x.Kind)
			.HasColumnName("reservation_kind")
			.HasColumnType("booking.reservation_kind")
			.IsRequired()
			.HasComment("spot (con postazioni) o service (soli servizi).");

		builder.Property(x => x.CustomerId)
			.HasColumnName("customer_id")
			.HasComment("public_id di identity.customers, copiato — riferimento vivo, nessuna FK cross-schema.");

		builder.Property(x => x.Channel)
			.HasColumnName("booking_channel")
			.HasColumnType("booking.booking_channel")
			.IsRequired()
			.HasComment("Origine della prenotazione: customer (online) o manager (gestionale). Immutabile dopo la creazione.");

		builder.Property(x => x.Status)
			.HasColumnName("reservation_status")
			.HasColumnType("booking.reservation_status")
			.IsRequired()
			.HasComment("Stato del ciclo di vita, separato da soft-delete e da pagamento.");

		builder.Property(x => x.OnlineConfirmationState)
			.HasColumnName("online_confirmation_state")
			.HasColumnType("booking.online_confirmation_state")
			.IsRequired()
			.HasDefaultValueSql("'not_required'")
			.HasComment("Stato del flusso di conferma online via reservation_code.");

		builder.Property(x => x.OperatorUserId)
			.HasColumnName("operator_user_id")
			.HasComment("public_id di identity.access_users, copiato dal token JWT — operatore che ha creato la prenotazione, NULL se online.");

		builder.Property(x => x.UpdatedByUserId)
			.HasColumnName("updated_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — ultimo operatore che ha modificato la prenotazione.");

		builder.Property(x => x.AgreementPartnerId)
			.HasColumnName("agreement_partner_id")
			.HasComment("public_id di catalog.agreement_partners, copiato — valorizzato solo per reservation_kind = spot.");

		builder.Property(x => x.ListingType)
			.HasColumnName("listing_type")
			.HasColumnType("booking.listing_type")
			.IsRequired()
			.HasDefaultValueSql("'standard'")
			.HasComment("Tipo di listino applicato: standard, listino convenzione o voucher convenzione.");

		builder.Property(x => x.HasSeasonalPass)
			.HasColumnName("has_seasonal_pass")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Prenotazione stagionale / abbonamento; false per reservation_kind = service.");

		builder.Property(x => x.IsSplit)
			.HasColumnName("is_split")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Prenotazione split-location (derivabile dalle righe, tenuto per compatibilità di lettura).");

		builder.Property(x => x.PriceFlags)
			.HasColumnName("price_flags")
			.HasColumnType("booking.reservation_price_flag[]")
			.IsRequired()
			.HasDefaultValueSql("'{}'")
			.HasComment("Come si è formato il prezzo finale (accettato suggerito / ricalcolato / forzato a mano) — flag non esclusivi.");

		builder.Property(x => x.PayNow)
			.HasColumnName("pay_now")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Il cliente ha scelto di pagare subito online al checkout (canale customer).");

		builder.Property(x => x.IsOrderFilled)
			.HasColumnName("is_order_filled")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Riepilogo evasione ordini bar/servizio collegati; dettaglio per riga su booking_service_lines.");

		builder.Property(x => x.Note)
			.HasColumnName("note")
			.HasColumnType("text")
			.HasComment("Nota libera dell'operatore sulla prenotazione.");

		builder.Property(x => x.CustomerTags)
			.HasColumnName("customer_tags")
			.HasColumnType("text[]")
			.IsRequired()
			.HasDefaultValueSql("'{}'")
			.HasComment("Tag/icone cliente selezionati nel riepilogo (presentazione, vocabolario chiuso lato UI).");

		builder.Property(x => x.StayFrom)
			.HasColumnName("stay_from")
			.HasColumnType("date")
			.HasComment("Data minima dell'intervallo complessivo, denormalizzata dalle righe per i filtri di agenda.");

		builder.Property(x => x.StayTo)
			.HasColumnName("stay_to")
			.HasColumnType("date")
			.HasComment("Data massima dell'intervallo complessivo, denormalizzata dalle righe per i filtri di agenda.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.IsRequired()
			.HasDefaultValue("EUR")
			.HasComment("Valuta ISO 4217 degli importi della prenotazione.");

		builder.Property(x => x.Subtotal)
			.HasColumnName("subtotal")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Imponibile prima di sconti e supplementi, somma delle righe.");

		builder.Property(x => x.Discount)
			.HasColumnName("discount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Totale sconti/voucher applicati (valore assoluto negativo o 0); dettaglio su booking_discounts.");

		builder.Property(x => x.Surcharge)
			.HasColumnName("surcharge")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Supplemento/sovrapprezzo (commissione di servizio, extra manuale).");

		builder.Property(x => x.Total)
			.HasColumnName("total")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Totale dovuto = subtotal + surcharge + discount; calcolato dal motore prezzo server-side.");

		builder.Property(x => x.PriceOverride)
			.HasColumnName("price_override")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo finale forzato manualmente dall'operatore; NULL = nessun override.");

		builder.Property(x => x.Deposit)
			.HasColumnName("deposit")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Importo acconto/caparra corrente (riepilogo); i singoli acconti sono movimenti C3.");

		builder.Property(x => x.Paid)
			.HasColumnName("paid")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Totale già incassato, proiezione dagli eventi di pagamento di C3.");

		builder.Property(x => x.BalanceDue)
			.HasColumnName("balance_due")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Residuo da incassare = total - paid; può essere negativo per storni/incassi in eccesso.");

		builder.Property(x => x.PrevalentPaymentMode)
			.HasColumnName("prevalent_payment_mode")
			.HasColumnType("booking.payment_mode")
			.HasComment("Modalità di pagamento prevalente (riepilogo); NULL finché non ancora incassata/determinata.");

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
			.HasDatabaseName("uq_booking_reservations_public_id");

		builder.HasIndex(x => x.BookingNumber)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_booking_reservations_number");

		builder.HasIndex(x => x.ReservationCode)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_booking_reservations_reservation_code");

		// Overload con nome: due HasIndex sulle stesse colonne senza nome sono LO STESSO indice per EF,
		// e il secondo ne sovrascrive nome, filtro e unicità. Fino al 2026-10-06 a DB ne esisteva uno solo.
		builder.HasIndex(x => new { x.PropertyId, x.StayFrom }, "ix_booking_reservations_property_stay")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_reservations_property_stay");

		builder.HasIndex(x => new { x.PropertyId, x.Status })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_reservations_property_status");

		builder.HasIndex(x => x.CustomerId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_reservations_customer");

		builder.HasIndex(x => new { x.PropertyId, x.Channel })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_reservations_property_channel");

		builder.HasIndex(x => x.AgreementPartnerId)
			.HasFilter("agreement_partner_id IS NOT NULL AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_reservations_agreement");

		builder.HasIndex(x => new { x.PropertyId, x.StayFrom }, "ix_booking_reservations_unpaid")
			.HasFilter("balance_due > 0 AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_reservations_unpaid");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("online_confirmation_state = 'pending' AND deleted_at IS NULL")
			.HasDatabaseName("ix_booking_reservations_online_pending");
	}
}
