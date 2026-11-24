using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Privacy.Infrastructure.Migrations
{
    /// <summary>
    /// Policy per la tabella nuova degli ingressi di Booking («Segnala ingresso»): dato operativo del giorno, con
    /// l'operatore che l'ha segnato; si cancella dopo una stagione, come i consumi delle postazioni. Nasce in prova.
    /// </summary>
    public partial class AddBookingEntranceRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
INSERT INTO privacy.privacy_retention_policies
  (public_id, schema_name, table_name, field_names, reference_date_column,
   duration_value, duration_unit, action, only_if_unreferenced, reference_event,
   storage_prefix, storage_date_source, target_filter, linked_asset, is_active,
   basis_source, legal_basis)
VALUES
  ('01a1ac85-013b-717b-8668-8556fc57ef39', 'booking', 'entrances', '{}', 'entry_date', 1, 'seasons', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.c ed e GDPR: gli ingressi segnati servono solo nel giorno a cui si riferiscono e riportano l''operatore che li ha segnati. Conservati per la stagione, per le contestazioni, poi cancellati come i consumi delle postazioni.');
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM privacy.privacy_retention_policies WHERE public_id = '01a1ac85-013b-717b-8668-8556fc57ef39';");
        }
    }
}
