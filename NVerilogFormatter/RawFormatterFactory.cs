using CFGToolkit.AST;
using CFGToolkit.AST.Providers;

namespace NVerilogFormatter
{
    public class RawFormatterFactory
    {
        public const int IdentStep = 4;

        public static RawFormatter Create()
        {
            string[] emptyLineBefore = new string[]
              {
                "always_construct",
                "initial_construct",
                "analog_construct",
                "if_generate_construct",
                "loop_generate_construct"
              };

            string[] emptyLinesAfterNodes = new string[]
            {
                "nature_declaration",
                "discipline_declaration",
                "module_declaration",
            };

            string[] lineBreakAfterNodes = new string[]
            {
                "analog_event_control",
                "event_control",
                "analog_construct",
                "seq_block",
                "analog_function_seq_block",
                "analog_event_seq_block",
                "analog_seq_block",
                "if_generate_construct",

                "analog_function_seq_block_optional",
                "analog_event_seq_block_optional",
                "analog_seq_block_optional",
                "seq_block_optional",
                "par_block_optional",
                "if_generate_construct"
            };

            (string, Func<SyntaxNode, bool>)[] noSpaceAfter = new (string, Func<SyntaxNode, bool>)[]
            {
                ("#", null),
                ("!", null),
                ("(", null),
                (".", null),
                ("@", null),
                ("-", (token) => token.Parent.Name == "sign"),
                ("+", (token) => token.Parent.Name == "sign"),
                ("[", (token) => token.Parent.Name != "value_range")
            };

            (string, Func<SyntaxToken, bool>)[] removeSpaceBefore = new (string, Func<SyntaxToken, bool>)[]
            {
                (";", null),
                (",", null),
                ("(", (token) => 
                    token.Parent.Name == "branch_probe_function_call" 
                    || token.Parent.Name == "analog_filter_function_call" 
                    || token.Parent.Name == "analog_event_functions"
                    || token.Parent.Name == "analog_built_in_function_call"
                    || HasParent(new string[] {"system_task_enable", "analog_system_task_enable"}, token)),
                (")", null),
                ("[", (token) => token.Parent.Name != "value_range"),
                ("]", (token) => token.Parent.Name != "value_range")
            };


            var beforeActions = new List<Func<RawFormatterContext, ISyntaxElement, bool>>();

            var identedNodes = new List<(string node, string idented, Func<SyntaxNode, bool> condition)>();

            
            identedNodes.Add(("connectrules_declaration", "connectrules_item", null));
            identedNodes.Add(("module_declaration", "module_item", null));
            identedNodes.Add(("module_declaration", "non_port_module_item", (node) => { return node.Parent.Parent.Name == "module_declaration"; } ));

            identedNodes.Add(("nature_declaration", "nature_item", null));
            identedNodes.Add(("discipline_declaration", "discipline_item", null));

            identedNodes.Add(("procedural_timing_control_statement", "statement_or_null", null));

            identedNodes.Add(("initial_construct", "statement", null));
            identedNodes.Add(("analog_construct", "analog_function_statement", null));
            identedNodes.Add(("analog_construct", "analog_statement", null));
            identedNodes.Add(("analog_event_control_statement", "analog_event_statement", null));
            identedNodes.Add(("analog_function_seq_block", "analog_function_statement", null));
            identedNodes.Add(("analog_event_seq_block", "analog_event_statement", null));
            identedNodes.Add(("analog_seq_block", "analog_statement", null));
            identedNodes.Add(("analog_conditional_statement", "analog_statement_or_null", null));
            identedNodes.Add(("analog_function_conditional_statement_else", "analog_statement_or_null", null));

            identedNodes.Add(("conditional_statement", "statement_or_null", null));
            identedNodes.Add(("conditional_statement_else", "statement_or_null", null));

            identedNodes.Add(("seq_block", "statement", null));

            identedNodes.Add(("if_generate_construct", "generate_block_or_null", null));
            identedNodes.Add(("if_generate_construct_else", "generate_block_or_null", null));
            identedNodes.Add(("loop_generate_construct", "generate_block", null));
            identedNodes.Add(("generate_block", "module_or_generate_item", (SyntaxNode node) => { return ((SyntaxNode)node.Parent.Parent).Children.Any(d => d is SyntaxToken t && t.Value == "begin"); }));


            var inNewLine = new List<string>();
            inNewLine.Add("statement_or_null");
            inNewLine.Add("analog_statement_or_null");

            beforeActions.Add((context, element) =>
            {
                if (element is SyntaxNode node)
                {
                    if (!context.Indents.ContainsKey(node))
                    {
                        context.Indents[node] = 0;
                    }

                    if (emptyLineBefore.Contains(node.Name))
                    {
                        CreateNewLine(context, node);
                    }

                    if (inNewLine.Contains(node.Name))
                    {
                        BreakIfNecessary(context, element);
                    }

                    foreach (var definition in identedNodes.Where(item => item.node == node.Name))
                    {
                        foreach (var nested in node.GetNodes(definition.idented, 4))
                        {
                            if (definition.condition != null)
                            {
                                if (!definition.condition(nested))
                                {
                                    continue;
                                }
                            }

                            if (!context.Indents.ContainsKey(nested))
                            {
                                context.Indents[nested] = 0;
                            }
                            context.Indents[nested] = IdentStep;
                        }
                    }

                    var addSpaceBefore = GetNodeTokenize(node) && !noSpaceAfter.Any(str => context.CurrentLine.Text.EndsWith(str.Item1) && (str.Item2 == null || str.Item2(node)));

                    if (addSpaceBefore)
                    {
                        AddSpace(context.CurrentLine);
                    }
                }

                if (element is SyntaxToken token)
                {
                    UpdateIdent(context, token.Parent, false);

                    var addSpaceBefore = GetTokenTokenize(token.Parent as SyntaxNode)
                        && !noSpaceAfter.Any(str => context.CurrentLine.Text.EndsWith(str.Item1) && (str.Item2 == null || str.Item2(token.Parent as SyntaxNode)));

                    if (addSpaceBefore)
                    {
                        AddSpace(context.CurrentLine);
                    }

                    var action = removeSpaceBefore.FirstOrDefault(r => r.Item1 == token.Value);

                    if (action != default && (action.Item2 == null || action.Item2(token)))
                    {
                        context.CurrentLine.Text = context.CurrentLine.Text.TrimEnd();
                    }
                }

                return false;
            });

            var afterActions = new List<Func<RawFormatterContext, ISyntaxElement, bool>>();

            afterActions.Add((context, element) =>
            {
                if (emptyLinesAfterNodes.Contains(element.Name))
                {
                    EnsureLines(context, element, 2);
                }

                if (lineBreakAfterNodes.Contains(element.Name))
                {
                    EnsureLines(context, element, 1);
                }

                if (element is SyntaxToken token)
                {
                    context.CurrentLine.Text += token.Value;

                    var parent = token.Parent as SyntaxNode;
                    // new line after ;
                    if (token.Value == ";"
                        && (parent.Name != "analog_loop_statement"
                            && parent.Name != "analog_function_statement_loop_statement"
                            && parent.Name != "loop_statement")
                            && parent.Name != "loop_generate_construct"
                            && parent.Name != "analog_loop_generate_statement")
                    {
                        CreateNewLine(context, token);
                        return true;
                    }

                    if (token.Value == "begin")
                    {
                        var tokens = parent.GetTokens();
                        var tokenIndex = tokens.IndexOf(token);
                        var nextToken = tokens[tokenIndex + 1];

                        if (nextToken.Value == ":")
                        {
                            return true;
                        }

                        CreateNewLine(context, token);
                        return true;
                    }

                    if (token.Value == "end")
                    {
                        CreateNewLine(context, token);
                        return true;
                    }
                
                    return true;
                }

                return false;
            });

            return new RawFormatter(beforeActions, afterActions);
        }

