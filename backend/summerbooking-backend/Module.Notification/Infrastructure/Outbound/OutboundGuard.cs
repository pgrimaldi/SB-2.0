using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Module.Notification.Infrastructure.Outbound;

/// <summary>Chi può ricevere un messaggio inviato da questo ambiente.</summary>
public enum OutboundMode
{
	/// <summary>Nessun messaggio esce: l'invio viene soppresso e resta solo nel log.</summary>
	Off,
	/// <summary>Escono solo i messaggi diretti ai destinatari di <see cref="OutboundOptions.AllowedRecipients"/>.</summary>
	Allowlist,
	/// <summary>Esce tutto. Ammesso solo in Production.</summary>
	Live,
}

/// <summary>Sezione <c>Notification:Outbound</c>: vale per ogni canale (email, WhatsApp, SMS, SMTP degli stabilimenti).</summary>
public sealed class OutboundOptions
{
	public const string Section = "Notification:Outbound";

	/// <summary>Default <see cref="OutboundMode.Off"/>: un ambiente configurato a metà non scrive a nessuno.</summary>
	public OutboundMode Mode { get; set; } = OutboundMode.Off;

	/// <summary>
	/// Destinatari ammessi in <see cref="OutboundMode.Allowlist"/>: indirizzi email esatti, domini
	/// (<c>@summerbooking.it</c>) e numeri di telefono in formato internazionale (<c>+39…</c>).
	/// </summary>
	public List<string> AllowedRecipients { get; set; } = [];
}

/// <summary>
/// Unico punto che decide se un messaggio può uscire. Ogni mittente del modulo (email transazionali,
/// WhatsApp, SMS, SMTP configurati dagli stabilimenti) lo interroga prima di consegnare: in un ambiente di
/// prova con dati reali è la barriera che impedisce di scrivere ai clienti veri.
/// </summary>
public sealed class OutboundGuard(IOptions<OutboundOptions> options)
{
	private readonly OutboundOptions _options = options.Value;

	public OutboundMode Mode => _options.Mode;

	public bool Allows(string recipient)
	{
		switch (_options.Mode)
		{
			case OutboundMode.Live:
				return true;
			case OutboundMode.Off:
				return false;
		}

		var value = Normalize(recipient);
		foreach (var entry in _options.AllowedRecipients.Select(Normalize))
		{
			if (entry.Length == 0)
				continue;
			if (entry.StartsWith('@') ? value.EndsWith(entry, StringComparison.Ordinal) : value == entry)
				return true;
		}
		return false;
	}

	/// <summary>Email in minuscolo; telefoni ridotti a <c>+</c> e cifre, così "+39 333 12…" vale "+3933312…".</summary>
	private static string Normalize(string value)
	{
		var trimmed = value.Trim().ToLowerInvariant();
		return trimmed.StartsWith('+') && !trimmed.Contains('@')
			? "+" + new string(trimmed.Where(char.IsDigit).ToArray())
			: trimmed;
	}
}

/// <summary>
/// Controllo all'avvio: <see cref="OutboundMode.Live"/> solo in Production, e un'allowlist vuota non ha senso.
/// Un ambiente di prova con dati copiati dalla produzione non deve poter scrivere ai clienti veri per una
/// configurazione sbagliata.
/// </summary>
public sealed class OutboundOptionsValidator(IHostEnvironment environment) : IValidateOptions<OutboundOptions>
{
	public ValidateOptionsResult Validate(string? name, OutboundOptions options)
	{
		if (options.Mode == OutboundMode.Live && !environment.IsProduction())
			return ValidateOptionsResult.Fail(
				$"{OutboundOptions.Section}:Mode=Live non è ammesso in {environment.EnvironmentName}: usare Off o Allowlist.");
		if (options.Mode == OutboundMode.Allowlist && options.AllowedRecipients.All(string.IsNullOrWhiteSpace))
			return ValidateOptionsResult.Fail(
				$"{OutboundOptions.Section}:Mode=Allowlist senza {OutboundOptions.Section}:AllowedRecipients: usare Off.");
		return ValidateOptionsResult.Success;
	}
}
