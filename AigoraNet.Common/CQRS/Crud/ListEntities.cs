using Microsoft.EntityFrameworkCore;

namespace AigoraNet.Common.CQRS.Crud;

public record ListEntitiesQuery<T>() where T : class;

public static class ListEntitiesHandler
{
    public static async Task<IReadOnlyList<T>> Handle<T>(ListEntitiesQuery<T> query, DefaultContext db, CancellationToken ct) where T : class
    {
        return await db.Set<T>().AsNoTracking().ToListAsync(ct);
    }
}
