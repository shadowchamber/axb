using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace axb.Commands
{
    [Verb("deploy", HelpText = "Deploy")]
    public class DeployOptions : ICommandOptions
    {
        [Option('b', "branch", Required = true, HelpText = "branch")]
        public string Branch { get; set; }

        [Option('w', "workdir", Required = true, HelpText = "working directory")]
        public string WorkingDirectory { get; set; }

        [Option('s', "workspace", Required = true, HelpText = "workspace name")]
        public string WorkspaceName { get; set; }

        [Option('p', "modelstorepath", Required = true, HelpText = "modelstore path")]
        public string ModelstorePath { get; set; }


        [Option('r', "remotehost", Required = false, HelpText = "remote host")]
        public string RemoteHost { get; set; }
    }
}
