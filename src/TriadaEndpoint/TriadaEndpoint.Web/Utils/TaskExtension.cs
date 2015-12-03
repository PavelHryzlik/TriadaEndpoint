using System;
using System.Threading.Tasks;

namespace TriadaEndpoint.Web.Utils
{
    /// <summary>
    /// Extension of tasks
    /// </summary>
    public static class TaskExtension
    {
        /// <summary>
        /// Extension method to handle exception 
        /// </summary>
        /// <param name="task"></param>
        /// <param name="onFaulted"></param>
        /// <returns></returns>
        public static Task OnException(this Task task, Action<Exception> onFaulted)
        {
            task.ContinueWith(c =>
            {
                var excetion = c.Exception;
                onFaulted(excetion);
            },
                TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously);
            return task;
        }
    }
}