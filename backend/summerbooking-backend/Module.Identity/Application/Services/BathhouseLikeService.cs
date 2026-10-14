using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Module.Identity.Domain.Entities;
using Shared.Contracts.Errors;
using Shared.Contracts.Events;
using Shared.Contracts.Identity;
using Shared.Dtos;
using Shared.Infrastructure;

namespace Module.Identity.Application.Services;

/// <summary>"Mi piace" alle strutture, sempre a nome dell'utente autenticato. Mettere e togliere sono idempotenti.</summary>
public sealed class BathhouseLikeService
{
	private readonly CurrentAccount _current;
	private readonly IBathhouseLikeRepository _likes;
	private readonly IUnitOfWork _uow;
	private readonly IIntegrationEventPublisher _events;
	private readonly IDateTimeProvider _clock;

	public BathhouseLikeService(CurrentAccount current, IBathhouseLikeRepository likes, IUnitOfWork uow, IIntegrationEventPublisher events, IDateTimeProvider clock)
	{
		_current = current;
		_likes = likes;
		_uow = uow;
		_events = events;
		_clock = clock;
	}

	public async Task<PagedResult<LikedBathhouseDto>> ListAsync(int page, int pageSize, CancellationToken ct = default)
	{
		if (page < 1 || pageSize is < 1 or > 100)
			throw new ValidationFailedException("pageSize", ValidationErrorCodes.OutOfRange);

		var account = await _current.GetAsync(ct);
		var (items, total) = await _likes.ListByUserAsync(account.Id, page, pageSize, ct);
		return new PagedResult<LikedBathhouseDto>(total, page, pageSize, items.Select(LikedBathhouseDto.FromEntity).ToList());
	}

	public async Task LikeAsync(Guid bathhouseId, CancellationToken ct = default)
	{
		var account = await _current.GetAsync(ct);
		if (await _likes.FindAsync(account.Id, bathhouseId, ct) is not null)
			return;

		var now = _clock.UtcNow;
		_likes.Add(BathhouseLike.Create(account.Id, bathhouseId, now));
		await _uow.SaveChangesAsync(ct);
		await _events.PublishAsync(new BathhouseLikeChanged(Guid.CreateVersion7(), now, bathhouseId, account.PublicId, true), ct);
	}

	public async Task UnlikeAsync(Guid bathhouseId, CancellationToken ct = default)
	{
		var account = await _current.GetAsync(ct);
		var like = await _likes.FindAsync(account.Id, bathhouseId, ct);
		if (like is null)
			return;

		_likes.Remove(like);
		await _uow.SaveChangesAsync(ct);
		await _events.PublishAsync(new BathhouseLikeChanged(Guid.CreateVersion7(), _clock.UtcNow, bathhouseId, account.PublicId, false), ct);
	}
}
