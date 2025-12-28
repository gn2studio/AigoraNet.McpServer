using Microsoft.EntityFrameworkCore;

namespace AigoraNet.Common.CQRS.Crud;

public record UpdateEntityCommand<T>(T Entity) where T : class;

public static class UpdateEntityHandler
{
    public static async Task<T> Handle<T>(UpdateEntityCommand<T> command, DefaultContext db, CancellationToken ct) where T : class
    {
        db.Set<T>().Attach(command.Entity);
        db.Entry(command.Entity).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
        return command.Entity;
    }
}
