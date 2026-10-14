using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Payment.Domain.Entities;
using Module.Payment.Domain.Enums;

namespace Module.Payment.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="WalletAccount"/> su payment.wallet_accounts.</summary>
public sealed class WalletAccountConfiguration : IEntityTypeConfiguration<WalletAccount>
{
	public void Configure(EntityTypeBuilder<WalletAccount> builder)
	{
		builder.ToTable("wallet_accounts", "payment", t => t.HasComment(
			"Conto prepagato del cliente presso uno stabilimento: tessera fisica ricaricabile o crediti da cessione posto."));

		builder.HasKey(x => x.Id).HasName("pk_payment_wallet_accounts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, esposto al cliente e copiato da C1 sulla riga servizio pagata col borsellino.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento presso cui vale il borsellino.");

		builder.Property(x => x.CustomerId)
			.HasColumnName("customer_id")
			.IsRequired()
			.HasComment("public_id di identity.customer_customers, copiato — nessuna FK cross-schema; cliente titolare del conto.");

		builder.Property(x => x.WalletKind)
			.HasColumnName("wallet_kind")
			.HasColumnType("payment.wallet_kind")
			.HasDefaultValueSql("'prepaid_card'")
			.IsRequired()
			.HasComment("Tipo di borsellino: tessera fisica ricaricabile o crediti da cessione posto.");

		builder.Property(x => x.CardNumber)
			.HasColumnName("card_number")
			.HasMaxLength(24)
			.HasComment("Numero stampato sulla tessera fisica, generato server-side; NULL per wallet_kind = ResaleCredit.");

		builder.Property(x => x.Balance)
			.HasColumnName("balance")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Saldo corrente del conto, materializzato e mantenuto dai movimenti; può andare negativo per storni/rettifiche.");

		builder.Property(x => x.LifetimeCredited)
			.HasColumnName("lifetime_credited")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Totale storico accreditato, mai decrementato; distingue quanto maturato da quanto residuo.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.HasDefaultValue("EUR")
			.IsRequired()
			.HasComment("Valuta ISO 4217 del saldo.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("payment.wallet_status")
			.HasDefaultValueSql("'active'")
			.IsRequired()
			.HasComment("Stato operativo del conto; blocked/closed impediscono nuovi debiti.");

		builder.Property(x => x.OpenedAt)
			.HasColumnName("opened_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Apertura del conto.");

		builder.Property(x => x.LastMovementAt)
			.HasColumnName("last_movement_at")
			.HasComment("Ultimo movimento registrato, guida la retention per inattività (24 mesi).");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete alla chiusura del conto.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_payment_wallet_accounts_public_id");

		builder.HasIndex(x => new { x.PropertyId, x.CustomerId, x.WalletKind })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_payment_wallet_accounts_scope");

		builder.HasIndex(x => new { x.PropertyId, x.CardNumber })
			.IsUnique()
			.HasFilter("card_number IS NOT NULL AND deleted_at IS NULL")
			.HasDatabaseName("uq_payment_wallet_accounts_card_number");

		builder.HasIndex(x => x.CustomerId)
			.HasDatabaseName("ix_payment_wallet_accounts_customer");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("balance <> 0 AND deleted_at IS NULL")
			.HasDatabaseName("ix_payment_wallet_accounts_nonzero");
	}
}
