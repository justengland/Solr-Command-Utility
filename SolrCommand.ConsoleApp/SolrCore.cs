using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using System.Net.Mail;
using System.Reflection;
using System.IO;


namespace SolrCommand.Core
{
    public class SolrCore
    {
        //Enums
        public enum Commands
        {
            BackupCore,
            CheckForEqualCores,
            CommitCore,
            CompareCoreStatus,
            CreateDeltaIndex,
            CreateFastDeltaIndex,
            CreateIndex,
            CreateIndexAndSwap,
            GetCoreStatus,
            GetIndexVersion,
            Help,
            IncrementCoreVersion,
            OptimizeCore,
            SwapCores,
            WatchIndexCreateProgress
        }

        //Fields
        static Timer _isIndexCompleteTimer;
        static DateTime _createIndexStartTime;
        static bool _doSwapAfterIndex = false;
        static bool _doFastDelta = false;
        static bool _doOptimize = false;
        static string _lastIndexTime;
        static bool _isProcessingIndex = false;
        private static int _indexCompleteTimerDurationSeconds;
        private static int _maxCreateIndexDurationInMinutes;
        static CoreStatus _indexingCoreStatusBaseline;
        static string _liveCoreName;

        //Constructors
        private SolrCore()
        {
            //This class is not meant to be instantiated
        }

