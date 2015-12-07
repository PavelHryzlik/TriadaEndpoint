using System;
using System.Web.UI;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.DotNetRDF.SparqlLazyResultHandlers
{
    /// <summary>
    /// Handler formatting SparqlResults to Html representation
    /// </summary>
    public class HtmlLazyResultHandler : LazyResultHandler
    {
        private const String UriClass = "uri";
        private const String BnodeClass = "bnode";
        private const String LiteralClass = "literal";
        private const String DatatypeClass = "datatype";
        private const String LangClass = "langspec";
        private String _uriPrefix = String.Empty;

        private readonly HtmlFormatter _formatter = new HtmlFormatter();
        private INamespaceMapper _namespaces = new NamespaceMapper();
        private QNameOutputMapper _qnameMapper;
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
        /// Write start of the document
        /// </summary>
        protected override void StartResultsInternal()
        {
            using (var writer = new HtmlTextWriter(new System.IO.StringWriter()))
            {
                _qnameMapper = new QNameOutputMapper(_namespaces ?? new NamespaceMapper(true));

                //Page Header
                writer.Write(
                    "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
                writer.RenderBeginTag(HtmlTextWriterTag.Html);
                writer.RenderBeginTag(HtmlTextWriterTag.Head);
                writer.RenderBeginTag(HtmlTextWriterTag.Title);
                writer.WriteEncodedText("SPARQL Query Results");
                writer.RenderEndTag();

                //TODO: Add <meta>
                writer.RenderEndTag();

                //Start Body
                writer.RenderBeginTag(HtmlTextWriterTag.Body);

                AddToQueue(writer.InnerWriter.ToString());
            }
        }

        /// <summary>
        /// Write end of the document
        /// </summary>
        /// <param name="ok"></param>
        protected override void EndResultsInternal(bool ok)
        {
            using (var writer = new HtmlTextWriter(new System.IO.StringWriter()))
            {
                if (!_firstResult)
                {
                    //End Table Body
                    writer.Write("</tbody>");

                    //End Table
                    writer.Write("</table>");
                }

                //End of Page
                writer.Write("</body>"); //End Body
                writer.Write("</html>"); //End Html

                AddToQueue(writer.InnerWriter.ToString());
                CompleteQueue();
            }
        }

        /// <summary>
        /// Method to handle Boolean result
        /// </summary>
        /// <param name="result"></param>
        protected override void HandleBooleanResultInternal(bool result)
        {
            using (var writer = new HtmlTextWriter(new System.IO.StringWriter()))
            {
                //Show a Header and a Boolean value
                writer.RenderBeginTag(HtmlTextWriterTag.H3);
                writer.WriteEncodedText("ASK Query Result");
                writer.RenderEndTag();
                writer.RenderBeginTag(HtmlTextWriterTag.P);
                writer.WriteEncodedText(result.ToString());
                writer.RenderEndTag();

                AddToQueue(writer.InnerWriter.ToString());
            }
        }

        /// <summary>
        /// Parse incoming SparqlResult (one row) to html table
        /// </summary>
        /// <param name="result">SparqlResult</param>
        /// <returns></returns>
        protected override bool HandleResultInternal(SparqlResult result)
        {
            using (var writer = new HtmlTextWriter(new System.IO.StringWriter()))
            {
                //Write headers and output variables
                if (_firstResult)
                {
                    //Create a Table for the results
                    writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
                    writer.RenderBeginTag(HtmlTextWriterTag.Table);

                    //Create a Table Header with the Variable Names
                    writer.RenderBeginTag(HtmlTextWriterTag.Thead);
                    writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                    foreach (String var in result.Variables)
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.Th);
                        writer.WriteEncodedText(var);
                        writer.RenderEndTag();
                    }

                    writer.RenderEndTag();
                    writer.RenderEndTag();
#if !NO_WEB
                    writer.WriteLine();
#endif

                    //Create a Table Body for the Results
                    writer.RenderBeginTag(HtmlTextWriterTag.Tbody);

                    _firstResult = false;
                }

                //Start Row
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);

                foreach (String var in result.Variables)
                {
                    //Start Column
                    writer.RenderBeginTag(HtmlTextWriterTag.Td);

                    if (result.HasValue(var))
                    {
                        INode value = result[var];

                        if (value != null)
                        {
                            switch (value.NodeType)
                            {
                                case NodeType.Blank: // Write blank node
                                    writer.AddAttribute(HtmlTextWriterAttribute.Class, BnodeClass);
                                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                    writer.WriteEncodedText(value.ToString());
                                    writer.RenderEndTag();
                                    break;

                                case NodeType.Literal:
                                    var lit = (ILiteralNode) W3CSpecHelper.FormatNode(value); // Format by W3C spec.
                                    writer.AddAttribute(HtmlTextWriterAttribute.Class, LiteralClass);
                                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                    if (lit.DataType != null) // Set datatype 
                                    {
                                        writer.WriteEncodedText(lit.Value);
                                        writer.RenderEndTag();
                                        writer.WriteEncodedText("^^");
                                        writer.AddAttribute(HtmlTextWriterAttribute.Href,
                                            _formatter.FormatUri(lit.DataType.AbsoluteUri));
                                        writer.AddAttribute(HtmlTextWriterAttribute.Class, DatatypeClass);
                                        writer.RenderBeginTag(HtmlTextWriterTag.A);
                                        writer.WriteEncodedText(lit.DataType.ToString());
                                        writer.RenderEndTag();
                                    }
                                    else
                                    {
                                        writer.WriteEncodedText(lit.Value);
                                        if (!lit.Language.Equals(String.Empty)) // Set language
                                        {
                                            writer.RenderEndTag();
                                            writer.WriteEncodedText("@");
                                            writer.AddAttribute(HtmlTextWriterAttribute.Class, LangClass);
                                            writer.RenderBeginTag(HtmlTextWriterTag.Span);
                                            writer.WriteEncodedText(lit.Language);
                                            writer.RenderEndTag();
                                        }
                                        else
                                        {
                                            writer.RenderEndTag();
                                        }
                                    }
                                    break;

                                case NodeType.GraphLiteral:
                                    //Error
                                    throw new RdfOutputException(
                                        "Result Sets which contain Graph Literal Nodes cannot be serialized in the HTML Format");

                                case NodeType.Uri: // Write Uri as link
                                    writer.AddAttribute(HtmlTextWriterAttribute.Class, UriClass);
                                    writer.AddAttribute(HtmlTextWriterAttribute.Href,
                                        _formatter.FormatUri(UriPrefix + value.ToString()));
                                    writer.RenderBeginTag(HtmlTextWriterTag.A);

                                    String qname;
                                    if (_qnameMapper.ReduceToQName(value.ToString(), out qname))
                                    {
                                        writer.WriteEncodedText(qname);
                                    }
                                    else
                                    {
                                        writer.WriteEncodedText(value.ToString());
                                    }
                                    writer.RenderEndTag();
                                    break;

                                default:
                                    throw new RdfOutputException(
                                        "Result which contain Unknown Node Types cannot be serialized in the HTML Format");
                            }
                        }
                        else
                        {
                            writer.WriteEncodedText(" ");
                        }
                    }
                    else
                    {
                        writer.WriteEncodedText(" ");
                    }

                    //End Column
                    writer.RenderEndTag();
                }

                //End Row
                writer.RenderEndTag();
#if !NO_WEB
                writer.WriteLine();
#endif

                AddToQueue(writer.InnerWriter.ToString());
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

