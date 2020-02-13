using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow.Activities;
using System.Xml;
using Microsoft.SqlServer.Management.Smo;
using System.IO;

namespace axb
{
    class SQLManager
    {
        Server smoServer;
        Database database;
        public string Server;
        public string DatabaseName;

        public void Backup(string backupFile, string DatabaseName)
        {
            Microsoft.SqlServer.Management.Common.ServerConnection connection;

            Console.WriteLine(string.Format("Connecting to SQL server {0}", Server));

            // Initialize our common objects
            connection = new Microsoft.SqlServer.Management.Common.ServerConnection(Server);
            smoServer = new Server(connection);
            database = smoServer.Databases[DatabaseName];
            if (false && database == null)
            {
                // Invalid database name.
                Console.WriteLine(string.Format("Invalid database '{1}' on server '{0}'.", Server, DatabaseName));
                foreach (Database db in smoServer.Databases)
                {
                    Console.WriteLine(string.Format("Available database: '{1}' on server '{0}'.", Server, db.Name));
                }
                throw new Exception(string.Format("Invalid database '{1}' on server '{0}'.", Server, DatabaseName));
            }


            Backup backup = new Backup();
            
            backup.Action = BackupActionType.Database;
            backup.Database = DatabaseName;
            backup.Devices.AddDevice(backupFile, DeviceType.File);
            backup.Incremental = false;
            backup.CompressionOption = BackupCompressionOptions.On;
            backup.NoRecovery = true;

          //  FileNameAndPathSaved.Set(context, backupFile);

            // Set status handlers.
            backup.PercentCompleteNotification = 10; // Send updates every 10%
            backup.PercentComplete += new PercentCompleteEventHandler(BackupProgressEventHandler);
            // Purge the file if it's already there.
            if (File.Exists(backupFile))
            {
                File.Delete(backupFile);
            }

            Console.WriteLine(string.Format("Backing up to file {0}", backupFile));
            backup.SqlBackup(smoServer);
        }

        public void BackupProgressEventHandler(object sender, PercentCompleteEventArgs e)
        {
            Console.WriteLine(string.Format("Backup percent complete: {0}%", e.Percent));
        }

        public void Restore(string backupFile, string DatabaseName)
        {
            Microsoft.SqlServer.Management.Common.ServerConnection connection;

            Console.WriteLine(string.Format("Connecting to SQL server {0}", Server));

            // Initialize our common objects
            connection = new Microsoft.SqlServer.Management.Common.ServerConnection(Server);
            smoServer = new Server(connection);
            database = smoServer.Databases[DatabaseName];
            if (false && database == null)
            {
                // Invalid database name.
                Console.WriteLine(string.Format("Invalid database '{1}' on server '{0}'.", Server, DatabaseName));
                foreach (Database db in smoServer.Databases)
                {
                    Console.WriteLine(string.Format("Available database: '{1}' on server '{0}'.", Server, db.Name));
                }
                throw new Exception(string.Format("Invalid database '{1}' on server '{0}'.", Server, DatabaseName));
            }


            Restore restore = new Restore();

            restore.Action = RestoreActionType.Database;
            restore.Database = DatabaseName;

            restore.Devices.AddDevice(backupFile, DeviceType.File);

            restore.ReplaceDatabase = true;

            RelocateFile DataFile = new RelocateFile();
            var fileList = restore.ReadFileList(smoServer);
            string MDF = fileList.Rows[0][1].ToString();
            DataFile.LogicalFileName = restore.ReadFileList(smoServer).Rows[0][0].ToString();
            DataFile.PhysicalFileName = smoServer.Databases[DatabaseName].FileGroups[0].Files[0].FileName;

            RelocateFile LogFile = new RelocateFile();
            string LDF = restore.ReadFileList(smoServer).Rows[1][1].ToString();
            LogFile.LogicalFileName = restore.ReadFileList(smoServer).Rows[1][0].ToString();
            LogFile.PhysicalFileName = smoServer.Databases[DatabaseName].LogFiles[0].FileName;

            restore.RelocateFiles.Add(DataFile);
            restore.RelocateFiles.Add(LogFile);

            // Kill all connections on the database.
            if (database != null)
            {
                Console.WriteLine(string.Format("Deleting database {0}.", DatabaseName));
                smoServer.KillDatabase(DatabaseName);
                //context.TrackBuildMessage("Killing all processes");
                //smoServer.KillAllProcesses(database.Name);
                //context.TrackBuildMessage("Setting single-user mode");
                //database.DatabaseOptions.UserAccess = DatabaseUserAccess.Single;
                //database.Alter(TerminationClause.RollbackTransactionsImmediately);

                //context.TrackBuildMessage("Detatching database");
                //smoServer.DetachDatabase(DatabaseName.Get(context), false);
            }

            // Set some status update event handlers.
            restore.PercentCompleteNotification = 10; // Send updates every 10%
            restore.PercentComplete += new PercentCompleteEventHandler(RestoreProgressEventHandler);

            Console.WriteLine("Restoring");
            restore.SqlRestore(smoServer);
        }

        //Log progress of the restore
        public void RestoreProgressEventHandler(object sender, PercentCompleteEventArgs e)
        {
            Console.WriteLine(string.Format("Restore percent complete: {0}%", e.Percent));
        }
    }
}



