namespace Module.Booking.Infrastructure.Infras;

/// <summary>Orari dei job di Booking (sezione <c>Booking:Jobs</c>); senza configurazione valgono i predefiniti.</summary>
public sealed class BookingJobOptions
{
	public const string Section = "Booking:Jobs";

	/// <summary>Ora italiana del job notturno.</summary>
	public TimeSpan NightlyAt { get; set; } = new(2, 30, 0);

	/// <summary>Ogni quanto si controllano le prenotazioni online scadute.</summary>
	public TimeSpan OnlineExpiryEvery { get; set; } = TimeSpan.FromMinutes(1);
}
