using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Module.Booking.Application.Dtos;
using Module.Booking.Application.Services;
using Module.Booking.Application.Validators;
using Shared.Dtos;
using Shared.Security;

namespace Module.Booking.Endpoints;

/// <summary>
/// Endpoint HTTP (Minimal API) del modulo Booking, sotto <c>/api/booking</c>. Registrati dal progetto Boot:
/// app.MapBookingEndpoints(); Senza attributi un endpoint richiede il token utente (fallback policy dell'host).
/// </summary>
public static class Endpoints
{
	public const string GroupName = "booking";

	public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
	{
		var api = app.MapGroup("/api/booking").WithGroupName(GroupName);
		api.AddEndpointFilter<IdempotencyFilter>();
		MapReservations(api);
		MapOperations(api);
		MapGroups(api);
		MapMap(api);
		MapSearch(api);
		MapSharing(api);
		MapServicePlanners(api);
		MapOnline(api);
		api.MapGet("/sync", async (long? since, int? limit, SyncService service, CancellationToken ct)
			=> Results.Ok(await service.ChangesAsync(since ?? 0, limit ?? 500, ct)))
			.WithTags("Booking - Offline")
			.Produces<SyncDto>()
			.RequireAuthorization(Policy(Permissions.BookingsRead));
		MapTrash(api);
		return app;
	}

	private static void MapOperations(RouteGroupBuilder api)
	{
		var group = api.MapGroup("/reservations").WithTags("Booking - Rinnovo, dividi conto, scambio");

		group.MapPost("/{id:guid}/renew", async (Guid id, RenewalService service, CancellationToken ct)
			=> Results.Ok(await service.RenewAsync(id, ct)))
			.Produces<RenewedDto>()
			.RequireAuthorization(Policy(Permissions.BookingsMove));

		group.MapPost("/renew", async (BulkIdsRequest request, RenewalService service, CancellationToken ct)
			=> Results.Ok(await service.RenewManyAsync(request, ct)))
			.Produces<RenewResultDto>()
			.RequireAuthorization(Policy(Permissions.BookingsMove));

		group.MapPost("/{id:guid}/split", async (Guid id, SplitReservationRequest request, SplitService service, CancellationToken ct)
			=> Results.Ok(await service.SplitAsync(id, request, ct)))
			.Produces<SplitResultDto>()
			.RequireAuthorization(Policy(Permissions.BookingsWrite));

		group.MapPost("/{id:guid}/swap", async (Guid id, SwapReservationsRequest request, ReservationService service, CancellationToken ct)
			=> Results.Ok(await service.SwapAsync(id, request, ct)))
			.Produces<SwapResultDto>()
			.RequireAuthorization(Policy(Permissions.BookingsWrite));

		group.MapPost("/repeat-plan", async (RepeatPlanRequest request, SearchService service, CancellationToken ct)
			=> Results.Ok(await service.RepeatPlanAsync(request, ct)))
			.Produces<RepeatPlanDto>()
			.RequireAuthorization(Policy(Permissions.BookingsWrite));
	}

	private static void MapGroups(RouteGroupBuilder api)
	{
		var group = api.MapGroup("/groups").WithTags("Booking - Gruppi");

		group.MapGet("/", async ([AsParameters] GroupQuery query, GroupService service, CancellationToken ct)
			=> Results.Ok(await service.ListAsync(query, ct)))
			.Produces<PagedResult<GroupSummaryDto>>()
			.RequireAuthorization(Policy(Permissions.BookingsRead));

		group.MapGet("/{id:guid}", async (Guid id, GroupService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct)))
			.Produces<GroupDto>()
			.RequireAuthorization(Policy(Permissions.BookingsRead));

		group.MapPost("/", async (CreateGroupRequest request, GroupService service, CancellationToken ct) =>
		{
			var created = await service.CreateAsync(request, ct);
			return Results.Created($"/api/booking/groups/{created.Group.Id}", created);
		})
			.Produces<GroupChangeDto>(StatusCodes.Status201Created)
			.RequireAuthorization(Policy(Permissions.BookingsMove));

		group.MapPut("/{id:guid}", async (Guid id, RenameGroupRequest request, GroupService service, CancellationToken ct)
			=> Results.Ok(await service.RenameAsync(id, request, ct)))
			.Produces<GroupDto>()
			.RequireAuthorization(Policy(Permissions.BookingsMove));

		group.MapPost("/{id:guid}/members", async (Guid id, GroupMembersRequest request, GroupService service, CancellationToken ct)
			=> Results.Ok(await service.AddMembersAsync(id, request, ct)))
			.Produces<GroupChangeDto>()
			.RequireAuthorization(Policy(Permissions.BookingsMove));

