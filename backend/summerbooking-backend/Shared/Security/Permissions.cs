namespace Shared.Security;

/// <summary>
/// Permessi verificati dagli endpoint (policy <c>perm:&lt;codice&gt;</c>). La matrice ruolo→permessi è
/// in <see cref="RolePermissions"/>; il frontend la riceve da <c>GET /api/identity/me</c>.
/// </summary>
public static class Permissions
{
	public const string StaffManage = "staff.manage";
	public const string SecurityManage = "security.manage";
	public const string CustomersRead = "customers.read";
	public const string CustomersContacts = "customers.contacts";
	public const string CustomersWrite = "customers.write";
	public const string ContractsAccept = "contracts.accept";
	public const string AuditRead = "audit.read";
	public const string PlatformManage = "platform.manage";
	public const string CatalogRead = "catalog.read";
	public const string CatalogSettings = "catalog.settings";
	public const string CatalogPricing = "catalog.pricing";
	public const string InventoryRead = "inventory.read";
	public const string InventoryManage = "inventory.manage";
	public const string AgreementsManage = "agreements.manage";
	public const string RevenueManage = "revenue.manage";

	public static readonly IReadOnlyList<string> All =
	[
		StaffManage, SecurityManage, CustomersRead, CustomersContacts, CustomersWrite,
		ContractsAccept, AuditRead, PlatformManage,
		CatalogRead, CatalogSettings, CatalogPricing, InventoryRead, InventoryManage, AgreementsManage, RevenueManage,
	];

	public static string PolicyName(string permission) => $"perm:{permission}";
}
