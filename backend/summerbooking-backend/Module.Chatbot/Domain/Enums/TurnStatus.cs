namespace Module.Chatbot.Domain.Enums;

/// <summary>
/// Esito di un turno di conversazione: failed quando il provider LLM non ha prodotto una risposta
/// valida (errore, timeout, risposta vuota) — mai dedotto da una stringa vuota in output_text.
/// </summary>
public enum TurnStatus
{
	Answered = 0,
	Failed = 1
}
