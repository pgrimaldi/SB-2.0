using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Fiskaly.Domain.Entities;
using Module.Fiskaly.Domain.Enums;

namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class FiskalyAccountConfiguration : IEntityTypeConfiguration<FiskalyAccount>
{
	public void Configure(EntityTypeBuilder<FiskalyAccount> builder)
	{
		builder.ToTable("accounts", "fiskaly", t => t.HasComment(
			"Account fiscale di uno stabilimento presso Fiskaly SIGN IT: identità, credenziali (per riferimento) e stato di configurazione fiscale."));

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_accounts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile (UUIDv7); esposto all'export GDPR e al pannello Fiscalization.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_fiskaly_accounts_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Stabilimento proprietario dell'account.");
		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_fiskaly_accounts_property");

		builder.Property(x => x.FiscalSetupLevel)
			.HasColumnName("fiscal_setup_level")
			.HasColumnType("fiskaly.fiscal_setup_level")
			.IsRequired()
			.HasDefaultValueSql("'not_configured'")
			.HasComment("Stato di configurazione fiscale: NotConfigured / VatOnly / FiskalyActive. FiskalyActive è derivato e ricalcolato dal dominio, non liberamente scrivibile.");
		builder.HasIndex(x => x.FiscalSetupLevel)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_fiskaly_accounts_setup_level");

		builder.Property(x => x.TermsAcceptedAt)
			.HasColumnName("terms_accepted_at")
			.HasComment("Istante in cui il gestore ha accettato termini e condizioni Fiskaly; NULL = non accettati. Precondizione bloccante per il setup (sostituisce il flag terms_accepted).");

		builder.Property(x => x.IsEnabled)
			.HasColumnName("is_enabled")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Interruttore master della fiscalità elettronica, gestito da SuperAdmin.");

		builder.Property(x => x.AutoreceiptEnabled)
			.HasColumnName("autoreceipt_enabled")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Emissione automatica dello scontrino dopo un pagamento Stripe andato a buon fine.");

		// NON e' una catena monotona: l'abilitazione (is_enabled, autoreceipt_enabled) la imposta il
		// SystemAdmin, l'accettazione dei termini (terms_accepted_at) la fa il gestore DOPO. Quindi
		// "abilitato senza termini accettati" e' uno stato legittimo e transitorio, non un errore:
		// l'unica dipendenza strutturale e' che l'auto-scontrino richieda Fiskaly abilitato.
		// Il gate sui termini e' un controllo a RUNTIME (updateAccount / getCashRegisters /
		// emissione), non un'invariante di tabella.
		builder.ToTable(t => t.HasCheckConstraint(
			"ck_fiskaly_accounts_activation_chain",
			"autoreceipt_enabled = false OR is_enabled = true"));

		builder.Property(x => x.OrganizationId)
			.HasColumnName("organization_id")
			.HasMaxLength(36)
			.HasComment("Id opaco dell'Organization su Fiskaly SIGN IT.");
		builder.HasIndex(x => x.OrganizationId)
			.IsUnique()
			.HasFilter("organization_id IS NOT NULL")
			.HasDatabaseName("uq_fiskaly_accounts_organization");

		builder.Property(x => x.SubjectId)
			.HasColumnName("subject_id")
			.HasMaxLength(36)
			.HasComment("Id opaco del Subject API_KEY su Fiskaly SIGN IT.");
		builder.HasIndex(x => x.SubjectId)
			.IsUnique()
			.HasFilter("subject_id IS NOT NULL")
			.HasDatabaseName("uq_fiskaly_accounts_subject");

		builder.Property(x => x.TaxpayerId)
			.HasColumnName("taxpayer_id")
			.HasMaxLength(36)
			.HasComment("Id opaco del Taxpayer COMPANY su Fiskaly SIGN IT.");
		builder.HasIndex(x => x.TaxpayerId)
			.IsUnique()
			.HasFilter("taxpayer_id IS NOT NULL")
			.HasDatabaseName("uq_fiskaly_accounts_taxpayer");

		builder.Property(x => x.LocationId)
			.HasColumnName("location_id")
			.HasMaxLength(36)
			.HasComment("Id opaco della Location BRANCH su Fiskaly SIGN IT.");
		builder.HasIndex(x => x.LocationId)
			.IsUnique()
			.HasFilter("location_id IS NOT NULL")
			.HasDatabaseName("uq_fiskaly_accounts_location");

		builder.Property(x => x.OrganizationName)
			.HasColumnName("organization_name")
			.HasMaxLength(128)
			.HasComment("Nome assegnato all'Organization su Fiskaly.");

		builder.Property(x => x.SubjectApiKeyName)
			.HasColumnName("subject_api_key_name")
			.HasMaxLength(40)
			.HasComment("Nome del Subject API_KEY su Fiskaly.");

		builder.Property(x => x.OrganizationState)
			.HasColumnName("organization_state")
			.HasColumnType("fiskaly.org_state")
			.HasComment("Stato dell'Organization su Fiskaly (enabled/decommissioned); può divergere dal remoto dopo closeAccount.");

		builder.Property(x => x.SubjectState)
			.HasColumnName("subject_state")
			.HasColumnType("fiskaly.org_state")
			.HasComment("Stato del Subject su Fiskaly (enabled/decommissioned); può divergere dal remoto dopo closeAccount.");

		builder.Property(x => x.TaxpayerState)
			.HasColumnName("taxpayer_state")
			.HasColumnType("fiskaly.entity_state")
			.HasComment("Ciclo di vita del Taxpayer su Fiskaly (acquired/commissioned/decommissioned).");

		builder.Property(x => x.LocationState)
			.HasColumnName("location_state")
			.HasColumnType("fiskaly.entity_state")
			.HasComment("Ciclo di vita della Location su Fiskaly (acquired/commissioned/decommissioned).");

		builder.Property(x => x.TaxpayerMode)
			.HasColumnName("taxpayer_mode")
			.HasColumnType("fiskaly.entity_mode")
			.HasComment("Modalità operativa del Taxpayer su Fiskaly (inactive/operative/suspended/degraded).");

		builder.Property(x => x.LocationMode)
			.HasColumnName("location_mode")
			.HasColumnType("fiskaly.entity_mode")
			.HasComment("Modalità operativa della Location su Fiskaly (inactive/operative/suspended/degraded).");

		builder.Property(x => x.SubjectCredentialsSecretRef)
			.HasColumnName("subject_credentials_secret_ref")
			.HasMaxLength(255)
			.HasComment("Riferimento (ARN/nome) al segreto {key,secret} del Subject in secret manager — nessun materiale segreto in colonna.");

		builder.Property(x => x.TaxpayerLegalName)
			.HasColumnName("taxpayer_legal_name")
			.HasMaxLength(128)
			.HasComment("Ragione sociale legale del titolare, inviata a Fiskaly come name.legal.");

		builder.Property(x => x.TaxpayerTradeName)
			.HasColumnName("taxpayer_trade_name")
			.HasMaxLength(128)
			.HasComment("Nome commerciale / insegna, preferito nel PDF scontrino e nei messaggi.");

		builder.Property(x => x.TaxpayerVatNumber)
			.HasColumnName("taxpayer_vat_number")
			.HasMaxLength(13)
			.HasComment("Partita IVA del titolare (dato fiscale).");

		builder.Property(x => x.TaxpayerFiscalCode)
			.HasColumnName("taxpayer_fiscal_code")
			.HasMaxLength(16)
			.HasComment("Codice fiscale / partita IVA del soggetto passivo (dato fiscale).");

		builder.Property(x => x.TaxpayerAddressStreet)
			.HasColumnName("taxpayer_address_street")
			.HasMaxLength(128)
			.HasComment("Via dell'indirizzo del titolare inviato a Fiskaly e stampato sul PDF.");

		builder.Property(x => x.TaxpayerAddressStreetNumber)
			.HasColumnName("taxpayer_address_street_number")
			.HasMaxLength(16)
			.HasComment("Numero civico dell'indirizzo del titolare (default applicativo 'SNC' quando inviato).");

		builder.Property(x => x.TaxpayerAddressPostalCode)
			.HasColumnName("taxpayer_address_postal_code")
			.HasMaxLength(5)
			.HasComment("CAP dell'indirizzo del titolare.");

		builder.Property(x => x.TaxpayerAddressCity)
			.HasColumnName("taxpayer_address_city")
			.HasMaxLength(128)
			.HasComment("Città dell'indirizzo del titolare.");

		builder.Property(x => x.TaxpayerAddressCountryCode)
			.HasColumnName("taxpayer_address_country_code")
			.HasColumnType("char(2)")
			.IsRequired()
			.HasDefaultValue("IT")
			.HasComment("Nazione ISO 3166-1 alpha-2 dell'indirizzo del titolare. Vocabolario locale al modulo (non un tipo SharedKernel cross-schema in questa fase).");

		builder.Property(x => x.FisconlineHolderFiscalCode)
			.HasColumnName("fisconline_holder_fiscal_code")
			.HasMaxLength(16)
			.HasComment("Codice fiscale della persona fisica abilitata a Fisconline — dato personale, rilevante per l'export GDPR.");

		builder.Property(x => x.FisconlineCredentialsSecretRef)
			.HasColumnName("fisconline_credentials_secret_ref")
			.HasMaxLength(255)
			.HasComment("Riferimento (ARN/nome) al segreto {pin,password} Fisconline in secret manager — nessun materiale segreto in colonna.");

		builder.Property(x => x.FisconlineUpdatedAt)
			.HasColumnName("fisconline_updated_at")
			.HasComment("Data dell'ultima modifica di PIN o password Fisconline.");

		builder.Property(x => x.SoftwareCashRegisterIsDefault)
			.HasColumnName("software_cash_register_is_default")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Se true, la cassa software auto-creata è quella di default e le casse utente sono forzate a is_default=false.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha creato la riga.");

		builder.Property(x => x.UpdatedByUserId)
			.HasColumnName("updated_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Ultimo operatore che ha modificato la configurazione.");

		builder.HasMany(x => x.CashRegisters)
			.WithOne()
			.HasForeignKey(cr => cr.AccountId)
			.HasConstraintName("fk_fiskaly_cash_registers_account")
			.OnDelete(DeleteBehavior.Cascade);

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

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete = chiusura account (closeAccount); un successivo updateAccount lo azzera e ricrea da zero tutte le entità Fiskaly.");
	}
}
