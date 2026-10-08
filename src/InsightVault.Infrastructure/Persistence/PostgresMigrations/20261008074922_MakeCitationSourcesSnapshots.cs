using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightVault.Infrastructure.Persistence.PostgresMigrations
{
    /// <inheritdoc />
    public partial class MakeCitationSourcesSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatAnswerCitations_DocumentChunks_DocumentChunkId",
                table: "ChatAnswerCitations");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatAnswerCitations_Documents_DocumentId",
                table: "ChatAnswerCitations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_ChatAnswerCitations_DocumentChunks_DocumentChunkId",
                table: "ChatAnswerCitations",
                column: "DocumentChunkId",
                principalTable: "DocumentChunks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatAnswerCitations_Documents_DocumentId",
                table: "ChatAnswerCitations",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
