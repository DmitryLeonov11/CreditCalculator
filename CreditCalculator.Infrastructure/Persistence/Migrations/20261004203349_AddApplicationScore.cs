using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditCalculator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "Applications",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "Applications");
        }
    }
}
