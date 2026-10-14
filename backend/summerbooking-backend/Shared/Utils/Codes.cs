using System.Text;

namespace Shared.Utils;

/// <summary>Enum ↔ codice snake_case stabile usato nelle API (es. <c>ManagementBooking</c> ↔ <c>management_booking</c>).</summary>
public static class Codes
{
	public static string Of<TEnum>(TEnum value) where TEnum : struct, Enum
	{
		var name = value.ToString();
		var sb = new StringBuilder(name.Length + 8);
		for (var i = 0; i < name.Length; i++)
		{
			var c = name[i];
			if (char.IsUpper(c) && i > 0 && name[i - 1] != '_')
				sb.Append('_');
			sb.Append(char.ToLowerInvariant(c));
		}
		return sb.ToString();
	}

	public static bool TryParse<TEnum>(string? code, out TEnum value) where TEnum : struct, Enum
	{
		foreach (var candidate in Enum.GetValues<TEnum>())
		{
			if (string.Equals(Of(candidate), code, StringComparison.Ordinal))
			{
				value = candidate;
				return true;
			}
		}
		value = default;
		return false;
	}
}
