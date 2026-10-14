using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Payment.Domain.Entities;

namespace Module.Payment.Infrastructure.Configurations;

/// <summary>
/// Mapping EF Core di <see cref="PosChargeItem"/> su payment.pos_charge_items. Applicata dentro
/// <see cref="PosChargeConfiguration"/> via OwnsMany, non registrata da ApplyConfigurationsFromAssembly:
/// <see cref="OwnedNavigationBuilder{TOwner,TDependent}"/> non e un <see cref="EntityTypeBuilder{TEntity}"/>
/// (nessun ApplyConfiguration(IEntityTypeConfiguration) su di esso), quindi qui si espone un metodo
/// statico invece di implementare IEntityTypeConfiguration.
/// </summary>
public static class PosChargeItemConfiguration
{
	public static void Configure(OwnedNavigationBuilder<PosCharge, PosChargeItem> builder)
	{
		builder.ToTable("pos_charge_items", "payment", t =>
		{
			t.HasComment("Prodotto/riga di uno scontrino POS: descrizione, quantità, prezzo, aliquota IVA, totale, congelati al momento della trasmissione.");
			t.HasCheckConstraint("ck_payment_pos_charge_items_quantity", "quantity > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_payment_pos_charge_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		// Riusa ChargeId come FK reale della relazione owned (come LedgerEntryLineConfiguration). Senza
		// WithOwner().HasForeignKey(...) EF crea una shadow property con colonna propria (pos_charge_id),
		// che porta la FK vera, e lascia charge_id come colonna duplicata senza vincolo: era lo stato del
		// DB fino al 2026-10-06 (migration FixOwnedItemsForeignKeys).
		builder.WithOwner().HasForeignKey(x => x.ChargeId);

		builder.Property(x => x.ChargeId)
			.HasColumnName("charge_id")
			.IsRequired()
			.HasComment("Scontrino POS a cui la riga appartiene.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per i report IVA degli addebiti POS e il report per articolo senza join.");

		builder.HasIndex(x => new { x.PropertyId, x.VatRate })
			.HasDatabaseName("ix_payment_pos_charge_items_property");

		builder.Property(x => x.ExternalLineRef)
			.HasColumnName("external_line_ref")
			.HasMaxLength(64)
			.HasComment("Id della riga sul POS di origine, quando fornito (solo OfSale).");

		builder.Property(x => x.SaleItemPublicId)
			.HasColumnName("sale_item_public_id")
			.HasComment("public_id di catalog.service_sale_items, copiato — nessuna FK cross-schema; articolo di catalogo corrispondente, quando mappabile.");

		builder.Property(x => x.DescriptionSnapshot)
			.HasColumnName("description_snapshot")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Descrizione del prodotto congelata al momento della trasmissione dal POS; il POS non modifica retroattivamente le righe trasmesse.");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasDefaultValue(1)
			.IsRequired()
			.HasComment("Quantità.");

		builder.Property(x => x.UnitPrice)
			.HasColumnName("unit_price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Prezzo unitario, IVA inclusa.");

		builder.Property(x => x.LineTotal)
			.HasColumnName("line_total")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Totale della riga, IVA inclusa; la somma delle righe deve coincidere con il totale scontrino entro 1 cent.");

		builder.Property(x => x.VatRate)
			.HasColumnName("vat_rate")
			.HasColumnType("numeric(5,2)")
			.IsRequired()
			.HasComment("Aliquota IVA % della riga.");

		builder.Property(x => x.NetAmount)
			.HasColumnName("net_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComputedColumnSql("round(line_total / (1 + vat_rate / 100), 2)", stored: true)
			.HasComment("Imponibile scorporato — colonna generata: round(line_total / (1 + " +
				"vat_rate/100), 2). Sola lettura: lo storno di un addebito POS porta status = " +
				"'voided' e un documento di storno a parte, mai una modifica di questi importi.");

		builder.Property(x => x.VatAmount)
			.HasColumnName("vat_amount")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComputedColumnSql("line_total - round(line_total / (1 + vat_rate / 100), 2)", stored: true)
			.HasComment("Quota IVA scorporata — colonna generata (line_total - net_amount). La " +
				"formula ripete lo scorporo invece di referenziare net_amount: PostgreSQL non " +
				"ammette che una colonna generata ne referenzi un'altra generata.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: append-only con lo scontrino, sempre NULL.");

		builder.HasIndex(x => x.ChargeId)
			.HasDatabaseName("ix_payment_pos_charge_items_charge");

		builder.HasIndex(x => x.SaleItemPublicId)
			.HasFilter("sale_item_public_id IS NOT NULL")
			.HasDatabaseName("ix_payment_pos_charge_items_sale_item");

		builder.HasIndex(x => x.VatRate)
			.HasDatabaseName("ix_payment_pos_charge_items_vat");
	}
}
