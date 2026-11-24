using Module.Booking.Application.Dtos;
using Module.Booking.Application.Abstractions;
using Module.Booking.Domain.Entities;
using Shared.Infrastructure;

namespace Module.Booking.Application.Services;

/// <summary>Comandi già eseguiti, per chiave e stabilimento (rigiocati dal dispositivo dopo l'offline).</summary>
public sealed class IdempotencyService
{
	public const int KeyMaxLength = ProcessedCommand.KeyMaxLength;

	private readonly IProcessedCommandRepository _repository;
	private readonly IDateTimeProvider _clock;

	public IdempotencyService(IProcessedCommandRepository repository, IDateTimeProvider clock)
	{
		_repository = repository;
		_clock = clock;
	}

	public async Task<StoredCommandResponse?> FindAsync(Guid propertyId, string key, CancellationToken ct = default)
		=> await _repository.FindAsync(propertyId, key, ct) is { } done
			? new StoredCommandResponse(done.Method, done.Path, done.StatusCode, done.ResponseBody)
			: null;

	public Task RememberAsync(Guid propertyId, string key, string method, string path, int statusCode, string? body, CancellationToken ct = default)
		=> _repository.TryAddAsync(ProcessedCommand.Create(propertyId, key, method, path, statusCode, body, _clock.UtcNow), ct);
}
