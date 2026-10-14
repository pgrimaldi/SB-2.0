using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Module.Identity.Domain.Entities;
using Shared.Contracts.Audit;

namespace Module.Identity.Application.Services;

/// <summary>2FA dello stabilimento: vale per tutto lo staff della property.</summary>
public sealed class SecuritySettingsService
{
	private readonly CurrentAccount _current;
	private readonly IPropertySecuritySettingsRepository _settings;
	private readonly IUnitOfWork _uow;
	private readonly IAuditTrail _audit;

	public SecuritySettingsService(CurrentAccount current, IPropertySecuritySettingsRepository settings, IUnitOfWork uow, IAuditTrail audit)
	{
		_current = current;
		_settings = settings;
		_uow = uow;
		_audit = audit;
	}

	public async Task<SecuritySettingsDto> GetAsync(CancellationToken ct = default)
		=> SecuritySettingsDto.FromEntity(await _settings.GetAsync(_current.RequirePropertyId(), ct));

	public async Task<SecuritySettingsDto> UpdateAsync(UpdateSecuritySettingsRequest request, CancellationToken ct = default)
	{
		var propertyId = _current.RequirePropertyId();
		var settings = await _settings.GetAsync(propertyId, ct);
		if (settings is null)
		{
			settings = PropertySecuritySettings.Create(propertyId);
			_settings.Add(settings);
		}

		settings.SetTwoFactor(request.TwoFactorEnabled);
		await _uow.SaveChangesAsync(ct);

		await _audit.RecordAsync(new AuditEntry(
			AuditCategory.Security, IdentityAudit.Module, IdentityAudit.SecuritySettingsChanged,
			Metadata: new Dictionary<string, object?> { ["two_factor_enabled"] = request.TwoFactorEnabled }), ct);
		return SecuritySettingsDto.FromEntity(settings);
	}
}
