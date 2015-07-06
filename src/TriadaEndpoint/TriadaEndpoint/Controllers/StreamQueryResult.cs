using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using TriadaEndpoint.Models;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.Controllers
{
    /// <summary>
    /// Class for saving Sparql Result Sets to FileContentResult
    /// </summary>
    public class StreamQueryResult
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
                ITripleFormatter rdfFormatter;
                switch (resultFormats)
                {
                    case ResultFormats.Turtle:
                        sparqlResultsHandler = new TurtleResultHandler(sw, true);
                        rdfFormatter = new TurtleW3CFormatter();
                        break;
                    case ResultFormats.Json:
                        sparqlResultsHandler = new JsonResultHandler(sw, true);
                        rdfFormatter = new NTriples11Formatter(); //TODO
                        break;
                    case ResultFormats.NTripples:
                        sparqlResultsHandler = new NTriplesResultHandler(sw, true);
                        rdfFormatter = new NTriples11Formatter(); 
                        break;
                    case ResultFormats.Xml:
                        sparqlResultsHandler = new XmlResultHandler(sw, true);
                        rdfFormatter = new NTriples11Formatter(); //TODO
                        break;
                    case ResultFormats.RdfXml:
                        sparqlResultsHandler = new RdfXmlResultHandler(sw, true);
                        rdfFormatter = new RdfXmlFormatter();
                        break;
                    case ResultFormats.Csv:
                        sparqlResultsHandler = new CsvResultHandler(sw, true);
                        rdfFormatter = new CsvFormatter();
                        break;
                    default:
                        sparqlResultsHandler = new HtmlResultHandler(sw, true);
                        rdfFormatter = new NTriples11Formatter(); //TODO
                        break;
                }

                var rdfHandler = new WriteThroughHandler(rdfFormatter, sw, false);
                R2RmlStorageWrapper.Storage.Query(rdfHandler, sparqlResultsHandler, sparqlQuery);
            }
        }
    }
}