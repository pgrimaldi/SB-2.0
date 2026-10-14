using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Payment.Domain.Entities;

namespace Module.Payment.Infrastructure.Configurations;

/// <summary>
/// Mapping EF Core di <see cref="WalletTransactionItem"/> su payment.wallet_transaction_items. Applicata
/// dentro <see cref="WalletTransactionConfiguration"/> via OwnsMany, non registrata da
/// ApplyConfigurationsFromAssembly: <see cref="OwnedNavigationBuilder{TOwner,TDependent}"/> non e un
/// <see cref="EntityTypeBuilder{TEntity}"/> (nessun ApplyConfiguration(IEntityTypeConfiguration) su di
/// esso), quindi qui si espone un metodo statico invece di implementare IEntityTypeConfiguration.
/// </summary>
public static class WalletTransactionItemConfiguration
{
	public static void Configure(OwnedNavigationBuilder<WalletTransaction, WalletTransactionItem> builder)
	{
		builder.ToTable("wallet_transaction_items", "payment", t => t.HasComment(
			"Articolo consumato pagato con un movimento di debito del borsellino (es. una consumazione al bar), congelato al momento del consumo."));

		builder.HasKey(x => x.Id).HasName("pk_payment_wallet_transaction_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		// Riusa TransactionId come FK reale della relazione owned (come LedgerEntryLineConfiguration). Senza
		// WithOwner().HasForeignKey(...) EF crea una shadow property con colonna propria (wallet_transaction_id),
		// che porta la FK vera, e lascia transaction_id come colonna duplicata senza vincolo: era lo stato del
		// DB fino al 2026-10-06 (migration FixOwnedItemsForeignKeys).
		builder.WithOwner().HasForeignKey(x => x.TransactionId);

		builder.Property(x => x.TransactionId)
			.HasColumnName("transaction_id")
			.IsRequired()
			.HasComment("Movimento di debito a cui l'articolo appartiene.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per l'eventuale report per stabilimento senza join.");

		builder.HasIndex(x => x.PropertyId)
			.HasDatabaseName("ix_payment_wallet_transaction_items_property");

		builder.Property(x => x.SaleItemPublicId)
			.HasColumnName("sale_item_public_id")
			.HasComment("public_id di catalog.service_sale_items, copiato — nessuna FK cross-schema; articolo di catalogo consumato, quando risolvibile.");

		builder.Property(x => x.DescriptionSnapshot)
			.HasColumnName("description_snapshot")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Nome dell'articolo congelato al momento del consumo; il nome vivo si legge da C2 quando sale_item_public_id è valorizzato.");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasDefaultValue(1)
			.IsRequired()
			.HasComment("Quantità consumata.");

		builder.Property(x => x.UnitPrice)
			.HasColumnName("unit_price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Prezzo unitario dell'articolo al momento del consumo.");

		builder.Property(x => x.VariantsDescription)
			.HasColumnName("variants_description")
			.HasMaxLength(255)
			.HasComment("Varianti scelte in forma leggibile congelata; testo libero della riga-scontrino, non uno snapshot di un'entità modellata.");

		builder.Property(x => x.VariantsSurcharge)
			.HasColumnName("variants_surcharge")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Somma dei sovrapprezzi delle varianti su questa riga, inclusa in line_total.");

		builder.Property(x => x.LineTotal)
			.HasColumnName("line_total")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComputedColumnSql("quantity * unit_price + variants_surcharge", stored: true)
			.HasComment("Totale della riga — colonna generata (quantity * unit_price + " +
				"variants_surcharge). Sola lettura: le rettifiche sono un nuovo movimento con " +
				"source = 'adjustment'. Chiude anche un bug storico del legacy, dove il totale " +
				"arrivava spesso null o incoerente con le sue componenti.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: append-only con il movimento, sempre NULL.");

		builder.HasIndex(x => x.TransactionId)
			.HasDatabaseName("ix_payment_wallet_transaction_items_transaction");

		builder.HasIndex(x => x.SaleItemPublicId)
			.HasFilter("sale_item_public_id IS NOT NULL")
			.HasDatabaseName("ix_payment_wallet_transaction_items_sale_item");
	}
}
