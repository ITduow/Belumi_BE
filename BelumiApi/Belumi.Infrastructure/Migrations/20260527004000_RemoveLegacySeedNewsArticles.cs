using Belumi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belumi.Infrastructure.Migrations;

[Migration("20260527004000_RemoveLegacySeedNewsArticles")]
[DbContext(typeof(BelumiDbContext))]
public partial class RemoveLegacySeedNewsArticles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "NewsLikes"
            WHERE "NewsId" IN (
                SELECT "Id"
                FROM "NewsArticles"
                WHERE "Title" IN (
                    'How to Build a Gentle Morning Routine',
                    'Niacinamide: Small Ingredient, Big Range'
                )
                AND "Author" IN ('Belumi Team', 'Belumi Lab')
            );

            DELETE FROM "NewsSaves"
            WHERE "NewsId" IN (
                SELECT "Id"
                FROM "NewsArticles"
                WHERE "Title" IN (
                    'How to Build a Gentle Morning Routine',
                    'Niacinamide: Small Ingredient, Big Range'
                )
                AND "Author" IN ('Belumi Team', 'Belumi Lab')
            );

            DELETE FROM "NewsArticles"
            WHERE "Title" IN (
                'How to Build a Gentle Morning Routine',
                'Niacinamide: Small Ingredient, Big Range'
            )
            AND "Author" IN ('Belumi Team', 'Belumi Lab');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
