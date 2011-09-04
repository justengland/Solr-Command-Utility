using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolrCommand.Core
{
    /// <summary>
    /// Class used to manage the state of a process or sequence of processes.
    /// </summary>
    public class ProcessState
    {
        //Fields
        private static StringBuilder _response;
        private static bool _hasError;


        //Properties
        /// <summary>
        /// Gets or Sets the StringBuilder object used to store information about a process or sequence of processes during execution.
        /// </summary>
        public static StringBuilder Response
        {
            get
            {
                if (_response == null)
                {
                    _response = new StringBuilder();
                }
                return _response;
            }
            set { _response = value; }
        }

        /// <summary>
        /// Gets or Sets a boolean value to indicate of the process or sequence of processes has an error.
        /// </summary>
        public static bool HasError
        {
            get { return _hasError; }
            set { _hasError = value; }
        }

        //Public Methods
        /// <summary>
        /// Appends a line to the Response property with 'ERROR' prefix and sets the HasError property to true.
        /// </summary>
        /// <param name="message">String containing details about the error.</param>
        public static void LogError(string message)
        {
            HasError = true;
            Response.AppendLine(string.Format("{0} {1} {2}", DateTime.Now.ToString(), "ERROR", message));
        }

        /// <summary>
        /// Appends a line to the Response property with 'WARN' prefix.
        /// </summary>
        /// <param name="message">String containing details about the warning.</param>
        public static void LogWarn(string message)
        {
            Response.AppendLine(string.Format("{0} {1} {2}", DateTime.Now.ToString(), "WARN", message));
        }

        /// <summary>
        /// Writes an empty line to the Response StringBuilder
        /// </summary>
        public static void LogInfo()
        {
            Response.AppendLine();
        }

        /// <summary>
        /// Appends a line to the Response property with 'INFO' prefix.
        /// </summary>
        /// <param name="message">String containing information message.</param>
        public static void LogInfo(string message)
        {
            Response.AppendLine(string.Format("{0} {1} {2}", DateTime.Now.ToString(), "INFO", message));
        }
    }
}