        private static bool HasParent(string[] parents, ISyntaxElement element)
        {
            ISyntaxElement tmp = element;
            while (tmp != null)
            {
                if (tmp != element && parents.Contains(tmp.Name))
                {
                    return true;
                }
                tmp = tmp.Parent;
            }

            return false;
        }

        private static void UpdateIdent(RawFormatterContext context, ISyntaxElement element, bool force)
        {
            var level = GetIdentLevel(context, element);
            if (string.IsNullOrWhiteSpace(context.CurrentLine.Text))
            {
                context.CurrentLine.IdentLevel = level;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(context.CurrentLine.Text) && context.CurrentLine.IdentLevel != level)
                {
                    context.Lines.Add(new RawFormatterLine { IdentLevel = level });
                }
            }
        }

        private static void CreateNewLine(RawFormatterContext context, ISyntaxElement element)
        {
            var level = GetIdentLevel(context, element);
            context.Lines.Add(new RawFormatterLine() { IdentLevel = level });
        }
        private static void EnsureLines(RawFormatterContext context, ISyntaxElement element, int count)
        {
            if (string.IsNullOrWhiteSpace(context.CurrentLine.Text))
            {
                count--;
            }
            for (var i = 0; i < count; i++)
            {
                CreateNewLine(context, element);
            }
        }

        private static void BreakIfNecessary(RawFormatterContext context, ISyntaxElement element)
        {
            if (!string.IsNullOrWhiteSpace(context.CurrentLine.Text))
            {
                var level = GetIdentLevel(context, element);

                context.Lines.Add(new RawFormatterLine() { IdentLevel = level });
            }
        }
        
        private static int GetIdentLevel(RawFormatterContext context, ISyntaxElement node)
        {
            int totalIdent = 0;
            while (node != null)
            {
                totalIdent += context.Indents.ContainsKey(node) ? context.Indents[node] : 0;
                node = node.Parent;
            }

            return totalIdent;
        }

        private static void AddSpace(RawFormatterLine currentLine)
        {
            if (!string.IsNullOrWhiteSpace(currentLine.Text) && !currentLine.Text.EndsWith(' '))
            {
                currentLine.Text += " ";
            }
        }
        private static bool GetNodeTokenize(SyntaxNode node)
        {
            return node.Attributes.ContainsKey("nodeTokenize") && node.Attributes["nodeTokenize"] == "true";
        }

        private static bool GetTokenTokenize(SyntaxNode node)
        {
            return node.Attributes.ContainsKey("tokenTokenize") && node.Attributes["tokenTokenize"] == "true";
        }
    }
}
