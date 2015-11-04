using System;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;

namespace TriadaEndpoint.DotNetRDF.Utils
{
    public static class W3CSpecHelper
    {
        /// <summary>
        /// Support only DateTime, Date and Time
        /// </summary>
        /// <param name="node">Input node</param>
        /// <returns>result node</returns>
        public static INode FormatNode(INode node)
        {
            if (node != null &&
                node.NodeType == NodeType.Literal &&
                ((ILiteralNode)node).DataType != null)
            {
                switch (((ILiteralNode) node).DataType.ToString())
                {
                    case XmlSpecsHelper.XmlSchemaDataTypeDateTime:
                        var dateTime = DateTime.Parse(((ILiteralNode) node).Value);
                        return new DateTimeNode(node.Graph, new DateTimeOffset(dateTime));
                    case XmlSpecsHelper.XmlSchemaDataTypeDate:
                        var date = DateTime.Parse(((ILiteralNode) node).Value);
                        return new DateNode(node.Graph, date);
                    case XmlSpecsHelper.XmlSchemaDataTypeTime:
                        var timeSpan = TimeSpan.Parse(((ILiteralNode)node).Value);
                        return new TimeSpanNode(node.Graph, timeSpan);
                    default:
                        return node;
                }
            }
            return node;
        }
    }
}