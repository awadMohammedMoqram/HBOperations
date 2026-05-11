using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HBMail.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionRoleNotes2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiverNote",
                table: "Mails",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionNote",
                table: "Mails",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderNote",
                table: "Mails",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiverNote",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "RejectionNote",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "SenderNote",
                table: "Mails");
        }
    }
}
