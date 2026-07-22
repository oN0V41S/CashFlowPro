using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoreBanking.Domain.Accounts;

namespace CoreBanking.Data.Configurations;

// IEntityTypeConfiguration<T> = "configurator" of entity T
public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        // Table name on PostgreSQL
        builder.ToTable("Accounts");

        // Primary Key
        builder.HasKey(a => a.Id);

        // Restrict Columns
        builder.Property(a => a.HolderName)
            .HasMaxLength(200)              // Varchar(200)
            .IsRequired();                  // NOT NULL

        builder.Property(a => a.Balance)
            .HasColumnType("decimal(18,2)") // decimal(18,2) on database
            .IsRequired();
        
        builder.Property(a => a.Type)
            .HasConversion<string>()      // Saves ENUM as text: "Checking", "Savings"
            .HasMaxLength(20);
        
        builder.Property(a => a.CreatedAt)
            .IsRequired();
    }
}

