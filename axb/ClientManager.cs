using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.TeamFoundation.Build.Client;
using System.Xml;

namespace axb
{
    public enum ClientCommand
    {
        IMPORTXPO,
        IMPORTLABELFILE,
        IMPORTLABELFOLDER,
        SYNCHRONIZE,
        COMPILEXPP,
        GENERATECIL,
        RUNTESTPROJECT,
        XMLDOCUMENTATION,
        AUTORUN,
        STARTUPCMD
    }

    class ClientManager
    {
        public string AXClientBinPath { get; set; }
        public string AXServerBinPath { get; set; } // RRB
        public bool ContinueOnTimeout { get; set; } // RRB

        public string AXConfigurationFile { get; set; }

        public string ServerName { get; set; }
        public UInt16 PortNumber { get; set; }
        public string Layer { get; set; }
        public string LayerCode { get; set; }
        public string ModelManifest { get; set; }
        public string ModelName { get; set; }
        public string ModelPublisher { get; set; }

        public UInt16 TimeOutMinutes { get; set; }

        public ClientCommand Command { get; set; }
        public string CommandArgument { get; set; }

        public bool NoCompileOnImport = true;

        public int Execute()
        {
            string modelName = ModelName;
            string modelPublisher = ModelPublisher;
            string configFile = AXConfigurationFile;
            string serverName = ServerName;
            string portNumber = PortNumber.ToString();
            string layer = Layer;
            string layerCode = Layer;
            string commandArgument = CommandArgument;
            List<string> parameterList = new List<string>();

            int exitcode = 0;

            if (!String.IsNullOrEmpty(configFile))
            {
                parameterList.Add(String.Format("\"{0}\"", configFile));
            }
            else
            {
                // TODO: -AOS doesn't seem to be working!!
                if (!String.IsNullOrEmpty(serverName) && !String.IsNullOrEmpty(portNumber))
                    parameterList.Add(String.Format("-aos={0}@{1}", serverName, portNumber));
                else
                    Console.WriteLine("No configuration file or server/port specified.");

                if (!String.IsNullOrEmpty(layer))
                {
                    if (String.IsNullOrEmpty(layerCode) && layer.ToLower() != "usr" && layer.ToLower() != "usp")
                        throw new Exception("Missing layer code for layer different than USR/USP");

                    parameterList.Add(String.Format("-aol={0}", layer));
                    parameterList.Add(String.Format("-aolcode={0}", layerCode));
                }
            }

            if (!String.IsNullOrEmpty(ModelManifest))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(ModelManifest);

                modelName = doc.DocumentElement.SelectSingleNode("Name").InnerText;
                modelPublisher = doc.DocumentElement.SelectSingleNode("Publisher").InnerText;
                string modelLayer = doc.DocumentElement.SelectSingleNode("Layer").InnerText;

                if (!String.IsNullOrEmpty(layer) && modelLayer.ToLower() != layer.ToLower())
                {
                    throw new Exception(String.Format("Trying to import into model {0} in layer {1}, but configuration file connects to layer {2}", modelName, modelLayer.ToLower(), layer.ToLower()));
                }
                // TODO: check against configuration file??!
            }

            if (!String.IsNullOrEmpty(modelName))
            {
                if (String.IsNullOrEmpty(modelPublisher) || String.IsNullOrEmpty(modelPublisher.Trim()))
                {
                    parameterList.Add(String.Format("\"-Model={0}\"", modelName));
                }
                else
                {
                    parameterList.Add(String.Format("\"-Model=({0},{1})\"", modelName, modelPublisher));
                }
            }

            parameterList.Add("-MINIMIZE");

            //if(LazyClassLoading.Get(context) == true)
            parameterList.Add("-LAZYCLASSLOADING");

            //if(LazyClassLoading.Get(context) == true)
            parameterList.Add("-LAZYTABLELOADING");

