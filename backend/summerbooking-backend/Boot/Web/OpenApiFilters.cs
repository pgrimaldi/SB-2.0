using System.Reflection;
using System.Runtime.CompilerServices;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Boot.Web;

/// <summary>
/// Lucchetto dello Swagger solo sugli endpoint che chiedono il token: quelli anonimi (sito pubblico, login, sfida
/// anti-robot) restano senza, così Swagger UI non manda l'header e chi legge capisce che non serve.
/// </summary>
internal sealed class BearerOnlyWhenRequired : IOperationFilter
{
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
		// Senza AllowAnonymous vale la policy di fallback, che chiede il token utente.
		if (metadata.OfType<IAllowAnonymous>().Any())
			return;
		operation.Security ??= [];
		operation.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = [] });
	}
}

/// <summary>
/// Campi obbligatori dei DTO nello Swagger, decisi da due fonti:
/// <list type="bullet">
/// <item>il tipo: un riferimento non nullable senza valore predefinito nel costruttore va mandato;</item>
/// <item>il validatore: un campo che la validazione rifiuta quando manca, con una regola senza condizioni (When/Unless),
/// va mandato anche se il tipo è nullable o è un valore (es. un decimal che deve essere maggiore di zero).</item>
/// </list>
/// Il validatore si prova su un'istanza vuota del DTO, una proprietà alla volta.
/// </summary>
internal sealed class RequiredFromTypesAndValidators : ISchemaFilter
{
	private static readonly NullabilityInfoContext Nullability = new();
	private readonly Dictionary<Type, IValidator> _validators;

	public RequiredFromTypesAndValidators(IServiceProvider services)
	{
		// I validatori sono registrati per tipo concreto, uno per DTO.
		_validators = AppDomain.CurrentDomain.GetAssemblies()
			.Where(a => a.GetName().Name?.StartsWith("Module.", StringComparison.Ordinal) == true)
			.SelectMany(a => a.GetTypes())
			.Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IValidator).IsAssignableFrom(t) && TargetOf(t) is not null)
			.Select(t => (Target: TargetOf(t)!, Validator: services.GetService(t) as IValidator))
			.Where(x => x.Validator is not null)
			.GroupBy(x => x.Target)
			.ToDictionary(g => g.Key, g => g.First().Validator!);
	}

	public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
	{
		if (schema is not OpenApiSchema target || target.Properties is not { Count: > 0 } properties || context.Type.IsPrimitive)
			return;

		var ctor = context.Type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
		var withDefault = ctor?.GetParameters().Where(p => p.HasDefaultValue).Select(p => p.Name!).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
		_validators.TryGetValue(context.Type, out var validator);

		var required = new SortedSet<string>(StringComparer.Ordinal);
		foreach (var name in properties.Keys)
		{
			var property = context.Type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			if (property is null || withDefault.Contains(property.Name))
				continue;
			var byType = !property.PropertyType.IsValueType && Nullability.Create(property).ReadState == NullabilityState.NotNull;
			if (byType || validator is not null && RejectedWhenMissing(validator, context.Type, property.Name))
				required.Add(name);
		}
		target.Required = required.Count > 0 ? required : null;
	}

	private static bool RejectedWhenMissing(IValidator validator, Type type, string property)
	{
		var unconditional = ((IEnumerable<IValidationRule>)validator)
			.Where(r => r.Member?.Name == property && !r.HasCondition && !r.HasAsyncCondition)
			.SelectMany(r => r.Components)
			.Where(c => !c.HasCondition && !c.HasAsyncCondition)
			.Select(c => c.ErrorCode ?? c.Validator.Name)
			.ToHashSet();
		if (unconditional.Count == 0)
			return false;
		try
		{
			var context = ValidationContext<object>.CreateWithOptions(RuntimeHelpers.GetUninitializedObject(type), o => o.IncludeProperties(property));
			return validator.Validate(context).Errors.Any(e => string.Equals(e.PropertyName, property, StringComparison.OrdinalIgnoreCase) && unconditional.Contains(e.ErrorCode));
		}
		catch (Exception ex) when (ex is NullReferenceException or ArgumentNullException or InvalidOperationException)
		{
			return false;
		}
	}

	private static Type? TargetOf(Type validator)
	{
		for (var b = validator.BaseType; b is not null; b = b.BaseType)
			if (b.IsGenericType && b.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
				return b.GetGenericArguments()[0];
		return null;
	}
}
