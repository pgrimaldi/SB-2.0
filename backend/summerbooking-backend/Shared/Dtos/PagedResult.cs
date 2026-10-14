namespace Shared.Dtos;

public sealed record PagedResult<T>(int Total, int Page, int PageSize, IReadOnlyList<T> Items);

/// <summary>Valori del parametro di ordinamento <c>sortDir</c>.</summary>
public static class SortDirections
{
	public const string Asc = "asc";
	public const string Desc = "desc";
}
