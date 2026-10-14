using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Switchboard.Domain.Entities.Voice;
using Module.Switchboard.Domain.Enums.Voice;

namespace Module.Switchboard.Infrastructure.Configurations.Voice;

public sealed class VoiceNumberRegistrationConfiguration : IEntityTypeConfiguration<VoiceNumberRegistration>
{
	public void Configure(EntityTypeBuilder<VoiceNumberRegistration> builder)
	{
		builder.ToTable("switchboard_voice_number_registrations", "switchboard", t => t.HasComment(
			"Stato dell'ordine del numero PSTN italiano su Telnyx e fascicolo dei requisiti regolatori (KYC) del centralino, 1:1 con la configurazione."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_voice_number_registrations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.IsRequired()
			.HasComment("Centralino a cui appartiene il fascicolo (1:1).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping e policy d'accesso.");

		builder.Property(x => x.ProvisioningState)
			.HasColumnName("provisioning_state")
			.HasColumnType("switchboard.number_provisioning_state")
			.IsRequired()
			.HasDefaultValueSql("'not_started'")
			.HasComment("Stato interno dell'ordine numero e del suo ciclo di vita (not_started → documents_pending → submitted → active/rejected/released).");

		builder.Property(x => x.TelnyxStatusRaw)
			.HasColumnName("telnyx_status_raw")
			.HasMaxLength(50)
			.HasComment("Stato del numero così come restituito da Telnyx, conservato grezzo per diagnostica.");

		builder.Property(x => x.TelnyxNumberOrderId)
			.HasColumnName("telnyx_number_order_id")
			.HasMaxLength(64)
			.HasComment("Id del number order Telnyx, per il polling dello stato di provisioning.");

		builder.Property(x => x.TelnyxPhoneNumberId)
			.HasColumnName("telnyx_phone_number_id")
			.HasMaxLength(64)
			.HasComment("Id dell'oggetto phone number Telnyx, per PATCH/GET stato/release.");

		builder.Property(x => x.RegRequirementGroupId)
			.HasColumnName("reg_requirement_group_id")
			.HasMaxLength(64)
			.HasComment("Id del requirement group Telnyx (contenitore dei requisiti regolatori compilati).");

		builder.Property(x => x.RegAddressId)
			.HasColumnName("reg_address_id")
			.HasMaxLength(64)
			.HasComment("Id dell'address Telnyx principale usato per i requisiti regolatori.");

		builder.Property(x => x.RegSupplementalAddressId)
			.HasColumnName("reg_supplemental_address_id")
			.HasMaxLength(64)
			.HasComment("Id dell'address Telnyx supplementare usato per i requisiti regolatori.");

		builder.Property(x => x.RegProofOfAddressDocId)
			.HasColumnName("reg_proof_of_address_doc_id")
			.HasMaxLength(64)
			.HasComment("Id del document Telnyx caricato come prova di indirizzo.");

		builder.Property(x => x.RegIdDocumentCopyDocId)
			.HasColumnName("reg_id_document_copy_doc_id")
			.HasMaxLength(64)
			.HasComment("Id del document Telnyx caricato come copia del documento d'identità.");

		builder.Property(x => x.RegCompanyRegCertDocId)
			.HasColumnName("reg_company_reg_cert_doc_id")
			.HasMaxLength(64)
			.HasComment("Id del document Telnyx caricato come certificato camerale/visura.");

		builder.Property(x => x.RegItCustomerType)
			.HasColumnName("reg_it_customer_type")
			.HasColumnType("switchboard.it_customer_type")
			.HasComment("Tipo cliente IT richiesto da Telnyx per l'ordine numero locale.");

		builder.Property(x => x.RegEndUserName)
			.HasColumnName("reg_end_user_name")
			.HasMaxLength(150)
			.HasComment("Nome dell'utilizzatore finale del numero (persona o azienda).");

		builder.Property(x => x.RegIdentityDocType)
			.HasColumnName("reg_identity_doc_type")
			.HasColumnType("switchboard.identity_doc_type")
			.HasComment("Tipo del documento d'identità del referente.");

		builder.Property(x => x.RegIdentityDocNumberEncrypted)
			.HasColumnName("reg_identity_doc_number")
			.HasColumnType("bytea")
			.HasComment("Numero del documento d'identità del referente — PII ad alto rischio, cifrato a riposo (KMS).");

		builder.Property(x => x.RegIdentityDocIssuer)
			.HasColumnName("reg_identity_doc_issuer")
			.HasMaxLength(80)
			.HasComment("Ente emittente del documento d'identità.");

		builder.Property(x => x.RegIdentityDocExpiry)
			.HasColumnName("reg_identity_doc_expiry")
			.HasColumnType("date")
			.HasComment("Data di scadenza del documento d'identità (giorno di calendario puro).");

		builder.Property(x => x.RegDateOfBirthEncrypted)
			.HasColumnName("reg_date_of_birth")
			.HasColumnType("bytea")
			.HasComment("Data di nascita del referente — PII, cifrata a riposo (KMS).");

		builder.Property(x => x.RegPlaceOfBirthEncrypted)
			.HasColumnName("reg_place_of_birth")
			.HasColumnType("bytea")
			.HasComment("Luogo di nascita del referente — PII, cifrato a riposo (KMS).");

		builder.Property(x => x.RegPersonalTaxCodeEncrypted)
			.HasColumnName("reg_personal_tax_code")
			.HasColumnType("bytea")
			.HasComment("Codice fiscale personale del referente — PII ad alto rischio, cifrato a riposo, mai in export/log.");

		builder.Property(x => x.RegCompanyTaxCode)
			.HasColumnName("reg_company_tax_code")
			.HasMaxLength(16)
			.HasComment("Codice fiscale aziendale.");

		builder.Property(x => x.RegVatNumber)
			.HasColumnName("reg_vat_number")
			.HasMaxLength(20)
			.HasComment("Partita IVA.");

		builder.Property(x => x.SubmittedAt)
			.HasColumnName("submitted_at")
			.HasComment("Istante di invio del requirement group a Telnyx.");

		builder.Property(x => x.ActivatedAt)
			.HasColumnName("activated_at")
			.HasComment("Istante di attivazione del numero.");

		builder.Property(x => x.ReleasedAt)
			.HasColumnName("released_at")
			.HasComment("Istante di rilascio del numero; la riga resta come registro storico.");

		builder.ToTable(t => t.HasCheckConstraint(
			"ck_switchboard_voice_number_registrations_release_sequence",
			"released_at IS NULL OR activated_at IS NOT NULL"));

		// bug reale osservato il 2026-09-16: created_at/updated_at non avevano default now() a
		// schema (nessuna convenzione EF globale lo applica, verificato in ModuleDbContextConventions
		// - solo converter UTC e query filter soft-delete) - un ETL che omette queste colonne
		// dall'INSERT (comportamento normale, per lasciare scattare il default) falliva NOT NULL.
		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga.");


		builder.HasIndex(x => x.ConfigId)
			.IsUnique()
			.HasDatabaseName("uq_switchboard_voice_number_registrations_config");

		builder.HasIndex(x => x.ProvisioningState)
			.HasFilter("provisioning_state IN ('documents_pending','submitted')")
			.HasDatabaseName("ix_switchboard_voice_number_registrations_state");

		builder.HasIndex(x => x.RegIdentityDocExpiry)
			.HasFilter("reg_identity_doc_expiry IS NOT NULL")
			.HasDatabaseName("ix_switchboard_voice_number_registrations_doc_expiry");

		builder.HasOne(x => x.Config)
			.WithOne(c => c.NumberRegistration)
			.HasForeignKey<VoiceNumberRegistration>(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_voice_number_registrations_config")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
