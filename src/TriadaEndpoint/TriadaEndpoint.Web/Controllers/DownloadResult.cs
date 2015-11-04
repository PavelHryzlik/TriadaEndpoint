using System.IO;
using System.Web;
using System.Web.Mvc;

namespace TriadaEndpoint.Web.Controllers
{
    public class DownloadResult : FileStreamResult
    {
        public DownloadResult(Stream fileStream, string contentType)
            : base(fileStream, contentType)
        { }

        public long? FileSize { get; set; }

        protected override void WriteFile(HttpResponseBase response)
        {
            response.BufferOutput = false;

            if (FileSize.HasValue)
            {
                response.AddHeader("Content-Length", FileSize.ToString());
            }
            base.WriteFile(response);
        }
    }
}