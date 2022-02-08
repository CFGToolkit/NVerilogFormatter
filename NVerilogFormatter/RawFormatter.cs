using CFGToolkit.AST;
using CFGToolkit.AST.Visitors;
using CFGToolkit.AST.Visitors.Traversals;

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

                    var vistor = new PrePostOrderTreeTraversal<bool, bool>(new ActionExecutorVisitor(context, BeforeActions), new ActionExecutorVisitor(context, AfterActions));
                    vistor.Accept(result, new TreeTraversalContext());

                    return String.Join(Environment.NewLine, context.Lines.Select(line => line.ToString()));
                }

                return "Problem wit formatting";
            }
            else
            {

                return "Failed to format";
            }
        }

        public class ActionExecutorVisitor : IVisitor<ISyntaxElement, TreeTraversalContext, bool>
        {
            public ActionExecutorVisitor(RawFormatterContext formatterContext, List<Func<RawFormatterContext, ISyntaxElement, bool>> actions)
            {
                FormatterContext = formatterContext;
                Actions = actions;
            }

            public RawFormatterContext FormatterContext { get; }

            public List<Func<RawFormatterContext, ISyntaxElement, bool>> Actions { get; }

            public bool Visit(ISyntaxElement element, TreeTraversalContext context)
            {
                foreach (var action in Actions)
                {
                    action(FormatterContext, element);
                }
                return true;
            }
        }
    }
}
