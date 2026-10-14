namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Read-model di ricerca pubblica: una riga per vetrina pubblicata, ricostruibile da catalog_bathhouses +
/// catalog_pricing_* + catalog_bathhouse_amenities + identity_customer_bathhouse_likes (C5). Proiezione
/// denormalizzata scritta solo dal processo di rebuild, mai da un aggregate di dominio. Chiave naturale
/// (nessun id surrogato), nessuna FK reale in uscita (id copiati per valore). Non implementa
/// IAuditable/ISoftDeletable nel senso pieno: ha timestamp ma la visibilità è governata da
/// <see cref="IsPublished"/>, non da soft-delete funzionale.
/// Nota di implementazione: <see cref="Latitude"/>/<see cref="Longitude"/> sostituiscono la colonna
/// PostGIS geography(Point,4326) prevista in analisi — nessuna dipendenza NetTopologySuite è presente altrove
/// nella solution; introdurla per questa sola colonna è fuori scopo per lo strato di sola migrazione DB.
/// <see cref="Card"/> è il documento JSON grezzo (jsonb) anziché un JsonDocument tipizzato.
/// </summary>
public sealed class SearchBathhouse
{
	public Guid BathhousePublicId { get; private set; }
	public long PropertyId { get; private set; }
	public string Name { get; private set; } = null!;
	public string? City { get; private set; }
	public string? Region { get; private set; }
	public string? Province { get; private set; }
	public string? Slug { get; private set; }
	public double? Latitude { get; private set; }
	public double? Longitude { get; private set; }
	public decimal? MinPrice { get; private set; }
	public short[] AmenityIds { get; private set; } = [];
	public int LikeCount { get; private set; }
	public bool IsPublished { get; private set; }
	public string Card { get; private set; } = "{}";
	public byte[] ContentHash { get; private set; } = [];
	public DateTime BuiltAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private SearchBathhouse() { }
}
