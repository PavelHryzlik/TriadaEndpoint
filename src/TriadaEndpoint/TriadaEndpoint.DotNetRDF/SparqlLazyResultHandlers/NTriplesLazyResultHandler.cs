using System.Text;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF.Query;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.DotNetRDF.SparqlLazyResultHandlers
{
    /// <summary>
    /// Handler formatting SparqlResults to NTriples representation
    /// </summary>
    public class NTriplesLazyResultHandler : LazyResultHandler
    {
        private readonly StringBuilder _resultItem;

        private readonly NTriples11Formatter _formatter = new NTriples11Formatter();

        /// <summary>
        /// Handler constructor
        /// </summary>
        public NTriplesLazyResultHandler()
        {
            _resultItem = new StringBuilder();
        }

        protected override void EndResultsInternal(bool ok)
        {
            CompleteQueue();
        }

        /// <summary>
        /// Method to handle Boolean result
        /// </summary>
        /// <param name="result"></param>
        protected override void HandleBooleanResultInternal(bool result)
        {
            AddToQueue(result.ToString());
        }

        /// <summary>
        /// Parse incoming SparqlResult (one row) to Turtle
        /// </summary>
        /// <param name="result">SparqlResult</param>
        /// <returns></returns>
        protected override bool HandleResultInternal(SparqlResult result)
        {
            _resultItem.Clear();

            foreach (var var in result.Variables)
            {
                if (result.HasValue(var))
                {
                    _resultItem.Append(_formatter.Format(W3CSpecHelper.FormatNode(result.Value(var))) + " "); // Format by W3C spec.
                }
            }

            _resultItem.Append(".");
            _resultItem.AppendLine();

            AddToQueue(_resultItem.ToString());

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
