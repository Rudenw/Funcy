using Funcy.Core.Interfaces;
using Funcy.Core.Model;
using Funcy.Data;
using Funcy.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Funcy.Infrastructure.Repositories;

public class FunctionAppRepository(IDbContextFactory<FunctionAppDbContext> dbContextFactory) : IFunctionAppRepository
{
    public async Task<FunctionAppDetails> UpsertAsync(FunctionAppDetails details, List<FunctionDetails>? functions, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await dbContext.FunctionApps
            .Include(f => f.Functions)
            .FirstOrDefaultAsync(f =>
                f.Name == details.Name &&
                f.ResourceGroup == details.ResourceGroup &&
                f.Subscription == details.Subscription, cancellationToken);

        var functionEntities = functions?.Select(f => f.MapToEntity()).ToList();

        if (existing is null)
        {
            var entity = details.MapToEntity();
            if (functionEntities is not null)
            {
                entity.Functions = functionEntities;
            }

            dbContext.FunctionApps.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return entity.Map();
        }

        existing.State = details.State;
        if (functionEntities is not null)
        {
            dbContext.Functions.RemoveRange(existing.Functions);
            existing.Functions = functionEntities;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing.Map();
    }

    public async Task RemoveAsync(FunctionAppDetails details, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await dbContext.FunctionApps.FirstOrDefaultAsync(f =>
            f.Name == details.Name &&
            f.ResourceGroup == details.ResourceGroup &&
            f.Subscription == details.Subscription, cancellationToken);

        if (entity is not null)
        {
            dbContext.FunctionApps.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<FunctionAppDetails>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var list = await dbContext.FunctionApps.Include(x => x.Functions).ToListAsync(cancellationToken);
        return list.Select(x => x.Map()).ToList();
    }
}
