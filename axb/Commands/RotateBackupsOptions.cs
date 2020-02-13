using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace axb.Commands
{
    [Verb("rotatebackups", HelpText = "Rotate backups")]
    public class RotateBackupsOptions : ICommandOptions
    {
        [Option('p', "backuppath", Required = true, HelpText = "path to backups")]
        public string BackupPath { get; set; }
    }
}
