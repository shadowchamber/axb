using Microsoft.Dynamics.AX.Framework.Management;
using Microsoft.Dynamics.AX.Framework.Management.Reports;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace axb.Commands
{
    public class SynchronizeDB
    {
        Logger logger = LogManager.GetCurrentClassLogger();

        public void log(string msg)
        {
            logger.Info(msg);
        }

        string branch = "trunc";
        string workingDirectory = @"c:\tfs\";
        string modelName = "EliciteCustomizations";

        ClientConfigManager clientConfigManager;
        ServerConfigManager serverConfigManager;
        AOSManager mgr;
        ClientManager client;
        ModelManager model;
        SQLManager sqlManager;
        LabelManager labelManager;

        string rootPath;
        string binPath;

        public async Task<int> RunAsync(SynchronizeDBOptions options)
        {
            branch = options.Branch;
            workingDirectory = options.WorkingDirectory;

            await Task.Run(() => this.DoSynchronizeDB());

            return 0;
        }

        void DoSynchronizeDB()
        {
            this.loadConfig();
            this.synchronizeDB();
        }

        void loadConfig(bool forDeploy = false)
        {
            log("Loading client configuration");

            clientConfigManager = new ClientConfigManager();

            rootPath = workingDirectory;
            binPath = rootPath + "bin\\" + branch + "\\";

            if (!System.IO.Directory.Exists(rootPath + "bin\\"))
            {
                System.IO.Directory.CreateDirectory(rootPath + "bin\\");
            }

            if (!System.IO.Directory.Exists(rootPath + "bin\\" + branch + "\\"))
            {
                System.IO.Directory.CreateDirectory(rootPath + "bin\\" + branch + "\\");
            }

            string buildconf = "build";

            if (rootPath.Contains("buildagent2"))
            {
                buildconf = "build2";
            }

            clientConfigManager.load(rootPath + "config\\" + (forDeploy ? branch : buildconf) + "_" + "usp" + ".axc"); //  rootPath + "config\\" + clientConfig);

            log("Client configuration loaded");

            log("Loading server configuration");

            serverConfigManager = new ServerConfigManager();

            serverConfigManager.load(clientConfigManager.ServerName, clientConfigManager.PortNumber);

            log("Server configuration loaded");

            mgr = new AOSManager()
            {
                ServerName = clientConfigManager.ServerName, // "april-ax-build",
                ServiceId = serverConfigManager.ServerServiceIdentifier, // "AOS60$01",
                TimeOutMinutes = 10
            };

            if (forDeploy)
            {
                mgr.TimeOutMinutes = 30;
            }

            client = new ClientManager();

            client.AXClientBinPath = clientConfigManager.ClientBinPath;
            client.AXServerBinPath = serverConfigManager.ServerBinPath;
            client.AXConfigurationFile = rootPath + "config\\" + (forDeploy ? branch : buildconf) + "_" + "usp" + ".axc"; // rootPath + "config\\" + clientConfig;
            client.ModelManifest = rootPath + branch + "\\" + modelName + "\\Model.xml";
            client.TimeOutMinutes = 60;

            model = new ModelManager()
            {
                AOSName = serverConfigManager.AOSName,
                SchemaName = "temp"
            };

            sqlManager = new SQLManager();

            sqlManager.DatabaseName = serverConfigManager.DatabaseName;
            sqlManager.Server = serverConfigManager.DatabaseServer;

            labelManager = new LabelManager();
        }

        void synchronizeDB()
        {
            log("Synchronizing DB");

            client.Command = ClientCommand.SYNCHRONIZE;
            client.ContinueOnTimeout = true;
            client.Execute();

            log("Synchronized");
        }
    }
}
