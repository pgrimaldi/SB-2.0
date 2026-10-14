namespace Module.Privacy.Domain.Enums;

/// <summary>
/// Tipo di richiesta dell'interessato: cancellazione (Art. 17 GDPR) o accesso/portabilità (Art. 15/20).
/// </summary>
public enum RequestKind
{
	Erasure = 0,
	Export = 1
}
