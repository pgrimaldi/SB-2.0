using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Shared.Realtime;
using Shared.Security;

namespace Boot.Web;

/// <summary>
/// Canale in tempo reale del gestionale: ogni connessione entra nel gruppo del suo stabilimento (dal token) e riceve
/// <c>changed</c> con <c>{ topic, ids }</c>. Una sola istanza dell'API per ora: con più istanze serve un backplane.
/// </summary>
[Authorize]
public sealed class PropertyHub : Hub
{
	public const string Path = "/hubs/property";

	public static string Group(Guid propertyId) => $"property:{propertyId}";

	public override async Task OnConnectedAsync()
	{
		if (Guid.TryParse(Context.User?.FindFirst(AuthClaims.PropertyId)?.Value, out var propertyId))
			await Groups.AddToGroupAsync(Context.ConnectionId, Group(propertyId));
		await base.OnConnectedAsync();
	}
}

/// <summary>Avvisi dei moduli inoltrati ai gestionali collegati.</summary>
public sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
	private readonly IHubContext<PropertyHub> _hub;

	public SignalRRealtimeNotifier(IHubContext<PropertyHub> hub) => _hub = hub;

	public Task NotifyAsync(Guid propertyId, string topic, IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
		=> _hub.Clients.Group(PropertyHub.Group(propertyId)).SendAsync("changed", new { topic, ids }, ct);
}
