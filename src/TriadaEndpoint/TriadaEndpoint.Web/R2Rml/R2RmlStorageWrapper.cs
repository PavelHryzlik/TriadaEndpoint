using System;
using System.IO;
using Slp.r2rml4net.Storage;
using Slp.r2rml4net.Storage.Bootstrap;
using TCode.r2rml4net;
using TCode.r2rml4net.Mapping.Fluent;

namespace TriadaEndpoint.Web.R2Rml
{
    public class R2RmlStorageWrapper
    {
        /// <summary>
        /// The storage
        /// </summary>
        private static R2RMLStorage _storage;

        /// <summary>
        /// Gets the storage.
        /// </summary>
        /// <value>The storage.</value>
        public static R2RMLStorage Storage { get { return _storage; } }

        /// <summary>
        /// Initialize R2RmlStorage, typically when application started.
        /// </summary>
        public static void InitializeR2RmlStorage()
        {
            StartException = null;

            try
            {
                var mappingPath = System.Configuration.ConfigurationManager.AppSettings["r2rmlInputScript"];
                var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["r2rmlstoreconnection"].ConnectionString;

                var path = System.Web.Hosting.HostingEnvironment.MapPath(mappingPath);

                IR2RML mapping;
                using (var fs = new FileStream(path, FileMode.Open))
                {
                    mapping = R2RMLLoader.Load(fs);
                }

                _storage = new R2RMLStorage((new DefaultSqlDbFactory()).CreateSQLDb(connectionString), mapping, new DefaultR2RMLStorageFactory());
            }
            catch (Exception e)
            {
                StartException = e;
            }
        }

        /// <summary>
        /// Dispose R2RmlStorage, typically when application ended.
        /// </summary>
        public static void DisposeR2RmlStorage()
        {
            if (_storage != null)
                _storage.Dispose();
        }

        /// <summary>
        /// Gets the start exception.
        /// </summary>
        /// <value>The start exception.</value>
        public static Exception StartException { get; private set; }
    }
}