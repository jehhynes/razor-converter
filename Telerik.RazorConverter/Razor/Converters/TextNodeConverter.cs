namespace Telerik.RazorConverter.Razor.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Telerik.RazorConverter.Razor.DOM;
    using Telerik.RazorConverter.WebForms.DOM;

    public class TextNodeConverter : INodeConverter<IRazorNode>
    {
        private IRazorTextNodeFactory TextNodeFactory
        {
            get;
            set;
        }

        public TextNodeConverter(IRazorTextNodeFactory nodeFactory)
        {
            TextNodeFactory = nodeFactory;
        }
        
        public IList<IRazorNode> ConvertNode(IWebFormsNode node)
        {
            if (node is IWebFormsTextNode)
            {
                var srcNode = node as IWebFormsTextNode;
                var destNode = TextNodeFactory.CreateTextNode(srcNode.Text);
                return new IRazorNode[] { destNode };
            }
            else
            {
                var srcNode = node as IWebFormsServerControlNode;
                return node.Children.OfType<IWebFormsTextNode>().Select(x => TextNodeFactory.CreateTextNode(x.Text)).ToArray();
            }
        }

        public bool CanConvertNode(IWebFormsNode node)
        {
            return node is IWebFormsTextNode || (node is IWebFormsServerControlNode serverControlNode && serverControlNode.TagName.ToLowerInvariant() == "script" && serverControlNode.Children.All(x => x is IWebFormsTextNode));
        }
    }
}
