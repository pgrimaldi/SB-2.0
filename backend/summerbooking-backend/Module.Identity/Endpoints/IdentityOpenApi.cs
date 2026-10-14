using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Module.Identity.Application.Dtos;
using Module.Identity.Application.Validators;
using Module.Identity.Domain.Enums;
using Shared.Dtos;
using Shared.Localization;
using Shared.Security;
using Shared.Utils;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Module.Identity.Endpoints;

/// <summary>
/// Esempi e valori ammessi nello Swagger di Identity. Gli elenchi chiusi vengono dalle stesse costanti ed enum
/// usati dalla validazione, quindi non si disallineano; per il testo libero ci sono esempi realistici.
/// Le chiavi <c>Tipo.campo</c> prevalgono sulle chiavi <c>campo</c>.
/// </summary>
internal static class IdentityOpenApi
{
	private const string PathPrefix = "api/identity/";

	private static readonly string[] Languages = SupportedLanguages.All.Select(SupportedLanguages.Code).ToArray();
	private static readonly string[] Apps = [ClientApps.Hub, ClientApps.Customer, ClientApps.Admin];
	private static readonly string[] AllRoles = typeof(RoleCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
		.Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()!).ToArray();
	// I ruoli con is_assignable_via_panel nel seed dei ruoli: l'elenco vivo è GET /roles/assignable.
	private static readonly string[] AssignableRoles =
	[
		RoleCodes.Admin, RoleCodes.Lifeguard, RoleCodes.Manager, RoleCodes.CashOperator, RoleCodes.DailyCashOperator,
		RoleCodes.User1, RoleCodes.Waiter, RoleCodes.Affiliate, RoleCodes.AdvancedLifeguard, RoleCodes.Marketing,
	];
	private static readonly string[] SocialProviders = CodesOf<LoginMethod>().Where(c => c != Codes.Of(LoginMethod.Normal)).ToArray();

	private static readonly Dictionary<string, string[]> AllowedValues = new(StringComparer.OrdinalIgnoreCase)
	{
		["app"] = Apps,
		["language"] = Languages,
		["role"] = AllRoles,
		["CreateStaffUserRequest.role"] = AssignableRoles,
		["UpdateStaffUserRequest.role"] = AssignableRoles,
		["RoleDto.code"] = AssignableRoles,
		["loginMethod"] = CodesOf<LoginMethod>(),
		["notificationSounds"] = CodesOf<NotificationSoundScope>(),
		["DeleteAccountRequest.provider"] = SocialProviders,
		["SubscriptionDto.status"] = CodesOf<SubscriptionStatus>(),
		["SubscriptionSummaryDto.status"] = CodesOf<SubscriptionStatus>(),
		["ContractDocumentDto.status"] = CodesOf<ContractDocumentStatus>(),
		["termType"] = CodesOf<SubscriptionTermType>(),
		["kind"] = CodesOf<SubscriptionPeriodKind>(),
		["closeReason"] = CodesOf<SubscriptionPeriodCloseReason>(),
		["source"] = CodesOf<CustomerSource>(),
		["channel"] = CodesOf<SecondFactorChannel>(),
	};

	private const string SampleJwt = "eyJhbGciOiJIUzI1NiIsImtpZCI6ImRldi11c2VyLTEiLCJ0eXAiOiJKV1QifQ.eyJzdWIiOiIwMTk5YzJhNC01YjdlLTdkMjEtOGYzYS0yYzRiNmQ4ZTBmMTIifQ.Qm9yZGVyLWZpcm1hLWRpLWVzZW1waW8";

