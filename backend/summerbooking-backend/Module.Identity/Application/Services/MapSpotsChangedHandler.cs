using Module.Identity.Application.Abstractions;
using Shared.Contracts.Catalog;
using Shared.Contracts.Events;

namespace Module.Identity.Application.Services;

/// <summary>
/// Postazioni tolte dalla mappa: spariscono dalle postazioni assegnate agli affiliati dello stabilimento. Le assegnazioni
/// scritte come posizione (<c>R{fila}C{colonna}</c>) restano, perché valgono anche per la postazione nuova nella stessa casella.
/// </summary>
public sealed class MapSpotsChangedHandler : IIntegrationEventHandler<MapSpotsChanged>
{
	private readonly IAccountRepository _accounts;
	private readonly IUnitOfWork _uow;

	public MapSpotsChangedHandler(IAccountRepository accounts, IUnitOfWork uow)
	{
		_accounts = accounts;
		_uow = uow;
	}

	public async Task HandleAsync(MapSpotsChanged e, CancellationToken ct = default)
	{
		if (e.Removed.Count == 0)
			return;
		var removed = e.Removed.Select(s => s.Id.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
		var changed = false;
		foreach (var account in await _accounts.ListByPropertyAsync(e.PropertyId, ct))
		{
			if (!account.ResourceScopes.Any(removed.Contains))
				continue;
			account.SetResourceScopes(account.IsResourceScopeRestricted, account.ResourceScopes.Where(s => !removed.Contains(s)).ToList());
			changed = true;
		}
		if (changed)
			await _uow.SaveChangesAsync(ct);
	}
}
