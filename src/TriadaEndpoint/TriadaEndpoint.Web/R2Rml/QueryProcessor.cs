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

namespace TriadaEndpoint.Web.R2Rml
{
    /// <summary>
    /// Class for saving Sparql Result Sets to FileContentResult
    /// </summary>
    public class QueryProcessor
    {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Stream ProcessQuery(string sparqlQuery, ResultFormats resultFormats)
        {
            var stopWatch = Stopwatch.StartNew();

            var serverPipe = new AnonymousPipeServerStream(PipeDirection.Out);
            Task.Factory.StartNew(() => Query(serverPipe, sparqlQuery, resultFormats));

            var clientPipe = new AnonymousPipeClientStream(PipeDirection.In, serverPipe.ClientSafePipeHandle);
            
            stopWatch.Stop();
            _log.Info("SparqlResult in " + stopWatch.ElapsedMilliseconds + "ms");

            return clientPipe;
        }

        private void Query(Stream stream, string sparqlQuery, ResultFormats resultFormats)
        {
            using (stream)
            using (var sw = new StreamWriter(stream, Encoding.UTF8, 4096))
            {
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

                R2RmlStorageWrapper.Storage.Query(rdfResultHandler, sparqlResultsHandler, sparqlQuery);
            }
        }
    }
}