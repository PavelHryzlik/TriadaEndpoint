using System;
using System.Xml;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace TriadaEndpoint.DotNetRDF.SparqlLazyResultHandlers
{
    /// <summary>
    /// Handler formatting SparqlResults to XML representation
    /// </summary>
    public class XmlLazyResultHandler : LazyResultHandler
    {
        private bool _firstResult = true;

        /// <summary>
        /// Write start of the document
        /// </summary>
        protected override void StartResultsInternal()
        {
            var xmlString = new System.IO.StringWriter();
            using (var writer = new XmlTextWriter(xmlString) { Formatting = Formatting.Indented })
            {
                writer.WriteStartDocument();

                writer.WriteStartElement("sparql");
                writer.WriteAttributeString("xmlns", SparqlSpecsHelper.SparqlNamespace);

                AddToQueue(xmlString.ToString());
            }
        }

        /// <summary>
        /// Write end of the document
        /// </summary>
        /// <param name="ok"></param>
        protected override void EndResultsInternal(bool ok)
        {
            var xmlString = new System.IO.StringWriter();
            using (var writer = new XmlTextWriter(xmlString) {Formatting = Formatting.Indented})
            {
                if (!_firstResult)
                    writer.WriteRaw("</results>");

                writer.WriteRaw("</sparql>");

                AddToQueue(xmlString.ToString());
                CompleteQueue();
            }
        }

        protected override void HandleBooleanResultInternal(bool result)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Parse incoming SparqlResult (one row) to XML
        /// </summary>
        /// <param name="result">SparqlResult</param>
        /// <returns></returns>
        protected override bool HandleResultInternal(SparqlResult result)
        {
            var xmlString = new System.IO.StringWriter();
            using (var writer = new XmlTextWriter(xmlString) {Formatting = Formatting.Indented})
            {
                //Write output variables first
                if (_firstResult)
                {
                    writer.WriteRaw(">");

                    writer.WriteStartElement("head");

                    foreach (String var in result.Variables)
                    {
                        //<variable> element
                        writer.WriteStartElement("variable");
                        writer.WriteAttributeString("name", var);
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();

                    //<results> Element
                    writer.WriteStartElement("results");

                    _firstResult = false;
                }

                //<result> Element
                writer.WriteStartElement("result");

                foreach (String var in result.Variables)
                {
                    if (result.HasValue(var))
                    {
                        //<binding> Element
                        writer.WriteStartElement("binding");
                        writer.WriteAttributeString("name", var);

                        INode n = result.Value(var);
                        if (n == null) continue; //NULLs don't get serialized in the XML Format
                        switch (n.NodeType)
                        {
                            case NodeType.Blank:
                                //<bnode> element
                                writer.WriteElementString("bnode", ((IBlankNode) n).InternalID); // Write blank node
                                break;

                            case NodeType.GraphLiteral:
                                throw new RdfOutputException(
                                    "Result Sets which contain Graph Literal Nodes cannot be serialized in the SPARQL Query Results XML Format");

                            case NodeType.Literal:
                                //<literal> element
                                writer.WriteStartElement("literal");

                                var l = (ILiteralNode) W3CSpecHelper.FormatNode(n); // Format by W3C spec.

                                if (!l.Language.Equals(String.Empty))
                                {
                                    writer.WriteAttributeString("xml:lang", l.Language); // Set language attribute
                                }
                                else if (l.DataType != null)
                                {
                                    writer.WriteAttributeString("datatype",
                                        WriterHelper.EncodeForXml(l.DataType.ToString())); // Set datatype attribute
                                }

                                // Write literal
                                writer.WriteValue(l.Value);

                                writer.WriteEndElement();
                                break;

                            case NodeType.Uri:
                                //<uri> element
                                writer.WriteElementString("uri",
                                    WriterHelper.EncodeForXml(((IUriNode) n).Uri.ToString())); // Write Uri
                                break;

                            default:
                                throw new RdfOutputException(
                                    "Result Sets which contain Nodes of unknown Type cannot be serialized in the SPARQL Query Results XML Format");

                        }

                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();

                AddToQueue(xmlString.ToString());
            }

            return true;
        }

        /// <summary>
        /// Method to handle the variables
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        protected override bool HandleVariableInternal(string var)
        {
            return true;
        }
    }
}
