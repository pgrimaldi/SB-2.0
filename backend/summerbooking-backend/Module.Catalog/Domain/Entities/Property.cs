using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Anagrafica gestionale/di piattaforma dello stabilimento (tenant): stato di approvazione, contatti
/// amministrativi, titolare. Id è il discriminatore multi-tenant copiato per valore nei moduli a valle.
/// </summary>
public sealed class Property : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public string Name { get; private set; } = null!;
	public PropertyStatus Status { get; private set; }
	public string? AdminComment { get; private set; }
	public Guid? OwnerUserPublicId { get; private set; }
	public string? ContactFullName { get; private set; }
	public string? ContactEmail { get; private set; }
	public string? ContactPhone { get; private set; }
	public string? ContactPhoneCountryCode { get; private set; }
	public string? City { get; private set; }
	public short? CountryId { get; private set; }
	public Country? Country { get; private set; }
	public bool IsOnlineSalesSuspended { get; private set; }
	public Guid? ContractDocumentId { get; private set; }
	/// <summary>Abbonamento al gestionale attivo (da Identity): senza, niente vendite online.</summary>
	public bool IsSubscriptionActive { get; private set; } = true;
	/// <summary>Eliminazione definitiva dal cestino: da qui partono i tempi di conservazione dei dati.</summary>
	public DateTime? PurgedAt { get; private set; }
	/// <summary>Primo abbonamento attivo al gestionale (da Identity): ordina la popolarità a parità di «mi piace».</summary>
	public DateOnly? SubscribedSince { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<MapLegendItem> MapLegendItems { get; private set; } = new List<MapLegendItem>();

	private Property() { }

	/// <summary>Struttura registrata dal form pubblico: in attesa di approvazione, senza titolare né paese.</summary>
	public static Property Register(string name, string city, string contactFullName, string contactEmail, string contactPhone, string contactPhoneCountryCode) => new()
	{
		PublicId = Guid.CreateVersion7(),
		Name = name.Trim(),
		City = city.Trim(),
		ContactFullName = contactFullName.Trim(),
		ContactEmail = contactEmail.Trim(),
		ContactPhone = contactPhone.Trim(),
		ContactPhoneCountryCode = contactPhoneCountryCode.Trim(),
		Status = PropertyStatus.Pending,
	};

	public void Rename(string name) => Name = name.Trim();

	public void SetCountry(short? countryId) => CountryId = countryId;

	/// <summary>Approvazione, rifiuto o ritorno «in attesa»; il commento del systemadmin è facoltativo.</summary>
	public void ChangeStatus(PropertyStatus status, string? comment)
	{
		Status = status;
		AdminComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
	}

	public void AssignOwner(Guid ownerUserId) => OwnerUserPublicId = ownerUserId;

	public void SetOnlineSalesSuspended(bool suspended) => IsOnlineSalesSuspended = suspended;

	public void SetSubscriptionActive(bool active, DateOnly today)
	{
		IsSubscriptionActive = active;
		if (active)
			SubscribedSince ??= today;
	}

	public void Purge(DateTime now) => PurgedAt = now;
}
