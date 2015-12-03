using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TriadaEndpoint.DotNetRDF.Writters;
using TriadaEndpoint.Web.Models;
using TriadaEndpoint.Web.R2Rml;
using TriadaEndpoint.Web.Rdf;
using TriadaEndpoint.Web.TriadaDUL;
using TriadaEndpoint.Web.Utils;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace TriadaEndpoint.Web.Controllers
{
    /// <summary>
    /// Main controller of the application. Handle requests and return corresponding views 
    /// </summary>
    public class MainController : Controller
    {
        private const string InMemory = "InMemory";
        private const string Stream = "Stream";
        private const string JsonLd = "JsonLd";
        private const string JsonLdContentType = "application/ld+json";

        /// <summary>
        /// Handle the request to home page
        /// Redirect to ~/sparql
        /// </summary>
        /// <returns></returns>
        [HandleError]
        public ActionResult Index()
        {
            return RedirectPermanent("~/sparql");
        }

        /// <summary>
        /// Handle the request to about page
        /// </summary>
        /// <returns></returns>
        [HandleError]
        public ActionResult About()
        {
            return View();
        }

        /// <summary>
        /// Handle the request to getting dump
        /// </summary>
        /// <param name="format">Output format</param>
        /// <param name="store">Processing method InMemory/Stream</param>
        /// <returns></returns>
        [HandleError]
        [ValidateInput(false)]
        public ActionResult GetDump(string format, string store)
        {
            if (!String.IsNullOrEmpty(format))
            {
                try
                {
                    // Default workaround is in the memory
                    if (String.IsNullOrEmpty(store))
                        store = InMemory;

                    // Dump with in memory workaround
                    if (store == InMemory)
                    {
                        // Initialize RDF wirtter and set corresponding handler or formatter
                        ResultFormats resultFormat;
                        string contentType = MimeTypeHelper.GetMimeType(ResultFormats.Html);
                        var graphStreamResultWritter = new RdfWritter(new HtmlWriter());
                        if (Enum.TryParse(format, out resultFormat))
                        {
                            switch ((ResultFormats)Enum.Parse(typeof(ResultFormats), format))
                            {
                                case ResultFormats.Turtle:
                                    graphStreamResultWritter = new RdfWritter(new CompressingTurtleWriter());
                                    break;
                                case ResultFormats.Json:
                                    graphStreamResultWritter = new RdfWritter(new RdfJsonWriter());
                                    break;
                                case ResultFormats.NTripples:
                                    graphStreamResultWritter = new RdfWritter(new NTriplesWriter());
                                    break;
                                case ResultFormats.RdfXml:
                                    graphStreamResultWritter = new RdfWritter(new PrettyRdfXmlWriter());
                                    break;
                                case ResultFormats.Csv:
                                    graphStreamResultWritter = new RdfWritter(new CsvWriter());
                                    break;
                                default:
                                    graphStreamResultWritter = new RdfWritter(new HtmlWriter());
                                    break;
                            }

                            // Set outpu mime type
                            contentType = MimeTypeHelper.GetMimeType(resultFormat); 
                        }
                        else if (format == JsonLd)
                        {
                            // Set Json-Ld writter with Context
                            graphStreamResultWritter = new RdfWritter(new JsonLdWriter { Context = new Uri(SparqlQueryConstants.JsonLdContractContext) });
                            contentType = JsonLdContentType;
                        }

                        // Write Graph to output
                        var g = new Graph();
                        var clientPipe = graphStreamResultWritter.Write(g);

                        return new FileStreamResult(clientPipe, contentType);
                    }

                    // Dump with stream workaround
                    if (store == Stream)
                    {
                        ResultFormats resultFormat;
 
                        if (Enum.TryParse(format, out resultFormat))
                        {
                            // Execute query
                            var sparqlActionResultWritter = new QueryProcessor();
                            var stream = sparqlActionResultWritter.ProcessQuery(SparqlQueryConstants.ConstructAll, resultFormat);

                            return new FileStreamResult(stream, MimeTypeHelper.GetMimeType(resultFormat));
                        }

                        // Not recomended, for processing JSON-LD, in memory variant is way better 
                        if (format == JsonLd)
                        {
                            var serverPipe = new AnonymousPipeServerStream(PipeDirection.Out);
                            Task.Run(() =>
                            {
                                using (serverPipe)
                                using (var sw = new StreamWriter(serverPipe, Encoding.UTF8, 4096))
                                {
                                    var jsonLdDumpHandler = new JsonLdHandler(sw, new Uri(SparqlQueryConstants.JsonLdContractContext), true, false);
                                    jsonLdDumpHandler.WriteStartDocument();
                                    jsonLdDumpHandler.WriteStartArray("documents");
                                    R2RmlStorageWrapper.Storage.Query(null, jsonLdDumpHandler, SparqlQueryConstants.SelectContracts);
                                    R2RmlStorageWrapper.Storage.Query(null, jsonLdDumpHandler, SparqlQueryConstants.ConstructAmendments);
                                    R2RmlStorageWrapper.Storage.Query(null, jsonLdDumpHandler, SparqlQueryConstants.SelectAttachments);
                                    jsonLdDumpHandler.WriteEndArray();
                                    jsonLdDumpHandler.WriteStartArray("parties");
                                    R2RmlStorageWrapper.Storage.Query(null, jsonLdDumpHandler, SparqlQueryConstants.SelectParties);
                                    jsonLdDumpHandler.WriteEndArray();
                                    jsonLdDumpHandler.WriteEndDocument();
                                }
                            });

                            var clientPipe = new AnonymousPipeClientStream(PipeDirection.In,
                                serverPipe.ClientSafePipeHandle);
                            return new FileStreamResult(clientPipe, "application/ld+json");
                        }
                    }                
                }
                catch (AggregateException ae)
                {
                    var stringBuilder = new StringBuilder();
                    foreach (var e in ae.InnerExceptions)
                    {
                        stringBuilder.AppendLine(e.Message);
                    }
                    throw new Exception(stringBuilder.ToString());
                }    
            }
            return View("~/Views/Main/Index.cshtml");
        }

        /// <summary>
        /// Handle the SPARQL query URI request
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HandleError]
        [ValidateInput(false)]
        public ActionResult GetSparqlQuery(string query)
        {
            if (!String.IsNullOrEmpty(query))
            {
                try
                {
                    // Parse Format parameter
                    var parsedQuery = query.Split('&').ToList();
                    var sparqlQuery = parsedQuery[0];
                    var format = (parsedQuery.Count > 1) ? parsedQuery[1].Split('=')[1] : "Html";

                    // Get result format
                    var resultFormat = (ResultFormats) Enum.Parse(typeof (ResultFormats), format);
                    
                    // Execute query
                    var sparqlActionResultWritter = new QueryProcessor();
                    var stream = sparqlActionResultWritter.ProcessQuery(sparqlQuery, resultFormat);

                    return new FileStreamResult(stream, MimeTypeHelper.GetMimeType(resultFormat));
                }
                catch (AggregateException ae)
                {
                    var stringBuilder = new StringBuilder();
                    foreach (var e in ae.InnerExceptions)
                    {
                        stringBuilder.AppendLine(e.Message);
                    }
                    throw new Exception(stringBuilder.ToString());
                }
            }
            return View("~/Views/Main/Index.cshtml");
        }

        /// <summary>
        /// Handle the SPARQL query POST request 
        /// Redirect to URI request
        /// </summary>
        /// <param name="queryViewModel"></param>
        /// <returns></returns>
        [HandleError]
        [ValidateInput(false)]
        public ActionResult PostSparqlQuery(QueryViewModel queryViewModel)
        {
            var queryString = new SparqlParameterizedString();
            if (!String.IsNullOrEmpty(queryViewModel.Query))
            {
                queryString.CommandText = Url.Encode(queryViewModel.Query);
            }
            return RedirectPermanent("~/sparql?query=" + queryString + Url.Encode("&Format=" + queryViewModel.ResultFormat));
        }

        /// <summary>
        /// Handle the URI request Contract and corresponding entities
        /// Dereference to SPARQL query URI request
        /// </summary>
        /// <param name="id"></param>
        /// <param name="verze"></param>
        /// <param name="parameter"></param>
        /// <param name="parameterId"></param>
        /// <returns></returns>
        [HandleError]
        [Route("~/contract/{id?}/{verze?}/{parameter?}/{parameterId?}")]
        public ActionResult GetContract(string id, string verze, string parameter, string parameterId)
        {
            var queryString = new SparqlParameterizedString();
            if (Request.Url != null)
            {
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);

                if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) &&
                    !String.IsNullOrEmpty(parameter) && !String.IsNullOrEmpty(parameterId)) // Get Milestone
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/contract/{1}/{2}/{3}/{4}", baseUrl, id, verze, parameter, parameterId)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                    parameter.Equals("publisher")) // Get Publisher
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/publisher", baseUrl)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                    (parameter.Equals("version") || parameter.Equals("implementation") || parameter.Equals("amount"))) // Get Version / Implementation / Amount
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/contract/{1}/{2}/{3}", baseUrl, id, verze, parameter)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze)) // Get Contract
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/contract/{1}/{2}", baseUrl, id, verze)));
                }
                else
                {
                    queryString.CommandText = Url.Encode(SparqlQueryConstants.SelectContracts);
                }
            }

            return RedirectPermanent("~/sparql?query=" + queryString);
        }

        /// <summary>
        /// Handle the URI request to Amendment and corresponding entities
        /// Dereference to SPARQL query URI request
        /// </summary>
        /// <param name="id"></param>
        /// <param name="verze"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HandleError]
        [Route("~/amendment/{id?}/{verze?}/{parameter?}")]
        public ActionResult GetAmendment(string id, string verze, string parameter)
        {
            var queryString = new SparqlParameterizedString();
            if (Request.Url != null)
            {
                // Assemble SPARQL query with corresponding parameter 
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);
     
                if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                    (parameter.Equals("version"))) // Get Version
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/amendment/{1}/{2}/{3}", baseUrl, id, verze, parameter)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                     parameter.Equals("publisher")) // Get Publisher
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/publisher", baseUrl))); 
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze)) // Get Amendment
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/amendment/{1}/{2}", baseUrl, id, verze)));
                }
                else
                {
                    queryString.CommandText = Url.Encode(SparqlQueryConstants.SelectAmendments);
                }
            }

            return RedirectPermanent("~/sparql?query=" + queryString);
        }

        /// <summary>
        /// Handle the URI request to Attachment and corresponding entities
        /// Dereference to SPARQL query URI request
        /// </summary>
        /// <param name="id"></param>
        /// <param name="verze"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HandleError]
        [Route("~/attachment/{id?}/{verze?}/{parameter?}")]
        public ActionResult GetAttachment(string id, string verze, string parameter)
        {
            var queryString = new SparqlParameterizedString();
            if (Request.Url != null)
            {
                // Assemble SPARQL query with corresponding parameter 
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);

                if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                    (parameter.Equals("version"))) // Get Version
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/attachment/{1}/{2}/{3}", baseUrl, id, verze, parameter)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                         parameter.Equals("publisher")) // Get Publisher
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/publisher", baseUrl)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze)) // Get Attachment
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/attachment/{1}/{2}", baseUrl, id, verze)));
                }
                else
                {
                    queryString.CommandText = Url.Encode(SparqlQueryConstants.SelectAttachments);
                }
            }

            return RedirectPermanent("~/sparql?query=" + queryString);
        }

        /// <summary>
        /// Handle the URI request to Party and corresponding entities
        /// Dereference to SPARQL query URI request 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [HandleError]
        [Route("~/party/{id?}/{parameter?}")]
        public ActionResult GetParty(string id, string parameter)
        {
            var queryString = new SparqlParameterizedString();
            if (Request.Url != null)
            {
                // Assemble SPARQL query with corresponding parameter 
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);
                queryString.CommandText = SparqlQueryConstants.ConstructBySubject;

                // Get Address
                if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(parameter) && parameter.Equals("address")) 
                {
                    queryString.SetUri("subject", new Uri(String.Format("{0}/party/{1}/address", baseUrl, id)));
                }
                else if (!String.IsNullOrEmpty(id)) // Get Party
                {
                    queryString.SetUri("subject", new Uri(String.Format("{0}/party/{1}", baseUrl, id)));
                }
                else
                {
                    queryString.CommandText = Url.Encode(SparqlQueryConstants.SelectParties);
                }
            }

            return RedirectPermanent("~/sparql?query=" + queryString);
        }

        /// <summary>
        /// Handle the URI request to File source
        /// </summary>
        /// <param name="fileGuid"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [HandleError]
        [Route("~/file/{fileGuid?}/{fileName?}")]
        public ActionResult GetFileSource(string fileGuid, string fileName)
        {
            var queryString = new SparqlParameterizedString();
            if (!String.IsNullOrEmpty(fileGuid) && !String.IsNullOrEmpty(fileName))
            {
                // Get File source from Triada Data Store
                var file = DULWrapper.GetFile(Guid.Parse(fileGuid));
                var mimetype = MimeMapping.GetMimeMapping(fileName);

                if (file != null)
                {
                    var fileBytes = file.ToArray();
                    return File(fileBytes, mimetype, fileName);
                }
                return new EmptyResult();
            }
            queryString.CommandText = Url.Encode(SparqlQueryConstants.SelectFiles);

            return RedirectPermanent("~/sparql?query=" + queryString);
        }

        /// <summary>
        /// Handle the URI request to Publisher
        /// Dereference to SPARQL query URI request
        /// </summary>
        /// <returns></returns>
        [HandleError]
        [Route("~/publisher")]
        public ActionResult GetPublisher()
        {
            // Assemble SPARQL query with corresponding parameter 
            var queryString = new SparqlParameterizedString();
            if (Request.Url != null)
            {
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);
                
                queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                queryString.SetUri("subject", new Uri(String.Format("{0}/publisher", baseUrl)));
            }

            return RedirectPermanent("~/sparql?query=" + queryString);
        }
        
        /// <summary>
        /// Called before the action method is invoked.
        /// </summary>
        /// <param name="filterContext">Information about the current request and action.</param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            if (R2RmlStorageWrapper.StartException != null && filterContext.ActionDescriptor.ActionName != "AppStartFailed")
            {
                filterContext.Result = RedirectToAction("AppStartFailed");
            }

            if (DULWrapper.StartException != null && filterContext.ActionDescriptor.ActionName != "AppStartFailed")
            {
                filterContext.Result = RedirectToAction("AppStartFailed");
            }
        }
    }
}