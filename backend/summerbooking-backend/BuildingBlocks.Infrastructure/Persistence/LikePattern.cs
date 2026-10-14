namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>Pattern per <c>ILIKE</c> da un testo libero: <c>%</c>, <c>_</c> e <c>\</c> valgono come caratteri.</summary>
public static class LikePattern
{
	public static string Contains(string text)
		=> "%" + text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
}
