using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Retention;

/// <summary>
/// Un job che applica <b>una</b> policy il cui perimetro non è esprimibile in una riga di
/// configurazione.
/// </summary>
/// <remarks>
/// <para>
/// <b>Perché esiste, dato che c'è un motore generico.</b> Il motore legge le regole da
/// <c>privacy_retention_policies</c> e le applica: durata, decorrenza, azione, e dal 2026-10-05 anche
/// il perimetro (<c>target_filter</c>). Copre tutto tranne i perimetri che non sono un confronto su
/// una colonna.
/// </para>
/// <para>
/// Il caso che ha richiesto questa interfaccia: «alla cessazione dello stabilimento, dopo 24 mesi,
/// cancella le foto <b>oltre le prime tre per <c>sort_order</c></b>, e <b>mai</b> quella indicata come
/// copertina». È «escludi le prime N secondo un ordinamento» più «escludi quella puntata da una
/// colonna di un'altra tabella»: nessuna sintassi di filtro ragionevole la esprime, e inventare un
/// perimetro approssimato vorrebbe dire cancellare la scheda pubblica che il titolare ha chiesto
/// esplicitamente di conservare.
/// </para>
/// <para>
/// ⚠️ <b>Un job a mano resta comunque governato dalla tabella.</b> Legge la sua policy per
/// <see cref="PolicyPublicId"/> e ne prende durata, decorrenza e <c>is_dry_run_only</c>: scrive nel
/// codice <b>solo il perimetro</b>, che è la parte che non si può dichiarare. Se la policy non c'è, è
/// disattivata o è stata cancellata logicamente, il job non fa niente. Cablare anche la durata
/// significherebbe tornare a dover fare una release per cambiare un numero.
/// </para>
/// <para>
/// Scrive il proprio esito nel verbale con <see cref="RetentionRunLog"/>, lo stesso del motore, così
/// il report di una corsa è uno.
/// </para>
/// </remarks>
public interface IBespokeRetentionJob
{
	/// <summary>Nome leggibile, per il report e per i log.</summary>
	string Name { get; }

	/// <summary>
	/// La policy che questo job implementa. ⚠️ Deve essere una che il motore <b>salta</b>: se il motore
	/// la eseguisse, le due esecuzioni farebbero lo stesso lavoro con perimetri diversi.
	/// </summary>
	Guid PolicyPublicId { get; }

	/// <summary>Esegue il job. Non solleva: raccoglie l'errore nell'esito, come fa il motore.</summary>
	Task<BespokeRetentionOutcome> RunAsync(RetentionRunOptions options, CancellationToken ct);
}

/// <summary>Esito di un job scritto a mano, nella forma che il report sa stampare.</summary>
public sealed class BespokeRetentionOutcome
{
	public string JobName { get; init; } = "";
	public string SchemaName { get; init; } = "";
	public string TableName { get; init; } = "";

	/// <summary>Righe nel perimetro, oltre il taglio. ⚠️ È il numero che si legge nel report di prova.</summary>
	public long MatchedRows { get; set; }

	/// <summary>Righe davvero toccate. Zero in prova, anche con <see cref="MatchedRows"/> alto.</summary>
	public long AffectedRows { get; set; }

	public PolicyRunStatus Status { get; set; } = PolicyRunStatus.Succeeded;
	public string? SkipReason { get; set; }
	public string? Error { get; set; }
	public DateTime? CutoffAt { get; set; }
	public TimeSpan Duration { get; set; }

	public override string ToString() => JobName;
}

public static class BespokeRetentionRegistration
{
	/// <summary>
	/// Registra un job scritto a mano sul <c>DbContext</c> del proprio modulo.
	/// </summary>
	/// <remarks>
	/// Come <c>AddModuleRetention</c>: il job usa la <b>propria</b> connessione, non quella di un altro
	/// modulo. Che poi la sua SQL legga anche tabelle di un altro schema è un fatto del database — gli
	/// schemi stanno nello stesso database — e non una dipendenza di codice verso un altro modulo, che
	/// le regole di architettura vietano.
	/// </remarks>
	public static IServiceCollection AddBespokeRetention<TJob, TContext>(this IServiceCollection services)
		where TJob : class, IBespokeRetentionJob
		where TContext : DbContext
	{
		services.AddScoped<IBespokeRetentionJob, TJob>();
		return services;
	}
}
