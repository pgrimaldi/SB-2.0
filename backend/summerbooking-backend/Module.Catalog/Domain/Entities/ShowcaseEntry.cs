using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Struttura messa dal systemadmin in una sezione della vetrina («In evidenza», «Più frequentati»), in ordine.</summary>
public sealed class ShowcaseEntry : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public ShowcaseSection Section { get; private set; }
	public long BathhouseId { get; private set; }
	public Bathhouse? Bathhouse { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ShowcaseEntry() { }

	public static ShowcaseEntry Create(ShowcaseSection section, long bathhouseId, short sortOrder)
		=> new() { Section = section, BathhouseId = bathhouseId, SortOrder = sortOrder };

	public void MoveTo(short sortOrder) => SortOrder = sortOrder;
}
