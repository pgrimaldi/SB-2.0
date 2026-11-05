using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Tavoli di un servizio con lo stesso numero di posti (es. 5 tavoli da 4).</summary>
public sealed class TableServiceTableType : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long TableServiceId { get; private set; }
	public short Seats { get; private set; }
	public short Count { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private TableServiceTableType() { }

	internal static TableServiceTableType Create(short seats, short count) => new() { Seats = seats, Count = count };
}
