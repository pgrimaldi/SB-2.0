namespace Module.Booking.Application.Dtos;

/// <summary>Un file caricato dal gestionale.</summary>
public sealed record UploadedAttachment(string FileName, string ContentType, long Length, Stream Content);

/// <param name="Url">Link di download a tempo; null se il file è già stato cancellato (fine anno).</param>
public sealed record AttachmentDto(Guid Id, string FileName, string ContentType, long SizeBytes, DateTime CreatedAt, Guid? UploadedBy, string? Url, DateTime? FileDeletedAt);
