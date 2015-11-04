using TriadaEndpoint.Web.Models;

namespace TriadaEndpoint.Web.Utils
{
    public static class MimeTypeHelper
    {
        public static string GetMimeType(ResultFormats resultFormats)
        {
            switch (resultFormats)
            {
                case ResultFormats.Turtle:
                    return "text/turtle";
                case ResultFormats.Json:
                    return "application/json";
                case ResultFormats.NTripples:
                    return "text/n-triples";
                case ResultFormats.Xml:
                    return "text/xml";
                case ResultFormats.RdfXml:
                    return "text/rdf+xml";
                case ResultFormats.Csv:
                    return "text/csv";
                default:
                    return "text/html";
            }
        }
    }
}