using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using log4net;
using TriadaEndpoint.Models;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace TriadaEndpoint.Controllers
{
    public class MainController : Controller
    {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string BasePrefix = "PREFIX : <http://tiny.cc/open-contracting#> ";
        private const string Contracts = BasePrefix + "SELECT * WHERE { ?contracts a :Contract }";
        private const string Amendment = BasePrefix + "SELECT * WHERE { ?amendments a :Amendment }";
        private const string Attachment = BasePrefix + "SELECT * WHERE { ?attachments a :Attachment }";
        private const string Parties = BasePrefix + "SELECT * WHERE { ?parties a :party }";
        private const string Files = BasePrefix + "SELECT ?files WHERE { ?_ :document ?files }";
        private const string SelectBySubject = "SELECT * WHERE { @subject ?p ?o }";

        private const string JsonLdContractContext = "http://tiny.cc/open-contracting_context";

        public ActionResult Index()
        {
            return View();
        }

        [ValidateInput(false)]
        public ActionResult GetDump(string format)
        {
            if (!String.IsNullOrEmpty(format))
            {
                try
                {
                    var stopWatch = Stopwatch.StartNew();

                    var graph = new Graph();
                    R2RmlStorageWrapper.Storage.LoadGraph(graph, "http://tiny.cc/open-contracting#");

                    var elepsedTime = stopWatch.ElapsedMilliseconds;
                    _log.Info("GetDump - LoadGraph in " + stopWatch.ElapsedMilliseconds + "ms");

                    IGraphActionResultWritter graphActionWriter;

                    switch ((ResultFormats)Enum.Parse(typeof(ResultFormats), format))
                    {
                        case ResultFormats.Turtle:
                            graphActionWriter = new GraphActionResultWritter(new CompressingTurtleWriter(),
                                "text/turtle");
                            break;
                        case ResultFormats.Json:
                            graphActionWriter = new GraphActionResultWritter(new RdfJsonWriter(), "application/json");
                            break;
                        case ResultFormats.NTripples:
                            graphActionWriter = new GraphActionResultWritter(new NTriplesWriter(), "text/n-triples");
                            break;
                        case ResultFormats.RdfXml:
                            graphActionWriter = new GraphActionResultWritter(new PrettyRdfXmlWriter(),
                                "text/rdf+xml");
                            break;
                        case ResultFormats.Csv:
                            graphActionWriter = new GraphActionResultWritter(new CsvWriter(), "text/csv");
                            break;
                        case ResultFormats.JsonLd:
                            graphActionWriter = new GraphActionResultWritter(new JsonLdWriter { Context = new Uri(JsonLdContractContext) }, "application/ld+json");
                            break;
                        default:
                            graphActionWriter = new GraphActionResultWritter(new HtmlWriter(), "text/html");
                            break;
                    }

                    var resultActionResult = graphActionWriter.Write(graph);

                    stopWatch.Stop();
                    var partTime = stopWatch.ElapsedMilliseconds - elepsedTime;
                    _log.Info("GetDump - WriteGraph (" + format + ") in " + partTime + "ms");
                    _log.Info("GetDump in " + stopWatch.ElapsedMilliseconds + "ms");

                    return resultActionResult;
                }
                catch (Exception ex)
                {
                    return Content("Chyba: " + ex.Message);
                }      
            }
            return new EmptyResult();
        }

        [ValidateInput(false)]
        public ActionResult GetSparqlQuery(string query)
        {
            if (!String.IsNullOrEmpty(query))
            {
                try
                {
                    var stopWatch = Stopwatch.StartNew();

                    var parsedQuery = query.Split('&').ToList();
                    var sparqlQuery = parsedQuery[0];
                    var format = (parsedQuery.Count > 1) ? parsedQuery[1].Split('=')[1] : "Html";

                    var result = R2RmlStorageWrapper.Storage.Query(sparqlQuery);

                    var elepsedTime = stopWatch.ElapsedMilliseconds;
                    _log.Info("GetSparqlQuery " + query + " - QueryResult in " + stopWatch.ElapsedMilliseconds + "ms");

                    if (result is SparqlResultSet)
                    {
                        var resultSet = (SparqlResultSet)result;

                        ISparqlActionResultWritter sparqlActionWriter;

                        switch ((ResultFormats)Enum.Parse(typeof(ResultFormats), format))
                        {
                            case ResultFormats.Turtle:
                                sparqlActionWriter =
                                    new SparqlActionResultWritter(new SparqlRdfWriter(new CompressingTurtleWriter()),
                                        "text/turtle");
                                break;
                            case ResultFormats.Json:
                            case ResultFormats.JsonLd:
                                sparqlActionWriter = new SparqlActionResultWritter(new SparqlJsonWriter(),
                                    "application/json");
                                break;
                            case ResultFormats.NTripples:
                                sparqlActionWriter =
                                    new SparqlActionResultWritter(new SparqlRdfWriter(new NTriplesWriter()),
                                        "text/n-triples");
                                break;
                            case ResultFormats.RdfXml:
                                sparqlActionWriter =
                                    new SparqlActionResultWritter(new SparqlRdfWriter(new PrettyRdfXmlWriter()),
                                        "text/rdf+xml");
                                break;
                            case ResultFormats.Csv:
                                sparqlActionWriter = new SparqlActionResultWritter(new SparqlCsvWriter(), "text/csv");
                                break;
                            default:
                                sparqlActionWriter = new SparqlActionResultWritter(new SparqlHtmlWriter(), "text/html");
                                break;
                        }

                        var resultActionResult = sparqlActionWriter.Write(resultSet);

                        stopWatch.Stop();
                        var partTime = stopWatch.ElapsedMilliseconds - elepsedTime;
                        _log.Info("GetSparqlQuery " + query + " - WriteSparqlResult (" + format + ") in " + partTime + "ms");
                        _log.Info("GetSparqlQuery " + query + " in " + stopWatch.ElapsedMilliseconds + "ms");

                        return resultActionResult;
                    }

                    if (result is IGraph)
                    {
                        var graph = (IGraph)result;

                        IGraphActionResultWritter graphActionWriter;

                        switch ((ResultFormats)Enum.Parse(typeof(ResultFormats), format))
                        {
                            case ResultFormats.Turtle:
                                graphActionWriter = new GraphActionResultWritter(new CompressingTurtleWriter(),
                                    "text/turtle");
                                break;
                            case ResultFormats.Json:
                                graphActionWriter = new GraphActionResultWritter(new RdfJsonWriter(), "application/json");
                                break;
                            case ResultFormats.NTripples:
                                graphActionWriter = new GraphActionResultWritter(new NTriplesWriter(), "text/n-triples");
                                break;
                            case ResultFormats.RdfXml:
                                graphActionWriter = new GraphActionResultWritter(new PrettyRdfXmlWriter(),
                                    "text/rdf+xml");
                                break;
                            case ResultFormats.Csv:
                                graphActionWriter = new GraphActionResultWritter(new CsvWriter(), "text/csv");
                                break;
                            case ResultFormats.JsonLd:
                                graphActionWriter = new GraphActionResultWritter(new JsonLdWriter { Context = new Uri(JsonLdContractContext) }, "application/ld+json");
                                break;
                            default:
                                graphActionWriter = new GraphActionResultWritter(new HtmlWriter(), "text/html");
                                break;
                        }

                        var resultActionResult = graphActionWriter.Write(graph);

                        stopWatch.Stop();
                        var partTime = stopWatch.ElapsedMilliseconds - elepsedTime;
                        _log.Info("GetSparqlQuery " + query + " - WriteGraph (" + format + ") in " + partTime + "ms");
                        _log.Info("GetSparqlQuery " + query + " in " + stopWatch.ElapsedMilliseconds + "ms");

                        return resultActionResult;
                    }
                }
                catch (Exception ex)
                {
                    return Content("Chyba: " + ex.Message);
                }
            }
            return new EmptyResult();
        }

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

        [Route("~/contract/{id?}/{verze?}/{parameter?}/{milestone?}/{milestoneId?}")]
        public ActionResult GetContract(string id, string verze, string parameter, string milestone, string milestoneId)
        {
            var queryString = new SparqlParameterizedString();
            if (Request.Url != null)
            {
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);

                if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) &&
                    !String.IsNullOrEmpty(parameter) && !String.IsNullOrEmpty(milestone)
                    && !String.IsNullOrEmpty(milestoneId) && parameter.Equals("implementation"))
                {
                    queryString.CommandText = SelectBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/contract/{1}/{2}/{3}/{4}/{5}", baseUrl, id, verze, parameter, milestone, milestoneId)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                    parameter.Equals("publisher"))
                {
                    queryString.CommandText = SelectBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/publisher", baseUrl)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                    (parameter.Equals("version") || parameter.Equals("implementation")))
                {
                    queryString.CommandText = SelectBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/contract/{1}/{2}/{3}", baseUrl, id, verze, parameter)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze))
                {
                    queryString.CommandText = SelectBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/contract/{1}/{2}", baseUrl, id, verze)));
                }
                else
                {
                    queryString.CommandText = Url.Encode(Contracts);
                }
            }

            return RedirectPermanent("~/sparql?query=" + queryString);
        }

        [Route("~/amendment/{id?}/{verze?}/{parameter?}")]
        public ActionResult GetSupplement(string id, string verze, string parameter)
        {
            var queryString = new SparqlParameterizedString();
            if (Request.Url != null)
            {
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);

                if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                    (parameter.Equals("version")))
                {
                    queryString.CommandText = SelectBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/amendment/{1}/{2}/{3}", baseUrl, id, verze, parameter)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                     parameter.Equals("publisher"))
                {
                    queryString.CommandText = SelectBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/publisher", baseUrl)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze))
                {
                    queryString.CommandText = SelectBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/amendment/{1}/{2}", baseUrl, id, verze)));
                }
                else
                {
                    queryString.CommandText = Url.Encode(Amendment);
                }
            }

            return RedirectPermanent("~/sparql?query=" + queryString);
        }

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
                    queryString.CommandText = SelectBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/attachment/{1}/{2}/{3}", baseUrl, id, verze, parameter)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                         parameter.Equals("publisher"))
                {
                    queryString.CommandText = SelectBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/publisher", baseUrl)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze))
                {
                    queryString.CommandText = SelectBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/attachment/{1}/{2}", baseUrl, id, verze)));
                }
                else
                {
                    queryString.CommandText = Url.Encode(Attachment);
                }
            }

            return RedirectPermanent("~/sparql?query=" + queryString);
        }

        [Route("~/party/{id?}/{parameter?}")]
        public ActionResult GetParty(string id, string parameter)
        {
            var queryString = new SparqlParameterizedString();
            if (Request.Url != null)
            {
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);
                queryString.CommandText = SelectBySubject;

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
                    queryString.CommandText = Url.Encode(Parties);
                }
            }

            return RedirectPermanent("~/sparql?query=" + queryString);
        }

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
            queryString.CommandText = Url.Encode(Files);

            return RedirectPermanent("~/sparql?query=" + queryString);
        }

        [Route("~/publisher")]
        public ActionResult GetPublisher()
        {
            var queryString = new SparqlParameterizedString();
            if (Request.Url != null)
            {
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);
                
                queryString.CommandText = SelectBySubject;
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