namespace TriadaEndpoint.Web.Rdf
{
    public static class SparqlQueryConstants
    {
        //TODO - maybe, move to configuration file

        //Namespaceses
        public const string BasePrefix = "PREFIX : <http://tiny.cc/open-contracting#> ";
        public const string JsonLdContractContext = "http://tiny.cc/open-contracting_context";

        //Selects
        public const string SelectContracts = BasePrefix + "SELECT * WHERE { ?contracts a :Contract }";
        public const string SelectAmendments = BasePrefix + "SELECT * WHERE { ?amendments a :Amendment }";
        public const string SelectAttachments = BasePrefix + "SELECT * WHERE { ?attachments a :Attachment }";
        public const string SelectParties = BasePrefix + "SELECT * WHERE { ?parties a :party }";
        public const string SelectFiles = BasePrefix + "SELECT ?files WHERE { ?_ :document ?files }";
        public const string SelectBySubject = "SELECT * WHERE { @subject ?p ?o }";

        //Constructs
        public const string ConstructContracts = BasePrefix + "CONSTRUCT { ?s ?p ?o } WHERE { ?s ?p ?o ; a :Contract }";
        public const string ConstructAmendments = BasePrefix + "CONSTRUCT { ?s ?p ?o } WHERE { ?s ?p ?o ; a :Amendment }";
        public const string ConstructAttachments = BasePrefix + "CONSTRUCT { ?s ?p ?o } WHERE {?s ?p ?o ; a :Attachment }";
        public const string ConstructParties = BasePrefix + "CONSTRUCT { ?s ?p ?o } WHERE { ?s ?p ?o ; a :party }";
        public const string ConstructBySubject = "CONSTRUCT { @subject ?p ?o } WHERE { @subject ?p ?o }";
        public const string ConstructAll = "CONSTRUCT { ?s ?p ?o } WHERE {?s ?p ?o }";
    }
}