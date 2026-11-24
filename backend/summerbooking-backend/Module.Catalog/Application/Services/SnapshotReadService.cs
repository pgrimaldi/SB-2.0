using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>
/// Lettura degli snapshot di configurazione per gli endpoint: quello dello stabilimento del token, quelli pubblici (vetrina e
/// mappa) per slug, e la rigenerazione a mano del systemadmin. Uno snapshot che manca si costruisce al volo.
/// </summary>
public sealed class SnapshotReadService
{
	private static readonly ConfigSnapshotKind[] PublicKinds = [ConfigSnapshotKind.PublicShowcase, ConfigSnapshotKind.BeachMap];

	private readonly CurrentProperty _property;
	private readonly IConfigSnapshotRepository _snapshots;
	private readonly IPublicCatalogRepository _publicCatalog;
	private readonly IPropertyRepository _properties;
	private readonly ConfigSnapshotService _rebuild;

	public SnapshotReadService(
		CurrentProperty property, IConfigSnapshotRepository snapshots, IPublicCatalogRepository publicCatalog, IPropertyRepository properties,
		ConfigSnapshotService rebuild)
	{
		_property = property;
		_snapshots = snapshots;
		_publicCatalog = publicCatalog;
		_properties = properties;
		_rebuild = rebuild;
	}

	public async Task<SnapshotContent> GetAsync(string kind, CancellationToken ct = default)
		=> await ServeAsync(await _property.IdAsync(ct), ParseKind(kind), ct);

	/// <summary>Solo vetrina e mappa, solo per gli stabilimenti pubblicati.</summary>
	public async Task<SnapshotContent> GetPublicAsync(string slug, string kind, CancellationToken ct = default)
	{
		var parsed = ParseKind(kind);
		if (!PublicKinds.Contains(parsed))
			throw DomainException.NotFound(CatalogErrors.SnapshotNotFound);
		var row = await _publicCatalog.FindPublishedBySlugAsync(slug.Trim().ToLowerInvariant(), ct)
			?? throw DomainException.NotFound(CatalogErrors.BathhouseNotFound);
		return await ServeAsync(row.PropertyId, parsed, ct);
	}

	public async Task<SnapshotRebuildResult> RebuildAsync(RebuildSnapshotsRequest request, CancellationToken ct = default)
	{
		List<long>? ids = null;
		if (request.PropertyIds is { Count: > 0 })
		{
			ids = [];
			foreach (var id in request.PropertyIds)
				ids.Add(await _properties.FindIdAsync(id, ct) ?? throw DomainException.NotFound(CatalogErrors.PropertyNotFound));
		}
		var kinds = request.Kinds is { Count: > 0 } ? request.Kinds.Select(ParseKind).ToList() : ConfigSnapshotService.AllKinds.ToList();
		return await _rebuild.RebuildAsync(ids, kinds, request.OnlyMissing, ct);
	}

	private async Task<SnapshotContent> ServeAsync(long propertyId, ConfigSnapshotKind kind, CancellationToken ct)
	{
		var row = await _snapshots.FindAsync(propertyId, kind, ct);
		if (row is null)
		{
			await _rebuild.RebuildAsync([propertyId], [kind], ct: ct);
			row = await _snapshots.FindAsync(propertyId, kind, ct) ?? throw DomainException.NotFound(CatalogErrors.SnapshotNotFound);
		}
		return new SnapshotContent(row.Payload, $"\"{Convert.ToHexString(row.ContentHash)[..16].ToLowerInvariant()}\"");
	}

	private static ConfigSnapshotKind ParseKind(string kind)
		=> Codes.TryParse<ConfigSnapshotKind>(kind, out var parsed) ? parsed : throw DomainException.NotFound(CatalogErrors.SnapshotNotFound);
}
