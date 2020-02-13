using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;

namespace axb
{
    public enum AxApplicationAction
    {
        CLEANLAYERS,
        CLEANLABELS,
        INSTALLLABELS,
        INSTALLLAYERS,
        EXPORTLAYERS
    }

    class AxApplicationFiles
    {
        public AxApplicationAction Action { get; set; }
        /// <summary>
        /// Name of the AOS server (machine)
        /// </summary>
        public string ServerName { get; set; }
        /// <summary>
        /// The AOS port number
        /// </summary>
        public UInt16 PortNumber { get; set; }
        /// <summary>
        /// Options for the specific actions:
        /// CLEANLABELS - comma-separated list of label file names to delete (eg SIK,SLS,BBB )
        /// CLEANLAYERS - comma-separated list of layer (files) to delete (eg BUS,BUP,VAR,VAP )
        /// INSTALLLABELS - comma-separated list of label file names to copy from source folder into application path
        /// INSTALLLAYERS - comma-separated list of layer files to copy from source folder into application path
        /// EXPORTLAYERS - comma-separated list of layer files to copy from application path to export folder
        /// </summary>
        public string ActionOptions { get; set; }
        /// <summary>
        /// Sources folder, this needs to include any sub-folders needed (eg /labels/ for label file installation)
        /// </summary>
        public string SourcesFolder { get; set; }
        /// <summary>
        /// Export folder for use with the EXPORTLAYERS action
        /// </summary>
        public string ExportFolder { get; set; }
        public void Execute(AxApplicationAction Action)
        {
            string options = ActionOptions;
            string applPath = ApplicationPath(ServerName, PortNumber.ToString());
            string sourcePath = SourcesFolder;

            string[] labels;
            string[] layers;
            switch (Action)
            {
                case AxApplicationAction.CLEANLABELS:
                    labels = options.Split(',');
                    foreach (string label in labels)
                    {
                        IEnumerable<string> files = System.IO.Directory.EnumerateFiles(applPath, String.Format("ax{0}*.al?", label));
                        foreach (string file in files)
                        {
                            System.IO.File.SetAttributes(file, FileAttributes.Normal);
                            System.IO.File.Delete(file);
                        }
                    }
                    break;
                case AxApplicationAction.CLEANLAYERS:
                    layers = options.Split(',');
                    System.IO.File.Delete(String.Format(@"{0}\axapd.aoi"));
                    foreach (string layer in layers)
                    {
                        IEnumerable<string> files = System.IO.Directory.EnumerateFiles(applPath, String.Format("ax{0}*.ald", layer));
                        foreach (string file in files)
                        {
                            System.IO.File.SetAttributes(file, FileAttributes.Normal);
                            System.IO.File.Delete(file);
                        }
                    }
                    break;
                case AxApplicationAction.INSTALLLABELS:
                    options = options.ToUpper();
                    labels = options.Split(',');
                    foreach (string label in labels)
                    {
                        IEnumerable<string> files = System.IO.Directory.EnumerateFiles(sourcePath, String.Format("ax{0}*.ald", label));
                        foreach (string file in files)
                        {
                            if (!IsEmptyLabelFile(file))
                            {
                                string language = file.Substring(file.LastIndexOf('\\') + 6, file.Length - 4 - file.LastIndexOf('\\') - 6);

                                Console.WriteLine(String.Format("Copying language {0} for label file {1}", language, label), BuildMessageImportance.High);

                                string sourceFile = String.Format(@"{0}\ax{1}{2}.ald", sourcePath, label, language);
                                string targetFile = String.Format(@"{0}\ax{1}{2}.ald", applPath, label, language);
                                System.IO.File.Copy(sourceFile, targetFile);
                                System.IO.File.SetAttributes(targetFile, FileAttributes.Normal);

                                sourceFile = String.Format(@"{0}\ax{1}{2}.alc", sourcePath, label, language);
                                targetFile = String.Format(@"{0}\ax{1}{2}.alc", applPath, label, language);
                                System.IO.File.Copy(sourceFile, targetFile);
                                System.IO.File.SetAttributes(targetFile, FileAttributes.Normal);
                            }
                        }
                    }
                    break;
                case AxApplicationAction.INSTALLLAYERS:
                    layers = options.Split(',');
                    System.IO.File.Delete(String.Format(@"{0}\axapd.aoi"));
                    foreach (string layer in layers)
                    {
                        IEnumerable<string> files = System.IO.Directory.EnumerateFiles(sourcePath, String.Format("ax{0}*.ald", layer));
                        foreach (string file in files)
                        {
                            string newLayerFile = String.Format(@"{0}\ax{1}.ald", applPath, layer.ToUpper());
                            System.IO.File.Copy(file, newLayerFile);
                            System.IO.File.SetAttributes(newLayerFile, FileAttributes.Normal);
                        }
                    }
                    break;
                case AxApplicationAction.EXPORTLAYERS:
                    layers = options.Split(',');
                    string targetFolder = ExportFolder;
                    foreach (string layer in layers)
                    {
                        IEnumerable<string> files = System.IO.Directory.EnumerateFiles(applPath, String.Format("ax{0}*.ald", layer));
                        foreach (string file in files)
                        {
                            string exportFile = String.Format(@"{0}\ax{1}.ald", targetFolder, layer.ToUpper());
                            System.IO.File.Copy(file, exportFile);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Check if a label file (ald) actually contains label texts
        /// </summary>
        /// <param name="labelFile">path and file name of the label file to check</param>
        /// <returns>True is the label file contains no text</returns>
        public static bool IsEmptyLabelFile(string labelFile)
        {
            bool isEmptyFile = true;

            if (File.Exists(labelFile))
            {
                using (StreamReader streamReader = new StreamReader(File.OpenRead(labelFile)))
                {
                    int lineCounter = 0;
                    while (!streamReader.EndOfStream && lineCounter < 50)
                    {
                        string line = streamReader.ReadLine().Trim();

                        Match match = Regex.Match(line, @"@.{3}\d+\s.+");
                        if (match.Success)
                        {
                            isEmptyFile = false;
                        }

                        lineCounter++;
                    }
                }
            }

            return isEmptyFile;
        }

        /// <summary>
        /// Searches registry path (for AX 2009) to find application files path
        /// given a server name and AOS port number
        /// </summary>
        /// <param name="serverName">Name of the AOS server machine</param>
        /// <param name="portNumber">AOS port number</param>
        /// <returns>string containing application path</returns>
        private static string ApplicationPath(string serverName, string portNumber)
        {
            string aosRegistryPath = @"SYSTEM\CurrentControlSet\services\Dynamics Server\5.0";
            string applPath = "";
            RegistryKey aosEntries = null;

            if (serverName != System.Environment.MachineName)
            {
                // Open the registry on the remote machine
                aosEntries = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, serverName);
                // Get the list of servers running on the remote machine.
                aosEntries = aosEntries.OpenSubKey(aosRegistryPath);
            }
            else
            {
                // Get the list of servers running on this machine.
                aosEntries = Registry.LocalMachine.OpenSubKey(aosRegistryPath);
            }

            string[] aosRegistryEntries = aosEntries.GetSubKeyNames();
            foreach (string aosRegistryEntry in aosRegistryEntries)
            {
                RegistryKey aosRootKey = Registry.LocalMachine.OpenSubKey(aosRegistryPath + @"\" + aosRegistryEntry);
                RegistryKey aosInstanceKey = Registry.LocalMachine.OpenSubKey(aosRegistryPath + @"\" + aosRegistryEntry + @"\" + aosRootKey.GetValue("Current"));
                if (aosInstanceKey.GetValue("Port").Equals(portNumber))
                {
                    applPath = String.Format(@"{0}\Appl\{1}", aosRootKey.GetValue("directory").ToString(), aosRootKey.GetValue("application"));
                    break;
                }
            }

            if (String.IsNullOrEmpty(applPath))
            {
                throw new Exception(String.Format("Could not find configuration for server running on {0}:{1}", serverName, portNumber));
            }

            return applPath;
        }
    }


}

