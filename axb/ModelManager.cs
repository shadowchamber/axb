using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.Dynamics.AX.Framework.Tools.ModelManagement;

namespace axb
{
    class ModelManager
    {
        public String AOSName { get; set; }
        public String SchemaName { get; set; }

        public void InstallModelStore(string modelFile)
        {
            string parameters = String.Format("importstore /noPrompt /idconflict:overwrite /config:{0} \"/file:{1}\"", AOSName, modelFile);

            parameters += SchemaName == "" ? "" : " /schemaname:" + SchemaName;

            RunAxUtil(parameters, "Error importing model store.");
        }

        public void InitializeModelStore()
        {
            string parameters = String.Format("schema /config:{0} /schemaname:{1}", AOSName, SchemaName);

            RunAxUtil(parameters, "Error initializing model store.");
        }

        public void DeleteLayer(string layer)
        {
            AxUtilContext utilContext = new AxUtilContext();
            AxUtilConfiguration config = new AxUtilConfiguration();
            config.AOSConfiguration = AOSName;

            AxUtil util = new AxUtil();

            config.Layer = layer;

            util.Delete(utilContext, config);

            if (utilContext.ExecutionStatus == ExecutionStatus.Error)
            {
                foreach (string error in utilContext.Errors)
                {
                    Console.WriteLine(error);
                }

                throw new Exception("Layer deletion failed.");
            }
        }

        public void CreateModel(string manifestFile, string version = "", string description = "")
        {
            AxUtilContext utilContext = new AxUtilContext();
            AxUtilConfiguration config = new AxUtilConfiguration();
            config.AOSConfiguration = AOSName;

            AxUtil util = new AxUtil();

            ModelManifest manifest = ModelManifest.Read(manifestFile);
            if (!String.IsNullOrEmpty(version))
            {
                manifest.Version = GetModelVersion(version, manifest.Version);
            }
            if (!String.IsNullOrEmpty(description))
            {
                manifest.Description = description;
            }

            config.ModelArgument = new ModelArgument(manifest.Name, manifest.Publisher);
            config.Layer = manifest.Layer.ToString();

            bool created = util.Create(utilContext, config, manifest);

            if (utilContext.ExecutionStatus == ExecutionStatus.Error)
            {
                foreach (string error in utilContext.Errors)
                {
                    Console.WriteLine(error);
                }
                throw new Exception("Model creation failed.");
            }

            if (!created)
            {
                throw new Exception("Model creation failed.");
            }
        }

        public string GetModelVersion(string buildVersion, string manifestVersion)
        {
            string[] buildVersionArray = buildVersion.Split('.');
            string[] manifestVersionArray = manifestVersion.Split('.');
            string[] modelVersion = { "1", "0", "0", "1" };
            string version;

            if (manifestVersionArray.Length >= 4)
            {
                modelVersion[0] = manifestVersionArray[0];
                modelVersion[1] = manifestVersionArray[1];
                modelVersion[2] = manifestVersionArray[2];
                modelVersion[3] = manifestVersionArray[3];
            }

            // If the build version has a build number (reset to 1 every day), use that.
            if (buildVersion.Contains("."))
            {
                modelVersion[3] = buildVersionArray[buildVersionArray.Length - 1];
            }
            modelVersion[1] = DateTime.Now.ToString("yyyy");
            modelVersion[2] = DateTime.Now.ToString("MMdd");

            version = string.Join(".", modelVersion);

            return version;
        }

        public void CreateModel(string modelName, string modelPublisher, string layer, string displayName, string description, string version)
        {
            AxUtilContext utilContext = new AxUtilContext();
            AxUtilConfiguration config = new AxUtilConfiguration();
            config.AOSConfiguration = AOSName;

            AxUtil util = new AxUtil();

            //config.ExportFile = modelFile;
            config.ModelArgument = new ModelArgument(modelName, modelPublisher);
            config.Layer = layer;

            ModelManifest manifest = new ModelManifest();
            manifest.Name = modelName;
            manifest.Publisher = modelPublisher;
            manifest.Version = version;
            manifest.DisplayName = displayName;
            manifest.Description = description;
            bool created = util.Create(utilContext, config, manifest);

            if (utilContext.ExecutionStatus == ExecutionStatus.Error)
            {
                foreach (string error in utilContext.Errors)
                {
                    Console.WriteLine(error);
                }
                throw new Exception("Model creation failed.");
            }

            if (!created)
            {
                throw new Exception("Model creation failed.");
            }
        }
        
