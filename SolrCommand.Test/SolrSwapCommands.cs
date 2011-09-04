using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SolrCommand.ConsoleApp;

namespace SolrCommand.Test
{
	[TestClass]
	public class SolrCommandCommands
	{
		const string serverUri = "http://localhost:8983";
		const string coreName = "swap2";
		const string liveCore = "swap1";
		const string stageCore = "swap2";

		const string emailNotifyTo = "someone@gmail.com";
		const string emailNotifySmtpHost = "smtp.gmail.com";

		const string IndexCompleteTimerDuration = "2";

		[TestMethod]
		public void GetCoreStatus()
		{
			string args = string.Format("cmd=GetCoreStatus serverUri={0} CoreName={1} Notify=All emailNotifyTo={2} emailNotifySmtpHost={3}",
				serverUri, coreName, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains(coreName)
				&& (Program.LogContent.Contains("idle") || Program.LogContent.Contains("busy"))
				);
		}

		[TestMethod]
		public void CompareCoreStatus()
		{
			string args = string.Format("cmd=CompareCoreStatus serverUri={0} LiveCore={1} StageCore={2}  Notify=All emailNotifyTo={3} emailNotifySmtpHost={4}",
				serverUri, liveCore, stageCore, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				(Program.LogContent.Contains(liveCore) && Program.LogContent.Contains(stageCore))
				&& (Program.LogContent.Contains("idle") || Program.LogContent.Contains("busy"))
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}

		[TestMethod]
		public void GetIndexVersion()
		{
			string args = string.Format("cmd=GetIndexVersion serverUri={0} CoreName={1} Notify=All emailNotifyTo={2} emailNotifySmtpHost={3}",
				serverUri, coreName, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains(string.Format("The current version for core {0} is", coreName))
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}

		[TestMethod]
		public void CreateIndex_noBackup()
		{
			string args = string.Format("cmd=CreateIndex serverUri={0} CoreName={1} StatusDuration=5 Timeout=90 Notify=All emailNotifyTo={2} emailNotifySmtpHost={3}",
				serverUri, coreName, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				!Program.LogContent.Contains("INFO Backup Solr response was: OK")
				&& Program.LogContent.Contains("INFO Index created for core " + coreName)
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}
		[TestMethod]
		public void CreateIndex_Backup()
		{
			string args = string.Format("cmd=CreateIndex serverUri={0} CoreName={1} doBackup=true StatusDuration=5 Timeout=90 Notify=All emailNotifyTo={2} emailNotifySmtpHost={3}",
				serverUri, coreName, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains("INFO Backup Solr response was: OK")
				&& Program.LogContent.Contains("INFO Index created for core " + coreName)
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}

		[TestMethod]
		public void CreateIndexAndSwap_noBackup()
		{
			string args = string.Format("cmd=CreateIndexAndSwap serverUri={0} LiveCore={1} StageCore={2} StatusDuration=5 Timeout=90 Notify=All emailNotifyTo={3} emailNotifySmtpHost={4}",
				serverUri, liveCore, stageCore, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				!Program.LogContent.Contains("INFO Backup Solr response was: OK")
				&& Program.LogContent.Contains("INFO Swap Cores Completed")
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}
		[TestMethod]
		public void CreateIndexAndSwap_Backup()
		{
			string args = string.Format("cmd=CreateIndexAndSwap serverUri={0} LiveCore={1} StageCore={2} doBackup=true StatusDuration=5 Timeout=90 Notify=All emailNotifyTo={3} emailNotifySmtpHost={4}",
				serverUri, liveCore, stageCore, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains("INFO Backup Solr response was: OK")
				&& Program.LogContent.Contains("INFO Swap Cores Completed")
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}

		[TestMethod]
		public void OptimizeCore()
		{
			string args = string.Format("cmd=OptimizeCore serverUri={0} CoreName={1} StatusDuration=5 Timeout=90 Notify=All emailNotifyTo={2} emailNotifySmtpHost={3}",
				serverUri, coreName, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains("INFO Optimize Completed")
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}

		[TestMethod]
		public void CommitCore()
		{
			string args = string.Format("cmd=CommitCore serverUri={0} CoreName={1} StatusDuration=5 Timeout=90 Notify=All emailNotifyTo={2} emailNotifySmtpHost={3}",
				serverUri, coreName, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains("INFO Successfully committed core " + coreName)
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}

		[TestMethod]
		public void IncrementCoreVersion()
		{
			string args = string.Format("cmd=IncrementCoreVersion serverUri={0} CoreName={1} StatusDuration=5 Timeout=90 Notify=All emailNotifyTo={2} emailNotifySmtpHost={3}",
				serverUri, coreName, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains("INFO Incremented core version")
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}

		[TestMethod]
		public void SwapCores()
		{
			string args = string.Format("cmd=SwapCores serverUri={0} LiveCore={1} StageCore={2} StatusDuration=5 Timeout=90 Notify=All emailNotifyTo={3} emailNotifySmtpHost={4}",
				serverUri, liveCore, stageCore, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains("INFO Swap Cores Completed")
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}

		[TestMethod]
		public void CreateDeltaIndex_noOptimize()
		{
			string args = string.Format("cmd=CreateDeltaIndex serverUri={0} CoreName={1} StatusDuration=5 Timeout=90 Notify=All emailNotifyTo={2} emailNotifySmtpHost={3}",
				serverUri, coreName, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains("INFO Creating Delta Index for core " + coreName)
				&& Program.LogContent.Contains("optimize=false")
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}
		[TestMethod]
		public void CreateDeltaIndex_Optimize()
		{
			string args = string.Format("cmd=CreateDeltaIndex serverUri={0} CoreName={1} doOptimize=true StatusDuration=5 Timeout=90  Notify=All emailNotifyTo={2} emailNotifySmtpHost={3}",
				serverUri, coreName, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains("INFO Creating Delta Index for core " + coreName)
				&& Program.LogContent.Contains("optimize=true")
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}

		[TestMethod]
		public void CreateFastDeltaIndex_Optimize_noBackup()
		{
			string args = string.Format("cmd=CreateFastDeltaIndex serverUri={0} CoreName={1} StatusDuration=5 Timeout=90 doOptimize=true Notify=All emailNotifyTo={2} emailNotifySmtpHost={3}",
				serverUri, coreName, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains("INFO Creating Delta Index for core " + coreName)
				&& Program.LogContent.Contains("optimize=false&command=delta-import") //for the first step
				&& Program.LogContent.Contains("command=full-import&verbose=true&commit=true&clean=false&optimize=true") //for the second step
				&& !Program.LogContent.Contains("INFO Backup Solr response was: OK")
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}

		[TestMethod]
		public void CreateFastDeltaIndex_noOptimize_noBackup()
		{
			string args = string.Format("cmd=CreateFastDeltaIndex serverUri={0} CoreName={1} StatusDuration=5 Timeout=90 Notify=All emailNotifyTo={2} emailNotifySmtpHost={3}",
				serverUri, coreName, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains("INFO Creating Delta Index for core " + coreName)
				&& Program.LogContent.Contains("optimize=false&command=delta-import") //for the first step
				&& Program.LogContent.Contains("command=full-import&verbose=true&commit=true&clean=false&optimize=false") //for the second step
				&& !Program.LogContent.Contains("INFO Backup Solr response was: OK")
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}
		[TestMethod]
		public void CreateFastDeltaIndex_Optimize_Backup()
		{
			string args = string.Format("cmd=CreateFastDeltaIndex serverUri={0} CoreName={1} doBackup=true StatusDuration=5 Timeout=90 doOptimize=true Notify=All emailNotifyTo={2} emailNotifySmtpHost={3}",
				serverUri, coreName, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains("INFO Creating Delta Index for core " + coreName)
				&& Program.LogContent.Contains("optimize=false&command=delta-import") //for the first step
				&& Program.LogContent.Contains("command=full-import&verbose=true&commit=true&clean=false&optimize=true") //for the second step
				&& Program.LogContent.Contains("INFO Backup Solr response was: OK")
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}

		[TestMethod]
		public void CreateFastDeltaIndex_noOptimize_Backup()
		{
			string args = string.Format("cmd=CreateFastDeltaIndex serverUri={0} CoreName={1} doBackup=true StatusDuration=5 Timeout=90 Notify=All emailNotifyTo={2} emailNotifySmtpHost={3}",
				serverUri, coreName, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains("INFO Creating Delta Index for core " + coreName)
				&& Program.LogContent.Contains("optimize=false&command=delta-import") //for the first step
				&& Program.LogContent.Contains("command=full-import&verbose=true&commit=true&clean=false&optimize=false") //for the second step
				&& Program.LogContent.Contains("INFO Backup Solr response was: OK")
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}


		[TestMethod]
		public void BackupCore()
		{
			string args = string.Format("cmd=BackupCore serverUri={0} CoreName={1} Notify=All emailNotifyTo={2} emailNotifySmtpHost={3}",
				serverUri, coreName, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			//TODO: should go check that the newly backuped folder exists in the data directory for the specified core.

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains("INFO Backup Solr response was: OK")
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}

		[TestMethod]
		public void BackupCore_invalidCoreName()
		{
			string args = string.Format("cmd=BackupCore serverUri={0} CoreName=badCoreName Notify=All emailNotifyTo={1} emailNotifySmtpHost={2}",
				serverUri, emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt != 0, "Program response code was " + rslt.ToString() + ", expected a response greather than zero");
			Assert.IsTrue(
				Program.LogContent.Contains("ERROR Error while attempting to backup core: badCoreName")
				);
		}

		[TestMethod]
		public void CheckForEqualCores()
		{
			//NOTE: This test method does not use the constants... this executes against the Slave1 and Slave2 PRODUCTION instances!
			string args = string.Format("cmd=CheckForEqualCores CoreName=provs CoreName2=provs serverUri=http://localhost:8983 serverUri2=http://localhost:8983 Notify=All emailNotifyTo={2} emailNotifySmtpHost={3}",
				emailNotifyTo, emailNotifySmtpHost);

			int rslt = Program.Main(args.Split(" ".ToCharArray()));

			Assert.IsTrue(rslt == 0, "Program response code was " + rslt.ToString());
			Assert.IsTrue(
				Program.LogContent.Contains("INFO Cores provs and provs are equal")
				&& !Program.LogContent.Contains(" ERROR ")
				);
		}

		[TestMethod]
		public void GetHelp()
		{
			int rslt = Program.Main("".Split("".ToCharArray()));

			Assert.IsTrue(Program.LogContent.Contains("Solr Console Help system"));
		}

	}
}
