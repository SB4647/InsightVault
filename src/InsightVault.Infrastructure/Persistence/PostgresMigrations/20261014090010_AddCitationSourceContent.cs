using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace InsightVault.Infrastructure.Persistence.PostgresMigrations;

/// <summary>Adds immutable source content to PostgreSQL answer citations.</summary>
[DbContext(typeof(PostgresApplicationDbContext))]
[Migration("20261014090010_AddCitationSourceContent")]
public partial class AddCitationSourceContent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DocumentName",
            table: "ChatAnswerCitations",
            type: "character varying(260)",
            maxLength: 260,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "SourceText",
            table: "ChatAnswerCitations",
            type: "text",
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DocumentName", table: "ChatAnswerCitations");
        migrationBuilder.DropColumn(name: "SourceText", table: "ChatAnswerCitations");
    }
}

