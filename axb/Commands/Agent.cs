using Microsoft.Dynamics.AX.Framework.Management;
using Microsoft.Dynamics.AX.Framework.Management.Reports;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using Castle.Windsor;
using Castle.Core.Logging;

namespace axb.Commands
{
    public class Agent
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
        
        public async Task<int> RunAsync(AgentOptions options)
        {
            log("runnning");

            Service service = new Service();

            await Task.Run(() => service.MainThread());

            return 0;
        }
    }
}

