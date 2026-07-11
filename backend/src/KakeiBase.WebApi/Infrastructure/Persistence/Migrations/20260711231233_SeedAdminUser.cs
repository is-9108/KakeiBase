using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KakeiBase.WebApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        private static readonly Guid AdminUserId = new("00000000-0000-0000-0000-000000000001");
        private const string AdminEmail = "admin@kakeibase.local";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234!");
            var now = DateTimeOffset.UtcNow;

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "email", "password_hash", "created_at", "updated_at" },
                values: new object[] { AdminUserId, AdminEmail, passwordHash, now, now });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "id",
                keyValue: AdminUserId);
        }
    }
}
