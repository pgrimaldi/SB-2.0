using Module.Catalog.Domain;
using Shared.Contracts.Errors;

namespace Module.Catalog.Application.Services;

/// <summary>Tipi e dimensioni ammessi per i file caricati.</summary>
public static class UploadRules
{
	public const long MaxImageBytes = 5 * 1024 * 1024;
	public const long MaxPdfBytes = 10 * 1024 * 1024;
	public static readonly IReadOnlySet<string> ImageTypes = new HashSet<string> { "image/png", "image/jpeg", "image/webp" };
	public static readonly IReadOnlySet<string> PdfTypes = new HashSet<string> { "application/pdf" };

	public static void EnsureImage(string contentType, long length, string errorCode = CatalogErrors.InvalidImage)
		=> Ensure(contentType, length, ImageTypes, MaxImageBytes, errorCode);

	public static void EnsurePdf(string contentType, long length)
		=> Ensure(contentType, length, PdfTypes, MaxPdfBytes, CatalogErrors.InvalidPdf);

	private static void Ensure(string contentType, long length, IReadOnlySet<string> types, long maxBytes, string errorCode)
	{
		if (!types.Contains(contentType) || length is <= 0 || length > maxBytes)
			throw DomainException.Unprocessable(errorCode, new Dictionary<string, object?>
			{
				["maxBytes"] = maxBytes,
				["contentTypes"] = types,
			});
	}
}

/// <summary>File ricevuto da un form multipart.</summary>
public sealed record UploadedFile(string FileName, string ContentType, long Length, Stream Content);
