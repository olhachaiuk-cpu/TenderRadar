using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenderRadar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenderSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "tenders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Summary",
                table: "tenders");
        }
    }
}
