using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using log4net.Config;
using TriadaEndpoint.Web.R2Rml;
using TriadaEndpoint.Web.TriadaDUL;

namespace TriadaEndpoint.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        /// <summary>
        /// Application start
        /// </summary>
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            R2RmlStorageWrapper.InitializeR2RmlStorage(); // Initialize R2RML storage
            XmlConfigurator.Configure(); // Initialize XML serializer for logging
            DULWrapper.InitializeDulApi(); // Initialize Triada Data store wrapper
        }

        /// <summary>
        /// Application end.
        /// </summary>
        protected void Application_End()
        {
            R2RmlStorageWrapper.DisposeR2RmlStorage();
        }
    }
}
