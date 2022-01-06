using CFGToolkit.AST;

namespace NVerilogFormatter
{
    public class RawFormatter : IFormatter
    {
        public RawFormatter(List<Action<RawFormatterContext, ISyntaxElement>> beforeActions, List<Action<RawFormatterContext, ISyntaxElement>> afterActions)
        {
            BeforeActions = beforeActions;
            AfterActions = afterActions;
        }

        public List<Action<RawFormatterContext, ISyntaxElement>> BeforeActions { get; }

        public List<Action<RawFormatterContext, ISyntaxElement>> AfterActions { get; }

        public async Task<string> Format(string source, Func<string, Task<string>> fileProvider, Action<string> progress)
        {
            var parser = new NVerilogParser.VerilogParser(fileProvider);
            var results = await parser.TryParse(source, ((int pos, int count) arg) => {
                progress(arg.pos + "/" + arg.count);
            });

            if (results.WasSuccessful && results.Values.Count == 1)
            {
                var result = results.Values[0].Value as CFGToolkit.AST.SyntaxNode;

                if (result != null)
                {
                    var context = new RawFormatterContext()
                    {
                        CurrentLevel = 0,
                        DisableSpaces = false,
                        IdentNode = null,
                        Lines = new List<string> { "" }
                    };

                    var before = (ISyntaxElement e) => {
                        foreach (var beforeAction in BeforeActions)
                        {
                            beforeAction(context, e);
                        }
                        return true;
                    };

                    var after = (ISyntaxElement e) => {
                        foreach (var afterAction in AfterActions)
                        {
                            afterAction(context, e);
                        }
                    };

                    var vistor = new CFGToolkit.AST.Algorithms.TreeVisitors.PreAndPostTreeVistor(before, after);
                    vistor.Visit(result);

                    return String.Join(string.Empty, context.Lines);
                }

                return "Problem wit formatting";
            }
            else
            {

                return "Failed to format";
            }
        }
    }
}
