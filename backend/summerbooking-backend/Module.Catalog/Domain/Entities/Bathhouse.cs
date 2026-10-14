using Module.Catalog.Domain.Enums;
using Module.Catalog.Domain.ValueObjects;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Vetrina pubblica dello stabilimento: nome commerciale, indirizzo, contatti, descrizione, foto,
/// geolocalizzazione, orari, politiche. N:1 con Property (una property può avere più vetrine).
/// </summary>
public sealed class Bathhouse : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	public string? Name { get; private set; }
	public string? Slug { get; private set; }
	public string? Description { get; private set; }
	public string? Regulation { get; private set; }
	public string? Address { get; private set; }
	public string? City { get; private set; }
	public string? Cap { get; private set; }
	public string? Province { get; private set; }
	public string? Region { get; private set; }
	public short? CountryId { get; private set; }
	public Country? Country { get; private set; }
	public decimal? Latitude { get; private set; }
	public decimal? Longitude { get; private set; }
	public string? Phone { get; private set; }
	public string? Email { get; private set; }
	public string? Website { get; private set; }
	public string? Facebook { get; private set; }
	public string? Instagram { get; private set; }
	public string? Whatsapp { get; private set; }
	public TimeOnly? OpenHoursFrom { get; private set; }
	public TimeOnly? OpenHoursTo { get; private set; }
	public CancellationPolicy CancellationPolicy { get; private set; } = null!;
	public PetsPolicy PetsPolicy { get; private set; }
	public long? MainPhotoId { get; private set; }
	public MediaAsset? MainPhoto { get; private set; }
	public long? MenuPdfId { get; private set; }
	public MediaAsset? MenuPdf { get; private set; }
	public long? PriceListPdfId { get; private set; }
	public MediaAsset? PriceListPdf { get; private set; }
	public DateTime? PublishedAt { get; private set; }
	public Guid? UpdatedByUserPublicId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<BathhousePhoto> Photos { get; private set; } = new List<BathhousePhoto>();
	public ICollection<BathhouseAmenity> Amenities { get; private set; } = new List<BathhouseAmenity>();

	private Bathhouse() { }
}
