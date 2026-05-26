using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belumi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsLikeAndSave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsLikes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewsId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsLikes_BlogPosts_NewsId",
                        column: x => x.NewsId,
                        principalTable: "BlogPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NewsLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NewsSaves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewsId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsSaves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsSaves_BlogPosts_NewsId",
                        column: x => x.NewsId,
                        principalTable: "BlogPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NewsSaves_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsLikes_NewsId",
                table: "NewsLikes",
                column: "NewsId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsLikes_UserId_NewsId",
                table: "NewsLikes",
                columns: new[] { "UserId", "NewsId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsSaves_NewsId",
                table: "NewsSaves",
                column: "NewsId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsSaves_UserId_NewsId",
                table: "NewsSaves",
                columns: new[] { "UserId", "NewsId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsLikes");

            migrationBuilder.DropTable(
                name: "NewsSaves");
        }
    }
}
