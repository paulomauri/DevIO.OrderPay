using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevIO.OrderPay.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_ProcessedOn",
                table: "OutboxMessage");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedAt",
                table: "OutboxMessage",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimedBy",
                table: "OutboxMessage",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ProcessedOn_OccurredOn",
                table: "OutboxMessage",
                columns: new[] { "ProcessedOn", "OccurredOn" },
                filter: "[ProcessedOn] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessage_ProcessedOn_OccurredOn",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "ClaimedAt",
                table: "OutboxMessage");

            migrationBuilder.DropColumn(
                name: "ClaimedBy",
                table: "OutboxMessage");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ProcessedOn",
                table: "OutboxMessage",
                column: "ProcessedOn");
        }
    }
}
