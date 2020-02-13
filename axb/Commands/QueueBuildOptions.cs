using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace axb.Commands
{
    [Verb("queuebuild", HelpText = "Queue tfs build")]
    public class QueueBuildOptions : ICommandOptions
    {
        [Option('f', "deploybuilddefinition", Required = true, HelpText = "deploy build definition")]
        public string BuildDefinition { get; set; }

        [Option('c', "collectionurl", Required = true, HelpText = "collection url", Default = "http://sr6-tfs-pl:8080/tfs/elicite")]
        public string CollectionUrl { get; set; }

        [Option('p', "projectname", Required = true, HelpText = "tfs project name")]
        public string ProjectName { get; set; }
    }
}
