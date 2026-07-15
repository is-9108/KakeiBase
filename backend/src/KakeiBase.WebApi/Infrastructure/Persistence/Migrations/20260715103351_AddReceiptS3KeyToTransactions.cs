using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KakeiBase.WebApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptS3KeyToTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "receipt_s3_key",
                table: "transactions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "receipt_s3_key",
                table: "transactions");
        }
    }
}
