using Microsoft.EntityFrameworkCore;
using CoreBanking.Domain.Accounts;
using CoreBanking.Domain.Transaction;

namespace CoreBanking.Data;

public class AppDbContext : DbContext
{
    // Setting Tables and Data Types
    public DbSet<Account> Accounts => Set<Account>();
    
    public DbSet<Transaction> Transactions => Set<Transaction>();

    // "DbContextOptions<AppDbContext> options" - database configurations and env
    // ": base(options)" - pass options for father class constructor
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Configure how classes turn into Tables
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Automatically discover all configuration classes in this project and apply them all.
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly
        );
    }
}