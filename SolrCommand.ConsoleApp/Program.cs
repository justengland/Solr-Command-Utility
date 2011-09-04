using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Net.Mail;
using SolrCommand.Core;

namespace SolrCommand.ConsoleApp
{
	public class Program
	{
		private static string _logContent;

		public static string LogContent
		{
			get { return _logContent; }
		}

		public static int Main(string[] args)
		{
			try
			{
				_logContent = "";
				ProcessState.Response = new StringBuilder();
				ProcessState.HasError = false;
				ProcessState.LogInfo(string.Format("Solr Command Utility - Version {0}",
					System.Reflection.Assembly.GetExecutingAssembly().GetName().Version));

				//Make all arguments lower case to simplify argument handling
				for (int i = 0; i < args.Length; i++)
				{
					string val = args[i];
					if (val.Contains("="))
					{
						string[] param = val.Split("=".ToCharArray());
						if (param.Length == 2)
						{
							args[i] = param[0].ToLower() + "=" + param[1];
						}
					}
					else
					{
						args[i] = val.ToLower();
					}
				}

				////Show help content if appropriate
				//if (args.Length == 0 || args.Contains("?") || args.Contains("help"))
				//{
				//}

				//Configure Notification class used for email notification delivery
				Notification.From = getArgValue(args, "emailNotifyFrom", "someone@gmail.com");
				Notification.To = getArgValue(args, "emailNotifyTo", "someone@gmail.com");
				Notification.SmtpHost = getArgValue(args, "emailNotifySmtpHost", "smtp.gmail.com ");
				Notification.SmtpCredentialString = getArgValue(args, "emailNotifySmtpCredentials", "");

				try
				{
					Notification.Notify = (Notification.NotifyOptions)Enum.Parse(typeof(Notification.NotifyOptions), getArgValue(args, "Notify", "All"), true);
				}
				catch
				{
					ProcessState.LogWarn("Invalid value provided for Notify parameter.  Valid values include 'All', 'Warnings', 'Errors', or 'None'.  Setting value to default of 'All'.");
					Notification.Notify = Notification.NotifyOptions.All;
				}

				//Set parameter values from command line args
				string cmd = getArgValue(args, "cmd", "help");
				if (cmd == "?") { cmd = "help"; }

				string serverUri = getArgValue(args, "ServerUri", "");
				string serverUri2 = getArgValue(args, "ServerUri2", "");
				string coreName = getArgValue(args, "CoreName", "");
				string coreName2 = getArgValue(args, "CoreName2", "");
				string coreLive = getArgValue(args, "LiveCore", "");
				string coreStage = getArgValue(args, "StageCore", "");

				bool doBackup = false; //default
				bool.TryParse(getArgValue(args, "doBackup", "false"), out doBackup);

				bool doOptimize = false; //default
				bool.TryParse(getArgValue(args, "doOptimize", "false"), out doOptimize);


				int statusDuration = 30; //default in seconds
				string statusDurationString = getArgValue(args, "StatusDuration", "");
				if (!string.IsNullOrEmpty(statusDurationString)) { int.TryParse(statusDurationString, out statusDuration); }

				int timeout = 4 * 60;
				string timeoutString = getArgValue(args, "Timeout", "");
				if (!string.IsNullOrEmpty(timeoutString)) { int.TryParse(timeoutString, out timeout); }

				string additionalSubjectText = "";

				SolrCore.Commands command = (SolrCore.Commands)Enum.Parse(typeof(SolrCore.Commands), cmd, true);
				switch (command)
				{
					case SolrCore.Commands.BackupCore:
						additionalSubjectText = serverUri + " " + coreName;

						if (string.IsNullOrEmpty(serverUri) || string.IsNullOrEmpty(coreName))
						{
							ProcessState.LogError("Please set parameter values for serverUri, serverUri2, CoreName and CoreName2.");
						}
						else
						{
							SolrCore.BackupCore(serverUri, coreName);
						}

						break;
					case SolrCore.Commands.CheckForEqualCores:
						if (string.IsNullOrEmpty(serverUri) || string.IsNullOrEmpty(coreName)
							|| string.IsNullOrEmpty(serverUri2) || string.IsNullOrEmpty(coreName2))
						{
							ProcessState.LogError("Please set parameter values for serverUri, serverUri2, CoreName and CoreName2.");
						}
						else
						{

							CoreStatus coreStatus = SolrCore.GetSlaveCoreStatus(serverUri, coreName);
							CoreStatus core2Status = SolrCore.GetSlaveCoreStatus(serverUri2, coreName2);

							ProcessState.LogInfo(SolrCore.BuildCoreStatusSummaryString(coreStatus, core2Status));


							if (coreStatus.IndexVersion.Equals(core2Status.IndexVersion)
								&& coreStatus.DocumentCount.Equals(core2Status.DocumentCount))
							{
								additionalSubjectText = string.Format("{0} {1} and {2} {3} are equal.", serverUri, coreName, serverUri2, coreName2);
								ProcessState.LogInfo(additionalSubjectText);
							}
							else
							{
								additionalSubjectText = string.Format("{0} {1} and {2} {3} are NOT equal.", serverUri, coreName, serverUri2, coreName2);
								ProcessState.LogWarn(additionalSubjectText);
								ProcessState.HasError = true; //this will cause error email to be sent
							}
						}
						break;
					case SolrCore.Commands.CommitCore:
						additionalSubjectText = serverUri + " " + coreName;

						if (string.IsNullOrEmpty(serverUri) || string.IsNullOrEmpty(coreName))
						{
							ProcessState.LogError("Please set parameter values for serverUri and CoreName.");
						}
						else
						{
							SolrCore.CommitCore(serverUri, coreName);
							if (!ProcessState.HasError)
							{
								ProcessState.LogInfo("Successfully committed core " + coreName);
							}
						}
						break;
					case SolrCore.Commands.CreateDeltaIndex:
						additionalSubjectText = serverUri + " " + coreName;

						if (string.IsNullOrEmpty(serverUri) || string.IsNullOrEmpty(coreName))
						{
							ProcessState.LogError("Please set parameter values for serverUri and CoreName.");
						}
						else
						{
							ProcessState.LogInfo();
							ProcessState.LogInfo("Status Summary Before CreateDeltaIndex Command:");
							ProcessState.LogInfo("-------------------------------------------------");
							ProcessState.LogInfo(SolrCore.BuildCoreStatusSummaryString(serverUri, coreName));

							//create core backup if option is enabled
							if (doBackup)
							{
								bool backupOk = SolrCore.BackupCore(serverUri, coreName);
								if (!backupOk)
								{
									ProcessState.LogError("Aborting CreateDeltaIndex process because the backup command did not successfully complete.");
									break;
								}
							}

							SolrCore.CreateDeltaIndex(serverUri, coreName, timeout, statusDuration, false, doOptimize);

							ProcessState.LogInfo();
							ProcessState.LogInfo("Status Summary After CreateDeltaIndex Command:");
							ProcessState.LogInfo("------------------------------------------------");
							ProcessState.LogInfo(SolrCore.BuildCoreStatusSummaryString(serverUri, coreName));
						}
						break;
					case SolrCore.Commands.CreateFastDeltaIndex:
						additionalSubjectText = serverUri + " " + coreName;

						if (string.IsNullOrEmpty(serverUri) || string.IsNullOrEmpty(coreName))
						{
							ProcessState.LogError("Please set parameter values for serverUri and CoreName.");
						}
						else
						{
							ProcessState.LogInfo();
							ProcessState.LogInfo("Status Summary Before CreateFastDeltaIndex Command:");
							ProcessState.LogInfo("-------------------------------------------------");
							ProcessState.LogInfo(SolrCore.BuildCoreStatusSummaryString(serverUri, coreName));


							//create core backup if option is enabled
							if (doBackup)
							{
								bool backupOk = SolrCore.BackupCore(serverUri, coreName);
								if (!backupOk)
								{
									ProcessState.LogError("Aborting CreateDeltaIndex process because the backup command did not successfully complete.");
									break;
								}
							}

							SolrCore.CreateDeltaIndex(serverUri, coreName, timeout, statusDuration, true, doOptimize);

							ProcessState.LogInfo();
							ProcessState.LogInfo("Status Summary After CreateFastDeltaIndex Command:");
							ProcessState.LogInfo("------------------------------------------------");
							ProcessState.LogInfo(SolrCore.BuildCoreStatusSummaryString(serverUri, coreName));
						}
						break;
					case SolrCore.Commands.CreateIndex:
						additionalSubjectText = serverUri + " " + coreName;

						if (string.IsNullOrEmpty(serverUri) || string.IsNullOrEmpty(coreName))
						{
							ProcessState.LogError("Please set parameter values for serverUri and CoreName.");
						}
						else
						{

							ProcessState.LogInfo();
							ProcessState.LogInfo("Status Summary Before CreateIndex Command:");
							ProcessState.LogInfo("------------------------------------------");
							ProcessState.LogInfo(SolrCore.BuildCoreStatusSummaryString(serverUri, coreName));

							//create core backup if option is enabled
							if (doBackup)
							{
								bool backupOk = SolrCore.BackupCore(serverUri, coreName);
								if (!backupOk)
								{
									ProcessState.LogError("Aborting CreateDeltaIndex process because the backup command did not successfully complete.");
									break;
								}
							}

							SolrCore.CreateIndex(serverUri, coreName, timeout, statusDuration);

							ProcessState.LogInfo();
							ProcessState.LogInfo("Status Summary After CreateIndex Command:");
							ProcessState.LogInfo("-----------------------------------------");
							ProcessState.LogInfo(SolrCore.BuildCoreStatusSummaryString(serverUri, coreName));
						}

						break;
					case SolrCore.Commands.CreateIndexAndSwap:
						additionalSubjectText = serverUri + " " + coreStage + ", " + coreLive;

						if (string.IsNullOrEmpty(serverUri) || string.IsNullOrEmpty(coreStage) || string.IsNullOrEmpty(coreLive))
						{
							ProcessState.LogError("Please set parameter values for serverUri, StageCore and LiveCore.");
						}
						else
						{

							ProcessState.LogInfo();
							ProcessState.LogInfo("Status Summary Before CreateIndexAndSwap Command:");
							ProcessState.LogInfo("-------------------------------------------------");
							ProcessState.LogInfo(SolrCore.BuildCoreStatusSummaryString(serverUri, coreLive, coreStage));

							//create core backup if option is enabled
							if (doBackup)
							{
								bool backupOk = SolrCore.BackupCore(serverUri, coreLive);
								if (!backupOk)
								{
									ProcessState.LogError("Aborting CreateDeltaIndex process because the backup command did not successfully complete.");
									break;
								}
							}

							SolrCore.CreateIndexAndSwap(serverUri, coreStage, coreLive, timeout, statusDuration);

							ProcessState.LogInfo();
							ProcessState.LogInfo("Status Summary After CreateIndexAndSwap Command:");
							ProcessState.LogInfo("------------------------------------------------");
							ProcessState.LogInfo(SolrCore.BuildCoreStatusSummaryString(serverUri, coreLive, coreStage));
						}
						break;
					case SolrCore.Commands.GetCoreStatus:
						additionalSubjectText = serverUri + " " + coreName;

						if (string.IsNullOrEmpty(serverUri) || string.IsNullOrEmpty(coreName))
						{
							ProcessState.LogError("Please set parameter values for serverUri and CoreName.");
						}
						else
						{
							ProcessState.LogInfo(SolrCore.BuildCoreStatusSummaryString(serverUri, coreName));
						}

						break;
					case SolrCore.Commands.CompareCoreStatus:
						additionalSubjectText = serverUri + " " + coreLive + ", " + coreStage;

						if (string.IsNullOrEmpty(serverUri) || string.IsNullOrEmpty(coreLive) || string.IsNullOrEmpty(coreStage))
						{
							ProcessState.LogError("Please set parameter values for serverUri, LiveCore and StageCore.");
						}
						else
						{
							ProcessState.LogInfo(SolrCore.BuildCoreStatusSummaryString(serverUri, coreLive, coreStage));
						}

						break;
					case SolrCore.Commands.GetIndexVersion:
						additionalSubjectText = serverUri + " " + coreName;

						if (string.IsNullOrEmpty(serverUri) || string.IsNullOrEmpty(coreName))
						{
							ProcessState.LogError("Please set parameter values for serverUri and CoreName.");
						}
						else
						{

							ProcessState.LogInfo(string.Format("The current version for core {0} is {1}",
								coreName,
								SolrCore.GetCoreIndexVersion(serverUri, coreName)));

						}

						break;
					case SolrCore.Commands.IncrementCoreVersion:
						additionalSubjectText = serverUri + " " + coreName;

						if (string.IsNullOrEmpty(serverUri) || string.IsNullOrEmpty(coreName))
						{
							ProcessState.LogError("Please set parameter values for serverUri and CoreName.");
						}
						else
						{

							SolrCore.IncrementCoreIndexVersion(serverUri, coreName);
						}
						break;
					case SolrCore.Commands.OptimizeCore:
						additionalSubjectText = serverUri + " " + coreName;

						if (string.IsNullOrEmpty(serverUri) || string.IsNullOrEmpty(coreName))
						{
							ProcessState.LogError("Please set parameter values for serverUri and CoreName.");
						}
						else
						{
							//NOTE: We optimize two times here... the first one does the bulk of the work, but the second one performs additional cleanup of the data\index directory
							SolrCore.OptimizeCore(serverUri, coreName);
							SolrCore.OptimizeCore(serverUri, coreName);
						}

						break;
					case SolrCore.Commands.SwapCores:
						additionalSubjectText = serverUri + " " + coreStage + ", " + coreLive;

						if (string.IsNullOrEmpty(serverUri) || string.IsNullOrEmpty(coreLive) || string.IsNullOrEmpty(coreStage))
						{
							ProcessState.LogError("Please set parameter values for serverUri, StageCore and LiveCore.");
						}
						else
						{
							SolrCore.SwapCores(serverUri, coreLive, coreStage);
						}
						break;
					case SolrCore.Commands.WatchIndexCreateProgress:
						additionalSubjectText = serverUri + " " + coreName;

						if (string.IsNullOrEmpty(serverUri) || string.IsNullOrEmpty(coreName))
						{
							ProcessState.LogError("Please set parameter values for serverUri and CoreName.");
						}
						else
						{
							SolrCore.WatchIndexCreateProgress(serverUri, coreName, timeout, statusDuration);
						}

						break;
					default:
						GetHelp();
						Console.WriteLine(ProcessState.Response.ToString());
						return 0;
				}


				Console.WriteLine(ProcessState.Response.ToString());

				//Send email
				string paramSummary = SolrCore.BuildParamSummaryString(command.ToString(),
					serverUri, coreName, serverUri2, coreName2,
					coreLive, coreStage,
					statusDuration, timeout,
					doBackup, doOptimize);

				if (ProcessState.HasError)
				{
					try
					{
						string subject = string.Format("SCU ERROR: {0} {1}",
							command,
							additionalSubjectText);

						StringBuilder body = new StringBuilder();
						body.AppendLine(string.Format("Error processing request for command: {0}", command.ToString()));
						body.Append(paramSummary);
						body.AppendLine();
						body.AppendLine("--------------");
						body.AppendLine("Processing Log");
						body.AppendLine("--------------");
						body.AppendLine(ProcessState.Response.ToString());

						ProcessState.LogInfo(Notification.SendEmail(subject, body.ToString(), MailPriority.High));
					}
					catch (Exception ex)
					{
						ProcessState.LogError(string.Format("Error sending error email: {0}", ex.Message));
					}

					return 4;
				}
				else if (Notification.Notify == Notification.NotifyOptions.All ||
					(Notification.Notify == Notification.NotifyOptions.Warnings && ProcessState.HasError) ||
					(Notification.Notify == Notification.NotifyOptions.Errors && ProcessState.HasError))
				{
					//SendConfirmationEmail(command.ToString(), additionalSubjectText);
					try
					{
						string subject = string.Format("SCU OK: {0} {1}",
							command,
							additionalSubjectText);

						StringBuilder body = new StringBuilder();
						body.AppendLine(string.Format("Successfully completed processing request for command: {0}", command.ToString()));
						body.Append(paramSummary);
						body.AppendLine();
						body.AppendLine("--------------");
						body.AppendLine("Processing Log");
						body.AppendLine("--------------");
						body.AppendLine(ProcessState.Response.ToString());

						body.AppendLine(Notification.SendEmail(subject, body.ToString(), MailPriority.Normal));
					}
					catch (Exception ex)
					{
						ProcessState.LogError(string.Format("Error sending confirmation email: {0}", ex.Message));
					}

					return 0;
				}
				else
				{
					return 0;
				}
			}
			catch (Exception ex)
			{
				//Send Unhandled Error Email
				StringBuilder body = new StringBuilder();
				body.AppendLine(string.Format("An unhandled exception has occurred.  Error Message: {0}", ex.Message));
				body.AppendLine();
				body.AppendLine("Error Detail:");
				body.AppendLine(ex.ToString());

				Console.WriteLine(Notification.SendEmail("SCU ERROR!",
					body.ToString(),
					MailPriority.High));

				Console.WriteLine("Error processing request: " + ex.Message);
				return 4;
			}
			finally
			{
				_logContent = ProcessState.Response.ToString();
			}
		}

