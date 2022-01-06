namespace NVerilogFormatter
{
    public interface IFormatter
    {
        Task<string> Format(string source, Func<string, Task<string>> fileProvider, Action<string> progress);
    }
}
