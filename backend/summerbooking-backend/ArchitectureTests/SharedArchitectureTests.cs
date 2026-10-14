using System.Collections.Generic;
using NetArchTest.Rules;

namespace ArchitectureTests;

/// <summary>
/// Regole di confine per il progetto Shared: e trasversale e non deve conoscere
/// ne i moduli ne il composition root (Boot).
/// </summary>
public class SharedArchitectureTests
{
	[Fact]
	public void Shared_non_dipende_dai_moduli()
	{
		var result = Types.InAssembly(ArchAssemblies.Shared)
			.That().ResideInNamespaceStartingWith("Shared")
			.ShouldNot().HaveDependencyOnAny("Module.Identity", "Module.ZTest")
			.GetResult();

		Assert.True(result.IsSuccessful, Format(result));
	}

	[Fact]
	public void Shared_non_dipende_da_Boot()
	{
		var result = Types.InAssembly(ArchAssemblies.Shared)
			.That().ResideInNamespaceStartingWith("Shared")
			.ShouldNot().HaveDependencyOn("Boot")
			.GetResult();

		Assert.True(result.IsSuccessful, Format(result));
	}

	/// <summary>BuildingBlocks dipende da Shared, non il contrario: Shared resta leggero per tutti.</summary>
	[Fact]
	public void Shared_non_dipende_da_BuildingBlocks()
	{
		var result = Types.InAssembly(ArchAssemblies.Shared)
			.That().ResideInNamespaceStartingWith("Shared")
			.ShouldNot().HaveDependencyOn("BuildingBlocks")
			.GetResult();

		Assert.True(result.IsSuccessful, Format(result));
	}

	/// <summary>
	/// Shared è il kernel dei contratti, referenziato anche dal Domain: nessuna libreria tecnica. Le
	/// implementazioni stanno in BuildingBlocks.Infrastructure.
	/// </summary>
	[Fact]
	public void Shared_non_dipende_da_librerie_tecniche()
	{
		var result = Types.InAssembly(ArchAssemblies.Shared)
			.That().ResideInNamespaceStartingWith("Shared")
			.ShouldNot().HaveDependencyOnAny(
				"Microsoft.EntityFrameworkCore", "Npgsql", "Amazon", "ClosedXML",
				"Microsoft.Extensions.Configuration", "Microsoft.AspNetCore", "FluentValidation")
			.GetResult();

		Assert.True(result.IsSuccessful, Format(result));
	}

	private static string Format(TestResult result)
		=> string.Join(", ", result.FailingTypeNames ?? new List<string>());
}
