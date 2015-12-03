using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using TriadaEndpoint.DotNetRDF.Formatters;
using TriadaEndpoint.DotNetRDF.RdfHandlers;
using TriadaEndpoint.DotNetRDF.SparqlResultHandlers;
using TriadaEndpoint.Web.Models;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Writing.Formatting;
using TriadaEndpoint.Web.Utils;

namespace TriadaEndpoint.Web.R2Rml
{
    /// <summary>
    /// Class processing SPARQL queries
    /// </summary>
    public class QueryProcessor
    {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Procces SPARQL query asynchronously
        /// </summary>
        /// <param name="sparqlQuery">SPARQL query</param>
        /// <param name="resultFormats">Output format</param>
        /// <returns></returns>
        public Stream ProcessQuery(string sparqlQuery, ResultFormats resultFormats)
        {
            var stopWatch = Stopwatch.StartNew();

            // Asynchronously execute query, threads connected with pipes - need revision
            var serverPipe = new AnonymousPipeServerStream(PipeDirection.Out);
            Task.Factory.StartNew(() => Query(serverPipe, sparqlQuery, resultFormats)).OnException(ex => _log.Error(ExceptionHelper.ParseMultiException((AggregateException)ex)));
            var clientPipe = new AnonymousPipeClientStream(PipeDirection.In, serverPipe.ClientSafePipeHandle);

            stopWatch.Stop();
            _log.Info("SparqlResult in " + stopWatch.ElapsedMilliseconds + "ms");

            return clientPipe;
        }

        /// <summary>
        /// Execute SPARQL query and write result to stream
        /// SELECT queries generates SparqlResult and needs ISparqlResultsHandler
        /// CONSTRUCT ueries generates Triples and needs IRdfHandler
        /// </summary>
        /// <param name="stream">Input stream</param>
        /// <param name="sparqlQuery">SPARQL query</param>
        /// <param name="resultFormats">Output format</param>
        private void Query(Stream stream, string sparqlQuery, ResultFormats resultFormats)
        {
            using (stream)
            using (var sw = new StreamWriter(stream, Encoding.UTF8, 4096))
            {
                // Set corresponding handler or formatter
                ISparqlResultsHandler sparqlResultsHandler;
                IRdfHandler rdfResultHandler;
                switch (resultFormats)
                {
                    case ResultFormats.Turtle:
                        sparqlResultsHandler = new TurtleResultHandler(sw, true);
                        rdfResultHandler = new WriteThroughHandler(new TurtleW3CFormatter(), sw, true);
                        break;
                    case ResultFormats.Json:
                        sparqlResultsHandler = new JsonResultHandler(sw, true);
                        rdfResultHandler = new JsonRdfHandler(sw, true); 
                        break;
                    case ResultFormats.NTripples:
                        sparqlResultsHandler = new NTriplesResultHandler(sw, true);
                        rdfResultHandler = new WriteThroughHandler(new NTriples11Formatter(), sw, true);
                        break;
                    case ResultFormats.Xml:
                        sparqlResultsHandler = new XmlResultHandler(sw, true);
                        rdfResultHandler = new WriteThroughHandler(new RdfXmlFormatter(), sw, true);
                        break;
                    case ResultFormats.RdfXml:
                        sparqlResultsHandler = new RdfXmlResultHandler(sw, true);
                        rdfResultHandler = new WriteThroughHandler(new RdfXmlFormatter(), sw, true);
                        break;
                    case ResultFormats.Csv:
                        sparqlResultsHandler = new CsvResultHandler(sw, true);
                        rdfResultHandler = new WriteThroughHandler(new BaseCsvFormatter(), sw, true);
                        break;
                    default:
                        sparqlResultsHandler = new HtmlResultHandler(sw, true);
                        rdfResultHandler = new HtmlRdfHandler(sw, true); 
                        break;
                }

                // Execute query with R2RML processor
                R2RmlStorageWrapper.Storage.Query(rdfResultHandler, sparqlResultsHandler, sparqlQuery);
            }
        }
    }
}