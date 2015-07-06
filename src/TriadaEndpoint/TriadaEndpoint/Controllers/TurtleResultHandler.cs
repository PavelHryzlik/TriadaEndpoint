using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Contexts;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.Controllers
{
    public class TurtleResultHandler : BaseResultsHandler
    {
        private readonly TextWriter _writter;
        private readonly TurtleW3CFormatter _formatter = new TurtleW3CFormatter();
        private readonly bool _closeOutput;
        private bool _firstResult = true;

        public TurtleResultHandler(TextWriter output, bool closeOutput)
        {
            _writter = output;
            _closeOutput = closeOutput;
        }

        protected override void StartResultsInternal()
        {
            _writter.WriteLine("@prefix rdf:" + ": <" + _formatter.FormatUri("http://www.w3.org/1999/02/22-rdf-syntax-ns#") + ">.");
            _writter.WriteLine("@prefix rdf:" + ": <" + _formatter.FormatUri("http://www.w3.org/2000/01/rdf-schema#") + ">.");
            _writter.WriteLine("@prefix rdf:" + ": <" + _formatter.FormatUri("http://www.w3.org/2001/XMLSchema#") + ">.");
            _writter.WriteLine("@prefix rdf:" + ": <" + _formatter.FormatUri(SparqlSpecsHelper.SparqlRdfResultsNamespace) + ">.");
            _writter.WriteLine();
            _writter.WriteLine("_:_ a res:ResultSet .");
        }

        protected override void EndResultsInternal(bool ok)
        {
            if (_closeOutput)
                _writter.Close();
        }

        protected override void HandleBooleanResultInternal(bool result)
        {
            //TODO
        }

        protected override bool HandleResultInternal(SparqlResult result)
        {
            var stringBuilder = new StringBuilder();

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

                    INode n = W3CSpecHelper.FormatNode(result.Value(var));
                    stringBuilder.Append(_formatter.Format(n));

                    stringBuilder.Append(" ] ; ");
                }
            }

            stringBuilder.Replace(";", "] .", stringBuilder.Length - 2, 1);
            stringBuilder.AppendLine();
            _writter.Write(stringBuilder);

            return true;
        }

        protected override bool HandleVariableInternal(string var)
        {
            return true;
        }
    }
}