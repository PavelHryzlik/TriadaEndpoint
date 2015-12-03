using System.Web.Mvc;
using System.Web.Routing;

namespace TriadaEndpoint.Web
{
    /// <summary>
    /// This class registers base route rules.  
    /// </summary>
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapMvcAttributeRoutes();

            // Route for dump functionality
            routes.MapRoute(
                "dump",
                "dump",
                new { controller = "Main", action = "GetDump" }
            );

            // Route fo queries
            routes.MapRoute(
                "sparql",
                "sparql",
                new { controller = "Main", action = "GetSparqlQuery" }
            );

            // Default route
            routes.MapRoute(
                "Default", 
                "{controller}/{action}/{id}", 
                new { controller = "Main", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
