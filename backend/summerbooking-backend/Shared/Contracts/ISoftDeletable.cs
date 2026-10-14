namespace Shared.Contracts;

/// <summary>
/// Marker per le entita con soft-delete (colonna deleted_at). Il valore e scritto
/// esclusivamente da BuildingBlocks.Infrastructure.Persistence.SoftDeleteInterceptor, mai da un
/// setter pubblico: la Delete() applicativa resta responsabilita del livello BL, non
/// ancora implementato in questa fase (solo persistenza).
/// </summary>
public interface ISoftDeletable
{
	DateTime? DeletedAt { get; }
}
