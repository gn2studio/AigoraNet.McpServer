using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace AigoraNet.Common.Extends;

public static class ExtendQueryable
{
    public static IQueryable<TSource> WhereIf<TSource>(
    [DisallowNull] this IQueryable<TSource> source,
    bool condition,
    [DisallowNull] Expression<Func<TSource, bool>> predicate)
    {
        return condition
        ? source.Where(predicate)
        : source;
    }

    public static IQueryable<TSource> WhereIfElse<TSource>(
    [DisallowNull] this IQueryable<TSource> source,
    bool condition,
    [DisallowNull] Expression<Func<TSource, bool>> predicateTrue,
    [DisallowNull] Expression<Func<TSource, bool>> predicateFalse)
    {
        return condition
            ? source.Where(predicateTrue)
            : source.Where(predicateFalse);
    }

    public static IQueryable<TSource> OrderAscIf<TSource>(
    [DisallowNull] this IQueryable<TSource> source,
    bool condition,
    [DisallowNull] Expression<Func<TSource, bool>> predicate)
    {
        return condition
        ? source.OrderBy(predicate)
        : source;
    }

    public static IQueryable<TSource> OrderDescIf<TSource>(
    [DisallowNull] this IQueryable<TSource> source,
    bool condition,
    [DisallowNull] Expression<Func<TSource, bool>> predicate)
    {
        return condition
        ? source.OrderByDescending(predicate)
        : source;
    }
}