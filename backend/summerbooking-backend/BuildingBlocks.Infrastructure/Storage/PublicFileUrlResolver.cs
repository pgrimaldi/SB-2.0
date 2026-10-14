namespace BuildingBlocks.Infrastructure.Storage;

/// <summary>Sezione <c>Storage</c>: indirizzo pubblico di ogni origine (<c>s3_us_east_2</c>, <c>cloudfront</c>, …).</summary>
public sealed class PublicFileOptions
{
	public const string Section = "Storage";

	public Dictionary<string, string> PublicBaseUrls { get; set; } = new();
}

/// <summary>
/// URL pubblico di un file salvato come origine + percorso. L'origine
/// <c>external</c> (es. avatar social) ha già l'URL intero nel percorso.
/// </summary>
public sealed class PublicFileUrlResolver
{
	public const string ExternalOrigin = "external";

	private readonly PublicFileOptions _options;

	public PublicFileUrlResolver(PublicFileOptions options) => _options = options;

	public string? Resolve(string? origin, string? path)
	{
		if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(path))
			return null;
		if (origin == ExternalOrigin)
			return path;
		return _options.PublicBaseUrls.TryGetValue(origin, out var baseUrl)
			? $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}"
			: null;
	}
}
