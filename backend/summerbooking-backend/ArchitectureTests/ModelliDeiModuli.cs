using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Shared.Infrastructure;

namespace ArchitectureTests;

/// <summary>
/// Costruisce i modelli EF dei moduli <b>senza toccare un database</b>: <c>context.Model</c> si
/// materializza dalle sole <c>IEntityTypeConfiguration</c>, quindi basta una connection string
/// sintattica e nessuna connessione viene mai aperta.
/// </summary>
/// <remarks>
/// Copre i moduli che seminano davvero dati di riferimento con <c>HasData</c> — oggi <b>Identity</b>
/// (ruoli, piani di licenza e relative traduzioni) e <b>Catalog</b> (metodi di calcolo): sono gli
/// unici quattro <c>HasData</c> del backend. Quando un altro modulo inizia a seminare, va aggiunto
/// qui: la riga in piu' costa nulla, il test che non lo guarda costa un difetto silenzioso.
/// </remarks>
internal static class ModelliDeiModuli
{
	/// <summary>Connection string mai usata per connettersi: serve solo a far configurare il provider.</summary>
	private const string ConnectionStringFinta = "Host=localhost;Database=architecture_tests";

	internal static IModel Identity() => Costruisci<Module.Identity.Infrastructure.DbContext>();

	internal static IModel Catalog() => Costruisci<Module.Catalog.Infrastructure.DbContext>();

	internal static IEnumerable<(string Modulo, IModel Modello)> Tutti()
	{
		yield return ("Identity", Identity());
		yield return ("Catalog", Catalog());
	}

	private static IModel Costruisci<TContext>() where TContext : DbContext
	{
		var opzioni = new DbContextOptionsBuilder<TContext>()
			.UseNpgsql(ConnectionStringFinta)
			.Options;

		var contesto = (TContext)Activator.CreateInstance(
			typeof(TContext), opzioni, new OrologioFermo())!;

		using (contesto)
			// IDesignTimeModel e non context.Model: il modello di runtime e' ottimizzato per la
			// lettura e non conserva i dati di HasData ("The requested configuration is not stored in
			// the read-optimized model"). E' lo stesso modello che usa "dotnet ef", cioe' esattamente
			// quello da cui nascono le migration — il punto giusto da confrontare.
			return contesto.GetService<IDesignTimeModel>().Model;
	}

	/// <summary>
	/// Sostituto di <see cref="IDateTimeProvider"/>: il modello non lo usa, ma il costruttore del
	/// contesto lo richiede. Un valore fisso rende anche esplicito che qui non si misura il tempo.
	/// </summary>
	private sealed class OrologioFermo : IDateTimeProvider
	{
		public DateTime UtcNow => new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	}
}
