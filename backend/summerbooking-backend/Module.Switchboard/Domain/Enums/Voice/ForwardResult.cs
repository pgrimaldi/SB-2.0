namespace Module.Switchboard.Domain.Enums.Voice;

/// <summary>Esito del tentativo di inoltro SIP, valorizzato solo se l'outcome della chiamata riguarda un forward.</summary>
public enum ForwardResult
{
	Answered = 0,
	NoAnswer = 1,
	Busy = 2,
	Failed = 3,
	SkippedNoEndpoint = 4
}
