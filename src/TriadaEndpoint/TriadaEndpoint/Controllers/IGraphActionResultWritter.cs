using System.Web.Mvc;
using VDS.RDF;

namespace TriadaEndpoint.Controllers
{
    /// <summary>
    /// Interface for Writer classes which serialize Graph into FileContentResult
    /// </summary>
    public interface IGraphActionResultWritter
    {
        /// <summary>
        /// Write the SPARQL Result Set to FileContentResult
        /// </summary>
        /// <param name="graph">Graph to write</param>
        /// <returns>FileContentResult as ActionResult</returns>
        ActionResult Write(IGraph graph);

        /// <summary>
        /// Write the SPARQL Result Set to FileContentResult
        /// </summary>
        /// <param name="graph">Graph to write</param>
        /// <param name="contentType">Result MIME Type</param>
        /// <returns>FileContentResult as ActionResult</returns>
        ActionResult Write(IGraph graph, string contentType);
    }
}