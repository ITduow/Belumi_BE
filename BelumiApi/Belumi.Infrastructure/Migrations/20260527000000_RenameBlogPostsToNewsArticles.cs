using Belumi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belumi.Infrastructure.Migrations;

[Migration("20260527000000_RenameBlogPostsToNewsArticles")]
[DbContext(typeof(BelumiDbContext))]
public partial class RenameBlogPostsToNewsArticles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_NewsLikes_BlogPosts_NewsId",
            table: "NewsLikes");

        migrationBuilder.DropForeignKey(
            name: "FK_NewsSaves_BlogPosts_NewsId",
            table: "NewsSaves");

        migrationBuilder.DropPrimaryKey(
            name: "PK_BlogPosts",
            table: "BlogPosts");

        migrationBuilder.RenameTable(
            name: "BlogPosts",
            newName: "NewsArticles");

        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF to_regclass('"IX_BlogPosts_Slug"') IS NOT NULL THEN
                    ALTER INDEX "IX_BlogPosts_Slug" RENAME TO "IX_NewsArticles_Slug";
                END IF;

                IF to_regclass('"IX_BlogPosts_Category_Status_PublishedAt"') IS NOT NULL THEN
                    ALTER INDEX "IX_BlogPosts_Category_Status_PublishedAt" RENAME TO "IX_NewsArticles_Category_Status_PublishedAt";
                END IF;
            END $$;

            WITH ranked AS (
                SELECT "Id", "Slug", ROW_NUMBER() OVER (PARTITION BY "Slug" ORDER BY "CreatedAt", "Id") AS rn
                FROM "NewsArticles"
            )
            UPDATE "NewsArticles" article
            SET "Slug" = article."Slug" || '-' || SUBSTRING(article."Id"::text, 1, 8)
            FROM ranked
            WHERE article."Id" = ranked."Id" AND ranked.rn > 1;

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_NewsArticles_Slug" ON "NewsArticles" ("Slug");
            CREATE INDEX IF NOT EXISTS "IX_NewsArticles_Category_Status_PublishedAt" ON "NewsArticles" ("Category", "Status", "PublishedAt");
            """);

        migrationBuilder.AddPrimaryKey(
            name: "PK_NewsArticles",
            table: "NewsArticles",
            column: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_NewsLikes_NewsArticles_NewsId",
            table: "NewsLikes",
            column: "NewsId",
            principalTable: "NewsArticles",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_NewsSaves_NewsArticles_NewsId",
            table: "NewsSaves",
            column: "NewsId",
            principalTable: "NewsArticles",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_NewsLikes_NewsArticles_NewsId",
            table: "NewsLikes");

        migrationBuilder.DropForeignKey(
            name: "FK_NewsSaves_NewsArticles_NewsId",
            table: "NewsSaves");

        migrationBuilder.DropPrimaryKey(
            name: "PK_NewsArticles",
            table: "NewsArticles");

        migrationBuilder.RenameTable(
            name: "NewsArticles",
            newName: "BlogPosts");

        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF to_regclass('"IX_NewsArticles_Slug"') IS NOT NULL THEN
                    ALTER INDEX "IX_NewsArticles_Slug" RENAME TO "IX_BlogPosts_Slug";
                END IF;

                IF to_regclass('"IX_NewsArticles_Category_Status_PublishedAt"') IS NOT NULL THEN
                    ALTER INDEX "IX_NewsArticles_Category_Status_PublishedAt" RENAME TO "IX_BlogPosts_Category_Status_PublishedAt";
                END IF;
            END $$;

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_BlogPosts_Slug" ON "BlogPosts" ("Slug");
            CREATE INDEX IF NOT EXISTS "IX_BlogPosts_Category_Status_PublishedAt" ON "BlogPosts" ("Category", "Status", "PublishedAt");
            """);

        migrationBuilder.AddPrimaryKey(
            name: "PK_BlogPosts",
            table: "BlogPosts",
            column: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_NewsLikes_BlogPosts_NewsId",
            table: "NewsLikes",
            column: "NewsId",
            principalTable: "BlogPosts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_NewsSaves_BlogPosts_NewsId",
            table: "NewsSaves",
            column: "NewsId",
            principalTable: "BlogPosts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
