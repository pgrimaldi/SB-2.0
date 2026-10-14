using Shared.Contracts;

namespace Module.Chatbot.Domain.Entities;

/// <summary>
/// Catalogo dei modelli/provider LLM utilizzabili dal modulo Chatbot per generare le risposte — una
/// riga per modello selezionabile, non per vendor.
/// </summary>
public sealed class LlmProvider : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public string Code { get; private set; } = null!;
	public string Vendor { get; private set; } = null!;
	public string? DisplayName { get; private set; }
	public bool IsSelectable { get; private set; }
	public DateOnly? RetiresOn { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private LlmProvider() { }
}
