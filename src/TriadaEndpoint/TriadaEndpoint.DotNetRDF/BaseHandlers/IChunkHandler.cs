using System.Collections.Generic;

namespace TriadaEndpoint.DotNetRDF.BaseHandlers
{
    /// <summary>
    /// Interface for handlers using simple producer-consumer workaround
    /// </summary>
    public interface IChunkHandler
    {
        // Consume next items
        IEnumerable<string> GetNextChunk();
    }
}
