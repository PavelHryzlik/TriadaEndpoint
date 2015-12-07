using System;
using System.Linq;
using System.Reflection;
using System.Text;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing.Formatting;

namespace TriadaEndpoint.DotNetRDF.LazyRdfHandlers
{
    /// <summary>
    /// Handler formatting Triples (RDF graph) by defined formatter
    /// </summary>
    public class ThroughLazyRdfHandler : LazyRdfHandler
    {
        private ITripleFormatter _formatter;
        private readonly Type _formatterType;
        private INamespaceMapper _formattingMapper;
        private readonly StringBuilder _resultItem;

        /// <summary>
        /// Handler constructor
        /// </summary>
        /// <param name="formatterType">type of formatter</param>
        public ThroughLazyRdfHandler(Type formatterType)
        {
            _formattingMapper = new QNameOutputMapper();
            if (formatterType == null)
            {
                throw new ArgumentNullException("formatterType", "Cannot use a null formatter type");
            }
            _formatterType = formatterType;
            _resultItem = new StringBuilder();
        }

        /// <summary>
        /// Handler constructor
        /// </summary>
        /// <param name="formatter">instance of formatter</param>
        public ThroughLazyRdfHandler(ITripleFormatter formatter)
        {
            _formattingMapper = new QNameOutputMapper();
            _formatter = formatter ?? new NTriplesFormatter();
            _resultItem = new StringBuilder();
        }

        /// <summary>
        /// Write end of the document
        /// </summary>
        /// <param name="ok"></param>
        protected override void EndRdfInternal(bool ok)
        {
            if (_formatter is IGraphFormatter)
            {
                _resultItem.AppendLine(((IGraphFormatter)_formatter).FormatGraphFooter());
            }
            AddToQueue(_resultItem.ToString());
            CompleteQueue();
        }

        /// <summary>
        /// Method to handle Base Uri
        /// </summary>
        /// <param name="baseUri">Variable</param>
        /// <returns></returns>
        protected override bool HandleBaseUriInternal(Uri baseUri)
        {
            _resultItem.Clear();
            if (_formatter is IBaseUriFormatter)
            {
               _resultItem.AppendLine(((IBaseUriFormatter)_formatter).FormatBaseUri(baseUri));
            }
            AddToQueue(_resultItem.ToString());
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="namespaceUri"></param>
        /// <returns></returns>
        protected override bool HandleNamespaceInternal(string prefix, Uri namespaceUri)
        {
            _resultItem.Clear();
            _formattingMapper?.AddNamespace(prefix, namespaceUri);
            if (_formatter is INamespaceFormatter)
            {
                _resultItem.AppendLine(((INamespaceFormatter)_formatter).FormatNamespace(prefix, namespaceUri));
            }
            AddToQueue(_resultItem.ToString());
            return true;
        }

        /// <summary>
        /// Parse incoming Triple
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        protected override bool HandleTripleInternal(Triple t)
        {
            _resultItem.Clear();
            _resultItem.AppendLine(_formatter.Format(t));
            AddToQueue(_resultItem.ToString());
            return true;
        }

        /// <summary>
        /// Write start of the document, initialize input formatter
        /// </summary>
        protected override void StartRdfInternal()
        {
            _resultItem.Clear();
            if (_formatterType != null)
            {
                _formatter = null;
                _formattingMapper = new QNameOutputMapper();
                ConstructorInfo[] constructors = _formatterType.GetConstructors();
                Type o = typeof(QNameOutputMapper);
                Type type2 = typeof(INamespaceMapper);
                foreach (ConstructorInfo info in from c in constructors
                                                 orderby c.GetParameters().Count() descending
                                                 select c)
                {
                    ParameterInfo[] parameters = info.GetParameters();
                    try
                    {
                        if (parameters.Length == 1)
                        {
                            if (parameters[0].ParameterType == o)
                            {
                                _formatter = Activator.CreateInstance(_formatterType, _formattingMapper) as ITripleFormatter;
                            }
                            else if (parameters[0].ParameterType == type2)
                            {
                                _formatter = Activator.CreateInstance(_formatterType, _formattingMapper) as ITripleFormatter;
                            }
                        }
                        else if (parameters.Length == 0)
                        {
                            _formatter = Activator.CreateInstance(_formatterType) as ITripleFormatter;
                        }
                        if (_formatter != null)
                        {
                            break;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                if (_formatter == null)
                {
                    throw new RdfParseException("Unable to instantiate a ITripleFormatter from the given Formatter Type " + _formatterType.FullName);
                }
            }
            if (_formatter is IGraphFormatter)
            {
                _resultItem.AppendLine(((IGraphFormatter)_formatter).FormatGraphHeader(_formattingMapper));
            }
            AddToQueue(_resultItem.ToString());
        }

        public override bool AcceptsAll => true;
    }
}
