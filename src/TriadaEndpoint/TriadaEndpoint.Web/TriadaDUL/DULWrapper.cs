using System;
using System.IO;
using Triada.Dul;

namespace TriadaEndpoint.Web.TriadaDUL
{
    public static class DULWrapper
    {
        /// <summary>
        /// Triada Data storage api
        /// </summary>
        private static DulApi _dulApi;

        /// <summary>
        /// Gets the Triada Data storage api
        /// </summary>
        /// <value>Data storage</value>
        public static DulApi DulApi { get { return _dulApi; } }

        /// <summary>
        /// Initialize Triada Data storage, typically when application started.
        /// </summary>
        public static void InitializeDulApi()
        {
            StartException = null;

            try
            {
                _dulApi = new DulApi(new api { Url = DULConfig.Settings.Url }, DULConfig.Settings.UserNameForFileMeta);
            }
            catch (Exception e)
            {
                StartException = e;
            }
        }

        /// <summary>
        /// Get File from Triada Data storage
        /// </summary>
        /// <param name="guid">Identificator of the file</param>
        /// <returns></returns>
        public static MemoryStream GetFile(Guid guid)
        {
            _dulApi.LogOn(DULConfig.Settings.Subject, 
                         DULConfig.Settings.LogOnModul, 
                         DULConfig.Settings.Login, 
                         DULConfig.Settings.Password, 
                         DULConfig.Settings.Version, 
                         new Guid(DULConfig.Settings.Identificator));

            return _dulApi.LoadFile(guid);
        }

        /// <summary>
        /// Gets the start exception
        /// </summary>
        /// <value>The start exception</value>
        public static Exception StartException { get; private set; }
    }
}