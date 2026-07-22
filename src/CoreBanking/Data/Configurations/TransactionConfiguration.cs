using CoreBanking.Domain.Accounts;
using CoreBanking.Domain.Transaction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreBanking.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.Description)
            .HasMaxLength(500);                  // VARCHAR(500)

        builder.Property(t => t.Amount)
            .HasColumnType("decimal(18,2)")           // NUMERIC(18,2) on database
            .IsRequired();                       // NOT NULL

        builder.Property(t => t.Type)
            .HasConversion<string>() 
            .HasMaxLength(20);

        // Relationship: Transaction -> Account
        builder.HasOne<Account>()                // The transaction belongs to an account
            .WithMany()                          // Account has many Transactions
            .HasForeignKey(t => t.AccountId)     // Foreign Key
            .OnDelete(DeleteBehavior.Restrict);  // Don't delete account if has transactions
    }
}