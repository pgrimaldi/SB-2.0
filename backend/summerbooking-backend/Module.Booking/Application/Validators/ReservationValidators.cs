using Module.Booking.Domain;
using BuildingBlocks.Application.Validation;
using FluentValidation;
using Module.Booking.Application.Dtos;
using Shared.Contracts.Errors;
using Shared.Dtos;

namespace Module.Booking.Application.Validators;

public static class BookingRules
{
	public const int MaxDays = 366;
	public const int MaxSpots = 50;
	public const int MaxGuests = 50;
	public const int MaxTags = 20;
	public const int MaxPlannerDays = 62;
	public const int NameMaxLength = 200;
	public const int TrashRestoreDays = 30;
	public static readonly IReadOnlySet<string> DayParts = Shared.Contracts.Catalog.DayParts.All;
	public static readonly IReadOnlySet<string> AdjustmentTypes = new HashSet<string> { "percentage", "amount" };
	public static readonly IReadOnlySet<string> PaymentModes = new HashSet<string> { "cash", "credit_card", "bank_transfer", "other" };
	public static readonly IReadOnlySet<string> PaymentStatuses = Domain.PaymentStatuses.All;
	public static readonly IReadOnlySet<int> PageSizes = new HashSet<int> { 10, 20, 25, 50, 100, 200, 300 };
	public static readonly IReadOnlySet<string> SortFields = new HashSet<string> { "from", "bookingNumber", "total", "balanceDue", "createdAt", "deletedAt" };
}

public sealed class SaveReservationValidator : AbstractValidator<SaveReservationRequest>
{
	public SaveReservationValidator()
	{
		RuleFor(x => x).Must(x => x.To >= x.From && x.To.DayNumber - x.From.DayNumber < BookingRules.MaxDays)
			.WithErrorCode(ValidationErrorCodes.EndBeforeStart).OverridePropertyName("to");
		RuleFor(x => x.DayPart).Must(p => p is null || BookingRules.DayParts.Contains(p)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Spots).NotNull().Must(s => s.Count > 0).When(x => x.Services is not { Count: > 0 } && x.Tables is not { Count: > 0 })
			.WithErrorCode(ValidationErrorCodes.Required);
		RuleFor(x => x.Spots).Must(s => s is null || s.Count <= BookingRules.MaxSpots).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Services).Must(s => s is null || s.Count <= 200).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.Services).ChildRules(s =>
		{
			s.RuleFor(v => v.Quantity).InclusiveBetween((short)1, (short)500);
			s.RuleFor(v => v.Variant).MaximumLength(100);
			s.RuleFor(v => v.ManualPrice).InclusiveBetween(0m, 1_000_000m).When(v => v.ManualPrice is not null);
			s.RuleFor(v => v).Must(v => v.TimeSlotId is null == v.StartTime is null).WithErrorCode(ValidationErrorCodes.Required).OverridePropertyName("startTime");
		});
		RuleFor(x => x.Tables).Must(t => t is null || t.Count <= 50).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.Tables).ChildRules(t =>
		{
			t.RuleFor(v => v.Covers).InclusiveBetween((short)1, (short)500);
			t.RuleFor(v => v.TableSeats).Must(s => s is null || (s.Count <= 20 && s.All(n => n > 0))).WithErrorCode(ValidationErrorCodes.Invalid);
		});
		RuleFor(x => x).Must(x => (x.Services ?? []).All(s => s.Date is null || (s.Date >= x.From && s.Date <= x.To))
				&& (x.Tables ?? []).All(t => t.Date >= x.From && t.Date <= x.To))
			.WithErrorCode(ValidationErrorCodes.OutOfRange).OverridePropertyName("services");
		RuleFor(x => x.DiscountCode).MaximumLength(100);
		RuleFor(x => x.PriceOverride).InclusiveBetween(0m, 1_000_000m).When(x => x.PriceOverride is not null);
		RuleFor(x => x.Note).MaximumLength(Domain.Entities.Reservation.NoteMaxLength);
		RuleFor(x => x.CustomerTags).Must(t => t is null || (t.Count <= BookingRules.MaxTags && t.All(v => v.Length <= 64))).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Guests).Must(g => g is null || (g.Count <= BookingRules.MaxGuests && g.All(v => v.Length <= BookingRules.NameMaxLength)))
			.WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.ManualDiscount!).SetValidator(new AdjustmentValidator()).When(x => x.ManualDiscount is not null);
		RuleFor(x => x.Surcharge!).SetValidator(new AdjustmentValidator()).When(x => x.Surcharge is not null);
		RuleFor(x => x.Customer!).SetValidator(new CustomerInputValidator()).When(x => x.Customer is not null);
		RuleForEach(x => x.Spots).ChildRules(spot =>
		{
			spot.RuleFor(s => s).Must(s => (s.PriceListId is null) != (s.AgreementPriceListId is null))
				.WithErrorCode(ValidationErrorCodes.Required).OverridePropertyName("priceListId");
			spot.RuleFor(s => s).Must(s => s.AgreementPriceListId is null && s.VoucherPriceListId is null || s.AgreementPartnerId is not null)
				.WithErrorCode(ValidationErrorCodes.Required).OverridePropertyName("agreementPartnerId");
			spot.RuleFor(s => s).Must(s => (s.From is null) == (s.To is null) && (s.From is null || s.To >= s.From))
				.WithErrorCode(ValidationErrorCodes.EndBeforeStart).OverridePropertyName("to");
			spot.RuleFor(s => s.ManualPrice).InclusiveBetween(0m, 1_000_000m).When(s => s.ManualPrice is not null);
			spot.RuleFor(s => s.Seats).InclusiveBetween((short)1, (short)500).When(s => s.Seats is not null);
			spot.RuleForEach(s => s.Additionals).ChildRules(a =>
			{
				a.RuleFor(x => x.Quantity).InclusiveBetween((short)1, (short)100);
				a.RuleFor(x => x.Dates).Must(d => d is null || d.Count <= BookingRules.MaxDays).WithErrorCode(ValidationErrorCodes.Invalid);
			});
		});
		// Spot di una prenotazione dentro le date della prenotazione.
		RuleFor(x => x).Must(x => x.Spots.All(s => s.From is null || (s.From >= x.From && s.To <= x.To)))
			.WithErrorCode(ValidationErrorCodes.OutOfRange).OverridePropertyName("spots");
	}
}

