using System;
using System.IO;
using Newtonsoft.Json;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Writing;

namespace TriadaEndpoint.DotNetRDF.RdfHandlers
{
    /// <summary>
    /// Handler formatting Triples (RDF graph) to Json representation
    /// </summary>
    public class JsonRdfHandler : BaseRdfHandler
    {
        private readonly JsonTextWriter _writter;
        private readonly bool _closeOnEnd;

        private Uri _baseUri;
        public Uri BaseUri => _baseUri;

        /// <summary>
        /// Handler constructor
        /// </summary>
        /// <param name="output">Input Text writter</param>
        /// <param name="closeOnEnd">Indicates whether to close writter at the end</param>
        public JsonRdfHandler(TextWriter output, bool closeOnEnd)
        {
            _writter = new JsonTextWriter(output);
            _closeOnEnd = closeOnEnd;
        }

        protected override void StartRdfInternal()
        {
            //Start a Json Object for the Result Set
            _writter.WriteStartObject();

            //Create the Head Object
            _writter.WritePropertyName("head");
            _writter.WriteStartObject();

            //SELECT query results

            //Create the Variables Object
            _writter.WritePropertyName("vars");
            _writter.WriteStartArray();
            _writter.WriteValue("s");
            _writter.WriteValue("p");
            _writter.WriteValue("o");
            _writter.WriteEndArray();

            //End Head Object
            _writter.WriteEndObject();

            //Create the Result Object
            _writter.WritePropertyName("results");
            _writter.WriteStartObject();
            _writter.WritePropertyName("bindings");
            _writter.WriteStartArray();
        }

        protected override void EndRdfInternal(bool ok)
        {
            //End Result Object
            _writter.WriteEndArray();
            _writter.WriteEndObject();

            //End the Json Object for the Result Set
            _writter.WriteEndObject();

            if (_closeOnEnd)
                _writter.Close();
        }

        /// <summary>
        /// Parse incoming Triple to Json
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        protected override bool HandleTripleInternal(Triple t)
        {
            //Create a Binding Object
            _writter.WriteStartObject();
            foreach (INode value in t.Nodes)
            {
                if (value == null) continue;

                //Create an Object for the Variable
                if (value.ToString() == t.Subject.ToString())
                    _writter.WritePropertyName("s");
                else if (value.ToString() == t.Predicate.ToString())
                    _writter.WritePropertyName("p");
                else if (value.ToString() == t.Object.ToString())
                    _writter.WritePropertyName("o");

                _writter.WriteStartObject();
                _writter.WritePropertyName("type");

                switch (value.NodeType)
                {
                    case NodeType.Blank:
                        //Blank Node
                        _writter.WriteValue("bnode");
                        _writter.WritePropertyName("value");
                        String id = ((IBlankNode)value).InternalID;
                        id = id.Substring(id.IndexOf(':') + 1);
                        _writter.WriteValue(id);
                        break;

                    case NodeType.GraphLiteral:
                        //Error
                        throw new RdfOutputException("Result Sets which contain Graph Literal Nodes cannot be serialized in the SPARQL Query Results JSON Format");

                    case NodeType.Literal:
                        //Literal
                        var lit = (ILiteralNode)W3CSpecHelper.FormatNode(value); // Format by W3C spec.

                        _writter.WriteValue(lit.DataType != null ? "typed-literal" : "literal");

                        _writter.WritePropertyName("value");

                        _writter.WriteValue(lit.Value);
                        if (!lit.Language.Equals(String.Empty)) // Set language attribute
                        {
                            _writter.WritePropertyName("xml:lang");
                            _writter.WriteValue(lit.Language);
                        }
                        else if (lit.DataType != null) // Set datatype 
                        {
                            _writter.WritePropertyName("datatype");
                            _writter.WriteValue(lit.DataType.AbsoluteUri);
                        }
                        break;

                    case NodeType.Uri:
                        //Uri
                        _writter.WriteValue("uri");
                        _writter.WritePropertyName("value");
                        _writter.WriteValue(value.ToString());
                        break;

                    default:
                        throw new RdfOutputException("Result Sets which contain Nodes of unknown Type cannot be serialized in the SPARQL Query Results JSON Format");
                }

                //End the Variable Object
                _writter.WriteEndObject();
            }
            //End the Binding Object
            _writter.WriteEndObject();

            return true;
        }

        /// <summary>
        /// Method to handle Base Uri
        /// </summary>
        /// <param name="baseUri">Variable</param>
        /// <returns></returns>
        protected override bool HandleBaseUriInternal(Uri baseUri)
        {
            _baseUri = baseUri;
            return true;
        }

        public override bool AcceptsAll => true;
    }
}