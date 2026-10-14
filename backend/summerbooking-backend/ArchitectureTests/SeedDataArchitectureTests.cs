using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ArchitectureTests;

/// <summary>
/// Regola sui dati di riferimento seminati con <c>HasData</c>.
/// </summary>
/// <remarks>
/// <para>
/// Nasce da un difetto reale, trovato il 2026-09-30:
/// <c>identity.access_roles.is_staff</c> era <c>true</c> per tutti e tredici i ruoli, mentre il seed
/// ne dichiarava <c>false</c> due — <c>systemadmin</c> e <c>consumer</c>. La causa non era il seed,
/// che era corretto, ma un <c>HasDefaultValue(true)</c> sulla colonna: <b>EF omette dall'INSERT le
/// colonne il cui valore coincide col default CLR del tipo</b> (<c>false</c> per <c>bool</c>,
/// <c>0</c> per i numerici), perche' non distingue "impostato a false" da "non impostato". Omessa la
/// colonna, vince il default del database e il valore seminato evapora.
/// </para>
/// <para>
/// Il difetto e' <b>silenzioso</b>: nessun errore, nessuna migration che fallisce. Si manifesta mesi
/// dopo, quando qualcuno legge quel flag — e nel caso di <c>is_staff</c> avrebbe dichiarato invalidi
/// 5.987 utenti <c>consumer</c> legittimi, oppure fatto fallire ogni nuova registrazione dall'app.
/// Undici valori su tredici erano giusti per coincidenza, non per controllo.
/// </para>
/// <para>
/// Un controllo sull'entita' che legge il flag non lo avrebbe intercettato, perche' quel codice e'
/// corretto: la divergenza sta fra cio' che il modello dichiara e cio' che finisce nel database.
/// Questo test guarda il punto giusto, il modello, e non ha bisogno di un database: e' la stessa
/// famiglia del tranello sulle colonne enum (<c>HasDefaultValueSql</c>, mai <c>HasDefaultValue</c>).
/// </para>
/// </remarks>
public class SeedDataArchitectureTests
{
	/// <summary>
	/// Nessuna proprieta' seminata con <c>HasData</c> puo' dichiarare un <c>HasDefaultValue</c>
	/// diverso dal default CLR del proprio tipo: quel valore, se seminato, non arriverebbe al
	/// database.
	/// </summary>
	[Fact]
	public void Nessuna_colonna_seminata_dichiara_un_default_che_ne_annulla_il_valore()
	{
		var violazioni = new List<string>();

		foreach (var (modulo, modello) in ModelliDeiModuli.Tutti())
		{
			foreach (var entita in modello.GetEntityTypes())
			{
				var righeSeminate = entita.GetSeedData().ToList();
				if (righeSeminate.Count == 0)
					continue;

				foreach (var proprieta in entita.GetProperties())
				{
					if (!proprieta.TryGetDefaultValue(out var dichiarato) || dichiarato is null)
						continue;

					var defaultClr = DefaultClr(proprieta.ClrType);
					if (Equals(dichiarato, defaultClr))
						continue;   // un default pari al default CLR e' innocuo: il valore seminato coincide

					// La colonna e' seminata con il default CLR in almeno una riga? Quella riga
					// perderebbe il proprio valore.
					var righeARischio = righeSeminate.Count(riga =>
						riga.TryGetValue(proprieta.Name, out var valore) && Equals(valore, defaultClr));
					if (righeARischio == 0)
						continue;

					violazioni.Add(
						$"{modulo}: {entita.ShortName()}.{proprieta.Name} dichiara HasDefaultValue({dichiarato}) " +
						$"ma {righeARischio} riga/e di HasData la semina a {Descrivi(defaultClr)}: quel valore " +
						"non arriverebbe al database (EF omette la colonna dall'INSERT). Togliere il default: " +
						"una tabella di riferimento che semina tutte le proprie righe non ne ha bisogno.");
				}
			}
		}

		Assert.True(violazioni.Count == 0, string.Join(Environment.NewLine, violazioni));
	}

	/// <summary>
	/// I ruoli che non appartengono a uno stabilimento non possono essere marcati come personale:
	/// <c>is_staff = true</c> implica <c>property_id</c> obbligatorio sull'account, e ne'
	/// <c>systemadmin</c> (sta sopra gli stabilimenti) ne' <c>consumer</c> (e' il cliente dell'app)
	/// ne ha uno.
	/// </summary>
	[Fact]
	public void I_ruoli_non_operativi_non_sono_marcati_come_personale()
	{
		var modello = ModelliDeiModuli.Identity();
		var ruoli = modello.GetEntityTypes()
			.Single(e => e.GetTableName() == "access_roles")
			.GetSeedData()
			.ToDictionary(r => (string)r["Code"]!, r => (bool)r["IsStaff"]!);

		var attesi = new Dictionary<string, bool>
		{
			["systemadmin"] = false,   // ruolo di piattaforma: sta sopra gli stabilimenti
			["consumer"] = false,      // cliente finale dell'app: usa gli stabilimenti, non ci lavora
		};

		var errori = attesi
			.Where(atteso => !ruoli.TryGetValue(atteso.Key, out var reale) || reale != atteso.Value)
			.Select(atteso => $"il ruolo '{atteso.Key}' deve avere IsStaff = {atteso.Value}, " +
				$"trovato {(ruoli.TryGetValue(atteso.Key, out var r) ? r.ToString() : "assente")}")
			.ToList();

		Assert.True(errori.Count == 0, string.Join(Environment.NewLine, errori));
	}

	private static object? DefaultClr(Type tipo)
	{
		var sottostante = Nullable.GetUnderlyingType(tipo) ?? tipo;
		return sottostante.IsValueType && sottostante == tipo ? Activator.CreateInstance(sottostante) : null;
	}

	private static string Descrivi(object? valore) => valore switch
	{
		null => "null",
		bool b => b ? "true" : "false",
		_ => valore.ToString() ?? "?",
	};
}
