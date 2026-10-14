using Module.ZTest.Domain.Entities;

namespace Module.ZTest.Infrastructure;

/// <summary>
/// Contesto di persistenza del modulo (esempio, in-memory).
/// In un modulo reale erediterebbe da Microsoft.EntityFrameworkCore.DbContext,
/// esporrebbe i DbSet delle entita aggregate e applicherebbe le classi in
/// Infrastructure/Configurations tramite ApplyConfigurationsFromAssembly.
/// </summary>
public sealed class DbContext
{
	// Con EF Core: public DbSet<Reservation> Reservations { get; set; }
	public ICollection<Reservation> Reservations { get; } = new List<Reservation>();
}
