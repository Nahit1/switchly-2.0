namespace Switchly.Sdk;

public sealed class SwitchlyException : Exception
{
    public SwitchlyException(string message) : base(message) { }
    public SwitchlyException(string message, Exception inner) : base(message, inner) { }
}
