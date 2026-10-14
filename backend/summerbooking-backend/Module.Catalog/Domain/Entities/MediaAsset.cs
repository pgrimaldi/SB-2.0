using Module.Catalog.Domain.Enums;
using Module.Catalog.Domain.ValueObjects;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Catalogo locale al modulo catalog di tutti i riferimenti a file (foto, PDF, icone) usati dalle
/// entità C2; referenziata solo internamente allo schema catalog (nessun public_id).
/// </summary>
public sealed class MediaAsset : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public MediaRef Ref { get; private set; } = null!;
	public MediaKind Kind { get; private set; }
	public byte[]? Checksum { get; private set; }
	public int? ByteSize { get; private set; }
	public string? ContentType { get; private set; }

	/// <summary>
	/// Property a cui il file appartiene quando <b>non esiste (ancora) una riga di aggancio</b>: foto di
	/// stabilimenti registrati o approvati che non hanno aperto la vetrina. Resta <c>NULL</c> per tutti
	/// gli asset che hanno un utilizzatore vero, dove la riga di aggancio rimane la fonte
	/// dell'appartenenza. FK logica verso <c>catalog.properties.public_id</c>, non dichiarata, come fa
	/// già <c>notification.media_assets.property_id</c>.
	/// </summary>
	/// <remarks>
	/// Serve alle policy di retention sui file: senza questa colonna una foto senza aggancio ha la
	/// stessa firma di un file orfano e verrebbe cancellata il giorno dopo la migrazione.
	/// </remarks>
	public long? PropertyId { get; private set; }

	/// <summary>
	/// Lo stabilimento proprietario, quando esiste. Navigazione dichiarata perché la chiave esterna è
	/// vera: siamo dentro lo stesso schema.
	/// </summary>
	public Property? Property { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private MediaAsset() { }
}
