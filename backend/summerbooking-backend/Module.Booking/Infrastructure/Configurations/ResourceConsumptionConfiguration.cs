using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class ResourceConsumptionConfiguration : IEntityTypeConfiguration<ResourceConsumption>
{
	public void Configure(EntityTypeBuilder<ResourceConsumption> builder)
	{
		builder.ToTable("resource_consumptions", "booking", t =>
		{
			t.HasComment(
				"Una riga per (risorsa × giorno occupato × sorgente): unica fonte di verità della " +
				"disponibilità di postazioni, articoli, servizi e slot orari.");
			t.HasCheckConstraint("ck_booking_resource_consumptions_quantity_positive", "quantity > 0");
		});

		// Chiave primaria composita (id, consumption_date): PostgreSQL impone che la PK di una
		// tabella partizionata contenga la chiave di partizione (partizionamento per anno solare). id resta di fatto univoco da solo — e' pur
		// sempre un'identita' globale sulla tabella padre — ma il vincolo dichiarato e' sulla
		// coppia: e' il prezzo del partizionamento, non una scelta di modellazione.
		builder.HasKey(x => new { x.Id, x.ConsumptionDate }).HasName("pk_booking_resource_consumptions");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; prima colonna di ogni indice e dell'exclusion constraint");

		builder.Property(x => x.ResourceType)
			.HasColumnName("resource_type")
			.HasColumnType("booking.resource_type")
			.IsRequired()
			.HasComment("Tipo di risorsa consumata (postazione, articolo, servizio, slot orario)");

		builder.Property(x => x.ResourceId)
			.HasColumnName("resource_id")
			.HasComment("public_id C2 della risorsa (map_spots / service_sale_items / inventory_items secondo resource_type), copiato — nessuna FK cross-schema; NULL per articolo personalizzato non a catalogo");

		builder.Property(x => x.ResourceNameSnapshot)
			.HasColumnName("resource_name_snapshot")
			.HasMaxLength(160)
			.HasComment("Nome della risorsa congelato al momento del consumo, per reportistica anche se rinominata/non a catalogo");

		builder.Property(x => x.ConsumptionDate)
			.HasColumnName("consumption_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Il giorno occupato: una riga per giorno, mai un intervallo");

		builder.Property(x => x.BookingPeriod)
			.HasColumnName("booking_period")
			.HasColumnType("booking.day_period")
			.HasDefaultValueSql("'full_day'")
			.IsRequired()
			.HasComment("Fascia occupata; alimenta la colonna generata period_mask usata dall'exclusion constraint");

		builder.Property(x => x.SlotStart)
			.HasColumnName("slot_start")
			.HasColumnType("time")
			.HasComment("Inizio fascia oraria interna, valorizzato solo per resource_type = hourly_service");

		builder.Property(x => x.SlotEnd)
			.HasColumnName("slot_end")
			.HasColumnType("time")
			.HasComment("Fine fascia oraria interna, valorizzato solo per resource_type = hourly_service");

		builder.Property(x => x.TimeSlotId)
			.HasColumnName("time_slot_id")
			.HasComment("public_id di catalog.service_sale_item_time_slots (fascia di catalogo prenotata), copiato — nessuna FK cross-schema; solo hourly_service");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasDefaultValue(1)
			.IsRequired()
			.HasComment("Unità consumate in quel giorno (1 per postazione puntuale, N per magazzino/area)");

		builder.Property(x => x.State)
			.HasColumnName("state")
			.HasColumnType("booking.consumption_state")
			.HasDefaultValueSql("'confirmed'")
			.IsRequired()
			.HasComment("Stato del consumo, indipendente dal pagamento: bloccato temporaneamente, occupazione effettiva, liberato");

		builder.Property(x => x.HeldUntil)
			.HasColumnName("held_until")
			.HasComment("Scadenza del blocco temporaneo (state = held), calcolata server-side (now() + Booking:HoldTtl), mai dal body");

		builder.Property(x => x.SourceKind)
			.HasColumnName("source_kind")
			.HasColumnType("booking.consumption_source")
			.IsRequired()
			.HasComment("Cosa ha generato il consumo: riga postazione, riga servizio, articolo aggiuntivo, blocco manuale, ripresa di rivendita");

		builder.Property(x => x.SourceId)
			.HasColumnName("source_id")
			.HasComment("Id (interno C1) dell'entità di origine indicata da source_kind — nessuna FK polimorfa DB, coerenza gestita in applicazione; NULL per manual_hold senza entità a monte");

		builder.Property(x => x.BookingNumber)
			.HasColumnName("booking_number")
			.HasMaxLength(40)
			.HasComment("Numero prenotazione copiato per query dirette; NULL per manual_hold");

		builder.Property(x => x.BookingChannel)
			.HasColumnName("booking_channel")
			.HasColumnType("booking.booking_channel")
			.HasComment("Canale della prenotazione di origine, per statistiche sui consumi; NULL per manual_hold");

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


		builder.HasIndex(x => new { x.PropertyId, x.ResourceId, x.ConsumptionDate })
			.HasFilter("state <> 'released'")
			.HasDatabaseName("ix_booking_resource_consumptions_availability");

		builder.HasIndex(x => new { x.PropertyId, x.ConsumptionDate, x.State })
			.HasDatabaseName("ix_booking_resource_consumptions_property_date_state");

		builder.HasIndex(x => new { x.PropertyId, x.ResourceType, x.ConsumptionDate })
			.HasFilter("state <> 'released'")
			.HasDatabaseName("ix_booking_resource_consumptions_type_date");

		builder.HasIndex(x => new { x.SourceKind, x.SourceId })
			.HasDatabaseName("ix_booking_resource_consumptions_source");

		builder.HasIndex(x => x.BookingNumber)
			.HasDatabaseName("ix_booking_resource_consumptions_booking_number");

		builder.HasIndex(x => x.HeldUntil)
			.HasFilter("state = 'held' AND held_until IS NOT NULL")
			.HasDatabaseName("ix_booking_resource_consumptions_holds_expiry");

		// source_id: nessuna FK EF (riferimento polimorfo secondo source_kind verso booking_lines,
		// booking_service_lines, booking_line_additional_items, booking_spot_holds, booking_resale_offers)

		// Exclusion constraint GiST anti-doppia-prenotazione + colonna generata period_mask: non
		// esprimibili in Fluent API, aggiunti con SQL raw nella migration.
	}
}
