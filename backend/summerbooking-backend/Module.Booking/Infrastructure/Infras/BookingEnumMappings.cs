using Module.Booking.Domain.Enums;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Module.Booking.Infrastructure.Infras;

/// <summary>
/// Enum nativi del modulo, un solo elenco per runtime e design-time: sul data source e nelle opzioni EF
/// (senza la seconda EF scrive l'enum come intero). In coppia con <c>HasPostgresEnum</c> in <see cref="DbContext"/>.
/// </summary>
internal static class BookingEnumMappings
{
	public static void Apply(NpgsqlDataSourceBuilder builder)
	{
		builder.MapEnum<AdditionalItemDateScope>("booking.additional_item_date_scope");
		builder.MapEnum<AdditionalItemSource>("booking.additional_item_source");
		builder.MapEnum<AdjustmentType>("booking.adjustment_type");
		builder.MapEnum<AgreementLineType>("booking.agreement_line_type");
		builder.MapEnum<BookingChannel>("booking.booking_channel");
		builder.MapEnum<CancellationReason>("booking.cancellation_reason");
		builder.MapEnum<ConsumptionSource>("booking.consumption_source");
		builder.MapEnum<ConsumptionState>("booking.consumption_state");
		builder.MapEnum<DayPeriod>("booking.day_period");
		builder.MapEnum<DiscountChannel>("booking.discount_channel");
		builder.MapEnum<DiscountOrigin>("booking.discount_origin");
		builder.MapEnum<DiscountScope>("booking.discount_scope");
		builder.MapEnum<DiscountType>("booking.discount_type");
		builder.MapEnum<HoldKind>("booking.hold_kind");
		builder.MapEnum<HoldStatus>("booking.hold_status");
		builder.MapEnum<ListingType>("booking.listing_type");
		builder.MapEnum<OnlineConfirmationState>("booking.online_confirmation_state");
		builder.MapEnum<ParticipantRole>("booking.participant_role");
		builder.MapEnum<PaymentMode>("booking.payment_mode");
		builder.MapEnum<ResaleCreditEntryType>("booking.resale_credit_entry");
		builder.MapEnum<ResaleOfferState>("booking.resale_offer_state");
		builder.MapEnum<ResaleOrigin>("booking.resale_origin");
		builder.MapEnum<ResaleScope>("booking.resale_scope");
		builder.MapEnum<ReservationKind>("booking.reservation_kind");
		builder.MapEnum<ReservationPriceFlag>("booking.reservation_price_flag");
		builder.MapEnum<ReservationStatus>("booking.reservation_status");
		builder.MapEnum<ResourceType>("booking.resource_type");
		builder.MapEnum<ServiceCategory>("booking.service_category");
	}

	public static NpgsqlDbContextOptionsBuilder Apply(NpgsqlDbContextOptionsBuilder options)
	{
		const string schema = DbContext.Schema;
		options.MapEnum<AdditionalItemDateScope>("additional_item_date_scope", schema);
		options.MapEnum<AdditionalItemSource>("additional_item_source", schema);
		options.MapEnum<AdjustmentType>("adjustment_type", schema);
		options.MapEnum<AgreementLineType>("agreement_line_type", schema);
		options.MapEnum<BookingChannel>("booking_channel", schema);
		options.MapEnum<CancellationReason>("cancellation_reason", schema);
		options.MapEnum<ConsumptionSource>("consumption_source", schema);
		options.MapEnum<ConsumptionState>("consumption_state", schema);
		options.MapEnum<DayPeriod>("day_period", schema);
		options.MapEnum<DiscountChannel>("discount_channel", schema);
		options.MapEnum<DiscountOrigin>("discount_origin", schema);
		options.MapEnum<DiscountScope>("discount_scope", schema);
		options.MapEnum<DiscountType>("discount_type", schema);
		options.MapEnum<HoldKind>("hold_kind", schema);
		options.MapEnum<HoldStatus>("hold_status", schema);
		options.MapEnum<ListingType>("listing_type", schema);
		options.MapEnum<OnlineConfirmationState>("online_confirmation_state", schema);
		options.MapEnum<ParticipantRole>("participant_role", schema);
		options.MapEnum<PaymentMode>("payment_mode", schema);
		options.MapEnum<ResaleCreditEntryType>("resale_credit_entry", schema);
		options.MapEnum<ResaleOfferState>("resale_offer_state", schema);
		options.MapEnum<ResaleOrigin>("resale_origin", schema);
		options.MapEnum<ResaleScope>("resale_scope", schema);
		options.MapEnum<ReservationKind>("reservation_kind", schema);
		options.MapEnum<ReservationPriceFlag>("reservation_price_flag", schema);
		options.MapEnum<ReservationStatus>("reservation_status", schema);
		options.MapEnum<ResourceType>("resource_type", schema);
		options.MapEnum<ServiceCategory>("service_category", schema);
		return options;
	}
}
