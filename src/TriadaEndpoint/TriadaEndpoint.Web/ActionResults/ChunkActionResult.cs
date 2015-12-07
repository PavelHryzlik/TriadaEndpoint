using System.Diagnostics;
using System.Reflection;
using System.Web.Mvc;
using log4net;
using TriadaEndpoint.DotNetRDF.BaseHandlers;

namespace TriadaEndpoint.Web.ActionResults
{
    /// <summary>
    /// Special action result for writing ouput data from handlers to output
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChunkActionResult<T> : ActionResult where T : IChunkHandler
    {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly T Handler;
        protected readonly string MimeType;
        protected readonly int FlushGranularity = 50;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="handler">handler</param>
        /// <param name="mimeType">mimeType</param>
        public ChunkActionResult(T handler, string mimeType)
        {
            Handler = handler;
            MimeType = mimeType;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="handler">handler</param>
        /// <param name="mimeType">mimeType</param>
        /// <param name="flushGranularity">granularity of flush to output</param>
        public ChunkActionResult(T handler, string mimeType, int flushGranularity)
        {
            Handler = handler;
            MimeType = mimeType;
            FlushGranularity = flushGranularity;
        }

        /// <summary>
        /// Consume data from handler and flush that to output 
        /// </summary>
        /// <param name="cc"></param>
        public override void ExecuteResult(ControllerContext cc)
        {
            var stopWatch = Stopwatch.StartNew();

            cc.HttpContext.Response.ContentType = MimeType;

            int flushCounter = 0;
            foreach (var chunk in Handler.GetNextChunk())
            {
                cc.HttpContext.Response.Write(chunk);
                flushCounter++;

                if (flushCounter == FlushGranularity)
                {
                    cc.HttpContext.Response.Flush();
                    flushCounter = 0;
                }
            }

            stopWatch.Stop();
            _log.Info("ChunkActionResult - SparqlResult in " + stopWatch.ElapsedMilliseconds + "ms");
        }
    }
}