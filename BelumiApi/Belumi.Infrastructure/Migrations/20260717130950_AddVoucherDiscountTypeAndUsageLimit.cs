using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Belumi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherDiscountTypeAndUsageLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DiscountAmount",
                table: "Vouchers",
                newName: "DiscountValue");

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "Vouchers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsageLimit",
                table: "Vouchers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "UsageLimit",
                table: "Vouchers");

            migrationBuilder.RenameColumn(
                name: "DiscountValue",
                table: "Vouchers",
                newName: "DiscountAmount");
        }
    }
}
