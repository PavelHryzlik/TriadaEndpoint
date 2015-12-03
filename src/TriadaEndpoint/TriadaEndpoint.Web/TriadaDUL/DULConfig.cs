using System.Configuration;

namespace TriadaEndpoint.Web.TriadaDUL
{
    /// <summary>
    /// Class of settting for Triada Data storage, parsing values from web.config
    /// </summary>
    public class DULConfig : ConfigurationSection
    {
        // Get DULConfig section from web.config
        private static readonly DULConfig ConfigSection = ConfigurationManager.GetSection("DULConfig") as DULConfig;

        public static DULConfig Settings
        {
            get
            {
                return ConfigSection;
            }
        }

        /// <summary>
        /// Url of the Triada Data storage
        /// </summary>
        [ConfigurationProperty("Url", IsRequired = true)]
        public string Url
        {
            get { return (string)this["Url"]; }
            set { this["Url"] = value; }
        }

        // Logging method to Triada Data storage
        [ConfigurationProperty("LogOnModul", IsRequired = true)]
        public string LogOnModul
        {
            get { return (string)this["LogOnModul"];}
            set { this["LogOnModul"] = value;}
        }

        // Logging subject
        [ConfigurationProperty("Subject", IsRequired = true)]
        public string Subject
        {
            get { return (string)this["Subject"]; }
            set { this["Subject"] = value; }
        }

        // Login to Triada Data storage
        [ConfigurationProperty("Login", IsRequired = true)]
        public string Login
        {
            get { return (string)this["Login"]; }
            set { this["Login"] = value; }
        }

        // Password to Triada Data storage
        [ConfigurationProperty("Password", IsRequired = true)]
        public string Password
        {
            get { return (string)this["Password"]; }
            set { this["Password"] = value; }
        }

        // Vesrion of the Triada Data storage
        [ConfigurationProperty("Version", IsRequired = true)]
        public string Version
        {
            get { return (string)this["Version"]; }
            set { this["Version"] = value; }
        }

        // GUID identificator for log
        [ConfigurationProperty("Identificator", IsRequired = true)]
        public string Identificator
        {
            get { return (string)this["Identificator"]; }
            set { this["Identificator"] = value; }
        }

        // User name for metadata
        [ConfigurationProperty("UserNameForFileMeta", IsRequired = true)]
        public string UserNameForFileMeta
        {
            get { return (string)this["UserNameForFileMeta"]; }
            set { this["UserNameForFileMeta"] = value; }
        }
    }
}