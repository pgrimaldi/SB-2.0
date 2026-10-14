namespace Module.Fiskaly.Domain.Enums;

/// <summary>Codice della sorgente da cui e servito un file di fiskaly_media_assets.</summary>
public enum FiskalyMediaOrigin
{
	// S3_us_east_2 (non S3UsEast2): il traduttore snake_case di default di Npgsql non inserisce un
	// underscore prima di una cifra finale, producendo "s3_us_east2" invece di "s3_us_east_2" — stesso
	// accorgimento di Module.Identity.StorageOrigin e Module.Notification.NotificationMediaOrigin.
	S3_us_east_2 = 0,
	Cloudfront = 1,
	External = 2
}
