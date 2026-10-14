using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Infra.Domain.Entities;

namespace Module.Infra.Infrastructure.Configurations;

public sealed class ApiClientConfiguration : IEntityTypeConfiguration<ApiClient>
{
	public void Configure(EntityTypeBuilder<ApiClient> builder)
	{
		builder.ToTable("integration_api_clients", "infra", t => t.HasComment(
			"Registro dei client applicativi partner (POS/cassa esterni, export dati) che autenticano chiamate server-to-server verso l'API — nessuna credenziale in chiaro, solo un riferimento al secret manager."));

		builder.HasKey(x => x.Id).HasName("pk_infra_integration_api_clients");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile (UUIDv7) per il pannello systemadmin; non usato dal flusso di autenticazione, che chiave su client_id.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_infra_integration_api_clients_public_id");

		builder.Property(x => x.AppName)
			.HasColumnName("app_name")
			.HasColumnType("infra.integration_app")
			.IsRequired()
			.HasComment("Applicazione/integrazione partner a cui il client appartiene; instrada le autorizzazioni (es. lo scambio OAuth di TeamSystem).");

		builder.Property(x => x.ClientId)
			.HasColumnName("client_id")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Identificativo pubblico del client (client_id OAuth / apiKey firmata in signinApi); chiave di ricerca dell'autenticazione.");

		builder.Property(x => x.ClientSecretRef)
			.HasColumnName("client_secret_ref")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Riferimento (ARN/nome) al segreto condiviso nel secret manager, path convenzionale infra/integration-api-clients/{client_id} — il segreto vero non è mai a DB.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties a cui il client è vincolato; NULL = client non scoped a una singola property — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Interruttore attivo/revocato: un client con is_active = false non autentica più, senza essere cancellato (storico/audit preservato).");

		builder.Property(x => x.ExpiresAt)
			.HasColumnName("expires_at")
			.HasComment("Scadenza effettiva del client, quando impostata; a differenza del legacy va verificata dal servizio di autenticazione.");

		builder.Property(x => x.RotatedAt)
			.HasColumnName("rotated_at")
			.HasComment("Data dell'ultima rotazione del segreto (aggiornamento di client_secret_ref); unico modo di saperlo senza consultare il secret manager.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione del client.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di ultima modifica.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete: revoca definitiva del client, distinta da is_active (disattivazione reversibile).");

		builder.HasIndex(x => x.ClientId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_infra_integration_api_clients_client_id");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("property_id IS NOT NULL")
			.HasDatabaseName("ix_infra_integration_api_clients_property");

		builder.HasIndex(x => x.AppName)
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_infra_integration_api_clients_app_active");
	}
}
