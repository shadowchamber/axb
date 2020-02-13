using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Diagnostics;

namespace axb
{
    public enum ServiceStatus
    {
        Started,
        Stopped
    }

    class AOSManager
    {
        public String ServiceId { get; set; }
        public String ServerName { get; set; }
        public UInt16 TimeOutMinutes { get; set; }

        public void start()
        {
            if (String.IsNullOrWhiteSpace(ServiceId))
            {
                throw new Exception("Service identifier not defined");
            }

            if (String.IsNullOrWhiteSpace(ServerName))
            {
                throw new Exception("Server name not defined");
            }

            ServiceController service = new ServiceController(ServiceId, ServerName);

            if (service.Status == ServiceControllerStatus.Running)
            {
                return;
            }

            Console.WriteLine(String.Format("Current status: {0}", service.Status));

            if (service.Status != ServiceControllerStatus.StartPending)
            {
                Console.WriteLine(String.Format("Initiating start"));
                service.Start();
            }

            Console.WriteLine(String.Format("Waiting"));

            service.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, TimeOutMinutes, 0));
        }

        public void stop()
        {
            if (String.IsNullOrWhiteSpace(ServiceId))
            {
                throw new Exception("Service identifier not defined");
            }

            if (String.IsNullOrWhiteSpace(ServerName))
            {
                throw new Exception("Server name not defined");
            }

            ServiceController service = new ServiceController(ServiceId, ServerName);

            Console.WriteLine(String.Format("Current status: {0}", service.Status));

            if (service.Status == ServiceControllerStatus.Stopped)
            {
                return;
            }

            if (service.Status != ServiceControllerStatus.StopPending)
            {
                Console.WriteLine(String.Format("Initiating stop"));

                service.Stop();
            }

            Console.WriteLine(String.Format("Waiting"));

            service.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, TimeOutMinutes, 0));
        }

        public void KillClient()
        {
            Process[] procList = Process.GetProcessesByName("Ax32.exe"); // TODO: choose correct instance

            if (procList == null)
                return;

            foreach (Process proc in procList)
            {
                proc.Kill();
            }
        }
    }
}
