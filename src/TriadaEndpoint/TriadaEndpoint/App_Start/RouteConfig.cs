using System.Web.Mvc;
using System.Web.Routing;

namespace TriadaEndpoint
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapMvcAttributeRoutes();

            routes.MapRoute(
                "sparql",
                "sparql",
                new { controller = "Main", action = "GetSparqlQuery" }
            );

            routes.MapRoute(
                "Default", 
                "{controller}/{action}/{id}", 
                new { controller = "Main", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
