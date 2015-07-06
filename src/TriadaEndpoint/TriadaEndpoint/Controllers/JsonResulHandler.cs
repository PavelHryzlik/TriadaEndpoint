﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace TriadaEndpoint.Controllers
{
    public class JsonResultHandler : BaseResultsHandler
    {
        private readonly JsonTextWriter _writter;
        private readonly bool _closeOutput;
        private bool _firstResult = true;

        public JsonResultHandler(TextWriter output, bool closeOutput)
        {
            _writter = new JsonTextWriter(output) {Formatting = Formatting.Indented};
            _closeOutput = closeOutput;
        }

        protected override void StartResultsInternal()
        {
            //Start a Json Object for the Result Set
            _writter.WriteStartObject();

            //Create the Head Object
            _writter.WritePropertyName("head");
            _writter.WriteStartObject();
        }

        protected override void EndResultsInternal(bool ok)
        {
            if (!_firstResult)
            {
                //End Result Object
                _writter.WriteEndArray();
                _writter.WriteEndObject();
            }

            //End the Json Object for the Result Set
            _writter.WriteEndObject();

            if (_closeOutput)
                _writter.Close();
        }

        protected override void HandleBooleanResultInternal(bool result)
        {
            //ASK query result

            //Set an empty Json Object in the Head
            _writter.WriteEndObject();

            //Create a Boolean Property
            _writter.WritePropertyName("boolean");
            _writter.WriteValue(result);
        }

        protected override bool HandleResultInternal(SparqlResult result)
        {
            if (_firstResult)
            {
                //SELECT query results

                //Create the Variables Object
                _writter.WritePropertyName("vars");
                _writter.WriteStartArray();
                foreach (String var in result.Variables)
                {
                    _writter.WriteValue(var);
                }
                _writter.WriteEndArray();

                //End Head Object
                _writter.WriteEndObject();

                //Create the Result Object
                _writter.WritePropertyName("results");
                _writter.WriteStartObject();
                _writter.WritePropertyName("bindings");
                _writter.WriteStartArray();

                _firstResult = false;
            }

            //Create a Binding Object
            _writter.WriteStartObject();
            foreach (String var in result.Variables)
            {
                if (!result.HasValue(var)) continue; //No output for unbound variables

                INode value = result.Value(var);
                if (value == null) continue;

                //Create an Object for the Variable
                _writter.WritePropertyName(var);
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
                        var lit = (ILiteralNode)W3CSpecHelper.FormatNode(value);
                        if (lit.DataType != null)
                        {
                            _writter.WriteValue("typed-literal");
                        }
                        else
                        {
                            _writter.WriteValue("literal");
                        }
                        _writter.WritePropertyName("value");

                        _writter.WriteValue(lit.Value);
                        if (!lit.Language.Equals(String.Empty))
                        {
                            _writter.WritePropertyName("xml:lang");
                            _writter.WriteValue(lit.Language);
                        }
                        else if (lit.DataType != null)
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

        protected override bool HandleVariableInternal(string var)
        {
            return true;
        }
    }
}