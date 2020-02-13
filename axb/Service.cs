using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace axb
{
    public class Service : ServiceBase
    {
        Thread thread = null;

        ClientContext runningLock = new ClientContext();
        bool running = true;

        class ClientContext
        {
            public TcpClient Client;
            public Stream Stream;
            public byte[] Buffer = new byte[4];
            public MemoryStream Message = new MemoryStream();

            public void RunCommand(string args)
            {
                ProcessStartInfo processStartInfo;

                string exeFilePath = Process.GetCurrentProcess().MainModule.FileName;
                string path = Path.GetDirectoryName(exeFilePath);

                this.SendMessage("Executing: " + exeFilePath + " " + args);

                processStartInfo = new ProcessStartInfo(exeFilePath, args);
                processStartInfo.WindowStyle = ProcessWindowStyle.Normal; // ProcessWindowStyle.Minimized;
                processStartInfo.WorkingDirectory = path;

                Process process = new Process();

                processStartInfo.UseShellExecute = false;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.CreateNoWindow = true;

                process.StartInfo = processStartInfo;

                process.ErrorDataReceived += Process_DataReceived;
                process.OutputDataReceived += Process_DataReceived;

                process.Start();

                process.WaitForExit();

                this.SendMessage(process.StandardOutput.ReadToEnd());
                this.SendMessage(process.StandardError.ReadToEnd());

                if (!process.HasExited
                    && !process.WaitForExit((int)new TimeSpan(4, 0, 0).TotalMilliseconds))   // Don't care about sub-millisecond precision here.
                {
                    this.SendMessage("Operation timed out");
                }

                this.SendClose();

                Client.Close();
                Stream.Dispose();
                Stream = null;
            }

            void SendClose()
            {
                int size = 0;
                byte[] intBuff = new byte[4];
                intBuff = BitConverter.GetBytes(size);
                Stream.Write(intBuff, 0, intBuff.Length);
            }

            void SendMessage(string text)
            {
                if (String.IsNullOrEmpty(text))
                {
                    return;
                }

                int size = text.Length;
                byte[] intBuff = new byte[4];
                intBuff = BitConverter.GetBytes(size);
                Stream.Write(intBuff, 0, intBuff.Length);

                byte[] dataSend = Encoding.UTF8.GetBytes(text);
                Stream.Write(dataSend, 0, dataSend.Length);
            }

            private void Process_DataReceived(object sender, DataReceivedEventArgs e)
            {
                string strMessage = e.Data;

                Console.WriteLine(strMessage);

                if (!String.IsNullOrEmpty(strMessage))
                {
                    this.SendMessage(strMessage);
                }
            }
        }

        public static string args { get; set; }

        public Service()
        {
            ServiceName = "AXB Remote Deploy Service";
        }

        void OnMessageReceived(ClientContext context)
        {
            context.Message.Position = 0;

            StreamReader streamReader = new StreamReader(context.Message);
            string text = streamReader.ReadToEnd();

            try
            {
                context.RunCommand(text);
            }
            catch (Exception _e)
            {
                Console.WriteLine(_e.Message);
            }

            context.Message = new MemoryStream();
        }

        void OnClientRead(IAsyncResult ar)
        {
            ClientContext context = ar.AsyncState as ClientContext;
            if (context == null)
                return;

            try
            {
                int read = context.Stream.EndRead(ar);

                int length = BitConverter.ToInt32(context.Buffer, 0);
                byte[] buffer = new byte[1024];
                while (length > 0)
                {
                    read = context.Stream.Read(buffer, 0, Math.Min(buffer.Length, length));
                    context.Message.Write(buffer, 0, read);
                    length -= read;
                }

                OnMessageReceived(context);
            }
            catch (System.Exception)
            {
                context.Client.Close();
                context.Stream.Dispose();
                context.Message.Dispose();
                context = null;
            }
            finally
            {
                if (context != null && context.Stream != null)
                {
                    context.Stream.BeginRead(context.Buffer, 0, context.Buffer.Length, OnClientRead, context);
                }
            }
        }

        void OnClientAccepted(IAsyncResult ar)
        {
            TcpListener listener = ar.AsyncState as TcpListener;
            if (listener == null)
                return;

            try
            {
                ClientContext context = new ClientContext();
                context.Client = listener.EndAcceptTcpClient(ar);
                context.Stream = context.Client.GetStream();
                context.Stream.BeginRead(context.Buffer, 0, context.Buffer.Length, OnClientRead, context);
            }
            finally
            {
                listener.BeginAcceptTcpClient(OnClientAccepted, listener);
            }
        }

        public void MainThread()
        {
            Program p = new Program();
            TcpListener listener = null;

            try
            {
                IPAddress localAddress = IPAddress.Any;
                listener = new TcpListener(localAddress, 35777);
                listener.Start();

                listener.BeginAcceptTcpClient(OnClientAccepted, listener);

                while (true)
                {
                    lock (runningLock)
                    {
                        if (!running)
                        {
                            break;
                        }
                    }

                    Thread.Sleep(1000);
                }

                listener.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        protected override void OnStart(string[] args)
        {
            thread = new Thread(MainThread);

            thread.Start();
        }

        protected override void OnStop()
        {
            lock (runningLock)
            {
                running = false;

                Thread.Sleep(2000);
            }

            if (thread.IsAlive)
            {
                thread.Abort();
            }
        }
    }
}
