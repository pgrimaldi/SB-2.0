using Module.Catalog.Application.Abstractions;
using Module.Catalog.Domain;
using Shared.Contracts.Errors;
using Shared.Security;

namespace Module.Catalog.Application.Services;

/// <summary>Stabilimento della richiesta: sempre dal claim del token, mai dal corpo o dalla querystring.</summary>
public sealed class CurrentProperty
{
	private readonly ICurrentUser _user;
	private readonly IPropertyRepository _properties;
	private long? _id;

	public CurrentProperty(ICurrentUser user, IPropertyRepository properties)
	{
		_user = user;
		_properties = properties;
	}

	private Guid? _fixed;

	public Guid PublicId => _fixed ?? _user.PropertyId ?? throw DomainException.Forbidden(CatalogErrors.PropertyRequired);

	/// <summary>Fuori da una richiesta (job, rigenerazioni): lo stabilimento su cui lavorano i servizi di questo scope.</summary>
	public void Use(long id, Guid publicId)
	{
		_id = id;
		_fixed = publicId;
	}

	/// <summary>Id interno, risolto una volta per richiesta.</summary>
	public async Task<long> IdAsync(CancellationToken ct = default)
		=> _id ??= await _properties.FindIdAsync(PublicId, ct) ?? throw DomainException.NotFound(CatalogErrors.PropertyNotFound);
}
