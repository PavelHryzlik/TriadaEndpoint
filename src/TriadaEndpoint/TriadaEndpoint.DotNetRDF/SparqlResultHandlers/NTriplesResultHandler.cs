using System.IO;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.DotNetRDF.SparqlResultHandlers
{
    public class NTriplesResultHandler : BaseResultsHandler
    {
        private readonly TextWriter _writter;
        private readonly NTriples11Formatter _formatter = new NTriples11Formatter();
        private readonly bool _closeOutput;

        public NTriplesResultHandler(TextWriter output, bool closeOutput)
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
            foreach (var var in result.Variables)
            {
                if (result.HasValue(var))
                {
                    _writter.Write(_formatter.Format(W3CSpecHelper.FormatNode(result.Value(var))) + " ");
                }
            }

            _writter.Write(".");
            _writter.WriteLine();

            return true;
        }

        protected override bool HandleVariableInternal(string var)
        {
            return true;
        }
    }
}