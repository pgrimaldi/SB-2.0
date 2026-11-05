using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Payment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgreementWording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "agreement_line_public_id",
                schema: "payment",
                table: "ledger_entries",
                type: "uuid",
                nullable: true,
                comment: "public_id di booking.booking_agreement_lines, copiato — nessuna FK cross-schema; valorizzato per addebiti reali su voucher di una convenzione.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id di booking.booking_agreement_lines, copiato — nessuna FK cross-schema; valorizzato per addebiti reali su voucher hotel.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "agreement_line_public_id",
                schema: "payment",
                table: "ledger_entries",
                type: "uuid",
                nullable: true,
                comment: "public_id di booking.booking_agreement_lines, copiato — nessuna FK cross-schema; valorizzato per addebiti reali su voucher hotel.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id di booking.booking_agreement_lines, copiato — nessuna FK cross-schema; valorizzato per addebiti reali su voucher di una convenzione.");
        }
    }
}
