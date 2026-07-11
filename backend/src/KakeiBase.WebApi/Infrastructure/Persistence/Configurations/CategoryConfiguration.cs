using KakeiBase.WebApi.Domain.Entities;
using KakeiBase.WebApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KakeiBase.WebApi.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.UserId).HasColumnName("user_id");

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Type)
            .HasColumnName("transaction_type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(c => c.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(c => new { c.UserId, c.Name, c.Type }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
