using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.Build.Client;

namespace axb
{
    class ClientConfigManager
    {
        public string ServerName { get; set; }
        public int PortNumber { get; set; }
        public string ClientBinPath { get; set; }
        public string Layer { get; set; }
        public string LayerCode { get; set; }
        public string LogPath { get; set; }

        public void load(string filePath)
        {
            using (StreamReader streamReader = new StreamReader(File.OpenRead(filePath)))
            {
                while (!streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine().Trim();

                    if (line.IndexOf(',') < 0)
                        continue;

                    string property = line.Substring(0, line.IndexOf(',')).ToUpper();
                    string value = line.Substring(line.LastIndexOf(',') + 1);

                    switch (property)
                    {
                        case "AOS2":
                            Match match = Regex.Match(value, @"@(.+):(\d+)");  // Group0 = entire match, Group1 = server name, Group2 = port

                            if (match.Success && match.Groups.Count == 3)
                            {
                                ServerName = match.Groups[1].Value;
                                PortNumber = UInt16.Parse(match.Groups[2].Value);
                            }
                            break;
                        case "BINDIR":
                            value = System.Environment.ExpandEnvironmentVariables(value);
                            ClientBinPath = value;
                            break;
                        case "AOL":
                            if (value == "")
                                value = "USR";

                            Layer = value;
                            break;
                        case "AOLCODE":
                            LayerCode = value;
                            break;
                        case "LOGDIR":
                            value = System.Environment.ExpandEnvironmentVariables(value);
                            LogPath = value;
                            break;
                    }
                }
            }
        }
    }
}


