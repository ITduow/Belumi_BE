using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belumi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSkinTypesToIngredient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotForSkin",
                table: "Ingredients",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SuitableSkin",
                table: "Ingredients",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotForSkin",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "SuitableSkin",
                table: "Ingredients");
        }
    }
}
