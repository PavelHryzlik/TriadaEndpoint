using System;
using System.Text;

namespace TriadaEndpoint.Web.Utils
{
    public static class ExceptionHelper
    {
        /// <summary>
        /// Parse multiple exceptions to one string
        /// </summary>
        /// <param name="ae"></param>
        /// <returns></returns>
        public static string ParseMultiException(AggregateException ae)
        {
            var stringBuilder = new StringBuilder();
            foreach (var e in ae.InnerExceptions)
            {
                stringBuilder.AppendLine(e.Message);
            }
            return stringBuilder.ToString();
        }
    }
}