		/// <summary>
		/// Searches the arguments for a given argument name and returns the argument value, or the default value.
		/// </summary>
		/// <param name="args">String array of arguments.</param>
		/// <param name="argName">The argument name to find.</param>
		/// <param name="defaultValue">The default value if the argument name or value is not found.</param>
		/// <returns></returns>
		static string getArgValue(string[] args, string argName, string defaultValue)
		{
			if (args.Count(x => x.ToLower().StartsWith(argName.ToLower())) > 0)
			{
				return getArgValue(args.First(x => x.ToLower().StartsWith(argName.ToLower())), defaultValue);
			}
			else
			{
				return defaultValue;
			}
		}

		/// <summary>
		/// Returns the value for the provided argument, or the default value if the value is an empty string.
		/// </summary>
		/// <param name="arg">The argument value.  Can contain a name/value pair separated with an equal character to parse.</param>
		/// <param name="defaultValue">The default value to return if the argument does not contain a value.</param>
		/// <returns></returns>
		static string getArgValue(string arg, string defaultValue)
		{
			string rslt = getArgValue(arg);
			return rslt == "" ? defaultValue : rslt;
		}

		/// <summary>
		/// Returns the value of a name/value pair argument separated with an equal character.
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		static string getArgValue(string arg)
		{
			return arg.Contains("=") ? arg.Split("=".ToCharArray())[1] : "";
		}

		/// <summary>
		/// Returns string with help content.
		/// </summary>
		public static void GetHelp()
		{
			try
			{
				Assembly currentAssembly = Assembly.GetExecutingAssembly();
				//ProcessState.LogInfo("Solr Command Utility Version: " + currentAssembly.GetName().Version);

				using (Stream helpStream = currentAssembly.GetManifestResourceStream("SolrCommand.ConsoleApp.README.txt"))
				{
					StreamReader sr = new StreamReader(helpStream);
					ProcessState.LogInfo(sr.ReadToEnd());
				}
			}
			catch (Exception)
			{
				ProcessState.LogError("Could not get help.");
			}
		}
	}
}

