using Funcy.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Funcy.Data;

public class FunctionAppDbContext : DbContext
{
    public FunctionAppDbContext(DbContextOptions<FunctionAppDbContext> options) : base(options)
    {
    }
    
    public DbSet<FunctionApp> FunctionApps { get; set; }
    public DbSet<Function> Functions { get; set; }
    public DbSet<FunctionAppSlot> FunctionAppSlots { get; set; }
}