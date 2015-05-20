using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VDS.RDF;

namespace TriadaEndpoint.Controllers
{
    public class GraphActionResultWritter : IGraphActionResultWritter
    {
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
            using (var ms = new MemoryStream())
            using (var sw = new StreamWriter(ms))
            {
                _writter.Save(graph, sw);
                return new FileContentResult(ms.ToArray(), contentType);
            }
        }
    }
}