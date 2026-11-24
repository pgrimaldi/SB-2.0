namespace Module.Booking.Domain.Entities;

/// <summary>
/// Comando già eseguito con la sua chiave di idempotenza (header <c>Idempotency-Key</c>): un dispositivo che lo rimanda
/// dopo l'offline riceve la stessa risposta, senza rifare l'operazione. Si cancella dopo 7 giorni.
/// </summary>
public sealed class ProcessedCommand
{
	public const int KeyMaxLength = 100;

	public Guid PropertyId { get; private set; }
	public string IdempotencyKey { get; private set; } = null!;
	public string Method { get; private set; } = null!;
	public string Path { get; private set; } = null!;
	public int StatusCode { get; private set; }
	public string? ResponseBody { get; private set; }
	public DateTime CreatedAt { get; private set; }

	private ProcessedCommand() { }

	public static ProcessedCommand Create(Guid propertyId, string key, string method, string path, int status, string? body, DateTime now)
		=> new()
		{
			PropertyId = propertyId, IdempotencyKey = key, Method = method, Path = path, StatusCode = status, ResponseBody = body, CreatedAt = now,
		};
}
