namespace Module.Identity.Domain.Enums;

/// <summary>
/// Metodo di autenticazione usato l'ultima volta da un account (identity_access_users.login_method).
/// </summary>
public enum LoginMethod
{
	Normal = 0,
	Google = 1,
	Facebook = 2,
	Apple = 3
}
