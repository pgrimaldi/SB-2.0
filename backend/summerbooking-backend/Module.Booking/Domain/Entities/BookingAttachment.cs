using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// File allegato a una prenotazione («Allega file»: PDF, JPEG, PNG, anche documenti). Al 31 dicembre il file si cancella
/// dall'archivio e resta il nome.
/// </summary>
public sealed class BookingAttachment : IAuditable, ISoftDeletable
{
	public const int MaxPerReservation = 10;
	public const long MaxBytes = 2 * 1024 * 1024;

	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long ReservationId { get; private set; }
	public Guid PropertyId { get; private set; }
	public string FileName { get; private set; } = null!;
	public string ContentType { get; private set; } = null!;
	public long SizeBytes { get; private set; }
	public string? StorageOrigin { get; private set; }
	public string? StoragePath { get; private set; }
	public string? Sha256 { get; private set; }
	public Guid? UploadedByUserId { get; private set; }
	public DateTime? FileDeletedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public Reservation Reservation { get; private set; } = null!;

	private BookingAttachment() { }

	public static BookingAttachment Create(
		Reservation reservation, string fileName, string contentType, long size, string origin, string path, string sha256, Guid? userId)
		=> new()
		{
			PublicId = Guid.CreateVersion7(),
			Reservation = reservation,
			PropertyId = reservation.PropertyId,
			FileName = fileName.Length > 255 ? fileName[..255] : fileName,
			ContentType = contentType,
			SizeBytes = size,
			StorageOrigin = origin,
			StoragePath = path,
			Sha256 = sha256,
			UploadedByUserId = userId,
		};

	public bool HasFile => StoragePath is not null;

	/// <summary>File cancellato dall'archivio (fine anno): il nome resta.</summary>
	public void FileDeleted(DateTime now)
	{
		StoragePath = null;
		FileDeletedAt = now;
	}
}
