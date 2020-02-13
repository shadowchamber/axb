using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.ServiceProcess;
using Microsoft.TeamFoundation.Build.Client;

namespace axb
{
    class ServerConfigManager
    {
        public string ServerServiceIdentifier;
        public string ServerBinPath;
        public string ServerLogPath;
        public string ServerApplicationPath;
        public string ServerLabelFilePath;
        public string DatabaseServer;
        public string DatabaseName;
        public string AOSName;

        private const string aosRegistryPath = @"SYSTEM\CurrentControlSet\services\Dynamics Server\";

        public void load(string serverName, int portNumber)
        {
            Console.WriteLine(String.Format("Loading server configuration for Machine Name: {0}", System.Environment.MachineName));
            Console.WriteLine(String.Format("Server: {0} Port: {1}", serverName, portNumber));

            string aosRegPath = ServerConfigManager.aosRegistryPath;

            aosRegPath = aosRegPath + "6.0";

            RegistryKey aosEntries;

            // The code technically supports execution from a remote machine
            // This is untested and the usefulness is questionable

            if (serverName != System.Environment.MachineName)
            {
                // Open the registry on the remote machine
                aosEntries = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, serverName);
                
                // Get the list of servers running on the remote machine.
                aosEntries = aosEntries.OpenSubKey(aosRegPath);
            }
            else
            {
                // Get the list of servers running on this machine.
                aosEntries = Registry.LocalMachine.OpenSubKey(aosRegPath);
            }

            // Get subkeys which contain the different AOS instances
            string[] aosRegistryEntries = aosEntries.GetSubKeyNames();

            // Try and find the windows service identifier for the AOS on the given server/portnumber
            bool foundService = false;
            foreach (string aosRegistryEntry in aosRegistryEntries)
            {

                RegistryKey aosRootKey;
                RegistryKey aosInstanceKey;

                if (serverName != System.Environment.MachineName)
                {
                    // Open the AOS instance root key
                    aosRootKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, serverName);
                    // Use the 'current' key value to find the current settings for the instance
                    aosRootKey = aosRootKey.OpenSubKey(aosRegPath + @"\" + aosRegistryEntry);

                    aosInstanceKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, serverName);
                    aosInstanceKey = aosInstanceKey.OpenSubKey(aosRegPath + @"\" + aosRegistryEntry + @"\" + aosRootKey.GetValue("Current"));
                }
                else
                {
                    aosRootKey = Registry.LocalMachine.OpenSubKey(aosRegPath + @"\" + aosRegistryEntry);
                    aosInstanceKey = Registry.LocalMachine.OpenSubKey(aosRegPath + @"\" + aosRegistryEntry + @"\" + aosRootKey.GetValue("Current"));
                }

                // Check if this instance is tied to the port the AOS of interest is on
                if (aosInstanceKey.GetValue("Port").Equals(portNumber.ToString()))
                {
                    if (foundService)
                    {
                        // The port number matches, but we've already found another service entry running on the same port.
                        // Can't proceed because the port number was not strong enough to uniquely identify a single service.
                        throw new Exception("Multiple service instances found running on port " + portNumber);
                    }

                    foundService = true;

                    ServerServiceIdentifier = "AOS60$" + aosRegistryEntry;

                    ServerBinPath = aosInstanceKey.GetValue("bindir").ToString();
                    ServerLogPath = aosInstanceKey.GetValue("logdir").ToString(); // RRB
                    ServerApplicationPath = aosInstanceKey.GetValue("directory").ToString();
                    ServerLabelFilePath = aosInstanceKey.GetValue("directory").ToString() + @"\Appl\Standard";
                    DatabaseServer = aosInstanceKey.GetValue("dbserver").ToString();
                    DatabaseName = aosInstanceKey.GetValue("database").ToString();
                    AOSName = aosRootKey.GetValue("InstanceName").ToString();
                }
            }

            if (!foundService)
            {
                throw new Exception("Could not find configuration for server running on port " + portNumber);
            }
        }
    }
}




