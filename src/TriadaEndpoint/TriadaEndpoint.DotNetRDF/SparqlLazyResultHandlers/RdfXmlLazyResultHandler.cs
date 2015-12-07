using System;
using System.Xml;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace TriadaEndpoint.DotNetRDF.SparqlLazyResultHandlers
{
    /// <summary>
    /// Handler formatting SparqlResults to Rdf/Xml representation
    /// </summary>
    public class RdfXmlLazyResultHandler : LazyResultHandler
    {
        private bool _firstResult = true;

        /// <summary>
        /// Write start of the document
        /// </summary>
        protected override void StartResultsInternal()
        {
            var xmlString = new System.IO.StringWriter();
            using (var writer = new XmlTextWriter(xmlString) {Formatting = Formatting.Indented})
            {
                writer.WriteStartDocument();

                writer.WriteStartElement("rdf:RDF");
                writer.WriteAttributeString("xmlns", SparqlSpecsHelper.SparqlNamespace);
                writer.WriteAttributeString("xmlns", SparqlSpecsHelper.SparqlRdfResultsNamespace);

                writer.WriteStartElement("rs:ResultSet");

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
                writer.WriteRaw("</rs:ResultSet>");
                writer.WriteRaw("</rdf:RDF>");

                AddToQueue(xmlString.ToString());
                CompleteQueue();
            }
        }

        protected override void HandleBooleanResultInternal(bool result)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Parse incoming SparqlResult (one row) to Rdf/Xml
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
                    foreach (String var in result.Variables)
                    {
                        //<variable> element
                        writer.WriteElementString("rs:resultVariable", var);
                    }

                    _firstResult = false;
                }

                //<result> Element
                writer.WriteStartElement("rs:solution");
                writer.WriteAttributeString("rdf:parseType", "Resource");

                foreach (String var in result.Variables)
                {
                    if (result.HasValue(var))
                    {
                        //<binding> Element
                        writer.WriteStartElement("rs:binding");

                        writer.WriteElementString("rs:variable", var);

                        writer.WriteStartElement("rs:value");

                        INode n = result.Value(var);
                        if (n == null) continue; //NULLs don't get serialized in the XML Format
                        switch (n.NodeType)
                        {
                            case NodeType.Blank:
                                //<bnode> element
                                writer.WriteAttributeString("rdf:nodeID", ((IBlankNode) n).InternalID);
                                break;

                            case NodeType.GraphLiteral:
                                //Error!
                                throw new RdfOutputException(
                                    "Result Sets which contain Graph Literal Nodes cannot be serialized in the SPARQL Query Results XML Format");

                            case NodeType.Literal:
                                //<literal> element
                                var l = (ILiteralNode) W3CSpecHelper.FormatNode(n); // Format by W3C spec.

                                if (l.DataType != null) // Set datatype 
                                {
                                    writer.WriteAttributeString("rdf:datatype",
                                        WriterHelper.EncodeForXml(l.DataType.ToString()));
                                }

                                writer.WriteValue(l.Value);
                                break;

                            case NodeType.Uri:
                                //<uri> element
                                writer.WriteAttributeString("rdf:resource",
                                    WriterHelper.EncodeForXml(((IUriNode) n).Uri.ToString()));
                                break;

                            default:
                                throw new RdfOutputException(
                                    "Result Sets which contain Nodes of unknown Type cannot be serialized in the SPARQL Query Results XML Format");

                        }

                        writer.WriteEndElement();
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
        /// <param name="var">Variable</param>
        /// <returns></returns>
        protected override bool HandleVariableInternal(string var)
        {
            return true;
        }
    }
}
