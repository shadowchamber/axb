using Microsoft.Dynamics.AX.Framework.Management;
using Microsoft.Dynamics.AX.Framework.Management.Reports;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Common;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace axb.Commands
{
    public class RotateBackups
    {
        Logger logger = LogManager.GetCurrentClassLogger();

        public void log(string msg)
        {
            logger.Info(msg);
        }

        void moveToArchive(string _sourceFilename, string _archieveFilename)
        {
            string zPath = @"C:\Program Files\7-Zip\7z.exe";

            ProcessStartInfo pro = new ProcessStartInfo();
            pro.WindowStyle = ProcessWindowStyle.Normal;
            pro.FileName = zPath;
            pro.Arguments = "a -t7z \"" + _archieveFilename + "\" \"" + _sourceFilename + "\""; //  a -tgzip \"" + _archieveFilename + "\" \"" + _sourceFilename + "\" -mx=9";
            Process process = Process.Start(pro);

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.ErrorDataReceived += build_ErrorDataReceived;
            process.OutputDataReceived += build_ErrorDataReceived;

            process.WaitForExit();

            if (File.Exists(_archieveFilename))
            {
                log(string.Format("Archive file {0} successfully created.", _archieveFilename));
                File.Delete(_sourceFilename);
                log(string.Format("Source file {0} successfully deleted.", _sourceFilename));
            }
        }

        void build_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string strMessage = e.Data;

            if (!String.IsNullOrEmpty(strMessage))
            {
                Console.WriteLine(strMessage);
            }
        }

        public async Task<int> RunAsync(RotateBackupsOptions options)
        {
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(options.BackupPath);
            FileInfo[] files = dir.GetFiles().OrderByDescending(p => p.CreationTime).ToArray();

            foreach (FileInfo file in files)
            {
                if (file.Extension != ".7z")
                {
                    this.moveToArchive(Path.Combine(options.BackupPath, file.Name), Path.Combine(options.BackupPath, file.Name + ".7z"));
                }
            }

            files = dir.GetFiles().OrderByDescending(p => p.CreationTime).ToArray();

            if (DateTime.Today.DayOfWeek == DayOfWeek.Monday)//week
            {
                foreach (FileInfo file in files)
                {
                    if (   file.CreationTime < DateTime.Today.AddDays(-8) 
                        && file.CreationTime >= DateTime.Today.AddDays(-14)
                        && file.CreationTime != new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1))
                    {
                        await Task.Run(() => File.Delete(Path.Combine(options.BackupPath, file.Name)));
                        log(string.Format("Backup file {0} deleted.", file.Name));
                    }
                }
            }

            files = dir.GetFiles().OrderByDescending(p => p.CreationTime).ToArray();

            if (DateTime.Today == new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1))//month
            {
                foreach (FileInfo file in files)
                {
                    if (   file.CreationTime.Month == DateTime.Today.AddMonths(-2).Month 
                        && file.CreationTime != DateTime.Today.AddMonths(-1).AddDays(-1)
                        && file.CreationTime != new DateTime(file.CreationTime.Year, 12, 31))
                    {
                        File.Delete(Path.Combine(options.BackupPath, file.Name));
                        log(string.Format("Backup file {0} deleted.", file.Name));
                    }
                }
            }

            files = dir.GetFiles().OrderByDescending(p => p.CreationTime).ToArray();

            if (DateTime.Today == new DateTime(DateTime.Today.Year, 1, 1))//year
            {
                foreach (FileInfo file in files)
                {
                    if (   file.CreationTime.Year == DateTime.Today.AddYears(-2).Year
                        && file.CreationTime != DateTime.Today.AddYears(-1).AddDays(-1))
                    {
                        File.Delete(Path.Combine(options.BackupPath, file.Name));
                        log(string.Format("Backup file {0} deleted.", file.Name));
                    }
                }
            }

            return 0;
        }
    }
}
