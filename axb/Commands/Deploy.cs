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
using System.Net.Sockets;
using System.Threading;

namespace axb.Commands
{
    public class Deploy 
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

            Console.WriteLine(msg);
        }

        string branch = "trunc";
        string workingDirectory = @"c:\tfs\";
        string modelName = "EliciteCustomizations";
        string workspaceName = "EliciteBuildWorkspace4";
        string modelstorePath = "c:\\temp\\";

        ClientConfigManager clientConfigManager;
        ServerConfigManager serverConfigManager;
        AOSManager mgr;
        ClientManager client;
        ModelManager model;
        SQLManager sqlManager;
        LabelManager labelManager;

        string rootPath;
        string binPath;

        public async Task<int> RunAsync(DeployOptions options)
        {
            log("runnning");

            branch = options.Branch;
            modelstorePath = options.ModelstorePath;
            workingDirectory = options.WorkingDirectory;
            workspaceName = options.WorkspaceName;

            log(String.Format("RemoteHost: '{0}'", options.RemoteHost));

            if (options.RemoteHost != null && options.RemoteHost.Trim() != String.Empty)
            {
                log("remote");
                await Task.Run(() => this.DoRemoteDeploy(options));
            }
            else
            {
                log("local");
                await Task.Run(() => this.DoDeploy());
            }

            return 0;
        }

        void DoRemoteDeploy(DeployOptions options)
        {
            TcpClient client = new TcpClient();

            log("connecting");

            client.Connect(options.RemoteHost, 35777);

            log("connected");

            NetworkStream stream = client.GetStream();

            string text;

            text = String.Format("deploy --branch {0} --workspace {1} --workdir {2} --modelstorepath {3}", 
                                 options.Branch,
                                 options.WorkspaceName,
                                 options.WorkingDirectory,
                                 options.ModelstorePath
                                 );

            int size = text.Length;
            byte[] intBuff = new byte[4];
            intBuff = BitConverter.GetBytes(size);
            stream.Write(intBuff, 0, intBuff.Length);

            byte[] dataSend = Encoding.UTF8.GetBytes(text);
            stream.Write(dataSend, 0, dataSend.Length);
                        
            while (client != null && client.Connected)
            {
                int read = 0;

                byte[] Buffer = new byte[4];

                read = stream.Read(Buffer, 0, Buffer.Length);

                if (read == 0)
                {
                    Thread.Sleep(1000);

                    continue;
                }

                int length = BitConverter.ToInt32(Buffer, 0);

                if (length == 0)
                {
                    log("close command received");

                    client.Close();

                    break;
                }

                byte[] buffer = new byte[1024];

                Stream Message = new MemoryStream();

                while (length > 0)
                {
                    read = stream.Read(buffer, 0, Math.Min(buffer.Length, length));
                    Message.Write(buffer, 0, read);
                    length -= read;
                }

                Message.Position = 0;
                StreamReader streamReader = new StreamReader(Message);
                string recvtext = streamReader.ReadToEnd();

                log("received:");
                log(recvtext);
            }

            log("finished");
        }

        void DoDeploy()
        {
            log("loading config");
            this.loadConfig(true);

            client.ModelManifest = modelstorePath + branch + "\\" + modelName + "\\Model.xml";

            ModelManager tempModel = new ModelManager();

            tempModel.SchemaName = "Temp";
            tempModel.AOSName = serverConfigManager.AOSName;

            log("initializing temp modelstore");
            tempModel.InitializeModelStore();
            
            log("importing temp modelstore");
            tempModel.ImportModelStore(modelstorePath + "latest_" + branch + ".axmodelstore");

            this.stopAOS();

            log("applying modelstore");
            tempModel.ApplyModelStore();

            log("dropping temp modelstore");
            tempModel.DropModelStore();

            log("cleaning XppIL");
            this.clearFolder(serverConfigManager.ServerBinPath + "\\XppIL");

            log("cleaning Assemblies");
            this.clearFolder(serverConfigManager.ServerBinPath + "\\VSAssemblies");

            this.startAOS();

            this.generateCIL();
            this.synchronizeDB();
            this.DeployReports();

            log("done");
        }

        void DeployReports()
        {
            String reportName = "*";
            String serverId = serverConfigManager.AOSName;

            PublishReportCommand cmd = new PublishReportCommand();

            cmd.ReportName = new string[] { reportName };
            cmd.Id = new string[] { serverId };

            XppLoggerAdapter logger = new XppLoggerAdapter();

            log("deploying reports");
            cmd.Process(logger);

            foreach (var logItem in logger.LogItems)
            {
                log(logItem.Message);
            }

            log("deploying finished");
        }

        void SynchronizeDB()
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

        void stopAOS()
        {
            mgr.TimeOutMinutes = 60;

            log("Stopping AOS");

            mgr.stop();

            log("AOS Stopped");
        }

        void startAOS()
        {
            mgr.TimeOutMinutes = 60;

            log("Starting AOS");

            mgr.start();

            log("AOS Started");
        }

        void clearFolder(string _path)
        {
            System.IO.DirectoryInfo directory = new System.IO.DirectoryInfo(_path);

            foreach (System.IO.FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }

            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories())
            {
                subDirectory.Delete(true);
            }
        }

        void generateCIL()
        {
            log("Generating CIL");
            client.Command = ClientCommand.GENERATECIL;
            client.TimeOutMinutes = 120;
            client.Execute();
            log("Generated");
        }

        void synchronizeDB()
        {
            log("Synchronizing DB");

            client.TimeOutMinutes = 120;
            client.Command = ClientCommand.SYNCHRONIZE;
            client.ContinueOnTimeout = true;
            int exitcode = client.Execute();

            log("Synchronized");
        }
    }
}
