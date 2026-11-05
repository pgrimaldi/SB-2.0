using Module.Catalog.Application.Abstractions;

namespace Module.Catalog.Application.Services;

/// <summary>URL pubblici dei file del catalogo, risolti in blocco.</summary>
public sealed class CatalogMedia
{
	private readonly IMediaAssetRepository _assets;
	private readonly IMediaUrls _urls;

	public CatalogMedia(IMediaAssetRepository assets, IMediaUrls urls)
	{
		_assets = assets;
		_urls = urls;
	}

	public async Task<IReadOnlyDictionary<long, string?>> UrlsAsync(IEnumerable<long?> ids, CancellationToken ct = default)
	{
		var refs = await _assets.FindRefsAsync(ids.Where(i => i is not null).Select(i => i!.Value).Distinct().ToList(), ct);
		return refs.ToDictionary(r => r.Key, r => _urls.Resolve(r.Value));
	}
}

public static class MediaUrlExtensions
{
	public static string? UrlOf(this IReadOnlyDictionary<long, string?> urls, long? id)
		=> id is { } value ? urls.GetValueOrDefault(value) : null;
}
