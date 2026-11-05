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
	public string? VirtualTourUrl { get; private set; }
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
	/// <summary>«Mi piace» dei clienti (da Identity).</summary>
	public int LikeCount { get; private set; }
	public long? RegionId { get; private set; }
	public ShowcaseRegion? ShowcaseRegion { get; private set; }
	/// <summary>Regione scelta a mano dal systemadmin: non si ricalcola dall'indirizzo.</summary>
	public bool IsRegionManual { get; private set; }
	public Guid? UpdatedByUserPublicId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<BathhousePhoto> Photos { get; private set; } = new List<BathhousePhoto>();
	public ICollection<BathhouseAmenity> Amenities { get; private set; } = new List<BathhouseAmenity>();
	public ICollection<BathhouseBadge> Badges { get; private set; } = new List<BathhouseBadge>();

	public const int MaxPhotos = 30;

	private Bathhouse() { }

	public static Bathhouse Create(long propertyId, Guid? updatedBy) => new()
	{
		PublicId = Guid.CreateVersion7(),
		PropertyId = propertyId,
		CancellationPolicy = new CancellationPolicy(false, null),
		PetsPolicy = PetsPolicy.OnRequest,
		UpdatedByUserPublicId = updatedBy,
	};

	/// <summary>Dati della pagina «Informazioni stabilimento».</summary>
	public void Update(BathhouseInfo info, Guid? updatedBy)
	{
		if (info.OpenHoursFrom is { } from && info.OpenHoursTo is { } to && to <= from)
			throw new ArgumentException("Orario di chiusura prima dell'apertura.", nameof(info));
		if (!info.IsFreeCancellation && info.CancellationNoticeHours is not null)
			throw new ArgumentException("Preavviso valido solo con la cancellazione gratuita.", nameof(info));

		Name = info.Name.Trim();
		Description = info.Description;
		Regulation = info.Regulation;
		Address = info.Address;
		City = info.City;
		Cap = info.Cap;
		Province = info.Province;
		Region = info.Region;
		CountryId = info.CountryId;
		Latitude = info.Latitude;
		Longitude = info.Longitude;
		Phone = info.Phone;
		Email = info.Email;
		Website = info.Website;
		Facebook = info.Facebook;
		Instagram = info.Instagram;
		Whatsapp = info.Whatsapp;
		OpenHoursFrom = info.OpenHoursFrom;
		OpenHoursTo = info.OpenHoursTo;
		CancellationPolicy = new CancellationPolicy(info.IsFreeCancellation, info.CancellationNoticeHours);
		PetsPolicy = info.PetsPolicy;
		VirtualTourUrl = info.VirtualTourUrl;
		UpdatedByUserPublicId = updatedBy;
	}

	public void SetSlug(string slug) => Slug = slug;

	public void SetMainPhoto(long? mediaAssetId) => MainPhotoId = mediaAssetId;

	public void SetMenuPdf(long? mediaAssetId) => MenuPdfId = mediaAssetId;

	public void SetPriceListPdf(long? mediaAssetId) => PriceListPdfId = mediaAssetId;

	public BathhousePhoto AddPhoto(long mediaAssetId)
	{
		if (Photos.Count >= MaxPhotos)
			throw new InvalidOperationException("Galleria piena.");
		var photo = BathhousePhoto.Create(mediaAssetId, (short)(Photos.Count == 0 ? 0 : Photos.Max(p => p.SortOrder) + 1));
		Photos.Add(photo);
		return photo;
	}

	/// <summary>Dotazioni dichiarate: si aggiungono le nuove e si tolgono quelle non più spuntate.</summary>
	public void SetAmenities(IReadOnlyCollection<int> amenityIds)
	{
		foreach (var removed in Amenities.Where(a => !amenityIds.Contains(a.AmenityId)).ToList())
			Amenities.Remove(removed);
		foreach (var added in amenityIds.Where(id => Amenities.All(a => a.AmenityId != id)))
			Amenities.Add(BathhouseAmenity.Create(added));
	}

	public void ChangeLikes(int delta) => LikeCount = Math.Max(0, LikeCount + delta);

	/// <summary>Regione automatica dall'indirizzo, se il systemadmin non l'ha fissata a mano.</summary>
	public void AssignRegionAutomatically(long? regionId)
	{
		if (!IsRegionManual)
			RegionId = regionId;
	}

	/// <summary>Regione a mano; null torna all'assegnazione automatica.</summary>
	public void AssignRegionManually(long? regionId)
	{
		RegionId = regionId;
		IsRegionManual = regionId is not null;
	}

	public void SetBadges(IReadOnlyCollection<long> badgeIds)
	{
		foreach (var removed in Badges.Where(b => !badgeIds.Contains(b.BadgeId)).ToList())
			Badges.Remove(removed);
		foreach (var added in badgeIds.Where(id => Badges.All(b => b.BadgeId != id)))
			Badges.Add(BathhouseBadge.Create(added));
	}
}
