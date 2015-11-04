﻿using System;
using System.Text;
using TriadaEndpoint.DotNetRDF.Formatters;
using VDS.RDF;
using VDS.RDF.Parsing.Handlers;

namespace TriadaEndpoint.DotNetRDF.RdfHandlers
{
    public class NTriplesRdfHandler : BaseRdfHandler
    {
        private readonly StringBuilder _tripleCollection = new StringBuilder();
        public StringBuilder TripleCollection
        {
            get { return _tripleCollection; }
        }

        public NTriplesRdfHandler()
        {
            
        }

        public NTriplesRdfHandler(StringBuilder tripleCollection)
        {
            _tripleCollection = tripleCollection;
        }

        private Uri _baseUri;
        public Uri BaseUri
        {
            get { return _baseUri; }
        }

        protected override bool HandleTripleInternal(Triple t)
        {
            _tripleCollection.AppendLine(t.ToString(new BaseNTripleFormatter()));

            return true;
        }

        protected override bool HandleBaseUriInternal(Uri baseUri)
        {
            _baseUri = baseUri;
            return true;
        }

        public override bool AcceptsAll
        {
            get { return true; }
        }
    }
}