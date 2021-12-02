namespace Telerik.RazorConverter.Razor.Converters
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Telerik.RazorConverter.Razor.DOM;
    using Telerik.RazorConverter.WebForms.DOM;

    public class ExpressionBlockConverter : INodeConverter<IRazorNode>
    {
        private IRazorExpressionNodeFactory ExpressionNodeFactory
        {
            get;
            set;
        }

        public ExpressionBlockConverter(IRazorExpressionNodeFactory nodeFactory)
        {
            ExpressionNodeFactory = nodeFactory;
        }

        public IList<IRazorNode> ConvertNode(IWebFormsNode node)
        {
            var srcNode = node as IWebFormsExpressionBlockNode;
            var isMultiline = srcNode.Expression.Contains("\r") || srcNode.Expression.Contains("\n");
            bool needsParens = HasUnbalancedParens(srcNode.Expression) || HasNonWordCharactersOutsideParens(srcNode.Expression);
            var expression = srcNode.Expression.Trim(new char[] { ' ', '\t' });
            if (needsParens)
                expression = "(" + expression + ")";

            expression = expression.Replace("ResolveUrl", "Url.Content");
            expression = RemoveHtmlEncode(expression);
            expression = WrapHtmlDecode(expression);
            return new IRazorNode[] 
            {
                ExpressionNodeFactory.CreateExpressionNode(expression, isMultiline)
            };
        }

        public bool CanConvertNode(IWebFormsNode node)
        {
            return node is IWebFormsExpressionBlockNode;
        }

        private string RemoveHtmlEncode(string input)
        {
            var searchRegex = new Regex(@"(Html\.Encode|HttpUtility\.HtmlEncode)\s*\((?<statement>(?>[^()]+|\((?<Depth>)|\)(?<-Depth>))*(?(Depth)(?!)))\)", RegexOptions.Singleline | RegexOptions.Multiline);
            var stringCastRegex = new Regex(@"^\(\s*string\s*\)\s*", RegexOptions.IgnoreCase);
            return searchRegex.Replace(input, m =>
            {
                return stringCastRegex.Replace(m.Groups["statement"].Value.Trim(), "");
            });
        }

        private string WrapHtmlDecode(string input)
        {
            var searchRegex = new Regex(@"HttpUtility.HtmlDecode\((?<statement>.*)\)", RegexOptions.Singleline | RegexOptions.Multiline);
            return searchRegex.Replace(input, m =>
            {
                return string.Format("Html.Raw(HttpUtility.HtmlDecode({0}))", m.Groups["statement"].Value.Trim());
            });
        }


        static Regex parensRegex = new Regex(@"(\(|\))", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);

        private bool HasUnbalancedParens(string expression)
        {
            if (expression[0] == '(' && expression[expression.Length - 1] != ')')
                return true;

            bool isFirst = true;
            int level = 0;
            foreach (Match match in parensRegex.Matches(expression))
            {
                if (!isFirst && level == 0)
                    return true;
                
                char paren = match.Groups[1].Value[0];
                if (paren == '(')
                    level++;
                else if (paren == ')')
                    level--;

                isFirst = false;
            }

            return false;
        }

        static Regex nonWordRegex = new Regex(@"[^\w\d\.\(\)\]\[""]", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);
        private bool HasNonWordCharactersOutsideParens(string expression)
        {
            expression = expression.Trim();
            foreach (Match match in nonWordRegex.Matches(expression))
            {
                int idx = match.Index;
                if (idx == 0 || idx == expression.Length - 1) 
                    return true;

                int parenLevelBefore = new Regex(@"\(").Matches(expression.Substring(0, idx + 1)).Count
                    - new Regex(@"\)").Matches(expression.Substring(0, idx + 1)).Count;

                int parenLevelAfter = new Regex(@"\)").Matches(expression.Substring(idx + 1)).Count
                    - new Regex(@"\(").Matches(expression.Substring(idx + 1)).Count;

                int quotesBefore = new Regex("\"").Matches(expression.Substring(0, idx + 1)).Count
                    + new Regex("\"").Matches(expression.Substring(0, idx + 1)).Count;

                int quotesAfter = new Regex("\"").Matches(expression.Substring(idx + 1)).Count
                    + new Regex("\"").Matches(expression.Substring(idx + 1)).Count;

                if ((parenLevelBefore <= 0 || parenLevelAfter <= 0) && !(quotesBefore % 2 == 1 && quotesAfter % 2 == 1))
                    return true;
            }
            return false;
        }
    }
}
