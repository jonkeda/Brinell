namespace Bruinell.TestData;

/// <summary>
/// The rules a loaded test set must follow, beyond its shape.
/// </summary>
/// <remarks>
/// A rule names the JSON path of the collection it checks (<c>server</c>), so a failure points at
/// the element's line (<c>server[2]</c>).
/// </remarks>
/// <typeparam name="TSet">The type one test-set file is read into.</typeparam>
public sealed class TestDataRules<TSet>
{
    private readonly List<Action<TSet, TestDataRuleContext>> _rules = [];

    internal IReadOnlyList<Action<TSet, TestDataRuleContext>> All => _rules;

    /// <summary>A rule over the whole test set.</summary>
    public TestDataRules<TSet> Check(Action<TSet, TestDataRuleContext> rule)
    {
        _rules.Add(rule);
        return this;
    }

    /// <summary>Checks every item; <paramref name="problem"/> returns an error message, or <c>null</c> when the item is fine.</summary>
    public TestDataRules<TSet> ForEach<TItem>(string path, Func<TSet, IEnumerable<TItem>> items, Func<TItem, string?> problem)
        => ForEach(path, items, (_, item) => problem(item));

    /// <summary>Checks every item with the whole test set at hand, e.g. to compare it with another collection.</summary>
    public TestDataRules<TSet> ForEach<TItem>(string path, Func<TSet, IEnumerable<TItem>> items, Func<TSet, TItem, string?> problem)
        => Check((set, context) =>
        {
            var index = 0;
            foreach (var item in items(set))
            {
                if (problem(set, item) is { } message)
                {
                    context.Error($"{path}[{index}]", message);
                }

                index++;
            }
        });

    /// <summary>Every item must satisfy <paramref name="predicate"/>.</summary>
    public TestDataRules<TSet> Require<TItem>(string path, Func<TSet, IEnumerable<TItem>> items, Func<TItem, bool> predicate, string message)
        => ForEach(path, items, item => predicate(item) ? null : message);

    /// <summary>No two items may share a key.</summary>
    public TestDataRules<TSet> UniqueIds<TItem, TKey>(string path, Func<TSet, IEnumerable<TItem>> items, Func<TItem, TKey> key)
        where TKey : notnull
        => Check((set, context) =>
        {
            var seen = new Dictionary<TKey, int>();
            var index = 0;
            foreach (var item in items(set))
            {
                var id = key(item);
                if (seen.TryGetValue(id, out var first))
                {
                    context.Error($"{path}[{index}]", $"id {id} is already used by {path}[{first}].", TestDataCodes.DuplicateId);
                }
                else
                {
                    seen[id] = index;
                }

                index++;
            }
        });
}

/// <summary>Where a rule reports what it finds.</summary>
public sealed class TestDataRuleContext
{
    private readonly Action<string, string, string> _report;

    internal TestDataRuleContext(Action<string, string, string> report) => _report = report;

    /// <summary>Reports an error at the JSON path <paramref name="path"/>, e.g. <c>local[1]</c>.</summary>
    public void Error(string path, string message, string code = TestDataCodes.Rule) => _report(path, message, code);
}
