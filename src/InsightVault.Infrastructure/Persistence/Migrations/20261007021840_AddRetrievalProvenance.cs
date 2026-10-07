using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightVault.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRetrievalProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Documents",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "SectionTitle",
                table: "DocumentChunks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourcePageNumber",
                table: "DocumentChunks",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "ChatAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Question = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TopK = table.Column<int>(type: "int", nullable: false),
                    MinimumSimilarity = table.Column<decimal>(type: "decimal(4,3)", precision: 4, scale: 3, nullable: false),
                    RetrievalStrategyVersion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatAnswers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatAnswerCitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatAnswerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentVersion = table.Column<int>(type: "int", nullable: false),
                    DocumentChunkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourcePageNumber = table.Column<int>(type: "int", nullable: false),
                    SectionTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Score = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatAnswerCitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatAnswerCitations_ChatAnswers_ChatAnswerId",
                        column: x => x.ChatAnswerId,
                        principalTable: "ChatAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatAnswerCitations_DocumentChunks_DocumentChunkId",
                        column: x => x.DocumentChunkId,
                        principalTable: "DocumentChunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatAnswerCitations_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatAnswerCitations_ChatAnswerId_Rank",
                table: "ChatAnswerCitations",
                columns: new[] { "ChatAnswerId", "Rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatAnswerCitations_DocumentChunkId",
                table: "ChatAnswerCitations",
                column: "DocumentChunkId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatAnswerCitations_DocumentId",
                table: "ChatAnswerCitations",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatAnswers_OwnerUserId_CreatedAtUtc",
                table: "ChatAnswers",
                columns: new[] { "OwnerUserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatAnswerCitations");

            migrationBuilder.DropTable(
                name: "ChatAnswers");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SectionTitle",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "SourcePageNumber",
                table: "DocumentChunks");
        }
    }
}
