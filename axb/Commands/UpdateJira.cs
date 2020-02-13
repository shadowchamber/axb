using Microsoft.Dynamics.AX.Framework.Management;
using Microsoft.Dynamics.AX.Framework.Management.Reports;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlassian;
using Atlassian.Jira;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using axb.Data;
using Castle.Core.Logging;

namespace axb.Commands
{
    public class UpdateJira
    {
        private ILogger logger = NullLogger.Instance;

        public ILogger Logger
        {
            get { return logger; }
            set { logger = value; }
        }

        public void log(string msg)
        {
            Logger.Info(msg);
        }

        public async Task<int> RunAsync(UpdateJiraOptions options)
        {
            using (IDbConnection db = new SqlConnection(String.Format("Data Source={0};Initial Catalog={1};Integrated Security=SSPI;", 
                                                                      options.DatabaseServer, 
                                                                      options.DatabaseName)))
            {
                var branch = db.Query<Branch>("select top 1 * from dbo.Branch where Name = @Name", new { Name = options.Branch }).SingleOrDefault();

                await Task.Run(() => this.updateTasks(branch.BuildStartChangeset, branch.BuildEndChangeset, options, branch));
            }

            return 0;
        }

        async Task UpdateTask(int taskNumber, UpdateJiraOptions options)
        {
            Jira jiraConn = Jira.CreateRestClient(options.JiraUrl, options.JiraUsername, options.JiraPassword);
            var issue = await jiraConn.Issues.GetIssueAsync(options.IssuePrefix + taskNumber);

            if (issue.Status == options.SourceStatus)
            {
                await issue.WorkflowTransitionAsync(options.DestinationStatus);

                log(string.Format("Task {0}: Status changed to {1}.", taskNumber, issue.Status));
            }
            else
            {
                log(string.Format("Task {0}: Status is {1}.", taskNumber, issue.Status));
            }

            var customField = issue.CustomFields[options.AssigneeFieldName];

            if (customField != null && customField.Values != null && customField.Values.Count() > 0)
            {
                issue.Assignee = customField.Values.First();
                await issue.SaveChangesAsync();

                log(string.Format("Task {0}: Assignee changed to {1}.", taskNumber, issue.Assignee));
            }
            else
            {
                log(string.Format("Task {0}: Assignee is {1}.", taskNumber, issue.Assignee));
            }
        }

        public void updateTasks(int fromChangeset, int toChangeset, UpdateJiraOptions options, Branch branch)
        {
            using (TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(new Uri(options.CollectionUrl)))
            {
                string tfsRoot = options.TfsWorkspacePath;
                var vsStore = tpc.GetService<VersionControlServer>();

                var changesets = vsStore.QueryHistory(branch.Path + "/" + options.ModelName,
                                                      VersionSpec.Latest,
                                                      0,
                                                      RecursionType.Full,
                                                      null,
                                                      VersionSpec.ParseSingleSpec(fromChangeset.ToString(), null),
                                                      VersionSpec.ParseSingleSpec(toChangeset.ToString(), null), 
                                                      Int32.MaxValue, 
                                                      true, 
                                                      false, 
                                                      true);

                foreach (Changeset changeset in changesets)
                {
                    if (   changeset.ChangesetId < fromChangeset 
                        || changeset.ChangesetId > toChangeset)
                    {
                        continue;
                    }

                    int n = 0;

                    string taskNumber = getTaskNumber(changeset.Comment);

                    if (int.TryParse(taskNumber, out n))
                    {
                        UpdateTask(int.Parse(taskNumber), options).GetAwaiter().GetResult();
                    }
                }
            }
        }
        public string getTaskNumber(string comment)
        {
            int first = comment.IndexOf(" ");

            if (first == -1)
            {
                return comment;
            }

            string task = comment.Substring(0, first).TrimStart('0');
            return task;
        }
    }
}
