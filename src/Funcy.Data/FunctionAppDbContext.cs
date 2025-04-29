using Funcy.Console.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Funcy.Console.Data;

public class FunctionAppDbContext : DbContext
{
    public FunctionAppDbContext(DbContextOptions<FunctionAppDbContext> options) : base(options)
    {
    }
    
    public DbSet<FunctionApp> FunctionApps { get; set; }
    public DbSet<Function> Functions { get; set; }
}