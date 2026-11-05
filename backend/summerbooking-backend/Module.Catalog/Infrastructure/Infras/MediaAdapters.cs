using BuildingBlocks.Infrastructure.Storage;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Domain.Enums;
using Module.Catalog.Domain.ValueObjects;

namespace Module.Catalog.Infrastructure.Infras;

/// <summary>Codici delle origini in configurazione (<c>Storage:PublicBaseUrls</c>), comuni a tutti i moduli.</summary>
internal static class MediaOrigins
{
	private static readonly Dictionary<MediaOrigin, string> Codes = new()
	{
		[MediaOrigin.S3UsEast2] = "s3_us_east_2",
		[MediaOrigin.S3UsEast1] = "s3_us_east_1",
		[MediaOrigin.Cloudfront] = "cloudfront",
		[MediaOrigin.External] = PublicFileUrlResolver.ExternalOrigin,
	};

	public static string Code(MediaOrigin origin) => Codes[origin];

	public static MediaOrigin Parse(string code)
		=> Codes.FirstOrDefault(c => c.Value == code) is { Value: not null } match
			? match.Key
			: throw new InvalidOperationException($"{PropertyFileStorage.Section}:Origin '{code}' non è un valore di catalog.media_origin.");
}

public sealed class MediaUrls : IMediaUrls
{
	private readonly PublicFileUrlResolver _resolver;

	public MediaUrls(PublicFileUrlResolver resolver) => _resolver = resolver;

	public IReadOnlyDictionary<string, string> Origins => _resolver.Origins;

	public string? Resolve(MediaRef? reference)
		=> reference is null ? null : _resolver.Resolve(MediaOrigins.Code(reference.Origin), reference.Path);
}

/// <summary>File caricati per gli stabilimenti sull'archivio di file comune.</summary>
public sealed class PropertyFileStorage : IPropertyFileStorage
{
	public const string Section = "Catalog:Media";

	private readonly IObjectStorage _storage;

	public PropertyFileStorage(IObjectStorage storage) => _storage = storage;

	public async Task<StoredFile> SaveAsync(string fileName, Stream content, string contentType, CancellationToken ct = default)
	{
		var origin = MediaOrigins.Parse(_storage.Origin);
		using var buffer = new MemoryStream();
		await content.CopyToAsync(buffer, ct);
		buffer.Position = 0;

		var stored = await _storage.SaveAsync(fileName, buffer, contentType, ct);
		return new StoredFile(new MediaRef(origin, stored.Path), Convert.FromHexString(stored.Sha256), (int)buffer.Length, contentType);
	}
}
