using System.Configuration;

namespace TriadaEndpoint.Web.TriadaDUL
{
    public class DULConfig : ConfigurationSection
    {
        private static readonly DULConfig ConfigSection = ConfigurationManager.GetSection("DULConfig") as DULConfig;

        public static DULConfig Settings
        {
            get
            {
                return ConfigSection;
            }
        }

        [ConfigurationProperty("Url", IsRequired = true)]
        public string Url
        {
            get { return (string)this["Url"]; }
            set { this["Url"] = value; }
        }

        [ConfigurationProperty("LogOnModul", IsRequired = true)]
        public string LogOnModul
        {
            get { return (string)this["LogOnModul"];}
            set { this["LogOnModul"] = value;}
        }

        [ConfigurationProperty("Subject", IsRequired = true)]
        public string Subject
        {
            get { return (string)this["Subject"]; }
            set { this["Subject"] = value; }
        }

        [ConfigurationProperty("Login", IsRequired = true)]
        public string Login
        {
            get { return (string)this["Login"]; }
            set { this["Login"] = value; }
        }

        [ConfigurationProperty("Password", IsRequired = true)]
        public string Password
        {
            get { return (string)this["Password"]; }
            set { this["Password"] = value; }
        }

        [ConfigurationProperty("Version", IsRequired = true)]
        public string Version
        {
            get { return (string)this["Version"]; }
            set { this["Version"] = value; }
        }

        [ConfigurationProperty("Identificator", IsRequired = true)]
        public string Identificator
        {
            get { return (string)this["Identificator"]; }
            set { this["Identificator"] = value; }
        }

        [ConfigurationProperty("UserNameForFileMeta", IsRequired = true)]
        public string UserNameForFileMeta
        {
            get { return (string)this["UserNameForFileMeta"]; }
            set { this["UserNameForFileMeta"] = value; }
        }
    }
}