using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightVault.Infrastructure.Persistence.PostgresMigrations
{
    /// <inheritdoc />
    public partial class AddDocumentProcessingOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentProcessingOutboxEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DispatchedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDispatchAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DispatchAttemptCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentProcessingOutboxEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentProcessingOutboxEntries_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentProcessingOutboxEntries_DispatchedAtUtc_CreatedAtUtc",
                table: "DocumentProcessingOutboxEntries",
                columns: new[] { "DispatchedAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentProcessingOutboxEntries_DocumentId",
                table: "DocumentProcessingOutboxEntries",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentProcessingOutboxEntries_OwnerUserId_DocumentId",
                table: "DocumentProcessingOutboxEntries",
                columns: new[] { "OwnerUserId", "DocumentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentProcessingOutboxEntries");
        }
    }
}
