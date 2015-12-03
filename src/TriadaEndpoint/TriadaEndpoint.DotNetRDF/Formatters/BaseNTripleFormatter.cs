using System;
using System.Text;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.DotNetRDF.Formatters
{
    /// <summary>
    /// Base formatter to parse Triples to NTriples
    /// </summary>
    public class BaseNTripleFormatter : BaseFormatter
    {
        private readonly BlankNodeOutputMapper _bnodeMapper = new BlankNodeOutputMapper(WriterHelper.IsValidBlankNodeID);

        /// <summary>
        /// Creates a new NTriples Formatter
        /// </summary>
        public BaseNTripleFormatter()
            : base("BaseNTriples") { }

        /// <summary>
        /// Creates a new NTriples Formatter
        /// </summary>
        /// <param name="formatName">Format Name</param>
        protected BaseNTripleFormatter(String formatName)
            : base(formatName) { }

        /// <summary>
        /// Formats a URI Node
        /// </summary>
        /// <param name="u">URI Node</param>
        /// <param name="segment">Triple Segment</param>
        /// <returns>Formatted Uri node</returns>
        protected override string FormatUriNode(IUriNode u, TripleSegment? segment)
        {
            var output = new StringBuilder();
            output.Append('<');
            output.Append(FormatUri(u.Uri));
            output.Append('>');
            return output.ToString();
        }

        /// <summary>
        /// Formats a Literal Node
        /// </summary>
        /// <param name="l">Literal Node</param>
        /// <param name="segment">Triple Segment</param>
        /// <returns>Formatted Literal</returns>
        protected override string FormatLiteralNode(ILiteralNode l, TripleSegment? segment)
        {
            var output = new StringBuilder();

            output.Append('"');
            output.Append(((ILiteralNode)W3CSpecHelper.FormatNode(l)).Value);
            output.Append('"');

            if (!l.Language.Equals(String.Empty))
            {
                output.Append('@');
                output.Append(l.Language.ToLower());
            }
            else if (l.DataType != null)
            {
                output.Append("^^<");
                output.Append(FormatUri(l.DataType));
                output.Append('>');
            }

            return output.ToString();
        }

        /// <summary>
        /// Formats a Blank Node
        /// </summary>
        /// <param name="b">Blank Node</param>
        /// <param name="segment">Triple Segment</param>
        /// <returns>Formatted Blank node</returns>
        protected override string FormatBlankNode(IBlankNode b, TripleSegment? segment)
        {
            return "_:" + _bnodeMapper.GetOutputID(b.InternalID);
        }
    }
}