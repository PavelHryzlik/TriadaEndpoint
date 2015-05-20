using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json.Converters;
using TriadaEndpoint.Models;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.Controllers
{
    public class MainController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        private const string BasePrefix = "PREFIX ex: <http://example.com/ns#> ";
        private const string Contracts = BasePrefix + "SELECT * WHERE { ?contracts a ex:Contract }";
        private const string Parties = BasePrefix + "SELECT * WHERE { ?parties a ex:Party }";
        private const string Files = BasePrefix + "SELECT ?files WHERE { ?_ ex:document ?files }";
        private const string SelectBySubject = "SELECT * WHERE { @subject ?p ?o }";

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
                var file = DULWrapper.GetFile(Guid.Parse(fileGuid));
                var mimetype = MimeMapping.GetMimeMapping(fileName);

                if (file != null)
                {
                    var fileBytes = file.ToArray();
                    return File(fileBytes, mimetype, fileName);
                }
                return new EmptyResult();
            }
            queryString.CommandText = Url.Encode(Files);

            return RedirectPermanent("~/sparql?query=" + queryString);
        }

        [ValidateInput(false)]
        public ActionResult GetSparqlQuery(string query)
        {
            if (!String.IsNullOrEmpty(query))
            {
                var parsedQuery = query.Split('&').ToList();
                var sparqlQuery = parsedQuery[0];

                var resultSet = R2RmlStorageWrapper.Storage.Query(sparqlQuery) as SparqlResultSet;

                var format = (parsedQuery.Count > 1) ? parsedQuery[1].Split('=')[1] : "Html";

                SparqlActionResultWritter sparqlActionWriter;

                switch ((ResultFormats)Enum.Parse(typeof(ResultFormats), format))
                {
                    case ResultFormats.Turtle:
                        sparqlActionWriter = new SparqlActionResultWritter(new SparqlRdfWriter(new CompressingTurtleWriter()), "text/turtle");
                        break;
                    case ResultFormats.Json:
                        sparqlActionWriter = new SparqlActionResultWritter(new SparqlJsonWriter(), "application/json");
                        break;
                    case ResultFormats.NTripples:
                        sparqlActionWriter = new SparqlActionResultWritter(new SparqlRdfWriter(new NTriplesWriter()), "text/n-triples");
                        break;
                    case ResultFormats.RdfXml:
                        sparqlActionWriter = new SparqlActionResultWritter(new SparqlRdfWriter(new PrettyRdfXmlWriter()), "text/rdf+xml");
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