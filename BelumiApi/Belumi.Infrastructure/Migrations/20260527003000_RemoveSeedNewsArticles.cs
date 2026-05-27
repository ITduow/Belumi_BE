using Belumi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belumi.Infrastructure.Migrations;

[Migration("20260527003000_RemoveSeedNewsArticles")]
[DbContext(typeof(BelumiDbContext))]
public partial class RemoveSeedNewsArticles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "NewsLikes"
            WHERE "NewsId" IN (
                SELECT "Id"
                FROM "NewsArticles"
                WHERE "Slug" IN (
                    'gentle-morning-routine',
                    'niacinamide-guide',
                    'makeup-nen-mong-ngay-nang',
                    'doc-bang-thanh-phan-3-phut',
                    'xu-huong-skin-cycling-cho-nguoi-moi',
                    'chon-serum-cho-da-dau-mun',
                    'trang-diem-cong-so-10-phut'
                )
            );

            DELETE FROM "NewsSaves"
            WHERE "NewsId" IN (
                SELECT "Id"
                FROM "NewsArticles"
                WHERE "Slug" IN (
                    'gentle-morning-routine',
                    'niacinamide-guide',
                    'makeup-nen-mong-ngay-nang',
                    'doc-bang-thanh-phan-3-phut',
                    'xu-huong-skin-cycling-cho-nguoi-moi',
                    'chon-serum-cho-da-dau-mun',
                    'trang-diem-cong-so-10-phut'
                )
            );

            DELETE FROM "NewsArticles"
            WHERE "Slug" IN (
                'gentle-morning-routine',
                'niacinamide-guide',
                'makeup-nen-mong-ngay-nang',
                'doc-bang-thanh-phan-3-phut',
                'xu-huong-skin-cycling-cho-nguoi-moi',
                'chon-serum-cho-da-dau-mun',
                'trang-diem-cong-so-10-phut'
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