        static void CreateIndex(string serverUri, string coreName, bool clean, bool doOptimize, string lastIndexTime)
        {
            try
            {

                _createIndexStartTime = DateTime.Now;

                if (string.IsNullOrEmpty(coreName))
                {
                    ProcessState.LogError("Invalid request. Please specify the core to create an index for by setting the CoreB parameter value.");
                    return;
                }

                _indexingCoreStatusBaseline = GetCoreStatus(serverUri, coreName);
                if (_indexingCoreStatusBaseline.Status != "idle")
                {
                    ProcessState.LogError(string.Format("Cannot build index on core {0} because the current core status is {1}. Please try again later (indexing) or restart the Solr core (locked).",
                        coreName,
                        _indexingCoreStatusBaseline.Status));
                    return;
                }

                //Kickoff SOLR Index command
                string cmd = string.Format("/solr/{0}/select?qt=%2Fdataimport&command=full-import&verbose=true&commit=true&clean={1}&optimize={2}&last_index_time={3}",
                            coreName.ToString().ToLower(),
                            clean.ToString().ToLower(),
                            doOptimize.ToString().ToLower(),
                            lastIndexTime.ToString());
                ProcessState.LogInfo(string.Format("Indexing Solr Core {0} with command {1}", coreName, cmd));

                XDocument statusXDocument = SolrServer.ExecuteQuery(serverUri, cmd, null);

                WatchIndexCreateProgress();
            }
            catch (Exception ex)
            {
                ProcessState.LogError(string.Format("Error while attempting to create new index: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Executes Solr command to start the creation of a new index on CoreB.  Starts timer to check index creation status.
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <param name="coreName">The name of the Solr core to use for the new index.</param>
        /// <param name="maxCreateIndexDurationInMinutes"></param>
        /// <param name="indexCompleteTimerDurationSeconds"></param>
        public static void CreateIndex(string serverUri, string coreName, int maxCreateIndexDurationInMinutes, int indexCompleteTimerDurationSeconds)
        {
            _maxCreateIndexDurationInMinutes = maxCreateIndexDurationInMinutes;
            _indexCompleteTimerDurationSeconds = indexCompleteTimerDurationSeconds;

            CreateIndex(serverUri, coreName, true, true, GetLastIndexTime(serverUri, coreName));
        }

        /// <summary>
        /// Executes Solr command to start the creation of a new index on CoreB.  Starts timer to check index creation status.
        /// On create index completion, swaps the liveCore (CoreA) with the stage core (CoreB).
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <param name="stageCore">The name of the stage core used to create the new index on.</param>
        /// <param name="liveCore">The name of the live core used to swap with the stage core after indexing completes.</param>
        public static void CreateIndexAndSwap(string serverUri, string stageCore, string liveCore, int maxCreateIndexDurationInMinutes, int indexCompleteTimerDuration)
        {
            try
            {
                if (string.IsNullOrEmpty(stageCore) || string.IsNullOrEmpty(liveCore))
                {
                    ProcessState.LogError("Invalid request. Please specify the live core and stage core by setting the CoreA and CoreB parameters respectively.");
                    return;
                }

                _doSwapAfterIndex = true;
                _liveCoreName = liveCore;
                CreateIndex(serverUri, stageCore, maxCreateIndexDurationInMinutes, indexCompleteTimerDuration);
            }
            catch (Exception ex)
            {
                ProcessState.LogError(string.Format("Error while attempting to create new index and swap: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Executes Solr command to start the creation of a delta index on the specified core.  Starts timer to check index creation status.
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <param name="coreName">The name of the Solr core to create the delta index on.</param>
        public static void CreateDeltaIndex(string serverUri, string coreName, int maxCreateIndexDurationInMinutes, int indexCompleteTimerDuration, bool doFastDelta, bool doOptimize)
        {
            try
            {
                //Set member vars - these are used CheckForIndexCompletion to determine additional actions to take upon completion of the delta index creation
                _doFastDelta = doFastDelta;
                _doOptimize = doOptimize;
                _createIndexStartTime = DateTime.Now;
                _maxCreateIndexDurationInMinutes = maxCreateIndexDurationInMinutes;
                _indexCompleteTimerDurationSeconds = indexCompleteTimerDuration;

                if (string.IsNullOrEmpty(coreName))
                {
                    ProcessState.LogError("Invalid request. Please specify the core to create a delta index for by setting the CoreA parameter value.");
                    return;
                }

                //Check core status before processing delta index
                _indexingCoreStatusBaseline = GetCoreStatus(serverUri, coreName);
                if (_indexingCoreStatusBaseline == null)
                {
                    ProcessState.LogError("Cannot build delta index on core {0} because the current core status could not be determined.");
                    return;
                }
                else if (_indexingCoreStatusBaseline.Status != "idle")
                {
                    ProcessState.LogError(string.Format("Cannot build a delta index on core {0} because the current core status is {1}. Please try again later (indexing) or restart the Solr core (locked).",
                        coreName, _indexingCoreStatusBaseline.Status));
                    return;
                }

                //If we are doing fast delta, we need to use the Last Index Time for both step 1 and 2, so we'll set the 
                //_lastIndexTime member varialbe to last index time from the dataimport.properties file, which will be used
                //for step to instead of reloading the dateTime again from the file (step one updates the dataimport.properties file).
                _lastIndexTime = GetLastIndexTime(serverUri, coreName);
                if (ProcessState.HasError)
                {
                    ProcessState.LogError("Aborting CreateFastDeltaIndex process.");
                    return;
                }

                //If we are doing a fast delta and optimizing, we don't want to optimize until step 2, which will use 
                //the member var _doOptimize set above
                if (doFastDelta && doOptimize)
                {
                    doOptimize = false;
                }
                
                //Kickoff SOLR Index command
                string cmd = string.Format("/solr/{0}/select?qt=%2Fdataimport&verbose=true&clean=false&commit=true&optimize={1}&command=delta-import&last_index_time={2}",
                    coreName,
                    doOptimize.ToString().ToLower(),
                    _lastIndexTime.ToString());

                ProcessState.LogInfo(string.Format("Creating Delta Index for core {0} with command {1}.", coreName, cmd));
                XDocument statusXDocument = SolrServer.ExecuteQuery(serverUri,
                        cmd, null);

                WatchIndexCreateProgress();
            }
            catch (Exception ex)
            {
                ProcessState.LogError(string.Format("Error while attempting to create delta index on core: {0}; Error: {1}", coreName, ex.Message));
            }
        }

        private static string GetLastIndexTime(string serverUri, string coreName)
        {
            string cmd = string.Format("/solr/{0}/admin/file/?file=dataimport.properties", coreName);
            ProcessState.LogInfo(string.Format("Loading dataimport.properties to get the last_index_time with command: {0}.", cmd));

            string dataimportProperties = SolrCommand.Core.SolrServer.ExecuteQuery(serverUri, cmd, 1);

            //dataimportProperties.Substring(start + 21, dataimportProperties.IndexOf(Environment.NewLine, start + 21) - (start + 21)).Replace("\\", "")

            string key = "item.last_index_time=";
            int start = dataimportProperties.IndexOf(key) + key.Length;
            int end = dataimportProperties.IndexOf(Environment.NewLine, start);

            string dateString = dataimportProperties.Substring(start, end - start).Replace("\\", "");

            if (string.IsNullOrEmpty(dateString))
            {
                dateString = "1970-01-01 00:00:00";
                ProcessState.LogWarn("Could not load Last Import Date from dataimport.properties file.  Setting the date to " + dateString);
            }

            return dateString;
        }

        /// <summary>
        /// Issues command to Solr to create a backup of the specified core.  The backup is created in the data directory of the specified core 
        /// in a folder named snapshot.[dateTimeStamp].  Checks to make sure the core status is idle before issuing the backup command.
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <param name="coreName">The name of the Solr core to backup.</param>
        /// <returns>Returns true if Solr response is 'OK', otherwise returns false.</returns>
        public static bool BackupCore(string serverUri, string coreName)
        {
            try
            {
                if (string.IsNullOrEmpty(coreName))
                {
                    ProcessState.LogError("Invalid request. Please specify the core to backup.");
                    return false;
                }

                _indexingCoreStatusBaseline = GetCoreStatus(serverUri, coreName);
                if (_indexingCoreStatusBaseline.Status != "idle")
                {
                    ProcessState.LogError(string.Format("Cannot backup core {0} because the current core status is {1}. Please try again later (indexing) or restart the Solr core (locked).",
                        coreName, _indexingCoreStatusBaseline.Status));
                    return false;
                }

                //Kickoff SOLR backup command
                string cmd = string.Format("/solr/{0}/replication?command=backup", coreName);
                ProcessState.LogInfo(string.Format("Creating backup for core {0} with command {1}.", coreName, cmd));
                XDocument statusXDocument = SolrServer.ExecuteQuery(serverUri, cmd, null);

                var results = statusXDocument.Descendants("str");
                string response = SolrCore.GetElementValueByAttribute(results, "name", "status");
                ProcessState.LogInfo("Backup Solr response was: " + response);
                return response == "OK";
            }
            catch (Exception ex)
            {
                ProcessState.LogError(string.Format("Error while attempting to backup core: {0}; Error: {1}", coreName, ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Starts timer to get the core status at a regular interval.
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        public static void WatchIndexCreateProgress(string serverUri, string coreName, int maxCreateIndexDurationInMinutes, int indexCompleteTimerDuration)
        {
            _createIndexStartTime = DateTime.Now;
            _maxCreateIndexDurationInMinutes = maxCreateIndexDurationInMinutes;
            _indexCompleteTimerDurationSeconds = indexCompleteTimerDuration;

            if (_indexingCoreStatusBaseline == null)
            {
                _indexingCoreStatusBaseline = GetCoreStatus(serverUri, coreName);
            }

            WatchIndexCreateProgress();
        }
        static void WatchIndexCreateProgress()
        {
            //Start timer to check if the Indexing is complete
            //NOTE: This timer will not restart - we will do it in the callback depending on the status
            TimerCallback isIndexCompleteTimerCallback = new TimerCallback(CheckForIndexCompletion);
            _isIndexCompleteTimer = new Timer(isIndexCompleteTimerCallback, null, 0, Timeout.Infinite);

            //Keep this process alive until the index is finished processing - check for completion every .5 seconds
            _isProcessingIndex = true;
            do
            {
                Thread.Sleep(1000);
            } while (_isProcessingIndex);
        }

        /// <summary>
        /// Executes Solr command to increment the core index version on CoreB.
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <param name="coreName">The name of the Solr core to increment the index version on.</param>
        public static void IncrementCoreIndexVersion(string serverUri, string coreName)
        {
            try
            {
                if (string.IsNullOrEmpty(coreName))
                {
                    ProcessState.LogError("Invalid request. Please specify the core to increment the index version for by setting the CoreB parameter value.");
                }

                string currentCoreIndexVersion = GetCoreIndexVersion(serverUri, coreName);

                //ProcessState.LogInfo();
                //ProcessState.LogInfo(string.Format("Preparing to increment core Index Version for core {0}. ", coreName));

                //add a bogus document with id 'changeMe'
                XDocument statusXDocumentAdd = SolrServer.ExecuteQuery(serverUri,
                        string.Format("/solr/{0}/update?stream.body=<update><add><doc><field name=\"id\">changeMe</field><field name=\"hgid\">changeMe</field></doc></add></update>",
                        coreName), null);

                CommitCore(serverUri, coreName);

                //delete the bogus document with id 'changeMe'
                XDocument statusXDocumentDelete = SolrServer.ExecuteQuery(serverUri,
                        string.Format("/solr/{0}/update?stream.body=<update><delete><query>id:changeMe</query></delete></update>", coreName), null);

                CommitCore(serverUri, coreName);

                string newCoreIndexVersion = GetCoreIndexVersion(serverUri, coreName);
                if (newCoreIndexVersion.Equals(currentCoreIndexVersion))
                {
                    ProcessState.LogError("Failed to increment the index version.");
                }
                else
                {
                    ProcessState.LogInfo(string.Format("Incremented core version from {0} to {1} for core {2}.", currentCoreIndexVersion, newCoreIndexVersion, coreName));
                }
            }
            catch (Exception ex)
            {
                ProcessState.LogError(string.Format("Error while attempting to increment core index version: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Executes Solr command to swap cores between CoreB and CoreA.
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <param name="coreA">The name of the live core to swap.</param>
        /// <param name="coreB">The name of the stage core to swap.</param>
        public static void SwapCores(string serverUri, string coreA, string coreB)
        {
            try
            {
                if (string.IsNullOrEmpty(coreB) || string.IsNullOrEmpty(coreA))
                {
                    ProcessState.LogError("Invalid request.  Please specify the cores to swap by setting the CoreA and CoreB parameter values.");
                    return;
                }

                CoreStatus stageCoreStatus = GetCoreStatus(serverUri, coreB);
                if (stageCoreStatus.DocumentCount == 0)
                {
                    ProcessState.LogError(string.Format("The current Document Count for core {0} is zero.  Aborting SwapCores command!", coreB));
                    return;
                }
                else
                {
                    IncrementCoreBVersionAboveCoreA(serverUri, coreA, coreB);


                    string cmd = string.Format("/solr/admin/cores?action=SWAP&core={0}&other={1}", coreA, coreB);
                    ProcessState.LogInfo();
                    ProcessState.LogInfo(string.Format("Preparing to swap cores {0} and {1} with command {2}.", coreA, coreB, cmd));

                    XDocument statusXDocument = SolrServer.ExecuteQuery(serverUri, cmd, null);

                    ProcessState.LogInfo(string.Format("Swap Cores Completed at {0}.", DateTime.Now.ToString()));
                }
            }
            catch (Exception ex)
            {
                ProcessState.LogError(string.Format("Error while attempting to swap cores {0} and {1}: {2}",
                    coreA, coreB,
                    ex.Message));
            }
        }

        /// <summary>
        /// Checks to see if the core version of CoreB is greater than the core version of CoreA.  If it is not, the core version of CoreB
        /// will be incremented and this method will be called recursively until the CoreB version is greater than CoreA.
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <param name="coreA">The name of the live core.</param>
        /// <param name="coreB">The name of the staging core which should have a greater version number than CoreA.</param>
        private static void IncrementCoreBVersionAboveCoreA(string serverUri, string coreA, string coreB)
        {
            try
            {
                long coreAVersion = 0;
                long coreBVersion = 0;

                if (long.TryParse(GetCoreIndexVersion(serverUri, coreA), out coreAVersion) &&
                    long.TryParse(GetCoreIndexVersion(serverUri, coreB), out coreBVersion))
                {
                    if (coreBVersion <= coreAVersion)
                    {
                        IncrementCoreIndexVersion(serverUri, coreB);

                        //Now call thyself recursively until coreB Version is greater than coreA Version
                        IncrementCoreBVersionAboveCoreA(serverUri, coreA, coreB);
                    }
                }
            }
            catch (Exception ex)
            {
                ProcessState.LogError(string.Format("Error while attampting to increment the core version of core {0} above core {1}: {2}", coreB, coreA, ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <param name="coreName"></param>
        /// <returns></returns>
        public static string BuildCoreStatusSummaryString(string serverUri, string coreName)
        {
            //Get core status
            CoreStatus coreAStatus;
            if (string.IsNullOrEmpty(coreName))
            {
                coreAStatus = new CoreStatus();
            }
            else
            {
                coreAStatus = GetCoreStatus(serverUri, coreName);
            }

            return BuildCoreStatusSummaryString(coreAStatus);
        }

        /// <summary>
        /// Returns string with summary information for the specified core names.
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <param name="coreA"></param>
        /// <param name="coreB"></param>
        /// <returns></returns>
        public static string BuildCoreStatusSummaryString(string serverUri, string coreA, string coreB)
        {
            //Get core status
            CoreStatus coreAStatus;
            if (string.IsNullOrEmpty(coreA))
            {
                coreAStatus = new CoreStatus();
            }
            else
            {
                coreAStatus = GetCoreStatus(serverUri, coreA);
            }

            CoreStatus coreBStatus;
            if (string.IsNullOrEmpty(coreB))
            {
                coreBStatus = new CoreStatus();
            }
            else
            {
                coreBStatus = GetCoreStatus(serverUri, coreB);
            }

            return BuildCoreStatusSummaryString(coreAStatus, coreBStatus);
        }
        public static string BuildCoreStatusSummaryString(CoreStatus coreStatus)
        {
            if (coreStatus == null)
            {
                coreStatus = new CoreStatus();
                coreStatus.CoreName = "Core param not set";
            }

            StringBuilder sb = new StringBuilder();
            int pad = 27;
            sb.AppendLine("");

            sb.AppendLine(formatCoreStatusLine("Core Name:", coreStatus.CoreName, pad));
            sb.AppendLine(formatCoreStatusLine("Status:", coreStatus.Status, pad));

            sb.AppendLine(formatCoreStatusLine("Command Is Running:", coreStatus.CommandIsRunning.ToString(), pad));
            sb.AppendLine(formatCoreStatusLine("Time Elapsed:", coreStatus.TimeElapsed, pad));
            sb.AppendLine(formatCoreStatusLine("Time Taken:", coreStatus.TimeTaken, pad));
            sb.AppendLine(formatCoreStatusLine("Total Rows Fetched:", coreStatus.TotalRowsFetched, pad));
            sb.AppendLine(formatCoreStatusLine("Total Documents Processed:", coreStatus.TotalDocumentsProcessed.ToString(), pad));
            sb.AppendLine(formatCoreStatusLine("Total Documents Skipped:", coreStatus.TotalDocumentsSkipped, pad));

            sb.AppendLine(formatCoreStatusLine("Committed:", coreStatus.Committed, pad));
            sb.AppendLine(formatCoreStatusLine("Optimized:", coreStatus.Optimized, pad));
            sb.AppendLine(formatCoreStatusLine("Index Version:", coreStatus.IndexVersion, pad));
            sb.AppendLine(formatCoreStatusLine("Document Count:", coreStatus.DocumentCount.ToString(), pad));

            return sb.ToString();
        }
        public static string BuildCoreStatusSummaryString(CoreStatus coreAStatus, CoreStatus coreBStatus)
        {
            if (coreAStatus == null)
            {
                coreAStatus = new CoreStatus();
                coreAStatus.CoreName = "CoreA param not set";
            }

            if (coreBStatus == null)
            {
                coreBStatus = new CoreStatus();
                coreBStatus.CoreName = "CoreB param not set";
            }

            StringBuilder sb = new StringBuilder();
            int pad = 27;
            sb.AppendLine("");

            sb.AppendLine(formatCoreStatusLine("Core Name:", coreAStatus.CoreName, coreBStatus.CoreName, pad));
            sb.AppendLine(formatCoreStatusLine("Status:", coreAStatus.Status, coreBStatus.Status, pad));

            sb.AppendLine(formatCoreStatusLine("Command Is Running:", coreAStatus.CommandIsRunning.ToString(), coreBStatus.CommandIsRunning.ToString(), pad));
            sb.AppendLine(formatCoreStatusLine("Time Elapsed:", coreAStatus.TimeElapsed, coreBStatus.TimeElapsed, pad));
            sb.AppendLine(formatCoreStatusLine("Time Taken:", coreAStatus.TimeTaken, coreBStatus.TimeTaken, pad));
            sb.AppendLine(formatCoreStatusLine("Total Rows Fetched:", coreAStatus.TotalRowsFetched, coreBStatus.TotalRowsFetched, pad));
            sb.AppendLine(formatCoreStatusLine("Total Documents Processed:", coreAStatus.TotalDocumentsProcessed.ToString(), coreBStatus.TotalDocumentsProcessed.ToString(), pad));
            sb.AppendLine(formatCoreStatusLine("Total Documents Skipped:", coreAStatus.TotalDocumentsSkipped, coreBStatus.TotalDocumentsSkipped, pad));

            sb.AppendLine(formatCoreStatusLine("Committed:", coreAStatus.Committed, coreBStatus.Committed, pad));
            sb.AppendLine(formatCoreStatusLine("Optimized:", coreAStatus.Optimized, coreBStatus.Optimized, pad));
            sb.AppendLine(formatCoreStatusLine("Index Version:", coreAStatus.IndexVersion, coreBStatus.IndexVersion, pad));
            sb.AppendLine(formatCoreStatusLine("Document Count:", coreAStatus.DocumentCount.ToString(), coreBStatus.DocumentCount.ToString(), pad));

            return sb.ToString();
        }

        /// <summary>
        /// Returns string with summary information for the current properties/parameter values.
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <returns></returns>
		public static string BuildParamSummaryString(string command, string serverUri, string coreName, string serverUri2, string coreName2,
			string liveCore, string stageCore,
			int statusDuration, int timeout,
			bool doBackup, bool doOptimize)
        {
            int pad = 35;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("Request Parameter Summary");
			sb.AppendLine("-------------------------"); 
			sb.AppendLine("Command: ".PadRight(pad) + command.ToString());
            sb.AppendLine("ServerUri: ".PadRight(pad) + serverUri);

			if (!string.IsNullOrEmpty(serverUri2)) { sb.AppendLine("ServerUri2: ".PadRight(pad) + serverUri2); }
			if (!string.IsNullOrEmpty(liveCore)) { sb.AppendLine("LiveCore: ".PadRight(pad) + liveCore); }
			if (!string.IsNullOrEmpty(stageCore)) { sb.AppendLine("StageCore: ".PadRight(pad) + stageCore); }
			if (!string.IsNullOrEmpty(coreName)) { sb.AppendLine("CoreName: ".PadRight(pad) + coreName); }
			if (!string.IsNullOrEmpty(coreName2)) { sb.AppendLine("CoreName2: ".PadRight(pad) + coreName2); }

			sb.AppendLine("doBackup: ".PadRight(pad) + doBackup.ToString());
			sb.AppendLine("doOptimize: ".PadRight(pad) + doOptimize.ToString()); 

			sb.AppendLine("StatusDuration (seconds): ".PadRight(pad) + statusDuration.ToString()); 
			sb.AppendLine("Timeout (minutes): ".PadRight(pad) + timeout.ToString()); 

			sb.AppendLine("Notify: ".PadRight(pad) + Notification.Notify.ToString());
            sb.AppendLine("emailNotifyTo: ".PadRight(pad) + Notification.To);
            sb.AppendLine("emailNotifyFrom: ".PadRight(pad) + Notification.From);
            sb.AppendLine("emailNotifySmtpHost: ".PadRight(pad) + Notification.SmtpHost);

            sb.AppendLine("");

            return sb.ToString();
        }

        /// <summary>
        /// Executes Solr command to optimize the specified core.
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <param name="coreName">The name of the core to optimize.</param>
        public static void OptimizeCore(string serverUri, string coreName)
        {
            try
            {
                if (string.IsNullOrEmpty(coreName))
                {
                    throw new Exception("Invalid request.  Please specify the core to optimize by setting the CoreA parameter value.");
                }


                //Note: this has a 10 minute timout because it can take a long time to run...
                //Note: the waitFlush parameter has been pulled out of the current Solr release as of 1/24/2011.  This is supposed to
                //issue the command, then return immediately.  Currently, the command does not return a response until Optimize completes.
                string cmd = string.Format("/solr/{0}/update?optimize=true&waitFlush=false&waitSearcher=false", coreName);
                ProcessState.LogInfo(string.Format("Preparing to optimize core {0} with command {1}", coreName, cmd));

                XDocument statusXDocument = SolrServer.ExecuteQuery(serverUri, cmd, 10, null);
                //Alternate command: update?stream.body=<optimize waitFlush="false" waitSearcher="false"/>

                ProcessState.LogInfo("Optimize Completed at " + DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                ProcessState.LogError(string.Format("Error while attempting to optimize cores: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Executes Solr command to commit changes on the specified core.
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <param name="coreName">The name of the Solr core to commit.</param>
        public static void CommitCore(string serverUri, string coreName)
        {
            try
            {
                //NOTE: this could potentially take some time, so we are setting the timeout to 10 minutes...
                string cmd = string.Format("/solr/{0}/update?stream.body=<update><commit/></update>", coreName, 10);
                XDocument statusXDocument2 = SolrServer.ExecuteQuery(serverUri, cmd, null);
            }
            catch (Exception ex)
            {
                ProcessState.LogError(string.Format("Error while attempting to commit core {0}: {1}", coreName, ex.Message));
            }
        }

        /// <summary>
        /// Returns a string containing the current index version for the specified solr core.
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <param name="coreName">The name of the Solr core to get the current version for.</param>
        /// <returns></returns>
        public static string GetCoreIndexVersion(string serverUri, string coreName)
        {
            try
            {
                XDocument responseXDocument = SolrServer.ExecuteQuery(serverUri,
                    string.Format("/solr/{0}/admin/stats.jsp", coreName), null);

                var results = responseXDocument.Descendants("stat");
                string version = GetElementValueByAttribute(results, "name", "indexVersion").Trim();
                return version;
            }
            catch (Exception ex)
            {
                ProcessState.LogError(string.Format("Error in GetCoreIndexVersion; serverUri: {0}, coreName {1}; Error {2}", serverUri, coreName, ex.Message));
                return null;
            }
        }



        /// <summary>
        /// Executes Solr command to get the status of the specified core. 
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <param name="coreName">The name of the Solr core to get status for.</param>
        /// <returns>CoreStatus object containing details about the status of CoreB.</returns>
        public static CoreStatus GetCoreStatus(string serverUri, string coreName)
        {
            try
            {
                XDocument statusXDocument = SolrServer.ExecuteQuery(serverUri,
                        string.Format("/solr/{0}/select?clean=false&commit=true&qt=%2Fdataimport&command=status", coreName), null);

                var results = statusXDocument.Descendants("str");
                CoreStatus coreStatus = new CoreStatus();
                coreStatus.ServerUri = serverUri;
                coreStatus.CoreName = coreName;
                coreStatus.Status = GetElementValueByAttribute(results, "name", "status");
                coreStatus.TimeElapsed = GetElementValueByAttribute(results, "name", "Time Elapsed");
                coreStatus.TotalRowsFetched = GetElementValueByAttribute(results, "name", "Total Rows Fetched");
                coreStatus.TotalDocumentsSkipped = GetElementValueByAttribute(results, "name", "Total Documents Skipped");

                int docsProcessed = 0;
                int.TryParse(GetElementValueByAttribute(results, "name", "Total Documents Processed"), out docsProcessed);
                coreStatus.TotalDocumentsProcessed = docsProcessed;

                coreStatus.TimeTaken = GetElementValueByAttribute(results, "name", "Time taken "); //NOTE:  This has a space at the end because the response also contains a space!
                coreStatus.Committed = GetElementValueByAttribute(results, "name", "Committed");
                coreStatus.Optimized = GetElementValueByAttribute(results, "name", "Optimized");

                coreStatus.CommandIsRunning = (GetElementValueByAttribute(results, "name", "importResponse") == "A command is still running...");

                coreStatus.IsRolledback = (!string.IsNullOrEmpty(GetElementValueByAttribute(results, "name", "Rolledback")));

                coreStatus.IndexVersion = GetCoreIndexVersion(serverUri, coreName);
                coreStatus.DocumentCount = GetDocumentCount(serverUri, coreName);

                return coreStatus;
            }
            catch (Exception ex)
            {
                ProcessState.LogError(string.Format("Error in GetCoreStatus; serverUri: {0}, coreName {1}; Error {2}", serverUri, coreName, ex.Message));
                return null;
            }
        }

        /// <summary>
        /// Returns a string containing the current index version for the specified solr core.
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <param name="coreName">The name of the Solr core to get the current version for.</param>
        /// <returns></returns>
        public static CoreStatus GetSlaveCoreStatus(string serverUri, string coreName)
        {
            try
            {
                XDocument responseXDocument = SolrServer.ExecuteQuery(serverUri,
                    string.Format("/solr/{0}/admin/stats.jsp", coreName), null);

                var results = responseXDocument.Descendants("stat");

                CoreStatus coreStatus = new CoreStatus();
                coreStatus.ServerUri = serverUri;
                coreStatus.CoreName = coreName;
                int numDocs = 0;
                int.TryParse(GetElementValueByAttribute(results, "name", "numDocs").Trim(), out numDocs);
                coreStatus.DocumentCount = numDocs;
                coreStatus.IndexVersion = GetElementValueByAttribute(results, "name", "indexVersion").Trim();

                return coreStatus;
            }
            catch (Exception ex)
            {
                ProcessState.LogError(string.Format("Error in GetCoreIndexVersion; serverUri: {0}, coreName {1}; Error {2}", serverUri, coreName, ex.Message));
                return null;
            }
        }

        //Private Methods
        /// <summary>
        /// Returns a string containing the total number of documents in the specified Solr core.
        /// </summary>
        /// <param name="serverUri">The address of the Solr instance.</param>
        /// <param name="coreName">The name of the Solr core to get the document count for.</param>
        /// <returns></returns>
        static int GetDocumentCount(string serverUri, string coreName)
        {
            try
            {
                XDocument responseXDocument = SolrServer.ExecuteQuery(serverUri,
                    string.Format("/solr/{0}/select?q=*%3A*&start=0&rows=0", coreName), null);

                IEnumerable<XElement> results = responseXDocument.Descendants("result");
                string val = GetElementAttributeValueHavingAttribute(results, "name", "response", "numFound");
                int docCount = 0;
                int.TryParse(val, out docCount);
                return docCount;
            }
            catch (Exception ex)
            {
                ProcessState.LogError(string.Format("Error in GetDocumentCount; serverUri: {0}, coreName {1}; Error {2}", serverUri, coreName, ex.Message));
                return 0;
            }
        }

        static string formatCoreStatusLine(string header, string col1, string col2, int colWidth)
        {
            if (string.IsNullOrEmpty(header)) { header = ""; }
            if (string.IsNullOrEmpty(col1)) { col1 = ""; }
            if (string.IsNullOrEmpty(col2)) { col2 = ""; }


            return string.Format("{0}{1}{2}",
                header.PadRight(colWidth),
                col1.PadRight(colWidth),
                col2);
        }
        static string formatCoreStatusLine(string header, string col1, int colWidth)
        {
            if (string.IsNullOrEmpty(header)) { header = ""; }
            if (string.IsNullOrEmpty(col1)) { col1 = ""; }


            return string.Format("{0}{1}",
                header.PadRight(colWidth),
                col1.PadRight(colWidth));
        }

        /// <summary>
        /// Writes status of CoreB to ProcessState.Response.  Starts timer over if status is not idle and the
        /// threshold for max processing time has not been exceeded.  
        /// </summary>
        /// <param name="state"></param>
        static void CheckForIndexCompletion(object state)
        {
            CoreStatus coreStatus = GetCoreStatus(_indexingCoreStatusBaseline.ServerUri, _indexingCoreStatusBaseline.CoreName);

            if (coreStatus == null)
            {
                ProcessState.LogError("Could not determine Core Status.  GetCoreStatus returned null.");
                _isProcessingIndex = false;
                return;
            }
            else if (coreStatus.Status == "idle")
            {

                if (_doFastDelta)
                {
                    //Complete fast delta - Step 2
                    _doFastDelta = false;
                    ProcessState.LogInfo("Processsing step 2 of 2 for CreateFastDeltaIndex");
                    //important to set optimize and clean to false!
                    CreateIndex(_indexingCoreStatusBaseline.ServerUri, _indexingCoreStatusBaseline.CoreName, false, _doOptimize, _lastIndexTime);
                    return;
                }

                ProcessState.Response.AppendLine(string.Format("Status: {0}; Fetched: {1}; Processed: {2}; Skipped: {3}; Time: {4}",
                    coreStatus.Status,
                    coreStatus.TotalRowsFetched,
                    coreStatus.TotalDocumentsProcessed,
                    coreStatus.TotalDocumentsSkipped,
                    coreStatus.TimeTaken
                    ));

                //Check to see if the index was rolledback
                if (coreStatus.DocumentCount == 0)
                {
                    _isProcessingIndex = false;
                    ProcessState.LogWarn("CREATE INDEX FAILED! The index has ZERO documents.");
                    return;
                }
                else if (coreStatus.TotalDocumentsProcessed == 0)
                {
                    _isProcessingIndex = false;
                    ProcessState.LogInfo("We did not find any documents to process.");
                    return;
                }
                else if (coreStatus.IsRolledback)
                {
                    _isProcessingIndex = false;
                    ProcessState.LogError("CREATE INDEX FAILED! The index was rolled back.");
                    return;
                }

                ProcessState.LogInfo(string.Format("Index created for core {0} at {1}, optimized at {2}.",
                    coreStatus.CoreName,
                    coreStatus.Committed,
                    coreStatus.Optimized));

                //Check that the index has actually changed... DO NOT SWAP IF THE INDEX DID NOT CHANGE!
                if (coreStatus.IndexVersion.Equals(_indexingCoreStatusBaseline.IndexVersion))
                {
                    ProcessState.LogWarn(string.Format("The index version did not change on core {0}.  The index may not have been created successfully.",
                        _indexingCoreStatusBaseline.CoreName));
                    return;
                }
                else
                {
                    //Index changed... 
                    //Ok to swap if requested
                    if (_doSwapAfterIndex)
                    {
                        SwapCores(_indexingCoreStatusBaseline.ServerUri, _liveCoreName, _indexingCoreStatusBaseline.CoreName);
                        _doSwapAfterIndex = false;
                    }

                }

                _isProcessingIndex = false;
            }
            else if (coreStatus.Status == "busy")
            {

                string msg = string.Format("Status: {0}; Fetched: {1}; Processed: {2}; Skipped: {3}; Time: {4}",
                    coreStatus.Status,
                    coreStatus.TotalRowsFetched,
                    coreStatus.TotalDocumentsProcessed,
                    coreStatus.TotalDocumentsSkipped,
                    coreStatus.TimeElapsed
                    );

                //Note, we are intentionally writing to the console here in addition to appending to the ProcessState.Response so that the Console window
                //will display progress while processing.
                Console.WriteLine(msg);
                ProcessState.LogInfo(msg);

                //If TimeElapsed is greater than 4 hours, then print 'Time Elapsed has exceeded 4 hours.  We give up.  The query is probably blocked.'
                if (DateTime.Now.Subtract(_createIndexStartTime) > TimeSpan.FromMinutes(_maxCreateIndexDurationInMinutes))
                {
                    ProcessState.LogInfo();
                    ProcessState.LogError(string.Format("The max time allowed for creating the new Solr index has exceeded {0} minutes.  Aborting create index process.  Please check the status of the Solr server to determine the cause of this issue.",
                        _maxCreateIndexDurationInMinutes.ToString()));
                    _isProcessingIndex = false;
                    return;
                }

                //Start the timer over again to check for completion in 60 seconds
                _isIndexCompleteTimer.Change(_indexCompleteTimerDurationSeconds * 1000, Timeout.Infinite);
            }
            else
            {
                ProcessState.LogError(string.Format("Indexing Error.  Solr returned stats of {0}.  Indexing aborted.", coreStatus.Status));
                _isProcessingIndex = false;
                return;
            }
        }

        /// <summary>
        /// Parses XElement to return Value of element with specified attribute name and attribute value.
        /// </summary>
        /// <param name="results">The XElement collection to parse</param>
        /// <param name="attributeName">The name of the element attribute to find.</param>
        /// <param name="attributeValue">The value of the element attribute to find.</param>
        /// <returns>Value of the element, or an empty string if an element is not found with the specified attribute name and attribute value.</returns>
        static string GetElementValueByAttribute(IEnumerable<XElement> results, string attributeName, string attributeValue)
        {
            XElement resultElement = results.FirstOrDefault(element => element.Attribute(attributeName) != null
                                                                    && element.Attribute(attributeName).Value == attributeValue);


            return resultElement == null ? "" : resultElement.Value;
        }
        static string GetElementAttributeValueHavingAttribute(IEnumerable<XElement> results, string havingAttributeName, string havingAttributeValue, string selectValueForAttributeName)
        {
            XElement resultElement = results.FirstOrDefault(element => element.Attribute(havingAttributeName) != null
                                                        && element.Attribute(havingAttributeName).Value == havingAttributeValue);

            return resultElement != null && resultElement.Attribute(selectValueForAttributeName) != null ? resultElement.Attribute(selectValueForAttributeName).Value : "";

        }

    }
}
