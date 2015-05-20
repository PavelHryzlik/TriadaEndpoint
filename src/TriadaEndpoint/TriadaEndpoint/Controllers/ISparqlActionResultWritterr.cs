using System.Web.Mvc;
using VDS.RDF.Query;

namespace TriadaEndpoint.Controllers
{
    /// <summary>
    /// Interface for Writer classes which serialize Sparql Result Sets into FileContentResult
    /// </summary>
    public interface ISparqlActionResultWritter
    {
        /// <summary>
        /// Write the SPARQL Result Set to FileContentResult
        /// </summary>
        /// <param name="sparqlResultSet">SPARQL Result Set</param>
        /// <returns>FileContentResult as ActionResult</returns>
        ActionResult Write(SparqlResultSet sparqlResultSet);

        /// <summary>
        /// Write the SPARQL Result Set to FileContentResult
        /// </summary>
        /// <param name="sparqlResultSet">SPARQL Result Set</param>
        /// <param name="contentType">Result MIME Type</param>
        /// <returns>FileContentResult as ActionResult</returns>
        ActionResult Write(SparqlResultSet sparqlResultSet, string contentType);
    }
}
