using System.Text;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.DotNetRDF.Formatters
{
    /// <summary>
    /// Base formatter to parse Triples to CSV lines
    /// </summary>
    public class BaseCsvFormatter : BaseFormatter
    {
        /// <summary>
        /// Creates a new Csv Formatter
        /// </summary>
        public BaseCsvFormatter() : base("CSV")
        {
        }

        /// <summary>
        /// Creates a new Csv Formatter
        /// </summary>
        /// <param name="formatName">Format Name</param>
        public BaseCsvFormatter(string formatName) : base(formatName)
        {
        }

        /// <summary>
        /// Formats a URI Node
        /// </summary>
        /// <param name="u">URI Node</param>
        /// <param name="segment">Triple Segment</param>
        /// <returns>Formatted Uri node</returns>
        protected override string FormatUriNode(IUriNode u, TripleSegment? segment)
        {
            var output = new StringBuilder();
            output.Append('"');
            output.Append(FormatUri(u.Uri));
            output.Append('"');
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
            return output.ToString();
        }

        /// <summary>
        /// Formats Triple
        /// </summary>
        /// <param name="t">Input Triple</param>
        /// <returns>Formatted Triple</returns>
        public override string Format(Triple t)
        {
            return Format(t.Subject, TripleSegment.Subject) + "," + Format(t.Predicate, TripleSegment.Predicate) + "," + Format(t.Object, TripleSegment.Object);
        }
    }
}