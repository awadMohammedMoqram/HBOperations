using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HBMail.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInTransitWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- ?????: ????? ??? Enum ??????? ---
            // Old: Sent=0, Received=1, Rejected=2, Cancelled=3, Archived=4
            // New: Sent=0, AssignedToStaff=1, Received=2, Rejected=3, Archived=4
            // ????? ???? ????? ???????
            
            // Cancelled(3) ? ??? ????????? ??????? (?? ??????? ?? Rejected=3)
            migrationBuilder.Sql("DELETE FROM [Mails] WHERE [Status] = 3");
            
            // Rejected(2) ? Rejected(3)
            migrationBuilder.Sql("UPDATE [Mails] SET [Status] = 3 WHERE [Status] = 2");
            // ????? History ?????
            migrationBuilder.Sql("UPDATE [MailHistories] SET [FromStatus] = 3 WHERE [FromStatus] = 2");
            migrationBuilder.Sql("UPDATE [MailHistories] SET [ToStatus] = 3 WHERE [ToStatus] = 2");
            
            // Received(1) ? Received(2)
            migrationBuilder.Sql("UPDATE [Mails] SET [Status] = 2 WHERE [Status] = 1");
            migrationBuilder.Sql("UPDATE [MailHistories] SET [FromStatus] = 2 WHERE [FromStatus] = 1");
            migrationBuilder.Sql("UPDATE [MailHistories] SET [ToStatus] = 2 WHERE [ToStatus] = 1");

            // --- ??????: ????? ??????? ??????? ---
            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Mails",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourierName",
                table: "Mails",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSelfDelivery",
                table: "Mails",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickedUpAt",
                table: "Mails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PickedUpByUserId",
                table: "Mails",
                type: "uniqueidentifier",
                nullable: true);

            // IsSelfDelivery = true ??? ????????? ???????
            migrationBuilder.Sql("UPDATE [Mails] SET [IsSelfDelivery] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "CourierName",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "IsSelfDelivery",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "PickedUpAt",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "PickedUpByUserId",
                table: "Mails");
        }
    }
}
