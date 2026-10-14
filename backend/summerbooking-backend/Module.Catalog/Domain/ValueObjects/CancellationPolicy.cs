namespace Module.Catalog.Domain.ValueObjects;

/// <summary>
/// Politica di cancellazione mostrata al cliente. NoticeHours ha senso solo se IsFreeCancellation = true
/// (CHECK a DB in BathhouseConfiguration).
/// </summary>
public sealed record CancellationPolicy(bool IsFreeCancellation, short? NoticeHours);
