using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;
using Shared.Infrastructure;

namespace Module.Booking.Infrastructure;

/// <summary>
/// Contesto di persistenza del modulo Booking, schema PostgreSQL "booking".
/// Nome della classe = solo "DbContext": il namespace la disambigua dagli altri moduli.
/// Mai referenziato fuori da Module.Booking.Infrastructure.
/// </summary>
public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
	public const string Schema = "booking";

	private readonly IDateTimeProvider _dateTimeProvider;

	public DbContext(DbContextOptions<DbContext> options, IDateTimeProvider dateTimeProvider) : base(options)
	{
		_dateTimeProvider = dateTimeProvider;
	}

	// DbSet<TEntity> di ogni aggregato/entita del modulo — popolati
	// dall'implementazione del modulo, uno per tabella con IEntityTypeConfiguration corrispondente.
	// BookingGroupMember non ha DbSet pubblico: raggiungibile solo tramite BookingGroup.Members.
	public DbSet<BookingAgreementLine> BookingAgreementLines => Set<BookingAgreementLine>();
	public DbSet<BookingDiscount> BookingDiscounts => Set<BookingDiscount>();
	public DbSet<BookingGroup> BookingGroups => Set<BookingGroup>();
	public DbSet<BookingGuest> BookingGuests => Set<BookingGuest>();
	public DbSet<BookingLine> BookingLines => Set<BookingLine>();
	public DbSet<BookingLineAdditionalItem> BookingLineAdditionalItems => Set<BookingLineAdditionalItem>();
	public DbSet<BookingNumberSequence> BookingNumberSequences => Set<BookingNumberSequence>();
	public DbSet<BookingParticipant> BookingParticipants => Set<BookingParticipant>();
	public DbSet<BookingQuickActionDate> BookingQuickActionDates => Set<BookingQuickActionDate>();
	public DbSet<BookingServiceLine> BookingServiceLines => Set<BookingServiceLine>();
	public DbSet<BookingServiceTimeSlot> BookingServiceTimeSlots => Set<BookingServiceTimeSlot>();
	public DbSet<ResaleCreditLedgerEntry> ResaleCreditLedgerEntries => Set<ResaleCreditLedgerEntry>();
	public DbSet<ResaleOffer> ResaleOffers => Set<ResaleOffer>();
	public DbSet<ResaleSettings> ResaleSettings => Set<ResaleSettings>();
	public DbSet<Reservation> Reservations => Set<Reservation>();
	public DbSet<ResourceAvailabilityDaily> ResourceAvailabilityDailies => Set<ResourceAvailabilityDaily>();
	public DbSet<ResourceConsumption> ResourceConsumptions => Set<ResourceConsumption>();
	public DbSet<SpotHold> SpotHolds => Set<SpotHold>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.AddInterceptors(new AuditInterceptor(_dateTimeProvider), new SoftDeleteInterceptor(_dateTimeProvider));
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		ModuleDbContextConventions.ConfigureUtcDateTimes(configurationBuilder);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.HasDefaultSchema(Schema);

		// Enum nativi PostgreSQL del modulo (modelBuilder.HasPostgresEnum<TEnum>(Schema, "..."))
		// vanno qui.
		modelBuilder.HasPostgresEnum<AdditionalItemDateScope>(Schema, "additional_item_date_scope");
		modelBuilder.HasPostgresEnum<AdditionalItemSource>(Schema, "additional_item_source");
		modelBuilder.HasPostgresEnum<AgreementLineType>(Schema, "agreement_line_type");
		modelBuilder.HasPostgresEnum<BookingChannel>(Schema, "booking_channel");
		modelBuilder.HasPostgresEnum<ConsumptionSource>(Schema, "consumption_source");
		modelBuilder.HasPostgresEnum<ConsumptionState>(Schema, "consumption_state");
		modelBuilder.HasPostgresEnum<DayPeriod>(Schema, "day_period");
		modelBuilder.HasPostgresEnum<DiscountChannel>(Schema, "discount_channel");
		modelBuilder.HasPostgresEnum<DiscountOrigin>(Schema, "discount_origin");
		modelBuilder.HasPostgresEnum<DiscountScope>(Schema, "discount_scope");
		modelBuilder.HasPostgresEnum<DiscountType>(Schema, "discount_type");
		modelBuilder.HasPostgresEnum<HoldKind>(Schema, "hold_kind");
		modelBuilder.HasPostgresEnum<HoldStatus>(Schema, "hold_status");
		modelBuilder.HasPostgresEnum<ListingType>(Schema, "listing_type");
		modelBuilder.HasPostgresEnum<OnlineConfirmationState>(Schema, "online_confirmation_state");
		modelBuilder.HasPostgresEnum<ParticipantRole>(Schema, "participant_role");
		modelBuilder.HasPostgresEnum<PaymentMode>(Schema, "payment_mode");
		modelBuilder.HasPostgresEnum<ResaleCreditCalc>(Schema, "resale_credit_calc");
		modelBuilder.HasPostgresEnum<ResaleCreditEntryType>(Schema, "resale_credit_entry");
		modelBuilder.HasPostgresEnum<ResaleMode>(Schema, "resale_mode");
		modelBuilder.HasPostgresEnum<ResaleOfferState>(Schema, "resale_offer_state");
		modelBuilder.HasPostgresEnum<ResaleOrigin>(Schema, "resale_origin");
		modelBuilder.HasPostgresEnum<ResaleScope>(Schema, "resale_scope");
		modelBuilder.HasPostgresEnum<ReservationKind>(Schema, "reservation_kind");
		modelBuilder.HasPostgresEnum<ReservationPriceFlag>(Schema, "reservation_price_flag");
		modelBuilder.HasPostgresEnum<ReservationStatus>(Schema, "reservation_status");
		modelBuilder.HasPostgresEnum<ResourceType>(Schema, "resource_type");
		modelBuilder.HasPostgresEnum<ServiceCategory>(Schema, "service_category");

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(DbContext).Assembly);

		ModuleDbContextConventions.ApplySoftDeleteQueryFilter(modelBuilder);
	}
}