            string language;
            switch (Command)
            {
                case ClientCommand.IMPORTXPO:
                    Console.WriteLine("Executing import of XPO file");
                    if (NoCompileOnImport)
                        parameterList.Add(String.Format("-NOCOMPILEONIMPORT"));
                    parameterList.Add(String.Format("\"-AOTIMPORTFILE={0}\"", commandArgument));
                   
                    exitcode = CallClient(parameterList);
                    break;
                case ClientCommand.IMPORTLABELFOLDER:

                    Console.WriteLine("Executing label folder import");

                    if (NoCompileOnImport)
                    parameterList.Add(String.Format("-NOCOMPILEONIMPORT"));

                    if (System.IO.Directory.Exists(commandArgument))
                    {
                        IEnumerable<string> files = System.IO.Directory.EnumerateFiles(commandArgument, String.Format("*.ald"), System.IO.SearchOption.AllDirectories);
                        foreach (string file in files)
                        {
                            language = file.Substring(file.LastIndexOf('\\') + 6, file.Length - 4 - file.LastIndexOf('\\') - 6);

                            if (!AxApplicationFiles.IsEmptyLabelFile(file))
                            {
                                Console.WriteLine(String.Format("Importing labels for language {0}", language), BuildMessageImportance.High);

                                List<string> labelParameters = new List<string>();
                                labelParameters.AddRange(parameterList);
                                labelParameters.Add(String.Format("\"-StartupCmd=aldimport_{0}\"", file));

                                exitcode = CallClient(labelParameters);
                            }
                            else
                            {
                                Console.WriteLine(String.Format("Skipping label language {0} because it is empty", language));
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Label files folder not found, skipping label import.");
                    }
                    break;
                case ClientCommand.IMPORTLABELFILE:
                    
                    language = commandArgument.Substring(commandArgument.LastIndexOf('\\') + 6, commandArgument.Length - 4 - commandArgument.LastIndexOf('\\') + 6);

                    if (AxApplicationFiles.IsEmptyLabelFile(commandArgument))
                        Console.WriteLine(String.Format("Skipping label file language {0} because it is empty", language));

                    Console.WriteLine(String.Format("Executing label file import for language {0}", language), BuildMessageImportance.High);
                    parameterList.Add(String.Format("\"-StartupCmd=aldimport_{0}\"", commandArgument));

                    if (NoCompileOnImport)
                    parameterList.Add(String.Format("-NOCOMPILEONIMPORT"));

                    exitcode = CallClient(parameterList);
                    break;
                case ClientCommand.SYNCHRONIZE:
                    Console.WriteLine("Executing data dictionary synchronize");
                    parameterList.Add("-StartupCmd=Synchronize");

                    exitcode = CallClient(parameterList);
                    break;
                case ClientCommand.COMPILEXPP:
                    if (!String.IsNullOrEmpty(commandArgument))
                    {
                        Console.WriteLine("Executing full X++ compile with cross-reference update");
                        parameterList.Add("-StartupCmd=CompileAll_+");
                    }
                    else
                    {
                        Console.WriteLine("Executing full X++ compile");
                        parameterList.Add("-StartupCmd=CompileAll");
                    }
                    #region RRB CU7 updates
                    //CallClient(parameterList, context);
                    CallAxBuild();
                    #endregion 
                    break;
                case ClientCommand.GENERATECIL:
                    Console.WriteLine("Executing CIL generation");
                    parameterList.Add("-StartupCmd=CompileIL");

                    exitcode = CallClient(parameterList);
                    break;
                case ClientCommand.RUNTESTPROJECT:
                    Console.WriteLine("Executing test project run");
                    if (String.IsNullOrEmpty(commandArgument))
                        throw new Exception("Expecting command argument containing the test project to run");
                    parameterList.Add(String.Format("-StartupCmd=RunTestProject_{0}", commandArgument));

                    exitcode = CallClient(parameterList);
                    break;
                case ClientCommand.XMLDOCUMENTATION:
                    Console.WriteLine("Executing XML documentation");
                    if (String.IsNullOrEmpty(commandArgument))
                        throw new Exception("Expecting command argument containing xml documentation filename");
                    parameterList.Add(String.Format("\"-StartupCmd=xmldocumentation_{0}\"", commandArgument));

                    exitcode = CallClient(parameterList);
                    break;
                case ClientCommand.AUTORUN:
                    Console.WriteLine(String.Format("Executing autorun script \"{0}\"", commandArgument));
                    parameterList.Add("-nocompileonimport");
                    parameterList.Add(String.Format("\"-StartupCmd=AutoRun_{0}\"", commandArgument));

                    exitcode = CallClient(parameterList);
                    break;
                case ClientCommand.STARTUPCMD:
                    Console.WriteLine(String.Format("Executing startup command \"{0}\"", commandArgument));
                    parameterList.Add(String.Format("\"-StartupCmd={0}\"", commandArgument));

                    exitcode = CallClient(parameterList);
                    break;
            }

            return exitcode;
        }

        #region RRB - Add functionality for CU7 AXBuild.exe
        private void CallAxBuild()
        {
            string parameterString; // 
            string commandArgument = CommandArgument;
            List<string> parameterList = new List<string>();
            ProcessStartInfo processStartInfo; // = new ProcessStartInfo(AXClientBinPath.Get(context) + @"\Ax32.exe", parameterString);

            Console.WriteLine(String.Format("commandArgument \"{0}\"", commandArgument));

            // The parameters are fairly fixed as of CU7.
            parameterList.Add("xppcompileall");

            if (!commandArgument.Contains("/s=") || commandArgument.Contains("/s=01"))
            {
                parameterList.Add("/s=01"); // Assume AOS #1
            }
            parameterList.Add(string.Format("/altbin=\"{0}\"", AXClientBinPath));
            if (!string.IsNullOrEmpty(commandArgument))
            {
                parameterList.Add(commandArgument); // Add any user-specified commands, probably number of workers.
            }

            parameterString = String.Join(" ", parameterList.ToArray());

            parameterString = commandArgument;

            processStartInfo = new ProcessStartInfo(AXServerBinPath + @"\AxBuild.exe", parameterString);
            processStartInfo.WindowStyle = ProcessWindowStyle.Normal; // ProcessWindowStyle.Minimized;
            processStartInfo.WorkingDirectory = AXServerBinPath;

            Console.WriteLine(String.Format("AxBuild: \"{0}\"", AXServerBinPath + @"\AxBuild.exe"));
            Console.WriteLine(String.Format("Executing AX Build with parameters \"{0}\"", parameterString));

            RunProcess(processStartInfo);
        }

        private int RunProcess(ProcessStartInfo processStartInfo)
        {
            Process process = Process.Start(processStartInfo);

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.ErrorDataReceived += build_ErrorDataReceived;
            process.OutputDataReceived += build_ErrorDataReceived; 

            if (!process.HasExited 
                && !process.WaitForExit((int)new TimeSpan(0, TimeOutMinutes, 0).TotalMilliseconds))   // Don't care about sub-millisecond precision here.
            {
                // Process is still running after the timeout has elapsed.
                try
                {
                    string text = String.Format("AX client execution of {0} timed out", Command);
                    Console.WriteLine(text);
                    process.Kill();
                }
                catch (InvalidOperationException)
                {
                    // Process does not exist or has already been killed.
                    // Don't throw the error just yet...
                    Console.WriteLine("Could not kill client process because it is not running");
                }
                catch (Exception _e)
                {
                    Console.WriteLine(_e.Message + _e.StackTrace);
                }
                
                if (!ContinueOnTimeout)
                {
                    throw new System.TimeoutException(String.Format("Operation did not complete in {0} minutes.", TimeOutMinutes));
                }
            }

            return process.ExitCode;
        }
        #endregion

        void build_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string strMessage = e.Data;

            if (!String.IsNullOrEmpty(strMessage))
            {
                Console.WriteLine(strMessage);
            }
        }

        private int CallClient(List<string> parameterList)
        {
            string parameterString = String.Join(" ", parameterList.ToArray());
            ProcessStartInfo processStartInfo = new ProcessStartInfo(AXClientBinPath + @"\Ax32.exe", parameterString);
            processStartInfo.WindowStyle = ProcessWindowStyle.Minimized;

            Console.WriteLine(String.Format("Executing AX client with parameters:\n{0} {1}", AXClientBinPath + @"\Ax32.exe", parameterString));

            int exitCode = RunProcess(processStartInfo);

            Console.WriteLine(String.Format("Exit code: {0}", exitCode));

            if (exitCode != 0)
            {
                Console.WriteLine("failed: retying");

                processStartInfo = new ProcessStartInfo(AXClientBinPath + @"\Ax32.exe", parameterString);
                processStartInfo.WindowStyle = ProcessWindowStyle.Minimized;

                Console.WriteLine(String.Format("Executing AX client with parameters \"{0}\"", parameterString));

                exitCode = RunProcess(processStartInfo);

                Console.WriteLine(String.Format("Exit code: {0}", exitCode));
            }

            return exitCode;
        }
    }
}

