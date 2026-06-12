using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HBMail.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanMailModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Mails_Priority",
                table: "Mails");

            migrationBuilder.DropIndex(
                name: "IX_Mails_Type",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Mails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Mails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Mails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Mails_Priority",
                table: "Mails",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Mails_Type",
                table: "Mails",
                column: "Type");
        }
    }
}
