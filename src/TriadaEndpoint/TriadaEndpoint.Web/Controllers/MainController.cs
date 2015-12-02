using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using log4net;
using TriadaEndpoint.DotNetRDF;
using TriadaEndpoint.DotNetRDF.SparqlResultHandlers;
using TriadaEndpoint.DotNetRDF.Writters;
using TriadaEndpoint.Web.Models;
using TriadaEndpoint.Web.R2Rml;
using TriadaEndpoint.Web.TriadaDUL;
using TriadaEndpoint.Web.Utils;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.Web.Controllers
{
    public class MainController : Controller
    {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string InMemory = "InMemory";
        private const string Stream = "Stream";

        [HandleError]
        public ActionResult Index()
        {
            return View();
        }

        [HandleError]
        public ActionResult About()
        {
            return View();
        }

        [HandleError]
        [ValidateInput(false)]
        public ActionResult GetDump(string format, string store)
        {
            if (!String.IsNullOrEmpty(format))
            {
                try
                {
                    if (String.IsNullOrEmpty(store))
                        store = InMemory;

                    if (store == InMemory)
                    {
                        ResultFormats resultFormat;
                        string contentType = "text/html";
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

                            contentType = MimeTypeHelper.GetMimeType(resultFormat);
                        }
                        else
                        {
                            if (format == "JsonLd")
                            {
                                graphStreamResultWritter = new RdfWritter(new JsonLdWriter { Context = new Uri(SparqlQueryConstants.JsonLdContractContext) });
                                contentType = "application/ld+json";
                            }
                        }

                        var g = new Graph();
                        var clientPipe = graphStreamResultWritter.Write(g);

                        return new FileStreamResult(clientPipe, contentType);
                    }

                    if (store == Stream)
                    {
                        ResultFormats resultFormat;
                        if (format == "JsonLd")
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
                                    R2RmlStorageWrapper.Storage.Query(null, jsonLdDumpHandler, "SELECT * WHERE { ?s a <http://tiny.cc/open-contracting#Contract> }");
                                    R2RmlStorageWrapper.Storage.Query(null, jsonLdDumpHandler, "SELECT * WHERE { ?s a <http://tiny.cc/open-contracting#SelectAmendments> }");
                                    R2RmlStorageWrapper.Storage.Query(null, jsonLdDumpHandler, "SELECT * WHERE { ?s a <http://tiny.cc/open-contracting#SelectAttachments> }");
                                    jsonLdDumpHandler.WriteEndArray();
                                    jsonLdDumpHandler.WriteStartArray("parties");
                                    R2RmlStorageWrapper.Storage.Query(null, jsonLdDumpHandler, "SELECT * WHERE { ?s a <http://purl.org/goodrelations/v1#BusinessEntity> }");
                                    jsonLdDumpHandler.WriteEndArray();
                                    jsonLdDumpHandler.WriteEndDocument();
                                }
                            });

                            var clientPipe = new AnonymousPipeClientStream(PipeDirection.In,
                                     serverPipe.ClientSafePipeHandle);
                            return new FileStreamResult(clientPipe, "application/ld+json");
                        }

                        if (Enum.TryParse(format, out resultFormat))
                        {
                            var sparqlActionResultWritter = new QueryProcessor();
                            var stream = sparqlActionResultWritter.ProcessQuery("CONSTRUCT { ?s ?p ?o } FROM <http://tiny.cc/open-contracting#> WHERE { ?s ?p ?o }", resultFormat);

                            return new FileStreamResult(stream, MimeTypeHelper.GetMimeType(resultFormat));
                        }
                    }                
                }
                catch (Exception ex)
                {
                    return Content("Chyba: " + ex.Message);
                }      
            }
            return new EmptyResult();
        }

        [HandleError]
        [ValidateInput(false)]
        public ActionResult GetSparqlQuery(string query)
        {
            if (!String.IsNullOrEmpty(query))
            {
                try
                {
                    var parsedQuery = query.Split('&').ToList();
                    var sparqlQuery = parsedQuery[0];
                    var format = (parsedQuery.Count > 1) ? parsedQuery[1].Split('=')[1] : "Html";

                    var resultFormat = (ResultFormats) Enum.Parse(typeof (ResultFormats), format);
                    
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
                    return Content("Chyba: " + stringBuilder);
                }
                catch (Exception ex)
                {
                    return Content("Chyba: " + ex.Message);
                }
            }
            return new EmptyResult();
        }

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

        [HandleError]
        [Route("~/contract/{id?}/{verze?}/{parameter?}/{parameterId?}")]
        public ActionResult GetContract(string id, string verze, string parameter, string parameterId)
        {
            var queryString = new SparqlParameterizedString();
            if (Request.Url != null)
            {
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);

                if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) &&
                    !String.IsNullOrEmpty(parameter) && !String.IsNullOrEmpty(parameterId))
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/contract/{1}/{2}/{3}/{4}", baseUrl, id, verze, parameter, parameterId)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                    parameter.Equals("publisher"))
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/publisher", baseUrl)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                    (parameter.Equals("version") || parameter.Equals("implementation") || parameter.Equals("amount")))
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/contract/{1}/{2}/{3}", baseUrl, id, verze, parameter)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze))
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

        [HandleError]
        [Route("~/amendment/{id?}/{verze?}/{parameter?}")]
        public ActionResult GetAmendment(string id, string verze, string parameter)
        {
            var queryString = new SparqlParameterizedString();
            if (Request.Url != null)
            {
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);

                if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                    (parameter.Equals("version")))
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/amendment/{1}/{2}/{3}", baseUrl, id, verze, parameter)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                     parameter.Equals("publisher"))
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/publisher", baseUrl)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze))
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

        [HandleError]
        [Route("~/attachment/{id?}/{verze?}/{parameter?}")]
        public ActionResult GetAttachment(string id, string verze, string parameter)
        {
            var queryString = new SparqlParameterizedString();
            if (Request.Url != null)
            {
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);

                if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                    (parameter.Equals("version")))
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/attachment/{1}/{2}/{3}", baseUrl, id, verze, parameter)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                         parameter.Equals("publisher"))
                {
                    queryString.CommandText = SparqlQueryConstants.ConstructBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/publisher", baseUrl)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze))
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

        [HandleError]
        [Route("~/party/{id?}/{parameter?}")]
        public ActionResult GetParty(string id, string parameter)
        {
            var queryString = new SparqlParameterizedString();
            if (Request.Url != null)
            {
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);
                queryString.CommandText = SparqlQueryConstants.ConstructBySubject;

                if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(parameter) && parameter.Equals("address"))
                {
                    queryString.SetUri("subject", new Uri(String.Format("{0}/party/{1}/address", baseUrl, id)));
                }
                else if (!String.IsNullOrEmpty(id))
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

        [HandleError]
        [Route("~/file/{fileGuid?}/{fileName?}")]
        public ActionResult GetFileSource(string fileGuid, string fileName)
        {
            var queryString = new SparqlParameterizedString();
            if (!String.IsNullOrEmpty(fileGuid) && !String.IsNullOrEmpty(fileName))
            {
                try
                {
                    var file = DULWrapper.GetFile(Guid.Parse(fileGuid));
                    var mimetype = MimeMapping.GetMimeMapping(fileName);

                    if (file != null)
                    {
                        var fileBytes = file.ToArray();
                        return File(fileBytes, mimetype, fileName);
                    }
                    return new EmptyResult();
                }
                catch (Exception ex)
                {
                    return Content("Chyba: " + ex.Message);
                }               
            }
            queryString.CommandText = Url.Encode(SparqlQueryConstants.SelectFiles);

            return RedirectPermanent("~/sparql?query=" + queryString);
        }

        [HandleError]
        [Route("~/publisher")]
        public ActionResult GetPublisher()
        {
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