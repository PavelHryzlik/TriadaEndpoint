using System;
using System.IO;
using System.Xml;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace TriadaEndpoint.DotNetRDF.SparqlResultHandlers
{
    /// <summary>
    /// Handler formatting SparqlResults to XML representation
    /// </summary>
    public class XmlResultHandler : BaseResultsHandler
    {
        private readonly XmlWriter _writter;
        private readonly bool _closeOutput;
        private bool _firstResult = true;

        /// <summary>
        /// Handler constructor
        /// </summary>
        /// <param name="output">Input Text writter</param>
        /// <param name="closeOutput">Indicates whether to close writter at the end</param>
        public XmlResultHandler(TextWriter output, bool closeOutput)
        {
            _writter = new XmlTextWriter(output) { Formatting = Formatting.Indented };
            _closeOutput = closeOutput;
        }

        /// <summary>
        /// Write start of the document
        /// </summary>
        protected override void StartResultsInternal()
        {
            _writter.WriteStartDocument();

            _writter.WriteStartElement("sparql");
            _writter.WriteAttributeString("xmlns", SparqlSpecsHelper.SparqlNamespace);
        }

        /// <summary>
        /// Write end of the document
        /// </summary>
        /// <param name="ok"></param>
        protected override void EndResultsInternal(bool ok)
        {
            if (!_firstResult)
                _writter.WriteEndElement();

            _writter.WriteEndElement();
            _writter.WriteEndDocument();

            if (_closeOutput)
                _writter.Close();
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
            //Write output variables first
            if (_firstResult)
            {
                _writter.WriteStartElement("head");

                foreach (String var in result.Variables)
                {
                    //<variable> element
                    _writter.WriteStartElement("variable");
                    _writter.WriteAttributeString("name", var);
                    _writter.WriteEndElement();
                }

                _writter.WriteEndElement();

                //<results> Element
                _writter.WriteStartElement("results");

                _firstResult = false;
            }

            //<result> Element
            _writter.WriteStartElement("result");

            foreach (String var in result.Variables)
            {
                if (result.HasValue(var))
                {
                    //<binding> Element
                    _writter.WriteStartElement("binding");
                    _writter.WriteAttributeString("name", var);

                    INode n = result.Value(var);
                    if (n == null) continue; //NULLs don't get serialized in the XML Format
                    switch (n.NodeType)
                    {
                        case NodeType.Blank:
                            //<bnode> element
                            _writter.WriteElementString("bnode", ((IBlankNode)n).InternalID); // Write blank node
                            break;

                        case NodeType.GraphLiteral:
                            throw new RdfOutputException("Result Sets which contain Graph Literal Nodes cannot be serialized in the SPARQL Query Results XML Format");

                        case NodeType.Literal:
                            //<literal> element
                            _writter.WriteStartElement("literal");

                            var l = (ILiteralNode)W3CSpecHelper.FormatNode(n); // Format by W3C spec.

                            if (!l.Language.Equals(String.Empty))
                            {
                                _writter.WriteAttributeString("xml:lang", l.Language); // Set language attribute
                            }
                            else if (l.DataType != null)
                            {
                                _writter.WriteAttributeString("datatype", WriterHelper.EncodeForXml(l.DataType.ToString())); // Set datatype attribute
                            }

                            // Write literal
                            _writter.WriteValue(l.Value);

                            _writter.WriteEndElement();
                            break;

                        case NodeType.Uri:
                            //<uri> element
                            _writter.WriteElementString("uri", WriterHelper.EncodeForXml(((IUriNode)n).Uri.ToString())); // Write Uri
                            break;

                        default:
                            throw new RdfOutputException("Result Sets which contain Nodes of unknown Type cannot be serialized in the SPARQL Query Results XML Format");

                    }

                    _writter.WriteEndElement();
                }
            }

            _writter.WriteEndElement();

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