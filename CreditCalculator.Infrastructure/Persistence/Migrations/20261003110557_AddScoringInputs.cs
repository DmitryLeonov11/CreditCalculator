using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditCalculator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScoringInputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBelarusianMade",
                table: "CreditProducts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DependentsAtApply",
                table: "Applications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DownPaymentAmountAtApply",
                table: "Applications",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmploymentMonthsAtApply",
                table: "Applications",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBelarusianMade",
                table: "CreditProducts");

            migrationBuilder.DropColumn(
                name: "DependentsAtApply",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "DownPaymentAmountAtApply",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "EmploymentMonthsAtApply",
                table: "Applications");
        }
    }
}
