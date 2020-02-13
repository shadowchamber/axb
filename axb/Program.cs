using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.TeamFoundation.Build.WebApi;
using Change = Microsoft.TeamFoundation.VersionControl.Client.Change;
using Microsoft.Dynamics.AX.Framework.Management.Reports;
using Microsoft.Dynamics.AX.Framework.Management;
using CommandLine;
using axb.Commands;
using System.ServiceProcess;
using Castle.Windsor;
using Castle.MicroKernel.Registration;
using Castle.Facilities.Logging;
using Castle.Services.Logging.NLogIntegration;
using Castle.Core.Logging;

namespace axb
{
    class Program
    {
        public async Task<int> RunAsync(string[] args)
        {
            int result = 0;

            Logger.Info("Parsing arguments");

            result = await CommandLine.Parser.Default.ParseArguments<
                        FullBuildOptions, 
                        XPODeployOptions, 
                        DeployOptions, 
                        SynchronizeDBOptions, 
                        XPODeployAndSynchronizeDBOptions, 
                        QueueBuildOptions,
                        RotateBackupsOptions,
                        UpdateJiraOptions,
                        AgentOptions,
                        SendMailOptions
                    >(args)
                    .MapResult(
                        (FullBuildOptions                   options) => container.Resolve<ICommandRepository<FullBuildOptions>>().RunAsync(options), 
                        (XPODeployOptions                   options) => container.Resolve<ICommandRepository<XPODeployOptions>>().RunAsync(options),
                        (DeployOptions                      options) => container.Resolve<ICommandRepository<DeployOptions>>().RunAsync(options),
                        (SynchronizeDBOptions               options) => container.Resolve<ICommandRepository<SynchronizeDBOptions>>().RunAsync(options),
                        (XPODeployAndSynchronizeDBOptions   options) => container.Resolve<ICommandRepository<XPODeployAndSynchronizeDBOptions>>().RunAsync(options),
                        (QueueBuildOptions                  options) => container.Resolve<ICommandRepository<QueueBuildOptions>>().RunAsync(options),
                        (RotateBackupsOptions               options) => container.Resolve<ICommandRepository<RotateBackupsOptions>>().RunAsync(options),
                        (UpdateJiraOptions                  options) => container.Resolve<ICommandRepository<UpdateJiraOptions>>().RunAsync(options),
                        (AgentOptions                       options) => container.Resolve<ICommandRepository<AgentOptions>>().RunAsync(options),
                        (SendMailOptions                    options) => container.Resolve<ICommandRepository<SendMailOptions>>().RunAsync(options),
                    errs => Task.FromResult(1))
                    ;

            return result;
        }

        public int Run(string[] args)
        {
            int result = 0;

            if (args.Count() > 0)
            {
                try
                {
                    var p = container.Resolve<Program>();
                    result = p.RunAsync(args).GetAwaiter().GetResult();
                }
                catch (Exception _e)
                {
                    Console.WriteLine(_e.Message);

                    return 1;
                }
            }
            else
            {
                Service service = new Service();

                ServiceBase.Run(service);
            }

            return result;
        }
        
        public static WindsorContainer container;

        private ILogger logger = NullLogger.Instance;

        public ILogger Logger
        {
            get { return logger; }
            set { logger = value; }
        }

        static int Main(string[] args)
        {
            int result = 0;

            container = new WindsorContainer();

            container.AddFacility<LoggingFacility>(f => f.LogUsing<NLogFactory>());

            container.Register(Component.For<Program>());
            container.Register(Component.For(typeof(ICommandRepository<>)).ImplementedBy(typeof(MainCommandRepository<>)).LifestyleSingleton());
            
            var p = container.Resolve<Program>();
            
            result = p.Run(args);

            container.Release(p);

            return result;
        }
    }
}
