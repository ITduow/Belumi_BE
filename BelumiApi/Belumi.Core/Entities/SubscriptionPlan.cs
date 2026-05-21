namespace Belumi.Core.Entities;

public sealed class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; } = "Free";
    public decimal Price { get; set; }
    public int MonthlyAiLimit { get; set; }
    public int IngredientLookupLimit { get; set; }
    public int MakeupConsultationLimit { get; set; }
    public bool CanUseAdvancedAnalysis { get; set; }
}
