using System.Collections.Concurrent;
using System.Collections.Generic;
using TriadaEndpoint.DotNetRDF.BaseHandlers;
using VDS.RDF.Parsing.Handlers;

namespace TriadaEndpoint.DotNetRDF.SparqlLazyResultHandlers
{
    /// <summary>
    /// Very Simple Producer-consumer handler Sparql result handlers
    /// </summary>
    public abstract class LazyResultHandler : BaseResultsHandler, IChunkHandler
    {
        protected BlockingCollection<string> ResultQueue { get; set; }

        /// <summary>
        /// Constructor, initialize queue
        /// </summary>
        protected LazyResultHandler()
        {
            ResultQueue = new BlockingCollection<string>();
        }

        /// <summary>
        /// Add item to queue
        /// </summary>
        /// <param name="item">item</param>
        protected virtual void AddToQueue(string item)
        {
            ResultQueue.Add(item);
        }

        /// <summary>
        /// Indicate, that computation is completed
        /// </summary>
        protected virtual void CompleteQueue()
        {
            ResultQueue.CompleteAdding();
        }

        /// <summary>
        /// Consume next items
        /// </summary>
        public IEnumerable<string> GetNextChunk()
        {
            return ResultQueue.GetConsumingEnumerable();
        }
    }
}
