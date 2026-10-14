namespace Module.Chatbot.Domain.Enums;

/// <summary>
/// Canale applicativo da cui parte un turno di conversazione: manager (gestionale, sempre
/// autenticato) o customer (futuro assistente lato cliente finale, mai implementato, eventualmente
/// anonimo).
/// </summary>
public enum ChannelKind
{
	Manager = 0,
	Customer = 1
}
