using System;
using Newtonsoft.Json;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace TriadaEndpoint.DotNetRDF.SparqlLazyResultHandlers
{
    /// <summary>
    /// Handler formatting SparqlResults to Json representation
    /// </summary>
    public class JsonLazyResultHandler : LazyResultHandler
    {
        private bool _firstResult = true;

        /// <summary>
        /// Write start of the document
        /// </summary>
        protected override void StartResultsInternal()
        {
            var jsonString = new System.IO.StringWriter();
            using (var writer = new JsonTextWriter(jsonString))
            {
                //Start a Json Object for the Result Set
                writer.WriteStartObject();

                //Create the Head Object
                writer.WritePropertyName("head");
                writer.WriteStartObject();

                AddToQueue(jsonString.ToString());
            }
        }

        /// <summary>
        /// Write end of the document
        /// </summary>
        /// <param name="ok"></param>
        protected override void EndResultsInternal(bool ok)
        {
            var jsonString = new System.IO.StringWriter();
            using (var writer = new JsonTextWriter(jsonString))
            {
                if (!_firstResult)
                {
                    //End Result Object
                    writer.WriteRawValue("]");
                    writer.WriteRawValue("}");
                }

                //End the Json Object for the Result Set
                writer.WriteRawValue("}");

                AddToQueue(jsonString.ToString());
                CompleteQueue();
            }
        }

        /// <summary>
        /// Method to handle Boolean result
        /// </summary>
        /// <param name="result"></param>
        protected override void HandleBooleanResultInternal(bool result)
        {
            var jsonString = new System.IO.StringWriter();
            using (var writer = new JsonTextWriter(jsonString))
            {
                //ASK query result

                //Set an empty Json Object in the Head
                writer.WriteRawValue("}");

                //Create a Boolean Property
                writer.WritePropertyName("boolean");
                writer.WriteValue(result);

                AddToQueue(jsonString.ToString());
            }
        }

        /// <summary>
        /// Parse incoming SparqlResult (one row) to Json
        /// </summary>
        /// <param name="result">SparqlResult</param>
        /// <returns></returns>
        protected override bool HandleResultInternal(SparqlResult result)
        {
            var jsonString = new System.IO.StringWriter();
            using (var writer = new JsonTextWriter(jsonString))
            {
                //Write output variables first
                if (_firstResult)
                {
                    //Create the Variables Object
                    writer.WritePropertyName("vars");
                    writer.WriteStartArray();
                    foreach (String var in result.Variables)
                    {
                        writer.WriteValue(var);
                    }
                    writer.WriteEndArray();

                    //End Head Object
                    writer.WriteRawValue("}");

                    //Create the Result Object
                    writer.WritePropertyName("results");
                    writer.WriteStartObject();
                    writer.WritePropertyName("bindings");
                    writer.WriteStartArray();

                    _firstResult = false;
                }
                else
                    writer.WriteRawValue(",");

                //Create a Binding Object
                writer.WriteStartObject();
                foreach (String var in result.Variables)
                {
                    if (!result.HasValue(var)) continue; //No output for unbound variables

                    INode value = result.Value(var);
                    if (value == null) continue;

                    //Create an Object for the Variable
                    writer.WritePropertyName(var);
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
                            if (lit.DataType != null)
                            {
                                writer.WriteValue("typed-literal");
                            }
                            else
                            {
                                writer.WriteValue("literal");
                            }
                            writer.WritePropertyName("value");

                            writer.WriteValue(lit.Value);
                            if (!lit.Language.Equals(String.Empty)) // Set language
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
