namespace Module.Switchboard.Domain.Enums;

/// <summary>Da quale numero il SIP endpoint vede arrivare la chiamata inoltrata.</summary>
public enum ForwardCallerIdMode
{
	Caller = 0,
	Centralino = 1,
	Custom = 2
}
