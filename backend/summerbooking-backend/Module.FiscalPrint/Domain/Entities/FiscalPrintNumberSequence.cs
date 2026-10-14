using Shared.Contracts;

namespace Module.FiscalPrint.Domain.Entities;

/// <summary>
/// Contatore del numero progressivo scontrino RT, una riga per (printer_id, fiscal_date);
/// incrementato atomicamente a ogni stampa. Non implementa ISoftDeletable: deleted_at è esposto solo
/// per uniformità di colonna con le altre tabelle del modulo, ma non partecipa mai al soft-delete
/// (tabella di sequenza/contatore, non entità di dominio cancellabile — vedi scheda).
/// </summary>
public sealed class FiscalPrintNumberSequence : IAuditable
{
	public long Id { get; private set; }
	public long PrinterId { get; private set; }
	public Guid PropertyId { get; private set; }
	public DateOnly FiscalDate { get; private set; }
	public int LastNumber { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public PrinterConfig Printer { get; private set; } = null!;

	private FiscalPrintNumberSequence() { }
}
