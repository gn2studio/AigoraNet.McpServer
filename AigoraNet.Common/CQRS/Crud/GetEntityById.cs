using Microsoft.EntityFrameworkCore;

namespace AigoraNet.Common.CQRS.Crud;

public record GetEntityByIdQuery<T, TKey>(TKey Id) where T : class;

public static class GetEntityByIdHandler
{
    public static async Task<T?> Handle<T, TKey>(GetEntityByIdQuery<T, TKey> query, DefaultContext db, CancellationToken ct) where T : class
    {
        return await db.Set<T>().FindAsync(new object?[] { query.Id }, ct);
    }
}
