using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Validators;
using Module.Catalog.Domain.Enums;
using Shared.Dtos;
using Shared.Utils;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Module.Catalog.Endpoints;

/// <summary>
/// Parametri dello Swagger di Catalog: nomi in camelCase, valori ammessi e obbligatorietà. Gli elenchi vengono dalle
/// stesse costanti ed enum usati dalla validazione, quindi non si disallineano. Una regola vale per i percorsi che
/// finiscono con <c>Path</c> (null = tutti i percorsi del modulo).
/// </summary>
internal static partial class CatalogOpenApi
{
	private const string PathPrefix = "api/catalog/";

	private sealed record Hint(string? Path, string Name, string[]? Allowed = null, bool Required = false);

	private static readonly string[] PublicSnapshotKinds = [Codes.Of(ConfigSnapshotKind.PublicShowcase), Codes.Of(ConfigSnapshotKind.BeachMap)];

	private static readonly Hint[] Hints =
	[
		new("public/bathhouses/{slug}/snapshots/{kind}", "kind", PublicSnapshotKinds),
		new("snapshots/{kind}", "kind", CodesOf<ConfigSnapshotKind>()),
		new("revenue/suggested-prices", "from", Required: true),
		new("revenue/suggested-prices", "to", Required: true),
		new("price-lists", "kind", CodesOf<PriceListKind>()),
		new("price-lists/lookup", "kind", CodesOf<PriceListKind>()),
		new("price-lists", "sortBy", [.. PriceListSortFields.All]),
		new("price-lists/export", "format", ["csv", "xlsx"]),
		new("price-lists/import-template", "format", ["csv", "xlsx"]),
		new("services", "kind", CodesOf<ServiceSaleKind>()),
		new("services", "sortBy", [.. ServiceSortFields.All]),
		new("lookups/service-icons", "type", CodesOf<ServiceSaleKind>()),
		new("discounts", "sortBy", [.. DiscountSortFields.All]),
		new("inventory-items", "sortBy", InventorySortFields.All),
		new("agreement-partners", "sortBy", [.. AgreementPartnerSortFields.All]),
		new("agreement-partners/{id}/price-lists", "kind", CodesOf<PriceListKind>()),
		new("agreement-partners/{id}/price-lists", "listType", CodesOf<AgreementListType>()),
		new("agreement-reports", "status", CodesOf<AgreementReportStatus>()),
		new("admin/properties", "status", CodesOf<PropertyStatus>()),
		new("admin/showcase/{section}", "section", CodesOf<ShowcaseSection>()),
		new("map/icon-catalog", "category", CodesOf<MapIconCategoryCode>()),
		new("property-info/documents/{document}", "document", ["menu", "price-list"]),
		new("public/search", "sortBy", [.. PublicRules.SortFields]),
		new(null, "sortDir", [SortDirections.Asc, SortDirections.Desc]),
	];

	public static void Configure(SwaggerGenOptions options) => options.OperationFilter<ParameterHints>();

	private static string[] CodesOf<TEnum>() where TEnum : struct, Enum => Enum.GetValues<TEnum>().Select(Codes.Of).ToArray();

	[GeneratedRegex(@"\{(\w+):[^}]+\}")]
	private static partial Regex RouteConstraint();

	private static List<JsonNode> ToEnum(IEnumerable<object> values) => values.Select(v => JsonSerializer.SerializeToNode(v)!).ToList();

	private sealed class ParameterHints : IOperationFilter
	{
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			var path = context.ApiDescription.RelativePath;
			if (path?.StartsWith(PathPrefix, StringComparison.OrdinalIgnoreCase) != true || operation.Parameters is null)
				return;
			// i vincoli dei parametri ({id:guid}) non contano per il confronto
			path = RouteConstraint().Replace(path[PathPrefix.Length..].TrimEnd('/'), "{$1}");

			foreach (var parameter in operation.Parameters.OfType<OpenApiParameter>())
			{
				// [AsParameters] espone i nomi del record (Search, SortBy): il binding non distingue le maiuscole.
				if (parameter.Name is { Length: > 0 } name && char.IsUpper(name[0]))
					parameter.Name = char.ToLowerInvariant(name[0]) + name[1..];

				// la prima regola che corrisponde vince: le più specifiche stanno in cima
				var hint = Hints.FirstOrDefault(h => h.Name == parameter.Name && (h.Path is null || path == h.Path || path.EndsWith("/" + h.Path, StringComparison.Ordinal)));
				if (hint is null)
				{
					if (parameter.Name == "pageSize" && parameter.Schema is OpenApiSchema sizes && !path.StartsWith("public/", StringComparison.Ordinal))
						sizes.Enum = ToEnum(CatalogQueryRules.PageSizes.Cast<object>());
					continue;
				}
				if (hint.Allowed is not null && parameter.Schema is OpenApiSchema schema)
					schema.Enum = ToEnum(hint.Allowed);
				if (hint.Required)
					parameter.Required = true;
			}
		}
	}
}
