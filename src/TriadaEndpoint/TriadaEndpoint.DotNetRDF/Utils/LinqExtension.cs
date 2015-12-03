using System;
using System.Collections.Generic;

namespace TriadaEndpoint.DotNetRDF.Utils
{
    public static class LinqExtension
    {
        /// <summary>
        /// Enables foreach for Linq
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="enumeration">Input enumeration</param>
        /// <param name="action">Required action</param>
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }
    }
}