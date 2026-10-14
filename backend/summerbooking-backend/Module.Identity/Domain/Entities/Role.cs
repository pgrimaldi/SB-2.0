using Shared.Contracts;

namespace Module.Identity.Domain.Entities;

/// <summary>
/// Catalogo (lookup) dei ruoli applicativi; referenziata da Account.RoleId.
/// </summary>
public sealed class Role : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public string Code { get; private set; } = null!;
	public bool IsAssignableViaPanel { get; private set; }
	public bool IsStaff { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private Role() { }
}
