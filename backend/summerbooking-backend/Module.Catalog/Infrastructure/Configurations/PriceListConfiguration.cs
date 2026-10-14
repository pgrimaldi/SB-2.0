using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Module.Catalog.Domain.Entities;

using Module.Catalog.Domain.Enums;



namespace Module.Catalog.Infrastructure.Configurations;



/// <summary>Mapping EF Core di <see cref="PriceList"/> su catalog.pricing_price_lists.</summary>

public sealed class PriceListConfiguration : IEntityTypeConfiguration<PriceList>

{

	public void Configure(EntityTypeBuilder<PriceList> builder)

	{

		builder.ToTable("pricing_price_lists", "catalog", t => t.HasComment(

			"Voce di listino di uno stabilimento: un pacchetto o un articolo aggiuntivo."));



		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_lists");

		builder.Property(x => x.Id).UseIdentityAlwaysColumn();



		builder.Property(x => x.PublicId)

			.HasColumnName("public_id")

			.ValueGeneratedNever()

			.IsRequired()

			.HasComment("Id pubblico stabile, copiato in C1 booking_lines.price_list_package_id.");

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





		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_pricing_price_lists_public_id");



		builder.Property(x => x.PropertyId)

			.HasColumnName("property_id")

			.IsRequired()

			.HasComment("Stabilimento proprietario della voce di listino.");

		builder.HasOne(x => x.Property)

			.WithMany()

			.HasForeignKey(x => x.PropertyId)

			.HasConstraintName("fk_catalog_pricing_price_lists_property")

			.OnDelete(DeleteBehavior.Cascade);



		builder.Property(x => x.Kind)

			.HasColumnName("kind")

			.HasColumnType("catalog.price_list_kind")

			.IsRequired()

			.HasComment("Tipo di voce: pacchetto o articolo aggiuntivo.");



		builder.Property(x => x.Name)

			.HasColumnName("name")

			.HasMaxLength(255)

			.IsRequired()

			.HasComment("Nome della voce di listino, testo libero digitato dal gestore.");





		builder.Property(x => x.MaxPeople)

			.HasColumnName("max_people")

			.HasComment("Numero massimo di persone coperte dal pacchetto.");



		builder.Property(x => x.CalculationMethodId)

			.HasColumnName("calculation_method_id")

			.HasComment("Metodo di calcolo del prezzo multi-giorno (NULL = non impostato).");

		builder.HasOne(x => x.CalculationMethod)

			.WithMany()

			.HasForeignKey(x => x.CalculationMethodId)

			.HasConstraintName("fk_catalog_pricing_price_lists_calculation_method")

			.OnDelete(DeleteBehavior.Restrict);



		builder.Property(x => x.MonthlySetting)

			.HasColumnName("monthly_setting")

			.HasColumnType("catalog.monthly_setting")

			.HasDefaultValueSql("'effective_month_duration'")

			.HasComment("Come conteggiare la durata 'mese' nel calcolo mensile.");



		builder.Property(x => x.WeekendSetting)

			.HasColumnName("weekend_setting")

			.HasColumnType("catalog.weekend_setting")

			.HasDefaultValueSql("'saturday_sunday'")

			.HasComment("Quali giorni contano come weekend per la tariffa weekend.");



		builder.Property(x => x.SalesChannel)

			.HasColumnName("sales_channel")

			.HasColumnType("catalog.sales_channel")

			.HasDefaultValueSql("'manager_only'")

			.IsRequired()

			.HasComment("Canali su cui la voce è vendibile (gestionale / online / entrambi).");



		builder.Property(x => x.IsDefault)

			.HasColumnName("is_default")

			.HasDefaultValue(false)

			.IsRequired()

			.HasComment("Pacchetto predefinito dello stabilimento (base per prezzo minimo e Revenue Mgmt).");



		builder.Property(x => x.IsDefaultOnline)

			.HasColumnName("is_default_online")

			.HasDefaultValue(false)

			.IsRequired()

			.HasComment("Pacchetto predefinito per il contesto online (semantica da confermare col committente).");



		builder.Property(x => x.IsAreaPackage)

			.HasColumnName("is_area_package")

			.HasDefaultValue(false)

			.IsRequired()

			.HasComment("Il pacchetto copre un'area con più posti anziché una singola postazione.");



		builder.Property(x => x.IsCumulative)

			.HasColumnName("is_cumulative")

			.HasDefaultValue(false)

			.IsRequired()

			.HasComment("Pacchetto cumulabile con altri articoli/sconti (semantica esatta da chiarire).");



		builder.Property(x => x.AllowsPublicHolidays)

			.HasColumnName("allows_public_holidays")

			.HasDefaultValue(false)

			.IsRequired()

			.HasComment("La voce ha una tariffa dedicata per le date festive.");



		builder.Property(x => x.CreatedByUserId)

			.HasColumnName("created_by_user_id")

			.HasComment("public_id di identity_access_users, copiato — nessuna FK cross-schema.");



		builder.HasIndex(x => new { x.PropertyId, x.Kind })

			.HasFilter("deleted_at IS NULL")

			.HasDatabaseName("ix_catalog_pricing_price_lists_property_kind");



		// Overload con nome: due HasIndex sulle stesse colonne senza nome sono LO STESSO indice per EF,
		// e il secondo ne sovrascrive nome, filtro e unicità. Fino al 2026-10-06 a DB ne esisteva uno solo.
		builder.HasIndex(x => x.PropertyId, "ix_catalog_pricing_price_lists_property_default")

			.HasFilter("is_default AND deleted_at IS NULL")

			.HasDatabaseName("ix_catalog_pricing_price_lists_property_default");



		builder.HasIndex(x => x.PropertyId, "ix_catalog_pricing_price_lists_online")

			.HasFilter("sales_channel <> 'manager_only' AND deleted_at IS NULL")

			.HasDatabaseName("ix_catalog_pricing_price_lists_online");



		builder.HasMany(x => x.Items).WithOne().HasForeignKey("PriceListId");

		builder.HasMany(x => x.SectorPrices).WithOne().HasForeignKey("PriceListId");

		builder.HasMany(x => x.PeriodPrices).WithOne().HasForeignKey("PriceListId");

		builder.HasMany(x => x.DatePrices).WithOne().HasForeignKey("PriceListId");

		builder.HasMany(x => x.ReducedWindows).WithOne().HasForeignKey("PriceListId");

	}

}

