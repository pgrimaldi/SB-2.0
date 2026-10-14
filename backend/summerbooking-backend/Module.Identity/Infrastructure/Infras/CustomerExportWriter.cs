using System.Globalization;
using System.Resources;
using BuildingBlocks.Infrastructure.Export;
using Module.Identity.Application.Abstractions;
using Module.Identity.Domain.Entities;
using Shared.Dtos;
using Shared.Localization;

namespace Module.Identity.Infrastructure.Infras;

/// <summary>
/// Colonne dell'esportazione della rubrica, con etichette da Resources/CustomerExport.*.resx nella
/// lingua dell'operatore (italiano se manca la traduzione); il formato del file è in BuildingBlocks.
/// </summary>
public sealed class CustomerExportWriter : ICustomerExportWriter
{
	private static readonly ResourceManager Texts =
		new("Module.Identity.Resources.CustomerExport", typeof(CustomerExportWriter).Assembly);

	private static readonly string[] ColumnKeys =
	[
		"Column_FullName", "Column_PhoneCountryCode", "Column_Phone", "Column_PhoneSecondary", "Column_Email",
		"Column_FiscalCode", "Column_VatNumber", "Column_Address", "Column_City", "Column_PostalCode",
		"Column_DateOfBirth", "Column_Blacklist", "Column_Note", "Column_CreatedAt",
	];

	public TabularFile Write(IReadOnlyList<Customer> customers, TabularFormat format, AppLanguage language)
	{
		var culture = SupportedLanguages.Culture(language);
		string T(string key) => Texts.GetString(key, culture) ?? key;

		var headers = ColumnKeys.Select(T).ToArray();
		var yes = T("Value_Yes");
		var no = T("Value_No");

		return TabularExportWriter.Write(T("FileName"), T("SheetName"), headers, customers.Select(c => Row(c, yes, no)), format);
	}

	private static string?[] Row(Customer c, string yes, string no) =>
	[
		c.FullName, c.PhoneCountryCode, c.Phone, c.PhoneSecondary, c.Email, c.FiscalCode, c.VatNumber, c.Address,
		c.City, c.PostalCode, c.DateOfBirth?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
		c.IsBlacklisted ? yes : no, c.Note, c.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
	];
}
