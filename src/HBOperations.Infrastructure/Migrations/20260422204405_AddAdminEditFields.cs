using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HBOperations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminEditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdminEditedAt",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminEditedByName",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AdminEditedByUserId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminEditedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AdminEditedByName",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AdminEditedByUserId",
                table: "Transactions");
        }
    }
}
