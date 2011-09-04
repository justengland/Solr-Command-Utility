using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolrCommand.Core
{
    /// <summary>
    /// CoreStatus contains information about the current state of a Solr core.
    /// </summary>
    public class CoreStatus
    {
        //Fields
        private string _coreName;
        private string _status;
        private string _timeElapsed;
        private string _timeTaken;
        private bool _commandIsRunning;
        private string _totalRowsFetched;
        private string _totalDocumentsSkipped;
        private int _totalDocumentsProcessed;
        private bool _isRolledback;
        private string _indexVersion;
        private string _committed;
        private string _optimized;
        private int _documentCount;
        private string _serverUri;


        //Properties
        public string ServerUri
        {
            get { return _serverUri; }
            set { _serverUri = value; }
        }
        
        public string CoreName
        {
            get { return _coreName; }
            set { _coreName = value; }
        }
        
        public string Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public string TimeElapsed
        {
            get { return _timeElapsed; }
            set { _timeElapsed = value; }
        }

        public string TimeTaken
        {
            get { return _timeTaken; }
            set { _timeTaken = value; }
        }

        public bool CommandIsRunning
        {
            get { return _commandIsRunning; }
            set { _commandIsRunning = value; }
        }

        public string TotalRowsFetched
        {
            get { return _totalRowsFetched; }
            set { _totalRowsFetched = value; }
        }

        public string TotalDocumentsSkipped
        {
            get { return _totalDocumentsSkipped; }
            set { _totalDocumentsSkipped = value; }
        }

        public int TotalDocumentsProcessed
        {
            get { return _totalDocumentsProcessed; }
            set { _totalDocumentsProcessed = value; }
        }

        public bool IsRolledback
        {
            get { return _isRolledback; }
            set { _isRolledback = value; }
        }

        public string IndexVersion
        {
            get { return _indexVersion; }
            set { _indexVersion = value; }
        }

        public string Committed
        {
            get { return _committed; }
            set { _committed = value; }
        }

        public string Optimized
        {
            get { return _optimized; }
            set { _optimized = value; }
        }

        public int DocumentCount
        {
            get { return _documentCount; }
            set { _documentCount = value; }
        }
        
    }
}
