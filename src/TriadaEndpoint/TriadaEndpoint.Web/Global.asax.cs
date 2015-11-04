using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using log4net.Config;
using TriadaEndpoint.Web.Models;
using TriadaEndpoint.Web.R2Rml;
using TriadaEndpoint.Web.TriadaDUL;

namespace TriadaEndpoint.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            R2RmlStorageWrapper.InitializeR2RmlStorage();
            XmlConfigurator.Configure();
            DULWrapper.InitializeDulApi();
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
