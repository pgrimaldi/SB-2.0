using Shared.Contracts.Booking;
using Shared.Contracts.Events;

namespace Module.Catalog.Application.Services;

/// <summary>
/// Credito dello sharing maturato dal titolare: il voucher automatico (<c>VoucherAmount</c>) diventa un voucher dello
/// stabilimento intestato al cliente. Da fare (emissione e storno del voucher); la ricarica della tessera la gestisce Payment.
/// </summary>
public sealed class ResaleVoucherHandler : IIntegrationEventHandler<ResaleCreditEarned>, IIntegrationEventHandler<ResaleCreditReversed>
{
	public Task HandleAsync(ResaleCreditEarned @event, CancellationToken ct = default) => Task.CompletedTask;

	public Task HandleAsync(ResaleCreditReversed @event, CancellationToken ct = default) => Task.CompletedTask;
}
