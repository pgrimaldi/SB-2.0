using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Identity.Domain.Entities;

namespace Module.Identity.Infrastructure.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
	public void Configure(EntityTypeBuilder<Account> builder)
	{
		builder.ToTable("access_users", "identity", t => t.HasComment(
			"Account di login del sistema (titolare, staff, superadmin, cliente finale consumer)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_access_users");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Identificativo pubblico stabile (UUIDv7), usato in URL/API e come riferimento cross-modulo.");

		builder.Property(x => x.Username)
			.HasColumnName("username")
			.HasMaxLength(100)
			.HasComment("Username alternativo per il login da cassa (solo staff); NULL per i consumer.");

		builder.Property(x => x.Email)
			.HasColumnName("email")
			.HasColumnType("citext")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Email di contatto e identificatore primario di login.");

		builder.Property(x => x.PasswordHash)
			.HasColumnName("password_hash")
			.HasMaxLength(255)
			.HasComment("Hash della password; NULL per gli account creati solo via social login.");

		builder.Property(x => x.PasswordChangedAt)
			.HasColumnName("password_changed_at")
			.HasComment("Ultima modifica della password; usata per invalidare le sessioni al cambio.");

		builder.Property(x => x.FirstName)
			.HasColumnName("first_name")
			.HasMaxLength(100)
			.HasComment("Nome dell'account.");

		builder.Property(x => x.LastName)
			.HasColumnName("last_name")
			.HasMaxLength(100)
			.HasComment("Cognome dell'account.");

		builder.Property(x => x.PhoneCountryCode)
			.HasColumnName("phone_country_code")
			.HasMaxLength(8)
			.HasDefaultValue("+39")
			.HasComment("Prefisso telefonico internazionale (E.164) del numero di cellulare.");

		builder.Property(x => x.Phone)
			.HasColumnName("phone")
			.HasMaxLength(32)
			.HasComment("Numero di cellulare, senza prefisso.");

		builder.Property(x => x.ProfilePictureOrigin)
			.HasColumnName("profile_picture_origin")
			.HasColumnType("identity.storage_origin")
			.HasComment("Provenienza della foto profilo (storage interno o provider social esterno).");

		builder.Property(x => x.ProfilePicturePath)
			.HasColumnName("profile_picture_path")
			.HasMaxLength(500)
			.HasComment("Percorso relativo della foto profilo, o URL intero se origin = external.");

		builder.Property(x => x.LoginMethod)
			.HasColumnName("login_method")
			.HasColumnType("identity.login_method")
			.IsRequired()
			.HasDefaultValueSql("'normal'")
			.HasComment("Metodo di autenticazione usato l'ultima volta.");

		builder.Property(x => x.GoogleId)
			.HasColumnName("google_id")
			.HasMaxLength(64)
			.HasComment("Sub dell'account Google collegato (login social).");

		builder.Property(x => x.FacebookId)
			.HasColumnName("facebook_id")
			.HasMaxLength(64)
			.HasComment("Id dell'account Facebook collegato.");

		builder.Property(x => x.AppleSubject)
			.HasColumnName("apple_subject")
			.HasMaxLength(255)
			.HasComment("Sub Apple dell'account collegato (solo il claim, mai l'intero JWT). L'email puo' mancare su questo percorso: Apple la comunica una volta sola e l'utente puo' nasconderla - vedi le note su Account.AppleSubject.");

		builder.Property(x => x.Language)
			.HasColumnName("language")
			.HasColumnType("identity.ui_language")
			.IsRequired()
			.HasDefaultValueSql("'it'")
			.HasComment("Lingua preferita per email transazionali e UI.");

		builder.Property(x => x.RoleId)
			.HasColumnName("role_id")
			.IsRequired()
			.HasComment("Ruolo applicativo dell'account (determina tipo account e permessi).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id della property (catalog) di appartenenza dello staff, copiato — nessuna FK cross-schema. NULL per i consumer.");

		builder.Property(x => x.ResourceScopes)
			.HasColumnName("resource_scopes")
			.HasColumnType("text[]")
			.IsRequired()
			.HasDefaultValueSql("'{}'::text[]")
			.HasComment("Codici delle postazioni su cui l'account puo' operare quando la restrizione e' attiva. "
				+ "Era la tabella figlia access_user_resource_scopes, collassata in array.");

		builder.Property(x => x.IsResourceScopeRestricted)
			.HasColumnName("is_resource_scope_restricted")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se true, l'account opera solo sulle risorse elencate in access_user_resource_scopes.");

		builder.Property(x => x.NewsSectionEnabled)
			.HasColumnName("news_section_enabled")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Mostra/nasconde la sezione novità nel pannello personale.");

		builder.Property(x => x.EnabledNotificationSounds)
			.HasColumnName("enabled_notification_sounds")
			.HasColumnType("identity.notification_sound_scope[]")
			.HasDefaultValueSql("'{management_booking,online_booking,management_service_booking,online_service_booking}'")
			.IsRequired()
			.HasComment("Ambiti per cui l'utente vuole l'allerta sonora. Sostituisce quattro colonne " +
				"booleane parallele sullo stesso argomento; il default riproduce il comportamento " +
				"precedente (tutte e quattro attive). Array vuoto = nessun suono.");

		builder.Property(x => x.LastLoginAt)
			.HasColumnName("last_login_at")
			.HasComment("Data/ora dell'ultimo login realmente autenticato.");

		builder.Property(x => x.TermsAcceptedAt)
			.HasColumnName("terms_accepted_at")
			.HasComment("Accettazione di privacy e termini alla registrazione pubblica; NULL per lo storico e per lo staff.");

		builder.Property(x => x.TermsVersion)
			.HasColumnName("terms_version")
			.HasMaxLength(50)
			.HasComment("Versione del testo di privacy e termini accettato.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("Account che ha creato questo account (staff creato da admin/superadmin); NULL per i consumer auto-registrati.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune.

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_identity_access_users_public_id");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_access_users_property");

		builder.HasIndex(x => x.RoleId)
			.HasDatabaseName("ix_identity_access_users_role");

		builder.HasIndex(x => x.GoogleId)
			.HasFilter("google_id IS NOT NULL")
			.HasDatabaseName("ix_identity_access_users_google_id");

		builder.HasIndex(x => x.FacebookId)
			.HasFilter("facebook_id IS NOT NULL")
			.HasDatabaseName("ix_identity_access_users_facebook_id");

		builder.HasIndex(x => x.AppleSubject)
			.HasFilter("apple_subject IS NOT NULL")
			.HasDatabaseName("ix_identity_access_users_apple_sub");

		builder.HasIndex(x => x.LastLoginAt)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_access_users_last_login");

		builder.HasOne(x => x.Role)
			.WithMany()
			.HasForeignKey(x => x.RoleId)
			.HasConstraintName("fk_identity_access_users_role")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.CreatedByUser)
			.WithMany()
			.HasForeignKey(x => x.CreatedByUserId)
			.HasConstraintName("fk_identity_access_users_created_by")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
