using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using TriadaEndpoint.DotNetRDF.BaseHandlers;
using TriadaEndpoint.DotNetRDF.Formatters;
using TriadaEndpoint.DotNetRDF.LazyRdfHandlers;
using TriadaEndpoint.DotNetRDF.RdfHandlers;
using TriadaEndpoint.DotNetRDF.SparqlLazyResultHandlers;
using TriadaEndpoint.DotNetRDF.SparqlResultHandlers;
using TriadaEndpoint.Web.Models;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Writing.Formatting;
using TriadaEndpoint.Web.Utils;
using VDS.RDF.Parsing;
namespace TriadaEndpoint.Web.R2Rml
{
    /// <summary>
    /// Class processing SPARQL queries
    /// </summary>
    public class QueryProcessor
    {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SelectString = "SELECT";
        private const string ContructString = "CONSTRUCT";

        /// <summary>
        /// Execute SPARQL query
        /// </summary>
        /// <param name="sparqlQuery">SPARQL query</param>
        /// <param name="resultFormats">Output format</param>
        public void ProcessQuery(string sparqlQuery, IChunkHandler handler)
        {
            ISparqlResultsHandler sparqlResultsHandler = null;
            IRdfHandler rdfResultHandler = null;

            if (handler is IRdfHandler)
            {
                rdfResultHandler = handler as IRdfHandler;
            }
            else if (handler is ISparqlResultsHandler)
            {
                sparqlResultsHandler = handler as ISparqlResultsHandler;
            }

            // Execute query with R2RML processor
            Task.Run(() => R2RmlStorageWrapper.Storage.Query(rdfResultHandler, sparqlResultsHandler, sparqlQuery))
                .OnException(ex => _log.Error(ExceptionHelper.ParseMultiException((AggregateException)ex)));
        }

        /// <summary>
        /// Get handler (IChunkHandler type) from resultFormat, simple validate Sparql query
        /// </summary>
        /// <param name="sparqlQuery">SPARQL query</param>
        /// <param name="resultFormat">Output format</param>
        /// <returns></returns>
        public IChunkHandler GetHandler(string sparqlQuery, ResultFormats resultFormat)
        {
            SparqlQueryParser sparqlParser = new SparqlQueryParser();
            sparqlParser.ParseFromString(sparqlQuery);

            IChunkHandler handler;
            if (sparqlQuery.Contains(SelectString))
            {
                handler = GetSparqlResultHandler(resultFormat);
            }
            else if (sparqlQuery.Contains(ContructString))
            {
                handler = GetRdfHandler(resultFormat);
            }
            else
                throw new ArgumentException("Invalid SPARQL query");

            return handler;
        }

        /// <summary>
        /// Get SparqlResultHandler by resultFormat
        /// </summary>
        /// <param name="resultFormats">Output format></param>
        /// <returns></returns>
        private IChunkHandler GetSparqlResultHandler(ResultFormats resultFormats)
        {
            switch (resultFormats)
            {
                case ResultFormats.Turtle:
                    return new TurtleLazyResultHandler();
                case ResultFormats.Json:
                    return new JsonLazyResultHandler();
                case ResultFormats.NTripples:
                    return new NTriplesLazyResultHandler();
                case ResultFormats.Xml:
                    return new XmlLazyResultHandler();
                case ResultFormats.RdfXml:
                    return new RdfXmlLazyResultHandler();
                case ResultFormats.Csv:
                    return new CsvLazyResultHandler();
                default:
                    return new HtmlLazyResultHandler();
            }
        }

        /// <summary>
        /// Get RdfHandler by resultFormat
        /// </summary>
        /// <param name="resultFormats">Output format></param>
        /// <returns></returns>
        private IChunkHandler GetRdfHandler(ResultFormats resultFormats)
        {
            switch (resultFormats)
            {
                case ResultFormats.Turtle:
                    return new ThroughLazyRdfHandler(new TurtleW3CFormatter());
                case ResultFormats.Json:
                    return new JsonLazyRdfHandler();
                case ResultFormats.NTripples:
                    return new ThroughLazyRdfHandler(new NTriples11Formatter());
                case ResultFormats.Xml:
                    return new ThroughLazyRdfHandler(new RdfXmlFormatter());
                case ResultFormats.RdfXml:
                    return new ThroughLazyRdfHandler(new RdfXmlFormatter());
                case ResultFormats.Csv:
                    return new ThroughLazyRdfHandler(new BaseCsvFormatter());
                default:
                    return new HtmlLazyRdfHandler();
            }
        }

        /// <summary>
        /// Execute SPARQL query and write result to stream
        /// SELECT queries generates SparqlResult and needs ISparqlResultsHandler
        /// CONSTRUCT ueries generates Triples and needs IRdfHandler
        /// </summary>
        /// <param name="stream">Input stream</param>
        /// <param name="sparqlQuery">SPARQL query</param>
        /// <param name="resultFormats">Output format</param>
        public void ProcessQuery(Stream stream, string sparqlQuery, ResultFormats resultFormats)
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