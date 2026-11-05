namespace Shared.Localization;

/// <summary>Lingua della richiesta per i contenuti di dominio tradotti (header <c>Accept-Language</c>).</summary>
public interface IRequestLanguage
{
	/// <summary>Prima lingua gestita fra quelle accettate dal client, altrimenti quella di default.</summary>
	AppLanguage Language { get; }
}
