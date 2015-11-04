using System.Text;
using TriadaEndpoint.DotNetRDF.Utils;
using VDS.RDF;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.DotNetRDF.Formatters
{
    public class BaseCsvFormatter : BaseFormatter
    {
        public BaseCsvFormatter() : base("CSV")
        {
        }

        public BaseCsvFormatter(string formatName) : base(formatName)
        {
        }

        protected override string FormatUriNode(IUriNode u, TripleSegment? segment)
        {
            var output = new StringBuilder();
            output.Append('"');
            output.Append(FormatUri(u.Uri));
            output.Append('"');
            return output.ToString();
        }

        protected override string FormatLiteralNode(ILiteralNode l, TripleSegment? segment)
        {
            var output = new StringBuilder();
            output.Append('"');
            output.Append(((ILiteralNode)W3CSpecHelper.FormatNode(l)).Value);
            output.Append('"');
            return output.ToString();
        }

        public override string Format(Triple t)
        {
            return this.Format(t.Subject, new TripleSegment?(TripleSegment.Subject)) + "," + this.Format(t.Predicate, new TripleSegment?(TripleSegment.Predicate)) + "," + this.Format(t.Object, new TripleSegment?(TripleSegment.Object));
        }
    }
}