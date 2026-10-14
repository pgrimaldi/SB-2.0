using FluentValidation;
using Module.Audit.Application.Dtos;
using Module.Audit.Domain.Enums;
using Shared.Contracts.Errors;
using Shared.Dtos;
using Shared.Utils;

namespace Module.Audit.Application.Validators;

public static class AuditQueryRules
{
	public static readonly int[] PageSizes = [10, 20, 25, 50, 100];
	public const int SearchMaxLength = 100;
	public const int MaxActionCodes = 50;

	/// <summary>Righe massime di un export: oltre si chiede di restringere le date.</summary>
	public const int ExportMaxRows = 50_000;
}

/// <summary>Regole dei filtri comuni a lista ed export.</summary>
public abstract class AuditFilterValidator<T> : AbstractValidator<T> where T : IAuditEntryFilterQuery
{
	protected AuditFilterValidator()
	{
		RuleFor(x => x.To).GreaterThan(x => x.From).When(x => x.From is not null && x.To is not null);
		RuleFor(x => x.ActionCodes).Must(c => c is null || c.Length <= AuditQueryRules.MaxActionCodes).WithErrorCode(ValidationErrorCodes.Invalid);
		RuleForEach(x => x.ActionCodes).NotEmpty().MaximumLength(100);
		RuleFor(x => x.Search).MaximumLength(AuditQueryRules.SearchMaxLength);
		RuleFor(x => x.SortDir).Must(s => s is null or SortDirections.Asc or SortDirections.Desc).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

/// <summary>Filtri dell'export dello stabilimento (senza paginazione).</summary>
public class AuditEntryFilterValidator : AuditFilterValidator<AuditEntryListQuery>;

public sealed class AuditEntryListQueryValidator : AuditEntryFilterValidator
{
	public AuditEntryListQueryValidator()
	{
		RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
		RuleFor(x => x.PageSize).Must(s => AuditQueryRules.PageSizes.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

/// <summary>Filtri dell'export del systemadmin (senza paginazione).</summary>
public class PlatformAuditEntryFilterValidator : AuditFilterValidator<PlatformAuditEntryListQuery>
{
	public PlatformAuditEntryFilterValidator()
	{
		RuleForEach(x => x.Categories).Must(c => Codes.TryParse<EntryCategory>(c, out _)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}

public sealed class PlatformAuditEntryListQueryValidator : PlatformAuditEntryFilterValidator
{
	public PlatformAuditEntryListQueryValidator()
	{
		RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
		RuleFor(x => x.PageSize).Must(s => AuditQueryRules.PageSizes.Contains(s)).WithErrorCode(ValidationErrorCodes.Invalid);
	}
}
