
********************************************************************************
Solr Console Help system
********************************************************************************
COMMANDS
********************************************************************************

HELP or ? => return help content.
--------------------------------------------------------------------------------
BackupCore => Issues command to Solr to create a backup of the specified core.
  The backup is created in the data directory of the specified core 
  in a folder named snapshot.[dateTimeStamp].  Checks to make sure the core 
  status is idle before issuing the backup command.

  Required Params: serverUri, CoreName
  Optional Params: Notify
--------------------------------------------------------------------------------
CheckForEqualCores => Checks the 'Index Version' and 'Document Count' for two
    cores and sends an 'ERROR' email alert if they are not equal (if the
	Notify parameter is true). The cores can be on different servers. This is 
	used to validate replicated Solr cores.

  Required Params: serverUri, CoreName, serverUri2, CoreName2
  Optional Params: Notify
--------------------------------------------------------------------------------
CommitCore => Issues Solr Commit command for specified core.
  
  Required Parameters => serverUri, CoreName
  Optional Parameteres => Notify
--------------------------------------------------------------------------------
CompareCoreStatus => Gets core status for two cores and shows status of each.

  Required Parameters => serverUri, LiveCore, StageCore
  Optional Parameteres => Notify
--------------------------------------------------------------------------------
CreateDeltaIndex => Creates a delta index on the specified core.  This does 
    not swap the cores on index completion. This should be run against the 
	live production core at a regular interval to keep it up to date.

  Required Parameters: serverUri, CoreName
  Optional Parameters: StatusDuration, Timeout, doBackup, doOptimize, Notify
--------------------------------------------------------------------------------
CreateFastDeltaIndex => Creates a delta index on the specified core.  This does 
    not swap the cores on index completion. This should be run against the 
	live production core at a regular interval to keep it up to date.
	Executes delta-import command with optimize set to false, waits for
	completion, then executes full-import command with optimize set to false
	and clean set to false (so the current core is not removed).

  Required Parameters: serverUri, CoreName
  Optional Parameters: StatusDuration, Timeout, doBackup, doOptimize, Notify
--------------------------------------------------------------------------------
CreateIndex => Creates a new index on the specified core. This does not swap 
    the cores upon index completion.

  Required Parameters: serverUri, CoreName
  Optional Parameters: StatusDuration, Timeout, doBackup, Notify
--------------------------------------------------------------------------------
CreateIndexAndSwap => Creates a new index on the specified core, swaps 
    LiveCore and StageCore, then increments the Index Version for StageCore.
	If doBackup is true, a backup is created for the LiveCore.

  Required Parameters: serverUri, LiveCore, StageCore
  Optional Parameters: StatusDuration, Timeout, doBackup, Notify
--------------------------------------------------------------------------------
GetCoreStatus => Shows status information for the specified core.

  Required Parameters: serverUri, CoreName
  Optional Parameteres => Notify
--------------------------------------------------------------------------------
GetIndexVersion => Shows the current index version for the specified core.

  Required Parameters: serverUri, CoreName
  Optional Parameteres => Notify
--------------------------------------------------------------------------------
IncrementCoreVersion => Forces the core Index Version to increment by
    inserting a bogus row, committing, deleting the bogus row, then
    committing again.

  Required Parameters: serverUri, CoreName
  Optional Parameteres => Notify
--------------------------------------------------------------------------------
OptimizeCore => Executes Solr command to optimize the specified core.

  Required Parameters: serverUri, CoreName
  Optional Parameteres => Notify
--------------------------------------------------------------------------------
SwapCores => Swaps LiveCore and StageCore.

  Required Parameters: serverUri, LiveCore, StageCore
  Optional Parameteres => Notify
--------------------------------------------------------------------------------
WatchIndexCreateProgress => Checks the status of the specified core and 
  continues to check as long as the status is busy or until the Timout passes.

  Required Parameters: serverUri, CoreName
  Optional Parameters: StatusDuration, Timeout, Notify

********************************************************************************
PARAMETER DEFINITIONS
********************************************************************************
--------------------------------------------------------------------------------
serverUri and serverUri2 => The Uri to the Solr Server.
--------------------------------------------------------------------------------
CoreName => The name of a Solr core.
--------------------------------------------------------------------------------
LiveCore => The name of the core serving data to applications.
--------------------------------------------------------------------------------
StageCore => The name of the core used to refresh the index.
--------------------------------------------------------------------------------
StatusDuration => An integer used to set the number of seconds between status 
    checks when creating a new Solr index.  The default is 30 seconds.
--------------------------------------------------------------------------------
Timeout => Used to abort processing of a new index after this specified amount 
    of time.  The default is 240 minutes (4 hours).
--------------------------------------------------------------------------------
doBackup => Set to 'true' to create a backup of the Solr core prior to 
    processing a command.  The default is 'false'.
--------------------------------------------------------------------------------
doOptimize => Set to 'true' to optimize after a command.  
    The default is 'false'.
--------------------------------------------------------------------------------
emailNotifyFrom => From address for email notifications.  Defaults to
    hg_job_notification@healthgrades.com.
--------------------------------------------------------------------------------
emailNotifyTo => To address for email notifications.  Defaults to
    hg_job_notification@healthgrades.com.  Can include multiple addresses
	using a comma to separate each address.  Do not use a space between
	the commas and email addresses.
--------------------------------------------------------------------------------
emailNotifySmtpHost => SMTP server name for email notifications.  Defaults to
    hgcorpmail2.healthgrades.com.
--------------------------------------------------------------------------------
emailNotifySmtpCredentials => SMTP credentials used when sending email 
    notifications.  Should be separated by commas in the following format:
	'userName,password,domain'.  If not provided, the SMTP Credentials are
	not explicitly set.
--------------------------------------------------------------------------------
Notify => Sets the severity level threshold to send email notifications. 
    Options include 'All', 'Warnings', 'Errors' and 'None'.  
	Default value is 'All'.