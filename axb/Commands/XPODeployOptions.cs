using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace axb.Commands
{
    [Verb("xpodeploy", HelpText = "Xpo Deploy")]
    public class XPODeployOptions : ICommandOptions
    {
        [Option('b', "branch", Required = true, HelpText = "branch")]
        public string Branch { get; set; }

        [Option('w', "workdir", Required = true, HelpText = "working directory")]
        public string WorkingDirectory { get; set; }
    }
}
