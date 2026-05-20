using Microsoft.EntityFrameworkCore;

namespace Belumi.Infrastructure.Data;

public static class BelumiSchemaBootstrapper
{
    public static async Task EnsureSchemaAsync(BelumiDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);

        var sql = """
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "FirebaseUid" text;
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "SubscriptionPlan" text NOT NULL DEFAULT 'Free';
            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;

            ALTER TABLE "Products" ADD COLUMN IF NOT EXISTS "Brand" text NOT NULL DEFAULT 'Belumi';
            ALTER TABLE "Products" ADD COLUMN IF NOT EXISTS "ImageUrl" text;
            ALTER TABLE "Products" ADD COLUMN IF NOT EXISTS "SuitableSkinTypes" text;

            ALTER TABLE "BlogPosts" ADD COLUMN IF NOT EXISTS "Slug" text NOT NULL DEFAULT '';
            ALTER TABLE "BlogPosts" ADD COLUMN IF NOT EXISTS "Summary" text NOT NULL DEFAULT '';
            ALTER TABLE "BlogPosts" ADD COLUMN IF NOT EXISTS "Category" text NOT NULL DEFAULT 'Skincare';

            ALTER TABLE "SkinAnalyses" ADD COLUMN IF NOT EXISTS "AgeRange" text;
            ALTER TABLE "SkinAnalyses" ADD COLUMN IF NOT EXISTS "SensitivityLevel" text;
            ALTER TABLE "SkinAnalyses" ADD COLUMN IF NOT EXISTS "UserNote" text;
            ALTER TABLE "SkinAnalyses" ADD COLUMN IF NOT EXISTS "AiResult" text;
            ALTER TABLE "SkinAnalyses" ADD COLUMN IF NOT EXISTS "MorningRoutine" text;
            ALTER TABLE "SkinAnalyses" ADD COLUMN IF NOT EXISTS "NightRoutine" text;
            ALTER TABLE "SkinAnalyses" ADD COLUMN IF NOT EXISTS "RecommendedIngredients" text;
            ALTER TABLE "SkinAnalyses" ADD COLUMN IF NOT EXISTS "AvoidIngredients" text;

            CREATE TABLE IF NOT EXISTS "IngredientLookups" (
                "Id" uuid PRIMARY KEY,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone,
                "UserId" uuid NOT NULL,
                "InputText" text NOT NULL,
                "ImageUrl" text,
                "OcrText" text,
                "AiResult" text NOT NULL,
                "SafetyScore" integer NOT NULL,
                "SuitableSkinTypes" text NOT NULL,
                "WarningNotes" text NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "MakeupConsultations" (
                "Id" uuid PRIMARY KEY,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone,
                "UserId" uuid NOT NULL,
                "SkinTone" text NOT NULL,
                "Occasion" text NOT NULL,
                "StylePreference" text NOT NULL,
                "Note" text,
                "AiResult" text NOT NULL,
                "LipColorSuggestion" text NOT NULL,
                "FoundationSuggestion" text NOT NULL,
                "EyeMakeupSuggestion" text NOT NULL,
                "BlushSuggestion" text NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "SubscriptionPlans" (
                "Id" uuid PRIMARY KEY,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone,
                "Name" text NOT NULL,
                "Price" numeric(18,2) NOT NULL,
                "MonthlyAiLimit" integer NOT NULL,
                "IngredientLookupLimit" integer NOT NULL,
                "MakeupConsultationLimit" integer NOT NULL,
                "CanUseAdvancedAnalysis" boolean NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "UserSubscriptions" (
                "Id" uuid PRIMARY KEY,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone,
                "UserId" uuid NOT NULL,
                "PlanId" uuid NOT NULL,
                "Status" text NOT NULL,
                "StartDate" timestamp with time zone NOT NULL,
                "EndDate" timestamp with time zone,
                "PaymentStatus" text NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "AiUsageLogs" (
                "Id" uuid PRIMARY KEY,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone,
                "UserId" uuid NOT NULL,
                "FeatureName" text NOT NULL,
                "TokenUsed" integer NOT NULL,
                "RequestData" text NOT NULL,
                "ResponseData" text NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "Payments" (
                "Id" uuid PRIMARY KEY,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone,
                "UserId" uuid NOT NULL,
                "PlanId" uuid NOT NULL,
                "Amount" numeric(18,2) NOT NULL,
                "Currency" text NOT NULL,
                "PaymentMethod" text NOT NULL,
                "PaymentStatus" text NOT NULL,
                "TransactionCode" text NOT NULL
            );
            """;

        await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
