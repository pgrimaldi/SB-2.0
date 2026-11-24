namespace Shared.Security;

/// <summary>
/// Matrice ruolo→permessi: chi legge la lista contatti, chi ne vede i dettagli e li modifica, chi accetta i
/// contratti, chi gestisce utenti e 2FA (superadmin e admin), chi configura il catalogo dello stabilimento.
/// </summary>
public static class RolePermissions
{
	private static readonly string[] ContactRoles =
		[RoleCodes.SuperAdmin, RoleCodes.Admin, RoleCodes.Manager, RoleCodes.User1, RoleCodes.Marketing];

	private static readonly string[] StaffRoles =
	[
		RoleCodes.SuperAdmin, RoleCodes.Admin, RoleCodes.Lifeguard, RoleCodes.Manager, RoleCodes.CashOperator,
		RoleCodes.WebsiteAdmin, RoleCodes.User1, RoleCodes.Waiter, RoleCodes.Affiliate, RoleCodes.AdvancedLifeguard,
		RoleCodes.Marketing, RoleCodes.DailyCashOperator,
	];

	// Impostazioni dello stabilimento (magazzino, listini, sconti, mappa): titolare, amministratore e user1.
	private static readonly string[] SettingsRoles = [RoleCodes.SuperAdmin, RoleCodes.Admin, RoleCodes.User1];

	// Prenotazioni: come nel vecchio gestionale (menu, pulsanti e redirect per ruolo), ma controllate qui. Il bagnino vede
	// mappa e magazzino senza importi; il bagnino avanzato in più segna arrivi e uscite, sposta e crea gruppi; l'affiliato
	// lavora solo sulle sue postazioni e non incassa; la cassa non vede l'elenco delle prenotazioni; user1 non cambia i
	// prezzi. Il cassiere giornaliero (ruolo nuovo) vale come la cassa, senza eliminare.
	private static readonly string[] BookingReadRoles =
	[
		RoleCodes.SuperAdmin, RoleCodes.Admin, RoleCodes.User1, RoleCodes.Manager, RoleCodes.CashOperator, RoleCodes.DailyCashOperator,
		RoleCodes.Lifeguard, RoleCodes.AdvancedLifeguard, RoleCodes.Affiliate,
	];

	private static readonly string[] CashRoles =
		[RoleCodes.SuperAdmin, RoleCodes.Admin, RoleCodes.User1, RoleCodes.Manager, RoleCodes.CashOperator, RoleCodes.DailyCashOperator];

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
		Grant(Permissions.CatalogRead, StaffRoles);
		Grant(Permissions.CatalogSettings, SettingsRoles);
		Grant(Permissions.CatalogPricing, SettingsRoles);
		Grant(Permissions.InventoryManage, SettingsRoles);
		Grant(Permissions.InventoryRead,
			RoleCodes.SuperAdmin, RoleCodes.Admin, RoleCodes.User1, RoleCodes.Manager, RoleCodes.Lifeguard,
			RoleCodes.AdvancedLifeguard, RoleCodes.CashOperator, RoleCodes.DailyCashOperator);
		Grant(Permissions.AgreementsManage, RoleCodes.SuperAdmin, RoleCodes.Admin, RoleCodes.Manager);
		Grant(Permissions.RevenueManage, RoleCodes.SuperAdmin, RoleCodes.Admin, RoleCodes.Manager, RoleCodes.User1);
		Grant(Permissions.BookingsRead, BookingReadRoles);
		Grant(Permissions.BookingsList, RoleCodes.SuperAdmin, RoleCodes.Admin, RoleCodes.User1, RoleCodes.Manager);
		Grant(Permissions.BookingsPlanner, CashRoles);
		Grant(Permissions.BookingsAmounts, [.. BookingReadRoles.Where(r => r is not RoleCodes.Lifeguard and not RoleCodes.AdvancedLifeguard)]);
		Grant(Permissions.BookingsWrite, [.. CashRoles, RoleCodes.Affiliate]);
		Grant(Permissions.BookingsMove, [.. CashRoles, RoleCodes.Affiliate, RoleCodes.AdvancedLifeguard]);
		Grant(Permissions.BookingsOnline, RoleCodes.SuperAdmin, RoleCodes.Admin, RoleCodes.Manager, RoleCodes.CashOperator, RoleCodes.DailyCashOperator);
		Grant(Permissions.BookingsPricing, [.. CashRoles.Where(r => r != RoleCodes.User1)]);
		Grant(Permissions.BookingsCheckIn, [.. CashRoles, RoleCodes.Affiliate, RoleCodes.AdvancedLifeguard]);
		Grant(Permissions.BookingsDelete, RoleCodes.SuperAdmin, RoleCodes.Admin, RoleCodes.User1, RoleCodes.Manager, RoleCodes.CashOperator, RoleCodes.Affiliate);
		Grant(Permissions.BookingsCleanup, RoleCodes.SuperAdmin, RoleCodes.Admin);
		Grant(Permissions.PaymentsRecord, CashRoles);
		return matrix;
	}
}
