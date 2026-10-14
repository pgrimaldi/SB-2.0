namespace Shared.Security;

/// <summary>
/// Matrice ruolo→permessi: chi legge la lista contatti, chi ne vede i dettagli e li modifica, chi accetta i
/// contratti, chi gestisce utenti e 2FA (superadmin e admin).
/// </summary>
public static class RolePermissions
{
	private static readonly string[] ContactRoles =
		[RoleCodes.SuperAdmin, RoleCodes.Admin, RoleCodes.Manager, RoleCodes.User1, RoleCodes.Marketing];

	private static readonly Dictionary<string, HashSet<string>> Matrix = Build();

	public static bool Has(string? role, string permission)
		=> role is not null && Matrix.TryGetValue(role, out var set) && set.Contains(permission);

	public static IReadOnlyCollection<string> For(string? role)
		=> role is not null && Matrix.TryGetValue(role, out var set) ? set : [];

	private static Dictionary<string, HashSet<string>> Build()
	{
		var matrix = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

		void Grant(string permission, params string[] roles)
		{
			foreach (var role in roles)
			{
				if (!matrix.TryGetValue(role, out var set))
					matrix[role] = set = new HashSet<string>(StringComparer.Ordinal);
				set.Add(permission);
			}
		}

		Grant(Permissions.StaffManage, RoleCodes.SuperAdmin, RoleCodes.Admin);
		Grant(Permissions.SecurityManage, RoleCodes.SuperAdmin, RoleCodes.Admin);
		Grant(Permissions.CustomersRead, [.. ContactRoles, RoleCodes.Lifeguard, RoleCodes.CashOperator, RoleCodes.DailyCashOperator]);
		Grant(Permissions.CustomersContacts, ContactRoles);
		Grant(Permissions.CustomersWrite, ContactRoles);
		Grant(Permissions.ContractsAccept, RoleCodes.SuperAdmin, RoleCodes.Admin);
		Grant(Permissions.AuditRead, RoleCodes.SuperAdmin, RoleCodes.Admin);
		Grant(Permissions.PlatformManage, RoleCodes.SystemAdmin);
		return matrix;
	}
}
