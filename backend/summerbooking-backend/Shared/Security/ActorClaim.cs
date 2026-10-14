using System.Text.Json;

namespace Shared.Security;

/// <summary>Scrittura e lettura del claim <c>act</c> (RFC 8693): un oggetto con il <c>sub</c> di chi agisce.</summary>
public static class ActorClaim
{
	public static IDictionary<string, object> Create(Guid actorUserId)
		=> new Dictionary<string, object> { [AuthClaims.Subject] = actorUserId.ToString() };

	/// <summary>Accetta il valore come arriva dalle librerie JWT: testo JSON, <see cref="JsonElement"/> o dizionario.</summary>
	public static Guid? ReadSubject(object? value)
	{
		string? subject = value switch
		{
			string json when json.TrimStart().StartsWith('{') => FromElement(JsonDocument.Parse(json).RootElement),
			JsonElement element => FromElement(element),
			IDictionary<string, object> map => map.TryGetValue(AuthClaims.Subject, out var s) ? s?.ToString() : null,
			_ => null,
		};
		return Guid.TryParse(subject, out var id) ? id : null;
	}

	private static string? FromElement(JsonElement element)
		=> element.ValueKind == JsonValueKind.Object && element.TryGetProperty(AuthClaims.Subject, out var sub) ? sub.GetString() : null;
}
