using System;
using System.IO;
using System.Linq;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.DotNetRDF.SparqlResultHandlers
{
    public class CsvResultHandler : BaseResultsHandler
    {
        private readonly TextWriter _writter;
        private readonly CsvFormatter _formatter = new CsvFormatter();
        private readonly bool _closeOutput;
        private bool _firstResult = true;

        public CsvResultHandler(TextWriter output, bool closeOutput)
        {
            _writter = output;
            _closeOutput = closeOutput;
        }

        protected override void EndResultsInternal(bool ok)
        {
            if (_closeOutput)
                _writter.Close();
        }


        protected override void HandleBooleanResultInternal(bool result)
        {
            _writter.Write(result.ToString());
        }

        protected override bool HandleResultInternal(SparqlResult result)
        {           
            String[] vars = result.Variables.ToArray();
            if (_firstResult)
            {
                //Output Variables first
                for (int i = 0; i < vars.Length; i++)
                {
                    _writter.Write(vars[i]);
                    if (i < vars.Length - 1) _writter.Write(',');
                }
                _writter.Write("\r\n");

                _firstResult = false;
            }

            for (int i = 0; i < vars.Length; i++)
            {
                if (result.HasValue(vars[i]))
                {
                    INode temp = W3CSpecHelper.FormatNode(result[vars[i]]);
                    if (temp != null)
                    {
                        switch (temp.NodeType)
                        {
                            case NodeType.Blank:
                            case NodeType.Uri:
                            case NodeType.Literal:
                                _writter.Write(_formatter.Format(temp));
                                break;
                            case NodeType.GraphLiteral:
                                throw new RdfOutputException(WriterErrorMessages.GraphLiteralsUnserializable("SPARQL CSV"));
                            default:
                                throw new RdfOutputException(WriterErrorMessages.UnknownNodeTypeUnserializable("SPARQL CSV"));
                        }
                    }
                }
                if (i < vars.Length - 1) _writter.Write(',');
            }
            _writter.Write("\r\n");

            return true;
        }

        protected override bool HandleVariableInternal(string var)
        {
            return true;
        }
    }
}