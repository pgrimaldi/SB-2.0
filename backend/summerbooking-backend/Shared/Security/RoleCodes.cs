namespace Shared.Security;

/// <summary>Codici dei ruoli (identity.access_roles.code), gli stessi che viaggiano nel claim <c>role</c>.</summary>
public static class RoleCodes
{
	public const string SystemAdmin = "systemadmin";
	public const string SuperAdmin = "superadmin";
	public const string Consumer = "consumer";
	public const string Admin = "admin";
	public const string Lifeguard = "lifeguard";
	public const string Manager = "manager";
	public const string CashOperator = "cashoperator";
	public const string WebsiteAdmin = "websiteadmin";
	public const string User1 = "user1";
	public const string Waiter = "waiter";
	public const string Affiliate = "affiliate";
	public const string AdvancedLifeguard = "advancedlifeguard";
	public const string Marketing = "marketing";
	public const string DailyCashOperator = "dailycashoperator";
}
