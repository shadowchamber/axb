using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace axb.Commands
{
    [Verb("fullbuild", HelpText = "Full Build")]
    public class FullBuildOptions : ICommandOptions
    {
        [Option('b', "branch", Required = true, HelpText = "branch")]
        public string Branch { get; set; }

        [Option('d', "blankdbname", Required = true, HelpText = "blank database name")]
        public string BlankDatabaseName { get; set; }

        [Option('w', "workdir", Required = true, HelpText = "working directory")]
        public string WorkingDirectory { get; set; }

        [Option('s', "workspace", Required = true, HelpText = "workspace name")]
        public string WorkspaceName { get; set; }



        [Option('c', "collectionurl", Required = false, HelpText = "collection url")]
        public string CollectionUrl { get; set; }

        [Option('r', "tfsroot", Required = false, HelpText = "tfs root")]
        public string TFSRoot { get; set; }

        [Option('l', "clientconfig", Required = true, HelpText = "client config", Default = "build_usp.axc")]
        public string ClientConfig { get; set; }

        [Option('g', "skipgetlatest", Required = false, HelpText = "skip get latest (when tfs gets it before)", Default = false)]
        public bool SkipGetLatest { get; set; }

        [Option('y', "deploydefname", Required = false, HelpText = "deploy build definition name")]
        public string DeployBuildDefinitionName { get; set; }

        [Option('n', "buildnumber", Required = true, HelpText = "build number")]
        public string BuildNumber { get; set; }

        [Option('p', "modelstorepath", Required = true, HelpText = "modelstore path", Default = "c:\\temp\\")]
        public string ModelstorePath { get; set; }

        [Option('m', "modelstorebackuppath", Required = false, HelpText = "modelstore backup path")]
        public string ModelstoreBackupPath { get; set; }

        [Option('h', "dbserver", Required = false, HelpText = "database server hostname")]
        public string DatabaseServer { get; set; }

        [Option('d', "dbname", Required = false, HelpText = "database name", Default = "AXB")]
        public string DatabaseName { get; set; }
    }
}
