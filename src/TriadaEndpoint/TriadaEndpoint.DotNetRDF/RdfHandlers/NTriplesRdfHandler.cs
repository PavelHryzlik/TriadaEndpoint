using System;
using System.Text;
using TriadaEndpoint.DotNetRDF.Formatters;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;

namespace TriadaEndpoint.DotNetRDF.RdfHandlers
{
    /// <summary>
    /// Handler formatting Triples (RDF graph) to NTriples representation
    /// </summary>
    public class NTriplesRdfHandler : BaseRdfHandler
    {
        private readonly StringBuilder _tripleCollection = new StringBuilder();
        public StringBuilder TripleCollection => _tripleCollection;

        /// <summary>
        /// Handler constructor
        /// </summary>
        public NTriplesRdfHandler()
        {
            
        }

        /// <summary>
        /// Handler constructor with income triple collection
        /// </summary>
        /// <param name="tripleCollection">triple collection</param>
        public NTriplesRdfHandler(StringBuilder tripleCollection)
        {
            _tripleCollection = tripleCollection;
        }

        private Uri _baseUri;
        public Uri BaseUri => _baseUri;

        /// <summary>
        /// Parse incoming Triple to NTriples
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        protected override bool HandleTripleInternal(Triple t)
        {
            _tripleCollection.AppendLine(t.ToString(new BaseNTripleFormatter()));

            return true;
        }

        /// <summary>
        /// Method to handle Base Uri
        /// </summary>
        /// <param name="baseUri">Variable</param>
        /// <returns></returns>
        protected override bool HandleBaseUriInternal(Uri baseUri)
        {
            _baseUri = baseUri;
            return true;
        }

        public override bool AcceptsAll => true;
    }
}