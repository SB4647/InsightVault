using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace InsightVault.Infrastructure.Persistence.Migrations;

/// <summary>Adds immutable source content to answer citations so document deletion does not erase provenance.</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261014090000_AddCitationSourceContent")]
public partial class AddCitationSourceContent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DocumentName",
            table: "ChatAnswerCitations",
            type: "nvarchar(260)",
            maxLength: 260,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "SourceText",
            table: "ChatAnswerCitations",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DocumentName", table: "ChatAnswerCitations");
        migrationBuilder.DropColumn(name: "SourceText", table: "ChatAnswerCitations");
    }
}

