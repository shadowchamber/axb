using axb.Data;
using Dapper;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace axb.Commands
{
    public class FullBuild
    {
        Logger logger = LogManager.GetCurrentClassLogger();

        public void log(string msg)
        {
            logger.Info(msg);
        }

        string branch = "trunc";
        string collectionUrl = "http://sr6-tfs-pl:8080/tfs/elicite";
        string tfsRoot = "$/Elicite/";
        string workingDirectory = @"c:\tfs\";
        string workspaceName = "EliciteBuildWorkspace4";
        string userid = "a.pylypenko";
        string description = "Build";
        string clientConfig = "build_usp.axc";
        string modelName = "EliciteCustomizations";
        string sqlBackupFolder = "C:\\SQLDatar\\Backup\\";
        string modelstorePath = "c:\\temp\\";
        string blankDatabaseName = "Blank";
        string deployBuildDefinitionName = "";
        string buildNumber = "";
        string modelstoreBackupPath = "";

        string rootPath;
        string binPath;
        bool skipGetLatest = false;

        ClientConfigManager clientConfigManager;
        ServerConfigManager serverConfigManager;
        AOSManager mgr;
        ClientManager client;
        ModelManager model;
        SQLManager sqlManager;
        LabelManager labelManager;

        int latestChangesetNumber;

        public async Task<int> RunAsync(FullBuildOptions options)
        {
            branch = options.Branch;

            latestChangesetNumber = this.getLatestVersionNumber();
            
            blankDatabaseName = options.BlankDatabaseName;
            workingDirectory = options.WorkingDirectory;
            workspaceName = options.WorkspaceName;

            collectionUrl = options.CollectionUrl;
            tfsRoot = options.TFSRoot;
            clientConfig = options.ClientConfig;
            skipGetLatest = options.SkipGetLatest;
            deployBuildDefinitionName = options.DeployBuildDefinitionName;
            buildNumber = options.BuildNumber;
            modelstorePath = options.ModelstorePath;
            modelstoreBackupPath = options.ModelstoreBackupPath;

            await Task.Run(() => this.DoBuild(options));

            return 0;
        }

        void DoBuild(FullBuildOptions options)
        {
            this.getLatest();
            this.loadConfig();

            this.stopAOS();

            this.restoreBlank();

            this.clearAllLabelFiles();

            this.startAOS();

            //    this.processModel(modelName2);
            //    this.processModel(modelName3);
            this.processModelCombined(modelName, "usp");

            this.stopAOS();

            this.restoreConfig();

            //     this.clearModels();

            //     this.installModel(modelName2);
            //     this.installModel(modelName3);
            this.installModel(modelName);

            this.startAOS();

            //    this.processModelCombined(modelName, "usp", true);

            this.compileAll();

            // this.synchronizeDB();

            this.importLabels(modelName);
            //    this.importLabels(modelName2);
            //    this.importLabels(modelName3);

            this.generateCIL();

            this.restartAOS();

            this.importLabels(modelName);
            //    this.importLabels(modelName2);
            //    this.importLabels(modelName3);

            this.restartAOS();

            this.exportModelstore();

            this.backupConfig();

            this.RotateModelstore();

            this.updateData(options);
        }

        void updateData(FullBuildOptions options)
        {
            using (IDbConnection db = new SqlConnection(String.Format("Data Source={0};Initial Catalog={1};Integrated Security=SSPI;",
                                                                      options.DatabaseServer,
                                                                      options.DatabaseName)))
            {
                Branch branch = db.Query<Branch>("select top 1 * from dbo.Branch where Name = @Name", new { Name = options.Branch }).SingleOrDefault();

                log(String.Format("RecId:               {0}", branch.RecId));
                log(String.Format("Name:                {0}", branch.Name));
                log(String.Format("Path:                {0}", branch.Path));
                log(String.Format("BuildStartChangeset: {0}", branch.BuildStartChangeset));
                log(String.Format("BuildEndChangeset:   {0}", branch.BuildEndChangeset));

                branch.BuildStartChangeset = branch.BuildEndChangeset;
                branch.BuildEndChangeset = latestChangesetNumber;

                db.Execute("update dbo.Branch set BuildStartChangeset = @BuildStartChangeset, BuildEndChangeset = @BuildEndChangeset where Name = @Name", branch);

                log("Changing");
                log(String.Format("BuildStartChangeset: {0}", branch.BuildStartChangeset));
                log(String.Format("BuildEndChangeset:   {0}", branch.BuildEndChangeset));
            }
        }

        public int getLatestVersionNumber()
        {
            using (TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(new Uri(collectionUrl)))
            {
                var vsStore = tpc.GetService<VersionControlServer>();

                string tfsPath = tfsRoot + (branch == "trunc" || branch == "trunk" ? branch : "branches/" + branch);

                log(String.Format("Getting history: {0}", tfsPath));

                var changesets = vsStore.QueryHistory(tfsPath,
                                                      VersionSpec.Latest,
                                                      0,
                                                      RecursionType.Full,
                                                      null,
                                                      null,
                                                      VersionSpec.Latest,
                                                      Int32.MaxValue,
                                                      true, 
                                                      true,
                                                      false,
                                                      false);

                foreach (Changeset changeset in changesets)
                {
                    log(String.Format("Got changeset number: {0}", changeset.ChangesetId));

                    return changeset.ChangesetId;
                }

                return 0;
            }
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

        public void restartAOS()
        {
            mgr.TimeOutMinutes = 60;

            log("Stopping AOS");

            mgr.stop();

            log("AOS Stopped");

            log("Starting AOS");

            mgr.start();

            log("AOS Started");
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

        void restoreBlank()
        {
            log("Restoring blank");

            sqlManager.Restore(sqlBackupFolder + blankDatabaseName + ".bak", sqlManager.DatabaseName);
            sqlManager.Restore(sqlBackupFolder + blankDatabaseName + "_model.bak", sqlManager.DatabaseName + "_model");

            log("Restored");
        }

        void clearAllLabelFiles()
        {
            log("Clearing label files");

            labelManager.Clear(serverConfigManager.ServerApplicationPath + "\\Appl\\Standard\\");

            log("Clearing done");
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

        void restoreConfig()
        {
            log(String.Format("Restoring config: {0}", sqlManager.DatabaseName));

            sqlManager.Restore(sqlBackupFolder + branch + ".bak", sqlManager.DatabaseName);
            sqlManager.Restore(sqlBackupFolder + branch + "_model.bak", sqlManager.DatabaseName + "_model");

            log("Restored");
        }

        void backupConfig()
        {
            log(String.Format("Storing config backup: {0}", sqlManager.DatabaseName));

            sqlManager.Backup(sqlBackupFolder + branch + ".bak", sqlManager.DatabaseName);
            sqlManager.Backup(sqlBackupFolder + branch + "_model.bak", sqlManager.DatabaseName + "_model");

            log("Backup stored");
        }

        void clearModels()
        {
            ModelManager usrmodel = new ModelManager()
            {
                AOSName = serverConfigManager.AOSName
            };

            log("Deleting USP Layer");

            usrmodel.DeleteLayer("USP");

            log("USP Layer deleted");

            /*   log("Deleting USR Layer");

               usrmodel.DeleteLayer("USR");

               log("USR Layer deleted");  */
        }

        void installModel(string _modelName)
        {
            log("Installing model");
            log(String.Format("{0} {1}", binPath + _modelName + ".axmodel", rootPath + branch + "\\" + _modelName + "\\Model.xml"));

            model.InstallModel(binPath + _modelName + ".axmodel", rootPath + branch + "\\" + _modelName + "\\Model.xml");

            log("Model installed");

            log("Setting no install mode");

            model.SetNoInstallMode();

            log("No install mode has been set");
        }

        void compileAll()
        {
            string servid = serverConfigManager.ServerServiceIdentifier; // "AOS60$01",

            servid = servid.Replace("AOS60$", "");

            client.Command = ClientCommand.COMPILEXPP;
            client.TimeOutMinutes = 180;
            client.CommandArgument = @"xppcompileall /s=" + servid + @" /layer=usp /altbin=""C:\Program Files (x86)\Microsoft Dynamics AX\60\Client\Bin"" /compiler=""C:\Program Files\Microsoft Dynamics AX\60\Server\EliciteBuild\Bin\Ax32Serv.exe""";
            client.Execute();

            XppCompileParser xppCompileParser = new XppCompileParser();

            xppCompileParser.LogPath = serverConfigManager.ServerLogPath;
            xppCompileParser.FailBuildOnError = true;
            xppCompileParser.ShowErrors = true;
            xppCompileParser.Execute();
        }

        void processModelCombined(string _modelName, string _layer = "usr", bool _dontDrop = false)
        {
            log(String.Format("======= processing model {0} =======", _modelName));

            string buildconf = "build";

            if (rootPath.Contains("buildagent2"))
            {
                buildconf = "build2";
            }

            client.AXConfigurationFile = rootPath + "config\\" + buildconf + "_" + _layer + ".axc";
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

            //    int tableblocks;

            //autorun.CreateXPOImportXML(binPath, true, binPath + _modelName + "_CombinedXPOImport.xml", false, "");
            autorun.CreateVSProjectImportXML(combiner.XPOFolder + "Visual Studio Projects\\", true, binPath + _modelName + "_VSProjectAutoRun.xml", false, "");
            autorun.CreateLabelFlushXML(combiner.XPOFolder + "label files\\", true, binPath + _modelName + "_LabelsFlush.xml", false, "");

            log("XMLs created");

            if (!_dontDrop)
            {
                log("Dropping model (to be sure)");

                model.UninstallModel(combiner.XPOFolder + "Model.xml");

                log("Dropped");

                if (_modelName != "USRModel")
                {
                    log("Creating empty model");

                    model.CreateModel(combiner.XPOFolder + "Model.xml");

                    log("Created");
                }

                log("Setting no install mode");

                model.SetNoInstallMode();

                log("No install mode has been set");
            }

            // this.restartAOS();

            log("Importing xpo");

            client.Command = ClientCommand.IMPORTXPO;
            client.NoCompileOnImport = true;
            client.CommandArgument = binPath + _modelName + "_combined.xpo";
            client.Execute();

            //   client.CommandArgument = binPath + _modelName + "_combinedsystem.xpo";
            //   client.Execute();

            log("Xpo imported");

            this.restartAOS();

            log("Importing xpo");

            client.Command = ClientCommand.IMPORTXPO;
            client.CommandArgument = binPath + _modelName + "_combined.xpo";
            client.Execute();

            //   client.CommandArgument = binPath + _modelName + "_combinedsystem.xpo";
            //   client.Execute();

            log("Xpo imported");

            this.restartAOS();

            //log("Importing xpo second time");

            //  client.Command = ClientCommand.IMPORTXPO;
            //client.NoCompileOnImport = false;
            //client.TimeOutMinutes = 180;
            //client.CommandArgument = binPath + _modelName + "_combined.xpo";
            //  client.Execute();

            ////  client.CommandArgument = binPath + _modelName + "_combinedsystem.xpo";
            ////  client.Execute();

            //log("Xpo imported");

            //  this.restartAOS();

            client.Command = ClientCommand.AUTORUN;
            client.CommandArgument = binPath + _modelName + "_VSProjectAutoRun.xml";
            client.Execute();

            //  this.restartAOS();

            log(String.Format("exporting model {0} {1}", combiner.XPOFolder + "Model.xml", binPath + _modelName + ".axmodel"));

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

        void exportModelstore()
        {
            log("Exporting modelstore");
            log(binPath + branch + ".axmodelstore");
            model.ExportModelStore(binPath + branch + ".axmodelstore");
            log("Exported");
        }

        void moveToArchive(string _sourceFilename, string _archieveFilename)
        {
            string zPath = @"C:\Program Files\7-Zip\7zG.exe";

            ProcessStartInfo pro = new ProcessStartInfo();
            pro.WindowStyle = ProcessWindowStyle.Hidden;
            pro.FileName = zPath;
            pro.Arguments = "a -tgzip \"" + _archieveFilename + "\" \"" + _sourceFilename + "\" -mx=9";
            Process x = Process.Start(pro);
            x.WaitForExit();

            if (File.Exists(_archieveFilename))
            {
                log(string.Format("Archive file {0} successfully created.", _archieveFilename));
                File.Delete(_sourceFilename);
                log(string.Format("Source file {0} successfully deleted.", _sourceFilename));
            }
        }

        void RotateModelstore()
        {
            try
            {
                string destinationModelstorePath = @"\\172.25.80.72\d$\deploy\latest_" + branch + ".axmodelstore";
                string previousModelstorePath = @"\\172.25.80.72\d$\deploy\prev_" + branch + ".axmodelstore";
                                                
                log(String.Format("deleting {0}", previousModelstorePath));
                System.IO.File.Delete(previousModelstorePath);

                log(String.Format("moving {0} {1}", destinationModelstorePath, previousModelstorePath));
                System.IO.File.Move(destinationModelstorePath, previousModelstorePath);

                log(String.Format("copying {0} {1}", binPath + branch + ".axmodelstore", destinationModelstorePath));
                System.IO.File.Copy(binPath + branch + ".axmodelstore", destinationModelstorePath);
            }
            catch (Exception _e)
            {
                log(_e.Message);
            }

            if (branch == "release")
            {
                try
                {
                    string destinationUatModelstorePath = @"\\192.168.200.102\c$\TEMP\latest_uat.axmodelstore";
                    string previousUatModelstorePath = @"\\192.168.200.102\c$\TEMP\prev_uat.axmodelstore";

                    log(String.Format("deleting {0}", previousUatModelstorePath));
                    System.IO.File.Delete(previousUatModelstorePath);

                    log(String.Format("moving {0} {1}", destinationUatModelstorePath, previousUatModelstorePath));
                    System.IO.File.Move(destinationUatModelstorePath, previousUatModelstorePath);

                    log(String.Format("copying {0} {1}", binPath + branch + ".axmodelstore", destinationUatModelstorePath));
                    System.IO.File.Copy(binPath + branch + ".axmodelstore", destinationUatModelstorePath);
                }
                catch (Exception _e)
                {
                    log(_e.Message);
                }
            }

            if (branch == "preprod")
            {
                try
                {
                    string destinationPreprodModelstorePath = @"\\192.168.200.102\c$\TEMP\latest_preprod.axmodelstore";
                    string previousPreprodModelstorePath = @"\\192.168.200.102\c$\TEMP\prev_preprod.axmodelstore";

                    log(String.Format("deleting {0}", previousPreprodModelstorePath));
                    System.IO.File.Delete(previousPreprodModelstorePath);

                    log(String.Format("moving {0} {1}", destinationPreprodModelstorePath, previousPreprodModelstorePath));
                    System.IO.File.Move(destinationPreprodModelstorePath, previousPreprodModelstorePath);

                    log(String.Format("copying {0} {1}", binPath + branch + ".axmodelstore", destinationPreprodModelstorePath));
                    System.IO.File.Copy(binPath + branch + ".axmodelstore", destinationPreprodModelstorePath);
                }
                catch (Exception _e)
                {
                    log(_e.Message);
                }
            }

            try
            {
                string backupFilename = modelstoreBackupPath + "\\" + buildNumber.Replace(".", "_") + ".axmodelstore";

                if (modelstoreBackupPath != "")
                {
                    log(String.Format("copying {0} {1}", binPath + branch + ".axmodelstore", backupFilename));
                    System.IO.File.Copy(binPath + branch + ".axmodelstore", backupFilename);

                    // this.moveToArchive(backupFilename, backupFilename + ".7z");
                }
            }
            catch (Exception _e)
            {
                log(_e.Message);
            }
        }

        public void getDifferences()
        {
            using (TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(new Uri(collectionUrl)))
            {
                var vsStore = tpc.GetService<VersionControlServer>();

                string tfsPath = tfsRoot + (branch == "trunc" || branch == "trunk" ? branch : "branches/" + branch);

                var histories = vsStore.QueryHistory(tfsPath + "/EliciteCustomizations", VersionSpec.Latest, 0, RecursionType.Full, null, null, null, Int32.MaxValue, true, false, true);

                List<String> files = new List<string>();

                foreach (Changeset changeset in histories)
                {
                    if (changeset.ChangesetId < 256 || changeset.ChangesetId > 298)
                    {
                        continue;
                    }

                    foreach (Change change in changeset.Changes)
                    {
                        var item = change.Item;

                        string path = item.ServerItem;

                        if (files.IndexOf(path) < 0)
                        {
                            files.Add(path);
                        }
                    }
                }

                string workingFolder = workingDirectory + branch;

                if (!System.IO.Directory.Exists(workingFolder))
                {
                    System.IO.Directory.CreateDirectory(workingFolder);
                }

                Workspace wsp = vsStore.TryGetWorkspace(workingFolder);

                if (wsp == null)
                {
                    WorkingFolder wrkFolder = new WorkingFolder(tfsPath,
                        workingFolder, WorkingFolderType.Map, RecursionType.Full);

                    wsp = vsStore.CreateWorkspace(workspaceName + branch,
                        userid,
                        description,
                        new WorkingFolder[] { wrkFolder });
                }

                foreach (string file in files)
                {
                    bool fileexists = vsStore.ServerItemExists(file, ItemType.File);

                    if (!fileexists)
                    {
                        Console.WriteLine(String.Format("Already deleted: {0}", file));

                        continue;
                    }

                    Item item = vsStore.GetItem(file);

                    string relativePath = item.ServerItem.Replace(tfsPath, workingFolder);

                    if (item.ItemType == ItemType.Folder)
                    {
                        Directory.CreateDirectory(relativePath);
                    }
                    else
                    {
                        item.DownloadFile(relativePath);

                        Console.WriteLine(relativePath);
                    }
                }
            }
        }

    }
}
