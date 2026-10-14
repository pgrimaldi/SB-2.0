namespace Module.Audit.Domain.Enums;

/// <summary>Famiglia dell'azione: decide chi la legge e la retention (audit.entry_category).</summary>
public enum EntryCategory
{
	Operator = 0,
	Security = 1,
	Platform = 2
}
