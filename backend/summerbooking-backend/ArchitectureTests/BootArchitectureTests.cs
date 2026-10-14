using System.Collections.Generic;
using System.Reflection;
using NetArchTest.Rules;

namespace ArchitectureTests;

/// <summary>
/// Regole di confine relative a Boot (composition root): Boot puo dipendere dai moduli,
/// ma nessun modulo deve dipendere da Boot.
/// </summary>
public class BootArchitectureTests
{
	public static TheoryData<string> ModuleNames
	{
		get
		{
			var data = new TheoryData<string>();
			foreach (var name in ArchAssemblies.ModuleNames)
				data.Add(name);
			return data;
		}
	}

	// Parametrizzata su ArchAssemblies.ModuleNames (tutti i Module.*.dll presenti nell'output,
	// _Template escluso): un modulo nuovo entra nella regola senza che nessuno debba ricordarsi
	// di aggiungere un metodo di test. Prima la regola era scritta a mano solo per Identity e
	// ZTest, quindi gli altri nove moduli non erano coperti da nulla.
	[Theory]
	[MemberData(nameof(ModuleNames))]
	public void Nessun_modulo_dipende_da_Boot(string moduleName)
	{
		var result = Types.InAssembly(Assembly.Load(moduleName))
			.That().ResideInNamespaceStartingWith(moduleName)
			.ShouldNot().HaveDependencyOn("Boot")
			.GetResult();

		Assert.True(result.IsSuccessful, $"{moduleName}: {Format(result)}");
	}

	private static string Format(TestResult result)
		=> string.Join(", ", result.FailingTypeNames ?? new List<string>());
}
