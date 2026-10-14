using Shared.Contracts;

namespace Module.FiscalPrint.Domain.Entities;

/// <summary>
/// Mappatura reparto fisico del registratore di cassa RT → aliquota IVA con cui il reparto è
/// pre-programmato.
/// </summary>
public sealed class PrinterDepartment : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PrinterId { get; private set; }
	public Guid PropertyId { get; private set; }
	public short DeptNumber { get; private set; }
	public decimal VatRate { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public PrinterConfig Printer { get; private set; } = null!;

	private PrinterDepartment() { }
}
