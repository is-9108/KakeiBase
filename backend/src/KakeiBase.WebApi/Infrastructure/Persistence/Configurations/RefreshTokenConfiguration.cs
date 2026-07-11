using KakeiBase.WebApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KakeiBase.WebApi.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.UserId).HasColumnName("user_id");
        builder.Property(t => t.TokenHash).HasColumnName("token_hash").IsRequired();
        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.IsRevoked).HasColumnName("is_revoked");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.UserId).HasDatabaseName("IX_refresh_tokens_user_id");
    }
}
