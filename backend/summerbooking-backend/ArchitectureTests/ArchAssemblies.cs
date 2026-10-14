using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ArchitectureTests;

/// <summary>
/// Assembly della soluzione caricati per nome, usati come radice delle regole NetArchTest.
/// Il caricamento per nome evita di dover esporre tipi "seed" pubblici dai moduli.
/// </summary>
internal static class ArchAssemblies
{
	public static readonly Assembly Identity = Assembly.Load("Module.Identity");
	public static readonly Assembly ZTest = Assembly.Load("Module.ZTest");
	public static readonly Assembly Shared = Assembly.Load("Shared");
	public static readonly Assembly Boot = Assembly.Load("Boot");
	public static readonly Assembly BuildingBlocks = Assembly.Load("BuildingBlocks.Infrastructure");
	public static readonly Assembly BuildingBlocksApplication = Assembly.Load("BuildingBlocks.Application");

	/// <summary>
	/// Nomi di tutti gli assembly dei moduli applicativi (<c>Module.*</c>), ricavati dal
	/// contenuto della cartella di output. Serve alle regole parametrizzate (<c>[Theory]</c>):
	/// un modulo nuovo, una volta referenziato dal progetto di test, entra automaticamente
	/// in tutte le convenzioni senza doverle riscrivere a mano.
	/// </summary>
	public static readonly IReadOnlyList<string> ModuleNames =
		Directory.GetFiles(AppContext.BaseDirectory, "Module.*.dll")
			.Select(path => Path.GetFileNameWithoutExtension(path)!)
			.Where(name => !name.Contains("_Template", StringComparison.Ordinal))
			.Distinct(StringComparer.Ordinal)
			.OrderBy(name => name, StringComparer.Ordinal)
			.ToArray();

	/// <summary>Assembly dei moduli applicativi, nello stesso ordine di <see cref="ModuleNames"/>.</summary>
	public static IEnumerable<Assembly> Modules => ModuleNames.Select(Assembly.Load);
}
