using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace axb
{
    class Autorun
    {
        public int CreateXPOImportXMLBlocks(string sourcesFolder, bool recursive, string xmlFilenameBase, int blocksize = 10, bool append = false, string logFile = "")
        {
            int blockNumber = 1;

            if (System.IO.Directory.Exists(sourcesFolder))
            {
                AutoRun.AxaptaAutoRun autoRun = null;

                string xmlFilename = Path.GetDirectoryName(xmlFilenameBase) + "\\" +  Path.GetFileNameWithoutExtension(xmlFilenameBase) + blockNumber.ToString() + Path.GetExtension(xmlFilenameBase);


                if (append)
                {
                    autoRun = AutoRun.AxaptaAutoRun.FindOrCreate(xmlFilename);
                }
                else
                {
                    autoRun = new AutoRun.AxaptaAutoRun();
                }

                autoRun.ExitWhenDone = true;
                if (logFile != "")
                {
                    autoRun.LogFile = logFile;
                }
                autoRun.Version = "4.0";
                autoRun.Steps = new List<AutoRun.AutorunElement>();

                List<string> files = new List<string>();

                GetProjectFiles(sourcesFolder, recursive, files, "*.xpo");

                if (files.Count != 0)
                {
                    int i = 0;

                    List<string>.Enumerator fileEnumerator = files.GetEnumerator();
                    while (fileEnumerator.MoveNext())
                    {
                        i++;

                        autoRun.Steps.Add(new AutoRun.XpoImport() { File = fileEnumerator.Current });

                        if (i % blocksize == 0)
                        {
                            AutoRun.AxaptaAutoRun.SerializeAutoRun(autoRun, xmlFilename);

                            autoRun.Steps = new List<AutoRun.AutorunElement>();

                            blockNumber++;
                            xmlFilename = Path.GetDirectoryName(xmlFilenameBase) + "\\" + Path.GetFileNameWithoutExtension(xmlFilenameBase) + blockNumber.ToString() + Path.GetExtension(xmlFilenameBase);
                        }
                    }

                    AutoRun.AxaptaAutoRun.SerializeAutoRun(autoRun, xmlFilename);

                    return blockNumber;
                }
                else
                {
                    Console.WriteLine("No Visual Studio Projects found.");
                    // do not create autorun.xml file if there's nothing in it

                    return 0;
                }
            }
            else
            {
                Console.WriteLine("No Visual Studio Projects found.");

                return 0;
            }
        }

        public void CreateXPOImportXML(string sourcesFolder, bool recursive, string xmlFilename, bool append = false, string logFile = "")
        {
            if (System.IO.Directory.Exists(sourcesFolder))
            {
                AutoRun.AxaptaAutoRun autoRun = null;

                if (append)
                {
                    autoRun = AutoRun.AxaptaAutoRun.FindOrCreate(xmlFilename);
                }
                else
                {
                    autoRun = new AutoRun.AxaptaAutoRun();
                }

                autoRun.ExitWhenDone = true;
                if (logFile != "")
                {
                    autoRun.LogFile = logFile;
                }
                autoRun.Version = "4.0";
                autoRun.Steps = new List<AutoRun.AutorunElement>();

                List<string> files = new List<string>();

                GetProjectFiles(sourcesFolder, recursive, files, "*.xpo");

                if (files.Count != 0)
                {
                    List<string>.Enumerator fileEnumerator = files.GetEnumerator();
                    while (fileEnumerator.MoveNext())
                    {
                        autoRun.Steps.Add(new AutoRun.XpoImport() { File = fileEnumerator.Current });
                    }

                    AutoRun.AxaptaAutoRun.SerializeAutoRun(autoRun, xmlFilename);
                }
                else
                {
                    Console.WriteLine("No Visual Studio Projects found.");
                    // do not create autorun.xml file if there's nothing in it
                }
            }
            else
            {
                Console.WriteLine("No Visual Studio Projects found.");
            }
        }

        public void CreateVSProjectImportXML(string sourcesFolder, bool recursive, string xmlFilename, bool append, string logFile)
        {
            if (System.IO.Directory.Exists(sourcesFolder))
            {
                AutoRun.AxaptaAutoRun autoRun = null;
                if (append)
                {
                    autoRun = AutoRun.AxaptaAutoRun.FindOrCreate(xmlFilename);
                }
                else
                {
                    autoRun = new AutoRun.AxaptaAutoRun();
                }

                autoRun.ExitWhenDone = true;
                if (logFile != "")
                {
                    autoRun.LogFile = logFile;
                }
                autoRun.Version = "4.0";
                autoRun.Steps = new List<AutoRun.AutorunElement>();

                List<string> files = new List<string>();

                GetProjectFiles(sourcesFolder, recursive, files, "*.csproj");

                if (files.Count != 0)
                {
                    List<string>.Enumerator fileEnumerator = files.GetEnumerator();
                    // Add import steps
                    while (fileEnumerator.MoveNext())
                    {
                        autoRun.Steps.Add(new AutoRun.Run() { type = AutoRun.RunType.@class, name = "SysTreeNodeVSProject", method = "importProject", parameters = string.Format("@'{0}'", fileEnumerator.Current) });
                    }
                    fileEnumerator = files.GetEnumerator();
                    // Add compile steps
                    while (fileEnumerator.MoveNext())
                    {
                        autoRun.Steps.Add(new AutoRun.CompileApplication() { node = string.Format(@"\Visual Studio Projects\C Sharp Projects\{0}", System.IO.Path.GetFileNameWithoutExtension(fileEnumerator.Current)), crossReference = false });
                    }

                    AutoRun.AxaptaAutoRun.SerializeAutoRun(autoRun, xmlFilename);
                }
                else
                {
                    Console.WriteLine("No Visual Studio Projects found.");
                    // do not create autorun.xml file if there's nothing in it
                }
            }
            else
            {
                Console.WriteLine("No Visual Studio Projects found.");
            }
        }

        public void CreateRetailDeployXML(string sourcesFolder, bool recursive, string xmlFilename, bool append, string logFile)
        {
            AutoRun.AxaptaAutoRun autoRun = null;
            if (append)
            {
                autoRun = AutoRun.AxaptaAutoRun.FindOrCreate(xmlFilename);
            }
            else
            {
                autoRun = new AutoRun.AxaptaAutoRun();
            }

            autoRun.ExitWhenDone = true;
            if (logFile != "")
            {
                autoRun.LogFile = logFile;
            }
            autoRun.Version = "4.0";
            autoRun.Steps = new List<AutoRun.AutorunElement>();

            autoRun.Steps.Add(new AutoRun.Run() { type = AutoRun.RunType.@class, name = "PJC_DeployRetail", method = "main" });

            AutoRun.AxaptaAutoRun.SerializeAutoRun(autoRun, xmlFilename);
        }

        #region Label::flush()
        public void CreateLabelFlushXML(string sourcesFolder, bool recursive, string xmlFilename, bool append, string logFile)
        {
            string file;
            string label;
            string language;

            if (System.IO.Directory.Exists(sourcesFolder))
            {
                AutoRun.AxaptaAutoRun autoRun = null;
                if (append)
                {
                    autoRun = AutoRun.AxaptaAutoRun.FindOrCreate(xmlFilename);
                }
                else
                {
                    autoRun = new AutoRun.AxaptaAutoRun();
                }

                autoRun.ExitWhenDone = true;
                if (logFile != "")
                {
                    autoRun.LogFile = logFile;
                }
                autoRun.Version = "4.0";
                autoRun.Steps = new List<AutoRun.AutorunElement>();

                List<string> files = new List<string>();

                GetProjectFiles(sourcesFolder, recursive, files, "*.ald");

                if (files.Count != 0)
                {
                    List<string>.Enumerator fileEnumerator = files.GetEnumerator();

                    while (fileEnumerator.MoveNext())
                    {
                        file = fileEnumerator.Current;
                        if (!AxApplicationFiles.IsEmptyLabelFile(file))
                        {
                            // We only care to flush languages that have entries.
                            language = file.Substring(file.LastIndexOf('\\') + 6, file.Length - 4 - file.LastIndexOf('\\') - 6);
                            label = file.Substring(file.LastIndexOf('\\') + 3, 3);
                            autoRun.Steps.Add(new AutoRun.Run() { type = AutoRun.RunType.@class, name = "Label", method = "flush", parameters = string.Format("@'{0}', @'{1}'", label, language) });
                        }
                    }

                    AutoRun.AxaptaAutoRun.SerializeAutoRun(autoRun, xmlFilename);
                }
                else
                {
                    Console.WriteLine("No label files found.");
                    // do not create autorun.xml file if there's nothing in it
                }
            }
            else
            {
                Console.WriteLine("No label files found.");
            }
        }


        #endregion // Label::flush()
        private void GetProjectFiles(string directory, bool recursive, List<string> files, string filter)
        {
            foreach (string filename in Directory.GetFiles(directory, filter))
            {
                files.Add(filename);
            }

            if (recursive)
            {
                foreach (string subdirectory in Directory.GetDirectories(directory))
                {
                    GetProjectFiles(subdirectory, recursive, files, filter);
                }
            }
        }
    }

    namespace AutoRun
    {
        public enum RunType
        {
            @class
        }

        [Serializable]
        public class AxaptaAutoRun
        {
            [XmlAttribute("exitWhenDone")]
            public bool ExitWhenDone { get; set; }

            [XmlAttribute("version")]
            public string Version { get; set; }

            [XmlAttribute("logFile")]
            public String LogFile { get; set; }

            [XmlElement("Run", typeof(Run)),
            XmlElement("CompileApplication", typeof(CompileApplication)),
            XmlElement("XpoImport", typeof(XpoImport))]
            public List<AutorunElement> Steps { get; set; }

            public static AutoRun.AxaptaAutoRun DeserializeAutoRun(string filename)
            {
                AutoRun.AxaptaAutoRun autoRun = null;

                XmlSerializer serializer = new XmlSerializer(typeof(axb.AutoRun.AxaptaAutoRun));

                StreamReader reader = new StreamReader(filename);
                autoRun = (axb.AutoRun.AxaptaAutoRun)serializer.Deserialize(reader);
                reader.Close();

                return autoRun;
            }

            public static void SerializeAutoRun(AutoRun.AxaptaAutoRun autoRun, string filename)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(axb.AutoRun.AxaptaAutoRun));
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                // Set to blank so no namespace is used in output XML
                ns.Add("", "");
                TextWriter writer = new StreamWriter(filename);
                serializer.Serialize(writer, autoRun, ns);
                writer.Close();
            }

            public static AutoRun.AxaptaAutoRun FindOrCreate(string filename)
            {
                AutoRun.AxaptaAutoRun autoRun = null;

                if (System.IO.File.Exists(filename))
                {
                    autoRun = AxaptaAutoRun.DeserializeAutoRun(filename);
                }
                else
                {
                    autoRun = new AutoRun.AxaptaAutoRun();
                }

                return autoRun;
            }
        }

        public abstract class AutorunElement
        {
        }

        [Serializable]
        public class Run : AutorunElement
        {
            [XmlAttribute]
            public RunType type { get; set; }

            [XmlAttribute]
            public string name { get; set; }

            [XmlAttribute]
            public string method { get; set; }

            [XmlAttribute]
            public String parameters { get; set; }
        }

        [Serializable]
        public class CompileApplication : AutorunElement
        {
            [XmlAttribute]
            public String node { get; set; }

            [XmlAttribute]
            public bool crossReference { get; set; }
        }

        [Serializable]
        public class XpoImport : AutorunElement
        {
            [XmlAttribute("file")]
            public string File { get; set; }
        }
    }
}



