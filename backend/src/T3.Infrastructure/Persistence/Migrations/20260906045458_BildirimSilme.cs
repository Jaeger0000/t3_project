using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace T3.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BildirimSilme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RecipientDeletedAt",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecipientDeletedAt",
                table: "Notifications");
        }
    }
}
