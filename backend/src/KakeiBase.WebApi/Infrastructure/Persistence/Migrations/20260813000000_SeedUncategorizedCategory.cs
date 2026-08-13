using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KakeiBase.WebApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedUncategorizedCategory : Migration
    {
        private static readonly Guid AdminUserId = new("00000000-0000-0000-0000-000000000001");
        private static readonly Guid UncategorizedCategoryId = new("00000000-0000-0000-0000-000000000003");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "id", "user_id", "name", "transaction_type", "is_system", "created_at" },
                values: new object[] {
                    UncategorizedCategoryId,
                    AdminUserId,
                    "未分類",
                    "Expense",
                    true,
                    DateTimeOffset.UtcNow
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: UncategorizedCategoryId);
        }
    }
}
