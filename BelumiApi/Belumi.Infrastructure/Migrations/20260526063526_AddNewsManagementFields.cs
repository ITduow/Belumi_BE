using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belumi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsManagementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LikeCount",
                table: "BlogPosts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "BlogPosts",
                type: "text",
                nullable: false,
                defaultValue: "Published");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "BlogPosts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "BlogPosts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Category_Status_PublishedAt",
                table: "BlogPosts",
                columns: new[] { "Category", "Status", "PublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BlogPosts_Category_Status_PublishedAt",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "LikeCount",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "BlogPosts");
        }
    }
}
