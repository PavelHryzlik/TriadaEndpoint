using System;
using System.Linq;
using System.Text;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.DotNetRDF.SparqlLazyResultHandlers
{
    /// <summary>
    /// Handler formatting SparqlResults to Csv representation
    /// </summary>
    public class CsvLazyResultHandler : LazyResultHandler
    {
        private readonly StringBuilder _resultItem;
        private readonly CsvFormatter _formatter = new CsvFormatter();
        private bool _firstResult = true;

        /// <summary>
        /// Handler constructor
        /// </summary>
        public CsvLazyResultHandler()
        {
            _resultItem = new StringBuilder();
        }

        /// <summary>
        /// Write end of the document
        /// </summary>
        /// <param name="ok"></param>
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
        /// Parse incoming SparqlResult (one row) to CSV
        /// </summary>
        /// <param name="result">SparqlResult</param>
        /// <returns></returns>
        protected override bool HandleResultInternal(SparqlResult result)
        {
            _resultItem.Clear();

            String[] vars = result.Variables.ToArray();
            if (_firstResult)
            {
                //Write output variables first
                for (int i = 0; i < vars.Length; i++)
                {
                    _resultItem.Append(vars[i]);
                    if (i < vars.Length - 1) _resultItem.Append(',');
                }
                _resultItem.Append("\r\n");

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
                                _resultItem.Append(_formatter.Format(temp));
                                break;
                            case NodeType.GraphLiteral:
                                throw new RdfOutputException(WriterErrorMessages.GraphLiteralsUnserializable("SPARQL CSV"));
                            default:
                                throw new RdfOutputException(WriterErrorMessages.UnknownNodeTypeUnserializable("SPARQL CSV"));
                        }
                    }
                }
                if (i < vars.Length - 1) _resultItem.Append(',');
            }
            _resultItem.Append("\r\n");

            AddToQueue(result.ToString());

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
