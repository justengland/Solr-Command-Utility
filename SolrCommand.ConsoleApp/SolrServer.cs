using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Text.RegularExpressions;
using System.Web;

namespace SolrCommand.Core
{

    /// <summary>
    /// Class used to assist making solr web calls.
    /// </summary>
    internal sealed class SolrServer
    {
        //Fields
        private static string _solrServerBaseUri;


        //Properties
        public static string SolrServerBaseUri
        {
            get { return _solrServerBaseUri; }
            set { _solrServerBaseUri = value; }
        }

        //Methods
        internal static XDocument ExecuteQuery(string solrServerBaseUri, string path, params object[] arguments)
        {
            return ExecuteQuery(solrServerBaseUri, path, 1, arguments);
        }
        
        /// <summary>
        /// Download a document and convert it to an XDocument.
        /// </summary>
        /// <param name="source">The web address of the the file.</param>
        /// <returns>A <see cref="T:XDocument"/> that is parsed from the webservice.</returns>
        internal static XDocument ExecuteQuery(string solrServerBaseUri, string path, int timeoutMinutes, params object[] arguments)
        {
            if (!string.IsNullOrEmpty(solrServerBaseUri))
            {
                SolrServerBaseUri = solrServerBaseUri;
            }
           
            if (path == null)
            {
                throw new ArgumentNullException("path", "You must specify a source url.");
            }
            if (!path.ToUpperInvariant().StartsWith("/SOLR"))
            {
                throw new ArgumentException("The solr uri path must begin with /solr");
            }

            // todo: Add Solr url traceing.
            String requestUri = SolrServer.CreateSolrUri(path, arguments);

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(requestUri);
            webRequest.UserAgent = "SolrCommand/1.0.0.0";
            webRequest.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            webRequest.Timeout = timeoutMinutes * 60 * 1000; //10 min

            HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

            // todo: make sure I can use response compression
            // This works but is it necessary?
            Stream responseStream = responseStream = webResponse.GetResponseStream();
            if (webResponse.ContentEncoding.ToLower().Contains("gzip"))
                responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
            else if (webResponse.ContentEncoding.ToLower().Contains("deflate"))
                responseStream = new DeflateStream(responseStream, CompressionMode.Decompress);
            XmlReader reader = XmlReader.Create(responseStream);
            XDocument document = XDocument.Load(reader);

            reader.Close();
            responseStream.Dispose();

            return document;
        }

        internal static string ExecuteQuery(string solrServerBaseUri, string path, int timeoutMinutes)
        {
            if (!string.IsNullOrEmpty(solrServerBaseUri))
            {
                SolrServerBaseUri = solrServerBaseUri;
            }

            if (path == null)
            {
                throw new ArgumentNullException("path", "You must specify a source url.");
            }
            if (!path.ToUpperInvariant().StartsWith("/SOLR"))
            {
                throw new ArgumentException("The solr uri path must begin with /solr");
            }

            String requestUri = SolrServer.CreateSolrUri(path, null);

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(requestUri);
            webRequest.UserAgent = "SolrCommand/1.0.0.0";
            webRequest.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            webRequest.Timeout = timeoutMinutes * 60 * 1000; //10 min

            HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

            // todo: make sure I can use response compression
            // This works but is it necessary?
            Stream responseStream = responseStream = webResponse.GetResponseStream();
            if (webResponse.ContentEncoding.ToLower().Contains("gzip"))
                responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
            else if (webResponse.ContentEncoding.ToLower().Contains("deflate"))
                responseStream = new DeflateStream(responseStream, CompressionMode.Decompress);

            string document = "";
            using (StreamReader streamReader = new StreamReader(responseStream))
            {
                document = streamReader.ReadToEnd();
            }

            responseStream.Dispose();

            return document;
        }

        /// <summary>
        /// Escape parameters for solr.
        /// </summary>
        /// <remarks>
        /// This method was taken from the Solrnet project. I am not a big fan of Regex but it is effective.
        /// </remarks>
        /// <param name="value">The parameter value used to escape.</param>
        /// <returns>Returns <see cref="System.String"/> that is safe to use in solr url.</returns>
        internal static string EscapeSearchParameter(string value)
        {
            if (value == null || String.IsNullOrEmpty(value.Trim()))
            {
                throw new ArgumentNullException("value");
            }
            // todo: check for other malicious valuables
            string result = Regex.Replace(value, "(\\+|\\-|\\&\\&|\\|\\||\\!|\\{|\\}|\\[|\\]|\\^|\\(|\\)|\\\"|\\~|\\:|\\;|\\\\)", "\\$1");
            if (result.IndexOf(' ') != -1)
                result = string.Format("\"{0}\"", result);

            return result;
        }

        /// <summary>
        /// Using the string.format convention create a url using {0}.
        /// the path must begin with /solr and The parameters will be escaped and checked.
        /// </summary>
        /// <param name="path">The url of the solr query.</param>
        /// <param name="arguments">Objects to format into the url.</param>
        /// <returns>A Uri that can be used to query solr.</returns>
        private static String CreateSolrUri(string path, params object[] arguments)
        {
            if (path == null || string.IsNullOrEmpty(path.Trim()))
            {
                throw new ArgumentNullException("path");
            }
            if (!path.ToUpperInvariant().StartsWith("/SOLR"))
            {
                throw new ArgumentException("The solr uri path must begin with /solr");
            }

            if (arguments != null && arguments.Length > 0)
            {
                object[] validArguments = new object[arguments.Length];
                for (int i = 0; i < arguments.Length; i++)
                {
                    validArguments[i] = SolrServer.EscapeSearchParameter(arguments[i].ToString());
                }
                SolrServerBaseUri += String.Format(path, validArguments);
            }
            else
            {
                SolrServerBaseUri += path;
            }

            return SolrServerBaseUri;
        }
    }
}
