using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KakeiBase.WebApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScopeUniqueCategoryIndexToNonSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_categories_user_id_name_transaction_type",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "IX_categories_user_id_name_transaction_type",
                table: "categories",
                columns: new[] { "user_id", "name", "transaction_type" },
                unique: true,
                filter: "is_system = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_categories_user_id_name_transaction_type",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "IX_categories_user_id_name_transaction_type",
                table: "categories",
                columns: new[] { "user_id", "name", "transaction_type" },
                unique: true);
        }
    }
}
