namespace Module.Notification.Domain.Enums;

/// <summary>
/// Codice della sorgente da cui è servito un file (mappa origin → URL base in configurazione).
/// Npgsql non espone un attributo per-membro per forzare l'etichetta PG: il nome membro incorpora
/// già l'underscore desiderato (S3_us_east_2) cosi il traduttore snake_case di default produce
/// esattamente "s3_us_east_2" (stesso accorgimento di Module.Identity.StorageOrigin).
/// </summary>
public enum NotificationMediaOrigin
{
	S3_us_east_2 = 0,
	Cloudfront = 1,
	External = 2
}
