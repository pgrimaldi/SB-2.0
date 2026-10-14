using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Contracts.Events;
using Shared.Security;

namespace Boot.Web;

/// <summary>
/// Autenticazione, autorizzazione e protezioni trasversali dell'host:
/// <list type="bullet">
///   <item><c>UserBearer</c>: utenti, default e fallback (tutto protetto se non dichiarato);</item>
///   <item><c>ApiBearer</c>: partner M2M, audience diverso, policy <c>ExternalApi</c>;</item>
///   <item><c>[AllowAnonymous]</c>: login, registrazione, contenuti pubblici, webhook (verificati con la firma).</item>
/// </list>
/// </summary>
public static class SecuritySetup
{
	public static IServiceCollection AddSummerBookingSecurity(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
	{
		var jwt = configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
		JwtConfigurationGuard.Validate(jwt, environment);

		services.AddHttpContextAccessor();
		services.AddScoped<HttpRequestUser>();
		services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<HttpRequestUser>());
		services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<HttpRequestUser>());
		services.AddScoped<IIntegrationEventPublisher, InProcessEventPublisher>();
		services.AddScoped<IAuditTrail, AuditTrail>();

		services.AddAuthentication(AuthSchemes.UserBearer)
			.AddJwtBearer(AuthSchemes.UserBearer, o =>
			{
				o.MapInboundClaims = false;
				o.TokenValidationParameters = Parameters(jwt, jwt.UserAudience, jwt.UserKeys, "Jwt:UserKeys");
				o.Events = ProblemEvents();
			})
			.AddJwtBearer(AuthSchemes.ApiBearer, o =>
			{
				o.MapInboundClaims = false;
				o.TokenValidationParameters = Parameters(jwt, jwt.ApiAudience, jwt.ApiKeys, "Jwt:ApiKeys");
				o.Events = ProblemEvents();
				o.Events.OnMessageReceived = context =>
				{
					// Contratti legacy dei partner: header x-access-token (OfSale, Transactions) e
					// access_token in querystring (TeamSystem), come nel vecchio backend.
					if (string.IsNullOrEmpty(context.Request.Headers.Authorization))
					{
						context.Token = context.Request.Headers["x-access-token"].FirstOrDefault()
							?? (context.Request.Path.StartsWithSegments("/api/sb/teamsystem") ? context.Request.Query["access_token"].FirstOrDefault() : null);
					}
					return Task.CompletedTask;
				};
			});

		services.AddAuthorization(options =>
		{
			var userPolicy = new AuthorizationPolicyBuilder(AuthSchemes.UserBearer).RequireAuthenticatedUser().Build();
			options.DefaultPolicy = userPolicy;
			options.FallbackPolicy = userPolicy;

			options.AddPolicy(AuthPolicies.ExternalApi, p => p
				.AddAuthenticationSchemes(AuthSchemes.ApiBearer)
				.RequireAuthenticatedUser()
				.RequireClaim(AuthClaims.ClientId));

			foreach (var permission in Permissions.All)
			{
				options.AddPolicy(Permissions.PolicyName(permission), p => p
					.AddAuthenticationSchemes(AuthSchemes.UserBearer)
					.RequireAuthenticatedUser()
					.RequireAssertion(ctx => RolePermissions.Has(ctx.User.FindFirst(AuthClaims.Role)?.Value, permission)));
			}
		});

		services.AddRateLimiter(options =>
		{
			var permitPerMinute = configuration.GetValue("Auth:RateLimit:PermitPerMinute", 20);
			options.AddPolicy(RateLimitPolicies.Auth, context => RateLimitPartition.GetFixedWindowLimiter(
				context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
				_ => new FixedWindowRateLimiterOptions { PermitLimit = permitPerMinute, Window = TimeSpan.FromMinutes(1) }));
			options.OnRejected = (context, _) => new ValueTask(
				ProblemDetailsWriter.WriteAsync(context.HttpContext, StatusCodes.Status429TooManyRequests, CommonErrorCodes.TooManyRequests));
		});

		var origins = configuration.GetSection("Auth:AllowedOrigins").Get<string[]>() ?? [];
		services.AddCors(options => options.AddDefaultPolicy(p =>
		{
			if (origins.Length > 0)
				p.WithOrigins(origins).AllowCredentials().AllowAnyHeader().AllowAnyMethod();
		}));

		services.Configure<ForwardedHeadersOptions>(options =>
		{
			options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
			// Dietro il load balancer AWS l'IP del client arriva in X-Forwarded-For; ci si fida solo se configurato.
			if (configuration.GetValue("Hosting:TrustForwardedHeaders", false))
			{
				options.KnownIPNetworks.Clear();
				options.KnownProxies.Clear();
			}
		});

		return services;
	}

	private static TokenValidationParameters Parameters(JwtOptions jwt, string audience, IEnumerable<JwtKey> keys, string section) => new()
	{
		ValidIssuer = jwt.Issuer,
		ValidAudience = audience,
		IssuerSigningKeys = keys.Where(k => !string.IsNullOrWhiteSpace(k.Key))
			.Select(k => new SymmetricSecurityKey(Convert.FromBase64String(k.Key)) { KeyId = k.Kid })
			.ToArray() is { Length: > 0 } built ? built : throw new InvalidOperationException($"{section}: nessuna chiave configurata."),
		ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
		ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),
		NameClaimType = AuthClaims.Subject,
		RoleClaimType = AuthClaims.Role,
	};

	/// <summary>401 e 403 nello stesso formato degli altri errori, invece del corpo vuoto di default.</summary>
	private static JwtBearerEvents ProblemEvents() => new()
	{
		OnChallenge = async context =>
		{
			context.HandleResponse();
			await ProblemDetailsWriter.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, CommonErrorCodes.Unauthenticated);
		},
		OnForbidden = context => ProblemDetailsWriter.WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden, CommonErrorCodes.Forbidden),
	};
}
