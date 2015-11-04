using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using TriadaEndpoint.DotNetRDF.Utils;
using TriadaEndpoint.Web.Models;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using Context = System.Runtime.Remoting.Contexts.Context;
using Graph = VDS.RDF.Graph;


namespace TriadaEndpoint.Web.R2Rml
{
    public class RdfWritter
    {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// dotNetRDF RdfWriter
        /// </summary>
        private readonly IRdfWriter _writter;

        /// <summary>
        /// Creates a new Graph Writer which will save Graph in the serialization of your choice
        /// </summary>
        /// <param name="writter">dotNetRDF SparqlResultsWriter</param>
        /// <param name="contentType">Result MIME Type, default: text/plain</param>
        public RdfWritter(IRdfWriter writter)
        {
            _writter = writter;
        }

        /// <summary>
        /// ProcessQuery the SPARQL Result Set to FileContentResult
        /// </summary>
        /// <param name="graph">Graph to write</param>
        /// <returns>FileContentResult as ActionResult</returns>
        public Stream Write(IGraph graph)
        {
            var stopWatch = Stopwatch.StartNew();

            var resultGraph = new Graph {BaseUri = graph.BaseUri};
            graph.Triples.ForEach(x => resultGraph.Assert(new Triple(x.Subject, x.Predicate, W3CSpecHelper.FormatNode(x.Object))));
           
            _log.Info("Graph - Postprocess in " + stopWatch.ElapsedMilliseconds + "ms");

            var serverPipe = new AnonymousPipeServerStream(PipeDirection.Out);
            Task.Run(() =>
            {
                using (serverPipe)
                using (var sw = new StreamWriter(serverPipe, Encoding.UTF8, 4096))
                {
                    R2RmlStorageWrapper.Storage.Query(new GraphHandler(graph), null, "CONSTRUCT { ?s ?p ?o } FROM <http://tiny.cc/open-contracting#> WHERE { ?s ?p ?o }");

                    _writter.Save(graph, sw);
                }
            });

            var clientPipe = new AnonymousPipeClientStream(PipeDirection.In,
                     serverPipe.ClientSafePipeHandle);

            stopWatch.Stop();
            _log.Info("Graph - SaveGraphToFile in " + stopWatch.ElapsedMilliseconds + "ms");

            return clientPipe;
        }
    }
}