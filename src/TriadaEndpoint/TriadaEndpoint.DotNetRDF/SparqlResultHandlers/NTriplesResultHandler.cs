using System.IO;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.DotNetRDF.SparqlResultHandlers
{
    /// <summary>
    /// Handler formatting SparqlResults to NTriples representation
    /// </summary>
    public class NTriplesResultHandler : BaseResultsHandler
    {
        private readonly TextWriter _writter;
        private readonly NTriples11Formatter _formatter = new NTriples11Formatter();
        private readonly bool _closeOutput;

        /// <summary>
        /// Handler constructor
        /// </summary>
        /// <param name="output">Input Text writter</param>
        /// <param name="closeOutput">Indicates whether to close writter at the end</param>
        public NTriplesResultHandler(TextWriter output, bool closeOutput)
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
        /// Parse incoming SparqlResult (one row) to Turtle
        /// </summary>
        /// <param name="result">SparqlResult</param>
        /// <returns></returns>
        protected override bool HandleResultInternal(SparqlResult result)
        {
            foreach (var var in result.Variables)
            {
                if (result.HasValue(var))
                {
                    _writter.Write(_formatter.Format(W3CSpecHelper.FormatNode(result.Value(var))) + " "); // Format by W3C spec.
                }
            }

            _writter.Write(".");
            _writter.WriteLine();

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