using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TriadaEndpoint.DotNetRDF.Formatters;
using TriadaEndpoint.DotNetRDF.Utils;
using TriadaEndpoint.Web.R2Rml;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;

namespace TriadaEndpoint.Web.Rdf
{
    /// <summary>
    /// Obsolete, check JsonLdWriter in TriadaEndpoint.DotNetRdf project
    /// </summary>
    [Obsolete]
    public class JsonLdHandler : BaseResultsHandler
    {
        private readonly JsonTextWriter _writter;
        private readonly Uri _contextUri;
        private readonly Uri _baseDoamin = new Uri("http://localhost:7598/");
        private readonly bool _closeOutput;
        private JToken _context;

        public JsonLdHandler(TextWriter output, Uri contextUri, bool prettyprint, bool closeOutput)
        {
            _writter = new JsonTextWriter(output)
            {
                Formatting = prettyprint ? Formatting.Indented : Formatting.None
            };
            _contextUri = contextUri;
            _closeOutput = closeOutput;
        }

        public void WriteStartDocument()
        {
            _writter.WriteStartObject();

            _writter.WritePropertyName("@context");
            _writter.WriteValue(_contextUri);

            var id = "contracts_release_" + DateTime.Now.Date.ToString("yyyyMMdd");
            _writter.WritePropertyName("id");
            _writter.WriteValue(id);

            _writter.WritePropertyName("published");
            _writter.WriteValue(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz"));

            _writter.WritePropertyName("language");
            _writter.WriteValue("cs");
        }

        public void WriteEndDocument()
        {
            _writter.WriteEndObject();
        }

        public void WriteStartArray(string propertyName)
        {
            _writter.WritePropertyName(propertyName);
            _writter.WriteStartArray();
        }

        public void WriteEndArray()
        {
            _writter.WriteEndArray();
        }

        protected override void StartResultsInternal()
        {
            _context = JsonLD.Util.JSONUtils.FromURL(_contextUri);  
        }

        protected override void EndResultsInternal(bool ok)
        {
            if (_closeOutput)
                _writter.Close();
        }


        protected override void HandleBooleanResultInternal(bool result)
        {

        }

        protected override bool HandleResultInternal(SparqlResult result)
        {
            foreach (String var in result.Variables)
            {
                if (!result.HasValue(var)) continue;

                var value = result.Value(var).ToString();
                if (value.Contains("contract") || value.Contains("amendment") || value.Contains("attachment"))
                {
                    var g = new Graph();
                    R2RmlStorageWrapper.Storage.Query(new GraphHandler(g), null,"CONSTRUCT { <" + value + ">  ?p ?o } WHERE { <" + value + " > ?p ?o }");
                    R2RmlStorageWrapper.Storage.Query(new GraphHandler(g), null,"CONSTRUCT { <" + value + "/version>  ?p ?o } WHERE { <" + value + "/version> ?p ?o }");
                    R2RmlStorageWrapper.Storage.Query(new GraphHandler(g), null,"CONSTRUCT { <" + value + "/implementation>  ?p ?o } WHERE { <" + value + "/implementation> ?p ?o }");
                    R2RmlStorageWrapper.Storage.Query(new GraphHandler(g), null,"CONSTRUCT { ?s  ?p ?o } WHERE {?s ?p ?o .<" + value +"/implementation> <http://tiny.cc/open-contracting#milestone> ?s .}");
                    R2RmlStorageWrapper.Storage.Query(new GraphHandler(g), null, "CONSTRUCT { <" + _baseDoamin + "publisher>  ?p ?o } WHERE { <" + _baseDoamin + "publisher> ?p ?o }");

                    var strBuilder = new StringBuilder();
                    g.Triples.ForEach(triple => strBuilder.AppendLine((triple.ToString(new BaseNTripleFormatter()))));

                    var jsonLd = JsonLD.Core.JsonLdProcessor.FromRDF(strBuilder.ToString());

                    var jsonLdCompacted = JsonLD.Core.JsonLdProcessor.Compact(jsonLd, _context,
                        new JsonLD.Core.JsonLdOptions());

                    var document = (JObject)jsonLdCompacted["@graph"].Children().FirstOrDefault(n =>n["@type"].ToString() == "Contract" || n["@type"].ToString() == "SelectAttachments" || n["@type"].ToString() == "SelectAmendments");

                    if (document != null)
                    {
                        if (document["publisher"] != null)
                        {
                            document["publisher"] = jsonLdCompacted["@graph"].Children().FirstOrDefault(n => n["@type"].ToString() == "Publisher");
                        }

                        if (document["implementation"] != null)
                        {
                            document["implementation"] = jsonLdCompacted["@graph"].Children().FirstOrDefault(n => n["@id"].ToString() == document["implementation"]["@id"].ToString());

                            var implementation = document["implementation"];
                            if (implementation != null && implementation["milestones"] != null)
                            {
                                var milestones = new JArray();
                                foreach (var milestoneToken in implementation["milestones"])
                                {
                                    var milestone = jsonLdCompacted["@graph"].Children().FirstOrDefault(n => n["@id"].ToString() == ((JValue) milestoneToken).Value.ToString());
                                    milestones.Add(milestone);
                                }
                                implementation["milestones"] = milestones;
                            }
                        }

                        if (document["amount"] != null)
                        {
                            document["amount"] = jsonLdCompacted["@graph"].Children().FirstOrDefault(n => n["@id"].ToString() == document["amount"]["@id"].ToString());
                        }

                        if (document["versions"] != null)
                        {
                            var versions = new JArray();
                            foreach (var versionToken in document["versions"])
                            {
                                var version = jsonLdCompacted["@graph"].Children().FirstOrDefault(n => n["@id"].ToString() == ((JObject) versionToken)["@id"].ToString());
                                versions.Add(version);
                            }
                            document["versions"] = versions;
                        }

                        document.WriteTo(_writter);
                    }
                }

                if (value.Contains("party"))
                {
                    var g = new Graph();
                    R2RmlStorageWrapper.Storage.Query(new GraphHandler(g), null, "CONSTRUCT { <" + value + ">  ?p ?o } WHERE { <" + value + "> ?p ?o }");
                    R2RmlStorageWrapper.Storage.Query(new GraphHandler(g), null, "CONSTRUCT { <" + value + "/address>  ?p ?o } WHERE { <" + value + "/address> ?p ?o }");

                    var strBuilder = new StringBuilder();
                    g.Triples.ForEach(triple => strBuilder.AppendLine(triple.ToString(new BaseNTripleFormatter())));

                    var jsonLd = JsonLD.Core.JsonLdProcessor.FromRDF(strBuilder.ToString());

                    var jsonLdCompacted = JsonLD.Core.JsonLdProcessor.Compact(jsonLd, _context,
                        new JsonLD.Core.JsonLdOptions());

                    var parties = jsonLdCompacted["@graph"].Children().Where(n => n["@type"].ToString() == "Party").ToList();
                    foreach (var partyToken in parties)
                    {
                        var party = (JObject)partyToken;
                        if (party["address"] != null)
                        {
                            party["address"] = jsonLdCompacted["@graph"].Children().FirstOrDefault(n => n["@id"].ToString() == party["address"]["@id"].ToString());
                        }

                        partyToken.WriteTo(_writter);
                    }
                }
            }

            return true;
        }

        protected override bool HandleVariableInternal(string var)
        {
            return true;
        }
    }
}