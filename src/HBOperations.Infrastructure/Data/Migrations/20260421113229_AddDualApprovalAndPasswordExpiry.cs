using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HBOperations.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDualApprovalAndPasswordExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FirstApprovedAt",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FirstApprovedByUserId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecondApprovedAt",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SecondApprovedByUserId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ForcePasswordChange",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordChangedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstApprovedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FirstApprovedByUserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SecondApprovedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SecondApprovedByUserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ForcePasswordChange",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PasswordChangedAt",
                table: "AspNetUsers");
        }
    }
}
