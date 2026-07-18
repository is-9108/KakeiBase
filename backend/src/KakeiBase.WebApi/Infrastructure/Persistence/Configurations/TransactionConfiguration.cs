using KakeiBase.WebApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KakeiBase.WebApi.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.UserId).HasColumnName("user_id");
        builder.Property(t => t.CategoryId).HasColumnName("category_id");
        builder.Property(t => t.SubscriptionId).HasColumnName("subscription_id");

        builder.Property(t => t.Amount)
            .HasColumnName("amount")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(t => t.Date).HasColumnName("transaction_date").IsRequired();
        builder.Property(t => t.Memo).HasColumnName("memo");
        builder.Property(t => t.ReceiptS3Key).HasColumnName("receipt_s3_key");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Subscription>()
            .WithMany()
            .HasForeignKey(t => t.SubscriptionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
