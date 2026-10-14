using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class ResourceAvailabilityDailyConfiguration : IEntityTypeConfiguration<ResourceAvailabilityDaily>
{
	public void Configure(EntityTypeBuilder<ResourceAvailabilityDaily> builder)
	{
		builder.ToTable("resource_availability_daily", "booking", t => t.HasComment(
			"Proiezione di disponibilità per (risorsa/tipo × giorno), mantenuta dal DB via trigger su " +
			"booking_resource_consumptions: sostituisce il GROUP BY a runtime del legacy."));

		// Chiave primaria composita (id, consumption_date): PostgreSQL impone che la PK di una
		// tabella partizionata contenga la chiave di partizione (partizionamento per anno solare). id resta di fatto univoco da solo — e' pur
		// sempre un'identita' globale sulla tabella padre — ma il vincolo dichiarato e' sulla
		// coppia: e' il prezzo del partizionamento, non una scelta di modellazione.
		builder.HasKey(x => new { x.Id, x.ConsumptionDate }).HasName("pk_booking_resource_availability_daily");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema");

		builder.Property(x => x.ResourceType)
			.HasColumnName("resource_type")
			.HasColumnType("booking.resource_type")
			.IsRequired()
			.HasComment("Tipo di risorsa (stesso enum di booking_resource_consumptions.resource_type)");

		builder.Property(x => x.ResourceId)
			.HasColumnName("resource_id")
			.HasComment("public_id C2 della risorsa (proiezione fine); NULL per la proiezione grossolana per tipo");

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.HasComment("public_id di catalog.map_sectors, denormalizzato al momento del rebuild — nessuna FK cross-schema");

		builder.Property(x => x.ConsumptionDate)
			.HasColumnName("consumption_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Giorno della grana di disponibilità");

		builder.Property(x => x.BookingPeriod)
			.HasColumnName("booking_period")
			.HasColumnType("booking.day_period")
			.HasDefaultValueSql("'full_day'")
			.IsRequired()
			.HasComment("Fascia della grana (full_day aggrega mattina+pomeriggio in lettura)");

		builder.Property(x => x.BookedQty)
			.HasColumnName("booked_qty")
			.HasDefaultValue(0)
			.IsRequired()
			.HasComment("Unità occupate (state <> released) per la grana, mantenuto atomico dal trigger");

		builder.Property(x => x.HeldQty)
			.HasColumnName("held_qty")
			.HasDefaultValue(0)
			.IsRequired()
			.HasComment("Quota di booked_qty in stato held (blocco temporaneo non ancora confermato)");

		builder.Property(x => x.TotalQty)
			.HasColumnName("total_qty")
			.HasComment("Capienza totale della grana (1 per postazione, giacenza magazzino, capienza area, maximum_reservations servizio); NULL = nessun limite");

		builder.Property(x => x.AvailableQty)
			.HasColumnName("available_qty")
			.HasComputedColumnSql("total_qty - booked_qty", stored: true)
			.HasComment("total_qty - booked_qty quando total_qty è valorizzato, altrimenti NULL (illimitato) — colonna generata");

		builder.Property(x => x.RebuiltAt)
			.HasColumnName("rebuilt_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Ultimo ricalcolo completo dal job di riconciliazione");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione della riga di proiezione");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di ultimo UPSERT della riga di proiezione");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Presente per uniformità, non usato: la proiezione si ricostruisce, non si soft-elimina; le righe di date passate le rimuove la retention (hard-delete)");

		builder.HasIndex(x => new { x.PropertyId, x.ConsumptionDate, x.ResourceType })
			.HasFilter("available_qty > 0")
			.HasDatabaseName("ix_booking_resource_availability_daily_search");

		builder.HasIndex(x => new { x.PropertyId, x.ConsumptionDate })
			.HasFilter("resource_id IS NOT NULL")
			.HasDatabaseName("ix_booking_resource_availability_daily_spot_day");

		builder.HasIndex(x => new { x.SectorId, x.ConsumptionDate })
			.HasFilter("sector_id IS NOT NULL")
			.HasDatabaseName("ix_booking_resource_availability_daily_sector");

		// Indice unique di grana: usa coalesce(resource_id, ...) per trattare NULL come chiave, non
		// esprimibile in Fluent API pura — sostituito con SQL raw nella migration.
		builder.HasIndex(x => new { x.PropertyId, x.ResourceType, x.ResourceId, x.ConsumptionDate, x.BookingPeriod })
			.IsUnique()
			.HasDatabaseName("uq_booking_resource_availability_daily_grain");
	}
}
