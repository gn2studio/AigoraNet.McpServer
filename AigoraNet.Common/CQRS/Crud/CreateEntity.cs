using Microsoft.EntityFrameworkCore;

namespace AigoraNet.Common.CQRS.Crud;

public record CreateEntityCommand<T>(T Entity) where T : class;

public static class CreateEntityHandler
{
    public static async Task<T> Handle<T>(CreateEntityCommand<T> command, DefaultContext db, CancellationToken ct) where T : class
    {
        await db.Set<T>().AddAsync(command.Entity, ct);
        await db.SaveChangesAsync(ct);
        return command.Entity;
    }
}
