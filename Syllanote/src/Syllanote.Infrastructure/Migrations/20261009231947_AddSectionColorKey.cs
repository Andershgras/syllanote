using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Syllanote.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionColorKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorKey",
                table: "Sections",
                type: "TEXT",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorKey",
                table: "Sections");
        }
    }
}
