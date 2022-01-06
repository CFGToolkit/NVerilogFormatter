namespace NVerilogFormatter
{
    public class HtmlFormatter : IFormatter
    {
        public Task<string> Format(string source, Func<string, Task<string>> fileProvider, Action<string> progress)
        {
            throw new NotImplementedException();
        }
    }
}
