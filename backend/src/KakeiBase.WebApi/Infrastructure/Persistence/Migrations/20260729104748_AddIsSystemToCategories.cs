using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KakeiBase.WebApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSystemToCategories : Migration
    {
        private static readonly Guid AdminUserId = new("00000000-0000-0000-0000-000000000001");
        private static readonly Guid SystemSubscriptionCategoryId = new("00000000-0000-0000-0000-000000000002");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_system",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // 管理者ユーザーへサブスク専用のシステムカテゴリを挿入する
            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "id", "user_id", "name", "transaction_type", "is_system", "created_at" },
                values: new object[] {
                    SystemSubscriptionCategoryId,
                    AdminUserId,
                    "サブスク",
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
                keyValue: SystemSubscriptionCategoryId);

            migrationBuilder.DropColumn(
                name: "is_system",
                table: "categories");
        }
    }
}
