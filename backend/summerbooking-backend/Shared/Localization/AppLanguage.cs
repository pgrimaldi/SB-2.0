namespace Shared.Localization;

/// <summary>
/// Lingue gestite dalla piattaforma: interfaccia, email, documenti ed export. L'italiano è la lingua di
/// default. Il valore numerico non si salva: a DB ogni modulo usa un proprio enum Postgres con le
/// etichette di <see cref="SupportedLanguages.Code"/> (<c>it</c>, <c>en</c>, …).
/// </summary>
public enum AppLanguage
{
	It = 0,
	En = 1,
	Fr = 2,
	Es = 3,
	De = 4,
	/// <summary>Cinese semplificato.</summary>
	Zh = 5,
	/// <summary>Arabo: unica lingua scritta da destra a sinistra.</summary>
	Ar = 6
}
