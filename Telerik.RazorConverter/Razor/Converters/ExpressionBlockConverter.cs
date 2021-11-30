﻿namespace Telerik.RazorConverter.Razor.Converters
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

        static Regex ternaryRegex = new Regex(@"^.+?\?.+?:.+?$", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);

        public IList<IRazorNode> ConvertNode(IWebFormsNode node)
        {
            var srcNode = node as IWebFormsExpressionBlockNode;
            var isMultiline = srcNode.Expression.Contains("\r") || srcNode.Expression.Contains("\n");
            bool isTernaryExpression = ternaryRegex.IsMatch(srcNode.Expression);
            var expression = srcNode.Expression.Trim(new char[] { ' ', '\t' });
            if (isTernaryExpression)
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
    }
}
