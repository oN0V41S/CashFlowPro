using CoreBanking.Domain.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreBanking.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("User");
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.AccountId)
            .IsRequired();

        builder.HasIndex(u => u.AccountId)
            .IsUnique();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(u => u.Email)
            .IsUnique();
            
        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(u => u.CreatedAt)
            .IsRequired();
    }
}
