using CFGToolkit.AST;
using CFGToolkit.AST.Algorithms.TreeVisitors;
using CFGToolkit.AST.Providers;

namespace NVerilogFormatter
{
    public class RawFormatter : IFormatter
    {
        public RawFormatter(List<Func<RawFormatterContext, ISyntaxElement, bool>> beforeActions, List<Func<RawFormatterContext, ISyntaxElement, bool>> afterActions)
        {
            BeforeActions = beforeActions;
            AfterActions = afterActions;
        }

        public List<Func<RawFormatterContext, ISyntaxElement, bool>> BeforeActions { get; }

        public List<Func<RawFormatterContext, ISyntaxElement, bool>> AfterActions { get; }

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
                        Lines = new List<RawFormatterLine>() { new RawFormatterLine() }
                    };

                    // Set parents 
                    var algorithm = new SetParentsVisitor();
                    algorithm.Visit(result);

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




                    return String.Join(Environment.NewLine, context.Lines.Select(line => line.ToString()));
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
