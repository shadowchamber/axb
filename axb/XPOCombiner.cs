using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.Client;
using System.IO;
using System.Text.RegularExpressions;

namespace axb
{
    class XPOCombiner
    {
        const string XPOSTARTLINE1 = @"Exportfile for AOT version 1.0 or later";
        const string XPOSTARTLINE2 = @"Formatversion: 1";
        const string XPOENDLINE1 = @"***Element: END";
        public string SystemClasses = "syssetupformrun.xpo,info.xpo,classfactory.xpo,application.xpo,session.xpo";
        public string XPOFolder = "C:\\tfs\\dev\\axdev\\EliciteCustomizations\\";
        public bool Recursive = true;
        public string CombinedXPOFilename = "c:\\tfs\\bin\\combined.xpo";
        public string SystemClassesXPOFilename = "c:\\tfs\\bin\\combinedsystem.xpo";
        public string MacroName = "";
        public string MacroContents = "";

        public void Combine()
        {
            List<string> systemClasses = new List<string>(SystemClasses.ToString().Split(','));
            List<string> files = new List<string>();

            GetFiles(XPOFolder, Recursive, files);

            if (files.Count != 0)
            {
                StreamWriter writer = new StreamWriter(CombinedXPOFilename, false, Encoding.Unicode);
                writer.Write(String.Format("{0}{1}", XPOSTARTLINE1, System.Environment.NewLine));
                writer.Write(String.Format("{0}{1}", XPOSTARTLINE2, System.Environment.NewLine));

                StreamWriter systemWriter = null;
                if (!string.IsNullOrEmpty(SystemClassesXPOFilename))
                {
                    systemWriter = new StreamWriter(SystemClassesXPOFilename, false, Encoding.Unicode);
                    systemWriter.Write(String.Format("{0}{1}", XPOSTARTLINE1, System.Environment.NewLine));
                    systemWriter.Write(String.Format("{0}{1}", XPOSTARTLINE2, System.Environment.NewLine));
                }

                List<string>.Enumerator fileEnumerator = files.GetEnumerator();
                while (fileEnumerator.MoveNext())
                {
                    if (systemWriter != null && systemClasses.Contains(System.IO.Path.GetFileName(fileEnumerator.Current).ToLower()))
                    {
                        Combine(fileEnumerator.Current, systemWriter);
                    }
                    else
                    {
                        Combine(fileEnumerator.Current, writer);
                    }
                }

                if (!String.IsNullOrEmpty(MacroName))
                {
                    writer.WriteLine("");
                    AddMacro(MacroName, MacroContents, writer);
                    writer.WriteLine("");
                }

                writer.Write(String.Format("{0}{1}", XPOENDLINE1, System.Environment.NewLine));
                writer.Flush();
                writer.Close();

                if (systemWriter != null)
                {
                    systemWriter.Write(String.Format("{0}{1}", XPOENDLINE1, System.Environment.NewLine));
                    systemWriter.Flush();
                    systemWriter.Close();
                }
            }
            else
                throw new Exception(String.Format("No XPO files found in {0}", XPOFolder));
        }

        private void AddMacro(string macroName, string contents, StreamWriter combinedFile)
        {
            string xpoText = "";

            xpoText = String.Format("***Element: MCR{0}", System.Environment.NewLine);
            xpoText = String.Format("{0}{1}", xpoText, System.Environment.NewLine);
            xpoText = String.Format("{0}; Microsoft Dynamics AX Macro: {1} unloaded{2}", xpoText, macroName, System.Environment.NewLine);
            xpoText = String.Format("{0}; --------------------------------------------------------------------------------{1}", xpoText, System.Environment.NewLine);
            xpoText = String.Format("{0}  JOBVERSION 1{1}", xpoText, System.Environment.NewLine);
            xpoText = String.Format("{0}  {1}", xpoText, System.Environment.NewLine);
            xpoText = String.Format("{0}  SOURCE #{1}{2}", xpoText, macroName, System.Environment.NewLine);

            string[] lines = contents.Split('\n');
            foreach (string line in lines)
            {
                xpoText = String.Format("{0}    #{1}{2}", xpoText, line, System.Environment.NewLine);
            }
            xpoText = String.Format("{0}  ENDSOURCE{1}", xpoText, System.Environment.NewLine);

            combinedFile.Write(xpoText);
        }

        private void Combine(string filename, StreamWriter combinedFile)
        {
            using (StreamReader streamReader = new StreamReader(File.OpenRead(filename)))
            {
                while (!streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine();

                    if (line != XPOENDLINE1
                        && line != XPOSTARTLINE1
                        && line != XPOSTARTLINE2)
                    {
                        combinedFile.Write(String.Format("{0}{1}", line, System.Environment.NewLine));
                    }
                }
            }
        }

        private void GetFiles(string directory, bool recursive, List<string> files)
        {
            foreach (string filename in Directory.GetFiles(directory, "*.xpo"))
            {
                files.Add(filename);
            }

            if (recursive)
            {
                foreach (string subdirectory in Directory.GetDirectories(directory))
                {
                    GetFiles(subdirectory, recursive, files);
                }
            }
        }
    }
}

