using Module.Fiskaly.Domain.Enums;
using Shared.Contracts;

namespace Module.Fiskaly.Domain.Entities;

/// <summary>
/// Catalogo (lookup) dei codici IVA e di esenzione ammessi da Fiskaly SIGN IT per le righe del
/// documento commerciale. Cresce senza deploy, seedato in migration; referenziata con FK reale da
/// ReceiptLine.VatCodeId.
/// </summary>
public sealed class VatCode : IAuditable
{
	public short Id { get; private set; }
	public string Code { get; private set; } = null!;
	public FiskalyVatKind Kind { get; private set; }
	public decimal? Rate { get; private set; }
	public string? NatureCode { get; private set; }
	public bool IsActive { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }

	/// <summary>Esposta per uniformità di schema: un codice IVA non si cancella, si disattiva con IsActive (non ISoftDeletable).</summary>
	public DateTime? DeletedAt { get; private set; }

	private VatCode() { }   // unico costruttore, per il materializzatore di EF Core
}
