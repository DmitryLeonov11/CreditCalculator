using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditCalculator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApplicationStatusHistories_ApplicationId",
                table: "ApplicationStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Applications_UserId",
                table: "Applications");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistories_ApplicationId_ChangedAt",
                table: "ApplicationStatusHistories",
                columns: new[] { "ApplicationId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_Status_CreatedAt",
                table: "Applications",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_UserId_CreatedAt",
                table: "Applications",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApplicationStatusHistories_ApplicationId_ChangedAt",
                table: "ApplicationStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Applications_Status_CreatedAt",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_UserId_CreatedAt",
                table: "Applications");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusHistories_ApplicationId",
                table: "ApplicationStatusHistories",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_UserId",
                table: "Applications",
                column: "UserId");
        }
    }
}
