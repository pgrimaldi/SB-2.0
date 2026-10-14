namespace Module.Identity.Domain.Enums;

/// <summary>
/// Provenienza di un riferimento a file (foto profilo, documento contrattuale): storage interno
/// (bucket/regione S3, eventualmente dietro CloudFront) o URL di un provider esterno. Usato da due
/// tabelle di questo solo modulo (identity_access_users, identity_account_contract_documents): per
/// non introdurre un tipo Postgres davvero cross-schema resta locale a "identity", non condiviso in
/// uno schema a parte. Npgsql non espone un attributo per-membro per
/// forzare l'etichetta PG (rimosso il predecente [PgName], inesistente nell'API attuale): i nomi
/// membro incorporano gia' gli underscore desiderati (S3_us_east_2 ecc.) cosi il traduttore
/// snake_case di default produce esattamente "s3_us_east_2"/"s3_us_east_1" (stesso accorgimento
/// usato in Module.FiscalPrint per Fp_81/Fp_90).
/// </summary>
public enum StorageOrigin
{
	S3_us_east_2 = 0,
	S3_us_east_1 = 1,
	Cloudfront = 2,
	External = 3
}
