﻿using System.ComponentModel.DataAnnotations;

namespace TriadaEndpoint.Web.Models
{
    /// <summary>
    /// Class represents viewmodel for SPARQL endpoint
    /// </summary>
    public class QueryViewModel
    {
        public QueryViewModel()
        {
            ResultFormat = ResultFormats.Html;
        }

        [Required]
        [Display(Name = "Query")]
        public string Query { get; set; }

        [Required]
        [Display(Name = "ResultFormat")]
        public ResultFormats ResultFormat { get; set; }
    }

    /// <summary>
    /// Avaible formats to output 
    /// </summary>
    public enum ResultFormats
    {
        [Display(Name = "HTML")]
        Html,
        [Display(Name = "Turtle")]
        Turtle,
        [Display(Name = "JSON")]
        Json,
        [Display(Name = "N-Tripples")]
        NTripples,
        [Display(Name = "XML")]
        Xml,
        [Display(Name = "RDF/XML")]
        RdfXml,
        [Display(Name = "CSV")]
        Csv
    }
}