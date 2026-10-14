using BuildingBlocks.Infrastructure.Storage;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Identity.Application;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Services;
using Module.Identity.Endpoints;
using Module.Identity.Infrastructure.ExternalApi;
using Module.Identity.Infrastructure.Infras;
using Module.Identity.Infrastructure.Repositories;
using Module.Identity.Infrastructure.Security;
using Shared.Contracts.Identity;
using Shared.Security;

namespace Module.Identity;

/// <summary>
/// Punto di composizione del modulo Identity: registra i servizi del modulo nel container DI.
/// Invocato dal progetto Boot in fase di avvio: builder.Services.AddIdentityModule();
/// </summary>
public static class Module
{
	public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddModuleIdentityPersistence(configuration);

		services.Configure<IdentityAuthOptions>(configuration.GetSection(IdentityAuthOptions.Section));
		services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));
		services.Configure<PasswordHashOptions>(configuration.GetSection(PasswordHashOptions.Section));
		services.Configure<SocialAuthOptions>(configuration.GetSection(SocialAuthOptions.Section));
		services.Configure<RefreshCookieOptions>(configuration.GetSection(RefreshCookieOptions.Section));

		// Esempi e valori ammessi nello Swagger: la conoscenza dei DTO resta nel modulo.
		services.ConfigureSwaggerGen(IdentityOpenApi.Configure);

		services.AddMemoryCache();
		services.AddHttpClient(SocialIdentityVerifier.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(10));

		// Infrastructure: repository sul DbContext (Scoped), servizi tecnici senza stato (Singleton).
		services.AddScoped<IAccountRepository, AccountRepository>();
		services.AddScoped<IRoleRepository, RoleRepository>();
		services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
		services.AddScoped<IAuthChallengeRepository, AuthChallengeRepository>();
		services.AddScoped<IPropertySecuritySettingsRepository, PropertySecuritySettingsRepository>();
		services.AddScoped<ICustomerRepository, CustomerRepository>();
		services.AddScoped<IBathhouseLikeRepository, BathhouseLikeRepository>();
		services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
		services.AddScoped<ILicensePlanRepository, LicensePlanRepository>();
		services.AddScoped<IContractRepository, ContractRepository>();
		services.AddScoped<IUnitOfWork, UnitOfWork>();

		services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
		// Password diffuse: elenco ricaricabile e job mensile notturno che lo aggiorna.
		services.Configure<CommonPasswordOptions>(configuration.GetSection(CommonPasswordOptions.Section));
		services.AddSingleton<CommonPasswordList>();
		services.AddSingleton<ICommonPasswordList>(sp => sp.GetRequiredService<CommonPasswordList>());
		services.AddHttpClient(CommonPasswordDownloader.HttpClientName, c => c.Timeout = TimeSpan.FromMinutes(2));
		services.AddSingleton<CommonPasswordDownloader>();
		if (configuration.GetSection(CommonPasswordOptions.Section).GetValue(nameof(CommonPasswordOptions.RefreshEnabled), true))
			services.AddHostedService<CommonPasswordRefreshJob>();
		services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
		services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
		services.AddSingleton<ILoginThrottle, MemoryLoginThrottle>();
		services.AddSingleton<IReplayGuard, MemoryReplayGuard>();
		services.AddSingleton<ISocialIdentityVerifier, SocialIdentityVerifier>();
		// Archivio file e URL pubblici comuni (BuildingBlocks.Infrastructure/Storage); qui solo la sezione dei contratti.
		var documents = configuration.GetSection(ContractDocumentStorage.Section).Get<ObjectStorageOptions>() ?? new ObjectStorageOptions();
		if (string.IsNullOrEmpty(documents.KeyPrefix))
			documents.KeyPrefix = "contracts";
		services.AddSingleton<IDocumentStorage>(new ContractDocumentStorage(new S3ObjectStorage(documents)));
		services.AddSingleton(new PublicFileUrlResolver(configuration.GetSection(PublicFileOptions.Section).Get<PublicFileOptions>() ?? new PublicFileOptions()));
		services.AddSingleton<IFileUrlResolver, FileUrlResolver>();
		services.AddSingleton<ICustomerExportWriter, CustomerExportWriter>();

		// Application: validatori senza stato, servizi per richiesta.
		foreach (var validator in typeof(Module).Assembly.GetTypes()
			.Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IValidator).IsAssignableFrom(t)))
			services.AddSingleton(validator);

		services.AddScoped<CurrentAccount>();
		services.AddScoped<SessionFactory>();
		services.AddScoped<AuthService>();
		services.AddScoped<SocialAuthService>();
		services.AddScoped<RegistrationService>();
		services.AddScoped<AccountRecoveryService>();
		services.AddScoped<MeService>();
		services.AddScoped<BathhouseLikeService>();
		services.AddScoped<StaffService>();
		services.AddScoped<SecuritySettingsService>();
		services.AddScoped<CustomerService>();
		services.AddScoped<SubscriptionService>();
		services.AddScoped<LicensePlanService>();
		services.AddScoped<ContractService>();
		services.AddScoped<PlatformAccountService>();
		services.AddScoped<ExternalTokenService>();

		// Contratti verso gli altri moduli (Booking, Payment, Audit).
		services.AddScoped<ICustomerDirectory, CustomerDirectory>();
		services.AddScoped<IUserDirectory, UserDirectory>();

		return services;
	}
}