public sealed class CreateReservationValidator : AbstractValidator<CreateReservationRequest>
{
	public CreateReservationValidator()
	{
		RuleFor(x => x.Reservation).NotNull().SetValidator(new SaveReservationValidator());
		RuleFor(x => x.PaidWith).Must(m => m is null || BookingRules.PaymentModes.Contains(m)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class AdjustmentValidator : AbstractValidator<AdjustmentInput>
{
	public AdjustmentValidator()
	{
		RuleFor(x => x.Type).Must(t => BookingRules.AdjustmentTypes.Contains(t)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Value).GreaterThanOrEqualTo(0m);
		RuleFor(x => x.Value).LessThanOrEqualTo(100m).When(x => x.Type == "percentage");
		RuleFor(x => x.Value).LessThanOrEqualTo(1_000_000m);
		RuleFor(x => x.Label).MaximumLength(120);
	}
}

public sealed class CustomerInputValidator : AbstractValidator<CustomerInput>
{
	public CustomerInputValidator()
	{
		RuleFor(x => x.FullName).MaximumLength(BookingRules.NameMaxLength);
		RuleFor(x => x.Email).OptionalEmail();
		RuleFor(x => x.Phone).PhoneNumber();
		RuleFor(x => x.PhoneCountryCode).PhonePrefix();
	}
}

public sealed class MoveReservationValidator : AbstractValidator<MoveReservationRequest>
{
	public MoveReservationValidator()
	{
		RuleFor(x => x).Must(x => (x.From is null) == (x.To is null) && (x.From is null || x.To >= x.From))
			.WithErrorCode(ValidationErrorCodes.EndBeforeStart).OverridePropertyName("to");
	}
}

public sealed class RecordPaymentValidator : AbstractValidator<RecordPaymentRequest>
{
	public RecordPaymentValidator()
	{
		RuleFor(x => x.Amount).GreaterThan(0m).LessThanOrEqualTo(1_000_000m).Must(a => decimal.Round(a, 2) == a).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Mode).Must(m => BookingRules.PaymentModes.Contains(m)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class MarkPaidValidator : AbstractValidator<MarkPaidRequest>
{
	public MarkPaidValidator()
		=> RuleFor(x => x.Mode).Must(m => BookingRules.PaymentModes.Contains(m)).WithErrorCode(ValidationErrorCodes.Invalid);
}

public sealed class DeleteReservationsValidator : AbstractValidator<DeleteReservationsRequest>
{
	public DeleteReservationsValidator()
		=> RuleFor(x => x.Ids).NotEmpty().Must(i => i.Count <= BulkIdsRequest.MaxIds).WithErrorCode(ValidationErrorCodes.Invalid);
}

public sealed class CleanupMoveValidator : AbstractValidator<CleanupMoveRequest>
{
	public CleanupMoveValidator()
	{
		RuleFor(x => x.Ids).NotEmpty().Must(i => i.Count <= BulkIdsRequest.MaxIds).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
	}
}

public sealed class BulkIdsValidator : AbstractValidator<BulkIdsRequest>
{
	public BulkIdsValidator()
		=> RuleFor(x => x.Ids).NotEmpty().Must(i => i.Count <= BulkIdsRequest.MaxIds).WithErrorCode(ValidationErrorCodes.Invalid);
}

public sealed class CreateHoldsValidator : AbstractValidator<CreateHoldsRequest>
{
	public CreateHoldsValidator()
	{
		RuleFor(x => x).Must(x => (x.SpotIds is { Count: > 0 }) != (x.Segments is { Count: > 0 }))
			.WithErrorCode(ValidationErrorCodes.Required).OverridePropertyName("spotIds");
		RuleFor(x => x.SpotIds).Must(s => s is null || s.Count <= 300).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Segments).Must(s => s is null || s.Count <= 300).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x).Must(x => x.To >= x.From && x.To.DayNumber - x.From.DayNumber < BookingRules.MaxDays)
			.WithErrorCode(ValidationErrorCodes.EndBeforeStart).OverridePropertyName("to").When(x => x.SpotIds is { Count: > 0 });
		RuleForEach(x => x.Segments).Must(s => s.To >= s.From && s.To.DayNumber - s.From.DayNumber < BookingRules.MaxDays)
			.WithErrorCode(ValidationErrorCodes.EndBeforeStart);
		RuleFor(x => x.DayPart).Must(p => p is null || BookingRules.DayParts.Contains(p)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class CreateGroupValidator : AbstractValidator<CreateGroupRequest>
{
	public CreateGroupValidator()
	{
		RuleFor(x => x.Name).NotEmpty().MaximumLength(Domain.Entities.BookingGroup.NameMaxLength);
		RuleFor(x => x.ReservationIds).NotEmpty().Must(i => i.Count <= BulkIdsRequest.MaxIds).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class RenameGroupValidator : AbstractValidator<RenameGroupRequest>
{
	public RenameGroupValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(Domain.Entities.BookingGroup.NameMaxLength);
}

public sealed class GroupMembersValidator : AbstractValidator<GroupMembersRequest>
{
	public GroupMembersValidator()
		=> RuleFor(x => x.ReservationIds).NotEmpty().Must(i => i.Count <= BulkIdsRequest.MaxIds).WithErrorCode(ValidationErrorCodes.Invalid);
}

public sealed class GroupPaymentValidator : AbstractValidator<GroupPaymentRequest>
{
	public GroupPaymentValidator()
	{
		RuleFor(x => x.Amount).GreaterThan(0m).LessThanOrEqualTo(1_000_000m).Must(a => decimal.Round(a, 2) == a).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Mode).Must(m => BookingRules.PaymentModes.Contains(m)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.ReservationIds).Must(i => i is null || (i.Count > 0 && i.Count <= BulkIdsRequest.MaxIds)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class SplitReservationValidator : AbstractValidator<SplitReservationRequest>
{
	public SplitReservationValidator()
	{
		RuleFor(x => x.Mode).Must(m => m is null or SplitModes.Proportional or SplitModes.Manual).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Shares).NotEmpty().When(x => x.Mode == SplitModes.Manual).WithErrorCode(ValidationErrorCodes.Required);
		RuleFor(x => x.Shares).Must(s => s is null || s.Count == 0).When(x => x.Mode != SplitModes.Manual).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.Shares).ChildRules(s =>
		{
			s.RuleFor(v => v.Total).GreaterThanOrEqualTo(0m).LessThanOrEqualTo(1_000_000m).Must(a => decimal.Round(a, 2) == a).WithErrorCode(ValidationErrorCodes.Invalid);
			s.RuleFor(v => v.Paid).GreaterThanOrEqualTo(0m).Must(a => a is null || decimal.Round(a.Value, 2) == a).WithErrorCode(ValidationErrorCodes.Invalid)
				.When(v => v.Paid is not null);
		});
	}
}

public sealed class SwapReservationsValidator : AbstractValidator<SwapReservationsRequest>
{
	public SwapReservationsValidator()
		=> RuleFor(x => x).Must(x => (x.From is null || x.To is null || x.To >= x.From) && x.LineId != x.WithLineId)
			.WithErrorCode(ValidationErrorCodes.Invalid).OverridePropertyName("withLineId");
}

public sealed class GuidedSearchValidator : AbstractValidator<GuidedSearchQuery>
{
	public GuidedSearchValidator()
	{
		RuleFor(x => x).Must(x => x.To >= x.From && x.To.DayNumber - x.From.DayNumber < BookingRules.MaxDays)
			.WithErrorCode(ValidationErrorCodes.EndBeforeStart).OverridePropertyName("to");
		RuleFor(x => x.DayPart).Must(p => p is null || BookingRules.DayParts.Contains(p)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.MaxMoves).InclusiveBetween(0, 100).When(x => x.MaxMoves is not null);
		RuleFor(x => x.SpotType).MaximumLength(40);
	}
}

public sealed class RepeatPlanValidator : AbstractValidator<RepeatPlanRequest>
{
	public RepeatPlanValidator()
	{
		RuleFor(x => x).Must(x => x.To >= x.From && x.To.DayNumber - x.From.DayNumber < BookingRules.MaxDays)
			.WithErrorCode(ValidationErrorCodes.EndBeforeStart).OverridePropertyName("to");
		RuleFor(x => x.Weekdays).NotEmpty().Must(w => w.All(d => d is >= 1 and <= 7)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.DayPart).Must(p => p is null || BookingRules.DayParts.Contains(p)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

/// <summary>Elenco dei gruppi: ricerca sul nome.</summary>
/// <param name="SortBy">name, reservations, createdAt.</param>
public sealed record GroupQuery(string? Search, int Page = 1, int PageSize = 25, string? SortBy = null, string? SortDir = null);

public sealed class GroupQueryValidator : AbstractValidator<GroupQuery>
{
	public GroupQueryValidator()
	{
		RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
		RuleFor(x => x.PageSize).Must(s => BookingRules.PageSizes.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.SortBy).Must(s => s is null or "name" or "reservations" or "createdAt").WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.SortDir).Must(s => s is null or SortDirections.Asc or SortDirections.Desc).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Search).MaximumLength(100);
	}
}

/// <summary>Filtri della pagina Prenotazioni, del Database e del Cestino.</summary>
/// <param name="Status">pending, confirmed, cancelled.</param>
/// <param name="PaymentStatus">paid, partial, unpaid.</param>
/// <param name="Channel">customer, manager.</param>
/// <param name="Search">Numero di prenotazione, nome, email o telefono del cliente, nome della postazione.</param>
/// <param name="SpotId">Storico di una postazione: le prenotazioni che la contengono.</param>
public sealed record ReservationQuery(
	DateOnly? From, DateOnly? To, string? Status, string? PaymentStatus, string? Channel, Guid? AgreementPartnerId, bool? Seasonal,
	string? Search, int Page = 1, int PageSize = 25, string? SortBy = null, string? SortDir = null, Guid? SpotId = null);

public sealed class ReservationQueryValidator : AbstractValidator<ReservationQuery>
{
	public ReservationQueryValidator()
	{
		RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
		RuleFor(x => x.PageSize).Must(s => BookingRules.PageSizes.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Status).Must(s => s is null or "pending" or "confirmed" or "cancelled").WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.PaymentStatus).Must(s => s is null || BookingRules.PaymentStatuses.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Channel).Must(s => s is null or "customer" or "manager").WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.SortBy).Must(s => s is null || BookingRules.SortFields.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.SortDir).Must(s => s is null or SortDirections.Asc or SortDirections.Desc).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Search).MaximumLength(100);
		RuleFor(x => x).Must(x => x.From is null || x.To is null || x.To >= x.From).WithErrorCode(ValidationErrorCodes.EndBeforeStart).OverridePropertyName("to");
	}
}

public sealed class OnlineReservationValidator : AbstractValidator<OnlineReservationRequest>
{
	public OnlineReservationValidator()
	{
		RuleFor(x => x.Customer).NotNull();
		RuleFor(x => x.Customer.FullName).NotEmpty().MaximumLength(BookingRules.NameMaxLength).When(x => x.Customer is not null);
		RuleFor(x => x.Customer.Email).OptionalEmail().When(x => x.Customer is not null && !string.IsNullOrEmpty(x.Customer.Email));
		RuleFor(x => x.Customer.Phone).PhoneNumber().When(x => x.Customer is not null);
		RuleFor(x => x.Customer.PhoneCountryCode).PhonePrefix().When(x => x.Customer is not null);
		RuleFor(x => x).Must(x => x.To >= x.From && x.To.DayNumber - x.From.DayNumber < BookingRules.MaxDays)
			.WithErrorCode(ValidationErrorCodes.EndBeforeStart).OverridePropertyName("to");
		RuleFor(x => x.DayPart).Must(p => p is null || BookingRules.DayParts.Contains(p)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Spots).NotNull().Must(s => s.Count <= BookingRules.MaxSpots).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x).Must(x => x.Spots is { Count: > 0 } || x.Services is { Count: > 0 }).WithErrorCode(ValidationErrorCodes.Required)
			.OverridePropertyName("spots");
		RuleFor(x => x.Spots).Must(s => s is null || s.Select(i => i.SpotId).Distinct().Count() == s.Count).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(40);
		RuleFor(x => x.DiscountCode).MaximumLength(100);
		RuleFor(x => x.Note).MaximumLength(1000);
		RuleFor(x => x.Services).Must(s => s is null || (s.Count <= 50 && s.All(i => i.ManualPrice is null && i.Quantity is >= 1 and <= 100)))
			.WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x).Must(x => (x.Services ?? []).All(s => s.Date is null || (s.Date >= x.From && s.Date <= x.To)))
			.WithErrorCode(ValidationErrorCodes.OutOfRange).OverridePropertyName("services");
	}
}

public sealed class SharingSettingsValidator : AbstractValidator<SharingSettingsDto>
{
	public SharingSettingsValidator()
	{
		RuleFor(x => x.MinBookingDays).InclusiveBetween(1, 366).When(x => x.MinBookingDays is not null);
		RuleFor(x => x.ManualCreditUnitsPerDay).InclusiveBetween(0, 1000);
		RuleFor(x => x.ManualCreditAmount).InclusiveBetween(0m, 100_000m).Must(a => decimal.Round(a, 2) == a).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.VoucherCreditAmount).InclusiveBetween(0m, 100_000m).Must(a => decimal.Round(a, 2) == a).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.CardTopUpAmount).InclusiveBetween(0m, 100_000m).Must(a => decimal.Round(a, 2) == a).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class ReleaseDaysValidator : AbstractValidator<ReleaseDaysRequest>
{
	public ReleaseDaysValidator()
	{
		RuleFor(x => x.Kind).Must(k => k is ReleaseKinds.Absence or ReleaseKinds.Sharing).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Dates).NotEmpty().Must(d => d.Count <= BookingRules.MaxDays).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.DayPart).Must(p => p is null || BookingRules.DayParts.Contains(p)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.RecalculatePrice).Equal(false).When(x => x.Kind == ReleaseKinds.Sharing).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class WithdrawDaysValidator : AbstractValidator<WithdrawDaysRequest>
{
	public WithdrawDaysValidator()
		=> RuleFor(x => x.OfferIds).NotEmpty().Must(i => i.Count <= BookingRules.MaxDays).WithErrorCode(ValidationErrorCodes.Invalid);
}

public sealed class SharingQueryValidator : AbstractValidator<SharingQuery>
{
	public SharingQueryValidator()
	{
		RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
		RuleFor(x => x.PageSize).Must(s => BookingRules.PageSizes.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Kind).Must(k => k is null or ReleaseKinds.Sharing or ReleaseKinds.Absence or ReleaseKinds.All).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Status).Must(k => k is null or "available" or "completed").WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Search).MaximumLength(100);
	}
}

public sealed class CreditAdjustmentValidator : AbstractValidator<CreditAdjustmentRequest>
{
	public CreditAdjustmentValidator()
	{
		RuleFor(x => x).Must(x => x.Units != 0 || x.Amount != 0).WithErrorCode(ValidationErrorCodes.Required).OverridePropertyName("units");
		RuleFor(x => x.Units).InclusiveBetween(-100_000, 100_000);
		RuleFor(x => x.Amount).InclusiveBetween(-100_000m, 100_000m).Must(a => decimal.Round(a, 2) == a).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleFor(x => x.Note).MaximumLength(255);
	}
}
