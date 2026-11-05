namespace Module.Catalog.Application.Dtos;

public sealed record CountryDto(string Code, string Name);

public sealed record AmenityDto(string Code, string Name, string? IconUrl);

public sealed record AmenityCategoryDto(string Code, IReadOnlyList<AmenityDto> Amenities);

public sealed record ServiceIconDto(Guid Id, string Type, string Name, string? IconUrl);

/// <param name="Rates">Aliquote in percentuale; la prima è quella proposta.</param>
/// <param name="ExemptNatures">Natura da indicare scegliendo «Esente» (n1…n6).</param>
public sealed record VatOptionsDto(string CountryCode, IReadOnlyList<decimal> Rates, IReadOnlyList<string> ExemptNatures, string DefaultExemptNature);
