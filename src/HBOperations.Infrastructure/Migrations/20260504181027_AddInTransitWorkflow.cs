using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HBOperations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInTransitWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ═══ أولاً: تحويل قيم Enum القديمة ═══
            // Old: Sent=0, Received=1, Rejected=2, Cancelled=3, Archived=4
            // New: Sent=0, InTransit=1, Received=2, Rejected=3, Archived=4
            // ترتيب عكسي لتجنب التضارب
            
            // Cancelled(3) → حذف المعاملات الملغاة (أو تحويلها لـ Rejected=3)
            migrationBuilder.Sql("DELETE FROM [Transactions] WHERE [Status] = 3");
            
            // Rejected(2) → Rejected(3)
            migrationBuilder.Sql("UPDATE [Transactions] SET [Status] = 3 WHERE [Status] = 2");
            // تحديث History أيضاً
            migrationBuilder.Sql("UPDATE [TransactionHistories] SET [FromStatus] = 3 WHERE [FromStatus] = 2");
            migrationBuilder.Sql("UPDATE [TransactionHistories] SET [ToStatus] = 3 WHERE [ToStatus] = 2");
            
            // Received(1) → Received(2)
            migrationBuilder.Sql("UPDATE [Transactions] SET [Status] = 2 WHERE [Status] = 1");
            migrationBuilder.Sql("UPDATE [TransactionHistories] SET [FromStatus] = 2 WHERE [FromStatus] = 1");
            migrationBuilder.Sql("UPDATE [TransactionHistories] SET [ToStatus] = 2 WHERE [ToStatus] = 1");

            // ═══ ثانياً: إضافة الأعمدة الجديدة ═══
            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Transactions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourierName",
                table: "Transactions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSelfDelivery",
                table: "Transactions",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickedUpAt",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PickedUpByUserId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);

            // IsSelfDelivery = true لكل المعاملات الحالية
            migrationBuilder.Sql("UPDATE [Transactions] SET [IsSelfDelivery] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CourierName",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsSelfDelivery",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PickedUpAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PickedUpByUserId",
                table: "Transactions");
        }
    }
}
