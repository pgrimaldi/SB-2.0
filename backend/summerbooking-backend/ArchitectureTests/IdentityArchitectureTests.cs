using System.Collections.Generic;
using NetArchTest.Rules;

namespace ArchitectureTests;

/// <summary>
/// Regole di confine (boundary) per il modulo Identity.
/// </summary>
public class IdentityArchitectureTests
{
	[Fact]
	public void Domain_non_dipende_da_Infrastructure()
	{
		var result = Types.InAssembly(ArchAssemblies.Identity)
			.That().ResideInNamespaceStartingWith("Module.Identity.Domain")
			.ShouldNot().HaveDependencyOn("Module.Identity.Infrastructure")
			.GetResult();

		Assert.True(result.IsSuccessful, Format(result));
	}

	[Fact]
	public void Application_non_dipende_da_Infrastructure()
	{
		var result = Types.InAssembly(ArchAssemblies.Identity)
			.That().ResideInNamespaceStartingWith("Module.Identity.Application")
			.ShouldNot().HaveDependencyOn("Module.Identity.Infrastructure")
			.GetResult();

		Assert.True(result.IsSuccessful, Format(result));
	}

	[Fact]
	public void Domain_e_Application_non_dipendono_da_EFCore_o_AspNetCore()
	{
		var result = Types.InAssembly(ArchAssemblies.Identity)
			.That().ResideInNamespaceStartingWith("Module.Identity.Domain")
			.Or().ResideInNamespaceStartingWith("Module.Identity.Application")
			.ShouldNot().HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
			.GetResult();

		Assert.True(result.IsSuccessful, Format(result));
	}

	[Fact]
	public void Identity_non_dipende_dagli_altri_moduli()
	{
		var result = Types.InAssembly(ArchAssemblies.Identity)
			.That().ResideInNamespaceStartingWith("Module.Identity")
			.ShouldNot().HaveDependencyOn("Module.ZTest")
			.GetResult();

		Assert.True(result.IsSuccessful, Format(result));
	}

	private static string Format(TestResult result)
		=> string.Join(", ", result.FailingTypeNames ?? new List<string>());
}