        public void RunAxUtil(string parameters, string exceptionMessage)
        {
            RegistryKey AXInstall = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Dynamics\6.0\Setup");
            string path = AXInstall.GetValue("InstallDir") + @"\ManagementUtilities\";

            Console.WriteLine(path + "axutil.exe " + parameters);

            ProcessStartInfo processStartInfo = new ProcessStartInfo(path + "axutil.exe", parameters);
            processStartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            processStartInfo.WorkingDirectory = path;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.UseShellExecute = false;

            Process process = Process.Start(processStartInfo);

            process.OutputDataReceived += (
                object sender, System.Diagnostics.DataReceivedEventArgs e
            ) => Console.WriteLine("output>>" + e.Data);
            process.BeginOutputReadLine();

            process.ErrorDataReceived += (
                object sender, System.Diagnostics.DataReceivedEventArgs e
            ) => Console.WriteLine("error>>" + e.Data);
            process.BeginErrorReadLine();

            try
            {
                process.WaitForExit();
            }
            catch
            {
                throw new Exception(exceptionMessage);
            }

            if (process.ExitCode != 0)
            {
                throw new Exception(String.Format("Exit code - {0}", process.ExitCode));
            }
        }

        public void SetNoInstallMode()
        {
            AxUtilContext utilContext = new AxUtilContext();
            AxUtilConfiguration config = new AxUtilConfiguration();
            config.AOSConfiguration = AOSName;

            AxUtil util = new AxUtil();
            util.Config = config;
            util.Context = utilContext;
            util.ApplyInstallModeState(InstallModeState.NoInstallMode);
        }

        public void UninstallModel(string manifestFile)
        {
            ModelManifest manifest = ModelManifest.Read(manifestFile);

            UninstallModel(manifest.Name, manifest.Publisher);
        }

        public bool ModelExists(string modelName, string modelPublisher)
        {
            AxUtilContext utilContext = new AxUtilContext();
            AxUtilConfiguration config = new AxUtilConfiguration();
            config.AOSConfiguration = AOSName;
            bool modelFound = false;

            AxUtil util = new AxUtil();

            config.ModelArgument = new ModelArgument(modelName, modelPublisher);

            IList<ModelManifest> list = util.List(utilContext, config);
            foreach (ModelManifest manifest in list)
            {
                if (manifest.Name == modelName && manifest.Publisher == modelPublisher)
                    modelFound = true;
            }

            return modelFound;
        }

        public void UninstallModel(string modelName, string modelPublisher)
        {
            AxUtilContext utilContext = new AxUtilContext();
            AxUtilConfiguration config = new AxUtilConfiguration();
            config.AOSConfiguration = AOSName;

            AxUtil util = new AxUtil();

            config.ModelArgument = new ModelArgument(modelName, modelPublisher);

            if (ModelExists(modelName, modelPublisher))
            {
                util.Delete(utilContext, config);
            }

            if (utilContext.ExecutionStatus == ExecutionStatus.Error)
            {
                foreach (string error in utilContext.Errors)
                {
                    Console.WriteLine(error);
                }

                throw new Exception("Model uninstall failed.");
            }
        }

        public void ExportModel(string manifestFile, string modelFile)
        {
            ModelManifest manifest = ModelManifest.Read(manifestFile);

            ExportModel(manifest.Name, manifest.Publisher, modelFile);
        }

