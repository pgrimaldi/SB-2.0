using System.Collections.Generic;
using System.Linq;
using NetArchTest.Rules;

namespace ArchitectureTests;

/// <summary>
/// I BuildingBlocks sono condivisi fra i moduli: non devono conoscerne nessuno, né il composition root.
/// </summary>
public class BuildingBlocksArchitectureTests
{
	[Fact]
	public void BuildingBlocks_non_dipende_dai_moduli_ne_da_Boot()
	{
		var result = Types.InAssembly(ArchAssemblies.BuildingBlocks)
			.That().ResideInNamespaceStartingWith("BuildingBlocks")
			.ShouldNot().HaveDependencyOnAny(ArchAssemblies.ModuleNames.Append("Boot").ToArray())
			.GetResult();

		Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? new List<string>()));
	}

	[Fact]
	public void BuildingBlocks_Application_non_dipende_dai_moduli_ne_da_Boot()
	{
		var result = Types.InAssembly(ArchAssemblies.BuildingBlocksApplication)
			.That().ResideInNamespaceStartingWith("BuildingBlocks")
			.ShouldNot().HaveDependencyOnAny(ArchAssemblies.ModuleNames.Append("Boot").ToArray())
			.GetResult();

		Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? new List<string>()));
	}

	/// <summary>
	/// BuildingBlocks.Application è usabile dall'Application dei moduli: niente Infrastructure, niente librerie tecniche.
	/// </summary>
	[Fact]
	public void BuildingBlocks_Application_non_dipende_da_Infrastructure_ne_da_librerie_tecniche()
	{
		var result = Types.InAssembly(ArchAssemblies.BuildingBlocksApplication)
			.That().ResideInNamespaceStartingWith("BuildingBlocks")
			.ShouldNot().HaveDependencyOnAny(
				"BuildingBlocks.Infrastructure", "Microsoft.EntityFrameworkCore", "Npgsql", "Amazon", "ClosedXML", "Microsoft.AspNetCore")
			.GetResult();

		Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? new List<string>()));
	}
}
