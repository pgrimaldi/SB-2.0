using Shared.Contracts;

namespace Module.Chatbot.Domain.Entities;

/// <summary>
/// Limiti di utilizzo e preferenza di provider LLM del chatbot per stabilimento — una riga per
/// property_id.
/// </summary>
public sealed class ChatbotManagerConfig : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public int DailyRequestLimit { get; private set; }
	public int HistoryPageSizeLimit { get; private set; }
	public long? LlmProviderId { get; private set; }
	public LlmProvider? LlmProvider { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ChatbotManagerConfig() { }
}