        public void ExportModel(string modelName, string modelPublisher, string modelFile)
        {
            ExportModelPS(modelName, modelPublisher, modelFile);

            /*
             * There is a reported bug where exported model is .NET 4.0 instead of 2.0
             *  which then results in a "invalid format" error when trying to import the model
             *  from the command line tools
             * 
             * Only current workaround is to use the command line tools.
             * 
             * Below code works, but model is .NET 4.0
             
            
            AxUtilContext utilContext = new AxUtilContext();
            AxUtilConfiguration config = new AxUtilConfiguration();
            config.AOSConfiguration = aosName;

            config.ExportFile = modelFile;
            config.ModelArgument = new ModelArgument(modelName, modelPublisher);

            if (!String.IsNullOrEmpty(key))
            {
                config.StrongNameKeyFile = key;
            }
            AxUtil util = new AxUtil();

            util.Export(utilContext, config);

            if (utilContext.ExecutionStatus == ExecutionStatus.Error)
            {
                foreach (string error in utilContext.Errors)
                {
                    context.TrackBuildError(error);
                }
                throw new Exception("Model export failed.");
            }

            if (!System.IO.File.Exists(modelFile))
            {
                throw new Exception("Model export failed.");
            }
            */
        }

        public void ExportModelPS(string modelName, string modelPublisher, string modelFile)
        {
            string parameters = String.Format("export /config:{0} \"/model:{1}\" \"/file:{3}\"", AOSName, modelName, modelPublisher, modelFile);

            RunAxUtil(parameters, "Error exporting model.");
        }

        public void ImportModelPS(string modelFile)
        {
            string parameters = String.Format("import /config:{0} \"/file:{1}\" /noprompt  /conflict:overwrite", AOSName, modelFile); // Can also use /conflict:push which creates a new model

            RunAxUtil(parameters, "Error imorting model.");
        }

        public void InstallModel(string modelFile, string modelManifest)
        {
            // Getting errors around TFSBuildServiceHost version, probably due to .Net version incompatibility of the AX Utils.
            ImportModelPS(modelFile);

            //AxUtilContext utilContext = new AxUtilContext();
            //AxUtilConfiguration config = new AxUtilConfiguration();
            //config.AOSConfiguration = aosName;

            //AxUtil util = new AxUtil();

            //config.ImportFiles.Add(modelFile);
            //util.Import(utilContext, config);
            //IList<ModelContents> importedModels = util.Import(utilContext, config);
            //if (utilContext.ExecutionStatus == ExecutionStatus.Error)
            //{
            //    foreach (string error in utilContext.Errors)
            //    {
            //        context.TrackBuildError(error);
            //    }
            //    throw new Exception("Model install failed.");
            //}

            //if (importedModels == null || importedModels.Count != 1)
            //{
            //    throw new Exception("Model install failed.");
            //}
        }

        public void ExportModelStore(string modelFile)
        {
            string parameters = String.Format("exportstore /config:{0} \"/file:{1}\"", AOSName, modelFile);

            RunAxUtil(parameters, "Error exporting model store.");
        }

        public void ImportModelStore(string modelFile)
        {
            string parameters = String.Format("importstore /verbose /noprompt /config:{0} \"/file:{1}\" /idconflict:overwrite", AOSName, modelFile);

            if (SchemaName != "")
            {
                parameters += String.Format(" /schemaname:{0}", SchemaName);
            }

            RunAxUtil(parameters, "Error importing model store.");
        }

        public void ApplyModelStore()
        {
            string parameters = String.Format("importstore /verbose /noprompt /config:{0}", AOSName);

            if (SchemaName != "")
            {
                parameters += String.Format(" /apply:{0}", SchemaName);
            }

            RunAxUtil(parameters, "Error importing model store.");
        }

        public void DropModelStore()
        {
            string parameters = String.Format("schema /noprompt /config:{0}", AOSName);

            if (SchemaName != "")
            {
                parameters += String.Format(" /drop:{0}", SchemaName);
            }

            RunAxUtil(parameters, "Error dropping model store.");
        }
    }
}
