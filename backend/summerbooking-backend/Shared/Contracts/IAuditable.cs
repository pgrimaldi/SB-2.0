namespace Shared.Contracts;

/// <summary>
/// Marker per le entita con created_at/updated_at. I valori sono scritti esclusivamente da
/// BuildingBlocks.Infrastructure.Persistence.AuditInterceptor tramite il ChangeTracker (mai un
/// setter pubblico e mai DateTime.UtcNow diretto in Infrastructure).
/// </summary>
public interface IAuditable
{
	DateTime CreatedAt { get; }
	DateTime UpdatedAt { get; }
}
