using Boot.Web;
using Microsoft.OpenApi;
using Module.Audit;
using Module.Audit.Endpoints;
using Module.Booking;
using Module.Catalog;
using Module.Chatbot;
using Module.FiscalPrint;
using Module.Fiskaly;
using Module.Identity;
using Module.Identity.Endpoints;
using Module.Infra;
using Module.Notification;
using Module.Payment;
using Module.Privacy;
using Module.Switchboard;

namespace Boot
{
	public class Program
	{
		// Documenti Swagger esposti, uno per modulo. Aggiungere un modulo = una riga.
		private static readonly (string Name, string Title)[] ApiDocs =
		{
			("identity", "Identity API"),
			("catalog", "Catalog API"),
			("booking", "Booking API"),
			("payment", "Payment API"),
			("fiskaly", "Fiskaly API"),
			("fiscalprint", "FiscalPrint API"),
			("notification", "Notification API"),
			("switchboard", "Switchboard API"),
			("chatbot", "Chatbot API"),
			("privacy", "Privacy API"),
			("infra", "Infra API"),
			("audit", "Audit API"),
		};

		public static int Main(string[] args)
		{
			// Modalita' a riga di comando: applica le policy di retention e esce, senza avviare l'host web.
			// Non e' uno scheduler (la pianificazione arrivera' con l'host dei job): e'
			// il modo di eseguire le policy e leggerne il report, che e' il passo che la politica richiede
			// prima dell'esecuzione reale.
			if (args.Length > 0 && args[0] == RetentionCommand.Verb)
				return RetentionCommand.RunAsync(args).GetAwaiter().GetResult();

			// Accendere una policy (is_dry_run_only -> false) e' un verbo a parte, non un'opzione di
			// `retention`: non esegue niente, modifica una riga di configurazione. Tenerli separati
			// evita che un `--apply` di troppo accenda qualcosa oltre a eseguirlo.
			if (args.Length > 0 && args[0] == RetentionEnableCommand.Verb)
				return RetentionEnableCommand.RunAsync(args).GetAwaiter().GetResult();

			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.

			builder.Services.AddControllers();

			builder.Services.AddHealthChecks();

			// Errori in formato Problem Details con codice
			// e sicurezza trasversale: schemi JWT, policy, rate limit, CORS (Boot/Web/SecuritySetup.cs).
			builder.Services.AddProblemDetails();
			builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
			builder.Services.AddSummerBookingSecurity(builder.Configuration, builder.Environment);

			// Swagger / OpenAPI: un documento per modulo
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(x =>
			{
				foreach (var doc in ApiDocs)
					x.SwaggerDoc(doc.Name, new OpenApiInfo { Title = doc.Title, Version = "v1" });
				x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Type = SecuritySchemeType.Http,
					Scheme = "bearer",
					BearerFormat = "JWT",
				});
				// Senza il requisito Swagger UI mostra "Authorize" ma non invia il token. Sugli endpoint
				// anonimi l'header viene ignorato.
				x.AddSecurityRequirement(document => new OpenApiSecurityRequirement
				{
					[new OpenApiSecuritySchemeReference("Bearer", document)] = [],
				});
			});

			// Moduli — uno per schema PostgreSQL, registrati tutti qui: Boot e' il composition
			// root del monolite modulare, non un host di un modulo solo. Ogni Add*Module registra
			// (per ora) la sola persistenza: il DbContext del modulo e la mappatura dei suoi enum
			// nativi sulla connessione. Nessuna migration viene applicata all'avvio: lo schema si
			// costruisce con "dotnet ef database update".
			//
			// Nota sul dimensionamento: ogni modulo costruisce il proprio NpgsqlDataSource, quindi
			// il proprio pool. I pool sono pigri (Build() non apre connessioni, si riempiono al
			// primo uso), ma il default di Npgsql e' MaxPoolSize=100 per data source: con dodici
			// moduli sotto carico il tetto teorico supera di molto un max_connections tipico. Per
			// questo ogni modulo ha un tetto esplicito (ModuleConnectionStrings.ApplyModulePoolLimit,
			// configurabile con Database:MaxPoolSizePerModule); in produzione va valutato un pooler.
			builder.Services.AddBookingModule(builder.Configuration);
			builder.Services.AddCatalogModule(builder.Configuration);
			builder.Services.AddChatbotModule(builder.Configuration);
			builder.Services.AddFiscalPrintModule(builder.Configuration);
			builder.Services.AddFiskalyModule(builder.Configuration);
			builder.Services.AddIdentityModule(builder.Configuration);
			builder.Services.AddInfraModule(builder.Configuration);
			builder.Services.AddNotificationModule(builder.Configuration);
			builder.Services.AddPaymentModule(builder.Configuration);
			builder.Services.AddPrivacyModule(builder.Configuration);
			builder.Services.AddSwitchboardModule(builder.Configuration);
			builder.Services.AddAuditModule(builder.Configuration);

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				// Genera i documenti /swagger/{documentName}/swagger.json
				app.UseSwagger();
				app.UseSwaggerUI(options =>
				{
					foreach (var doc in ApiDocs)
						options.SwaggerEndpoint($"/swagger/{doc.Name}/swagger.json", doc.Title);
				});

				// Home page (solo Development) con i link agli Swagger dei moduli
				app.MapGet("/", () => Results.Content(BuildDevHomePage(), "text/html"))
					.ExcludeFromDescription()
					.AllowAnonymous();
			}

			app.UseForwardedHeaders();
			app.UseExceptionHandler();
			app.UseHttpsRedirection();

			app.UseRouting();
			app.UseCors();
			app.UseAuthentication();
			app.UseAuthorization();
			app.UseRateLimiter();


			app.MapControllers();

			app.MapHealthChecks("/health").AllowAnonymous();

			// Endpoint dei moduli
			app.MapIdentityEndpoints();
			app.MapAuditEndpoints();

			app.Run();
			return 0;
		}

		private static string BuildDevHomePage()
		{
			var links = string.Concat(ApiDocs.Select(doc =>
				$"<li><a href=\"/swagger/index.html?urls.primaryName={Uri.EscapeDataString(doc.Title)}\">{doc.Title}</a></li>"));

			const string html = """
			<!doctype html>
			<html lang="it">
			<head>
			  <meta charset="utf-8">
			  <meta name="viewport" content="width=device-width, initial-scale=1">
			  <title>SummerBooking · Dev</title>
			  <style>
			    body { font-family: system-ui, -apple-system, sans-serif; max-width: 640px; margin: 3rem auto; padding: 0 1rem; color: #1f2937; }
			    h1 { margin-bottom: .25rem; }
			    h2 { margin-top: 2rem; font-size: 1rem; text-transform: uppercase; letter-spacing: .05em; color: #6b7280; }
			    .env { color: #b45309; font-weight: 600; }
			    ul { line-height: 2; padding-left: 1.2rem; }
			    a { color: #2563eb; text-decoration: none; }
			    a:hover { text-decoration: underline; }
			  </style>
			</head>
			<body>
			  <h1>SummerBooking</h1>
			  <p class="env">Ambiente: Development</p>
			  <h2>Swagger per modulo</h2>
			  <ul>__LINKS__</ul>
			  <h2>Utility</h2>
			  <ul>
			    <li><a href="/swagger">Swagger UI (tutti i documenti)</a></li>
			    <li><a href="/health">Health check</a></li>
			  </ul>
			</body>
			</html>
			""";

			return html.Replace("__LINKS__", links);
		}
	}
}
