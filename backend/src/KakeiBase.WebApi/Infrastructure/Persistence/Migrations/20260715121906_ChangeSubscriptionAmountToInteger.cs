using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KakeiBase.WebApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSubscriptionAmountToInteger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "amount",
                table: "subscriptions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "subscriptions",
                type: "numeric(12,0)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
