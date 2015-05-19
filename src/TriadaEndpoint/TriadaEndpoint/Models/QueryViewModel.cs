using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TriadaEndpoint.Models
{
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

    public enum ResultFormats
    {
        Html,
        Turtle,
        JsonLD
    }
}