using Microsoft.EntityFrameworkCore;

namespace AigoraNet.Common.CQRS.Crud;

public record DeleteEntityCommand<T, TKey>(TKey Id) where T : class;

public static class DeleteEntityHandler
{
    public static async Task<bool> Handle<T, TKey>(DeleteEntityCommand<T, TKey> command, DefaultContext db, CancellationToken ct) where T : class
    {
        var entity = await db.Set<T>().FindAsync(new object?[] { command.Id }, ct);
        if (entity is null) return false;
        db.Set<T>().Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
