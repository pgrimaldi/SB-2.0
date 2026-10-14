using Module.Chatbot.Domain.Enums;
using Shared.Contracts;

namespace Module.Chatbot.Domain.Entities;

/// <summary>
/// Uno scambio completo domanda/risposta tra un utente (operatore o, in futuro, cliente finale) e
/// l'assistente LLM.
/// </summary>
public sealed class ChatbotConversationTurn : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public ChannelKind Channel { get; private set; }
	public Guid? ActorUserId { get; private set; }
	public string? ActorSessionId { get; private set; }
	public TurnStatus Status { get; private set; }
	public long? LlmProviderId { get; private set; }
	public LlmProvider? LlmProvider { get; private set; }
	public string? LlmModelSnapshot { get; private set; }
	public string? ResponseLanguage { get; private set; }
	public string InputText { get; private set; } = null!;
	public string? OutputText { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ChatbotConversationTurn() { }
}
