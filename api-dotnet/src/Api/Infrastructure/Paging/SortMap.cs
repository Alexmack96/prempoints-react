using Api.Domain.Contracts;
using System.Linq.Expressions;

namespace Api.Infrastructure.Paging;

/// <summary>
/// A resource's sort allow-list, as data.
/// <para>
/// An allow-list rather than reflection over property names: a sort parameter
/// that maps straight onto the entity lets a caller order by unindexed columns
/// and leaks the storage model into the public contract. A leading <c>-</c>
/// means descending.
/// </para>
/// <para>
/// Generic because every collection needs exactly this and the only thing that
/// differs is the key list — the previous version was a hand-written static
/// class per resource, which would have meant seven near-identical copies.
/// </para>
/// </summary>
public sealed class SortMap<TEntity> where TEntity : IAuditableEntity
{
    private readonly Dictionary<string, Expression<Func<TEntity, object?>>> _keys;
    private readonly string _defaultKey;

    public SortMap(string defaultKey, Dictionary<string, Expression<Func<TEntity, object?>>> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultKey);

        if (!keys.ContainsKey(defaultKey))
        {
            throw new ArgumentException(
                $"Default sort key '{defaultKey}' is not in the allow-list.", nameof(defaultKey));
        }

        _keys = new Dictionary<string, Expression<Func<TEntity, object?>>>(keys, StringComparer.OrdinalIgnoreCase);
        _defaultKey = defaultKey;
    }

    /// <summary>Every accepted value, for the validation message and the OpenAPI description.</summary>
    public string Allowed =>
        string.Join(", ", _keys.Keys.OrderBy(k => k, StringComparer.Ordinal).SelectMany(k => new[] { k, $"-{k}" }));

    public bool IsValid(string? sort) =>
        string.IsNullOrWhiteSpace(sort) || _keys.ContainsKey(Key(sort));

    public IOrderedQueryable<TEntity> Apply(IQueryable<TEntity> query, string? sort)
    {
        ArgumentNullException.ThrowIfNull(query);

        var selector = _keys[Key(sort)];

        var ordered = sort?.StartsWith('-') == true
            ? query.OrderByDescending(selector)
            : query.OrderBy(selector);

        // The id tiebreaker is not optional. Without it, rows with equal sort
        // keys can land on different pages between requests, so offset paging
        // silently repeats or skips them — and every row seeded in one
        // SaveChanges shares a CreatedAtUtc, so ties are the normal case rather
        // than the exotic one.
        return ordered.ThenBy(entity => entity.Id);
    }

    private string Key(string? sort) =>
        (string.IsNullOrWhiteSpace(sort) ? _defaultKey : sort).TrimStart('-');
}
