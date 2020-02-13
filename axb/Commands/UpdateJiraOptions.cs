using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace axb.Commands
{
    [Verb("updatejira", HelpText = "Update JIRA tasks status")]
    public class UpdateJiraOptions : ICommandOptions
    {
        [Option('j', "jiraurl", Required = true, HelpText = "JIRA url", Default = "https://jd.eleks.com/")]
        public string JiraUrl { get; set; }

        [Option('b', "branch", Required = true, HelpText = "branch")]
        public string Branch { get; set; }

        [Option('p', "tfsworkspacepath", Required = false, HelpText = "tfs workspace path", Default = @"E:\tfs\diff\")]
        public string TfsWorkspacePath { get; set; }

        [Option('c', "collectionurl", Required = false, HelpText = "collection url", Default = "http://sr6-tfs-pl:8080/tfs/elicite")]
        public string CollectionUrl { get; set; }

        [Option('m', "modelname", Required = false, HelpText = "model name", Default = "/EliciteCustomizations")]
        public string ModelName { get; set; }

        [Option('i', "issueprefix", Required = false, HelpText = "issue prefix", Default = "APGR-")]
        public string IssuePrefix { get; set; }

        [Option('a', "assigneefield", Required = false, HelpText = "source assignee custom field name", Default = "BA Owner")]
        public string AssigneeFieldName { get; set; }

        [Option('u', "jirauser", Required = false, HelpText = "jira username", Default = "TFS.service")]
        public string JiraUsername { get; set; }

        [Option('w', "jirapass", Required = false, HelpText = "jira password", Default = "yFX7fD9wve")]

        public string JiraPassword { get; set; }

        [Option('s', "status", Required = false, HelpText = "destination status", Default = "Test Queue")]
        public string DestinationStatus { get; set; }

        [Option('f', "fromstatus", Required = false, HelpText = "source status", Default = "Code review")]
        public string SourceStatus { get; set; }

        [Option('h', "dbserver", Required = false, HelpText = "database server hostname", Default = "APRIL-SQL")]
        public string DatabaseServer { get; set; }

        [Option('d', "dbname", Required = false, HelpText = "database name", Default = "AXB")]
        public string DatabaseName { get; set; }
    }
}

