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
			.HasComment("Id pubblico stabile, copiato cross-modulo da Payment/Fiskaly/Notification.");

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
			.HasComment("Conferma del gestore delle prenotazioni online che la richiedono: not_required, pending, confirmed.");

		builder.Property(x => x.OperatorUserId)
			.HasColumnName("operator_user_id")
			.HasComment("public_id di identity.access_users, copiato dal token JWT — operatore che ha creato la prenotazione, NULL se online.");

		builder.Property(x => x.UpdatedByUserId)
			.HasColumnName("updated_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — ultimo operatore che ha modificato la prenotazione.");

		builder.Property(x => x.AgreementPartnerId)
			.HasColumnName("agreement_partner_id")
			.HasComment("public_id di catalog.agreement_partners, copiato — riepilogo: struttura della prima riga con una convenzione; il dettaglio per riga è in booking.agreement_lines.");

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
			.HasComment("Importo acconto/caparra corrente (riepilogo); i singoli acconti sono movimenti Payment.");

		builder.Property(x => x.Paid)
			.HasColumnName("paid")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Totale già incassato, proiezione dagli eventi di pagamento di Payment.");

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

		builder.Property(x => x.NoteUpdatedAt)
			.HasColumnName("note_updated_at")
			.HasComment("Ultima modifica del testo della nota.");

		builder.Property(x => x.NoteUpdatedByUserId)
			.HasColumnName("note_updated_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — chi ha scritto per ultimo la nota.");

		builder.Property(x => x.ManualDiscountType)
			.HasColumnName("manual_discount_type")
			.HasColumnType("booking.adjustment_type")
			.HasComment("Sconto manuale dell'operatore: percentage o amount; NULL = nessuno. Si somma al codice sconto, entrambi sul subtotale.");

		builder.Property(x => x.ManualDiscountValue)
			.HasColumnName("manual_discount_value")
			.HasColumnType("numeric(12,2)")
			.HasComment("Valore dello sconto manuale: percentuale (0-100) o euro.");

		builder.Property(x => x.ManualDiscountAmount)
			.HasColumnName("manual_discount_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Importo in euro dello sconto manuale, compreso in discount.");

		builder.Property(x => x.SurchargeLabel)
			.HasColumnName("surcharge_label")
			.HasMaxLength(120)
			.HasComment("Etichetta libera dell'unico incremento, riga a sé sullo scontrino.");

		builder.Property(x => x.SurchargeType)
			.HasColumnName("surcharge_type")
			.HasColumnType("booking.adjustment_type")
			.HasComment("Incremento: percentage o amount; NULL = nessuno.");

		builder.Property(x => x.SurchargeValue)
			.HasColumnName("surcharge_value")
			.HasColumnType("numeric(12,2)")
			.HasComment("Valore dell'incremento: percentuale (0-100) o euro.");

		builder.Property(x => x.PaidMarkEntryId)
			.HasColumnName("paid_mark_entry_id")
			.HasComment("public_id di payment.ledger_entries, copiato — l'incasso creato con la spunta «Pagato», annullato togliendola.");

		builder.Property(x => x.CheckedInAt)
			.HasColumnName("checked_in_at")
			.HasComment("Check-in dell'intera prenotazione; NULL = cliente non arrivato.");

		builder.Property(x => x.CheckedOutAt)
			.HasColumnName("checked_out_at")
			.HasComment("Check-out dell'intera prenotazione, anche anticipato.");

		builder.Property(x => x.CheckoutEffectiveAt)
			.HasColumnName("checkout_effective_at")
			.HasComment("Fine effettiva dell'occupazione con un check-out anticipato: i giorni da lì in poi tornano liberi.");

		builder.Property(x => x.CancellationReason)
			.HasColumnName("cancellation_reason")
			.HasColumnType("booking.cancellation_reason")
			.HasComment("Motivo dell'annullamento: customer, manager, rejected, property_trashed, spot_removed, payment_failed, confirmation_expired.");

		builder.Property(x => x.CancelledAt)
			.HasColumnName("cancelled_at")
			.HasComment("Quando la prenotazione è stata annullata.");

		builder.Property(x => x.ConfirmationExpiresAt)
			.HasColumnName("confirmation_expires_at")
			.HasComment("Scadenza della richiesta online da confermare: la prima fra 48 ore, le 23:00 della vigilia e, per il giorno stesso, 1 ora.");

		builder.Property(x => x.ConfirmedByUserId)
			.HasColumnName("confirmed_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — chi ha confermato la richiesta online.");

		builder.Property(x => x.ConfirmedAt)
			.HasColumnName("confirmed_at")
			.HasComment("Quando il gestore ha confermato la richiesta online.");

		builder.Property(x => x.Version).IsRowVersion();

		builder.Property(x => x.SyncSeq)
			.HasColumnName("sync_seq")
			.HasDefaultValueSql("nextval('booking.sync_seq')")
			.ValueGeneratedOnAddOrUpdate()
			.HasComment("Cursore di sincronizzazione dei dispositivi: nuovo numero a ogni modifica (trigger).");
		builder.HasIndex(x => new { x.PropertyId, x.SyncSeq }).HasDatabaseName("ix_booking_reservations_sync");

		builder.Property(x => x.PaymentHoldUntil)
			.HasColumnName("payment_hold_until")
			.HasComment("Prenotazione online in attesa del pagamento: postazioni tenute fino a quest'ora.");

		builder.HasIndex(x => x.PaymentHoldUntil)
			.HasFilter("payment_hold_until IS NOT NULL")
			.HasDatabaseName("ix_booking_reservations_payment_hold");

		builder.Ignore(x => x.AwaitsPayment);
		builder.Ignore(x => x.AwaitsConfirmation);

		builder.Property(x => x.RenewedFromReservationId)
			.HasColumnName("renewed_from_reservation_id")
			.HasComment("Prenotazione da cui nasce il rinnovo; impedisce di rinnovarla due volte.");

		builder.Property(x => x.SplitFromReservationId)
			.HasColumnName("split_from_reservation_id")
			.HasComment("Prenotazione da cui nasce con «Dividi conto».");

		builder.HasOne<Reservation>().WithMany().HasForeignKey(x => x.RenewedFromReservationId)
			.HasConstraintName("fk_booking_reservations_renewed_from").OnDelete(DeleteBehavior.SetNull);

		builder.HasOne<Reservation>().WithMany().HasForeignKey(x => x.SplitFromReservationId)
			.HasConstraintName("fk_booking_reservations_split_from").OnDelete(DeleteBehavior.SetNull);

		// Default now() a schema su created_at/updated_at: nessuna convenzione EF globale lo applica
		// (ModuleDbContextConventions mette solo il converter UTC e il query filter soft-delete), e un ETL
		// che omette queste colonne dall'INSERT per lasciare scattare il default fallirebbe sul NOT NULL.
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
		// e il secondo ne sovrascrive nome, filtro e unicità.
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

		builder.HasIndex(x => x.DeletedAt, "ix_booking_reservations_trash")
			.HasFilter("deleted_at IS NOT NULL")
			.HasDatabaseName("ix_booking_reservations_trash");

		builder.HasIndex(x => x.RenewedFromReservationId)
			.IsUnique()
			.HasFilter("renewed_from_reservation_id IS NOT NULL AND deleted_at IS NULL")
			.HasDatabaseName("uq_booking_reservations_renewed_from");

		builder.HasIndex(x => x.SplitFromReservationId)
			.HasFilter("split_from_reservation_id IS NOT NULL")
			.HasDatabaseName("ix_booking_reservations_split_from");
	}
}
