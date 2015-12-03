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
    /// <summary>
    /// Handler formatting SparqlResults to Csv representation
    /// </summary>
    public class CsvResultHandler : BaseResultsHandler
    {
        private readonly TextWriter _writter;
        private readonly CsvFormatter _formatter = new CsvFormatter();
        private readonly bool _closeOutput;
        private bool _firstResult = true;

        /// <summary>
        /// Handler constructor
        /// </summary>
        /// <param name="output">Input Text writter</param>
        /// <param name="closeOutput">Indicates whether to close writter at the end</param>
        public CsvResultHandler(TextWriter output, bool closeOutput)
        {
            _writter = output;
            _closeOutput = closeOutput;
        }

        /// <summary>
        /// Write end of the document
        /// </summary>
        /// <param name="ok"></param>
        protected override void EndResultsInternal(bool ok)
        {
            if (_closeOutput)
                _writter.Close();
        }

        /// <summary>
        /// Method to handle Boolean result
        /// </summary>
        /// <param name="result"></param>
        protected override void HandleBooleanResultInternal(bool result)
        {
            _writter.Write(result.ToString());
        }

        /// <summary>
        /// Parse incoming SparqlResult (one row) to CSV
        /// </summary>
        /// <param name="result">SparqlResult</param>
        /// <returns></returns>
        protected override bool HandleResultInternal(SparqlResult result)
        {           
            String[] vars = result.Variables.ToArray();
            if (_firstResult)
            {
                //Write output variables first
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
                    INode temp = W3CSpecHelper.FormatNode(result[vars[i]]); // Format by W3C spec.
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