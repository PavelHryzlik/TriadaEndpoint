using System;
using System.IO;
using System.Xml;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace TriadaEndpoint.Controllers
{
    public class XmlResultHandler : BaseResultsHandler
    {
        private readonly XmlWriter _writter;
        private readonly bool _closeOutput;
        private bool _firstResult = true;

        public XmlResultHandler(TextWriter output, bool closeOutput)
        {
            _writter = new XmlTextWriter(output) { Formatting = Formatting.Indented };
            _closeOutput = closeOutput;
        }

        protected override void StartResultsInternal()
        {
            _writter.WriteStartDocument();

            _writter.WriteStartElement("sparql");
            _writter.WriteAttributeString("xmlns", SparqlSpecsHelper.SparqlNamespace);
        }

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
            //TODO
        }

        protected override bool HandleResultInternal(SparqlResult result)
        {
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
                            _writter.WriteElementString("bnode", ((IBlankNode)n).InternalID);
                            break;

                        case NodeType.GraphLiteral:
                            //Error!
                            throw new RdfOutputException("Result Sets which contain Graph Literal Nodes cannot be serialized in the SPARQL Query Results XML Format");

                        case NodeType.Literal:
                            //<literal> element
                            _writter.WriteStartElement("literal");

                            var l = (ILiteralNode)W3CSpecHelper.FormatNode(n);

                            if (!l.Language.Equals(String.Empty))
                            {
                                _writter.WriteAttributeString("xml:lang", l.Language);
                            }
                            else if (l.DataType != null)
                            {
                                _writter.WriteAttributeString("datatype", WriterHelper.EncodeForXml(l.DataType.ToString()));
                            }

                            _writter.WriteValue(l.Value);

                            _writter.WriteEndElement();
                            break;

                        case NodeType.Uri:
                            //<uri> element
                            _writter.WriteElementString("uri", WriterHelper.EncodeForXml(((IUriNode)n).Uri.ToString()));
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

        protected override bool HandleVariableInternal(string var)
        {
            return true;
        }
    }
}