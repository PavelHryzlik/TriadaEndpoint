using System;
using System.IO;
using System.Web.UI;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.DotNetRDF.SparqlResultHandlers
{
    /// <summary>
    /// Handler formatting SparqlResults to Html representation
    /// </summary>
    public class HtmlResultHandler : BaseResultsHandler
    {
        private const String UriClass = "uri";
        private const String BnodeClass = "bnode";
        private const String LiteralClass = "literal";
        private const String DatatypeClass = "datatype";
        private const String LangClass = "langspec";
        private String _uriPrefix = String.Empty;

        private readonly HtmlTextWriter _writter;
        private readonly HtmlFormatter _formatter = new HtmlFormatter();
        private INamespaceMapper _namespaces = new NamespaceMapper();
        private QNameOutputMapper _qnameMapper;
        private readonly bool _closeOutput;
        private bool _firstResult = true;

        public INamespaceMapper DefaultNamespaces
        {
            get
            {
                return _namespaces;
            }
            set
            {
                _namespaces = value;
            }
        }

        public String UriPrefix
        {
            get
            {
                return _uriPrefix;
            }
            set
            {
                if (value != null) _uriPrefix = value;
            }
        }

        /// <summary>
        /// Handler constructor
        /// </summary>
        /// <param name="output">Input Text writter</param>
        /// <param name="closeOutput">Indicates whether to close writter at the end</param>
        public HtmlResultHandler(TextWriter output, bool closeOutput)
        {
            //TODO More settings
            _writter = new HtmlTextWriter(output);
            _closeOutput = closeOutput;
        }

        /// <summary>
        /// Write start of the document
        /// </summary>
        protected override void StartResultsInternal()
        {
            _qnameMapper = new QNameOutputMapper(_namespaces ?? new NamespaceMapper(true));

            //Page Header
            _writter.Write("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
            _writter.RenderBeginTag(HtmlTextWriterTag.Html);
            _writter.RenderBeginTag(HtmlTextWriterTag.Head);
            _writter.RenderBeginTag(HtmlTextWriterTag.Title);
            _writter.WriteEncodedText("SPARQL Query Results");
            _writter.RenderEndTag();

            //TODO: Add <meta>
            _writter.RenderEndTag();

            //Start Body
            _writter.RenderBeginTag(HtmlTextWriterTag.Body);
        }

        /// <summary>
        /// Write end of the document
        /// </summary>
        /// <param name="ok"></param>
        protected override void EndResultsInternal(bool ok)
        {
            if (!_firstResult)
            {
                //End Table Body
                _writter.RenderEndTag();

                //End Table
                _writter.RenderEndTag();
            }

            //End of Page
            _writter.RenderEndTag(); //End Body
            _writter.RenderEndTag(); //End Html

            if(_closeOutput)
                _writter.Close();
        }

        /// <summary>
        /// Method to handle Boolean result
        /// </summary>
        /// <param name="result"></param>
        protected override void HandleBooleanResultInternal(bool result)
        {
            //Show a Header and a Boolean value
            _writter.RenderBeginTag(HtmlTextWriterTag.H3);
            _writter.WriteEncodedText("ASK Query Result");
            _writter.RenderEndTag();
            _writter.RenderBeginTag(HtmlTextWriterTag.P);
            _writter.WriteEncodedText(result.ToString());
            _writter.RenderEndTag();
        }

        /// <summary>
        /// Parse incoming SparqlResult (one row) to html table
        /// </summary>
        /// <param name="result">SparqlResult</param>
        /// <returns></returns>
        protected override bool HandleResultInternal(SparqlResult result)
        {
            //Write headers and output variables
            if (_firstResult)
            {
                //Create a Table for the results
                _writter.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
                _writter.RenderBeginTag(HtmlTextWriterTag.Table);

                //Create a Table Header with the Variable Names
                _writter.RenderBeginTag(HtmlTextWriterTag.Thead);
                _writter.RenderBeginTag(HtmlTextWriterTag.Tr);

                foreach (String var in result.Variables)
                {
                    _writter.RenderBeginTag(HtmlTextWriterTag.Th);
                    _writter.WriteEncodedText(var);
                    _writter.RenderEndTag();
                }

                _writter.RenderEndTag();
                _writter.RenderEndTag();
#if !NO_WEB
                _writter.WriteLine();
#endif

                //Create a Table Body for the Results
                _writter.RenderBeginTag(HtmlTextWriterTag.Tbody);

                _firstResult = false;
            }

            //Start Row
            _writter.RenderBeginTag(HtmlTextWriterTag.Tr);

            foreach (String var in result.Variables)
            {
                //Start Column
                _writter.RenderBeginTag(HtmlTextWriterTag.Td);

                if (result.HasValue(var))
                {
                    INode value = result[var];

                    if (value != null)
                    {
                        switch (value.NodeType)
                        {
                            case NodeType.Blank: // Write blank node
                                _writter.AddAttribute(HtmlTextWriterAttribute.Class, BnodeClass);
                                _writter.RenderBeginTag(HtmlTextWriterTag.Span);
                                _writter.WriteEncodedText(value.ToString());
                                _writter.RenderEndTag();
                                break;

                            case NodeType.Literal:
                                var lit = (ILiteralNode)W3CSpecHelper.FormatNode(value); // Format by W3C spec.
                                _writter.AddAttribute(HtmlTextWriterAttribute.Class, LiteralClass);
                                _writter.RenderBeginTag(HtmlTextWriterTag.Span);
                                if (lit.DataType != null) // Set datatype 
                                {
                                    _writter.WriteEncodedText(lit.Value);
                                    _writter.RenderEndTag();
                                    _writter.WriteEncodedText("^^");
                                    _writter.AddAttribute(HtmlTextWriterAttribute.Href, _formatter.FormatUri(lit.DataType.AbsoluteUri));
                                    _writter.AddAttribute(HtmlTextWriterAttribute.Class, DatatypeClass);
                                    _writter.RenderBeginTag(HtmlTextWriterTag.A);
                                    _writter.WriteEncodedText(lit.DataType.ToString());
                                    _writter.RenderEndTag();
                                }
                                else
                                {
                                    _writter.WriteEncodedText(lit.Value);
                                    if (!lit.Language.Equals(String.Empty)) // Set language
                                    {
                                        _writter.RenderEndTag();
                                        _writter.WriteEncodedText("@");
                                        _writter.AddAttribute(HtmlTextWriterAttribute.Class, LangClass);
                                        _writter.RenderBeginTag(HtmlTextWriterTag.Span);
                                        _writter.WriteEncodedText(lit.Language);
                                        _writter.RenderEndTag();
                                    }
                                    else
                                    {
                                        _writter.RenderEndTag();
                                    }
                                }
                                break;

                            case NodeType.GraphLiteral:
                                //Error
                                throw new RdfOutputException("Result Sets which contain Graph Literal Nodes cannot be serialized in the HTML Format");

                            case NodeType.Uri:  // Write Uri as link
                                _writter.AddAttribute(HtmlTextWriterAttribute.Class, UriClass);
                                _writter.AddAttribute(HtmlTextWriterAttribute.Href, _formatter.FormatUri(UriPrefix + value.ToString()));
                                _writter.RenderBeginTag(HtmlTextWriterTag.A);

                                String qname;
                                if (_qnameMapper.ReduceToQName(value.ToString(), out qname))
                                {
                                    _writter.WriteEncodedText(qname);
                                }
                                else
                                {
                                    _writter.WriteEncodedText(value.ToString());
                                }
                                _writter.RenderEndTag();
                                break;

                            default:
                                throw new RdfOutputException("Result which contain Unknown Node Types cannot be serialized in the HTML Format");
                        }
                    }
                    else
                    {
                        _writter.WriteEncodedText(" ");
                    }
                }
                else
                {
                    _writter.WriteEncodedText(" ");
                }

                //End Column
                _writter.RenderEndTag();
            }

            //End Row
            _writter.RenderEndTag();
#if !NO_WEB
            _writter.WriteLine();
#endif

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