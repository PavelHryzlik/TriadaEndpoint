using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace TriadaEndpoint.Controllers
{
    public class RdfXmlResultHandler: BaseResultsHandler
    {
        private readonly XmlWriter _writter;
        private readonly bool _closeOutput;
        private bool _firstResult = true;

        public RdfXmlResultHandler(TextWriter output, bool closeOutput)
        {
            _writter = new XmlTextWriter(output) { Formatting = Formatting.Indented };
            _closeOutput = closeOutput;
        }

        protected override void StartResultsInternal()
        {
            _writter.WriteStartDocument();

            _writter.WriteStartElement("rdf:RDF");
            _writter.WriteAttributeString("xmlns", SparqlSpecsHelper.SparqlNamespace);
            _writter.WriteAttributeString("xmlns", SparqlSpecsHelper.SparqlRdfResultsNamespace);

            _writter.WriteStartElement("rs:ResultSet");
        }

        protected override void EndResultsInternal(bool ok)
        {
            _writter.WriteEndElement();
            _writter.WriteEndDocument();

            if (_closeOutput)
                _writter.Close();
        }


        protected override void HandleBooleanResultInternal(bool result)
        {
            //TODO
        }

        protected override bool HandleResultInternal(SparqlResult result)
        {
            if (_firstResult)
            {
                foreach (String var in result.Variables)
                {
                    //<variable> element
                    _writter.WriteElementString("rs:resultVariable",var);
                }

                _firstResult = false;
            }

            //<result> Element
            _writter.WriteStartElement("rs:solution");
            _writter.WriteAttributeString("rdf:parseType", "Resource");

            foreach (String var in result.Variables)
            {
                if (result.HasValue(var))
                {
                    //<binding> Element
                    _writter.WriteStartElement("rs:binding");

                    _writter.WriteElementString("rs:variable", var);

                    _writter.WriteStartElement("rs:value");

                    INode n = result.Value(var);
                    if (n == null) continue; //NULLs don't get serialized in the XML Format
                    switch (n.NodeType)
                    {
                        case NodeType.Blank:
                            //<bnode> element
                            _writter.WriteAttributeString("rdf:nodeID", ((IBlankNode)n).InternalID);
                            break;

                        case NodeType.GraphLiteral:
                            //Error!
                            throw new RdfOutputException("Result Sets which contain Graph Literal Nodes cannot be serialized in the SPARQL Query Results XML Format");

                        case NodeType.Literal:
                            //<literal> element
                            var l = (ILiteralNode)W3CSpecHelper.FormatNode(n);

                            if (l.DataType != null)
                            {
                                _writter.WriteAttributeString("rdf:datatype", WriterHelper.EncodeForXml(l.DataType.ToString()));
                            }

                            _writter.WriteValue(l.Value);
                            break;

                        case NodeType.Uri:
                            //<uri> element
                            _writter.WriteAttributeString("rdf:resource", WriterHelper.EncodeForXml(((IUriNode)n).Uri.ToString()));
                            break;

                        default:
                            throw new RdfOutputException("Result Sets which contain Nodes of unknown Type cannot be serialized in the SPARQL Query Results XML Format");

                    }

                    _writter.WriteEndElement();
                    _writter.WriteEndElement();
                }
            }

            _writter.WriteEndElement();

            return true;
        }

        protected override bool HandleVariableInternal(string var)
        {
            return true;
        }
    }
}