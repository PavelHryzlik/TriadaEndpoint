using System;
using Newtonsoft.Json;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Writing;

namespace TriadaEndpoint.DotNetRDF.LazyRdfHandlers
{
    /// <summary>
    /// Handler formatting Triples (RDF graph) to Json representation
    /// </summary>
    public class JsonLazyRdfHandler : LazyRdfHandler
    {
        private Uri _baseUri;
        public Uri BaseUri => _baseUri;
        private bool _firstResult = true;

        protected override void StartRdfInternal()
        {
            var jsonString = new System.IO.StringWriter();
            using (var writer = new JsonTextWriter(jsonString))
            {
                //Start a Json Object for the Result Set
                writer.WriteStartObject();

                //Create the Head Object
                writer.WritePropertyName("head");
                writer.WriteStartObject();

                //SELECT query results

                //Create the Variables Object
                writer.WritePropertyName("vars");
                writer.WriteStartArray();
                writer.WriteValue("s");
                writer.WriteValue("p");
                writer.WriteValue("o");
                writer.WriteEndArray();

                //End Head Object
                writer.WriteEndObject();

                //Create the Result Object
                writer.WritePropertyName("results");
                writer.WriteStartObject();
                writer.WritePropertyName("bindings");
                writer.WriteStartArray();

                AddToQueue(jsonString.ToString());
            }
        }

        protected override void EndRdfInternal(bool ok)
        {
            var jsonString = new System.IO.StringWriter();
            using (var writer = new JsonTextWriter(jsonString))
            {
                //End Result Object
                writer.WriteRawValue("]");
                writer.WriteRawValue("}");

                //End the Json Object for the Result Set
                writer.WriteRawValue("}");

                AddToQueue(jsonString.ToString());
                CompleteQueue();
            }
        }

        /// <summary>
        /// Parse incoming Triple to Json
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        protected override bool HandleTripleInternal(Triple t)
        {
            var jsonString = new System.IO.StringWriter();
            using (var writer = new JsonTextWriter(jsonString))
            {
                if (_firstResult)
                    _firstResult = false;
                else
                    writer.WriteRawValue(",");

                //Create a Binding Object
                writer.WriteStartObject();
                foreach (INode value in t.Nodes)
                {
                    if (value == null) continue;

                    //Create an Object for the Variable
                    if (value.ToString() == t.Subject.ToString())
                        writer.WritePropertyName("s");
                    else if (value.ToString() == t.Predicate.ToString())
                        writer.WritePropertyName("p");
                    else if (value.ToString() == t.Object.ToString())
                        writer.WritePropertyName("o");

                    writer.WriteStartObject();
                    writer.WritePropertyName("type");

                    switch (value.NodeType)
                    {
                        case NodeType.Blank:
                            //Blank Node
                            writer.WriteValue("bnode");
                            writer.WritePropertyName("value");
                            String id = ((IBlankNode) value).InternalID;
                            id = id.Substring(id.IndexOf(':') + 1);
                            writer.WriteValue(id);
                            break;

                        case NodeType.GraphLiteral:
                            //Error
                            throw new RdfOutputException(
                                "Result Sets which contain Graph Literal Nodes cannot be serialized in the SPARQL Query Results JSON Format");

                        case NodeType.Literal:
                            //Literal
                            var lit = (ILiteralNode) W3CSpecHelper.FormatNode(value); // Format by W3C spec.

                            writer.WriteValue(lit.DataType != null ? "typed-literal" : "literal");

                            writer.WritePropertyName("value");

                            writer.WriteValue(lit.Value);
                            if (!lit.Language.Equals(String.Empty)) // Set language attribute
                            {
                                writer.WritePropertyName("xml:lang");
                                writer.WriteValue(lit.Language);
                            }
                            else if (lit.DataType != null) // Set datatype 
                            {
                                writer.WritePropertyName("datatype");
                                writer.WriteValue(lit.DataType.AbsoluteUri);
                            }
                            break;

                        case NodeType.Uri:
                            //Uri
                            writer.WriteValue("uri");
                            writer.WritePropertyName("value");
                            writer.WriteValue(value.ToString());
                            break;

                        default:
                            throw new RdfOutputException(
                                "Result Sets which contain Nodes of unknown Type cannot be serialized in the SPARQL Query Results JSON Format");
                    }

                    //End the Variable Object
                    writer.WriteEndObject();
                }
                //End the Binding Object
                writer.WriteEndObject();

                AddToQueue(jsonString.ToString());
            }

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
