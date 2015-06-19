using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using log4net;
using Microsoft.Ajax.Utilities;
using VDS.RDF;
using WebGrease.Css.Extensions;
using Context = System.Runtime.Remoting.Contexts.Context;
using Graph = VDS.RDF.Graph;


namespace TriadaEndpoint.Controllers
{
    public class GraphActionResultWritter : IGraphActionResultWritter
    {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// dotNetRDF RdfWriter
        /// </summary>
        private readonly IRdfWriter _writter;

        /// <summary>
        /// Result MIME Type
        /// </summary>
        private readonly string _contentType;

        /// <summary>
        /// Get result MIME Type
        /// </summary>
        public string ContentType
        {
            get { return _contentType; }
        }

        /// <summary>
        /// Creates a new Graph Writer which will save Graph in the serialization of your choice
        /// </summary>
        /// <param name="writter">dotNetRDF SparqlResultsWriter</param>
        /// <param name="contentType">Result MIME Type, default: text/plain</param>
        public GraphActionResultWritter(IRdfWriter writter, string contentType = "text/plain")
        {
            _writter = writter;
            _contentType = contentType;
        }

        /// <summary>
        /// Write the SPARQL Result Set to FileContentResult
        /// </summary>
        /// <param name="graph">Graph to write</param>
        /// <returns>FileContentResult as ActionResult</returns>
        public ActionResult Write(IGraph graph)
        {
            return Write(graph, _contentType);
        }

        /// <summary>
        /// Write the SPARQL Result Set to FileContentResult
        /// </summary>
        /// <param name="graph">Graph to write</param>
        /// <param name="contentType">Result MIME Type</param>
        /// <returns>FileContentResult as ActionResult</returns>
        public ActionResult Write(IGraph graph, string contentType)
        {
            var stopWatch = Stopwatch.StartNew();

            var resultGraph = new Graph {BaseUri = graph.BaseUri};
            graph.Triples.ForEach(x => resultGraph.Assert(new Triple(x.Subject, x.Predicate, W3CSpecHelper.FormatNode(x.Object))));
           
            _log.Info("Graph - Postprocess in " + stopWatch.ElapsedMilliseconds + "ms");

            var filename = AppDomain.CurrentDomain.BaseDirectory + "App_Data\\output";

            using (var fsw = new FileStream(filename, FileMode.Create, FileAccess.Write))
            using (var sw = new StreamWriter(fsw))
            {
                //_writter.Save(graph, sw);
                _writter.Save(resultGraph, sw);    
            }

            stopWatch.Stop();
            _log.Info("Graph - SaveGraphToFile in " + stopWatch.ElapsedMilliseconds + "ms");

            return new DownloadResult(new FileStream(filename, FileMode.Open), contentType);
        }
    }
}