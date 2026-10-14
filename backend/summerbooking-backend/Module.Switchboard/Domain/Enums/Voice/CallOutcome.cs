namespace Module.Switchboard.Domain.Enums.Voice;

/// <summary>Esito finale di una chiamata gestita dal centralino.</summary>
public enum CallOutcome
{
	AnsweredByIvr = 0,
	ForwardedAnswered = 1,
	ForwardedNoAnswer = 2,
	ForwardedBusy = 3,
	ForwardedFailed = 4,
	CallerHangup = 5,
	RejectedInactive = 6,
	NoConfig = 7,
	HangupAfterTimeout = 8,
	WhatsappOnly = 9
}
