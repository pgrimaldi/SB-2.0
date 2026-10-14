using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Contatore progressivo del numero prenotazione per stabilimento (1 riga per property); booking_number
/// si compone come number_prefix || '-' || lpad(last_sequence, 10, '0'). Tabella di registro/contatore:
/// non implementa ISoftDeletable, deleted_at è mappato solo per uniformità di schema.
/// </summary>
public sealed class BookingNumberSequence : IAuditable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public string NumberPrefix { get; private set; } = null!;
	public long LastSequence { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private BookingNumberSequence() { }
}
