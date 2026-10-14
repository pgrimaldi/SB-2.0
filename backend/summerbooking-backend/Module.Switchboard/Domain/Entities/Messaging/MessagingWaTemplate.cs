using Module.Switchboard.Domain.Enums.Messaging;
using Shared.Contracts;

namespace Module.Switchboard.Domain.Entities.Messaging;

/// <summary>
/// Cache/audit locale di un template WhatsApp Business registrato su Meta per uno stabilimento; non
/// governa l'invio (l'IVR/API passano un template_name libero), la fonte di verità resta Meta.
/// </summary>
public sealed class MessagingWaTemplate : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long ConfigId { get; private set; }
	public Guid PropertyId { get; private set; }
	public string TemplateName { get; private set; } = null!;
	public string Language { get; private set; } = null!;
	public WaTemplateCategory Category { get; private set; }
	public string Components { get; private set; } = null!;
	public WaTemplateStatus MetaStatus { get; private set; }
	public string? MetaStatusRaw { get; private set; }
	public string? MetaTemplateId { get; private set; }
	public string? MetaRejectionReason { get; private set; }
	public WaHeaderFormat HeaderFormat { get; private set; }
	public bool HasButtons { get; private set; }
	public short ButtonsCount { get; private set; }
	public short BodyParamCount { get; private set; }
	public string? BodyPreview { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public SwitchboardConfig Config { get; private set; } = null!;

	private MessagingWaTemplate() { }   // unico costruttore, per il materializzatore di EF Core
}
