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
    public class XPODeploy
    {
        Logger logger = LogManager.GetCurrentClassLogger();

        public void log(string msg)
        {
            logger.Info(msg);
        }

        string branch = "trunc";
        string workingDirectory = @"c:\tfs\";
        string modelName = "modelname";
        bool skipGetLatest = false;
        string collectionUrl = "http://hostname:8080/tfs/name";
        string tfsRoot = "$/name/";
        string workspaceName = "workspacename";
        string userid = "username";
        string description = "Build";

        ClientConfigManager clientConfigManager;
        ServerConfigManager serverConfigManager;
        AOSManager mgr;
        ClientManager client;
        ModelManager model;
        SQLManager sqlManager;
        LabelManager labelManager;

        string rootPath;
        string binPath;

        public async Task<int> RunAsync(XPODeployOptions options)
        {
            branch = options.Branch;
            workingDirectory = options.WorkingDirectory;

            await Task.Run(() => this.XpoDeploy());

            return 0;
        }

        void XpoDeploy()
        {
            // this.getDifferences();

            this.getLatest();
            this.loadConfig();

            this.importXpo(modelName, "usp");
            this.importLabels(modelName);

            this.generateCIL();
        }

        public void getLatest()
        {
            if (skipGetLatest)
            {
                log("get latest ignored");

                return;
            }

            if (!System.IO.Directory.Exists(workingDirectory + branch))
            {
                System.IO.Directory.CreateDirectory(workingDirectory + branch);
            }

            this.clearFolder(workingDirectory + branch);

            using (TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(new Uri(collectionUrl)))
            {
                var vsStore = tpc.GetService<VersionControlServer>();

                string workingFolder = workingDirectory + branch;

                Workspace wsp = vsStore.TryGetWorkspace(workingFolder);

                string tfsPath = tfsRoot + (branch == "trunc" || branch == "trunk" ? branch : "branches/" + branch);

                if (wsp == null)
                {
                    WorkingFolder wrkFolder = new WorkingFolder(tfsPath,
                        workingFolder, WorkingFolderType.Map, RecursionType.Full);

                    wsp = vsStore.CreateWorkspace(workspaceName + branch,
                        userid,
                        description,
                        new WorkingFolder[] { wrkFolder });
                }

                ItemSet items = vsStore.GetItems(workingFolder, VersionSpec.Latest, RecursionType.Full);

                foreach (Item item in items.Items)
                {
                    string relativePath = item.ServerItem.Replace(tfsPath, workingFolder);

                    if (item.ItemType == ItemType.Folder)
                    {
                        Directory.CreateDirectory(relativePath);
                    }
                    else
                    {
                        item.DownloadFile(relativePath);

                        log(relativePath);
                    }
                }
            }
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

        void importXpo(string _modelName, string _layer = "usr")
        {
            log(String.Format("======= processing model {0} =======", _modelName));

            client.AXConfigurationFile = rootPath + "config\\" + branch + "_" + _layer + ".axc";
            client.ModelManifest = rootPath + branch + "\\" + _modelName + "\\Model.xml";

            XPOCombiner combiner = new XPOCombiner();

            combiner.XPOFolder = rootPath + branch + "\\" + _modelName + "\\"; // client.ModelManifest;
            combiner.CombinedXPOFilename = binPath + _modelName + "_combined.xpo";
            combiner.SystemClassesXPOFilename = binPath + _modelName + "_combinedsystem.xpo";

            log("Combining xpo");

            combiner.Combine();

            log("Combined");

            Autorun autorun = new Autorun();

            log("Creating autorun XMLs");



            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Macros",
                true,
                binPath + _modelName + "_macros.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Data Dictionary\\Extended Data Types",
                true,
                binPath + _modelName + "_edt.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Data Dictionary\\Base Enums",
                true,
                binPath + _modelName + "_enums.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Data Dictionary\\Views",
                true,
                binPath + _modelName + "_views.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Data Dictionary\\Maps",
                true,
                binPath + _modelName + "_maps.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Data Dictionary\\Tables",
                true,
                binPath + _modelName + "_tables.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Data Dictionary\\Table Collections",
                true,
                binPath + _modelName + "_tablecollections.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Queries",
                true,
                binPath + _modelName + "_queries.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Classes",
                true,
                binPath + _modelName + "_classes.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Parts",
                true,
                binPath + _modelName + "_parts.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Resources",
                true,
                binPath + _modelName + "_resources.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Forms",
                true,
                binPath + _modelName + "_forms.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\SSRS Reports",
                true,
                binPath + _modelName + "_ssrsreports.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Web",
                true,
                binPath + _modelName + "_web.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Menu Items",
                true,
                binPath + _modelName + "_menuitems.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Menus",
                true,
                binPath + _modelName + "_menus.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Security",
                true,
                binPath + _modelName + "_security.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Jobs",
                true,
                binPath + _modelName + "_jobs.xml"
            );

            autorun.CreateXPOImportXML(
                rootPath + branch + "\\" + _modelName + "\\Projects",
                true,
                binPath + _modelName + "_projects.xml"
            );

            autorun.CreateXPOImportXML(binPath, true, binPath + _modelName + "_CombinedXPOImport.xml", false, "");
            autorun.CreateVSProjectImportXML(combiner.XPOFolder + "Visual Studio Projects\\", true, binPath + _modelName + "_VSProjectAutoRun.xml", false, "");
            autorun.CreateLabelFlushXML(combiner.XPOFolder + "label files\\", true, binPath + _modelName + "_LabelsFlush.xml", false, "");

            log("XMLs created");

            log("Setting no install mode");

            model.SetNoInstallMode();

            log("No install mode has been set");

            for (int i = 0; i < 2; i++)
            {
                this.autorunXML(binPath + _modelName + "_macros.xml", 5);
                this.autorunXML(binPath + _modelName + "_edt.xml", 5);
                this.autorunXML(binPath + _modelName + "_enums.xml", 5);

                this.autorunXML(binPath + _modelName + "_tables.xml", 60);



                this.autorunXML(binPath + _modelName + "_views.xml", 60);
                this.autorunXML(binPath + _modelName + "_maps.xml", 15);
                this.autorunXML(binPath + _modelName + "_tablecollections.xml", 15);
                this.autorunXML(binPath + _modelName + "_queries.xml", 15);
                this.autorunXML(binPath + _modelName + "_classes.xml", 20);
                this.autorunXML(binPath + _modelName + "_parts.xml", 15);
                this.autorunXML(binPath + _modelName + "_resources.xml", 15);
                this.autorunXML(binPath + _modelName + "_forms.xml", 15);
                this.autorunXML(binPath + _modelName + "_ssrsreports.xml", 15);
                this.autorunXML(binPath + _modelName + "_web.xml", 15);
                this.autorunXML(binPath + _modelName + "_menuitems.xml", 15);
                this.autorunXML(binPath + _modelName + "_menus.xml", 15);
                this.autorunXML(binPath + _modelName + "_jobs.xml", 15);
                this.autorunXML(binPath + _modelName + "_security.xml", 15);
                this.autorunXML(binPath + _modelName + "_projects.xml", 15);
                this.autorunXML(binPath + _modelName + "_VSProjectAutoRun.xml", 15);
            }

            model.ExportModel(combiner.XPOFolder + "Model.xml", binPath + _modelName + ".axmodel");

            log(String.Format("======= model {0} processed =======", _modelName));
        }

        void importLabels(string _modelName)
        {
            log("Importing labels");
            client.Command = ClientCommand.IMPORTLABELFOLDER;
            client.CommandArgument = rootPath + branch + "\\" + _modelName + "\\" + "label files";
            client.Execute();
            log("Imported");

            log("Flushing labels");
            client.Command = ClientCommand.AUTORUN;
            client.CommandArgument = binPath + _modelName + "_LabelsFlush.xml";
            client.Execute();
            log("Flushed");
        }

        void generateCIL()
        {
            log("Generating CIL");
            client.Command = ClientCommand.GENERATECIL;
            client.TimeOutMinutes = 120;
            client.Execute();
            log("Generated");
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

        int autorunXML(string _filename, ushort _timeOut = 0)
        {
            if (!File.Exists(_filename))
            {
                log("skipped - not exists");

                return 0;
            }

            log(String.Format("Importing {0}", _filename));

            client.Command = ClientCommand.AUTORUN;
            client.CommandArgument = _filename;
            client.TimeOutMinutes = _timeOut;

            int exitcode = client.Execute();

            log("Imported");

            return exitcode;
        }
    }
}