		group.MapDelete("/{id:guid}/members/{reservationId:guid}", async (Guid id, Guid reservationId, GroupService service, CancellationToken ct)
			=> Results.Ok(await service.RemoveMemberAsync(id, reservationId, ct)))
			.Produces<GroupDto>()
			.RequireAuthorization(Policy(Permissions.BookingsMove));

		group.MapPost("/bulk-delete", async (BulkIdsRequest request, GroupService service, CancellationToken ct)
			=> Results.Ok(await service.DeleteAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(Policy(Permissions.BookingsMove));

		group.MapPost("/{id:guid}/payments", async (Guid id, GroupPaymentRequest request, GroupService service, CancellationToken ct)
			=> Results.Ok(await service.PayAsync(id, request, ct)))
			.Produces<GroupPaymentResultDto>()
			.RequireAuthorization(Policy(Permissions.PaymentsRecord));
	}

	private static void MapSharing(RouteGroupBuilder api)
	{
		var reservations = api.MapGroup("/reservations").WithTags("Booking - Sharing e assenze");

		reservations.MapPost("/{id:guid}/released-days", async (Guid id, ReleaseDaysRequest request, SharingService service, CancellationToken ct)
			=> Results.Ok(await service.ReleaseAsync(id, request, ct)))
			.Produces<ReservationDto>()
			.RequireAuthorization(Policy(Permissions.BookingsWrite));

		reservations.MapPost("/{id:guid}/released-days/withdraw", async (Guid id, WithdrawDaysRequest request, SharingService service, CancellationToken ct)
			=> Results.Ok(await service.WithdrawAsync(id, request, ct)))
			.Produces<ReservationDto>()
			.RequireAuthorization(Policy(Permissions.BookingsWrite));

		var group = api.MapGroup("/sharing").WithTags("Booking - Sharing e assenze");

		group.MapGet("/", async ([AsParameters] SharingQuery query, SharingService service, CancellationToken ct)
			=> Results.Ok(await service.ListAsync(query, ct)))
			.Produces<PagedResult<SharingRowDto>>()
			.RequireAuthorization(Policy(Permissions.BookingsRead));

		group.MapPost("/withdraw", async (WithdrawDaysRequest request, SharingService service, CancellationToken ct)
			=> Results.Ok(await service.WithdrawManyAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(Policy(Permissions.BookingsWrite));

		group.MapGet("/settings", async (SharingService service, CancellationToken ct) => Results.Ok(await service.GetSettingsAsync(ct)))
			.Produces<SharingSettingsDto>()
			.RequireAuthorization(Policy(Permissions.BookingsRead));

		group.MapPut("/settings", async (SharingSettingsDto request, SharingService service, CancellationToken ct)
			=> Results.Ok(await service.SaveSettingsAsync(request, ct)))
			.Produces<SharingSettingsDto>()
			.RequireAuthorization(Policy(Permissions.CatalogSettings));

		group.MapGet("/credits/{customerId:guid}", async (Guid customerId, SharingService service, CancellationToken ct)
			=> Results.Ok(await service.GetCreditsAsync(customerId, ct)))
			.Produces<CustomerCreditsDto>()
			.RequireAuthorization(Policy(Permissions.BookingsAmounts));

		group.MapPost("/credits/adjustments", async (CreditAdjustmentRequest request, SharingService service, CancellationToken ct)
			=> Results.Ok(await service.AdjustCreditsAsync(request, ct)))
			.Produces<CustomerCreditsDto>()
			.RequireAuthorization(Policy(Permissions.PaymentsRecord));
	}

	private static void MapOnline(RouteGroupBuilder api)
	{
		var site = api.MapGroup("/public/{slug}").WithTags("Booking - Prenotazione online");

		site.MapGet("/availability", async (string slug, DateOnly from, DateOnly to, string? dayPart, OnlineBookingService service, CancellationToken ct)
			=> Results.Ok(await service.AvailabilityAsync(slug, from, to, dayPart, ct)))
			.Produces<OnlineAvailabilityDto>()
			.AllowAnonymous();

		site.MapPost("/reservations", async (string slug, OnlineReservationRequest request, OnlineBookingService service, CancellationToken ct) =>
		{
			var created = await service.CreateAsync(slug, request, ct);
			return Results.Created($"/api/booking/me/reservations/{created.Reservation.Id}", created);
		})
			.Produces<OnlineReservationResultDto>(StatusCodes.Status201Created)
			.AllowAnonymous();

		var reservations = api.MapGroup("/reservations").WithTags("Booking - Prenotazione online");

		reservations.MapPost("/{id:guid}/confirm", async (Guid id, OnlineBookingService service, ReservationService reservationService, CancellationToken ct)
			=> Results.Ok(await service.ConfirmAsync(id, reservationService, ct)))
			.Produces<ReservationDto>()
			.RequireAuthorization(Policy(Permissions.BookingsOnline));

		reservations.MapPost("/{id:guid}/reject", async (Guid id, OnlineBookingService service, ReservationService reservationService, CancellationToken ct)
			=> Results.Ok(await service.RejectAsync(id, reservationService, ct)))
			.Produces<ReservationDto>()
			.RequireAuthorization(Policy(Permissions.BookingsOnline));

		var me = api.MapGroup("/me/reservations").WithTags("Booking - Area cliente");

		me.MapGet("/", async (CustomerAreaService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)))
			.Produces<IReadOnlyList<CustomerReservationDto>>();

		me.MapGet("/{id:guid}", async (Guid id, CustomerAreaService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct)))
			.Produces<CustomerReservationDto>();

		me.MapPost("/{id:guid}/cancel", async (Guid id, CustomerAreaService service, CancellationToken ct) => Results.Ok(await service.CancelAsync(id, ct)))
			.Produces<CustomerCancellationDto>();

		me.MapPost("/{id:guid}/released-days", async (Guid id, ReleaseDaysRequest request, CustomerAreaService service, CancellationToken ct)
			=> Results.Ok(await service.ShareAsync(id, request, ct)))
			.Produces<CustomerReservationDto>();
	}

	private static void MapServicePlanners(RouteGroupBuilder api)
	{
		var group = api.MapGroup("").WithTags("Booking - Servizi e tavoli");

		group.MapGet("/services/planner", async (DateOnly? date, ServicePlannerService service, CancellationToken ct)
			=> Results.Ok(await service.HourlyAsync(date, ct)))
			.Produces<ServicePlannerDto>()
			.RequireAuthorization(Policy(Permissions.BookingsPlanner));

		group.MapGet("/tables/planner", async (DateOnly? date, ServicePlannerService service, CancellationToken ct)
			=> Results.Ok(await service.TablesAsync(date, ct)))
			.Produces<TablePlannerDto>()
			.RequireAuthorization(Policy(Permissions.BookingsPlanner));
	}

	private static void MapSearch(RouteGroupBuilder api)
	{
		var group = api.MapGroup("/map").WithTags("Booking - Mappa e planner");

		group.MapGet("/free-ranges", async (DateOnly from, DateOnly to, Guid? sectorId, string? spotType, SearchService service, CancellationToken ct)
			=> Results.Ok(await service.FreeRangesAsync(from, to, sectorId, spotType, ct)))
			.Produces<FreeRangesDto>()
			.RequireAuthorization(Policy(Permissions.BookingsRead));

		group.MapGet("/orphans", async (OrphanReportService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)))
			.Produces<IReadOnlyList<OrphanLineDto>>()
			.RequireAuthorization(Policy(Permissions.BookingsRead));

		group.MapGet("/orphans/pdf", async (OrphanReportService service, CancellationToken ct) =>
			{
				var file = await service.PdfAsync(ct);
				return Results.File(file.Content, file.ContentType, file.FileName);
			})
			.Produces(StatusCodes.Status200OK, contentType: "application/pdf")
			.RequireAuthorization(Policy(Permissions.BookingsRead));

		group.MapGet("/guided-search", async ([AsParameters] GuidedSearchQuery query, SearchService service, CancellationToken ct)
			=> Results.Ok(await service.GuidedAsync(query, ct)))
			.Produces<GuidedSearchDto>()
			.RequireAuthorization(Policy(Permissions.BookingsRead));
	}

