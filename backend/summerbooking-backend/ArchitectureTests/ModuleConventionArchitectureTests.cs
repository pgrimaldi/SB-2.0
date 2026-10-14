using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetArchTest.Rules;

namespace ArchitectureTests;

/// <summary>
/// Convenzioni di Clean Architecture verificate su <b>tutti</b> i moduli applicativi
/// (<c>Module.*</c>) tramite regole parametrizzate: un modulo nuovo è coperto
/// automaticamente, senza riscrivere le regole a mano.
/// </summary>
public class ModuleConventionArchitectureTests
{
	public static IEnumerable<object[]> Moduli =>
		ArchAssemblies.ModuleNames.Select(nome => new object[] { nome });

	/// <summary>
	/// Gli <c>Endpoints</c> parlano solo con l'<c>Application</c> (servizi + DTO):
	/// non devono vedere l'<c>Infrastructure</c> del modulo.
	/// </summary>
	[Theory]
	[MemberData(nameof(Moduli))]
	public void Gli_endpoint_non_dipendono_da_Infrastructure(string modulo)
	{
		var result = Types.InAssembly(Assembly.Load(modulo))
			.That().ResideInNamespaceStartingWith($"{modulo}.Endpoints")
			.ShouldNot().HaveDependencyOn($"{modulo}.Infrastructure")
			.GetResult();

		Assert.True(result.IsSuccessful, Format(modulo, result));
	}

	/// <summary>
	/// Gli <c>Endpoints</c> non restituiscono entità di dominio: si esce sempre con un DTO.
	/// </summary>
	[Theory]
	[MemberData(nameof(Moduli))]
	public void Gli_endpoint_non_espongono_entita_di_dominio(string modulo)
	{
		var result = Types.InAssembly(Assembly.Load(modulo))
			.That().ResideInNamespaceStartingWith($"{modulo}.Endpoints")
			.ShouldNot().HaveDependencyOn($"{modulo}.Domain.Entities")
			.GetResult();

		Assert.True(result.IsSuccessful, Format(modulo, result));
	}

	/// <summary>
	/// Solo l'<c>Infrastructure</c> del modulo può dipendere dal proprio <c>DbContext</c>:
	/// Domain, Application ed Endpoints non lo conoscono.
	/// </summary>
	[Theory]
	[MemberData(nameof(Moduli))]
	public void Solo_Infrastructure_dipende_dal_DbContext(string modulo)
	{
		var dbContexts = DbContextFullNames(modulo);
		if (dbContexts.Length == 0)
			return; // il modulo non ha ancora un DbContext: niente da verificare

		var result = Types.InAssembly(Assembly.Load(modulo))
			.That().ResideInNamespaceStartingWith(modulo)
			.And().DoNotResideInNamespaceStartingWith($"{modulo}.Infrastructure")
			.ShouldNot().HaveDependencyOnAny(dbContexts)
			.GetResult();

		Assert.True(result.IsSuccessful, Format(modulo, result));
	}

	/// <summary>
	/// Nessun modulo può dipendere dal <c>DbContext</c> di un altro modulo:
	/// lo schema di un modulo è privato al modulo.
	/// </summary>
	[Theory]
	[MemberData(nameof(Moduli))]
	public void Nessun_modulo_dipende_dal_DbContext_di_un_altro_modulo(string modulo)
	{
		var dbContextAltrui = ArchAssemblies.ModuleNames
			.Where(altro => !string.Equals(altro, modulo, StringComparison.Ordinal))
			.SelectMany(DbContextFullNames)
			.ToArray();

		if (dbContextAltrui.Length == 0)
			return;

		var result = Types.InAssembly(Assembly.Load(modulo))
			.That().ResideInNamespaceStartingWith(modulo)
			.ShouldNot().HaveDependencyOnAny(dbContextAltrui)
			.GetResult();

		Assert.True(result.IsSuccessful, Format(modulo, result));
	}

	/// <summary>
	/// Nomi completi dei tipi <c>*DbContext</c> dichiarati nell'<c>Infrastructure</c> del modulo.
	/// </summary>
	private static string[] DbContextFullNames(string modulo)
		=> Types.InAssembly(Assembly.Load(modulo))
			.That().ResideInNamespaceStartingWith($"{modulo}.Infrastructure")
			.And().HaveNameEndingWith("DbContext")
			.GetTypes()
			.Select(type => type.FullName)
			.Where(fullName => fullName is not null)
			.Select(fullName => fullName!)
			.ToArray();

	/// <summary>
	/// BuildingBlocks.Infrastructure (S3, export CSV/XLSX, …) è roba di Infrastructure: Domain e Application
	/// non la vedono. Il modulo è un unico assembly, quindi il confine lo garantisce questa regola, non il compilatore.
	/// </summary>
	[Theory]
	[MemberData(nameof(Moduli))]
	public void Domain_e_Application_non_dipendono_da_BuildingBlocks_Infrastructure(string modulo)
	{
		var result = Types.InAssembly(Assembly.Load(modulo))
			.That().ResideInNamespaceStartingWith($"{modulo}.Domain")
			.Or().ResideInNamespaceStartingWith($"{modulo}.Application")
			.ShouldNot().HaveDependencyOnAny("BuildingBlocks.Infrastructure", "ClosedXML", "Amazon", "Swashbuckle", "Microsoft.OpenApi")
			.GetResult();

		Assert.True(result.IsSuccessful, Format(modulo, result));
	}

	/// <summary>
	/// BuildingBlocks.Application (validazione comune) serve all'Application; il Domain non dipende da nessun BuildingBlocks.
	/// </summary>
	[Theory]
	[MemberData(nameof(Moduli))]
	public void Domain_non_dipende_da_BuildingBlocks(string modulo)
	{
		var result = Types.InAssembly(Assembly.Load(modulo))
			.That().ResideInNamespaceStartingWith($"{modulo}.Domain")
			.ShouldNot().HaveDependencyOn("BuildingBlocks")
			.GetResult();

		Assert.True(result.IsSuccessful, Format(modulo, result));
	}

	private static string Format(string modulo, TestResult result)
		=> $"{modulo}: " + string.Join(", ", result.FailingTypeNames ?? new List<string>());
}
