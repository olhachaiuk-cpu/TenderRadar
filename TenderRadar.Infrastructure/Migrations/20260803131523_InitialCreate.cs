using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenderRadar.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenders",
                columns: table => new
                {
                    PublicationNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ShortTitle = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BuyerName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Country = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    PublicationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SubmissionDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CpvCodes = table.Column<string[]>(type: "text[]", nullable: false),
                    EstimatedValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MatchedKeywords = table.Column<string[]>(type: "text[]", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SearchText = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenders", x => new { x.Source, x.PublicationNumber });
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenders_ExportedAt",
                table: "tenders",
                column: "ExportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_tenders_PublicationDate",
                table: "tenders",
                column: "PublicationDate");

            migrationBuilder.CreateIndex(
                name: "IX_tenders_Score",
                table: "tenders",
                column: "Score");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenders");
        }
    }
}