	private static readonly Dictionary<string, object> Examples = new(StringComparer.OrdinalIgnoreCase)
	{
		// Identificativi
		["id"] = "0199c2a4-5b7e-7d21-8f3a-2c4b6d8e0f12",
		["propertyId"] = "0199c2a4-1a2b-7c3d-9e4f-5a6b7c8d9e01",
		["bathhouseId"] = "0199c2a4-1a2b-7c3d-9e4f-5a6b7c8d9e01",
		["impersonatorId"] = "0199c2a4-0000-7000-8000-00000000a001",
		["impersonatedBy"] = "0199c2a4-0000-7000-8000-00000000a001",
		// Accesso
		["username"] = "cassa1",
		["password"] = "Ombrellone-2026!",
		["newPassword"] = "Ombrellone-2026!",
		["currentPassword"] = "Sdraio-2025!",
		["remember"] = true,
		["app"] = ClientApps.Hub,
		["accessToken"] = SampleJwt,
		["expiresIn"] = 604800,
		["mfaToken"] = SampleJwt,
		["twoFactorRequired"] = true,
		["VerifyOtpRequest.code"] = "482913",
		["ImpersonationExchangeRequest.code"] = SampleJwt,
		["ImpersonationCodeResponse.code"] = SampleJwt,
		["ImpersonationCodeResponse.expiresIn"] = 60,
		["SocialLoginRequest.token"] = "eyJhbGciOiJSUzI1NiIsImtpZCI6Imdvb2dsZS0xIn0.eyJzdWIiOiIxMDk4NzY1NDMyMTAifQ.firma-del-provider",
		["providerToken"] = "eyJhbGciOiJSUzI1NiIsImtpZCI6Imdvb2dsZS0xIn0.eyJzdWIiOiIxMDk4NzY1NDMyMTAifQ.firma-del-provider",
		["provider"] = "google",
		["ResetPasswordRequest.token"] = "q8Zx3vN1kPf7Lr2Ws9Ty4Ua6Ob0Mc5Hd",
		["termsAccepted"] = true,
		["newsletterConsent"] = false,
		["available"] = true,
		// Persone e contatti
		["email"] = "mario.rossi@example.com",
		["firstName"] = "Mario",
		["lastName"] = "Rossi",
		["fullName"] = "Rossi Mario",
		["phoneCountryCode"] = "+39",
		["phone"] = "333 1234567",
		["phoneSecondary"] = "0541 123456",
		["emailMasked"] = "m*********@example.com",
		["phoneMasked"] = "*******567",
		["fiscalCode"] = "RSSMRA80A01H294L",
		["vatNumber"] = "01234567890",
		["address"] = "Viale Regina Elena 12",
		["city"] = "Rimini",
		["postalCode"] = "47921",
		["dateOfBirth"] = "1980-01-01",
		["otherNames"] = "Anna Rossi, Luca Rossi",
		["discountNote"] = "Sconto 10% cliente abituale",
		["note"] = "Preferisce la prima fila",
		["isBlacklisted"] = false,
		["blacklistReason"] = "Mancato pagamento stagione 2025",
		["hasAccount"] = true,
		["language"] = "it",
		["role"] = RoleCodes.Manager,
		["CreateStaffUserRequest.role"] = RoleCodes.CashOperator,
		["UpdateStaffUserRequest.role"] = RoleCodes.CashOperator,
		["profilePictureUrl"] = "https://lh3.googleusercontent.com/a/ACg8ocLesempio=s96-c",
		["emailIsContact"] = true,
		["phoneIsContact"] = true,
		["isResourceScopeRestricted"] = false,
		["resourceScopes"] = new[] { "settore-a", "settore-b" },
		["onlineReservationEnabled"] = true,
		// Preferenze e sessioni
		["notificationSounds"] = new[] { Codes.Of(NotificationSoundScope.ManagementBooking), Codes.Of(NotificationSoundScope.OnlineBooking) },
		["newsSectionEnabled"] = true,
		["twoFactorEnabled"] = true,
		["userAgent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/141.0 Safari/537.36",
		["ip"] = "203.0.113.10",
		["isPersistent"] = true,
		["isCurrent"] = true,
		// Abbonamenti e piani
		["licensePlanCode"] = "advanced",
		["CreateLicensePlanRequest.code"] = "premium",
		["LicensePlanDto.code"] = "advanced",
		["durationMonths"] = 12,
		["sortOrder"] = 2,
		["isActive"] = true,
		["culture"] = "it",
		["label"] = "Advanced",
		["description"] = "Tutte le funzioni del gestionale, compresa la prenotazione online",
		["amount"] = 600.00,
		["currency"] = CurrencyCodes.Default,
		["startsAt"] = "2026-04-01T00:00:00Z",
		["endsAt"] = "2027-03-31T23:59:59Z",
		["monthsToExpiry"] = 5,
		["allFeaturesUnlocked"] = true,
		// Contratti
		["versionCode"] = "2026.1",
		["title"] = "Condizioni di servizio 2026",
		["notes"] = "Aggiornate le clausole di recesso",
		["contentSha256"] = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08",
		["downloadUrl"] = "https://documenti.summerbooking.it/contracts/2026.1.pdf?X-Amz-Expires=300&X-Amz-Signature=esempio",
		["accepted"] = true,
	};

	/// <summary>Parametri in querystring e nel percorso, per nome (indipendente dalle maiuscole).</summary>
	private static readonly Dictionary<string, (string[]? Allowed, object? Example)> Parameters = new(StringComparer.OrdinalIgnoreCase)
	{
		["format"] = (CodesOf<TabularFormat>(), Codes.Of(TabularFormat.Xlsx)),
		["sortBy"] = (CustomerSortFields.All, CustomerSortFields.FullName),
		["sortDir"] = ([SortDirections.Asc, SortDirections.Desc], SortDirections.Asc),
		["search"] = (null, "mario"),
		["blacklistOnly"] = (null, false),
		["page"] = (null, 1),
		["email"] = (null, "mario.rossi@example.com"),
		["code"] = (null, "advanced"),
		["propertyId"] = (null, "0199c2a4-1a2b-7c3d-9e4f-5a6b7c8d9e01"),
		["bathhouseId"] = (null, "0199c2a4-1a2b-7c3d-9e4f-5a6b7c8d9e01"),
		["userId"] = (null, "0199c2a4-5b7e-7d21-8f3a-2c4b6d8e0f12"),
		["customerId"] = (null, "0199c2a4-7c8d-7e9f-8a0b-1c2d3e4f5a6b"),
		["documentId"] = (null, "0199c2a4-9d0e-7f1a-8b2c-3d4e5f6a7b8c"),
		["sessionId"] = (null, "0199c2a4-2b3c-7d4e-8f5a-6b7c8d9e0f1a"),
	};

	public static void Configure(SwaggerGenOptions options)
	{
		options.SchemaFilter<SchemaHints>();
		options.OperationFilter<ParameterHints>();
	}

	private static string[] CodesOf<TEnum>() where TEnum : struct, Enum => Enum.GetValues<TEnum>().Select(Codes.Of).ToArray();

	private static T? Lookup<T>(Dictionary<string, T> map, string typeName, string name)
		=> map.TryGetValue($"{typeName}.{name}", out var specific) ? specific : map.GetValueOrDefault(name);

	private static JsonNode? ToNode(object value) => JsonSerializer.SerializeToNode(value);

	private static List<JsonNode> ToEnum(IEnumerable<object> values) => values.Select(v => ToNode(v)!).ToList();

	/// <summary>Esempi ed elenchi sulle proprietà dei DTO del modulo.</summary>
	private sealed class SchemaHints : ISchemaFilter
	{
		public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
		{
			if (context.Type.Namespace?.StartsWith("Module.Identity", StringComparison.Ordinal) != true || schema.Properties is null)
				return;

			foreach (var (name, property) in schema.Properties)
			{
				if (property is not OpenApiSchema target)
					continue;

				if (Lookup(AllowedValues, context.Type.Name, name) is { } allowed)
				{
					// Per gli array l'elenco vale per ciascun elemento.
					if (target.Items is OpenApiSchema items)
						items.Enum = ToEnum(allowed);
					else
						target.Enum = ToEnum(allowed);
				}

				if (Lookup(Examples, context.Type.Name, name) is { } example)
					target.Example = ToNode(example);
			}
		}
	}

	/// <summary>Parametri di Identity: nomi in camelCase, elenchi chiusi ed esempi.</summary>
	private sealed class ParameterHints : IOperationFilter
	{
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			if (context.ApiDescription.RelativePath?.StartsWith(PathPrefix, StringComparison.OrdinalIgnoreCase) != true || operation.Parameters is null)
				return;

			foreach (var parameter in operation.Parameters.OfType<OpenApiParameter>())
			{
				// [AsParameters] espone i nomi del record (Search, SortBy): il binding non distingue le maiuscole.
				if (parameter.Name is { Length: > 0 } name && char.IsUpper(name[0]))
					parameter.Name = char.ToLowerInvariant(name[0]) + name[1..];

				var schema = parameter.Schema as OpenApiSchema;
				if (string.Equals(parameter.Name, "pageSize", StringComparison.Ordinal))
				{
					// Contatti: misure fisse; preferiti: da 1 a 100.
					if (context.ApiDescription.RelativePath!.Contains("customers", StringComparison.OrdinalIgnoreCase) && schema is not null)
						schema.Enum = ToEnum(CustomerListQueryValidator.PageSizes.Cast<object>());
					parameter.Example = ToNode(20);
					continue;
				}

				if (Parameters.TryGetValue(parameter.Name!, out var hint))
				{
					if (hint.Allowed is not null && schema is not null)
						schema.Enum = ToEnum(hint.Allowed);
					if (hint.Example is not null)
						parameter.Example = ToNode(hint.Example);
				}
			}
		}
	}
}
