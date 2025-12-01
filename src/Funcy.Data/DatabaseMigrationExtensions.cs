using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Funcy.Data;

public static class DatabaseMigrationExtensions
{
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FunctionAppDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}