using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Infrastructure;

namespace Module.Catalog.Application.Services;

/// <summary>Esito di una rigenerazione: snapshot scritti, invariati, tolti (non piu' dovuti), in errore.</summary>
public sealed record SnapshotRebuildResult(int Written, int Unchanged, int Removed, int Failed, IReadOnlyList<string> Errors);

/// <summary>
/// Rigenerazione degli snapshot di configurazione: dopo ogni salvataggio che li riguarda (per gli stabilimenti toccati),
/// di notte (tutti), all'avvio (solo quelli che mancano) e a mano dal systemadmin. Ogni stabilimento si costruisce in uno
/// scope suo, con lo stabilimento fissato. Gli snapshot del gestionale esistono per gli stabilimenti non importati, la
/// vetrina solo se pubblicata, le convenzioni solo se ce ne sono.
/// </summary>
public sealed class ConfigSnapshotService
{
	public static readonly IReadOnlyList<ConfigSnapshotKind> AllKinds = Enum.GetValues<ConfigSnapshotKind>();

	private readonly IServiceScopeFactory _scopes;
	private readonly IDateTimeProvider _clock;
	private readonly ILogger<ConfigSnapshotService> _logger;

	public ConfigSnapshotService(IServiceScopeFactory scopes, IDateTimeProvider clock, ILogger<ConfigSnapshotService> logger)
	{
		_scopes = scopes;
		_clock = clock;
		_logger = logger;
	}

	/// <param name="propertyIds">null = tutti gli stabilimenti.</param>
	/// <param name="onlyMissing">Solo le coppie (stabilimento, tipo) che non hanno ancora uno snapshot.</param>
	public async Task<SnapshotRebuildResult> RebuildAsync(
		IReadOnlyCollection<long>? propertyIds, IReadOnlyCollection<ConfigSnapshotKind> kinds, bool onlyMissing = false, CancellationToken ct = default)
	{
		IReadOnlyList<SnapshotTarget> targets;
		IReadOnlySet<(long, ConfigSnapshotKind)> existing;
		using (var scope = _scopes.CreateScope())
		{
			var repository = scope.ServiceProvider.GetRequiredService<IConfigSnapshotRepository>();
			targets = await repository.ListTargetsAsync(propertyIds, ct);
			existing = onlyMissing ? await repository.ListExistingAsync(ct) : new HashSet<(long, ConfigSnapshotKind)>();
		}
		int written = 0, unchanged = 0, removed = 0, failed = 0;
		var errors = new List<string>();
		foreach (var target in targets)
		{
			var wanted = kinds.Where(k => Applies(k, target) || !onlyMissing).Where(k => !existing.Contains((target.PropertyId, k))).ToList();
			if (wanted.Count == 0)
				continue;
			try
			{
				using var scope = _scopes.CreateScope();
				scope.ServiceProvider.GetRequiredService<CurrentProperty>().Use(target.PropertyId, target.PublicId);
				var builder = scope.ServiceProvider.GetRequiredService<ConfigSnapshotBuilder>();
				var repository = scope.ServiceProvider.GetRequiredService<IConfigSnapshotRepository>();
				foreach (var kind in wanted)
				{
					var row = await repository.FindAsync(target.PropertyId, kind, ct);
					var payload = Applies(kind, target) ? await builder.BuildAsync(kind, target.PublishedSlug, ct) : null;
					if (payload is null)
					{
						if (row is not null)
						{
							repository.Remove(row);
							removed++;
						}
						continue;
					}
					if (row is null)
					{
						row = ConfigSnapshot.Create(target.PropertyId, kind);
						repository.Add(row);
					}
					if (row.Replace(payload, SHA256.HashData(Encoding.UTF8.GetBytes(payload)), _clock.UtcNow))
						written++;
					else
						unchanged++;
				}
				await repository.SaveChangesAsync(ct);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				failed++;
				if (errors.Count < 20)
					errors.Add($"stabilimento {target.PropertyId}: {ex.Message}");
				_logger.LogWarning(ex, "Snapshot dello stabilimento {PropertyId} non ricostruiti", target.PropertyId);
			}
		}
		return new SnapshotRebuildResult(written, unchanged, removed, failed, errors);
	}

	private static bool Applies(ConfigSnapshotKind kind, SnapshotTarget target)
		=> kind == ConfigSnapshotKind.PublicShowcase ? target.PublishedSlug is not null : target.HasManagement;
}
