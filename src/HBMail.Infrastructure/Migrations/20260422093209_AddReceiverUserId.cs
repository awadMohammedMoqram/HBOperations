using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HBMail.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiverUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReceiverUserId",
                table: "Mails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mails_ReceiverUserId_Status",
                table: "Mails",
                columns: new[] { "ReceiverUserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Mails_ReceiverUserId_Status",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "ReceiverUserId",
                table: "Mails");
        }
    }
}
