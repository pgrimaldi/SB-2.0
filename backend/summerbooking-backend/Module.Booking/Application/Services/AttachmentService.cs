using Module.Booking.Application.Dtos;
using Module.Booking.Application.Abstractions;
using Module.Booking.Domain;
using Module.Booking.Domain.Entities;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Localization;

namespace Module.Booking.Application.Services;

/// <summary>
/// «Allega file»: PDF, JPEG o PNG, al massimo 2 MB, al massimo 10 per prenotazione. Al 31 dicembre il job notturno
/// cancella i file dell'anno e lascia il nome.
/// </summary>
public sealed class AttachmentService
{
	private static readonly IReadOnlyDictionary<string, byte[]> Signatures = new Dictionary<string, byte[]>
	{
		["application/pdf"] = "%PDF"u8.ToArray(),
		["image/png"] = [0x89, 0x50, 0x4E, 0x47],
		["image/jpeg"] = [0xFF, 0xD8, 0xFF],
	};

	private readonly IReservationRepository _reservations;
	private readonly IAttachmentRepository _attachments;
	private readonly IAttachmentStorage _storage;
	private readonly SpotDirectory _spots;
	private readonly BookingScope _scope;
	private readonly BookingAudit _audit;

	public AttachmentService(
		IReservationRepository reservations, IAttachmentRepository attachments, IAttachmentStorage storage, SpotDirectory spots, BookingScope scope,
		BookingAudit audit)
	{
		_reservations = reservations;
		_attachments = attachments;
		_storage = storage;
		_spots = spots;
		_scope = scope;
		_audit = audit;
	}

	public async Task<IReadOnlyList<AttachmentDto>> ListAsync(Guid reservationId, CancellationToken ct = default)
	{
		var reservation = await LoadAsync(reservationId, ct);
		return (await _attachments.ListAsync(reservation.Id, ct)).Select(ToDto).ToList();
	}

	public async Task<AttachmentDto> UploadAsync(Guid reservationId, UploadedAttachment file, CancellationToken ct = default)
	{
		var reservation = await LoadAsync(reservationId, ct);
		if (file.Length <= 0 || file.Length > BookingAttachment.MaxBytes)
			throw DomainException.Unprocessable(BookingErrors.AttachmentTooLarge, new Dictionary<string, object?> { ["maxBytes"] = BookingAttachment.MaxBytes });
		using var buffer = new MemoryStream();
		await file.Content.CopyToAsync(buffer, ct);
		var bytes = buffer.ToArray();
		var type = Signatures.FirstOrDefault(s => bytes.AsSpan().StartsWith(s.Value)).Key;
		if (type is null)
			throw DomainException.Unprocessable(BookingErrors.AttachmentType);
		if ((await _attachments.ListAsync(reservation.Id, ct)).Count >= BookingAttachment.MaxPerReservation)
			throw DomainException.Unprocessable(BookingErrors.AttachmentLimit, new Dictionary<string, object?> { ["max"] = BookingAttachment.MaxPerReservation });

		buffer.Position = 0;
		var (origin, path, sha) = await _storage.SaveAsync(file.FileName, buffer, type, ct);
		var attachment = BookingAttachment.Create(reservation, Path.GetFileName(file.FileName), type, bytes.Length, origin, path, sha, _scope.UserId);
		_attachments.Add(attachment);
		await _attachments.SaveChangesAsync(ct);
		await _audit.RecordAsync(AuditActionCodes.Booking.AttachmentAdded, reservation, new Dictionary<string, object?> { ["fileName"] = attachment.FileName }, ct: ct);
		return ToDto(attachment);
	}

	public async Task DeleteAsync(Guid reservationId, Guid attachmentId, CancellationToken ct = default)
	{
		var reservation = await LoadAsync(reservationId, ct);
		var attachment = (await _attachments.ListAsync(reservation.Id, ct)).FirstOrDefault(a => a.PublicId == attachmentId)
			?? throw DomainException.NotFound(BookingErrors.AttachmentNotFound);
		if (attachment.StoragePath is { } path)
			await _storage.DeleteAsync(path, ct);
		attachment.FileDeleted(_scope.UtcNow);
		_attachments.Remove(attachment);
		await _attachments.SaveChangesAsync(ct);
		await _audit.RecordAsync(AuditActionCodes.Booking.AttachmentRemoved, reservation, new Dictionary<string, object?> { ["fileName"] = attachment.FileName }, ct: ct);
	}

	/// <summary>Job notturno: i file caricati negli anni passati si cancellano (resta il nome). Restituisce quanti.</summary>
	public async Task<int> PurgePastYearsAsync(DateTime nowUtc, CancellationToken ct = default)
	{
		var startOfYear = ItalianTime.ToUtc(new DateTime(ItalianTime.FromUtc(nowUtc).Year, 1, 1));
		var old = await _attachments.ListWithFilesBeforeAsync(startOfYear, 500, ct);
		foreach (var attachment in old)
		{
			await _storage.DeleteAsync(attachment.StoragePath!, ct);
			attachment.FileDeleted(nowUtc);
		}
		await _attachments.SaveChangesAsync(ct);
		return old.Count;
	}

	private async Task<Reservation> LoadAsync(Guid id, CancellationToken ct)
	{
		var reservation = await _reservations.GetAsync(_scope.PropertyId, id, includeTrashed: true, ct)
			?? throw DomainException.NotFound(BookingErrors.ReservationNotFound);
		if (!await _spots.CanSeeAsync(reservation, ct))
			throw DomainException.NotFound(BookingErrors.ReservationNotFound);
		return reservation;
	}

	private AttachmentDto ToDto(BookingAttachment a)
		=> new(a.PublicId, a.FileName, a.ContentType, a.SizeBytes, a.CreatedAt, a.UploadedByUserId,
			a.StoragePath is { } path ? _storage.GetDownloadUrl(path) : null, a.FileDeletedAt);
}
