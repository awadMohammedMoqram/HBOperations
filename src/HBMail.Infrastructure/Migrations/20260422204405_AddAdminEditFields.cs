using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HBMail.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminEditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdminEditedAt",
                table: "Mails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminEditedByName",
                table: "Mails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AdminEditedByUserId",
                table: "Mails",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminEditedAt",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "AdminEditedByName",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "AdminEditedByUserId",
                table: "Mails");
        }
    }
}
