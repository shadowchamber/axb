using Microsoft.Dynamics.AX.Framework.Management;
using Microsoft.Dynamics.AX.Framework.Management.Reports;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Common;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace axb.Commands
{
    public class QueueBuild
    {
        Logger logger = LogManager.GetCurrentClassLogger();

        public void log(string msg)
        {
            logger.Info(msg);
        }
        
        public async Task<int> RunAsync(QueueBuildOptions options)
        {
            var build       = new BuildHttpClient(new Uri(options.CollectionUrl), new VssCredentials());
            var definitions = await build.GetDefinitionsAsync(project: options.ProjectName);
            var target      = definitions.First(d => d.Name == options.BuildDefinition);

            var res = await build.QueueBuildAsync(new Build
            {
                Definition = new DefinitionReference
                {
                    Id = target.Id
                },
                Project = target.Project
            });

            return 0;
        }
    }
}
