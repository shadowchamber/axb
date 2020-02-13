using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;
using Castle.MicroKernel.Facilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace axb.Commands
{
    public interface ICommandOptions
    {

    }

    public interface ICommandRepository<T> where T : ICommandOptions
    {
        Task<int> RunAsync(T options);
    }

    public class MainCommandRepository<T> : ICommandRepository<T> where T : ICommandOptions
    {
        public Task<int> RunAsync(T options)
        {
            string typeName = "axb.Commands." + typeof(T).Name.Replace("Options", "");

            Type commandType = Type.GetType(typeName);

            Object command = Activator.CreateInstance(commandType);

            Type objType = command.GetType();
            MethodInfo methodInfo = objType.GetMethod("RunAsync");

            return (Task<int>)methodInfo.Invoke(command, new object[] { (T)options });
        }
    }
}
