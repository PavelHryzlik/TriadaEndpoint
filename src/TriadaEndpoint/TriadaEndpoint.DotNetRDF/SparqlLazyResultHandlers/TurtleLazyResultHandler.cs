using System;
using System.Text;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.DotNetRDF.SparqlLazyResultHandlers
{
    /// <summary>
    /// Handler formatting SparqlResults to Turtle representation
    /// </summary>
    public class TurtleLazyResultHandler : LazyResultHandler
    {
        private readonly StringBuilder _resultItem;
        private readonly TurtleW3CFormatter _formatter = new TurtleW3CFormatter();
        private bool _firstResult = true;

        /// <summary>
        /// Handler constructor
        /// </summary>
        public TurtleLazyResultHandler()
        {
            _resultItem = new StringBuilder();
        }

        /// <summary>
        /// Write start of the document
        /// </summary>
        protected override void StartResultsInternal()
        {
            _resultItem.Clear();

            // base prefixes
            _resultItem.AppendLine("@prefix rdf:" + ": <" + _formatter.FormatUri("http://www.w3.org/1999/02/22-rdf-syntax-ns#") + ">.");
            _resultItem.AppendLine("@prefix rdf:" + ": <" + _formatter.FormatUri("http://www.w3.org/2000/01/rdf-schema#") + ">.");
            _resultItem.AppendLine("@prefix rdf:" + ": <" + _formatter.FormatUri("http://www.w3.org/2001/XMLSchema#") + ">.");
            _resultItem.AppendLine("@prefix rdf:" + ": <" + _formatter.FormatUri(SparqlSpecsHelper.SparqlRdfResultsNamespace) + ">.");
            _resultItem.AppendLine();
            _resultItem.AppendLine("_:_ a res:ResultSet .");

            AddToQueue(_resultItem.ToString());
        }

        /// <summary>
        /// Write end of the document
        /// </summary>
        /// <param name="ok"></param>
        protected override void EndResultsInternal(bool ok)
        {
            CompleteQueue();
        }

        protected override void HandleBooleanResultInternal(bool result)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Parse incoming SparqlResult (one row) to Turtle
        /// </summary>
        /// <param name="result">SparqlResult</param>
        /// <returns></returns>
        protected override bool HandleResultInternal(SparqlResult result)
        {
            _resultItem.Clear();
            var stringBuilder = new StringBuilder();

            //Write output variables first
            if (_firstResult)
            {
                stringBuilder.Append("_:_ res:resultVariable ");
                foreach (String var in result.Variables)
                {
                    //<variable> element
                    stringBuilder.Append("\"" + var + "\", ");
                }
                stringBuilder.Replace(",", ".", stringBuilder.Length - 2, 1);
                _resultItem.AppendLine(stringBuilder.ToString());

                _firstResult = false;
            }

            stringBuilder.Clear();
            stringBuilder.Append("_:_ a res:solution [ ");

            foreach (String var in result.Variables)
            {
                stringBuilder.AppendLine();

                if (result.HasValue(var))
                {
                    stringBuilder.Append("\t\tres:binding [ ");
                    stringBuilder.Append("res:variable \"" + var + "\" ; ");
                    stringBuilder.Append("res:value ");

                    INode n = W3CSpecHelper.FormatNode(result.Value(var)); // Format by W3C spec.
                    stringBuilder.Append(_formatter.Format(n));

                    stringBuilder.Append(" ] ; ");
                }
            }

            stringBuilder.Replace(";", "] .", stringBuilder.Length - 2, 1);
            stringBuilder.AppendLine();
            _resultItem.Append(stringBuilder);

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
