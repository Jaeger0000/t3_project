using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace T3.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AsistanExcelDisaAktarma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExportDownloadToken",
                table: "AiConversationMessages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExportFileName",
                table: "AiConversationMessages",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExportDownloadToken",
                table: "AiConversationMessages");

            migrationBuilder.DropColumn(
                name: "ExportFileName",
                table: "AiConversationMessages");
        }
    }
}
