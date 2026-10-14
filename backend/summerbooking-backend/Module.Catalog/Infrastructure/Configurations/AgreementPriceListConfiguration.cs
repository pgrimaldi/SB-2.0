using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="AgreementPriceList"/> su catalog.agreement_price_lists.</summary>
public sealed class AgreementPriceListConfiguration : IEntityTypeConfiguration<AgreementPriceList>
{
	public void Configure(EntityTypeBuilder<AgreementPriceList> builder)
	{
		builder.ToTable("agreement_price_lists", "catalog", t =>
		{
			t.HasComment("Listino ordinario o voucher venduto da una struttura partner (convenzione).");
			t.HasCheckConstraint("ck_catalog_agreement_price_lists_min_booking_days",
				"list_type = 'voucher' OR min_booking_days IS NULL");
			t.HasCheckConstraint("ck_catalog_agreement_price_lists_max_voucher_quantity",
				"list_type = 'voucher' OR max_voucher_quantity IS NULL");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_agreement_price_lists");
		builder.Property(x => x.Id).HasColumnType("integer").UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico (UUIDv7), copiato — C1 booking_agreement_lines referenzia il listino/voucher consumato.");

		builder.Property(x => x.PartnerId)
			.HasColumnName("partner_id")
			.HasColumnType("integer")
			.IsRequired()
			.HasComment("Struttura partner a cui appartiene il listino/voucher.");

		builder.Property(x => x.ListType)
			.HasColumnName("list_type")
			.HasColumnType("catalog.agreement_list_type")
			.IsRequired()
			.HasDefaultValueSql("'regular'")
			.HasComment("regular = listino ordinario; voucher = buono riscattabile presso la struttura partner, con vincoli d'uso.");

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("catalog.price_list_kind")
			.IsRequired()
			.HasDefaultValueSql("'package'")
			.HasComment("Tipo di voce: pacchetto o articolo aggiuntivo (enum condiviso con catalog_pricing_price_lists).");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Nome del listino/voucher (testo libero digitato dal manager).");


		builder.Property(x => x.MaxPeople)
			.HasColumnName("max_people")
			.HasColumnType("smallint")
			.HasComment("Numero massimo di persone coperte.");

		builder.Property(x => x.CalculationMethodId)
			.HasColumnName("calculation_method_id")
			.HasColumnType("smallint")
			.HasComment("Metodo di calcolo prezzo multi-giorno (lookup condiviso catalog_pricing_calculation_methods).");

		builder.Property(x => x.MonthlySetting)
			.HasColumnName("monthly_setting")
			.HasColumnType("catalog.monthly_setting")
			.HasDefaultValueSql("'effective_month_duration'")
			.HasComment("Impostazione durata mensile (enum condiviso con catalog_pricing_price_lists).");

		builder.Property(x => x.WeekendSetting)
			.HasColumnName("weekend_setting")
			.HasColumnType("catalog.weekend_setting")
			.HasDefaultValueSql("'saturday_sunday'")
			.HasComment("Impostazione giorni weekend (enum condiviso con catalog_pricing_price_lists).");

		builder.Property(x => x.AllowsPublicHolidays)
			.HasColumnName("allows_public_holidays")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Indica se il listino/voucher prevede una tariffa festiva dedicata.");

		builder.Property(x => x.MinBookingDays)
			.HasColumnName("min_booking_days")
			.HasColumnType("smallint")
			.HasComment("Durata minima di soggiorno perché il voucher sia applicabile (solo list_type = voucher).");

		builder.Property(x => x.MaxVoucherQuantity)
			.HasColumnName("max_voucher_quantity")
			.HasColumnType("smallint")
			.HasComment("Numero massimo di utilizzi del voucher per cliente per stagione; NULL = nessun limite (solo list_type = voucher).");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access_users (C5), copiato — operatore che ha creato il listino, nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete, anche a cascata dalla cancellazione della struttura partner.");

		builder.HasOne(x => x.Partner)
			.WithMany(p => p.PriceLists)
			.HasForeignKey(x => x.PartnerId)
			.HasConstraintName("fk_catalog_agreement_price_lists_partner")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<CalculationMethod>()
			.WithMany()
			.HasForeignKey(x => x.CalculationMethodId)
			.HasConstraintName("fk_catalog_agreement_price_lists_calculation_method")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasMany(x => x.Items)
			.WithOne(i => i.PriceList)
			.HasForeignKey(i => i.PriceListId);
		builder.HasMany(x => x.SectorPrices)
			.WithOne(s => s.PriceList)
			.HasForeignKey(s => s.PriceListId);
		builder.HasMany(x => x.PeriodPrices)
			.WithOne(p => p.PriceList)
			.HasForeignKey(p => p.PriceListId);
		builder.HasMany(x => x.DatePrices)
			.WithOne(d => d.PriceList)
			.HasForeignKey(d => d.PriceListId);

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_catalog_agreement_price_lists_public_id");

		builder.HasIndex(x => new { x.PartnerId, x.ListType })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_price_lists_partner_kind");
	}
}
