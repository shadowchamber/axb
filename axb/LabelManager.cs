using System;
using System.Linq;
using System.IO;
using Microsoft.TeamFoundation.Build.Client;

namespace axb
{
    class LabelManager
    {
        private string[] labelFileFilters = { "*.ald", "*.alc", "*.ali" };
        public void Clear(string ServerLabelFilePath)
        {
            string serverLabelFilePath = ServerLabelFilePath;

            if (String.IsNullOrWhiteSpace(serverLabelFilePath))
            {
                throw new Exception("Label file path not specified");
            }

            if (!Directory.Exists(serverLabelFilePath))   // TODO - Handle non-local server?
            {
                throw new Exception("Cannot access server label file path: " + serverLabelFilePath);
            }

            string fileslog = "";

            foreach (string fileName in labelFileFilters.AsParallel().SelectMany(searchPattern => Directory.EnumerateFiles(serverLabelFilePath, searchPattern)))
            {
                fileslog += " " + Path.GetFileName(fileName);

               // Console.WriteLine(String.Format("Attempting to delete {0}", fileName), BuildMessageImportance.Normal);
                // An exception only from deleting the label file is not severe enough
                // to fail the build step.  It must be logged with the proper importance though.
                try
                {
                    File.Delete(fileName);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine(String.Format("Access error deleting {0}: {1}", fileName, ex.Message), BuildMessageImportance.High);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(String.Format("General error deleting {0}: {1}", fileName, ex.Message), BuildMessageImportance.High);
                }
            }

            Console.WriteLine(fileslog);
        }
    }
}


