namespace Switchly.Sdk.Internal;

internal sealed class RulesetCache
{
    private volatile Ruleset? _current;

    public Ruleset? Current => _current;

    public bool IsReady => _current is not null;

    public void Set(Ruleset ruleset) => _current = ruleset;
}
