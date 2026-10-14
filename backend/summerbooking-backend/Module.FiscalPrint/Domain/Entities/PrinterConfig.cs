using Module.FiscalPrint.Domain.Enums;
using Shared.Contracts;

namespace Module.FiscalPrint.Domain.Entities;

/// <summary>
/// Configurazione di una stampante (termica/fiscale RT) di uno stabilimento: modello, connessione,
/// modalità d'uso, intestazioni scontrino.
/// </summary>
public sealed class PrinterConfig : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public string Name { get; private set; } = null!;
	public PrinterModel Model { get; private set; }
	public PrinterMode Mode { get; private set; }
	public ConnectionKind ConnectionKind { get; private set; }
	public string? Host { get; private set; }
	public int? Port { get; private set; }
	public string? UsbDevice { get; private set; }
	public bool UseTls { get; private set; }
	public bool IsActive { get; private set; }
	public string? HeadingLine1 { get; private set; }
	public string? HeadingLine2 { get; private set; }
	public string? FooterLine { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public Guid? UpdatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<PrinterDepartment> Departments { get; private set; } = new List<PrinterDepartment>();

	private PrinterConfig() { }
}
