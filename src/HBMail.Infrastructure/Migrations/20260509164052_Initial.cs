using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HBMail.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MailAttachments_Mails_TransactionId",
                table: "MailAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_MailHistories_Mails_TransactionId",
                table: "MailHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_MailNotes_Mails_TransactionId",
                table: "MailNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Mails_TransactionId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "CourierName",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "IsSelfDelivery",
                table: "Mails");

            migrationBuilder.RenameColumn(
                name: "TransactionId",
                table: "Notifications",
                newName: "MailId");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_TransactionId",
                table: "Notifications",
                newName: "IX_Notifications_MailId");

            migrationBuilder.RenameColumn(
                name: "PickedUpByUserId",
                table: "Mails",
                newName: "OriginalReceiverUserId");

            migrationBuilder.RenameColumn(
                name: "PickedUpAt",
                table: "Mails",
                newName: "AssignedAt");

            migrationBuilder.RenameColumn(
                name: "TransactionId",
                table: "MailNotes",
                newName: "MailId");

            migrationBuilder.RenameIndex(
                name: "IX_MailNotes_TransactionId",
                table: "MailNotes",
                newName: "IX_MailNotes_MailId");

            migrationBuilder.RenameColumn(
                name: "TransactionId",
                table: "MailHistories",
                newName: "MailId");

            migrationBuilder.RenameIndex(
                name: "IX_MailHistories_TransactionId",
                table: "MailHistories",
                newName: "IX_MailHistories_MailId");

            migrationBuilder.RenameColumn(
                name: "TransactionId",
                table: "MailAttachments",
                newName: "MailId");

            migrationBuilder.RenameIndex(
                name: "IX_MailAttachments_TransactionId",
                table: "MailAttachments",
                newName: "IX_MailAttachments_MailId");

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedByUserId",
                table: "Mails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ManagerNotifiedUserId",
                table: "Mails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MailCCs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailCCs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailCCs_Mails_MailId",
                        column: x => x.MailId,
                        principalTable: "Mails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailCCs_MailId_UserId",
                table: "MailCCs",
                columns: new[] { "MailId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MailCCs_UserId",
                table: "MailCCs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MailAttachments_Mails_MailId",
                table: "MailAttachments",
                column: "MailId",
                principalTable: "Mails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MailHistories_Mails_MailId",
                table: "MailHistories",
                column: "MailId",
                principalTable: "Mails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MailNotes_Mails_MailId",
                table: "MailNotes",
                column: "MailId",
                principalTable: "Mails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Mails_MailId",
                table: "Notifications",
                column: "MailId",
                principalTable: "Mails",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MailAttachments_Mails_MailId",
                table: "MailAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_MailHistories_Mails_MailId",
                table: "MailHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_MailNotes_Mails_MailId",
                table: "MailNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Mails_MailId",
                table: "Notifications");

            migrationBuilder.DropTable(
                name: "MailCCs");

            migrationBuilder.DropColumn(
                name: "AssignedByUserId",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "ManagerNotifiedUserId",
                table: "Mails");

            migrationBuilder.RenameColumn(
                name: "MailId",
                table: "Notifications",
                newName: "TransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_Notifications_MailId",
                table: "Notifications",
                newName: "IX_Notifications_TransactionId");

            migrationBuilder.RenameColumn(
                name: "OriginalReceiverUserId",
                table: "Mails",
                newName: "PickedUpByUserId");

            migrationBuilder.RenameColumn(
                name: "AssignedAt",
                table: "Mails",
                newName: "PickedUpAt");

            migrationBuilder.RenameColumn(
                name: "MailId",
                table: "MailNotes",
                newName: "TransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_MailNotes_MailId",
                table: "MailNotes",
                newName: "IX_MailNotes_TransactionId");

            migrationBuilder.RenameColumn(
                name: "MailId",
                table: "MailHistories",
                newName: "TransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_MailHistories_MailId",
                table: "MailHistories",
                newName: "IX_MailHistories_TransactionId");

            migrationBuilder.RenameColumn(
                name: "MailId",
                table: "MailAttachments",
                newName: "TransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_MailAttachments_MailId",
                table: "MailAttachments",
                newName: "IX_MailAttachments_TransactionId");

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
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_MailAttachments_Mails_TransactionId",
                table: "MailAttachments",
                column: "TransactionId",
                principalTable: "Mails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MailHistories_Mails_TransactionId",
                table: "MailHistories",
                column: "TransactionId",
                principalTable: "Mails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MailNotes_Mails_TransactionId",
                table: "MailNotes",
                column: "TransactionId",
                principalTable: "Mails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Mails_TransactionId",
                table: "Notifications",
                column: "TransactionId",
                principalTable: "Mails",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
