using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TriadaEndpoint.Models;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace TriadaEndpoint.Controllers
{
    public class MainController : Controller
    {
        private const string BasePrefix = "PREFIX ex: <http://example.com/ns#> ";
        private const string Contracts = BasePrefix + "SELECT * WHERE { ?contracts a ex:Contract }";
        private const string Supplement = BasePrefix + "SELECT * WHERE { ?supplements a ex:Supplement }";
        private const string Parties = BasePrefix + "SELECT * WHERE { ?parties a ex:Party }";
        private const string Files = BasePrefix + "SELECT ?files WHERE { ?_ ex:document ?files }";
        private const string SelectBySubject = "SELECT * WHERE { @subject ?p ?o }";

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
                    var graph = new Graph();
                    R2RmlStorageWrapper.Storage.LoadGraph(graph, "http://example.com/ns#");

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
                        default:
                            graphActionWriter = new GraphActionResultWritter(new HtmlWriter(), "text/html");
                            break;
                    }
                    return graphActionWriter.Write(graph);
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
                    var parsedQuery = query.Split('&').ToList();
                    var sparqlQuery = parsedQuery[0];
                    var format = (parsedQuery.Count > 1) ? parsedQuery[1].Split('=')[1] : "Html";

                    var result = R2RmlStorageWrapper.Storage.Query(sparqlQuery);

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
                        return sparqlActionWriter.Write(resultSet);
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
                            default:
                                graphActionWriter = new GraphActionResultWritter(new HtmlWriter(), "text/html");
                                break;
                        }
                        return graphActionWriter.Write(graph);
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

        [Route("~/contract/{id?}/{verze?}/{parameter?}")]
        public ActionResult GetContract(string id, string verze, string parameter)
        {
            var queryString = new SparqlParameterizedString();
            if (Request.Url != null)
            {
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);

                if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                    (parameter.Equals("version") || parameter.Equals("publisher")))
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

        [Route("~/supplement/{id?}/{verze?}/{parameter?}")]
        public ActionResult GetSupplement(string id, string verze, string parameter)
        {
            var queryString = new SparqlParameterizedString();
            if (Request.Url != null)
            {
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);

                if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze) && !String.IsNullOrEmpty(parameter) &&
                    (parameter.Equals("version") || parameter.Equals("publisher")))
                {
                    queryString.CommandText = SelectBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/supplement/{1}/{2}/{3}", baseUrl, id, verze, parameter)));
                }
                else if (!String.IsNullOrEmpty(id) && !String.IsNullOrEmpty(verze))
                {
                    queryString.CommandText = SelectBySubject;
                    queryString.SetUri("subject", new Uri(String.Format("{0}/supplement/{1}/{2}", baseUrl, id, verze)));
                }
                else
                {
                    queryString.CommandText = Url.Encode(Supplement);
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