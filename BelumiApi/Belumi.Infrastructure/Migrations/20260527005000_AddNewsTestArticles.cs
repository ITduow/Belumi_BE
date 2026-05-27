using Belumi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belumi.Infrastructure.Migrations;

[Migration("20260527005000_AddNewsTestArticles")]
[DbContext(typeof(BelumiDbContext))]
public partial class AddNewsTestArticles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO "NewsArticles"
                ("Id", "Title", "Slug", "Summary", "Content", "CoverImageUrl", "Category", "Tags", "Author", "Status", "ViewCount", "LikeCount", "PublishedAt", "IsActive", "CreatedAt", "UpdatedAt")
            VALUES
                (
                    'b1010000-0000-4000-8000-000000000001',
                    'Routine phục hồi da sau treatment',
                    'routine-phuc-hoi-da-sau-treatment',
                    'Các bước làm dịu, cấp ẩm và củng cố hàng rào bảo vệ da sau khi dùng active.',
                    'Sau treatment, làn da cần được ưu tiên phục hồi. Hãy dùng sữa rửa mặt dịu nhẹ, toner cấp ẩm, serum panthenol hoặc niacinamide nồng độ vừa phải, kem dưỡng có ceramide và kem chống nắng vào ban ngày. Tránh tẩy da chết mạnh hoặc layer quá nhiều active trong cùng một routine.',
                    'https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=1200&q=80',
                    'Skincare',
                    'skincare,routine,barrier',
                    'Belumi Lab',
                    'Published',
                    156,
                    24,
                    TIMESTAMPTZ '2026-05-24 09:00:00+00',
                    TRUE,
                    TIMESTAMPTZ '2026-05-24 09:00:00+00',
                    NULL
                ),
                (
                    'b1010000-0000-4000-8000-000000000002',
                    'Niacinamide có hợp với da dầu mụn không?',
                    'niacinamide-co-hop-voi-da-dau-mun-khong',
                    'Tìm hiểu công dụng, nồng độ nên bắt đầu và cách kết hợp niacinamide trong routine.',
                    'Niacinamide là thành phần đa nhiệm thường được dùng để hỗ trợ điều tiết dầu, cải thiện vẻ ngoài lỗ chân lông và làm đều màu da. Người mới nên bắt đầu ở nồng độ vừa phải, dùng đều đặn và theo dõi phản ứng da. Nếu da đang kích ứng, hãy giảm tần suất và ưu tiên phục hồi.',
                    'https://images.unsplash.com/photo-1608248543803-ba4f8c70ae0b?auto=format&fit=crop&w=1200&q=80',
                    'Ingredient Knowledge',
                    'ingredient,niacinamide,oily-skin',
                    'Belumi Ingredient Desk',
                    'Published',
                    211,
                    37,
                    TIMESTAMPTZ '2026-05-23 09:00:00+00',
                    TRUE,
                    TIMESTAMPTZ '2026-05-23 09:00:00+00',
                    NULL
                ),
                (
                    'b1010000-0000-4000-8000-000000000003',
                    'Makeup nền mỏng cho ngày nắng',
                    'makeup-nen-mong-cho-ngay-nang',
                    'Gợi ý lớp nền nhẹ, bền màu và không gây cảm giác bí da trong thời tiết nóng.',
                    'Với ngày nắng, hãy chuẩn bị da bằng dưỡng ẩm mỏng nhẹ và kem chống nắng ráo mặt. Chọn cushion hoặc skin tint có độ che phủ vừa phải, set phấn vùng chữ T và dùng má kem tông đào để giữ vẻ tươi tắn. Xịt khóa nền ở bước cuối nếu cần di chuyển nhiều.',
                    'https://images.unsplash.com/photo-1596462502278-27bfdc403348?auto=format&fit=crop&w=1200&q=80',
                    'Makeup',
                    'makeup,base,summer',
                    'Belumi Studio',
                    'Published',
                    132,
                    18,
                    TIMESTAMPTZ '2026-05-22 09:00:00+00',
                    TRUE,
                    TIMESTAMPTZ '2026-05-22 09:00:00+00',
                    NULL
                ),
                (
                    'b1010000-0000-4000-8000-000000000004',
                    'Đọc bảng thành phần mỹ phẩm trong 3 phút',
                    'doc-bang-thanh-phan-my-pham-trong-3-phut',
                    'Cách nhận diện nhóm cấp ẩm, phục hồi, hoạt chất chính và thành phần dễ kích ứng.',
                    'Khi đọc bảng thành phần, hãy nhìn các nhóm chính: nền dung môi, chất cấp ẩm như glycerin hoặc hyaluronic acid, hoạt chất như BHA/AHA/retinoid, nhóm phục hồi như ceramide và các chất có khả năng gây kích ứng như hương liệu hoặc cồn khô. Không cần thuộc hết, chỉ cần nhận diện nhóm phù hợp với mục tiêu da.',
                    'https://images.unsplash.com/photo-1571781926291-c477ebfd024b?auto=format&fit=crop&w=1200&q=80',
                    'Ingredient Knowledge',
                    'ingredient,ocr,safety',
                    'Belumi Lab',
                    'Published',
                    178,
                    29,
                    TIMESTAMPTZ '2026-05-21 09:00:00+00',
                    TRUE,
                    TIMESTAMPTZ '2026-05-21 09:00:00+00',
                    NULL
                ),
                (
                    'b1010000-0000-4000-8000-000000000005',
                    'Skin cycling cho người mới bắt đầu',
                    'skin-cycling-cho-nguoi-moi-bat-dau',
                    'Lịch luân phiên treatment và phục hồi để giảm nguy cơ quá tải da.',
                    'Skin cycling thường chia routine tối theo chu kỳ: một đêm tẩy da chết, một đêm retinoid và các đêm phục hồi. Với người mới, hãy bắt đầu chậm, dùng kem dưỡng phục hồi đầy đủ và chống nắng kỹ vào ban ngày. Nếu da đỏ rát kéo dài, nên dừng treatment và tham khảo chuyên gia.',
                    'https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?auto=format&fit=crop&w=1200&q=80',
                    'Beauty Trend',
                    'trend,skincare,retinoid',
                    'Belumi Beauty Team',
                    'Published',
                    94,
                    12,
                    TIMESTAMPTZ '2026-05-20 09:00:00+00',
                    TRUE,
                    TIMESTAMPTZ '2026-05-20 09:00:00+00',
                    NULL
                ),
                (
                    'b1010000-0000-4000-8000-000000000006',
                    'Checklist chọn serum cấp ẩm mùa hè',
                    'checklist-chon-serum-cap-am-mua-he',
                    'Những tiêu chí giúp chọn serum nhẹ mặt, đủ ẩm và dễ phối trong routine.',
                    'Một serum cấp ẩm mùa hè nên có texture mỏng, thấm nhanh và chứa các chất hút ẩm như glycerin, hyaluronic acid hoặc beta-glucan. Nếu da dầu, ưu tiên công thức không quá bí và dùng lượng vừa đủ. Serum cấp ẩm không thay thế kem dưỡng hoàn toàn, nhưng giúp routine dễ chịu hơn.',
                    'https://images.unsplash.com/photo-1620916566398-39f1143ab7be?auto=format&fit=crop&w=1200&q=80',
                    'Product Review',
                    'serum,hydration,summer',
                    'Belumi Review',
                    'Published',
                    121,
                    16,
                    TIMESTAMPTZ '2026-05-19 09:00:00+00',
                    TRUE,
                    TIMESTAMPTZ '2026-05-19 09:00:00+00',
                    NULL
                )
            ON CONFLICT ("Slug") DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "NewsArticles"
            WHERE "Slug" IN (
                'routine-phuc-hoi-da-sau-treatment',
                'niacinamide-co-hop-voi-da-dau-mun-khong',
                'makeup-nen-mong-cho-ngay-nang',
                'doc-bang-thanh-phan-my-pham-trong-3-phut',
                'skin-cycling-cho-nguoi-moi-bat-dau',
                'checklist-chon-serum-cap-am-mua-he'
            );
            """);
    }
}
