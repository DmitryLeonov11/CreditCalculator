using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditCalculator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScoringResultsAndAgeSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Profiles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDateAtApply",
                table: "Applications",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenderAtApply",
                table: "Applications",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScoringResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RuleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoringResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoringResults_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScoringResults_ApplicationId_RuleCode",
                table: "ScoringResults",
                columns: new[] { "ApplicationId", "RuleCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScoringResults");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "BirthDateAtApply",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "GenderAtApply",
                table: "Applications");
        }
    }
}
