using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TriadaEndpoint.Models;

namespace TriadaEndpoint.Controllers
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