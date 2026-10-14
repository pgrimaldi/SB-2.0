namespace Module.Catalog.Domain.Enums;

/// <summary>
/// Sorgente da cui viene servito un file catalogato in <see cref="Entities.MediaAsset"/>. La mappa
/// origin -> URL base vive in configurazione (appsettings/Parameter Store), mai a DB.
/// </summary>
public enum MediaOrigin
{
	S3UsEast2 = 0,
	S3UsEast1 = 1,
	Cloudfront = 2,
	External = 3
}
