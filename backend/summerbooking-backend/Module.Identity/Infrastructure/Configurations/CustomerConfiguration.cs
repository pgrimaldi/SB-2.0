using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Identity.Domain.Entities;

namespace Module.Identity.Infrastructure.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
	public void Configure(EntityTypeBuilder<Customer> builder)
	{
		builder.ToTable("customer_customers", "identity", t => t.HasComment(
			"Rubrica dei clienti finali per singolo stabilimento (contatti da prenotazioni/transazioni di cassa)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_customer_customers");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, riferimento cross-modulo (Booking/Payment/Fiskaly).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id della property (catalog) a cui il contatto appartiene, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.HasComment("Account di login (ruolo consumer) collegato a questo contatto, se esiste.");

		builder.Property(x => x.FullName)
			.HasColumnName("full_name")
			.HasMaxLength(200)
			.HasComment("Nome del cliente/contatto.");

		builder.Property(x => x.PhoneCountryCode)
			.HasColumnName("phone_country_code")
			.HasMaxLength(8)
			.HasDefaultValue("+39")
			.HasComment("Prefisso telefonico internazionale del telefono primario.");

		builder.Property(x => x.Phone)
			.HasColumnName("phone")
			.HasMaxLength(32)
			.HasComment("Telefono primario, chiave di matching/deduplica per stabilimento.");

		builder.Property(x => x.PhoneSecondary)
			.HasColumnName("phone_secondary")
			.HasMaxLength(32)
			.HasComment("Recapito telefonico alternativo.");

		builder.Property(x => x.Email)
			.HasColumnName("email")
			.HasColumnType("citext")
			.HasMaxLength(255)
			.HasComment("Email di contatto, seconda chiave di matching.");

		builder.Property(x => x.FiscalCode)
			.HasColumnName("fiscal_code")
			.HasMaxLength(16)
			.HasComment("Codice fiscale, per clienti che richiedono fattura/scontrino intestato.");

		builder.Property(x => x.VatNumber)
			.HasColumnName("vat_number")
			.HasMaxLength(20)
			.HasComment("Partita IVA per clienti business.");

		builder.Property(x => x.Address)
			.HasColumnName("address")
			.HasMaxLength(255)
			.HasComment("Indirizzo di residenza/fatturazione.");

		builder.Property(x => x.City)
			.HasColumnName("city")
			.HasMaxLength(120)
			.HasComment("Città di residenza/fatturazione.");

		builder.Property(x => x.PostalCode)
			.HasColumnName("postal_code")
			.HasMaxLength(16)
			.HasComment("CAP di residenza/fatturazione.");

		builder.Property(x => x.DateOfBirth)
			.HasColumnName("date_of_birth")
			.HasComment("Data di nascita.");

		builder.Property(x => x.OtherNames)
			.HasColumnName("other_names")
			.HasMaxLength(255)
			.HasComment("Nominativi aggiuntivi/alias del contatto.");

		builder.Property(x => x.DiscountNote)
			.HasColumnName("discount_note")
			.HasMaxLength(255)
			.HasComment("Nota testuale libera su sconti/condizioni particolari riservate al cliente.");

		builder.Property(x => x.Note)
			.HasColumnName("note")
			.HasMaxLength(255)
			.HasComment("Nota libera sul cliente.");

		builder.Property(x => x.IsBlacklisted)
			.HasColumnName("is_blacklisted")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Flag di blacklist per questo stabilimento.");

		builder.Property(x => x.BlacklistReason)
			.HasColumnName("blacklist_reason")
			.HasMaxLength(255)
			.HasComment("Motivo della blacklist.");

		builder.Property(x => x.SourceChannel)
			.HasColumnName("source_channel")
			.HasColumnType("identity.customer_source")
			.IsRequired()
			.HasComment("Come è nata la riga (form contatti, prenotazione, cassa, import).");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("Operatore che ha creato/modificato il contatto dal form Contatti; NULL per le creazioni automatiche.");

		builder.Property(x => x.LastActivityAt)
			.HasColumnName("last_activity_at")
			.HasComment("Data dell'ultima prenotazione/transazione che coinvolge questo cliente.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune.

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_identity_customer_customers_public_id");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_customer_customers_property_alive");

		builder.HasIndex(x => new { x.PropertyId, x.Phone })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_customer_customers_property_phone");

		builder.HasIndex(x => new { x.PropertyId, x.FullName })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_customer_customers_property_name");

		// Matching per email dentro lo stabilimento. La scheda lo progettava su lower(email), ma la colonna
		// è citext: un indice sulla colonna è già case-insensitive e serve le query `email = @x` senza
		// espressioni. Fino al 2026-10-06 l'indice non esisteva.
		builder.HasIndex(x => new { x.PropertyId, x.Email })
			.HasFilter("deleted_at IS NULL AND email IS NOT NULL")
			.HasDatabaseName("ix_identity_customer_customers_property_email");

		builder.HasIndex(x => x.UserId)
			.HasFilter("user_id IS NOT NULL")
			.HasDatabaseName("ix_identity_customer_customers_user");

		builder.HasIndex(x => new { x.PropertyId, x.LastActivityAt })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_customer_customers_last_activity");

		builder.HasOne(x => x.User)
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.HasConstraintName("fk_identity_customer_customers_user")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(x => x.CreatedByUser)
			.WithMany()
			.HasForeignKey(x => x.CreatedByUserId)
			.HasConstraintName("fk_identity_customer_customers_created_by")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
