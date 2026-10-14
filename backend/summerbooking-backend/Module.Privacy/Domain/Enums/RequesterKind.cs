namespace Module.Privacy.Domain.Enums;

/// <summary>Chi ha originato una richiesta dell'interessato: l'interessato stesso, lo staff o systemadmin.</summary>
public enum RequesterKind
{
	DataSubject = 0,
	Staff = 1,
	SystemAdmin = 2
}
