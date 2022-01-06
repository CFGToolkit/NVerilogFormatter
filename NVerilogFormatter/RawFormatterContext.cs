using CFGToolkit.AST;

namespace NVerilogFormatter
{
    public class RawFormatterContext
    {
        public int CurrentLevel { get; set; }

        public List<string> Lines { get; set; }

        public SyntaxNode IdentNode { get; set; }

        public bool DisableSpaces { get; set; }
    }
}
