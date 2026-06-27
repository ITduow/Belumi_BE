using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belumi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBeautyProfileQuizFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgeGroup",
                table: "BeautyProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvoidedIngredients",
                table: "BeautyProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BudgetRange",
                table: "BeautyProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentProducts",
                table: "BeautyProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "BeautyProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nickname",
                table: "BeautyProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QuizCompletedAt",
                table: "BeautyProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkinGoals",
                table: "BeautyProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkinSensitivity",
                table: "BeautyProfiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgeGroup",
                table: "BeautyProfiles");

            migrationBuilder.DropColumn(
                name: "AvoidedIngredients",
                table: "BeautyProfiles");

            migrationBuilder.DropColumn(
                name: "BudgetRange",
                table: "BeautyProfiles");

            migrationBuilder.DropColumn(
                name: "CurrentProducts",
                table: "BeautyProfiles");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "BeautyProfiles");

            migrationBuilder.DropColumn(
                name: "Nickname",
                table: "BeautyProfiles");

            migrationBuilder.DropColumn(
                name: "QuizCompletedAt",
                table: "BeautyProfiles");

            migrationBuilder.DropColumn(
                name: "SkinGoals",
                table: "BeautyProfiles");

            migrationBuilder.DropColumn(
                name: "SkinSensitivity",
                table: "BeautyProfiles");
        }
    }
}
