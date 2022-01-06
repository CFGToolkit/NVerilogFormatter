using CFGToolkit.AST;
using CFGToolkit.AST.Providers;

namespace NVerilogFormatter
{
    public class RawFormatterFactory
    {
        public const int IdentLevel = 4;

        public static RawFormatter Create()
        {
            var beforeActions = new List<Action<RawFormatterContext, ISyntaxElement>>();
            beforeActions.Add((context, element) =>
            {
                if (element is SyntaxNode node)
                {
                    if (node.Name == "statement" && node.Parent.Name == "always_construct")
                    {
                        context.CurrentLevel += IdentLevel;
                    }

                    if (node.Name == "number")
                    {
                        context.DisableSpaces = true;
                    }
                    else if (node.Name == "nature_declaration")
                    {
                        context.CurrentLevel += IdentLevel;
                        context.IdentNode = node;
                    }
                    else if (node.Name == "discipline_declaration")
                    {
                        context.CurrentLevel += IdentLevel;
                        context.IdentNode = node;
                    }
                    else if (node.Name == "module_declaration")
                    {
                        context.CurrentLevel += IdentLevel;
                        context.IdentNode = node;
                    }
                    else if (node.Name == "wait_statement")
                    {
                        context.CurrentLevel += IdentLevel;
                    }
                }
            });
            beforeActions.Add((context, element) =>
            {
                if (element is SyntaxToken token)
                {
                    if (token.Value == "always")
                    {
                        context.Lines.Add(Environment.NewLine);
                        context.Lines.Add("");
                    }
                    if (token.Value == "endmodule")
                    {
                        context.Lines.Add(Environment.NewLine);
                    }
                    if (token.Value == "begin")
                    {
                        if (!string.IsNullOrWhiteSpace(context.Lines[context.Lines.Count - 1]))
                        {
                            context.Lines.Add(Environment.NewLine);
                            context.Lines.Add("");
                        }

                        context.CurrentLevel += IdentLevel;

                        for (var i = 0; i < context.CurrentLevel; i++)
                        {
                            context.Lines[context.Lines.Count - 1] += " ";
                        }
                    }
                    else if (token.Value == "end")
                    {
                        context.Lines.Add("");

                        context.CurrentLevel -= IdentLevel;

                        for (var i = 0; i < context.CurrentLevel; i++)
                        {
                            context.Lines[context.Lines.Count - 1] += " ";
                        }
                    }
                    else if (context.Lines[context.Lines.Count - 1] == "")
                    {
                        if (context.IdentNode?.GetFirstToken() == token
                        || context.IdentNode?.GetTokens().LastOrDefault() == token)
                        {
                            return;
                        }

                        for (var i = 0; i < context.CurrentLevel; i++)
                        {
                            context.Lines[context.Lines.Count - 1] += " ";
                        }
                    }
                    else
                    {
                        if (token.Value == ","
                            || token.Value == ":"
                            || token.Value == "!"
                            || token.Value == "["
                            || token.Value == "]"
                            || token.Value == "="
                            || token.Value == ";"
                            || token.Value == "("
                            || token.Value == ")")
                        {
                            return;
                        }

                        if (context.Lines[context.Lines.Count - 1].EndsWith("(")
                            || context.Lines[context.Lines.Count - 1].EndsWith("!"))
                        {
                            return;
                        }


                        if (!context.DisableSpaces && !context.Lines[context.Lines.Count - 1].EndsWith(" "))
                        {
                            context.Lines[context.Lines.Count - 1] += " ";
                        }
                    }
                }
            });

            var afterActions = new List<Action<RawFormatterContext, ISyntaxElement>>();

            afterActions.Add((context, element) =>
            {
                if (element is SyntaxNode node)
                {
                    if (node.Name == "statement" && node.Parent.Name == "always_construct")
                    {
                        context.CurrentLevel -= IdentLevel;
                    }

                    if (node.Name == "number")
                    {
                        context.DisableSpaces = false;
                    }
                    else if (node.Name == "nature_declaration")
                    {
                        context.CurrentLevel -= IdentLevel;
                    }
                    else if (node.Name == "discipline_declaration")
                    {
                        context.CurrentLevel -= IdentLevel;
                    }
                    else if (node.Name == "module_declaration")
                    {
                        context.CurrentLevel -= IdentLevel;
                    }
                    else if (node.Name == "analog_construct")
                    {
                        context.CurrentLevel -= IdentLevel;
                    }

                    if (node.Name == "event_control")
                    {
                        context.Lines.Add(Environment.NewLine);
                        context.Lines.Add("");
                    }

                    if (node.Name == "analog_event_control")
                    {
                        context.Lines.Add(Environment.NewLine);
                        context.Lines.Add("");
                    }
                }
            });

            afterActions.Add((context, element) =>
            {
                if (element is SyntaxToken t2)
                {
                    context.Lines[context.Lines.Count - 1] += t2.Value;

                    if (t2.Value == ";")
                    {
                        context.Lines.Add(Environment.NewLine);
                        context.Lines.Add("");
                    }

                    if (t2.Value == "enddiscipline" || t2.Value == "endnature" || t2.Value == "endmodule")
                    {
                        context.Lines.Add(Environment.NewLine);
                        context.Lines.Add(Environment.NewLine);
                        context.Lines.Add("");
                    }

                    if (t2.Value == "end")
                    {
                        context.Lines.Add(Environment.NewLine);
                        context.Lines.Add("");
                        context.CurrentLevel -= IdentLevel;
                    }

                    if (t2.Value == "begin")
                    {
                        context.Lines.Add(Environment.NewLine);
                        context.Lines.Add("");
                        context.CurrentLevel += IdentLevel;
                    }

                    if (t2.Value == "analog")
                    {
                        context.Lines.Add(Environment.NewLine);
                        context.Lines.Add("");
                        context.CurrentLevel += IdentLevel;
                    }
                }
            });

            return new RawFormatter(beforeActions, afterActions);
        }
    }
}
