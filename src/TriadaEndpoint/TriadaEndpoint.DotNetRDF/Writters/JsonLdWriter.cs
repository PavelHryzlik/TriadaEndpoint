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
    /// <summary>
    /// Class parsing RDF data to Json-LD reprezentation, according to contract data standard
    /// http://standard.zindex.cz/doku.php/cs/standard/publication
    /// </summary>
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

            // Extract RDF data as NTriples
            var strBuilder = new StringBuilder();
            g.Triples.ForEach(triple => strBuilder.AppendLine(triple.ToString(new BaseNTripleFormatter())));
      
            // Load NTriples to Json-LD
            var jsonLd = JsonLD.Core.JsonLdProcessor.FromRDF(strBuilder.ToString());

            // Load Json-LD context and compact with Json-LD data
            var jsonLdCompacted = _context != null 
                ? JsonLD.Core.JsonLdProcessor.Compact(jsonLd, JsonLD.Util.JSONUtils.FromURL(_context), new JsonLD.Core.JsonLdOptions())
                : jsonLd;

            // Specific formatting for data (due to Contract data standard)
            // Get all documents
            var documents = jsonLdCompacted["@graph"].Children().Where(n => n["@type"].ToString() == "Contract" || n["@type"].ToString() == "Attachment" || n["@type"].ToString() == "Amendment").ToList();
            foreach (var dokumentToken in documents)
            {
                var document = (JObject)dokumentToken;

                // Get publisher block and set it under corresponding document block
                if (document["publisher"] != null)
                {
                    document["publisher"] = jsonLdCompacted["@graph"].Children().FirstOrDefault(n => n["@type"].ToString() == "Publisher");
                }

                // Get implementation block and set it under corresponding document block
                if (document["implementation"] != null)
                {
                    document["implementation"] = jsonLdCompacted["@graph"].Children().FirstOrDefault(n => n["@id"].ToString() == document["implementation"]["@id"].ToString());

                    // Get milestones blocks and set it under corresponding implementation block
                    var implementation = document["implementation"];
                    if (implementation?["milestones"] != null)
                    {
                        var milestones = new JArray();
                        foreach (var milestoneToken in implementation["milestones"])
                        {
                            var milestone = jsonLdCompacted["@graph"].Children().FirstOrDefault(n => n["@id"].ToString() == ((JValue)milestoneToken).Value.ToString());
                            milestones.Add(milestone);
                        }
                        implementation["milestones"] = milestones;
                    }
                }

                // Get amount block and set it under corresponding document block
                if (document["amount"] != null)
                {
                    document["amount"] = jsonLdCompacted["@graph"].Children().FirstOrDefault(n => n["@id"].ToString() == document["amount"]["@id"].ToString());
                }

                // Get versions blocks and set it under corresponding document block
                if (document["versions"] != null)
                {
                    var versions = new JArray();
                    foreach (var versionToken in document["versions"])
                    {
                        var version = jsonLdCompacted["@graph"].Children().FirstOrDefault(n => n["@id"].ToString() == ((JObject)versionToken)["@id"].ToString());
                        versions.Add(version);
                    }
                    document["versions"] = versions;
                }
            }

            // Get all parties
            var parties = jsonLdCompacted["@graph"].Children().Where(n => n["@type"].ToString() == "Party").ToList();
            foreach (var partyToken in parties)
            {
                // Get address block and set it under corresponding party block
                var party = (JObject)partyToken;
                if (party["address"] != null)
                {
                    party["address"] = jsonLdCompacted["@graph"].Children().FirstOrDefault(n => n["@id"].ToString() == party["address"]["@id"].ToString());
                }
            }

            // Define base document structure
            var id = "contracts_release_" + DateTime.Now.Date.ToString("yyyyMMdd");
            var resultJsonLd = new JObject
            {
                { "@context" , _context },
                { "id" , id },
                { "published" , DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz") },
                { "language" , "cs" },
                { "documents" , new JArray { documents } },
                { "parties" , new JArray { parties } }
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
            Warning?.Invoke(message);
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