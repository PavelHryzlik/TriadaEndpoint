using System;
using System.IO;
using System.Text;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.DotNetRDF.SparqlResultHandlers
{
    /// <summary>
    /// Handler formatting SparqlResults to Turtle representation
    /// </summary>
    public class TurtleResultHandler : BaseResultsHandler
    {
        private readonly TextWriter _writter;
        private readonly TurtleW3CFormatter _formatter = new TurtleW3CFormatter();
        private readonly bool _closeOutput;
        private bool _firstResult = true;

        /// <summary>
        /// Handler constructor
        /// </summary>
        /// <param name="output">Input Text writter</param>
        /// <param name="closeOutput">Indicates whether to close writter at the end</param>
        public TurtleResultHandler(TextWriter output, bool closeOutput)
        {
            _writter = output;
            _closeOutput = closeOutput;
        }

        /// <summary>
        /// Write start of the document
        /// </summary>
        protected override void StartResultsInternal()
        {
            // base prefixes
            _writter.WriteLine("@prefix rdf:" + ": <" + _formatter.FormatUri("http://www.w3.org/1999/02/22-rdf-syntax-ns#") + ">.");
            _writter.WriteLine("@prefix rdf:" + ": <" + _formatter.FormatUri("http://www.w3.org/2000/01/rdf-schema#") + ">.");
            _writter.WriteLine("@prefix rdf:" + ": <" + _formatter.FormatUri("http://www.w3.org/2001/XMLSchema#") + ">.");
            _writter.WriteLine("@prefix rdf:" + ": <" + _formatter.FormatUri(SparqlSpecsHelper.SparqlRdfResultsNamespace) + ">.");
            _writter.WriteLine();
            _writter.WriteLine("_:_ a res:ResultSet .");
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
                _writter.WriteLine(stringBuilder);

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
            _writter.Write(stringBuilder);

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