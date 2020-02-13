using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow.Activities;
using System.IO;
using System.Xml;
using NLog;

namespace axb
{
    class XppCompileParser
    {
        public string LogPath { get; set; }

        public bool ShowTODOs { get; set; }
        public bool ShowWarnings { get; set; }
        public bool ShowErrors { get; set; }
        public bool ShowBestPractices { get; set; }
        public bool FailBuildOnError { get; set; }

        Logger logger = LogManager.GetCurrentClassLogger();

        public void log(string msg)
        {
            logger.Info(msg);

            // Console.WriteLine(String.Format("[{0}]: {1}", DateTime.Now, msg));
        }

        public void Execute()
        {
            bool failBuildOnError = FailBuildOnError;
            bool hasErrors = false;
            bool showTODOs = ShowTODOs;
            bool showWarnings = ShowWarnings;
            bool showErrors = ShowErrors;
            bool showBestPractices = ShowBestPractices;
            string filePath = LogPath + @"\AxCompileAll.html";

            XmlDocument doc = GetXML(filePath);

            XmlNodeList nodes;
            System.Collections.IEnumerator nodeEnumerator;

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("Table", "urn:www.microsoft.com/Formats/Table");
            nodes = doc.SelectNodes("//Table:Record", nsmgr);
            if (nodes != null)
            {
                nodeEnumerator = nodes.GetEnumerator();
                while (nodeEnumerator.MoveNext())
                {
                    XmlNode node = nodeEnumerator.Current as XmlNode;

                    string treeNodePath = node.SelectSingleNode("Table:Field[@name='TreeNodePath']", nsmgr).InnerText;
                    string line = node.SelectSingleNode("Table:Field[@name='Line']", nsmgr).InnerText;
                    string column = node.SelectSingleNode("Table:Field[@name='Column']", nsmgr).InnerText;
                    string severity = node.SelectSingleNode("Table:Field[@name='SysCompilerSeverity']", nsmgr).InnerText;
                    string message = node.SelectSingleNode("Table:Field[@name='SysCompileErrorMessage']", nsmgr).InnerText;

                    string output = String.Format("{0}, line {1}, column {2} : {3}", treeNodePath, line, column, message);

                    switch (severity)
                    {
                        case "0":
                            if (showErrors)
                            {
                                log("Error: " + output);
                            }
                            hasErrors = true;
                            break;
                        case "1":
                        case "2":
                        case "3":
                            if (showWarnings)
                                log(output);
                            break;
                        case "4":
                            if (showBestPractices)
                                log(output);
                            break;
                        case "254":
                        case "255":
                            if (showTODOs)
                                log(output);
                            break;
                    }
                }

                if (failBuildOnError && hasErrors)
                    throw new Exception("X++ Compile Error(s)");
            }
        }

        protected XmlDocument GetXML(string filename)
        {
            string xml = "";

            bool xmlBlock = false;
            using (StreamReader streamReader = new StreamReader(File.OpenRead(filename)))
            {
                while (!streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine().Trim();

                    if (line == "<AxaptaCompilerOutput>")
                        xmlBlock = true;

                    if (!xmlBlock)
                        continue;

                    xml = String.Format("{0}{1}{2}", xml, line, System.Environment.NewLine);

                    if (line == "</AxaptaCompilerOutput>")
                        xmlBlock = false;
                }
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            return doc;
        }
    }
}

