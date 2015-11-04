using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using TriadaEndpoint.DotNetRDF.Formatters;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Writing;

namespace TriadaEndpoint.DotNetRDF.Writters
{
    public class JsonLdWriter : IRdfWriter, IPrettyPrintingWriter
    {
        private bool _prettyprint = true;
        private Uri _context;

        /// <summary>
        /// Gets/Sets Pretty Print Mode for the Writer
        /// </summary>
        public bool PrettyPrintMode
        {
            get
            {
                return _prettyprint;
            }
            set
            {
                _prettyprint = value;
            }
        }

        public Uri Context
        {
            get
            {
                return _context;
            }
            set
            {
                _context = value;
            }
        }

        /// <summary>
        /// Saves a Graph in Json-LD syntax to the given File
        /// </summary>
        /// <param name="g">Graph to save</param>
        /// <param name="filename">Filename to save to</param>
        public void Save(IGraph g, string filename)
        {
            var output = new StreamWriter(filename, false, new UTF8Encoding(Options.UseBomForUtf8));
            Save(g, output);
        }

        /// <summary>
        /// Saves a Graph to an arbitrary output stream
        /// </summary>
        /// <param name="g">Graph to save</param>
        /// <param name="output">Stream to save to</param>
        public void Save(IGraph g, TextWriter output)
        {
            try
            {
                GenerateOutput(g, output);
                output.Close();
            }
            catch
            {
                //Close the Output Stream
                output.Close();
                throw;
            }
        }

        /// <summary>
        /// Internal method which generates the Json-LD Output for a Graph
        /// </summary>
        /// <param name="g">Graph to save</param>
        /// <param name="output">Stream to save to</param>
        private void GenerateOutput(IGraph g, TextWriter output)
        {
            if (_context == null)
                RaiseWarning("Warning: Json-LD context is null.");

            var strBuilder = new StringBuilder();
            g.Triples.ForEach(triple => strBuilder.AppendLine(triple.ToString(new BaseNTripleFormatter())));
      
            var jsonLd = JsonLD.Core.JsonLdProcessor.FromRDF(strBuilder.ToString());

            var jsonLdCompacted = _context != null 
                ? JsonLD.Core.JsonLdProcessor.Compact(jsonLd, JsonLD.Util.JSONUtils.FromURL(_context), new JsonLD.Core.JsonLdOptions())
                : jsonLd;

            var documents = Enumerable.Where<JToken>(jsonLdCompacted["@graph"].Children(), n => n["@type"].ToString() == "Contract" || n["@type"].ToString() == "Attachment" || n["@type"].ToString() == "Amendment").ToList();
            foreach (var dokumentToken in documents)
            {
                var document = (Newtonsoft.Json.Linq.JObject)dokumentToken;

                if (document["publisher"] != null)
                {
                    document["publisher"] = Enumerable.FirstOrDefault<JToken>(jsonLdCompacted["@graph"].Children(), n => n["@type"].ToString() == "Publisher");
                }

                if (document["implementation"] != null)
                {
                    document["implementation"] = Enumerable.FirstOrDefault<JToken>(jsonLdCompacted["@graph"].Children(), n => n["@id"].ToString() == document["implementation"]["@id"].ToString());

                    var implementation = document["implementation"];
                    if (implementation != null && implementation["milestones"] != null)
                    {
                        var milestones = new Newtonsoft.Json.Linq.JArray();
                        foreach (var milestoneToken in implementation["milestones"])
                        {
                            var milestone = Enumerable.FirstOrDefault<JToken>(jsonLdCompacted["@graph"].Children(), n => n["@id"].ToString() == ((Newtonsoft.Json.Linq.JValue)milestoneToken).Value.ToString());
                            milestones.Add(milestone);
                        }
                        implementation["milestones"] = milestones;
                    }
                }

                if (document["versions"] != null)
                {
                    var versions = new Newtonsoft.Json.Linq.JArray();
                    foreach (var versionToken in document["versions"])
                    {
                        var version = Enumerable.FirstOrDefault<JToken>(jsonLdCompacted["@graph"].Children(), n => n["@id"].ToString() == ((Newtonsoft.Json.Linq.JObject)versionToken)["@id"].ToString());
                        versions.Add(version);
                    }
                    document["versions"] = versions;
                }
            }

            var parties = Enumerable.Where<JToken>(jsonLdCompacted["@graph"].Children(), n => n["@type"].ToString() == "Party").ToList();
            foreach (var partyToken in parties)
            {
                var party = (Newtonsoft.Json.Linq.JObject)partyToken;
                if (party["address"] != null)
                {
                    party["address"] = Enumerable.FirstOrDefault<JToken>(jsonLdCompacted["@graph"].Children(), n => n["@id"].ToString() == party["address"]["@id"].ToString());
                }
            }

            var id = "contracts_release_" + DateTime.Now.Date.ToString("yyyyMMdd");
            var resultJsonLd = new Newtonsoft.Json.Linq.JObject
            {
                { "@context" , _context },
                { "id" , id },
                { "published" , DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz") },
                { "language" , "cs" },
                { "documents" , new Newtonsoft.Json.Linq.JArray { documents } },
                { "parties" , new Newtonsoft.Json.Linq.JArray { parties } }
            };

            //Get the Writer and Configure Options
            var writer = new Newtonsoft.Json.JsonTextWriter(output)
            {
                Formatting = _prettyprint ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None
            };
            resultJsonLd.WriteTo(writer);
        }


        private void RaiseWarning(String message)
        {
            if (Warning != null)
            {
                Warning(message);
            }
        }

        /// <summary>
        /// Event which is raised when there is a non-fatal issue with the RDF being output
        /// </summary>
        public event RdfWriterWarning Warning;

        /// <summary>
        /// Gets the String representation of the writer which is a description of the syntax it produces
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "JSON-LD";
        }

    }
}