	private static string Policy(string permission) => Permissions.PolicyName(permission);

	/// <summary>Versione attesa dall'header If-Match ("123" o W/"123"); assente o illeggibile = nessun controllo.</summary>
	private static uint? IfMatch(HttpContext http)
		=> uint.TryParse(http.Request.Headers.IfMatch.ToString().Replace("W/", "").Trim('"', ' '), out var v) ? v : null;

	private static void MapReservations(RouteGroupBuilder api)
	{
		var group = api.MapGroup("/reservations").WithTags("Booking - Prenotazioni");

		group.MapGet("/", async ([AsParameters] ReservationQuery query, ReservationService service, CancellationToken ct)
			=> Results.Ok(await service.ListAsync(query, false, ct)))
			.Produces<PagedResult<ReservationSummaryDto>>()
			.RequireAuthorization(Policy(Permissions.BookingsList));

		group.MapGet("/{id:guid}", async (Guid id, HttpContext http, ReservationService service, CancellationToken ct) =>
		{
			var reservation = await service.GetAsync(id, ct);
			http.Response.Headers.ETag = $"\"{reservation.Version}\"";
			return Results.Ok(reservation);
		})
			.Produces<ReservationDto>()
			.RequireAuthorization(Policy(Permissions.BookingsRead));

		group.MapPost("/", async (CreateReservationRequest request, ReservationService service, CancellationToken ct) =>
		{
			var created = await service.CreateAsync(request, ct);
			return Results.Created($"/api/booking/reservations/{created.Id}", created);
		})
			.Produces<ReservationDto>(StatusCodes.Status201Created)
			.RequireAuthorization(Policy(Permissions.BookingsWrite));

		group.MapPut("/{id:guid}", async (Guid id, SaveReservationRequest request, HttpContext http, ReservationService service, CancellationToken ct)
			=> Results.Ok(await service.UpdateAsync(id, request, IfMatch(http), ct)))
			.Produces<ReservationDto>()
			.RequireAuthorization(Policy(Permissions.BookingsWrite));

		group.MapPost("/{id:guid}/move", async (Guid id, MoveReservationRequest request, ReservationService service, CancellationToken ct)
			=> Results.Ok(await service.MoveAsync(id, request, ct)))
			.Produces<ReservationDto>()
			.RequireAuthorization(Policy(Permissions.BookingsMove));

		group.MapDelete("/{id:guid}", async (Guid id, bool? refund, bool? voidReceipts, TrashService service, CancellationToken ct)
			=> Results.Ok(await service.DeleteAsync(new DeleteReservationsRequest([id], refund ?? false, voidReceipts ?? false), ct)))
			.Produces<DeleteResultDto>()
			.RequireAuthorization(Policy(Permissions.BookingsDelete));

		group.MapPost("/bulk-delete", async (DeleteReservationsRequest request, TrashService service, CancellationToken ct)
			=> Results.Ok(await service.DeleteAsync(request, ct)))
			.Produces<DeleteResultDto>()
			.RequireAuthorization(Policy(Permissions.BookingsDelete));

		group.MapPost("/{id:guid}/payments", async (Guid id, RecordPaymentRequest request, ReservationService service, CancellationToken ct)
			=> Results.Ok(await service.RecordPaymentAsync(id, request, ct)))
			.Produces<ReservationDto>()
			.RequireAuthorization(Policy(Permissions.PaymentsRecord));

		group.MapPost("/{id:guid}/mark-paid", async (Guid id, MarkPaidRequest request, ReservationService service, CancellationToken ct)
			=> Results.Ok(await service.MarkPaidAsync(id, request, ct)))
			.Produces<ReservationDto>()
			.RequireAuthorization(Policy(Permissions.PaymentsRecord));

		group.MapDelete("/{id:guid}/mark-paid", async (Guid id, ReservationService service, CancellationToken ct)
			=> Results.Ok(await service.UnmarkPaidAsync(id, ct)))
			.Produces<ReservationDto>()
			.RequireAuthorization(Policy(Permissions.PaymentsRecord));

		group.MapGet("/{id:guid}/attachments", async (Guid id, AttachmentService service, CancellationToken ct) => Results.Ok(await service.ListAsync(id, ct)))
			.Produces<IReadOnlyList<AttachmentDto>>()
			.RequireAuthorization(Policy(Permissions.BookingsRead));

		group.MapPost("/{id:guid}/attachments", async (Guid id, IFormFile file, AttachmentService service, CancellationToken ct) =>
		{
			await using var content = file.OpenReadStream();
			return Results.Ok(await service.UploadAsync(id, new UploadedAttachment(file.FileName, file.ContentType, file.Length, content), ct));
		})
			.Produces<AttachmentDto>()
			.DisableAntiforgery()
			.RequireAuthorization(Policy(Permissions.BookingsWrite));

		group.MapDelete("/{id:guid}/attachments/{attachmentId:guid}", async (Guid id, Guid attachmentId, AttachmentService service, CancellationToken ct) =>
		{
			await service.DeleteAsync(id, attachmentId, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(Policy(Permissions.BookingsWrite));

		group.MapPost("/{id:guid}/check-in", async (Guid id, ReservationService service, CancellationToken ct)
			=> Results.Ok(await service.CheckInAsync(id, ct)))
			.Produces<ReservationDto>()
			.RequireAuthorization(Policy(Permissions.BookingsCheckIn));

		group.MapDelete("/{id:guid}/check-in", async (Guid id, ReservationService service, CancellationToken ct)
			=> Results.Ok(await service.UndoCheckInAsync(id, ct)))
			.Produces<ReservationDto>()
			.RequireAuthorization(Policy(Permissions.BookingsCheckIn));

		group.MapPost("/{id:guid}/check-out", async (Guid id, ReservationService service, CancellationToken ct)
			=> Results.Ok(await service.CheckOutAsync(id, ct)))
			.Produces<ReservationDto>()
			.RequireAuthorization(Policy(Permissions.BookingsCheckIn));

		group.MapPost("/{id:guid}/entrances", async (Guid id, EntranceRequest request, ReservationService service, CancellationToken ct)
			=> Results.Ok(await service.AddEntranceAsync(id, request, ct)))
			.Produces<ReservationDto>()
			.RequireAuthorization(Policy(Permissions.BookingsCheckIn));

		group.MapDelete("/{id:guid}/entrances/{lineId:guid}", async (Guid id, Guid lineId, ReservationService service, CancellationToken ct)
			=> Results.Ok(await service.RemoveEntranceAsync(id, lineId, ct)))
			.Produces<ReservationDto>()
			.RequireAuthorization(Policy(Permissions.BookingsCheckIn));
	}

	private static void MapMap(RouteGroupBuilder api)
	{
		var group = api.MapGroup("").WithTags("Booking - Mappa e planner");

		group.MapGet("/map", async (DateOnly? date, MapService service, CancellationToken ct) => Results.Ok(await service.DayAsync(date, ct)))
			.Produces<MapDayDto>()
			.RequireAuthorization(Policy(Permissions.BookingsRead));

		group.MapGet("/planner", async (DateOnly from, DateOnly to, MapService service, CancellationToken ct)
			=> Results.Ok(await service.PlannerAsync(from, to, ct)))
			.Produces<PlannerDto>()
			.RequireAuthorization(Policy(Permissions.BookingsPlanner));

		group.MapGet("/availability", async (DateOnly from, DateOnly to, string? dayPart, Guid? excludeReservationId, MapService service, CancellationToken ct)
			=> Results.Ok(await service.AvailabilityAsync(from, to, dayPart, excludeReservationId, ct)))
			.Produces<IReadOnlyList<SpotAvailabilityDto>>()
			.RequireAuthorization(Policy(Permissions.BookingsRead));

		group.MapPost("/holds", async (CreateHoldsRequest request, MapService service, CancellationToken ct)
			=> Results.Created("/api/booking/planner", await service.CreateHoldsAsync(request, ct)))
			.Produces<IReadOnlyList<HoldDto>>(StatusCodes.Status201Created)
			.RequireAuthorization(Policy(Permissions.BookingsWrite));

		group.MapDelete("/holds/{id:guid}", async (Guid id, MapService service, CancellationToken ct) =>
		{
			await service.RemoveHoldAsync(id, ct);
			return Results.NoContent();
		})
			.RequireAuthorization(Policy(Permissions.BookingsWrite));
	}

	private static void MapTrash(RouteGroupBuilder api)
	{
		var group = api.MapGroup("").WithTags("Booking - Cestino e pulizia dati");

		group.MapGet("/trash", async ([AsParameters] ReservationQuery query, ReservationService service, CancellationToken ct)
			=> Results.Ok(await service.ListAsync(query, true, ct)))
			.Produces<PagedResult<ReservationSummaryDto>>()
			.RequireAuthorization(Policy(Permissions.BookingsDelete));

		group.MapPost("/trash/restore", async (BulkIdsRequest request, TrashService service, CancellationToken ct)
			=> Results.Ok(await service.RestoreAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(Policy(Permissions.BookingsDelete));

		group.MapPost("/trash/purge", async (BulkIdsRequest request, TrashService service, CancellationToken ct)
			=> Results.Ok(await service.PurgeAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(Policy(Permissions.BookingsCleanup));

		group.MapPost("/trash/empty", async (TrashService service, CancellationToken ct)
			=> Results.Ok(new { purged = await service.EmptyAsync(ct) }))
			.RequireAuthorization(Policy(Permissions.BookingsCleanup));

		group.MapPost("/cleanup/move-to-trash", async (CleanupMoveRequest request, TrashService service, CancellationToken ct)
			=> Results.Ok(await service.CleanupAsync(request, ct)))
			.Produces<BulkResult>()
			.RequireAuthorization(Policy(Permissions.BookingsCleanup));
	}
}
