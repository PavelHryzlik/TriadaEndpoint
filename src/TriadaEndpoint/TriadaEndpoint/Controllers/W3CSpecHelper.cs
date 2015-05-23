using System;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;

namespace TriadaEndpoint.Controllers
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
                DateTime dateTime;

                switch (((ILiteralNode) node).DataType.ToString())
                {
                    case XmlSpecsHelper.XmlSchemaDataTypeDateTime:
                        dateTime = DateTime.Parse(((ILiteralNode) node).Value);
                        break;
                    case XmlSpecsHelper.XmlSchemaDataTypeDate:
                        dateTime = DateTime.Parse(((ILiteralNode) node).Value);
                        break;
                    case XmlSpecsHelper.XmlSchemaDataTypeTime:
                        dateTime = DateTime.Parse(((ILiteralNode) node).Value);
                        break;
                    default:
                        return node;
                }

                return new DateTimeNode(node.Graph, dateTime);
            }
            return node;
        }
    }
}