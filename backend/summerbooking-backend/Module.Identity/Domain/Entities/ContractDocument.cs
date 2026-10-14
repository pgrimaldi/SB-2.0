using Module.Identity.Domain.Enums;
using Shared.Contracts;
using Shared.Contracts.Errors;

namespace Module.Identity.Domain.Entities;

/// <summary>
/// Documento contrattuale (addendum) pubblicato dalla piattaforma, con versionamento immutabile:
/// dopo la pubblicazione non si modifica, una nuova versione e' una nuova riga.
/// </summary>
public sealed class ContractDocument : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public string VersionCode { get; private set; } = null!;
	public string? Title { get; private set; }
	public StorageOrigin DocumentOrigin { get; private set; }
	public string DocumentPath { get; private set; } = null!;
	public string? ContentSha256 { get; private set; }
	public ContractDocumentStatus Status { get; private set; }
	public long? SupersedesId { get; private set; }
	public ContractDocument? Supersedes { get; private set; }
	public string? Notes { get; private set; }
	public DateTime? PublishedAt { get; private set; }
	public DateTime? SupersededAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ContractDocument() { }

	public static ContractDocument CreateDraft(
		string versionCode, string? title, StorageOrigin origin, string path, string contentSha256, string? notes) => new()
	{
		PublicId = Guid.CreateVersion7(),
		VersionCode = versionCode.Trim(),
		Title = title,
		DocumentOrigin = origin,
		DocumentPath = path,
		ContentSha256 = contentSha256,
		Status = ContractDocumentStatus.Draft,
		Notes = notes,
	};

	/// <summary>Pubblica questa versione e ritira quella in vigore, se c'e'.</summary>
	public void Publish(ContractDocument? current, DateTime now)
	{
		if (Status != ContractDocumentStatus.Draft)
			throw DomainException.Unprocessable(IdentityErrors.ContractInvalidTransition);

		if (current is not null)
		{
			current.Status = ContractDocumentStatus.Retired;
			current.SupersededAt = now;
			Supersedes = current;
		}

		Status = ContractDocumentStatus.Published;
		PublishedAt = now;
	}
}
