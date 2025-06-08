using Funcy.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Funcy.Data;

public class FunctionAppDbContext : DbContext
{
    public FunctionAppDbContext(DbContextOptions<FunctionAppDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FunctionApp>()
            .HasMany(f => f.Functions)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FunctionApp>()
            .HasMany(f => f.Slots)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<Function>()
            .HasOne(f => f.FunctionApp)
            .WithMany(fa => fa.Functions)
            .HasForeignKey(f => f.FunctionAppId);

        modelBuilder.Entity<FunctionAppSlot>()
            .HasOne(s => s.FunctionApp)
            .WithMany(fa => fa.Slots)
            .HasForeignKey(s => s.FunctionAppId);
    }
    
    public DbSet<FunctionApp> FunctionApps { get; set; }
    public DbSet<Function> Functions { get; set; }
    public DbSet<FunctionAppSlot> FunctionAppSlots { get; set; }
}