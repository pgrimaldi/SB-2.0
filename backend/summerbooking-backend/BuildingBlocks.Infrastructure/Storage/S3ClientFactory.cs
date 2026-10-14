using Amazon.Runtime;
using Amazon.S3;

namespace BuildingBlocks.Infrastructure.Storage;

/// <summary>Unico punto che costruisce un client S3: AWS vera, oppure un S3 compatibile come LocalStack.</summary>
/// <remarks>Usata dall'archivio di file, dall'archivio della retention e dalla coda delle cancellazioni.</remarks>
public static class S3ClientFactory
{
	/// <param name="endpointUrl">Valorizzato = LocalStack o altro S3 compatibile (attiva il path-style); vuoto = AWS.</param>
	/// <param name="accessKeyId">Vuoto = credenziali dell'ambiente (ruolo del task ECS).</param>
	public static AmazonS3Client Create(string region, string? endpointUrl, string? accessKeyId, string? secretAccessKey)
	{
		// ⚠️ L'ORDINE NON E' STILISTICO: nell'SDK AWS assegnare RegionEndpoint AZZERA ServiceURL.
		// Invertendole le richieste andrebbero ad AWS vera invece che all'endpoint configurato, e
		// l'errore visibile ("The AWS Access Key Id you provided does not exist") sembra di credenziali.
		var config = new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region) };
		if (!string.IsNullOrWhiteSpace(endpointUrl))
		{
			config.ServiceURL = endpointUrl;
			// LocalStack non risolve i bucket come sottodominio (<bucket>.localhost).
			config.ForcePathStyle = true;
		}

		return string.IsNullOrWhiteSpace(accessKeyId)
			? new AmazonS3Client(config)
			: new AmazonS3Client(new BasicAWSCredentials(accessKeyId, secretAccessKey), config);
	}
}
