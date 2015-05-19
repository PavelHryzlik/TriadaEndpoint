using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using TriadaEndpoint.Controllers;
using TriadaEndpoint.Models;
using VDS.RDF.Configuration;

namespace TriadaEndpoint
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            ConfigurationLoader.AddObjectFactory(new R2RmlStorageFactoryForQueryHandler());

            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            R2RmlStorageWrapper.InitializeR2RmlStorage();
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
