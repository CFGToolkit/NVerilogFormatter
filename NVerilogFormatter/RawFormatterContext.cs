using CFGToolkit.AST;

namespace NVerilogFormatter
{
    public class RawFormatterContext
    {
        public List<RawFormatterLine> Lines { get; set; }

        public RawFormatterLine CurrentLine => Lines[Lines.Count - 1];

        public Dictionary<ISyntaxElement, int> Indents { get; set; } = new Dictionary<ISyntaxElement, int>();
    }
}
