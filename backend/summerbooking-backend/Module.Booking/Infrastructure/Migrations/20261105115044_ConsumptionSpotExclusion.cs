using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Booking.Infrastructure.Migrations
{
    /// <summary>
    /// Una postazione a uso esclusivo non si occupa due volte nello stesso giorno e nella stessa fascia: vincolo di
    /// esclusione sui consumi che portano la postazione (resource_id) e non sono rilasciati. Mattina e pomeriggio non si
    /// sovrappongono, la giornata intera si sovrappone a entrambi. Gli articoli dei pacchetti (resource_id NULL), le aree,
    /// il magazzino e i servizi restano fuori: li regola la capienza. La chiave di partizione (consumption_date) è nel
    /// vincolo con l'uguaglianza, come vuole PostgreSQL per le tabelle partizionate.
    /// </summary>
    public partial class ConsumptionSpotExclusion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE booking.resource_consumptions
    ADD CONSTRAINT ex_booking_resource_consumptions_spot
    EXCLUDE USING gist (
        resource_id WITH =,
        consumption_date WITH =,
        int4range(CASE booking_period WHEN 'afternoon' THEN 2 ELSE 1 END,
                  CASE booking_period WHEN 'morning' THEN 2 ELSE 3 END) WITH &&)
    WHERE (state <> 'released' AND deleted_at IS NULL AND resource_id IS NOT NULL
           AND resource_type IN ('umbrella', 'sunbed', 'sunbed_xl', 'cabin', 'special_cabin', 'tent', 'parking'));

COMMENT ON CONSTRAINT ex_booking_resource_consumptions_spot ON booking.resource_consumptions IS
    'Postazione a uso esclusivo occupata una sola volta per giorno e fascia (mattina, pomeriggio; la giornata intera vale entrambe).';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE booking.resource_consumptions DROP CONSTRAINT IF EXISTS ex_booking_resource_consumptions_spot;");
        }
    }
}
