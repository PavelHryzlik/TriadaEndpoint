using System.IO;
using System.Web.Mvc;
using VDS.RDF;
using VDS.RDF.Query;

namespace TriadaEndpoint.Controllers
{
    /// <summary>
    /// Class for saving Sparql Result Sets to FileContentResult
    /// </summary>
    public class SparqlActionResultWritter : ISparqlActionResultWritter
    {
        /// <summary>
        /// dotNetRDF SparqlResultsWriter
        /// </summary>
        private readonly ISparqlResultsWriter _writter;

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
        /// Creates a new SPARQL Result Writer which will save Result Sets in the serialization of your choice
        /// </summary>
        /// <param name="writter">dotNetRDF SparqlResultsWriter</param>
        /// <param name="contentType">Result MIME Type, default: text/plain</param>
        public SparqlActionResultWritter(ISparqlResultsWriter writter, string contentType = "text/plain")
        {
            _writter = writter;
            _contentType = contentType;
        }

        /// <summary>
        /// Write the SPARQL Result Set to FileContentResult
        /// </summary>
        /// <param name="sparqlResultSet">SPARQL Result Set</param>
        /// <returns>FileContentResult as ActionResult</returns>
        public ActionResult Write(SparqlResultSet sparqlResultSet)
        {
            return Write(sparqlResultSet, _contentType);
        }

        /// <summary>
        /// Write the SPARQL Result Set to FileContentResult
        /// </summary>
        /// <param name="sparqlResultSet">SPARQL Result Set</param>
        /// <param name="contentType">Result MIME Type</param>
        /// <returns>FileContentResult as ActionResult</returns>
        public ActionResult Write(SparqlResultSet sparqlResultSet, string contentType)
        {
            using (var ms = new MemoryStream())
            using (var sw = new StreamWriter(ms))
            {
                _writter.Save(sparqlResultSet, sw);
                return new FileContentResult(ms.ToArray(), contentType);
            }
        }
    }
}