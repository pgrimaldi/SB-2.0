namespace Module.Switchboard.Domain.Enums;

/// <summary>Codice della sorgente storage di un file audio; la mappa origin → URL base vive in configurazione.</summary>
public enum MediaOrigin
{
	// "S3_Private" (non "S3Private"): il traduttore snake_case di Npgsql non inserisce l'underscore
	// fra una cifra e la lettera successiva, producendo "s3private" invece di "s3_private" —
	// l'underscore va scritto esplicitamente nell'identificatore C# (niente [PgName] in Npgsql 10).
	S3_Private = 0,
	External = 1
}
