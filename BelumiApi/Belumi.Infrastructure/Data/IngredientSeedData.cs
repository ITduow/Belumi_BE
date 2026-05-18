using Belumi.Core.Entities;

namespace Belumi.Infrastructure.Data;

public static class IngredientSeedData
{
    public static List<Ingredient> Generate()
    {
        var ingredients = new List<Ingredient>();

        // 50 base ingredients with real data
        var baseData = new (string Name, string Inci, string Desc, string Skin, string Benefits, string? Concerns, string Safety)[]
        {
            ("Niacinamide","Niacinamide","Vitamin B3, thu nhỏ lỗ chân lông, cân bằng dầu","Oily,Combination,All","Thu nhỏ lỗ chân lông, kiểm soát dầu, làm sáng da","Nồng độ >10% có thể gây đỏ","Safe"),
            ("Hyaluronic Acid","Sodium Hyaluronate","Giữ nước gấp 1000 lần trọng lượng","All,Dry,Dehydrated","Cấp ẩm sâu, da mềm mịn",null,"Safe"),
            ("Retinol","Retinol","Dẫn xuất Vitamin A, tái tạo tế bào","Normal,Combination,Aging","Chống lão hóa, giảm nếp nhăn","Gây kích ứng, không dùng khi mang thai","Caution"),
            ("Vitamin C","Ascorbic Acid","Chống oxy hóa mạnh, ức chế melanin","Dull,Hyperpigmentation,All","Làm sáng da, mờ thâm nám","Dễ bị oxy hóa","Safe"),
            ("Ceramide NP","Ceramide NP","Lipid tự nhiên bảo vệ hàng rào da","Dry,Sensitive,Eczema","Phục hồi hàng rào da, dưỡng ẩm sâu",null,"Safe"),
            ("Salicylic Acid","Salicylic Acid","BHA tan trong dầu, làm sạch lỗ chân lông","Oily,Acne-prone","Trị mụn đầu đen, thông thoáng lỗ chân lông","Không dùng cho da khô","Caution"),
            ("Glycolic Acid","Glycolic Acid","AHA phân tử nhỏ nhất, tẩy tế bào chết","Normal,Combination,Aging","Tẩy da chết, làm mờ thâm","Tăng nhạy cảm với nắng","Caution"),
            ("Lactic Acid","Lactic Acid","AHA dịu nhẹ, phù hợp da nhạy cảm","Sensitive,Dry,Normal","Tẩy da chết nhẹ nhàng, dưỡng ẩm",null,"Safe"),
            ("Azelaic Acid","Azelaic Acid","Acid từ ngũ cốc, trị mụn và mờ thâm","Acne-prone,Rosacea,Sensitive","Trị mụn, mờ thâm, giảm đỏ",null,"Safe"),
            ("Squalane","Squalane","Dầu nhẹ từ ô liu, tương thích cao với da","All,Dry,Oily","Dưỡng ẩm không nhờn, chống oxy hóa",null,"Safe"),
            ("Centella Asiatica","Centella Asiatica Extract","Rau má làm dịu và phục hồi da","Sensitive,Acne-prone,All","Làm dịu kích ứng, phục hồi da",null,"Safe"),
            ("Snail Mucin","Snail Secretion Filtrate","Dịch nhầy ốc sên chứa glycoprotein","All,Acne-prone,Aging","Phục hồi da, cấp ẩm, mờ sẹo","Dị ứng động vật thân mềm","Safe"),
            ("Panthenol","Panthenol","Provitamin B5, dưỡng ẩm dịu nhẹ","All,Sensitive,Dry","Dưỡng ẩm, làm dịu, phục hồi",null,"Safe"),
            ("Allantoin","Allantoin","Hợp chất từ cây Comfrey, làm mềm da","Sensitive,Dry,All","Làm dịu, làm mềm da",null,"Safe"),
            ("Peptide","Palmitoyl Tripeptide-1","Chuỗi amino acid kích thích collagen","Aging,Normal,Dry","Chống lão hóa, tăng sinh collagen",null,"Safe"),
            ("Benzoyl Peroxide","Benzoyl Peroxide","Diệt khuẩn P.acnes gây mụn","Acne-prone,Oily","Diệt khuẩn, trị mụn viêm","Gây khô, bong tróc","Caution"),
            ("Tranexamic Acid","Tranexamic Acid","Ức chế melanin hiệu quả và an toàn","Hyperpigmentation,Melasma,All","Mờ thâm nám, làm đều màu da",null,"Safe"),
            ("Alpha Arbutin","Alpha-Arbutin","Ức chế tyrosinase giảm sản xuất melanin","All,Hyperpigmentation","Làm sáng da, mờ thâm",null,"Safe"),
            ("Kojic Acid","Kojic Acid","Acid tự nhiên từ nấm","Hyperpigmentation,Dull","Làm sáng da, mờ thâm nám","Kích ứng da nhạy cảm","Caution"),
            ("Resveratrol","Resveratrol","Chống oxy hóa mạnh từ vỏ nho","All,Aging","Chống oxy hóa, bảo vệ DNA da",null,"Safe"),
            ("Bakuchiol","Bakuchiol","Thay thế Retinol từ thực vật","Sensitive,All,Aging","Chống lão hóa, an toàn cho bà bầu",null,"Safe"),
            ("Zinc Oxide","Zinc Oxide","Khoáng chất chống nắng vật lý","Sensitive,Acne-prone,All","Chống nắng vật lý, kiểm soát dầu","Vệt trắng trên da tối màu","Safe"),
            ("Adenosine","Adenosine","Kích thích collagen và giảm viêm","All,Aging,Sensitive","Chống lão hóa, làm dịu",null,"Safe"),
            ("Caffeine","Caffeine","Co mạch máu, giảm sưng phù","All","Giảm thâm mắt, chống sưng phù",null,"Safe"),
            ("Tea Tree Oil","Melaleuca Alternifolia Leaf Oil","Tinh dầu tràm trà kháng khuẩn","Oily,Acne-prone","Kháng khuẩn, trị mụn","Cần pha loãng","Caution"),
            ("Aloe Vera","Aloe Barbadensis Leaf Extract","Nha đam làm dịu và phục hồi","All,Sensitive","Làm dịu, dưỡng ẩm, phục hồi",null,"Safe"),
            ("Ferulic Acid","Ferulic Acid","Chống oxy hóa, tăng hiệu quả Vit C","All,Aging","Chống oxy hóa, tăng cường Vitamin C",null,"Safe"),
            ("Green Tea Extract","Camellia Sinensis Leaf Extract","Chiết xuất trà xanh giàu EGCG","All,Oily,Sensitive","Chống oxy hóa, kiểm soát dầu",null,"Safe"),
            ("Licorice Root","Glycyrrhiza Glabra Root Extract","Cam thảo chứa Glabridin","Hyperpigmentation,Sensitive","Làm sáng da, mờ thâm",null,"Safe"),
            ("Collagen","Hydrolyzed Collagen","Protein cấu trúc chính của da","All,Aging,Dry","Tăng đàn hồi, dưỡng ẩm","Hiệu quả chủ yếu ở bề mặt","Safe"),
            ("Glycerin","Glycerin","Humectant phổ biến nhất, giữ nước","All","Dưỡng ẩm, làm mềm da",null,"Safe"),
            ("Tocopherol","Tocopherol","Vitamin E, chống oxy hóa","All,Dry","Chống oxy hóa, dưỡng ẩm",null,"Safe"),
            ("Jojoba Oil","Simmondsia Chinensis Seed Oil","Dầu jojoba giống bã nhờn tự nhiên","All,Dry,Oily","Dưỡng ẩm, cân bằng dầu",null,"Safe"),
            ("Rosehip Oil","Rosa Canina Fruit Oil","Dầu tầm xuân giàu vitamin A, C","Dry,Aging,Normal","Mờ sẹo, chống lão hóa",null,"Safe"),
            ("Shea Butter","Butyrospermum Parkii Butter","Bơ hạt mỡ dưỡng ẩm đậm đặc","Dry,Normal","Dưỡng ẩm sâu, làm mềm da",null,"Safe"),
            ("Madecassoside","Madecassoside","Hoạt chất chính từ rau má","Sensitive,Acne-prone","Chống viêm, phục hồi da",null,"Safe"),
            ("Mugwort","Artemisia Princeps Extract","Chiết xuất ngải cứu Hàn Quốc","Sensitive,Acne-prone","Làm dịu, kháng viêm",null,"Safe"),
            ("Propolis","Propolis Extract","Keo ong kháng khuẩn tự nhiên","Acne-prone,Sensitive","Kháng khuẩn, dưỡng ẩm, làm dịu",null,"Safe"),
            ("Rice Bran","Oryza Sativa Bran Extract","Chiết xuất cám gạo Nhật Bản","All,Dull","Làm sáng da, dưỡng ẩm",null,"Safe"),
            ("Turmeric","Curcuma Longa Root Extract","Chiết xuất nghệ chống viêm","All,Acne-prone","Chống viêm, làm sáng da","Có thể nhuộm vàng da","Safe"),
            ("Witch Hazel","Hamamelis Virginiana Extract","Nước cây phỉ se khít lỗ chân lông","Oily,Combination","Se khít lỗ chân lông, kiểm soát dầu","Có thể gây khô","Caution"),
            ("Mandelic Acid","Mandelic Acid","AHA phân tử lớn, dịu nhẹ nhất","Sensitive,Acne-prone","Tẩy da chết nhẹ, trị mụn",null,"Safe"),
            ("Urea","Urea","Humectant và keratolytic tự nhiên","Dry,Eczema","Dưỡng ẩm sâu, tẩy da chết nhẹ",null,"Safe"),
            ("Saccharomyces","Saccharomyces Ferment Filtrate","Men bia lên men, nền tảng essence","All","Dưỡng ẩm, tăng cường hàng rào da",null,"Safe"),
            ("Vitamin E","dl-Alpha-Tocopheryl Acetate","Vitamin E dạng ổn định","All,Dry","Chống oxy hóa, dưỡng ẩm",null,"Safe"),
            ("Chamomile","Chamomilla Recutita Flower Extract","Chiết xuất hoa cúc La Mã","Sensitive,All","Làm dịu, chống viêm",null,"Safe"),
            ("Argan Oil","Argania Spinosa Kernel Oil","Dầu Argan từ Morocco","Dry,Normal","Dưỡng ẩm, chống lão hóa",null,"Safe"),
            ("BHA","Beta Hydroxy Acid","Nhóm acid tan trong dầu","Oily,Acne-prone","Thông thoáng lỗ chân lông","Không dùng quá nhiều","Caution"),
            ("PHA","Polyhydroxy Acid","Acid thế hệ mới dịu nhẹ nhất","Sensitive,All","Tẩy da chết siêu nhẹ, dưỡng ẩm",null,"Safe"),
            ("Titanium Dioxide","Titanium Dioxide","Khoáng chất chống nắng vật lý","Sensitive,All","Chống nắng vật lý","Vệt trắng nhẹ","Safe"),
        };

        // Add 50 base ingredients
        foreach (var b in baseData)
        {
            ingredients.Add(new Ingredient
            {
                Name = b.Name, InciName = b.Inci, Description = b.Desc,
                SkinTypes = b.Skin, Benefits = b.Benefits, Concerns = b.Concerns, SafetyRating = b.Safety
            });
        }

        // Generate 450 more variants (concentration variants, combinations, formulation types)
        var concentrations = new[] { "0.5%", "1%", "2%", "3%", "5%", "10%", "15%", "20%", "25%", "30%" };
        var formTypes = new[] { "Serum", "Cream", "Toner", "Essence", "Ampoule", "Gel", "Oil", "Mask", "Emulsion" };
        var skinTypeOptions = new[] { "All", "Oily", "Dry", "Sensitive", "Combination", "Acne-prone", "Aging", "Normal", "Dehydrated" };
        var safetyOptions = new[] { "Safe", "Safe", "Safe", "Safe", "Caution" }; // 80% Safe

        var rng = new Random(42); // fixed seed for reproducibility
        var idx = 0;

        foreach (var b in baseData)
        {
            for (var c = 0; c < 9 && ingredients.Count < 500; c++)
            {
                var conc = concentrations[rng.Next(concentrations.Length)];
                var form = formTypes[idx % formTypes.Length];
                var skin1 = skinTypeOptions[rng.Next(skinTypeOptions.Length)];
                var skin2 = skinTypeOptions[rng.Next(skinTypeOptions.Length)];
                var safety = safetyOptions[rng.Next(safetyOptions.Length)];

                ingredients.Add(new Ingredient
                {
                    Name = $"{b.Name} {conc} ({form})",
                    InciName = b.Inci,
                    Description = $"{b.Desc} - Dạng {form.ToLower()} nồng độ {conc}.",
                    SkinTypes = $"{skin1},{skin2}",
                    Benefits = b.Benefits,
                    Concerns = b.Concerns,
                    SafetyRating = safety
                });
                idx++;
            }
        }

        return ingredients.Take(500).ToList();
    }
}
