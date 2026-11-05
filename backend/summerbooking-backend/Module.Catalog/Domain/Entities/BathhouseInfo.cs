using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Domain.Entities;

public sealed record BathhouseInfo(
	string Name, string? Description, string? Regulation, string? Address, string? City, string? Cap, string? Province,
	string? Region, short? CountryId, decimal? Latitude, decimal? Longitude, string? Phone, string? Email, string? Website,
	string? Facebook, string? Instagram, string? Whatsapp, TimeOnly? OpenHoursFrom, TimeOnly? OpenHoursTo,
	bool IsFreeCancellation, short? CancellationNoticeHours, PetsPolicy PetsPolicy, string? VirtualTourUrl);
