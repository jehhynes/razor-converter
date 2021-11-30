namespace Telerik.RazorConverter.Razor.Converters
{
    using System.Collections.Generic;
    using System.Linq;
    using Telerik.RazorConverter;
    using Telerik.RazorConverter.Razor.DOM;
    using Telerik.RazorConverter.WebForms.DOM;

    public class CodeGroupConverter : INodeConverter<IRazorNode>
    {
        private IRazorNodeConverterProvider NodeConverterProvider
        {
            get;
            set;
        }

        public CodeGroupConverter(IRazorNodeConverterProvider converterProvider)
        {
            NodeConverterProvider = converterProvider;
        }

        public IList<IRazorNode> ConvertNode(IWebFormsNode node)
        {
            var result = new List<IRazorNode>();

            foreach (var childNode in node.Children)
            {
                foreach (var converter in NodeConverterProvider.NodeConverters)
                {
                    if (converter.CanConvertNode(childNode))
                    {
                        foreach (var convertedChildNode in converter.ConvertNode(childNode))
                        {
                            result.Add(convertedChildNode);
                        }
                    }
                }
            }

            if (result.Count >= 3 && result[0] is RazorCodeNode && result.Last() is RazorCodeNode && !(result[1] is RazorCodeNode))
            {
                if(IsBetweenScriptTags(node))
                {
                    result.Insert(1, new RazorTextNodeFactory().CreateTextNode("<text>"));
                    result.Insert(result.Count - 1, new RazorTextNodeFactory().CreateTextNode("</text>"));
                }
            }

            return result;
        }

        public bool CanConvertNode(IWebFormsNode node)
        {
            return node as IWebFormsCodeGroupNode != null;
        }

        bool IsBetweenScriptTags(IWebFormsNode node)
        {
            int thisTagIndex = node.Parent.Children.IndexOf(node);

            int beginTagIndex = -1;
            int endTagIndex = -1;

            for(int i =0;i<node.Parent.Children.Count;i++)
            {
                var sibling = node.Parent.Children[i];
                if (sibling is IWebFormsTextNode tn)
                {
                    bool isScriptStartTag = tn.Text.ToLowerInvariant().Contains("<script");
                    bool isScriptEndTag = tn.Text.ToLowerInvariant().Contains("</script>");

                    if (i < thisTagIndex && isScriptStartTag)
                    {
                        beginTagIndex = i;
                    }

                    if (i > thisTagIndex && beginTagIndex >= 0 && isScriptEndTag)
                    {
                        endTagIndex = i;
                    }
                }
            }

            return beginTagIndex >= 0 && endTagIndex > 0;
        }
    }
}
