using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HBOperations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiverUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReceiverUserId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ReceiverUserId_Status",
                table: "Transactions",
                columns: new[] { "ReceiverUserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_ReceiverUserId_Status",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ReceiverUserId",
                table: "Transactions");
        }
    }
}
