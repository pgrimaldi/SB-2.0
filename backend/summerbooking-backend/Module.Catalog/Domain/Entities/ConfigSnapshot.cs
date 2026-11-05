using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Read-model materializzato in jsonb della configurazione di uno stabilimento, servito wholesale al
/// frontend; sempre ricostruibile dalle tabelle catalog_* normalizzate. Non è un aggregate root che
/// incapsula regole di business: è la riga prodotta da un processo di rebuild esterno a questo strato.
/// Nota di implementazione: il modulo mantiene qui solo la migrazione DB — <see cref="Payload"/> è il
/// documento JSON grezzo (jsonb); il mapping tipizzato/polimorfico per snapshot_kind previsto in analisi
/// è un concern di Application/ReadModel, fuori dallo scopo di questo strato di sola persistenza.
/// </summary>
public sealed class ConfigSnapshot : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	public ConfigSnapshotKind SnapshotKind { get; private set; }
	public string Payload { get; private set; } = null!;
	public short PayloadSchema { get; private set; }
	public byte[] SourceHash { get; private set; } = null!;
	public byte[] ContentHash { get; private set; } = null!;
	public DateTime BuiltAt { get; private set; }
	public DateTime? StaleSince { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public const short CurrentSchema = 1;

	private ConfigSnapshot() { }

	public static ConfigSnapshot Create(long propertyId, ConfigSnapshotKind kind) => new() { PropertyId = propertyId, SnapshotKind = kind };

	/// <summary>Nuovo contenuto; false se identico a quello salvato (niente da scrivere).</summary>
	public bool Replace(string payload, byte[] contentHash, DateTime now)
	{
		if (ContentHash is not null && ContentHash.SequenceEqual(contentHash) && PayloadSchema == CurrentSchema && StaleSince is null)
			return false;
		Payload = payload;
		PayloadSchema = CurrentSchema;
		SourceHash = contentHash;
		ContentHash = contentHash;
		BuiltAt = now;
		StaleSince = null;
		return true;
	}

}
