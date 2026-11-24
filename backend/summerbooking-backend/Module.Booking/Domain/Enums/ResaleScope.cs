namespace Module.Booking.Domain.Enums;

/// <summary>
/// Estensioni della rivendita — flag non esclusivi ("a cosa si applica lo sharing"), mappato come enum[].
/// </summary>
public enum ResaleScope
{
	/// <summary>Solo sulle prenotazioni stagionali.</summary>
	SeasonalOnly = 0,
	/// <summary>Su tutti gli articoli, non solo sugli ombrelloni.</summary>
	AllArticles = 1
}
