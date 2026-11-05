using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Module.Catalog.Domain.ValueObjects;

namespace Module.Catalog.Application.Abstractions;

public interface IMapRepository
{
	/// <summary>Layout con le icone degli articoli personalizzati.</summary>
	Task<MapLayout?> FindLayoutAsync(long propertyId, CancellationToken ct = default);
	void AddLayout(MapLayout layout);

	/// <summary>Tutti gli elementi del reticolo, tracciati: le modifiche al reticolo li spostano insieme.</summary>
	Task<List<MapSpot>> ListSpotsAsync(long propertyId, CancellationToken ct = default);
	void AddSpots(IEnumerable<MapSpot> spots);
	void RemoveSpots(IEnumerable<MapSpot> spots);

	Task<IReadOnlyList<MapIcon>> ListIconsAsync(MapIconCategoryCode? category, CancellationToken ct = default);
	Task<MapIcon?> FindIconAsync(Guid publicId, CancellationToken ct = default);
	Task<IReadOnlyList<MapIcon>> ListIconsByAssetAsync(IReadOnlyCollection<long> mediaAssetIds, CancellationToken ct = default);
	Task<IReadOnlyList<MapIcon>> ListDefaultIconsAsync(CancellationToken ct = default);

	Task<IReadOnlyList<MapBeachElement>> ListBeachElementsAsync(bool activeOnly, CancellationToken ct = default);
	Task<MapBeachElement?> FindBeachElementAsync(Guid publicId, CancellationToken ct = default);
}

/// <summary>File del catalogo: riferimenti per comporre gli URL e nuovi caricamenti.</summary>
public interface IMediaAssetRepository
{
	Task<IReadOnlyDictionary<long, MediaRef>> FindRefsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default);
	void Add(MediaAsset asset);
}

/// <summary>URL pubblico di un file del catalogo.</summary>
public interface IMediaUrls
{
	/// <summary>Origine -> indirizzo base, per comporre gli URL dai riferimenti { origin, path }.</summary>
	IReadOnlyDictionary<string, string> Origins { get; }
	string? Resolve(MediaRef? reference);
}

public sealed record StoredFile(MediaRef Ref, byte[] Sha256, int ByteSize, string ContentType);

/// <summary>Archivio dei file caricati per gli stabilimenti (foto, PDF, sfondo della mappa).</summary>
public interface IPropertyFileStorage
{
	Task<StoredFile> SaveAsync(string fileName, Stream content, string contentType, CancellationToken ct = default);
}
