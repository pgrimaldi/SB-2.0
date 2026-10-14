using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Catalogo master (globale, nessun property_id) delle icone disponibili per i tre tipi di servizio
/// venduto (bar/normale/a ore).
/// </summary>
public sealed class ServiceTypeIcon : IAuditable, ISoftDeletable
{
	public int Id { get; private set; }
	public ServiceSaleKind Type { get; private set; }
	public string Name { get; private set; } = null!;
	public long IconAssetId { get; private set; }
	public bool IsActive { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ServiceTypeIcon() { }
}
